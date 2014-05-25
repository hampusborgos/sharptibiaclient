using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using mshtml;

namespace CTC
{
    public partial class DebugWindow : Form
    {
        public DebugWindow()
        {
            InitializeComponent();

            //System.IO.StreamReader html = new System.IO.StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("RC.DebugPage.htm"));
            //DebugBrowser.DocumentText = html.ReadToEnd();

            Log.Instance.OnLogMessage += OnLogMessage;
        }

        ~DebugWindow()
        {
            Log.Instance.OnLogMessage -= OnLogMessage;
            //DebugBrowser.Dispose();
        }

        public void OnLogMessage(object sender, Log.Message message)
        {
            String m =
                message.time.ToLongTimeString()
                + " " + message.level.ToString() + " - "
                + (sender != null ? sender.ToString() : "Unknown") +
                ": " + message.text + "\r\n";
            DebugText.Text += m;
            DebugText.SelectionStart = DebugText.TextLength;
            DebugText.ScrollToCaret();
        }
    }
}
