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

namespace BridgeApp
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class startWindow : Window
    {
        //note for easy use. If you want to remove the start button the app.xaml chooses which window shows up first. 
        //This button apparently does very little at this point and is mostly so the player gets shown a start to everything
        public MainWindow gameWindow { get; private set; }
        public startWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }


        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            gameWindow = new MainWindow();
            gameWindow.Show();
        }
    }
}
