using System.Collections.Generic;
using System.Diagnostics;
using BridgeApp;
using System.Windows;
using System.Threading;

namespace ISU_Bridge
{
    public class Game
    {

        public static Game Instance { get; private set; }
        public static bool debugging = false; // true: displays logger, and shows all card faces.

        public Game()
        {
            if (Instance != null)
            {
                // maybe events here not such a great idea?
                MainWindow.Instance.Bid.OnBidPlaced -= Instance.PlaceBid;
                MainWindow.Instance.Bid.OnPass -= Instance.Pass;
            }
            else
            {
                MainWindow.Instance.Bid.OnBidPlaced += PlaceBid;
                MainWindow.Instance.Bid.OnPass += Pass;
            }

            Instance = this;

            PrepHand();
        }

        public Contract Contract { get; private set; }
        public bool IsBidding { get; private set; }

        private void DealCards()
        {
            List<Hand> hands = new List<Hand>();
            foreach (Player p in Table.Players)
            {
                hands.Add(p.Hand);
            }
            Table.Deck = new Deck();
            Table.Deck.Initialize(hands);
        }

        #region Bidding
        private void StartBidding()
        {
            Contract = new Contract();
            IsBidding = true;

            // Reset dummy hand
            Table.Players[2].IsHuman = false;
            for (int i = 1; i < 4; i++) Table.Players[i].Hand.IsDummy = false;

            // Repopulate tiles to erase old bid indicators.
            MainWindow.Instance.Bid.PopulateTiles();

            MainWindow.Instance.Bid.Show();
            //MainWindow.Instance.Bid.Update(Contract);
            MainWindow.Instance.SetCardButtonsEnabled(0, false);

            BridgeConsole.Log("Bidding started...");
            Table.CurrentPlayerIndex = Table.DealerIndex;
            MainWindow.Instance.Bid.Update(Contract);
            Table.CurrentPlayer.QueryBid(Contract);
        }

        /// <summary>
        /// Called by a player to confirm a bid
        /// </summary>
        /// <param name="face"></param>
        /// <param name="num"></param>
        private void PlaceBid(Card.Face face, int num)
        {
            // update contract
            Contract.Bid(Table.CurrentPlayerIndex, face, num);

            BridgeConsole.Log($"{Table.CurrentPlayer} bid {num} {face}");

            Table.NextPlayer();
            MainWindow.Instance.Bid.Update(Contract);

            Table.CurrentPlayer.QueryBid(Contract);
        }

        /// <summary>
        /// Called by a player to confirm a pass in bidding
        /// </summary>
        private void Pass()
        {
            BridgeConsole.Log($"{Table.CurrentPlayer} passed");
            Contract.Pass();

            Table.NextPlayer();
            MainWindow.Instance.Bid.Update(Contract);

            // finished bidding? - move to contract
            if (!Contract.HasOneBid && Contract.NumPassed == 4)
            {
                PrepHand();
                Contract.Reset();
            }
            if (Contract.HasOneBid && Contract.NumPassed == 3)
            {
                IsBidding = false;
            }

            if (IsBidding)
            {
                Table.CurrentPlayer.QueryBid(Contract);
            }
            else
            {
                MainWindow.Instance.Bid.EndBidding();
            }
        }
        #endregion] Bidding


        /// <summary>
        /// Called to start the entire game. Won't run if the scoreboard has determined the game is over.
        /// </summary>
        public void Play()
        {
            if (!Table.Scoreboard.MatchOver)
            {
                BridgeConsole.Log("Started playing");
                Table.NextPlayer();
                PrepTrick(Table.CurrentPlayerIndex);
            }
            else
            {
                MainWindow.Instance.ScoreboardWindow.Show();
                MessageBoxResult result = MessageBox.Show("Game Over!" + ((Table.Scoreboard.T1total > Table.Scoreboard.T2total) ? 
                    " You Win!" : 
                    " You Lose...") + "\nPlay Again?", "Bridge", MessageBoxButton.YesNo);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        Table.Initialize();
                        MainWindow.Instance.ScoreboardWindow.Update(Table.Scoreboard);
                        PrepHand();
                        break;
                    case MessageBoxResult.No:
                        Application.Current.Shutdown();
                        break;
                }
            }
        }

