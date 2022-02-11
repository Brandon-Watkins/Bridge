using System;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ISU_Bridge;

namespace BridgeApp
{
    //Keep a bool of whoes player is
    //see if you can layer buttons 
    // there is a property called topmost could set it to active whenever you hover over a button
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private string _addresses;
        private Image[] _northHandNames;
        private Image[] _southHandNames;
        private Image[] _eastHandNames;
        private Image[] _westHandNames;
        private Image[] _northTricksDisplay;
        private Image[] _southTricksDisplay;
        private Image[] _eastTricksDisplay;
        private Image[] _westTricksDisplay;
        private Button[] _northButtons;
        private Button[] _southButtons;
        private Button[] _eastButtons;
        private Button[] _westButtons;
        private Button[][] _allButtons;


        public static MainWindow Instance { get; private set; }
        public TiledBiddingWindow Bid { get; private set; }
        public ScoreboardWindow ScoreboardWindow { get; private set; }
        public BridgeConsole ConsoleWindow { get; private set; }

        public MainWindow()
        {
            Instance = this;

            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            //This should give me the way to load a file dynamically
            //http://csharphelper.com/blog/2015/07/load-an-image-at-runtime-in-wpf-and-c/

            GetAddresses();

            ScoreboardWindow = new ScoreboardWindow();
            Bid = new TiledBiddingWindow();
            // https://stackoverflow.com/questions/2546566/how-to-make-a-wpf-window-be-on-top-of-all-other-windows-of-my-app-not-system-wi
            Bid.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Bid.Topmost = true;
            Bid.Hide();

            ConsoleWindow = new BridgeConsole();
            ConsoleWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (Game.debugging) ConsoleWindow.Show();

            BuildArraysForDisplay();
            BuildArraysOfButtons();

            // Flipping South and West's hands around to compensate for the table's layout inconsistencies (N and E card indices go from left to right, S and W are right to left)
            spSouth.LayoutTransform = new RotateTransform(180);
            spWest.LayoutTransform = new RotateTransform(180);

            Table.Initialize();
        }

        /// <summary>
        /// This will find the current directory so abosolute directions are usable.
        /// Unless you really know you're way around WPF, and explicitely WPF, do not try to use relative URLs they do not work
        /// </summary>
        private void GetAddresses()
        {
            //Note if this breaks it probably is because of the only weakness this has, 
            //which is that if bridgeAPP appears to early or to repeatedly the location it searches for images could be wrong
            //This probably will not break but if it does work to fix how the directory is got not the relative URL.
            //It is much easier to mess with strings than get an app specifically made to run on the internet to run exclusively on the local directories.

            string getAddress = Directory.GetCurrentDirectory();
            //remember the double backslash for a single backslash
            int position = getAddress.IndexOf("BridgeApp\\");

            int secondposition = getAddress.IndexOf("BridgeApp\\BridgeApp\\");
            if (position == secondposition)
            {
                //a simple error check when people use a zip folder it often creates a second outer folder and sometimes people forget to get rid of the outer folder
                //best to make sure this doesn't ruin the project.
                position += 20;
            }
            else
            {
                position += 10;
            }
            getAddress = getAddress.Remove(position);
            _addresses = getAddress;
        }

        //https://stackoverflow.com/questions/397117/wpf-image-dynamically-changing-image-source-during-runtime
        //imgNameDispaly is refering to the image in the xaml file
        /// <summary>
        /// This will display a card to a given image object
        /// </summary>
        /// <param name="chosenCard">Card to display</param>
        /// <param name="display">Image to display in</param>
        private void ShowCard(Card chosenCard, Image display)
        {
            //This is a little complicated and worth noting. 
            //WPF is tempermental and cannot use relative locations attempting to do so will only bring missery.
            //instead always use abosolute directions made with getAddresses
            string address = _addresses + "cards/" + chosenCard.ImageLocation;
            display.Source = new BitmapImage(new Uri(address));
        }

        /// <summary>
        /// This will display the correct card but a modified version that's yellow to show which cards to play
        /// </summary>
        /// <param name="chosenCard">Card to display</param>
        /// <param name="display">Image to display in</param>
        private void ShowCardAvailable(Card chosenCard, Image display)
        {
            string address = _addresses + "cards/" + "available" + chosenCard.ImageLocation;
            display.Source = new BitmapImage(new Uri(address));
        }

