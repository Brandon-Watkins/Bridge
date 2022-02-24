using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using ISU_Bridge;

namespace BridgeApp
{
    /// <summary>
    /// Interaction logic for TiledBiddingWindow.xaml
    /// </summary>
    public partial class TiledBiddingWindow : Window
    {

        private Button[][] tiles;
        private const int MAX_ROW = 7;
        private const int MAX_COL = 5;
        private int hoveredNum;
        private Card.Face hoveredSuit;

        // the below code removes the close, minimize, maximize buttons
        // https://stackoverflow.com/questions/743906/how-to-hide-close-button-in-wpf-window
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public event Action<Card.Face, int> OnBidPlaced;
        public event Action OnPass;

        public TiledBiddingWindow()
        {
            InitializeComponent();

            PopulateTiles();
        }

        public void PopulateTiles()
        {
            this.Dispatcher.Invoke(() =>
            {
                tiles = new Button[7][];
                for (int row = 0; row < 7; row++)
                {
                    tiles[row] = new Button[5];
                }

                foreach (var child in BiddingTable.Children)
                {
                    if (child.GetType() == typeof(Button))
                    {
                        Button b = (Button)child;

                        // get row
                        int.TryParse(b.Name.ToString()[1].ToString(), out int row);

                        // get col
                        char c = b.Name.ToString()[2];
                        int col = CharToInt(c);

                        // Resetting the name of the button, to erase old bid indicators
                        string name = row + " " + (col < 4 ? Card.Suits[col].ToString() : "No Trump");
                        if (row == 1 && col != 4) name = name.Substring(0, name.Length - 1);
                        b.Content = name;

                        tiles[row - 1][col] = b;
                    }
                }
            });
        }

        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;

            // get num
            int.TryParse(b.Name.ToString()[1].ToString(), out int num);

            // get suit
            char c = b.Name.ToString()[2];

            Card.Face f = Card.Face.NoTrump;
            if (c.Equals('C'))
                f = Card.Face.Clubs;
            else if (c.Equals('D'))
                f = Card.Face.Diamonds;
            else if (c.Equals('H'))
                f = Card.Face.Hearts;
            else if (c.Equals('S'))
                f = Card.Face.Spades;
            else
                f = Card.Face.NoTrump;

            Bid(f, num);
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Help?");
        }

        private void Bid_Click(object sender, RoutedEventArgs e)
        {
            Bid(hoveredSuit, hoveredNum);
        }

        private void Pass_Click(object sender, RoutedEventArgs e)
        {
            Pass();
        }

        public void Bid(Card.Face f, int num)
        {
            // roundabout way of calling Game.PlaceBid
            OnBidPlaced.Invoke(f, num);
            MainWindow.Instance.UpdateBidContent();
        }
        public void Pass()
        {
            // roundabout way of calling Game.Pass
            OnPass.Invoke();
            MainWindow.Instance.UpdateBidContent();
        }

