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
    /// Interaction logic for Bidding_Window.xaml
    /// </summary>
    /// 
    //This is a dinosoar of early itterations for early testing it was quickly left for tiled bidding window
    public partial class Bidding_Window : Window
    {


        public event Action<Card.Face, int> onBidPlaced;
        public event Action onPass;

        private Card.Face lastBidFace = Card.Face.NoTrump;
        private int lastBidNum = -1;

        int currentBid =1;

        public Bidding_Window()
        {
            InitializeComponent();
        }

        public void UpdateBidText(string text)
        {
            OutputForBid_txt.Text = text;
        }
        public void UpdateCurrentPlayerText(int p)
        {
            txtCurrentPlayer.Text = "Waiting for bid from: Player " + p;
        }

        //so clearly the way it will need to work with this function is you bid and when the final bid has been tallied it will close the window
        private void Bid_Click(object sender, RoutedEventArgs e)
        {
            Card.Face bidFace = Card.Face.NoTrump;

            if ((bool)rbHearts.IsChecked)
                bidFace = Card.Face.Hearts;
            else if ((bool)rbClubs.IsChecked)
                bidFace = Card.Face.Clubs;
            else if ((bool)rbDiamonds.IsChecked)
                bidFace = Card.Face.Diamonds;
            else if ((bool)rbSpades.IsChecked)
                bidFace = Card.Face.Spades;
            else if ((bool)rbNoSuit.IsChecked)
                bidFace = Card.Face.NoTrump;
            else
                MessageBox.Show("nothing was checked");

            int bidNum = 0;
            if (Int32.TryParse(InputForBid_txt.Text, out bidNum))
            {
                if (isValidBid(bidFace, bidNum))
                {
                    lastBidNum = bidNum;
                    lastBidFace = bidFace;
                    onBidPlaced?.Invoke(bidFace, bidNum);
                }
                else
                {
                    MessageBox.Show("Not a valid bid!");
                }
            }
            else
            {
                MessageBox.Show("try putting in a number");
            }
        }

        //You can use this to simply pass the phase if you need it for testing.
        private void End_Bid_Phase_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void Pass_Click(object sender, RoutedEventArgs e)
        {
            onPass?.Invoke();
        }

        private bool isValidBid(Card.Face f, int num)
        {
            if (num < 1 || num > 7)
                return false;
            else if (num < lastBidNum)
                return false;
            else if (num == lastBidNum && f <= lastBidFace)
                return false;
            else
                return true;
        }

        public void selectBidNumber_Click(object sender, RoutedEventArgs e) 
        {
            var b = sender as Button;

            currentBid= (int)b.Content;
        }
    }
}