        /// <summary>
        /// Calls the functions neccesary to begin a single hand of bridge
        /// </summary>
        private void PrepHand()
        {
            DealCards();
            for (int i = 0; i < 4; i++) { Table.Players[i].TricksWonInHand = 0; }
            StartBidding();
            MainWindow.Instance.DisplayAllCards();
        }

        /// <summary>
        /// Called after the previous trick is processed. updates variables neccessary to reset and prep for the next trick
        /// </summary>
        /// <param name="last_winner_index">player index of the winner of the previous trick</param>
        private void PrepTrick(int last_winner_index)
        {
            for (int i = 0; i < 4; i++) { Table.CardsPlayed[i] = null; }
            Table.LeadPlayerIndex = last_winner_index;
            Table.CurrentPlayerIndex = last_winner_index;

            // let player or dummy click cards if they won the contract
            MainWindow.Instance.SetCardButtonsEnabled(0, IsPlayersTurn() && !IsDummysTurn());
            // Dummy
            MainWindow.Instance.SetCardButtonsEnabled(2, IsDummysTurn());

            if (Table.CurrentPlayer.Hand.Cards.Count > 0)
            {
                MainWindow.Instance.DisplayAllCards();
                if (!IsPlayersTurn())
                {
                    Table.CurrentPlayer.PickCard();
                }
            }
            else
            {
                Table.NextDealer();
                Table.Scoreboard.HandOver();
                if (Table.Scoreboard.MatchOver)
                {
                    Play();
                }
                else
                {
                    PrepHand();
                }
            }

        }

        /// <summary>
        /// Determines the winner of a single trick and increments that player's trickswon variable
        /// </summary>
        private void ProcessTrick()
        {
            int high_scorer = -1;
            int best_score = 0;
            int count = 0;
            foreach (Card c in Table.CardsPlayed)
            {
                int card_score = 0;
                card_score += (c.Suit == Table.TrumpSuit) ? 1000 : 0;
                card_score += (c.Suit == Table.CardsPlayed[Table.LeadPlayerIndex].Suit) ? 100 : 0;
                card_score += c.Number;
                if (card_score > best_score) { best_score = card_score; high_scorer = count; }
                count++;
            }
            Table.Players[high_scorer].TricksWonInHand++;
            BridgeConsole.Log(Table.Players[high_scorer] + " won the trick.");

            // Ensures the game shows the last card played for half a second before clearing the cards.
            MainWindow.Instance.DisplayAllCards();
            Thread.Sleep(500);

            PrepTrick(high_scorer);
        }

        /// <summary>
        /// Called whenever a card is played. This is what ticks the game forward
        /// </summary>
        public void UpdateCardPlayed()
        {
            Table.NextPlayer();

            // update card clickability between cards played
            MainWindow.Instance.DisplayAllCards();

            if (Table.CurrentPlayerIndex == Table.LeadPlayerIndex) { ProcessTrick(); }
            else
            {
                // Dummy
                if (IsPlayersTurn())
                {
                    MainWindow.Instance.SetCardButtonsEnabled(Table.CurrentPlayerIndex, true);
                }
                else
                {
                    Table.CurrentPlayer.PickCard();
                }
            }
        }

        /// <summary>
        /// Checks to see if current player is a USER-CONTROLLED dummy.
        /// Brandon Watkins
        /// </summary>
        /// <returns>(bool) True if current player is a user-controlled dummy</returns>
        public bool IsDummysTurn()
        {
            // Dummy
            return Table.CurrentPlayerIndex == 2 && Table.CurrentPlayer.IsHuman;
        }

        /// <summary>
        /// Checks to see if current player is the user OR a user-controlled dummy.
        /// Brandon Watkins
        /// </summary>
        /// <returns>(bool) True if current player is the user or user-controlled dummy</returns>
        public bool IsPlayersTurn()
        {
            // Dummy
            return Table.CurrentPlayerIndex == 0 || IsDummysTurn();
        }
    }
}
