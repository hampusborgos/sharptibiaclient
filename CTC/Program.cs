using System;

namespace CTC
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            DebugWindow dbw = new DebugWindow();
            dbw.Show();

            using (Game game = new Game())
            {
                game.Run();
            }
        }
    }
#endif
}

