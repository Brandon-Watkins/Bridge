using System.Windows;
using ISU_Bridge;

namespace BridgeApp
{
    /// <summary>
    /// Interaction logic for ScoreboardWindow.xaml
    /// </summary>
    public partial class ScoreboardWindow : Window
    {

        public ScoreboardWindow()
        {
            InitializeComponent();
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This should explain the scoring");
        }
        private void Scoreboard_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // https://stackoverflow.com/questions/3001525/how-to-override-default-window-close-operation
            e.Cancel = true;
            this.Dispatcher.Invoke(() =>
            {
                Hide();
            });
        }

        //Delaney Moore wrote this update function
        public void Update(Scoreboard s)
        {
            this.Dispatcher.Invoke(() =>
            {
                txt_t1g1.Text = s.T1GameScore[0].ToString();
                txt_t2g1.Text = s.T2GameScore[0].ToString();
                txt_t1g2.Text = s.T1GameScore[1].ToString();
                txt_t2g2.Text = s.T2GameScore[1].ToString();
                txt_t1g3.Text = s.T1GameScore[2].ToString();
                txt_t2g3.Text = s.T2GameScore[2].ToString();
                txt_t1ut.Text = s.T1Bonus.ToString();
                txt_t2ut.Text = s.T2Bonus.ToString();
                txt_t1tp.Text = s.T1Total.ToString();
                txt_t2tp.Text = s.T2Total.ToString();
            });
        }
    }
}
