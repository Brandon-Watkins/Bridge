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

                    tiles[row - 1][col] = b;
                }
            }
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

            HoverBid(num, f);
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
        }
        public void Pass()
        {
            // roundabout way of calling Game.Pass
            OnPass.Invoke();
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

            btnBid.IsEnabled = false;
            btnPass.IsEnabled = true;
        }

        public void Update(Contract c)
        {
            DisableAll();
            UpdatePassText(c);
            UpdateHighestBid(c);
            UpdateColors(c);
            UpdateWaiting();
            RefreshWindow();
        }

        private void UpdateColors(Contract c)
        {
            if (c.HasOneBid)
            {
                int row = c.NumTricks - 1;
                int col = CharToInt(c.Suit.ToString()[0]);

                // show different color for previous bids
                for (int i = 0; i < row; i++)
                {
                    for (int j = 0; j < MAX_COL; j++)
                    {
                        tiles[i][j].Background = new SolidColorBrush(Color.FromRgb(160, 150, 150));
                    }
                }
                for (int j = 0; j < col; j++)
                {
                    tiles[row][j].Background = new SolidColorBrush(Color.FromRgb(160, 150, 150));
                }

                // current bid
                tiles[row][col].Background = new SolidColorBrush(Color.FromRgb(150, 160, 150));

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

            if (Table.CurrentPlayer.IsHuman)
                txtWaiting.Background = new SolidColorBrush(Color.FromRgb(200, 255, 200));
            else
                txtWaiting.Background = new SolidColorBrush(Color.FromRgb(255, 200, 200));
        }

        private void DisableAll()
        {
            // prevent the player from clicking buttons while they're not supposed to
            for (int i = 0; i < MAX_ROW; i++)
                for (int j = 0; j < MAX_COL; j++)
                    // https://stackoverflow.com/questions/25406878/wpf-disabled-buttons-background
                    tiles[i][j].IsHitTestVisible = false;

            btnBid.IsEnabled = false;
            btnPass.IsEnabled = false;
        }

        private void HoverBid(int num, Card.Face f)
        {
            btnBid.Content = "Confirm Bid: " + num + " " + f.ToString();
            hoveredNum = num;
            hoveredSuit = f;
            btnBid.IsEnabled = true;
        }

        private void UpdateHighestBid(Contract c)
        {
            txtHighestBid.Text = c.NumTricks == 0 ? "" : "Highest Bid: " + c.NumTricks + " " + c.Suit.ToString() + " " + Table.Players[c.Player].Name;
        }

        private void UpdateWaiting()
        {
            string playerName = Table.CurrentPlayer.Name;

            txtWaiting.Text = "Waiting for bid from " + playerName;
        }

        public void EndBidding()
        {
            MessageBox.Show("Bidding Finished");

            Hide();

            // update contract info text
            string contractText = "Contract: ";
            if (Game.Instance.Contract.Player % 2 == 0)
            {
                contractText += "N/S\n";
                // play for south if north or south won bid
                Table.Players[2].Hand.IsDummy = true;
                Table.Players[2].IsHuman = true;
            }
            else
            {
                contractText += "E/W\n";
                Table.Players[2].Hand.IsDummy = false;
                Table.Players[2].IsHuman = false;
            }
            contractText += Game.Instance.Contract.NumTricks + " " + Game.Instance.Contract.Suit.ToString();
            MainWindow.Instance.lblContract.Content = contractText;

            foreach (Player p in Table.Players)
            {
                p.Hand.Sort(Game.Instance.Contract.Suit);
            }

            Table.Game.Play();
        }

        public void UpdatePassText(Contract c)
        {
            int max = c.HasOneBid ? 3 : 4;
            btnPass.Content = "Pass (" + c.NumPassed + "/" + max + ")";
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
