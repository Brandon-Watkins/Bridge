using System.Text;
using System.Windows;

namespace BridgeApp
{
    /// <summary>
    /// Interaction logic for ConsoleWindow.xaml
    /// </summary>
    public partial class BridgeConsole : Window
    {

        public static BridgeConsole instance => MainWindow.Instance.ConsoleWindow;

        private static readonly int maxLines = 40;

        public BridgeConsole()
        {
            InitializeComponent();

            txtLog.Text = "";
        }

        private void ConsoleWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // https://stackoverflow.com/questions/3001525/how-to-override-default-window-close-operation
            e.Cancel = true;
            Hide();
        }

        public static void Log(string s)
        {
            string time = System.DateTime.Now.ToString("hh:mm:ss");
            string logString = s + "\n" + instance.txtLog.Text;
            string[] lines = logString.Split('\n');
            if (lines.Length > maxLines)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < maxLines; i++)
                {
                    sb.Append(lines[i]);
                }
                logString = sb.ToString();
            }
            logString = "[" + time + "] - " + logString;
            instance.txtLog.Text = logString;
        }

        public static void Clear()
        {
            instance.txtLog.Text = "";
        }
    }
}
