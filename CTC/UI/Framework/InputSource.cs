using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace CTC
{
    /// <summary>
    /// Where the game reads the mouse from.
    ///
    /// Everything the UI does is driven by polled mouse state, so putting an
    /// interface in front of it is enough to drive the whole interface from a
    /// script - no real cursor, no window focus, and nothing fighting the
    /// person sitting at the machine.
    /// </summary>
    public interface IInputSource
    {
        /// <summary>
        /// Called once per frame, before the state is handed to the UI.
        /// </summary>
        void Update(GameTime Time);

        MouseState CurrentMouseState { get; }

        KeyboardState CurrentKeyboardState { get; }

        /// <summary>
        /// Set by a script when it wants the frame just drawn saved to disk.
        /// The game clears it once the file has been written.
        /// </summary>
        string PendingScreenshot { get; set; }

        /// <summary>
        /// Set by a script when it has run out of steps and the game should exit.
        /// </summary>
        bool Finished { get; }
    }

    /// <summary>
    /// The real mouse.
    /// </summary>
    public class SystemInputSource : IInputSource
    {
        public void Update(GameTime Time) { }

        public MouseState CurrentMouseState
        {
            get { return Mouse.GetState(); }
        }

        public KeyboardState CurrentKeyboardState
        {
            get { return Keyboard.GetState(); }
        }

        public string PendingScreenshot { get; set; }

        public bool Finished
        {
            get { return false; }
        }
    }

    /// <summary>
    /// Replays a list of mouse actions read from a text file.
    ///
    /// One step is consumed per frame. That matters: the UI only notices a click
    /// when the button state differs between two polls, so a press and release
    /// applied in the same frame would be invisible - which is exactly how real
    /// synthetic clicks get lost against an uncapped frame rate.
    ///
    /// Script commands, one per line, # for comments:
    ///
    ///   move X Y        put the cursor at X Y, in window coordinates
    ///   down [right]    press the left, or right, button
    ///   up [right]      release it
    ///   click [right]   down then up, taking two frames
    ///   keydown NAME    hold a key down, named as in Microsoft.Xna.Framework.Input.Keys
    ///   keyup NAME      let it go
    ///   key NAME        keydown then keyup, taking two frames
    ///   wait MS         hold still for MS milliseconds
    ///   shot PATH       save the next drawn frame as a png
    ///   quit            stop the game
    /// </summary>
    public class ScriptedInputSource : IInputSource
    {
        private struct Step
        {
            public string Command;
            public string[] Arguments;
        }

        private readonly Queue<Step> Steps = new Queue<Step>();

        private int X;
        private int Y;
        private ButtonState Left = ButtonState.Released;
        private ButtonState Right = ButtonState.Released;
        private readonly List<Keys> Held = new List<Keys>();

        private double WaitRemaining;

        public string PendingScreenshot { get; set; }
        public bool Finished { get; private set; }

        public ScriptedInputSource(string Path)
        {
            foreach (string RawLine in File.ReadAllLines(Path))
            {
                string Line = RawLine.Trim();
                if (Line.Length == 0 || Line.StartsWith("#"))
                    continue;

                string[] Parts = Line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                Step Next = new Step();
                Next.Command = Parts[0].ToLowerInvariant();
                Next.Arguments = new string[Parts.Length - 1];
                Array.Copy(Parts, 1, Next.Arguments, 0, Parts.Length - 1);

                // click is just a press and a release, but they have to land on
                // separate frames to be seen, so expand it here.
                if (Next.Command == "click")
                {
                    Steps.Enqueue(new Step { Command = "down", Arguments = Next.Arguments });
                    Steps.Enqueue(new Step { Command = "up", Arguments = Next.Arguments });
                    continue;
                }

                // Same for a key: the game only notices one that was up last frame
                // and is down this one.
                if (Next.Command == "key")
                {
                    Steps.Enqueue(new Step { Command = "keydown", Arguments = Next.Arguments });
                    Steps.Enqueue(new Step { Command = "keyup", Arguments = Next.Arguments });
                    continue;
                }

                Steps.Enqueue(Next);
            }
        }

        public MouseState CurrentMouseState
        {
            get
            {
                return new MouseState(
                    X, Y, 0,
                    Left, ButtonState.Released, Right,
                    ButtonState.Released, ButtonState.Released);
            }
        }

        public KeyboardState CurrentKeyboardState
        {
            get { return new KeyboardState(Held.ToArray()); }
        }

        public void Update(GameTime Time)
        {
            if (WaitRemaining > 0)
            {
                WaitRemaining -= Time.ElapsedGameTime.TotalMilliseconds;
                return;
            }

            if (Steps.Count == 0)
            {
                // Leave the last frame on screen long enough to be saved.
                if (PendingScreenshot == null)
                    Finished = true;
                return;
            }

            // Exactly one step per frame, so every state change is visible to
            // the polling the UI does.
            Step Current = Steps.Dequeue();
            switch (Current.Command)
            {
                case "move":
                    X = ParseInt(Current.Arguments, 0);
                    Y = ParseInt(Current.Arguments, 1);
                    break;

                case "down":
                    if (IsRight(Current.Arguments))
                        Right = ButtonState.Pressed;
                    else
                        Left = ButtonState.Pressed;
                    break;

                case "up":
                    if (IsRight(Current.Arguments))
                        Right = ButtonState.Released;
                    else
                        Left = ButtonState.Released;
                    break;

                case "keydown":
                    {
                        Keys Key;
                        if (ParseKey(Current.Arguments, out Key) && !Held.Contains(Key))
                            Held.Add(Key);
                    }
                    break;

                case "keyup":
                    {
                        Keys Key;
                        if (ParseKey(Current.Arguments, out Key))
                            Held.Remove(Key);
                    }
                    break;

                case "wait":
                    WaitRemaining = ParseInt(Current.Arguments, 0);
                    break;

                case "shot":
                    PendingScreenshot = Current.Arguments.Length > 0 ? Current.Arguments[0] : null;
                    break;

                case "quit":
                    // A screenshot is not written until the frame after the one
                    // that asked for it, so quitting straight after a shot would
                    // throw the file away. Wait a frame for it.
                    if (PendingScreenshot != null)
                        Steps.Enqueue(Current);
                    else
                        Finished = true;
                    break;

                default:
                    Log.Warning("Unknown input script command: " + Current.Command, this);
                    break;
            }
        }

        private static bool ParseKey(string[] Arguments, out Keys Key)
        {
            Key = Keys.None;
            if (Arguments.Length == 0)
                return false;

            if (!Enum.TryParse<Keys>(Arguments[0], true, out Key))
            {
                Log.Warning("Unknown key in input script: " + Arguments[0]);
                return false;
            }
            return true;
        }

        private static bool IsRight(string[] Arguments)
        {
            return Arguments.Length > 0 && Arguments[0].ToLowerInvariant() == "right";
        }

        private static int ParseInt(string[] Arguments, int Index)
        {
            if (Index >= Arguments.Length)
                return 0;

            int Value;
            if (!int.TryParse(Arguments[Index], NumberStyles.Integer, CultureInfo.InvariantCulture, out Value))
                return 0;
            return Value;
        }
    }
}
