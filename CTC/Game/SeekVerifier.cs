using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace CTC
{
    /// <summary>
    /// Checks that seeking lands on exactly the state playing straight through
    /// would have produced.
    ///
    /// This is the test for the snapshot machinery, and the reason it can exist
    /// at all is that nothing under the UI layer touches a graphics device:
    /// textures are only built inside GameRenderer.DrawImage. So the whole engine
    /// can be driven here with no window.
    ///
    /// What it is really guarding against is a field somebody adds to
    /// ClientCreature or ClientViewport later and forgets to copy. That produces
    /// no compiler error and no crash, just a state that quietly disagrees with
    /// itself after a seek.
    ///
    ///     CTC --verify-seek recordings/Xyvero.tmv
    /// </summary>
    public static class SeekVerifier
    {
        public static int Run(String MoviePath)
        {
            FileInfo File = new FileInfo(MoviePath);
            Stream Input = File.OpenRead();
            if (File.Extension == ".tmv")
                Input = new System.IO.Compression.GZipStream(
                    Input, System.IO.Compression.CompressionMode.Decompress);

            TibiaMovieRecording Recording = TibiaMovieRecording.Load(Input, File.Name);
            Console.WriteLine(
                "{0}: {1} packets, {2:hh\\:mm\\:ss}", File.Name, Recording.Packets.Count, Recording.Duration);

            ClientState State = new ClientState(Recording);
            State.PrimeToLogin();

            // Two thirds in, so there is plenty of recording either side of it.
            TimeSpan Target = TimeSpan.FromTicks((long)(Recording.Duration.Ticks * 0.66));

            int Failures = 0;

            Stopwatch Watch = Stopwatch.StartNew();
            State.SeekTo(Target);
            Console.WriteLine(
                "played through to {0:hh\\:mm\\:ss} in {1}ms, {2} snapshots held",
                Target, Watch.ElapsedMilliseconds, State.SnapshotCount);

            String Expected = Fingerprint(State);

            Failures += Check(State, Target, Expected, "rewind to the start and come back",
                delegate(ClientState S)
                {
                    S.SeekTo(S.SeekableStart);
                    S.SeekTo(Target);
                });

            Failures += Check(State, Target, Expected, "step back 90 seconds and return",
                delegate(ClientState S)
                {
                    S.SeekTo(Target - TimeSpan.FromSeconds(90));
                    S.SeekTo(Target);
                });

            Failures += Check(State, Target, Expected, "step back 10 minutes and return",
                delegate(ClientState S)
                {
                    S.SeekTo(Target - TimeSpan.FromMinutes(10));
                    S.SeekTo(Target);
                });

            // The ladder: walk backwards a whole snapshot interval one reverse
            // step at a time, then come back. This is the path normal reverse
            // playback takes, and the one that exercises the fine snapshots.
            Failures += Check(State, Target, Expected, "reverse a step at a time through a block",
                delegate(ClientState S)
                {
                    TimeSpan Step = TimeSpan.FromSeconds(1.0 / 15.0);
                    for (TimeSpan At = Target; At > Target - TimeSpan.FromSeconds(35); At -= Step)
                        S.SeekTo(At);
                    S.SeekTo(Target);
                });

            // A state that has never been anywhere near the target, reached in one
            // jump. Nothing to restore from but the baseline.
            {
                ClientState Fresh = new ClientState(Recording);
                Fresh.PrimeToLogin();
                Watch.Restart();
                Fresh.SeekTo(Target);
                String Got = Fingerprint(Fresh);
                Boolean Ok = Got == Expected;
                Console.WriteLine("{0} one jump from the start ({1}ms)",
                    Ok ? "ok  " : "FAIL", Watch.ElapsedMilliseconds);
                if (!Ok)
                    Failures++;
            }

            Failures += CheckAliases(State);

            Console.WriteLine();
            Console.WriteLine("{0} snapshots held, {1:N1}MB managed heap",
                State.SnapshotCount, GC.GetTotalMemory(true) / (1024.0 * 1024.0));
            Console.WriteLine(Failures == 0 ? "all checks passed" : Failures + " CHECK(S) FAILED");
            return Failures == 0 ? 0 : 1;
        }

        private delegate void Excursion(ClientState State);

        private static int Check(
            ClientState State, TimeSpan Target, String Expected, String What, Excursion Go)
        {
            Stopwatch Watch = Stopwatch.StartNew();
            Go(State);
            long Elapsed = Watch.ElapsedMilliseconds;

            String Got = Fingerprint(State);
            if (Got == Expected)
            {
                Console.WriteLine("ok   {0} ({1}ms)", What, Elapsed);
                return 0;
            }

            Console.WriteLine("FAIL {0} ({1}ms)", What, Elapsed);
            Report(Expected, Got);
            return 1;
        }

        /// <summary>
        /// The aliasing a naive clone silently breaks.
        /// </summary>
        private static int CheckAliases(ClientState State)
        {
            ClientViewport Viewport = State.Viewport;
            int Failures = 0;

            if (Viewport.Player == null)
            {
                Console.WriteLine("FAIL player went missing");
                return 1;
            }

            ClientCreature Same;
            if (!Viewport.Creatures.TryGetValue(Viewport.Player.ID, out Same)
                || !ReferenceEquals(Same, Viewport.Player))
            {
                Console.WriteLine("FAIL Viewport.Player is not Creatures[PlayerID]");
                Failures++;
            }

            int Checked = 0;
            foreach (ClientTile Tile in Viewport.Map.AllTiles)
            {
                foreach (ClientThing Thing in Tile.Objects)
                {
                    ClientCreature Creature = Thing as ClientCreature;
                    if (Creature == null)
                        continue;

                    ClientCreature Listed;
                    if (Viewport.Creatures.TryGetValue(Creature.ID, out Listed)
                        && !ReferenceEquals(Listed, Creature))
                    {
                        Console.WriteLine(
                            "FAIL creature " + Creature.ID + " on a tile is a different object to Creatures[" + Creature.ID + "]");
                        Failures++;
                    }
                    Checked++;
                }
            }

            if (Failures == 0)
                Console.WriteLine("ok   aliasing held (" + Checked + " creatures on tiles)");
            return Failures;
        }

        private static void Report(String Expected, String Got)
        {
            String[] A = Expected.Split('\n');
            String[] B = Got.Split('\n');
            int Shown = 0;

            for (int i = 0; i < Math.Max(A.Length, B.Length) && Shown < 8; ++i)
            {
                String Left = i < A.Length ? A[i] : "<missing>";
                String Right = i < B.Length ? B[i] : "<missing>";
                if (Left == Right)
                    continue;

                Console.WriteLine("       expected: " + Left);
                Console.WriteLine("       got:      " + Right);
                Shown++;
            }
        }

        /// <summary>
        /// Every piece of state a clone could get wrong, in a stable order.
        ///
        /// Chat message timestamps are deliberately left out: ClientMessage stamps
        /// itself with DateTime.Now when it is parsed, so a replayed message never
        /// matches the original. The text and speaker are compared, which is what
        /// actually tells us the channel survived.
        /// </summary>
        public static String Fingerprint(ClientState State)
        {
            ClientViewport Viewport = State.Viewport;
            StringBuilder Out = new StringBuilder();

            Out.Append("view ").Append(Viewport.ViewPosition).Append('\n');
            Out.Append("light ").Append(Viewport.WorldLight.Level)
               .Append('/').Append(Viewport.WorldLight.Color).Append('\n');

            if (Viewport.Player == null)
            {
                Out.Append("player none\n");
            }
            else
            {
                ClientPlayer Player = Viewport.Player;
                Out.Append("player ").Append(Player.ID)
                   .Append(' ').Append(Player.Name)
                   .Append(" hp ").Append(Player.Health).Append('/').Append(Player.MaxHealth)
                   .Append(" mp ").Append(Player.Mana).Append('/').Append(Player.MaxMana)
                   .Append(" cap ").Append(Player.Capacity)
                   .Append(" exp ").Append(Player.Experience)
                   .Append(" lvl ").Append(Player.Level.Value).Append('%').Append(Player.Level.Percent)
                   .Append(" ml ").Append(Player.MagicLevel.Value).Append('%').Append(Player.MagicLevel.Percent)
                   .Append(" cond ").Append(Player.Conditions)
                   .Append(" dir ").Append(Player.Direction)
                   .Append('\n');

                foreach (String Key in Player.Skill.Keys.OrderBy(K => K, StringComparer.Ordinal))
                    Out.Append("skill ").Append(Key)
                       .Append(' ').Append(Player.Skill[Key].Value)
                       .Append('%').Append(Player.Skill[Key].Percent).Append('\n');
            }

            List<ClientTile> Tiles = Viewport.Map.AllTiles
                .OrderBy(T => T.Position.Z)
                .ThenBy(T => T.Position.Y)
                .ThenBy(T => T.Position.X)
                .ToList();

            foreach (ClientTile Tile in Tiles)
            {
                Out.Append("tile ").Append(Tile.Position)
                   .Append(" ground ").Append(Tile.Ground == null ? "-" : Tile.Ground.ID.ToString());
                foreach (ClientThing Thing in Tile.Objects)
                    Out.Append(' ').Append(Describe(Thing));
                Out.Append('\n');
            }

            for (int Slot = 0; Slot < Viewport.Inventory.Count; ++Slot)
                Out.Append("inv ").Append(Slot).Append(' ')
                   .Append(Describe(Viewport.Inventory[Slot])).Append('\n');

            foreach (int ID in Viewport.Containers.Keys.OrderBy(K => K))
            {
                ClientContainer Container = Viewport.Containers[ID];
                Out.Append("container ").Append(ID)
                   .Append(' ').Append(Container.Name)
                   .Append(" vol ").Append(Container.MaximumVolume)
                   .Append(" parent ").Append(Container.HasParent);
                if (Container.Contents != null)
                    foreach (ClientItem Item in Container.Contents)
                        Out.Append(' ').Append(Describe(Item));
                Out.Append('\n');
            }

            foreach (UInt32 ID in Viewport.Creatures.Keys.OrderBy(K => K))
                Out.Append("creature ").Append(Describe(Viewport.Creatures[ID])).Append('\n');

            foreach (UInt32 ID in Viewport.VIPList.Keys.OrderBy(K => K))
                Out.Append("vip ").Append(ID)
                   .Append(' ').Append(Viewport.VIPList[ID].Name)
                   .Append(' ').Append(Viewport.VIPList[ID].Online).Append('\n');

            foreach (UInt16 ID in Viewport.Channels.Keys.OrderBy(K => K))
            {
                ClientChannel Channel = Viewport.Channels[ID];
                Out.Append("channel ").Append(ID).Append(' ').Append(Channel.Name)
                   .Append(" messages ").Append(Channel.Messages.Count);
                if (Channel.Messages.Count > 0)
                {
                    ClientMessage Last = Channel.Messages[Channel.Messages.Count - 1];
                    Out.Append(" last ").Append(Last.Speaker).Append(": ").Append(Last.Text);
                }
                Out.Append('\n');
            }

            return Out.ToString();
        }

        private static String Describe(ClientThing Thing)
        {
            if (Thing == null)
                return "-";

            ClientCreature Creature = Thing as ClientCreature;
            if (Creature != null)
                return "c" + Creature.ID
                    + ":" + Creature.Name
                    + ":" + Creature.Health + "/" + Creature.MaxHealth
                    + ":" + Creature.Direction
                    + ":" + Creature.Speed
                    + ":" + Creature.Skull + "/" + Creature.Shield
                    + ":" + Creature.Light.Level + "/" + Creature.Light.Color
                    + ":" + Describe(Creature.Outfit);

            ClientContainer Container = Thing as ClientContainer;
            if (Container != null)
                return "k" + Container.ID + ":" + Container.ContainerID
                    + ":" + (Container.Contents == null ? 0 : Container.Contents.Count);

            ClientItem Item = Thing as ClientItem;
            if (Item != null)
                return "i" + Item.ID + ":" + Item.Subtype;

            return "?";
        }

        private static String Describe(ClientOutfit Outfit)
        {
            if (Outfit == null)
                return "-";
            return Outfit.LookType + "/" + Outfit.LookItem + "/" + Outfit.LookHead
                + "/" + Outfit.LookBody + "/" + Outfit.LookLegs + "/" + Outfit.LookFeet;
        }
    }
}
