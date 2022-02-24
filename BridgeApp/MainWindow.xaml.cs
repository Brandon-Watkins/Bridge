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
using System.Timers;

namespace BridgeApp
{
    //Keep a bool of whoes player is
    //see if you can layer buttons 
    // there is a property called topmost could set it to active whenever you hover over a button
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, SimpleObserver
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
        public Button[] _northButtons;
        private Button[] _southButtons;
        private Button[] _eastButtons;
        private Button[] _westButtons;
        private Button[][] _allButtons;
        private bool readyToEraseBids = true;


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

            SetUpPlayerIllumination();

            Table.Instance.Subscribe(this);

            Table.Initialize();

            for (int i = 0; i < 4; i++)
            {
                Table.Players[i].Hand.Subscribe(this);
            }
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
            display.Dispatcher.Invoke(() => display.Source = new BitmapImage(new Uri(address)));
        }

        /// <summary>
        /// This will display the correct card but a modified version that's yellow to show which cards to play
        /// </summary>
        /// <param name="chosenCard">Card to display</param>
        /// <param name="display">Image to display in</param>
        private void ShowCardAvailable(Card chosenCard, Image display)
        {
            string address = _addresses + "cards/" + "available" + chosenCard.ImageLocation;
            display.Dispatcher.Invoke(() => display.Source = new BitmapImage(new Uri(address)));
        }

        public void SetCardButtonsEnabled(int player, bool value)
        {
            this.Dispatcher.Invoke(() =>
            {
                var buttons = _allButtons[player];

                for (int i = 0; i < buttons.Length; i++)
                {
                    // https://stackoverflow.com/questions/25406878/wpf-disabled-buttons-background
                    buttons[i].IsHitTestVisible = value;
                }
            });
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
            this.Dispatcher.Invoke(() => display.Source = new BitmapImage(new Uri(address)));
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
            buttonArray[0].Dispatcher.Invoke(() =>
            {
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
            });

            displayOfHand[0].Dispatcher.Invoke(() =>
            {
                int index = 0;
                bool aCardWasShown = false;
                foreach (Card specificCard in handOfCards)
                {

                    if (Table.CardsPlayed[Table.LeadPlayerIndex] == NullCard.Instance || Table.CardsPlayed[Table.LeadPlayerIndex].Suit == specificCard.Suit)
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
            });
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
            buttonArray[0].Dispatcher.Invoke(() =>
            {
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
            });

            int index = 0;
            displayOfHand[0].Dispatcher.Invoke(() =>
            {
                foreach (Card specificCard in handOfCards)
                {
                    ShowBackOfCard(specificCard, displayOfHand[index]);
                    index++;
                }
            });
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
            this.Dispatcher.Invoke(() => InvalidateVisual());

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
            if (Table.CardsPlayed[0] != NullCard.Instance)
            { ShowCard(Table.CardsPlayed[0], northCardPlayed); }
            else
            { ShowBackOfCard(Table.CardsPlayed[0], northCardPlayed); }

            if (Table.CardsPlayed[1] != NullCard.Instance)
            { ShowCard(Table.CardsPlayed[1], eastCardPlayed); }
            else
            { ShowBackOfCard(Table.CardsPlayed[1], eastCardPlayed); }

            if (Table.CardsPlayed[2] != NullCard.Instance)
            { ShowCard(Table.CardsPlayed[2], southCardPlayed); }
            else
            { ShowBackOfCard(Table.CardsPlayed[2], southCardPlayed); }

            if (Table.CardsPlayed[3] != NullCard.Instance)
            { ShowCard(Table.CardsPlayed[3], westCardPlayed); }
            else
            { ShowBackOfCard(Table.CardsPlayed[3], westCardPlayed); }

            ShowTricksPlayed();
            RefreshWindow();
            UpdateCurrentPlayerIllumination();
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
            public CustomMessageBox(string text, string title = "")
            {
                Window w = new Window();
                w.Owner = Instance.mainWindow;
                w.Title = title;
                w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                w.Topmost = true;
                w.ResizeMode = ResizeMode.NoResize;
                w.SizeToContent = SizeToContent.WidthAndHeight;

                DockPanel panel = new DockPanel();
                TextBox tx = new TextBox();
                tx.IsReadOnly = true;
                tx.Width = 1000;
                tx.Height = 600;
                tx.Padding = new Thickness(20);
                tx.TextWrapping = TextWrapping.Wrap;
                tx.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                panel.Children.Add(tx);
                tx.Text = text;
                w.Content = panel;
                w.Show();
            }
        }

        /// <summary>
        /// Toolbar -> Help click event handler. Displays a window containing the rules for Bridge.
        /// Brandon Watkins
        /// </summary>
        /// <param name="sender">(object) Event source</param>
        /// <param name="e">(RoutedEventArgs) Event data</param>
        private void Help_Click(object sender, RoutedEventArgs e)
        {
            new CustomMessageBox(@"HOW TO PLAY BRIDGE
(Courtesy of https://bicyclecards.com/how-to-play/bridge/)


GAME SETUP/RANK OF SUITS
Spades (High), hearts, diamonds, clubs.
Rank of Cards: A (High), K, Q, J, 10, 9, 8, 7, 6, 5, 4, 3, 2


THE DEAL
The dealer distributes 13 cards to each player, one card at a time, face down, beginning with the player on their left.


OBJECT OF THE GAME
Each partnership attempts to score points by making its bid, or by defeating the opposing partnership's bid. At the end of play, the side with the most points wins.


THE BIDDING
Calls - Once the cards are dealt, each player picks up their hand and, beginning with the dealer, makes a call (pass, bid, double or redouble).


PASSING
When a player does not wish to bid, double, or redouble, they say, ""Pass."" If all four players pass in the first round, the deal is ""passed out, "" and the next dealer in turn deals a new hand.


BIDDING A SUIT
Bid a number of tricks greater than six that the bidder expects to win, and a suit which will become the trump suit.
Ex. = ""One Spade"" is a bid to win seven tricks(6 + 1) with spades as trumps.
A bid may be made in ""No-trump"", meaning that there will be no trump suit.The lowest possible bid is one, and the highest possible bid is seven.

Each bid must name a greater number of odd tricks than the last bid, or an equal number but in a higher denomination.No - trump is the highest denomination, outranking spades.
Ex. = ""Two No-trump"" will overcall a bid of ""Two Hearts"", and a bid of ""Four Clubs"" is required to overcall a bid of ""Three No-trump"".


DOUBLING AND REDOUBLING
Any player may double the last preceding bid if it was made by an opponent.
Any player may redouble the last preceding bid if it was made by their side and doubled by an opponent.

A doubled or redoubled bid may be overcalled by any bid, which would have been sufficient to overcall the same contract undoubled.
Ex. = ""Two Spades"" is doubled and redoubled, it may still be overcalled by a bid of ""Two No-trump,"" a bid of ""Three Clubs,"" or by any other higher bid.


FINAL BID AND THE DECLARER
When a bid, double, or redouble is followed by three consecutive passes, the bidding is closed.The final bid in the auction becomes the contract.The player who, for their side, first bid the denomination named in the contract becomes the ""declarer."" If the contract names a trump suit, every card of that suit becomes a trump.The declarer's partner becomes the ""dummy,"" and the opposing players become the ""defenders.""


THE PLAY
Take a card and place it, face up, in the center of the table.Four cards so played, one from each hand in rotation, constitute a trick.The first card played to a trick is a lead. The leader to a trick may lead any card.The other three hands must follow suit if they can.If a player is unable to follow suit, they may play any card. For the first trick, the defender on the declarer's left makes the first lead (the opening lead).


FACING THE DUMMY HAND
As soon as the opening lead has been made, the dummy then spreads their hand face up, grouped in suits, with each suit vertically arranged so that the other three players can easily view all 13 cards.The suits may be placed in any order as long as the trump suit(if any) is placed to the declarer's left. There is no particular order for placing the suits down in a No-trump bid.


WINNING OF TRICKS
A trick containing a trump is won by the hand playing the highest trump. A trick not containing a trump is won by the hand playing the highest card of the suit led. The winner of each trick leads next.


DECLARER'S PLAY
The declarer plays their own cards and the dummy's cards, but each in proper turn, since the dummy does not take an active part in the play.


PLAYED CARD
The declarer plays a card from their own hand when they places it on the table or when it is named as an intended play. When the declarer touches a card in the dummy hand, it is considered played (except when he is merely arranging the dummies cards). Alternatively, the declarer may name a card in the dummy and such a card must be played.A defender plays a card when they expose it so that the other defender can see its face. A card once played may not be withdrawn, except to correct a revoke or other irregularity.


TAKING IN TRICKS WON
A completed trick is gathered and turned face down on the table. The declarer and one of the defenders should keep all tricks won in front of them, and the tricks should be arranged so that the quantity and the order of the tricks played are apparent.


HOW TO KEEP SCORE
When the last(13th) trick has been played, the tricks taken by the respective sides are counted, and the points earned are then entered to the credit of that side on the score sheet. Any player may keep score.If only one player keeps score, both sides are equally responsible to see that the score for each deal is correctly entered.

The score sheet is ruled with a vertical line making two columns that are titled They and We.The scorekeeper enters all scores made by his side in the We column and all scores made by the opponents in the They column.A little below the middle of the score sheet is a horizontal line.Scores designated as ""trick score"" are entered below the line; all other scores are ""premium scores"" and are written above the line.


TRICK SCORE
If the declarer fulfills their bid by winning as many or more odd - tricks as the contract called for, their side scores below the line for every odd-trick named in the contract.Thus, if the declarer wins eight tricks and the bid is Two Hearts, the score for making ""two"" in a bid of hearts would be credited, as per the Scoring Table.
 

 OVERTRICKS
 Odd - tricks won by the declarer in excess of the contract are called ""overtricks"" and are scored to the credit of their side as premium score.
 

 THE GAME
 When a side has scored 100 or more points below the line, it has won a ""game."" To show this, the scorekeeper draws a horizontal line across the score sheet, below the score that ended the game.This signifies that the next game will begin.A game may be made in more than one deal, such as by scoring 60 and later 40, or it may be scored by making a larger bid and earning 100 or more points in a single deal.Once the next game begins, if the opponents had a score below the line for making a bid, such as 70, this score does not carry over, and each side needs the full 100 points to win the next game.


VULNERABLE
A side that has won its first game becomes ""vulnerable,"" and that side's objective is to win a second game and thus earn a bonus for the ""rubber."" When a side scores its second game, the rubber is over, and the scores are totaled. The winning partnership is the side with the most points. A vulnerable side is exposed to increased penalties if it fails to fulfill a future bid, but receives increased premiums for certain other bids that are fulfilled.


HONORS
When there is a trump suit, the ace, king, queen, jack, and ten of trumps are ""honors."" If a player holds four of the five trump honors, that partnership scores 100 above the line; all five honors in one hand score 150.If the contract is in No - trump, a player holding all four aces scores 150 above the line for their side. Note that the points for honors are the same whether the side is not vulnerable or vulnerable, and that the defenders can also score for honors.


SLAM BONUSES
Other premium scores are awarded for bidding and making a ""small slam""(a bid at the six - level, such as Six Hearts) or a ""grand slam""(a contract at the seven - level, such as Seven Spades or Seven No - trump).


DOUBLED OR REDOUBLED CONTRACT
When the declarer makes a doubled contract, a premium bonus is scored.Making a redoubled contract scores an even bigger premium bonus - this is a recent change in scoring.Note that doubling and redoubling do not affect honor, slam, or rubber bonus points.


UNFINISHED RUBBER
If the players are unable to complete a full rubber and only one side has a game, that side scores a 300 bonus.If only one side has a part score, that side earns a 100 bonus.


BACK SCORE
After each rubber, each player's standing, plus (+) or minus (-), in even hundreds of points, is entered on a separate score called the ""back score."" An odd 50 points or more count 100, so if a player wins a rubber by 950 he is +10, if he wins it by 940 the player is +9.",
"How To Play Bridge");
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
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    cardIndex = int.Parse(System.Text.RegularExpressions.Regex.Match(b.Name, @"\d+").Value) - 1;
                }
                catch
                {
                    if (Game.debugging) MessageBox.Show("MainWindow.Card_Click() Exception");
                    return;
                }
                // Dummy and player
                if (playerIndex >= 0 && cardIndex >= 0 && _allButtons[playerIndex][cardIndex] == b && Table.CurrentPlayerIndex == playerIndex && Game.Instance.IsPlayersTurn())
                {
                    Table.Players[playerIndex].ClickedCard = Table.Players[playerIndex].Hand.Cards[cardIndex];
                    Table.CurrentPlayer.PickCard();
                }
            });
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
        /// Updates GUI showing number of tricks won for each player.
        /// </summary>
        public void ShowTricksPlayed()
        {

            string address = _addresses + "cards/" + "cardBack.jpg";

            int length = Table.Players[0].TricksWonInHand;

            // Only shows the tricks played when not bidding, and readyToEraseBids == true.
            this.Dispatcher.Invoke(() =>
            {
                if (!Game.Instance.IsBidding && readyToEraseBids)
                {
                    lblNorthTricks.Content = length;
                }

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

                if (!Game.Instance.IsBidding && readyToEraseBids)
                {
                    lblSouthTricks.Content = length;
                }

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

                if (!Game.Instance.IsBidding && readyToEraseBids)
                {
                    lblEastTricks.Content = length;
                }

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

                if (!Game.Instance.IsBidding && readyToEraseBids)
                {
                    lblWestTricks.Content = length;
                }

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
            });
        }

        /// <summary>
        /// Sets up the window for bidding history, instead of tricks.
        /// Brandon Watkins
        /// </summary>
        public void StartBidding()
        {
            // Clear game content and resize to prep for bidding info
            this.Dispatcher.Invoke(() =>
            {
                lblNorth.Content = "";
                lblNorthTricks.Content = "";

                lblSouth.Content = "";
                lblSouthTricks.Content = "";

                lblEast.Content = "Bidding History";
                lblEastTricks.Content = "";
                lblEastTricks.Height = Convert.ToInt32(this.Height);

                lblWest.Content = "";
                lblWestTricks.Content = "";
                lblWestTricks.Height = Convert.ToInt32(this.Height);
                lblWestTricks.Width = 105;
            });

            // Enable user buttons immediately if user is dealer. 
            // Otherwise it takes almost a full second before the button is enabled, as it's set up
            SetCardButtonsEnabled(0, Table.CurrentPlayerIndex == 0 ? true : false);

            UpdateCurrentPlayerIllumination();

            // Wait a sec before finishing DelayedStartBidding, otherwise the first player is only briefly illuminated.
            new DelayedFunction(Table.CurrentPlayerIndex == 0 ? 0 : 500, DelayedStartBidding, this);
        }

        /// <summary>
        /// Finishes the StartBidding calls used to actually commence bidding
        /// Brandon Watkins
        /// </summary>
        /// <param name="src">(object) Event source</param>
        /// <param name="e">(ElapsedEventArgs) Event data</param>
        private void DelayedStartBidding(object src, ElapsedEventArgs e)
        {
            Bid.StartBidding();

            Table.CurrentPlayer.QueryBid(Game.Instance.Contract);
        }

        /// <summary>
        /// Sets up the window for gameplay.
        /// Use after bidding concludes.
        /// Brandon Watkins
        /// </summary>
        public void EndBidding()
        {
            // Makes the window wait to erase the bidding history.
            readyToEraseBids = false;
            this.Dispatcher.Invoke(() =>
            {
                // Resets the window to show tricks again, instead of bid history, after 2 seconds.
                new DelayedFunction(2000, RemoveBidContent, this);

                // update contract info text
                string contractText = "Contract: ";
                if (Game.Instance.Contract.Player % 2 == 0)
                {
                    contractText += "N/S\n";
                    // Dummy
                    Table.Players[2].Hand.IsDummy = true;
                    Table.Players[2].IsHuman = true;
                    // Resize South's hand to be same size as North's, because North is playing for both.
                    EnlargeSouthHand();
                }
                else
                {
                    contractText += "E/W\n";
                }
                contractText += Game.Instance.Contract.NumTricks + " " + Game.Instance.Contract.Suit.ToString();
                lblContract.Content = contractText;

                // Sets the current player to be the player after the winning bidder.
                Table.SetPlayer(Table.LeadPlayerIndex = (Game.Instance.Contract.Player + 1) % 4);
            });
        }

        /// <summary>
        /// Resets the window to show tricks again, instead of bid history.
        /// Sets readyToEraseBids to true, so tricks will be updated as usual.
        /// Brandon Watkins
        /// </summary>
        /// <param name="src">(object) Event source</param>
        /// <param name="e">(ElapsedEventArgs) Event data</param>
        private void RemoveBidContent(object src, ElapsedEventArgs e)
        {
            readyToEraseBids = true;
            this.Dispatcher.Invoke(() =>
            {
                lblNorth.Content = "North Tricks";
                lblNorthTricks.Content = "0";

                lblSouth.Content = "South Tricks";
                lblSouthTricks.Content = "0";

                lblEast.Content = "East Tricks";
                lblEastTricks.Content = "0";
                lblEastTricks.Height = 26;

                lblWest.Content = "West Tricks";
                lblWestTricks.Content = "0";
                lblWestTricks.Height = 26;
                lblWestTricks.Width = 56;
            });
        }

        /// <summary>
        /// Updates the window to display the current bidding history.
        /// Brandon Watkins
        /// </summary>
        public void UpdateBidContent()
        {
            this.Dispatcher.Invoke(() =>
            {
                // Verifying that the bidding history isn't already up to date.
                if (Game.Instance.Contract.BidHistory.Count < 1 || 
                    lblEastTricks.Content.ToString().Split(Environment.NewLine).Length > Game.Instance.Contract.BidHistory.Count) 
                    return;

                lblEastTricks.Content += Game.Instance.Contract.BidHistory[Game.Instance.Contract.BidHistory.Count - 1].player + Environment.NewLine;
                lblWestTricks.Content += Game.Instance.Contract.BidHistory[Game.Instance.Contract.BidHistory.Count - 1].bid + Environment.NewLine;
            });
        }

        /// <summary>
        /// Called by the Observables this is subscribed to.
        /// Brandon Watkins
        /// </summary>
        /// <param name="o">(SimpleObservable) Observable that called this method</param>
        public void OnNext(SimpleObservable o = null)
        {
            // Table notifies when tthe current player changes.
            // Thus, we need to update whose hand is glowing.
            if (o is Table) UpdateCurrentPlayerIllumination();
            // Hand notifies when a card has been played.
            // Thus, we need to resize tthe illumination behind the player's hand.
            else if (o is Hand)
            {
                int playerIndex = 
                    Table.Players[0].Hand == o ? 0 :
                    Table.Players[1].Hand == o ? 1 :
                    Table.Players[2].Hand == o ? 2 :
                    Table.Players[3].Hand == o ? 3 :
                    -1;
                if (playerIndex > -1) UpdatePlayerIlluminationSize(playerIndex);
            }
        }

        /// <summary>
        /// Sets the colors for the glow behind the players' hands.
        /// Brandon Watkins
        /// </summary>
        private void SetUpPlayerIllumination()
        {
            this.Dispatcher.Invoke(() =>
            {
                // Green
                RadialGradientBrush brush = GetPlayerIlluminationBrush(0);
                recNorthBox.Fill = brush;
                // Yellow-orange if South is AI-controlled, green if dummy.
                brush = GetPlayerIlluminationBrush(2);
                recSouthBox.Fill = brush;
                // Red
                brush = GetPlayerIlluminationBrush(1);
                recWestBox.Fill = brush;
                recEastBox.Fill = brush;
            });
        }

        /// <summary>
        /// Updates the visibility of each player's glow.
        /// If it's their turn, it's visible, else, invisible.
        /// Use whenever the current player changes.
        /// Brandon Watkins
        /// </summary>
        private void UpdateCurrentPlayerIllumination()
        {
            this.Dispatcher.Invoke(() =>
            {
                int index = Table.CurrentPlayerIndex;
                recNorthBox.Visibility = index == 0 ? Visibility.Visible : Visibility.Hidden;
                recEastBox.Visibility = index == 1 ? Visibility.Visible : Visibility.Hidden;
                recSouthBox.Visibility = index == 2 ? Visibility.Visible : Visibility.Hidden;
                recWestBox.Visibility = index == 3 ? Visibility.Visible : Visibility.Hidden;
            });
        }

        /// <summary>
        /// Resizes the given player's glow, based on how many cards are in their hand.
        /// If their hand is empty, Visibility is set to Hidden.
        /// Brandon Watkins
        /// </summary>
        /// <param name="playerIndex">(int) Index of player to update</param>
        private void UpdatePlayerIlluminationSize(int playerIndex)
        {
            this.Dispatcher.Invoke(() => {
                if (playerIndex == 0) recNorthBox.Width = Table.Players[0].Hand.Cards.Count == 0 ? 0 : 108 + (Table.Players[0].Hand.Cards.Count - 1) * 48;
                else if (playerIndex == 1) recEastBox.Height = Table.Players[1].Hand.Cards.Count == 0 ? 0 : 63 + (Table.Players[1].Hand.Cards.Count - 1) * 39;
                else if (playerIndex == 2) recSouthBox.Width = Table.Players[2].Hand.Cards.Count == 0 ? 0 : Table.Players[2].IsHuman ?
                    (108 + (Table.Players[2].Hand.Cards.Count - 1) * 48) :
                    (63 + (Table.Players[2].Hand.Cards.Count - 1) * 39);
                else recWestBox.Height = Table.Players[3].Hand.Cards.Count == 0 ? 0 : 63 + (Table.Players[3].Hand.Cards.Count - 1) * 39;
                
                if (recNorthBox.Height == 0) recNorthBox.Visibility = Visibility.Hidden;
                if (recEastBox.Height == 0) recEastBox.Visibility = Visibility.Hidden;
                if (recSouthBox.Height == 0) recSouthBox.Visibility = Visibility.Hidden;
                if (recWestBox.Height == 0) recWestBox.Visibility = Visibility.Hidden;
            });
        }

        /// <summary>
        /// Increases the size of the cards in South's hand, as well as the size of the illumination.
        /// Use when South is determined to be the dummy hand.
        /// Brandon Watkins
        /// </summary>
        private void EnlargeSouthHand()
        {
            this.Dispatcher.Invoke(() =>
            {
                // Increase Size of turn-indicator glow for South:
                recSouthBox.HorizontalAlignment = HorizontalAlignment.Right;
                recSouthBox.VerticalAlignment = VerticalAlignment.Top;
                recSouthBox.Margin = new Thickness(0, 20, 330, 0);
                recSouthBox.Height = 196;
                recSouthBox.Width = 684;

                // Increase size of the cards for south
                foreach (Button button in _southButtons)
                {
                    button.Height = 135;
                    button.Width = 108;
                    button.Margin = new Thickness(-30);
                }

                // Move their player label up a bit
                lblSouthPlayer.Margin = new Thickness(0, 15, 0, 0);

                // Update Color from yellow-orange to green:
                recSouthBox.Fill = GetPlayerIlluminationBrush(2);
            });
        }

        /// <summary>
        /// Resets the size and visibility of everyone's glow.
        /// Resets South's color and card size back to defaults.
        /// Use at the end of a hand, before bidding commences.
        /// Brandon Watkins
        /// </summary>
        public void ResetPlayerIllumination()
        {
            this.Dispatcher.Invoke(() =>
            {
                // Original Size for South:
                recSouthBox.HorizontalAlignment = HorizontalAlignment.Right;
                recSouthBox.VerticalAlignment = VerticalAlignment.Top;
                recSouthBox.Margin = new Thickness(0, 41, 404, 0);
                recSouthBox.Height = 134;
                recSouthBox.Width = 532;

                foreach (Button button in _southButtons)
                {
                    button.Height = 80;
                    button.Width = 63;
                    button.Margin = new Thickness(-12);
                }

                lblSouthPlayer.Margin = new Thickness(0, 30, 0, 0);

                // Original Color for South:
                recSouthBox.Fill = GetPlayerIlluminationBrush(2);

                // Original Width for North:
                recNorthBox.Width = 684;// 800;

                // Original Height for E/W:
                recWestBox.Height = 530;
                recEastBox.Height = 530;

                // Hide everything
                recNorthBox.Visibility = Visibility.Hidden;
                recSouthBox.Visibility = Visibility.Hidden;
                recWestBox.Visibility = Visibility.Hidden;
                recEastBox.Visibility = Visibility.Hidden;
            });
        }

        /// <summary>
        /// Gets an appropriately colored brush for the fill of the player's illumination.
        /// Brandon Watkins
        /// </summary>
        /// <param name="playerIndex">(int) Index of player whose brush you want</param>
        /// <returns>(RadialGradientBrush) Brush of appropriate color for the player</returns>
        private RadialGradientBrush GetPlayerIlluminationBrush(int playerIndex)
        {
            // Default color - East/West (orange-red)
            byte r = 255;
            byte g = 100;
            byte b = 65;
            if (playerIndex % 2 == 0)
            {
                // Player-controlled color (green-blue)
                if (playerIndex == 0 || (!(Table.Players is null) && Table.Players[2].IsHuman))
                {
                    r = 0;
                    g = 255;
                    b = 100;
                }
                // South AI color (yellow-orange)
                else
                {
                    r = 255;
                    g = 230;
                    b = 50;
                }
            }

            RadialGradientBrush brush = new RadialGradientBrush();
            brush.GradientOrigin = new Point(0.5, 0.5);
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(255, r, g, b), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(255, r, g, b), 0.75));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, r, g, b), 1.0));

            return brush;
        }
    }
}
