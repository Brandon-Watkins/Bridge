using System.Collections.Generic;
using BridgeApp;
using System.Windows;
using System.Timers;

namespace ISU_Bridge
{
    public class Game
    {

        public static Game Instance { get; private set; }
        // true: displays logger, and shows all card faces.
        public static bool debugging = false;
        // Make sure to divide by this number, not multiply.
        public static double computerSpeedMultiplier = 1.0; 

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
        public bool IsBidding { get; private set; } = true;

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
        /// <summary>
        /// Sets up the window in preparation for bidding.
        /// Brandon Watkins
        /// </summary>
        private void StartBidding()
        {
            if (!(Contract is null)) MainWindow.Instance.UpdateBidContent();

            Contract = new Contract();
            IsBidding = true;

            if (debugging) BridgeConsole.Log("Bidding started...");

            // Reset dummy hand
            Table.Players[2].IsHuman = false;
            for (int i = 1; i < 4; i++) Table.Players[i].Hand.IsDummy = false;

            Table.SetPlayer(Table.DealerIndex);

            MainWindow.Instance.StartBidding();
        }

        /// <summary>
        /// Called by a player to confirm a bid
        /// </summary>
        /// <param name="face"></param>
        /// <param name="num"></param>
        private void PlaceBid(Card.Face face, int num)
        {
            Contract.Bid(Table.CurrentPlayerIndex, face, num, Table.CurrentPlayer.ToString().Split(" ")[0]);

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
            if (Game.debugging) BridgeConsole.Log($"{Table.CurrentPlayer} passed");
            Contract.Pass(Table.CurrentPlayer.ToString().Split(" ")[0]);

            // Don't increment the player if bidding is over.
            if ((!Contract.HasOneBid && Contract.NumPassed < 4) || (Contract.HasOneBid && Contract.NumPassed < 3)) Table.NextPlayer();

            MainWindow.Instance.Bid.Update(Contract, true);

            // finished bidding? - move to contract
            if (!Contract.HasOneBid && Contract.NumPassed == 4)
            {
                // Make sure the GUI has been updated with the latest pass.
                MainWindow.Instance.UpdateBidContent();
                // Wait half a second before moving onto the next hand.
                new DelayedFunction(500, DelayedPrepHand, this);
                return;
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
                if (debugging) BridgeConsole.Log("Started playing");
                PrepTrick(Table.CurrentPlayerIndex);
            }
            else
            {
                // Show the scoreboard (it can't be clicked on once the messagebox pops up)
                MainWindow.Instance.ScoreboardWindow.Show();
                MessageBoxResult result = MessageBox.Show("Game Over!" + ((Table.Scoreboard.T1total > Table.Scoreboard.T2total) ? 
                    " You Win!" : 
                    " You Lose...") + "\nPlay Again?", "Bridge", MessageBoxButton.YesNo);
                // Hide the scoreboard again, in preparation for closing/restarting the game.
                MainWindow.Instance.ScoreboardWindow.Hide();
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
            Table.ResetTricks();
            MainWindow.Instance.DisplayAllCards();
            StartBidding();
        }

        /// <summary>
        /// Just calls PrepHand. Used for DelayedFunction call.
        /// Brandon Watkins
        /// </summary>
        /// <param name="src">(object) Event source</param>
        /// <param name="e">(ElapsedEventArgs) Event data</param>
        private void DelayedPrepHand(object src, ElapsedEventArgs e)
        {
            PrepHand();
        }

        /// <summary>
        /// Called after the previous trick is processed. updates variables neccessary to reset and prep for the next trick
        /// </summary>
        /// <param name="last_winner_index">player index of the winner of the previous trick</param>
        private void PrepTrick(int last_winner_index)
        {
            if (Table.Players[(Table.CurrentPlayerIndex + 1) % 4].Hand.Cards.Count > 0) 
                Table.SetPlayer(Table.LeadPlayerIndex = last_winner_index);

            // let player or dummy click cards if they won the contract
            MainWindow.Instance.SetCardButtonsEnabled(0, IsPlayersTurn() && !IsDummysTurn());
            // Dummy
            MainWindow.Instance.SetCardButtonsEnabled(2, IsDummysTurn());

            if (Table.CurrentPlayer.Hand.Cards.Count > 0)
            {
                // Cancels any active timers, so all players are able to play again.
                DelayedFunction.ClearTimers();
                MainWindow.Instance.DisplayAllCards();
                if (!IsPlayersTurn())
                {
                    Table.CurrentPlayer.PickCard();
                }
            }
            else
            {
                // Resets the size of everyone's turn-indicator glow
                // (and the size of South's cards), and hides them all.
                MainWindow.Instance.ResetPlayerIllumination();

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
                // This is handling situations where someone wins a game, plays a card, and then Process Trick is
                // called again, resulting in only 1 player in the trick.
                if (c == NullCard.Instance)
                {
                    UpdateCardPlayed();
                    return;
                }

                int card_score = 0;
                card_score += (c.Suit == Table.TrumpSuit) ? 1000 : 0;
                card_score += (c.Suit == Table.CardsPlayed[Table.LeadPlayerIndex].Suit) ? 100 : 0;
                card_score += c.Number;
                if (card_score > best_score) { best_score = card_score; high_scorer = count; }
                count++;
            }
            Table.Players[high_scorer].TricksWonInHand++;
            if (debugging) BridgeConsole.Log(Table.Players[high_scorer] + " won the trick.");

            Table.IsTrickComplete(Table.Players[high_scorer]);

            // Ensures the game shows the last card played for half a second before clearing the cards.
            MainWindow.Instance.DisplayAllCards();

            new DelayedFunction(500, DelayedPrepTrick, this);
        }

        /// <summary>
        /// Just calls PrepTrick. Used with DelayedFunction.
        /// Brandon Watkins
        /// </summary>
        /// <param name="src">(object) Event source</param>
        /// <param name="e">(ElapsedEventArgs) Event data</param>
        private void DelayedPrepTrick(object src, ElapsedEventArgs e)
        {
            PrepTrick(Table.Instance.Tricks[Table.Instance.Tricks.Count - 1].Player.Index);
        }

        /// <summary>
        /// Called whenever a card is played. This is what ticks the game forward
        /// </summary>
        public void UpdateCardPlayed()
        {
            // update card clickability between cards played
            MainWindow.Instance.DisplayAllCards();

            // Checking to see if the trick is over before setting next player, to keep from
            // having the turn indicator highlight the next person, instead of the winner.
            if ((Table.CurrentPlayerIndex + 1) % 4 == Table.LeadPlayerIndex) { ProcessTrick(); }
            else
            {
                Table.NextPlayer();
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
        /// Just restarts the current player's turn. Used when the player tried playing a card that broke the game, 
        /// and stopped the game progression.
        /// Brandon Watkins
        /// </summary>
        public void ResumePlay()
        {
            MainWindow.Instance.DisplayAllCards();
            if (IsPlayersTurn()) MainWindow.Instance.SetCardButtonsEnabled(Table.CurrentPlayerIndex, true);
            else Table.CurrentPlayer.PickCard();
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
