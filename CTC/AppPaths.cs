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
                    // Every flag the client takes also has a value after it.
                    ++i;
                    continue;
                }
                return Args[i];
            }
            return null;
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