        public void RefreshWindow()
        {
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

        public void SetPlayerGUI(Contract c)
        {
            int row, col = 0;
            if (!c.HasOneBid)
            {
                row = 0;
                col = -1;
            }
            else
            {
                row = c.NumTricks - 1;
                col = CharToInt(c.Suit.ToString()[0]);
            }

            this.Dispatcher.Invoke(() =>
            {
                // psuedo-enable
                for (int j = col + 1; j < MAX_COL; j++)
                {
                    tiles[row][j].IsHitTestVisible = true;
                }
                for (int i = row + 1; i < MAX_ROW; i++)
                {
                    for (int j = 0; j < MAX_COL; j++)
                    {
                        tiles[i][j].IsHitTestVisible = true;
                    }
                }

                // Got rid of the bid button. Currently just made the pass button span both grid columns.
                // Hoping to replace the bid button with a double/redouble button, in the future.
                //btnBid.IsEnabled = false;
                btnPass.IsEnabled = true;
            });
        }

        /// <summary>
        /// Updates the GUI to show the most recent Contract info.
        /// </summary>
        /// <param name="c">(Contract) Contract being bid on</param>
        /// <param name="passed">(bool) true if the update was caused by a player passing</param>
        public void Update(Contract c, bool passed = false)
        {
            DisableAll();
            UpdatePassText(c);
            UpdateHighestBid(c);
            UpdateColors(c);
            UpdateWaiting(passed);
            MainWindow.Instance.UpdateBidContent();
            RefreshWindow();
        }

        /// <summary>
        /// Updates the colors for the bidding tiles, to indicate placed bids.
        /// Brandon Watkins
        /// </summary>
        /// <param name="c">(Contract) Contract being bid on</param>
        private void UpdateColors(Contract c)
        {
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (c.HasOneBid)
                    {
                        int row = c.NumTricks - 1;
                        int col = CharToInt(c.Suit.ToString()[0]);

                        // show different color for previous bids
                        // Past bids will now stay colored to easily see the bid history
                        // Bids placed by E/W are red, N/S are green, other illegal bids are dark grey.
                        for (int i = 0; i < row; i++)
                        {
                            for (int j = 0; j < MAX_COL; j++)
                            {
                                string label = tiles[i][j].Content.ToString();
                                if (!(label.Contains("North") || label.Contains("South") || label.Contains("East") || label.Contains("West")))
                                    tiles[i][j].Background = new SolidColorBrush(Color.FromRgb(150, 150, 150));
                            }
                        }
                        // This threw an exception when I bid 6H -> E/W bid 7H -> I bid 7S
                        for (int j = 0; j < col; j++)
                        {
                            // Only re-colors buttons that weren't previously placed bids.
                            string label = tiles[row][j].Content.ToString();
                            if (!(label.Contains("North") || label.Contains("South") || label.Contains("East") || label.Contains("West")))
                                tiles[row][j].Background = new SolidColorBrush(Color.FromRgb(150, 150, 150));
                        }

                        // Latest bid. Green if bid is from N/S, red if E/W.
                        tiles[row][col].Background = Table.Players[Game.Instance.Contract.Player].Index % 2 == 0 ?
                            new SolidColorBrush(Color.FromRgb(150, 180, 150)) :
                            new SolidColorBrush(Color.FromRgb(180, 150, 150));

                        // Labels the buttons with the player that placed the bid
                        // (so you can easily see when your partner placed a bid, but was immediately outbid)
                        tiles[row][col].Content = Table.Players[Game.Instance.Contract.Player].Name.Split(" ")[0];
                    }
                    else
                    {
                        // reset colors...
                        for (int i = 0; i < MAX_ROW; i++)
                        {
                            for (int j = 0; j < MAX_COL; j++)
                            {
                                tiles[i][j].Background = new SolidColorBrush(Color.FromRgb(230, 230, 230));
                            }
                        }
                    }
                } catch (Exception e)
                {
                    if (Game.debugging) BridgeConsole.Log("UpdateColors() threw an exception: " + Environment.NewLine + e.Message);
                }

                if (Table.CurrentPlayer.IsHuman)
                    txtWaiting.Background = new SolidColorBrush(Color.FromRgb(200, 255, 200));
                else
                    txtWaiting.Background = new SolidColorBrush(Color.FromRgb(255, 200, 200));
            });
        }

        private void DisableAll()
        {
            this.Dispatcher.Invoke(() =>
            {
                // prevent the player from clicking buttons while they're not supposed to
                for (int i = 0; i < MAX_ROW; i++)
                    for (int j = 0; j < MAX_COL; j++)
                        // https://stackoverflow.com/questions/25406878/wpf-disabled-buttons-background
                        tiles[i][j].IsHitTestVisible = false;

                // Got rid of the bid button. Currently just made the pass button span both grid columns.
                // Hoping to replace the bid button with a double/redouble button, in the future.
                //btnBid.IsEnabled = false;
                btnPass.IsEnabled = Table.CurrentPlayerIndex == 0 ? true : false;
            });
        }

        private void UpdateHighestBid(Contract c)
        {
            this.Dispatcher.Invoke(() => txtHighestBid.Text = c.NumTricks == 0 ? "" : 
                "Highest Bid: " + c.NumTricks + " " + c.Suit.ToString() + " - " + Table.Players[c.Player].Name);
        }

        /// <summary>
        /// Updates the "waiting for bid from player X" text.
        /// Brandon Watkins
        /// </summary>
        /// <param name="passed">(bool) true if the last player passed</param>
        private void UpdateWaiting(bool passed)
        {
            string name = Table.Players[passed ? (Table.CurrentPlayerIndex + 1) % 4 : Table.CurrentPlayerIndex].Name;
            this.Dispatcher.Invoke(() => txtWaiting.Text = "Waiting for bid from " + name);
        }

        /// <summary>
        /// Resets the bid tiles to original names and colors, and displays the bidding window.
        /// Brandon Watkins
        /// </summary>
        public void StartBidding()
        {
            // Repopulate tiles to erase old bid indicators.
            PopulateTiles();

            Update(Game.Instance.Contract);
            this.Dispatcher.Invoke(() => Show());
        }

        /// <summary>
        /// Disables the user's pass button, displays bidding results in green if N/S won, else red.
        /// Brandon Watkins
        /// </summary>
        public void EndBidding()
        {
            // Indicate bidding finished, colored green if N/S, else red.
            this.Dispatcher.Invoke(() =>
            {
                if (Table.CurrentPlayerIndex == 0) btnPass.IsEnabled = false;

                txtWaiting.Text = "Bidding Finished";
                BiddingUtil.Background = new SolidColorBrush(Game.Instance.Contract.Player % 2 == 0 ? Color.FromRgb(200, 255, 200) : Color.FromRgb(255, 200, 200));
                txtWaiting.Background = Brushes.Transparent;
            });
            RefreshWindow();

            foreach (Player p in Table.Players)
            {
                p.Hand.Sort(Game.Instance.Contract.Suit);
            }

            new DelayedFunction(1500, DelayedEndBidding, this);
        }

        private void DelayedEndBidding(Object src, System.Timers.ElapsedEventArgs e)
        {
            MainWindow.Instance.EndBidding();
            this.Dispatcher.Invoke(() => {
                BiddingUtil.Background = new SolidColorBrush(Color.FromArgb(0, 200, 255, 200));
                Hide();
            });
            Table.Game.Play();
        }

        public void UpdatePassText(Contract c)
        {
            int max = c.HasOneBid ? 3 : 4;
            this.Dispatcher.Invoke(() => btnPass.Content = "Pass (" + c.NumPassed + "/" + max + ")");
        }

        private int CharToInt(char c)
        {
            if (c.Equals('C')) return 0;
            else if (c.Equals('D')) return 1;
            else if (c.Equals('H')) return 2;
            else if (c.Equals('S')) return 3;
            else return 4;
        }

        private void BiddingWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            
        }

        private void BiddingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // https://stackoverflow.com/questions/743906/how-to-hide-close-button-in-wpf-window
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
    }
}
