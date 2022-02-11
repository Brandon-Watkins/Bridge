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
            Hide();
        }

        //Delaney Moore wrote this update function
        public void Update(Scoreboard s)
        {
            txt_t1g1.Text = s.T1gamescore[0].ToString(); 
            txt_t2g1.Text = s.T2gamescore[0].ToString();
            txt_t1g2.Text = s.T1gamescore[1].ToString(); 
            txt_t2g2.Text = s.T2gamescore[1].ToString();
            txt_t1g3.Text = s.T1gamescore[2].ToString();
            txt_t2g3.Text = s.T2gamescore[2].ToString();
            txt_t1ut.Text = s.T1bonus.ToString();
            txt_t2ut.Text = s.T2bonus.ToString();
            txt_t1tp.Text = s.T1total.ToString();
            txt_t2tp.Text = s.T2total.ToString();
        }
    }
}