        public void SetCardButtonsEnabled(int player, bool value)
        {
            var buttons = _allButtons[player];

            for (int i = 0; i < buttons.Length; i++)
            {
                // https://stackoverflow.com/questions/25406878/wpf-disabled-buttons-background
                buttons[i].IsHitTestVisible = value;
            }
        }

        /// <summary>
        /// This will just grab the back of cards. Removing the chosen card is just fine it's never used, 
        /// I left it in as an artifact mostly to make the code in use slightly more readable and interchangable visually for me
        /// </summary>
        /// <param name="chosenCard">does nothing but needed for passing in Can remove or leave</param>
        /// <param name="display">Place where the image of the back of a card is displayed</param>
        private void ShowBackOfCard(Card chosenCard, Image display)
        {
            //playground idea if intrested a person could easily make it so that this changes dynamically upon user choice the back of the card
            //could use getAddressesIfSomethingGoesWrong for this
            string address = _addresses + "cards/" + "cardBack.jpg";
            display.Source = new BitmapImage(new Uri(address));
        }

        /// <summary>
        /// This will show the proper hand of cards in the correct order
        /// </summary>
        /// <param name="handOfCards">This will show the hand of the cards from the player</param>
        /// <param name="displayOfHand">This is the image array for display</param>
        /// <param name="buttonArray">this is the array of buttons to hide or not in order that the hand shrinks visually while played</param>
        private void ShowHand(List<Card> handOfCards, Image[] displayOfHand, Button[] buttonArray)
        {
            int numberCardsShown = handOfCards.Count;
            for (int i = 0; i < 13; i++)
            {
                if (i < numberCardsShown)
                {
                    buttonArray[i].Visibility = Visibility.Visible;
                }
                else
                {
                    buttonArray[i].Visibility = Visibility.Hidden;
                }
            }

            int index = 0;
            bool aCardWasShown = false;
            foreach (Card specificCard in handOfCards)
            {

                if (Table.CardsPlayed[Table.LeadPlayerIndex] == null || Table.CardsPlayed[Table.LeadPlayerIndex].Suit == specificCard.Suit)
                {
                    ShowCardAvailable(specificCard, displayOfHand[index]);
                    aCardWasShown = true;
                }
                else
                {
                    ShowCard(specificCard, displayOfHand[index]);
                }
                index++;
            }
            //ugly but basically we have to test everything and see if it's in the array of cards and we simply say everything is available if the suit isn't available
            if (!aCardWasShown)
            {
                index = 0;
                foreach (Card specificCard in handOfCards)
                {
                    ShowCardAvailable(specificCard, displayOfHand[index]);
                    index++;
                }
            }
        }

        /// <summary>
        /// This will show the backs of the cards instead of actual cards
        /// </summary>
        /// <param name="handOfCards">Does nothing but is left for easy interchangability with ShowHand() jusut change name and you see this</param>
        /// <param name="displayOfHand">place to display the backs of cards</param>
        /// <param name="buttonArray">controls the size of the hand visually</param>
        private void ShowBacksOfCards(List<Card> handOfCards, Image[] displayOfHand, Button[] buttonArray)
        {
            int numberCardsShown = handOfCards.Count;
            for (int i = 0; i < 13; i++)
            {
                if (i < numberCardsShown)
                {
                    buttonArray[i].Visibility = Visibility.Visible;
                }
                else
                {
                    buttonArray[i].Visibility = Visibility.Hidden;
                }
            }

            int index = 0;
            foreach (Card specificCard in handOfCards)
            {
                ShowBackOfCard(specificCard, displayOfHand[index]);
                index++;
            }
        }

        /// Used to update the GUI with a sleep
        /// </summary>
        public void RefreshWindow()
        {
            // for updating gui with a sleep
            // https://stackoverflow.com/questions/37787388/how-to-force-a-ui-update-during-a-lengthy-task-on-the-ui-thread
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(delegate (object parameter)
            {
                frame.Continue = false;
                return null;
            }), null);

