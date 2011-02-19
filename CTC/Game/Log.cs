using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    public class Log
    {
        public static Log Instance;

        public enum Level
        {
            Fatal,
            Error,
            Warning,
            Notice,
            Debug
        }

        public class Message
        {
            public Level level;
            public string text;
            public object sender;
            public DateTime time;
        }

        static Log()
        {
            Instance = new Log();
        }

        public delegate void LogMessageHandler(object sender, Message message);
        public event LogMessageHandler OnLogMessage;

        private void Dispatch(object sender, Level level, String text)
        {
            if (OnLogMessage != null)
            {
                Message m = new Message();
                m.text = text;
                m.level = level;
                m.sender = sender;
                m.time = DateTime.Now;
                OnLogMessage(sender, m);
            }
        }

        public static void Debug(String message, object sender = null)
        {
            Instance.Dispatch(sender, Level.Debug, message);
        }

        public static void Notice(String message, object sender = null)
        {
            Instance.Dispatch(sender, Level.Notice, message);
        }

        public static void Warning(String message, object sender = null)
        {
            Instance.Dispatch(sender, Level.Warning, message);
        }

        public static void Error(String message, object sender = null)
        {
            Instance.Dispatch(sender, Level.Error, message);
        }

        public static void Fatal(String message, object sender = null)
        {
            Instance.Dispatch(sender, Level.Fatal, message);
        }
    }
}
