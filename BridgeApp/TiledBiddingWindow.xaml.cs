using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
// using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using ISU_Bridge;

namespace BridgeApp
{
    /// <summary>
    /// Interaction logic for TiledBiddingWindow.xaml
    /// </summary>
    public partial class TiledBiddingWindow : Window
    {
        public event Action<Card.face, int> onBidPlaced;
        public event Action onPass;



        private Button[][] tiles;
        const int MAX_ROW = 7;
        const int MAX_COL = 5;

        private int hoveredNum;
        private Card.face hoveredSuit;

        public TiledBiddingWindow()
        {
            InitializeComponent();

            PopulateTiles();
        }

        private void PopulateTiles()
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

            Card.face f = Card.face.NoTrump;
            if (c.Equals('C'))
                f = Card.face.Clubs;
            else if (c.Equals('D'))
                f = Card.face.Diamonds;
            else if (c.Equals('H'))
                f = Card.face.Hearts;
            else if (c.Equals('S'))
                f = Card.face.Spades;
            else //if (c.Equals('N'))
                f = Card.face.NoTrump;

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

        public void Bid(Card.face f, int num)
        {
            // roundabout way of calling Game.PlaceBid
            onBidPlaced.Invoke(f, num);
        }
        public void Pass()
        {
            // roundabout way of calling Game.Pass
            onPass.Invoke();
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
            //EDIT:
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                                          new Action(delegate { }));

            Thread.Sleep(500);
        }

        public void SetPlayerGUI(Contract c)
        {
            int row, col = 0;
            if (!c.hasOneBid)
            {
                row = 0;
                col = -1;
            }
            else
            {
                row = c.numTricks - 1;
                col = CharToInt(c.suit.ToString()[0]);
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
            if (c.hasOneBid)
            {
                int row = c.numTricks - 1;
                int col = CharToInt(c.suit.ToString()[0]);

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


            if (Table.currentPlayer.isHuman)
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
        private void HoverBid(int num, Card.face f)
        {
            btnBid.Content = "Confirm Bid: " + num + " " + f.ToString();
            hoveredNum = num;
            hoveredSuit = f;
            btnBid.IsEnabled = true;
        }
        private void UpdateHighestBid(Contract c)
        {
            txtHighestBid.Text = "Highest Bid: " + c.numTricks + " " + c.suit.ToString() + " " + Table.players[c.player].name;
        }
        private void UpdateWaiting()
        {
            string playerName = Table.currentPlayer.name;

            txtWaiting.Text = "Waiting for bid from " + playerName;
        }

        public void EndBidding()
        {
            MessageBox.Show("Bidding Finished");

            this.Hide();

            // play for south if north or south won bid
            if (Game.instance.contract.player % 2 == 0)
                Table.players[2].hand.Is_dummy = true;

            // update contract info text
            string contractText = "Contract: ";
            if (Game.instance.contract.player % 2 == 0)
                contractText += "N/S\n";
            else
                contractText += "E/W\n";
            contractText += Game.instance.contract.numTricks + " " + Game.instance.contract.suit.ToString();
            MainWindow.instance.lblContract.Content = contractText;


            Table.game.Play();
        }
        public void UpdatePassText(Contract c)
        {
            int max = c.hasOneBid ? 3 : 4;
            btnPass.Content = "Pass (" + c.numPassed + "/" + max + ")";
        }
        private Card.face IntToFace(int n)
        {
            if (n == 0)
                return Card.face.Clubs;
            else if (n == 1)
                return Card.face.Diamonds;
            else if (n == 2)
                return Card.face.Hearts;
            else if (n == 3)
                return Card.face.Spades;
            else //if (n == 4)
                return Card.face.NoTrump;
        }
        private int CharToInt(char c)
        {
            if (c.Equals('C'))
                return 0;
            else if (c.Equals('D'))
                return 1;
            else if (c.Equals('H'))
                return 2;
            else if (c.Equals('S'))
                return 3;
            else //if (c.Equals('N'))
                return 4;
        }

        private void BiddingWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // MessageBox.Show("No");
            // e.Cancel = true;
        }


        // the below code removes the close, minimize, maximize buttons
        // https://stackoverflow.com/questions/743906/how-to-hide-close-button-in-wpf-window
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private void BiddingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // https://stackoverflow.com/questions/743906/how-to-hide-close-button-in-wpf-window
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }


    }
}