            Dispatcher.PushFrame(frame);
            
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                                          new Action(delegate { }));

            Thread.Sleep(500);
        }

        /// <summary>
        /// Displays player and dummy cards face up, and the rest face down
        /// </summary>
        public void DisplayAllCards()
        {
            InvalidateVisual();

            ShowHand(Table.Players[0].Hand.Cards, _northHandNames, _northButtons);
            if (Game.debugging)
            {
                ShowHand(Table.Players[1].Hand.Cards, _eastHandNames, _eastButtons);
                ShowHand(Table.Players[2].Hand.Cards, _southHandNames, _southButtons);
                ShowHand(Table.Players[3].Hand.Cards, _westHandNames, _westButtons);
            }
            else if (!Game.Instance.IsBidding) {
                if (Game.Instance.Contract.Player % 2 == 0)
                {
                    ShowHand(Table.Players[2].Hand.Cards, _southHandNames, _southButtons);
                    ShowBacksOfCards(Table.Players[1].Hand.Cards, _eastHandNames, _eastButtons);
                    ShowBacksOfCards(Table.Players[3].Hand.Cards, _westHandNames, _westButtons);
                }
                else
                {
                    ShowBacksOfCards(Table.Players[2].Hand.Cards, _southHandNames, _southButtons);
                    if (Game.Instance.Contract.Player == 1)
                    {
                        ShowHand(Table.Players[3].Hand.Cards, _westHandNames, _westButtons);
                        ShowBacksOfCards(Table.Players[1].Hand.Cards, _eastHandNames, _eastButtons);
                    }
                    else
                    {
                        ShowHand(Table.Players[1].Hand.Cards, _eastHandNames, _eastButtons);
                        ShowBacksOfCards(Table.Players[3].Hand.Cards, _westHandNames, _westButtons);
                    }
                }
            }
            else
            {
                ShowBacksOfCards(Table.Players[1].Hand.Cards, _eastHandNames, _eastButtons);
                ShowBacksOfCards(Table.Players[2].Hand.Cards, _southHandNames, _southButtons);
                ShowBacksOfCards(Table.Players[3].Hand.Cards, _westHandNames, _westButtons);
            }

            //updating cards in the middle
            if (Table.CardsPlayed[0] != null)
            { ShowCard(Table.CardsPlayed[0], northCardPlayed); }
            else
            { ShowBackOfCard(Table.CardsPlayed[0], northCardPlayed); }

            if (Table.CardsPlayed[1] != null)
            { ShowCard(Table.CardsPlayed[1], eastCardPlayed); }
            else
            { ShowBackOfCard(Table.CardsPlayed[1], eastCardPlayed); }

            if (Table.CardsPlayed[2] != null)
            { ShowCard(Table.CardsPlayed[2], southCardPlayed); }
            else
            { ShowBackOfCard(Table.CardsPlayed[2], southCardPlayed); }

            if (Table.CardsPlayed[3] != null)
            { ShowCard(Table.CardsPlayed[3], westCardPlayed); }
            else
            { ShowBackOfCard(Table.CardsPlayed[3], westCardPlayed); }

            //these ensure player had extra info
            ShowTricksPlayed();
            RefreshWindow();
            ShowCurrentPlayerLabel();
        }

        /// <summary>
        /// This builds an array of images for easy access and needs to run in order for everything to be accessed by the display functions
        /// </summary>
        private void BuildArraysForDisplay()
        {
            _northHandNames = new Image[13];
            _northHandNames[0] = northCard1Img;
            _northHandNames[1] = northCard2Img;
            _northHandNames[2] = northCard3Img;
            _northHandNames[3] = northCard4Img;
            _northHandNames[4] = northCard5Img;
            _northHandNames[5] = northCard6Img;
            _northHandNames[6] = northCard7Img;
            _northHandNames[7] = northCard8Img;
            _northHandNames[8] = northCard9Img;
            _northHandNames[9] = northCard10Img;
            _northHandNames[10] = northCard11Img;
            _northHandNames[11] = northCard12Img;
            _northHandNames[12] = northCard13Img;

            _southHandNames = new Image[13];
            _southHandNames[0] = southCard1Img;
            _southHandNames[1] = southCard2Img;
            _southHandNames[2] = southCard3Img;
            _southHandNames[3] = southCard4Img;
            _southHandNames[4] = southCard5Img;
            _southHandNames[5] = southCard6Img;
            _southHandNames[6] = southCard7Img;
            _southHandNames[7] = southCard8Img;
            _southHandNames[8] = southCard9Img;
            _southHandNames[9] = southCard10Img;
            _southHandNames[10] = southCard11Img;
            _southHandNames[11] = southCard12Img;
            _southHandNames[12] = southCard13Img;

            _eastHandNames = new Image[13];
            _eastHandNames[0] = eastCard1Img;
            _eastHandNames[1] = eastCard2Img;
            _eastHandNames[2] = eastCard3Img;
            _eastHandNames[3] = eastCard4Img;
            _eastHandNames[4] = eastCard5Img;
            _eastHandNames[5] = eastCard6Img;
            _eastHandNames[6] = eastCard7Img;
            _eastHandNames[7] = eastCard8Img;
            _eastHandNames[8] = eastCard9Img;
            _eastHandNames[9] = eastCard10Img;
            _eastHandNames[10] = eastCard11Img;
            _eastHandNames[11] = eastCard12Img;
            _eastHandNames[12] = eastCard13Img;

            _westHandNames = new Image[13];
            _westHandNames[0] = westCard1Img;
            _westHandNames[1] = westCard2Img;
            _westHandNames[2] = westCard3Img;
            _westHandNames[3] = westCard4Img;
            _westHandNames[4] = westCard5Img;
            _westHandNames[5] = westCard6Img;
            _westHandNames[6] = westCard7Img;
            _westHandNames[7] = westCard8Img;
            _westHandNames[8] = westCard9Img;
            _westHandNames[9] = westCard10Img;
            _westHandNames[10] = westCard11Img;
            _westHandNames[11] = westCard12Img;
            _westHandNames[12] = westCard13Img;

            _northTricksDisplay = new Image[13];
            _northTricksDisplay[0] = imgShowTricksN1;
            _northTricksDisplay[1] = imgShowTricksN2;
            _northTricksDisplay[2] = imgShowTricksN3;
            _northTricksDisplay[3] = imgShowTricksN4;
            _northTricksDisplay[4] = imgShowTricksN5;
            _northTricksDisplay[5] = imgShowTricksN6;
            _northTricksDisplay[6] = imgShowTricksN7;
            _northTricksDisplay[7] = imgShowTricksN8;
            _northTricksDisplay[8] = imgShowTricksN9;
            _northTricksDisplay[9] = imgShowTricksN10;
            _northTricksDisplay[10] = imgShowTricksN11;
            _northTricksDisplay[11] = imgShowTricksN12;
            _northTricksDisplay[12] = imgShowTricksN13;

            _southTricksDisplay = new Image[13];
            _southTricksDisplay[0] = imgShowTricksS1;
            _southTricksDisplay[1] = imgShowTricksS2;
            _southTricksDisplay[2] = imgShowTricksS3;
            _southTricksDisplay[3] = imgShowTricksS4;
            _southTricksDisplay[4] = imgShowTricksS5;
            _southTricksDisplay[5] = imgShowTricksS6;
            _southTricksDisplay[6] = imgShowTricksS7;
            _southTricksDisplay[7] = imgShowTricksS8;
            _southTricksDisplay[8] = imgShowTricksS9;
            _southTricksDisplay[9] = imgShowTricksS10;
            _southTricksDisplay[10] = imgShowTricksS11;
            _southTricksDisplay[11] = imgShowTricksS12;
            _southTricksDisplay[12] = imgShowTricksS13;


            _eastTricksDisplay = new Image[13];
            _eastTricksDisplay[0] = imgShowTricksE1;
            _eastTricksDisplay[1] = imgShowTricksE2;
            _eastTricksDisplay[2] = imgShowTricksE3;
            _eastTricksDisplay[3] = imgShowTricksE4;
            _eastTricksDisplay[4] = imgShowTricksE5;
            _eastTricksDisplay[5] = imgShowTricksE6;
            _eastTricksDisplay[6] = imgShowTricksE7;
            _eastTricksDisplay[7] = imgShowTricksE8;
            _eastTricksDisplay[8] = imgShowTricksE9;
            _eastTricksDisplay[9] = imgShowTricksE10;
            _eastTricksDisplay[10] = imgShowTricksE11;
            _eastTricksDisplay[11] = imgShowTricksE12;
            _eastTricksDisplay[12] = imgShowTricksE13;

            _westTricksDisplay = new Image[13];
            _westTricksDisplay[0] = imgShowTricksW1;
            _westTricksDisplay[1] = imgShowTricksW2;
            _westTricksDisplay[2] = imgShowTricksW3;
            _westTricksDisplay[3] = imgShowTricksW4;
            _westTricksDisplay[4] = imgShowTricksW5;
            _westTricksDisplay[5] = imgShowTricksW6;
            _westTricksDisplay[6] = imgShowTricksW7;
            _westTricksDisplay[7] = imgShowTricksW8;
            _westTricksDisplay[8] = imgShowTricksW9;
            _westTricksDisplay[9] = imgShowTricksW10;
            _westTricksDisplay[10] = imgShowTricksW11;
            _westTricksDisplay[11] = imgShowTricksW12;
            _westTricksDisplay[12] = imgShowTricksW13;
        }

        /// <summary>
        /// This is used to make the buttons into an array for easy use
        /// </summary>
        private void BuildArraysOfButtons()
        {
            _northButtons = new Button[13];
            _northButtons[0] = northCard1;
            _northButtons[1] = northCard2;
            _northButtons[2] = northCard3;
            _northButtons[3] = northCard4;
            _northButtons[4] = northCard5;
            _northButtons[5] = northCard6;
            _northButtons[6] = northCard7;
            _northButtons[7] = northCard8;
            _northButtons[8] = northCard9;
            _northButtons[9] = northCard10;
            _northButtons[10] = northCard11;
            _northButtons[11] = northCard12;
            _northButtons[12] = northCard13;

            _southButtons = new Button[13];
            _southButtons[0] = southCard1;
            _southButtons[1] = southCard2;
            _southButtons[2] = southCard3;
            _southButtons[3] = southCard4;
            _southButtons[4] = southCard5;
            _southButtons[5] = southCard6;
            _southButtons[6] = southCard7;
            _southButtons[7] = southCard8;
            _southButtons[8] = southCard9;
            _southButtons[9] = southCard10;
            _southButtons[10] = southCard11;
            _southButtons[11] = southCard12;
            _southButtons[12] = southCard13;

            _eastButtons = new Button[13];
            _eastButtons[0] = eastCard1;
            _eastButtons[1] = eastCard2;
            _eastButtons[2] = eastCard3;
            _eastButtons[3] = eastCard4;
            _eastButtons[4] = eastCard5;
            _eastButtons[5] = eastCard6;
            _eastButtons[6] = eastCard7;
            _eastButtons[7] = eastCard8;
            _eastButtons[8] = eastCard9;
            _eastButtons[9] = eastCard10;
            _eastButtons[10] = eastCard11;
            _eastButtons[11] = eastCard12;
            _eastButtons[12] = eastCard13;

            _westButtons = new Button[13];
            _westButtons[0] = westCard1;
            _westButtons[1] = westCard2;
            _westButtons[2] = westCard3;
            _westButtons[3] = westCard4;
            _westButtons[4] = westCard5;
            _westButtons[5] = westCard6;
            _westButtons[6] = westCard7;
            _westButtons[7] = westCard8;
            _westButtons[8] = westCard9;
            _westButtons[9] = westCard10;
            _westButtons[10] = westCard11;
            _westButtons[11] = westCard12;
            _westButtons[12] = westCard13;


            _allButtons = new Button[][] { _northButtons, _eastButtons, _southButtons, _westButtons };
        }

        public class CustomMessageBox
        {
            public CustomMessageBox(string text)
            {
                Window w = new Window();
                w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                w.Topmost = true;
                w.ResizeMode = ResizeMode.NoResize;
                w.SizeToContent = SizeToContent.WidthAndHeight;

                DockPanel panel = new DockPanel();
                TextBox tx = new TextBox();
                tx.IsReadOnly = true;
                panel.Children.Add(tx);
                tx.Text = text;
                w.Content = panel;
                w.Show();
            }
        }

        // Toolbar click events
        private void Help_Click(object sender, RoutedEventArgs e)
        {
            new CustomMessageBox("How to play bridge: \n  https://bicyclecards.com/how-to-play/bridge/  ");
        }

        /// <summary>
        /// this ensures that clicking on the exit will actually close the entire game as opposed to only closing the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            ///how-do-i-exit-a-wpf-application-programmatically
            Application.Current.Shutdown();
        }

        /// <summary>
        /// This will make it so clicking on a button will play the given card using the arrays built and the lists of player cards
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Card_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            int playerIndex = b.Name.Contains("north") ? 0 : b.Name.Contains("south") ? 2 : -1;
            int cardIndex = -1;
            try {
                cardIndex = int.Parse(System.Text.RegularExpressions.Regex.Match(b.Name, @"\d+").Value) - 1;
            } catch {
                if (Game.debugging) MessageBox.Show("MainWindow.Card_Click() Exception");
                return;
            }
            // Dummy and player
            if (playerIndex >= 0 && cardIndex >= 0 && _allButtons[playerIndex][cardIndex] == b && Table.CurrentPlayerIndex == playerIndex && Game.Instance.IsPlayersTurn())
            {
                Table.Players[playerIndex].ClickedCard = Table.Players[playerIndex].Hand.Cards[cardIndex];
                Table.CurrentPlayer.PickCard();
            }
        }

        /// <summary>
        /// This will open the coreboard window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Scoreboard_Click(object sender, RoutedEventArgs e)
        {
            Instance.ScoreboardWindow.Show();
        }

        private void Log_Click(object sender, RoutedEventArgs e)
        {
            ConsoleWindow.Show();
        }

        // close/load events
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Bid.Close();
            Application.Current.Shutdown();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            new Game();
        }

        /// <summary>
        /// Updates GUI showing number of tricks won for each player
        /// </summary>
        public void ShowTricksPlayed()
        {
            string address = _addresses + "cards/" + "cardBack.jpg";

            int length = Table.Players[0].TricksWonInHand;
            lblNorth.Content = length;
            foreach (Image trick in _northTricksDisplay)
            {
                if (length >= 1)
                {
                    trick.Source = new BitmapImage(new Uri(address));
                    trick.Visibility = Visibility.Visible;
                }
                else
                {
                    trick.Visibility = Visibility.Hidden;
                }
                length -= 1;
            }

            length = Table.Players[2].TricksWonInHand;
            lblSouth.Content = length;
            foreach (Image trick in _southTricksDisplay)
            {
                if (length >= 1)
                {
                    trick.Source = new BitmapImage(new Uri(address));
                    trick.Visibility = Visibility.Visible;
                }
                else
                {
                    trick.Visibility = Visibility.Hidden;
                }
                length -= 1;
            }

            length = Table.Players[1].TricksWonInHand;
            lblEast.Content = length;
            foreach (Image trick in _eastTricksDisplay)
            {
                if (length >= 1)
                {
                    trick.Source = new BitmapImage(new Uri(address));
                    trick.Visibility = Visibility.Visible;
                }
                else
                {
                    trick.Visibility = Visibility.Hidden;
                }
                length -= 1;
            }

            length = Table.Players[3].TricksWonInHand;
            lblWest.Content = length;
            foreach (Image trick in _westTricksDisplay)
            {
                if (length >= 1)
                {
                    trick.Source = new BitmapImage(new Uri(address));
                    trick.Visibility = Visibility.Visible;
                }
                else
                {
                    trick.Visibility = Visibility.Hidden;
                }
                length -= 1;
            }
        }

        /// <summary>
        /// This will highlight the current player's label(above their cards)
        /// </summary>
        private void ShowCurrentPlayerLabel()
        {
            int index = Table.CurrentPlayerIndex;
            if (index == 0)
            {
                lblNorthPlayer.Background = Brushes.Chartreuse;
                lblEastPlayer.Background = Brushes.White;
                lblSouthPlayer.Background = Brushes.White;
                lblWestPlayer.Background = Brushes.White;
            }
            else if (index == 1)
            {
                lblNorthPlayer.Background = Brushes.White;
                lblEastPlayer.Background = Brushes.Chartreuse;
                lblSouthPlayer.Background = Brushes.White;
                lblWestPlayer.Background = Brushes.White;
            }
            else if (index == 2)
            {
                lblNorthPlayer.Background = Brushes.White;
                lblEastPlayer.Background = Brushes.White;
                lblSouthPlayer.Background = Brushes.Chartreuse;
                lblWestPlayer.Background = Brushes.White;
            }
            else
            {
                lblNorthPlayer.Background = Brushes.White;
                lblEastPlayer.Background = Brushes.White;
                lblSouthPlayer.Background = Brushes.White;
                lblWestPlayer.Background = Brushes.Chartreuse;
            }
        }
    }
}
