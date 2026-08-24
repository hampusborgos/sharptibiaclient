using System;

namespace CTC
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        ///
        /// Usage: CTC [path/to/recording.tmv]
        /// With no argument, the first .tmv found next to the executable is played.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // The old WinForms DebugWindow is gone; send the same log stream to stdout.
            Log.Instance.OnLogMessage += WriteLogMessage;

            // --input-script <file> drives the interface from a script instead of
            // the real mouse, so interaction tests do not move the user's cursor.
            for (int i = 0; i < args.Length - 1; ++i)
            {
                if (args[i] == "--input-script")
                {
                    UIContext.Input = new ScriptedInputSource(args[i + 1]);
                    break;
                }
            }

            try
            {
                // --verify-seek checks the snapshot machinery against a straight
                // play-through, headless. See SeekVerifier.
                for (int i = 0; i < args.Length; ++i)
                {
                    if (args[i] == "--verify-seek")
                    {
                        Environment.Exit(SeekVerifier.Run(AppPaths.ResolveRecording(args)));
                        return;
                    }
                }

                using (Game game = new Game(args))
                {
                    game.Run();
                }
            }
            catch (System.IO.FileNotFoundException e)
            {
                // Missing Tibia.dat/Tibia.spr or recording: the message explains how to fix it.
                Console.Error.WriteLine();
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine();
                Environment.Exit(1);
            }
        }

        private static void WriteLogMessage(object sender, Log.Message message)
        {
            Console.WriteLine(
                "[" + message.time.ToString("HH:mm:ss") + "] " +
                message.level.ToString().ToUpperInvariant() + ": " +
                message.text);
        }
    }
}
