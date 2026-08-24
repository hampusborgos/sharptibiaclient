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

            try
            {
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
