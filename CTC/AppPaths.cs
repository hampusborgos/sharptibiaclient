using System;
using System.IO;
using System.Linq;

namespace CTC
{
    /// <summary>
    /// Resolves the runtime files the client needs.
    ///
    /// The original code opened "./Tibia.dat", "./Tibia.spr" and "./Test.tmv" relative to
    /// the current working directory, which only ever worked because Visual Studio launched
    /// the game with the CWD set to the build output folder. Under "dotnet run" the CWD is
    /// the project directory, and inside a macOS .app bundle it is "/", so everything is
    /// resolved against the executable's own directory instead.
    /// </summary>
    public static class AppPaths
    {
        /// <summary>
        /// Directory holding Tibia.dat and Tibia.spr, copied next to the executable at build time.
        /// </summary>
        public static string DataDirectory
        {
            get { return Path.Combine(AppContext.BaseDirectory, "Data"); }
        }

        /// <summary>
        /// Directory holding the bundled .tmv recordings.
        /// </summary>
        public static string RecordingsDirectory
        {
            get { return Path.Combine(AppContext.BaseDirectory, "recordings"); }
        }

        /// <summary>
        /// Path to one of the Tibia client data files, verified to exist.
        /// </summary>
        public static string GameDataFile(string FileName)
        {
            string Full = Path.Combine(DataDirectory, FileName);
            if (!File.Exists(Full))
                throw new FileNotFoundException(
                    "Missing Tibia client data file '" + FileName + "'.\n" +
                    "Expected it at: " + Full + "\n\n" +
                    "Extract the 7.41 client data with:\n" +
                    "  innoextract -d /tmp/tibia741 original/installers/tibia741.exe\n" +
                    "  cp /tmp/tibia741/app/Tibia.dat /tmp/tibia741/app/Tibia.spr CTC/Data/",
                    Full);
            return Full;
        }

        /// <summary>
        /// Flags that stand on their own. Everything else is followed by a value,
        /// which must not be mistaken for the recording to play.
        /// </summary>
        private static readonly string[] ValuelessFlags = { "--verify-seek" };

        /// <summary>
        /// The first bare argument, skipping any --flag together with its value.
        /// </summary>
        private static string FirstNonFlag(string[] Args)
        {
            if (Args == null)
                return null;

            for (int i = 0; i < Args.Length; ++i)
            {
                if (Args[i].StartsWith("--"))
                {
                    if (Array.IndexOf(ValuelessFlags, Args[i]) < 0)
                        ++i;
                    continue;
                }
                return Args[i];
            }
            return null;
        }

        /// <summary>
        /// The value following a --flag, or null when it was not given.
        /// </summary>
        private static string FlagValue(string[] Args, string Flag)
        {
            if (Args == null)
                return null;

            for (int i = 0; i < Args.Length - 1; ++i)
                if (Args[i] == Flag)
                    return Args[i + 1];
            return null;
        }

        /// <summary>
        /// Where playback should start, from --start MM:SS (or HH:MM:SS, or a
        /// plain number of seconds). Defaults to the beginning.
        ///
        /// This replaces the hardcoded 30 minute fast-forward the client used to
        /// do on every launch, which existed to skip the dull opening of one
        /// particular recording and cost a second and a half of blank window.
        /// </summary>
        public static TimeSpan StartPosition(string[] Args)
        {
            string Given = FlagValue(Args, "--start");
            if (Given == null)
                return TimeSpan.Zero;

            // Deliberately not TimeSpan.Parse: it reads "30:00" as thirty hours,
            // then rejects it for being out of range. A timecode is what anyone
            // typing this actually means.
            string[] Parts = Given.Split(':');
            double[] Fields = new double[Parts.Length];
            for (int i = 0; i < Parts.Length; ++i)
                if (!double.TryParse(Parts[i], System.Globalization.NumberStyles.Float,
                                     System.Globalization.CultureInfo.InvariantCulture, out Fields[i]))
                    throw new FormatException(
                        "Could not read --start '" + Given + "'; expected [HH:]MM:SS or a number of seconds.");

            double Total = 0;
            foreach (double Field in Fields)
                Total = Total * 60 + Field;

            return TimeSpan.FromSeconds(Total);
        }

        /// <summary>
        /// The playback speed to start at, from --speed N. Defaults to realtime.
        /// </summary>
        public static double StartSpeed(string[] Args)
        {
            string Given = FlagValue(Args, "--speed");
            if (Given == null)
                return 1.0;

            double Speed;
            if (!double.TryParse(Given, System.Globalization.NumberStyles.Float,
                                 System.Globalization.CultureInfo.InvariantCulture, out Speed) || Speed <= 0)
                throw new FormatException("Could not read --speed '" + Given + "'; expected a positive number.");
            return Speed;
        }

        /// <summary>
        /// Picks the recording to play: the first command line argument if given (resolved
        /// against the shell's working directory, so tab completion works), otherwise the
        /// first .tmv found next to the executable.
        /// </summary>
        public static string ResolveRecording(string[] Args)
        {
            string Given = FirstNonFlag(Args);
            if (Given != null)
            {
                string Requested = Path.GetFullPath(Given);
                if (!File.Exists(Requested))
                    throw new FileNotFoundException("No such recording: " + Requested, Requested);
                return Requested;
            }

            if (Directory.Exists(RecordingsDirectory))
            {
                string First = Directory.GetFiles(RecordingsDirectory, "*.tmv")
                                        .OrderBy(Name => Name)
                                        .FirstOrDefault();
                if (First != null)
                    return First;
            }

            throw new FileNotFoundException(
                "No recording to play.\n" +
                "Pass one on the command line, e.g.\n" +
                "  dotnet run --project CTC -- recordings/Kolskagg.tmv\n" +
                "or place a .tmv file in " + RecordingsDirectory);
        }
    }
}
