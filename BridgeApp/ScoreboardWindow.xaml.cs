using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
            this.Hide();
        }

        //Delaney Moore wrote this update function
        public void Update(Scoreboard s)
        {
            txt_t1g1.Text = s.t1gamescore[0].ToString(); 
            txt_t2g1.Text = s.t2gamescore[0].ToString();
            txt_t1g2.Text = s.t1gamescore[1].ToString(); 
            txt_t2g2.Text = s.t2gamescore[1].ToString();
            txt_t1g3.Text = s.t1gamescore[2].ToString();
            txt_t2g3.Text = s.t2gamescore[2].ToString();
            txt_t1ut.Text = s.t1bonus.ToString();
            txt_t2ut.Text = s.t2bonus.ToString();
            txt_t1tp.Text = s.t1total.ToString();
            txt_t2tp.Text = s.t2total.ToString();
        }
    }
}
