using System;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ISU_Bridge;
using Microsoft.Win32;
using System.Diagnostics;

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

        public static MainWindow instance;

        string addresses;
        Image[] northHandNames;
        Image[] southHandNames;
        Image[] eastHandNames;
        Image[] westHandNames;
        Image[] NorthTricksDisplay;
        Image[] SouthTricksDisplay;
        Image[] EastTricksDisplay;
        Image[] WestTricksDisplay;
        Button[] northButtons;
        Button[] southButtons;
        Button[] eastButtons;
        Button[] westButtons;
        Button[][] allButtons;

        public TiledBiddingWindow bid { get; private set; }
        public ScoreboardWindow scoreboardWindow { get; private set; }
        public BridgeConsole consoleWindow { get; private set; }

        public MainWindow()
        {
                instance = this;


            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //Card1.Visibility = Visibility.Collapsed;



            //This should give me the way to load a file dynamically
            //http://csharphelper.com/blog/2015/07/load-an-image-at-runtime-in-wpf-and-c/

            //card1Img.Source= new BitmapImage(new Uri("/5hearts.png"));
            //card1Img.Source = "5Hearts.png";
            //pack:application:,,,/5hearts.png
            //MessageBox.Show(card2Img.Source.ToString());





            getAddresses();

            scoreboardWindow = new ScoreboardWindow();
            bid = new TiledBiddingWindow();
            // https://stackoverflow.com/questions/2546566/how-to-make-a-wpf-window-be-on-top-of-all-other-windows-of-my-app-not-system-wi
            bid.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            bid.Topmost = true;
            bid.Hide();

            consoleWindow = new BridgeConsole();
            consoleWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            //showCard(Jacks, card7Img);
            //showHand(testing, playerHandNames);


            buildArraysForDisplay();
            buildArraysOfButtons();


            ISU_Bridge.Table.initialize();

            displayAllCardsDebug();
        }

        /// <summary>
        /// This is a hail mary solution. It allow you to navigate to a card with a windows search window.
        /// It works fine but it is clunky so best to fix getAddresses first, select a card in the Cards folder and with navigation and it will retain this and run jusut fine
        /// </summary>
        private void getAddressesIfSomethingGoesWrong()
        {
            //I see potential for issues if the current working directory is not always starting in the program in the same place
            //by this token I must use just BridgeApp, which might be problematic with zip files where the zip defaults to match the outermost function
            //also there is a bridgeapp subfolder for this reason this will be a recalibration button if it breaks
            OpenFileDialog ofdPicture = new OpenFileDialog();
            ofdPicture.Filter = "Image files|*.bmp;*.jpg;*.gif;*.png;*.tif|All files|*.*";
            ofdPicture.FilterIndex = 1;
            string getAddress = "";
            if (ofdPicture.ShowDialog() == true)
            {
                getAddress = ofdPicture.FileName;

                //remember the double backslash for a single backslash
                int position = getAddress.IndexOf("BridgeApp\\cards\\");
                position += 16;
                getAddress = getAddress.Remove(position);
                addresses = getAddress;
                //Uri temp = new Uri("5hearts", UriKind.Relative);
            }

        }

        /// <summary>
        /// This will find the current directory so abosolute directions are usable.
        /// Unless you really know you're way around WPF, and explicitely WPF, do not try to use relative URLs they do not work
        /// </summary>
        private void getAddresses()
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
            addresses = getAddress;
            //Uri temp = new Uri("5hearts", UriKind.Relative);
        }


        //https://stackoverflow.com/questions/397117/wpf-image-dynamically-changing-image-source-during-runtime
        //imgNameDispaly is refering to the image in the xaml file
        /// <summary>
        /// This will display a card to a given image object
        /// </summary>
        /// <param name="chosenCard">Card to display</param>
        /// <param name="display">Image to display in</param>
        private void showCard(Card chosenCard, Image display)
        {
            //This is a little complicated and worth noting. 
            //WPF is tempermental and cannot use relative locations attempting to do so will only bring missery.
            //instead always use abosolute directions made with getAddresses
            string address = addresses + "cards/" + chosenCard.ImageLocation;
            display.Source = new BitmapImage(new Uri(address));
        }
        /// <summary>
        /// This will display the correct card but a modified version that's yellow to show which cards to play
        /// </summary>
        /// <param name="chosenCard">Card to display</param>
        /// <param name="display">Image to display in</param>
        private void showCardAvailable(Card chosenCard, Image display)
        {
            string address = addresses + "cards/" + "available" + chosenCard.ImageLocation;
            display.Source = new BitmapImage(new Uri(address));
        }



        public void SetCardButtonsEnabled(int player, bool value)
        {
            var buttons = allButtons[player];

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
        private void showBackOfCard(Card chosenCard, Image display)
        {
            //playground idea if intrested a person could easily make it so that this changes dynamically upon user choice the back of the card
            //could use getAddressesIfSomethingGoesWrong for this
            string address = addresses + "cards/" + "cardBack.jpg";
            display.Source = new BitmapImage(new Uri(address));
        }

        /// <summary>
        /// This will show the proper hand of cards in the correct order
        /// </summary>
        /// <param name="handOfCards">This will show the hand of the cards from the player</param>
        /// <param name="displayOfHand">This is the image array for display</param>
        /// <param name="buttonArray">this is the array of buttons to hide or not in order that the hand shrinks visually while played</param>
        private void showHand(List<Card> handOfCards, Image[] displayOfHand, Button[] buttonArray)
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

                if (ISU_Bridge.Table.cardsPlayed[ISU_Bridge.Table.leadPlayerIndex] == null || ISU_Bridge.Table.cardsPlayed[ISU_Bridge.Table.leadPlayerIndex].Suit == specificCard.Suit)
                {
                    showCardAvailable(specificCard, displayOfHand[index]);
                    aCardWasShown = true;
                }
                else
                {
                    showCard(specificCard, displayOfHand[index]);
                }
                index++;
            }
            //ugly but basically we have to test everything and see if it's in the array of cards and we simply say everything is available if the suit isn't available
            if (!aCardWasShown)
            {
                index = 0;
                foreach (Card specificCard in handOfCards)
                {
                    showCardAvailable(specificCard, displayOfHand[index]);
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
        private void showBacksOfCards(List<Card> handOfCards, Image[] displayOfHand, Button[] buttonArray)
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
                showBackOfCard(specificCard, displayOfHand[index]);
                index++;
            }
        }

        /// <summary>
        /// Displays all cards face up usefull for debug
        /// </summary>
        public void displayAllCardsDebug()
        {
            this.InvalidateVisual();
            showHand(ISU_Bridge.Table.players[0].hand.Cards, northHandNames, northButtons);
            showHand(ISU_Bridge.Table.players[1].hand.Cards, eastHandNames, eastButtons);
            showHand(ISU_Bridge.Table.players[2].hand.Cards, southHandNames, southButtons);
            showHand(ISU_Bridge.Table.players[3].hand.Cards, westHandNames, westButtons);
            if (ISU_Bridge.Table.cardsPlayed[0] != null)
            { showCard(ISU_Bridge.Table.cardsPlayed[0], northCardPlayed); }
            else
            { showBackOfCard(ISU_Bridge.Table.cardsPlayed[0], northCardPlayed); }
            if (ISU_Bridge.Table.cardsPlayed[1] != null)
            { showCard(ISU_Bridge.Table.cardsPlayed[1], eastCardPlayed); }
            else
            { showBackOfCard(ISU_Bridge.Table.cardsPlayed[1], eastCardPlayed); }
            if (ISU_Bridge.Table.cardsPlayed[2] != null)
            { showCard(ISU_Bridge.Table.cardsPlayed[2], southCardPlayed); }
            else
            { showBackOfCard(ISU_Bridge.Table.cardsPlayed[2], southCardPlayed); }
            if (ISU_Bridge.Table.cardsPlayed[3] != null)
            { showCard(ISU_Bridge.Table.cardsPlayed[3], westCardPlayed); }
            else
            { showBackOfCard(ISU_Bridge.Table.cardsPlayed[3], westCardPlayed); }


            showTricksPlayed();

            RefreshWindow();
        }

        /// <summary>
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
            //EDIT:
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                                          new Action(delegate { }));

            Thread.Sleep(500);
        }

        /// <summary>
        /// Displays player cards face up, and the rest face down. for playing actual games
        /// </summary>
        public void displayAllCards()
        {

            this.InvalidateVisual();

            //show hand just makes the appropriate hand appear
            showHand(ISU_Bridge.Table.players[0].hand.Cards, northHandNames, northButtons);
            if (Game.instance.isBidding == false)
            {
                //making sure the different cases of players winning bids will make the right players hands appear
                if (Game.instance.contract.player == 1)
                    showHand(ISU_Bridge.Table.players[3].hand.Cards, westHandNames, westButtons);
                else
                    showBacksOfCards(ISU_Bridge.Table.players[3].hand.Cards, westHandNames, westButtons);

                if (Game.instance.contract.player == 2 || Game.instance.contract.player == 0)
                    showHand(ISU_Bridge.Table.players[2].hand.Cards, southHandNames, southButtons);
                else
                    showBacksOfCards(ISU_Bridge.Table.players[2].hand.Cards, southHandNames, southButtons);

                if (Game.instance.contract.player == 3)
                    showHand(ISU_Bridge.Table.players[1].hand.Cards, eastHandNames, eastButtons);
                else
                    showBacksOfCards(ISU_Bridge.Table.players[1].hand.Cards, eastHandNames, eastButtons);
            }
            else
            {
                showBacksOfCards(ISU_Bridge.Table.players[1].hand.Cards, eastHandNames, eastButtons);
                showBacksOfCards(ISU_Bridge.Table.players[2].hand.Cards, southHandNames, southButtons);
                showBacksOfCards(ISU_Bridge.Table.players[3].hand.Cards, westHandNames, westButtons);
            }

            //updating cards in the middle
            if (ISU_Bridge.Table.cardsPlayed[0] != null)
            { showCard(ISU_Bridge.Table.cardsPlayed[0], northCardPlayed); }
            else
            { showBackOfCard(ISU_Bridge.Table.cardsPlayed[0], northCardPlayed); }

            if (ISU_Bridge.Table.cardsPlayed[1] != null)
            { showCard(ISU_Bridge.Table.cardsPlayed[1], eastCardPlayed); }
            else
            { showBackOfCard(ISU_Bridge.Table.cardsPlayed[1], eastCardPlayed); }

            if (ISU_Bridge.Table.cardsPlayed[2] != null)
            { showCard(ISU_Bridge.Table.cardsPlayed[2], southCardPlayed); }
            else
            { showBackOfCard(ISU_Bridge.Table.cardsPlayed[2], southCardPlayed); }

            if (ISU_Bridge.Table.cardsPlayed[3] != null)
            { showCard(ISU_Bridge.Table.cardsPlayed[3], westCardPlayed); }
            else
            { showBackOfCard(ISU_Bridge.Table.cardsPlayed[3], westCardPlayed); }

            //these ensure player had extra info
            showTricksPlayed();
            RefreshWindow();
            ShowCurrentPlayerLabel();
        }

        /// <summary>
        /// This builds an array of images for easy access and needs to run in order for everything to be accessed by the display functions
        /// </summary>
        void buildArraysForDisplay()
        {
            northHandNames = new Image[13];
            northHandNames[0] = northCard1Img;
            northHandNames[1] = northCard2Img;
            northHandNames[2] = northCard3Img;
            northHandNames[3] = northCard4Img;
            northHandNames[4] = northCard5Img;
            northHandNames[5] = northCard6Img;
            northHandNames[6] = northCard7Img;
            northHandNames[7] = northCard8Img;
            northHandNames[8] = northCard9Img;
            northHandNames[9] = northCard10Img;
            northHandNames[10] = northCard11Img;
            northHandNames[11] = northCard12Img;
            northHandNames[12] = northCard13Img;

            southHandNames = new Image[13];
            southHandNames[0] = southCard1Img;
            southHandNames[1] = southCard2Img;
            southHandNames[2] = southCard3Img;
            southHandNames[3] = southCard4Img;
            southHandNames[4] = southCard5Img;
            southHandNames[5] = southCard6Img;
            southHandNames[6] = southCard7Img;
            southHandNames[7] = southCard8Img;
            southHandNames[8] = southCard9Img;
            southHandNames[9] = southCard10Img;
            southHandNames[10] = southCard11Img;
            southHandNames[11] = southCard12Img;
            southHandNames[12] = southCard13Img;

            eastHandNames = new Image[13];
            eastHandNames[0] = eastCard1Img;
            eastHandNames[1] = eastCard2Img;
            eastHandNames[2] = eastCard3Img;
            eastHandNames[3] = eastCard4Img;
            eastHandNames[4] = eastCard5Img;
            eastHandNames[5] = eastCard6Img;
            eastHandNames[6] = eastCard7Img;
            eastHandNames[7] = eastCard8Img;
            eastHandNames[8] = eastCard9Img;
            eastHandNames[9] = eastCard10Img;
            eastHandNames[10] = eastCard11Img;
            eastHandNames[11] = eastCard12Img;
            eastHandNames[12] = eastCard13Img;

            westHandNames = new Image[13];
            westHandNames[0] = westCard1Img;
            westHandNames[1] = westCard2Img;
            westHandNames[2] = westCard3Img;
            westHandNames[3] = westCard4Img;
            westHandNames[4] = westCard5Img;
            westHandNames[5] = westCard6Img;
            westHandNames[6] = westCard7Img;
            westHandNames[7] = westCard8Img;
            westHandNames[8] = westCard9Img;
            westHandNames[9] = westCard10Img;
            westHandNames[10] = westCard11Img;
            westHandNames[11] = westCard12Img;
            westHandNames[12] = westCard13Img;

            NorthTricksDisplay = new Image[13];
            NorthTricksDisplay[0] = imgShowTricksN1;
            NorthTricksDisplay[1] = imgShowTricksN2;
            NorthTricksDisplay[2] = imgShowTricksN3;
            NorthTricksDisplay[3] = imgShowTricksN4;
            NorthTricksDisplay[4] = imgShowTricksN5;
            NorthTricksDisplay[5] = imgShowTricksN6;
            NorthTricksDisplay[6] = imgShowTricksN7;
            NorthTricksDisplay[7] = imgShowTricksN8;
            NorthTricksDisplay[8] = imgShowTricksN9;
            NorthTricksDisplay[9] = imgShowTricksN10;
            NorthTricksDisplay[10] = imgShowTricksN11;
            NorthTricksDisplay[11] = imgShowTricksN12;
            NorthTricksDisplay[12] = imgShowTricksN13;

            SouthTricksDisplay = new Image[13];
            SouthTricksDisplay[0] = imgShowTricksS1;
            SouthTricksDisplay[1] = imgShowTricksS2;
            SouthTricksDisplay[2] = imgShowTricksS3;
            SouthTricksDisplay[3] = imgShowTricksS4;
            SouthTricksDisplay[4] = imgShowTricksS5;
            SouthTricksDisplay[5] = imgShowTricksS6;
            SouthTricksDisplay[6] = imgShowTricksS7;
            SouthTricksDisplay[7] = imgShowTricksS8;
            SouthTricksDisplay[8] = imgShowTricksS9;
            SouthTricksDisplay[9] = imgShowTricksS10;
            SouthTricksDisplay[10] = imgShowTricksS11;
            SouthTricksDisplay[11] = imgShowTricksS12;
            SouthTricksDisplay[12] = imgShowTricksS13;


            EastTricksDisplay = new Image[13];
            EastTricksDisplay[0] = imgShowTricksE1;
            EastTricksDisplay[1] = imgShowTricksE2;
            EastTricksDisplay[2] = imgShowTricksE3;
            EastTricksDisplay[3] = imgShowTricksE4;
            EastTricksDisplay[4] = imgShowTricksE5;
            EastTricksDisplay[5] = imgShowTricksE6;
            EastTricksDisplay[6] = imgShowTricksE7;
            EastTricksDisplay[7] = imgShowTricksE8;
            EastTricksDisplay[8] = imgShowTricksE9;
            EastTricksDisplay[9] = imgShowTricksE10;
            EastTricksDisplay[10] = imgShowTricksE11;
            EastTricksDisplay[11] = imgShowTricksE12;
            EastTricksDisplay[12] = imgShowTricksE13;

            WestTricksDisplay = new Image[13];
            WestTricksDisplay[0] = imgShowTricksW1;
            WestTricksDisplay[1] = imgShowTricksW2;
            WestTricksDisplay[2] = imgShowTricksW3;
            WestTricksDisplay[3] = imgShowTricksW4;
            WestTricksDisplay[4] = imgShowTricksW5;
            WestTricksDisplay[5] = imgShowTricksW6;
            WestTricksDisplay[6] = imgShowTricksW7;
            WestTricksDisplay[7] = imgShowTricksW8;
            WestTricksDisplay[8] = imgShowTricksW9;
            WestTricksDisplay[9] = imgShowTricksW10;
            WestTricksDisplay[10] = imgShowTricksW11;
            WestTricksDisplay[11] = imgShowTricksW12;
            WestTricksDisplay[12] = imgShowTricksW13;
        }

        /// <summary>
        /// This is used to make the buttons into an array for easy use
        /// </summary>
        void buildArraysOfButtons()
        {
            northButtons = new Button[13];
            northButtons[0] = northCard1;
            northButtons[1] = northCard2;
            northButtons[2] = northCard3;
            northButtons[3] = northCard4;
            northButtons[4] = northCard5;
            northButtons[5] = northCard6;
            northButtons[6] = northCard7;
            northButtons[7] = northCard8;
            northButtons[8] = northCard9;
            northButtons[9] = northCard10;
            northButtons[10] = northCard11;
            northButtons[11] = northCard12;
            northButtons[12] = northCard13;

            southButtons = new Button[13];
            southButtons[0] = southCard1;
            southButtons[1] = southCard2;
            southButtons[2] = southCard3;
            southButtons[3] = southCard4;
            southButtons[4] = southCard5;
            southButtons[5] = southCard6;
            southButtons[6] = southCard7;
            southButtons[7] = southCard8;
            southButtons[8] = southCard9;
            southButtons[9] = southCard10;
            southButtons[10] = southCard11;
            southButtons[11] = southCard12;
            southButtons[12] = southCard13;

            eastButtons = new Button[13];
            eastButtons[0] = eastCard1;
            eastButtons[1] = eastCard2;
            eastButtons[2] = eastCard3;
            eastButtons[3] = eastCard4;
            eastButtons[4] = eastCard5;
            eastButtons[5] = eastCard6;
            eastButtons[6] = eastCard7;
            eastButtons[7] = eastCard8;
            eastButtons[8] = eastCard9;
            eastButtons[9] = eastCard10;
            eastButtons[10] = eastCard11;
            eastButtons[11] = eastCard12;
            eastButtons[12] = eastCard13;

            westButtons = new Button[13];
            westButtons[0] = westCard1;
            westButtons[1] = westCard2;
            westButtons[2] = westCard3;
            westButtons[3] = westCard4;
            westButtons[4] = westCard5;
            westButtons[5] = westCard6;
            westButtons[6] = westCard7;
            westButtons[7] = westCard8;
            westButtons[8] = westCard9;
            westButtons[9] = westCard10;
            westButtons[10] = westCard11;
            westButtons[11] = westCard12;
            westButtons[12] = westCard13;


            allButtons = new Button[][] { northButtons, eastButtons, southButtons, westButtons };
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
            //MessageBox.Show("How to play bridge: \nhttps://bicyclecards.com/how-to-play/bridge/");

            new CustomMessageBox("How to play bridge: \n  https://bicyclecards.com/how-to-play/bridge/  ");
        }
        /// <summary>
        /// this ensures that clicking on the exit will actually close the entire game as opposed to only closing the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("Exit clicked");
            ///how-do-i-exit-a-wpf-application-programmatically
            System.Windows.Application.Current.Shutdown();
        }


        private void Deal_Click(object sender, RoutedEventArgs e)
        {
            new Game();

            showHand(ISU_Bridge.Table.players[0].hand.Cards, northHandNames, northButtons);
            showHand(ISU_Bridge.Table.players[1].hand.Cards, eastHandNames, eastButtons);
            showHand(ISU_Bridge.Table.players[2].hand.Cards, southHandNames, southButtons);
            showHand(ISU_Bridge.Table.players[3].hand.Cards, westHandNames, westButtons);
        }

        /// <summary>
        /// This will make it so clicking on a button will play the given card using the arrays built and the lists of player cards
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Card_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            for (int player = 0; player < 4; player++)
            {
                for (int card = 0; card < 13; card++)
                {
                    if (allButtons[player][card] == b)
                    {
                        //updated to fix bug, current player was originally of type int but now it's player, if bugs occur it's because currentplayer doesn't line up with currentplayerIndex
                        if (ISU_Bridge.Table.currentPlayerIndex == player && (player == 0 || player == 2))
                        {
                            Card CardForReturn = ISU_Bridge.Table.players[player].hand.Cards[card];
                            ISU_Bridge.Table.players[player].ClickedCard = CardForReturn;

                            if (player == 2 && ISU_Bridge.Table.players[player].hand.Is_dummy)
                            {
                                var dummy = (AI_Player)ISU_Bridge.Table.currentPlayer;
                                dummy.PlayDummy();
                            }
                            else
                                ISU_Bridge.Table.currentPlayer.PickCard();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// This will open the coreboard window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Scoreboard_Click(object sender, RoutedEventArgs e)
        {
            scoreboardWindow.Show();
        }

        private void Log_Click(object sender, RoutedEventArgs e)
        {
            consoleWindow.Show();
        }

        // close/load events
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bid.Close();
            System.Windows.Application.Current.Shutdown();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            new Game();
        }

        /// <summary>
        /// Updates GUI showing number of tricks won for each player
        /// </summary>
        public void showTricksPlayed()
        {

            string address = addresses + "cards/" + "cardBack.jpg";

            Random rand = new Random();

            int length = ISU_Bridge.Table.players[0].tricksWonInHand;
            //lblNorthSouth.Content = ISU_Bridge.Table.northSouthScore;
            lblNorth.Content = length;
            foreach (Image trick in NorthTricksDisplay)
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

            length = ISU_Bridge.Table.players[2].tricksWonInHand;
            lblSouth.Content = length;
            foreach (Image trick in SouthTricksDisplay)
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

            length = ISU_Bridge.Table.players[1].tricksWonInHand;
            //lblEastWest.Content = ISU_Bridge.Table.eastWestScore;
            lblEast.Content = length;
            foreach (Image trick in EastTricksDisplay)
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

            length = ISU_Bridge.Table.players[3].tricksWonInHand;
            //lblEastWest.Content = ISU_Bridge.Table.eastWestScore;
            lblWest.Content = length;
            foreach (Image trick in WestTricksDisplay)
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
            int index = ISU_Bridge.Table.currentPlayerIndex;
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
