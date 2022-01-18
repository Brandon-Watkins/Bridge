using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using BridgeApp;
using System.Threading;
using System.IO;
using System.Windows;

namespace ISU_Bridge
{
    public class Game
    {
        public static Game instance;

        public Game()
        {
            if (instance != null)
            {
                // maybe events here not such a great idea?
                MainWindow.instance.bid.onBidPlaced -= instance.PlaceBid;
                MainWindow.instance.bid.onPass -= instance.Pass;
            }
            else
            {
                MainWindow.instance.bid.onBidPlaced += PlaceBid;
                MainWindow.instance.bid.onPass += Pass;
            }

            instance = this;

            Prep_Hand();
        }

        public Contract contract { get; private set; }
        public bool isBidding { get; private set; }

        void DealCards()
        {
            List<Hand> hands = new List<Hand>();
            foreach (Player p in Table.players)
            {
                hands.Add(p.hand);
            }
            Table.deck = new Deck();
            Table.deck.Initialize(hands);
        }

        #region Bidding
        void StartBidding()
        {
            contract = new Contract();
            isBidding = true;

            MainWindow.instance.bid.Show();
            MainWindow.instance.bid.Update(contract);
            MainWindow.instance.SetCardButtonsEnabled(0, false);


            Table.currentPlayerIndex = 0;
            BridgeConsole.Log("Bidding started...");
            Table.currentPlayerIndex = 0;
            MainWindow.instance.bid.Update(contract);
            Table.players[Table.currentPlayerIndex].QueryBid(contract);
        }

        /// <summary>
        /// Called by a player to confirm a bid
        /// </summary>
        void PlaceBid(Card.face face, int num)
        {
            // update contract
            contract.numTricks = num;
            contract.player = Table.currentPlayerIndex;
            contract.suit = face;
            contract.numPassed = 0;
            contract.hasOneBid = true;

            Player p = Table.players[Table.currentPlayerIndex];
            Debug.WriteLine(p.name + " bid " + num + " " + face.ToString());
            BridgeConsole.Log($"{Table.currentPlayer.name} bid {num} {face.ToString()}");

            Table.updateCurrentPlayer();
            MainWindow.instance.bid.Update(contract);

            p = Table.players[Table.currentPlayerIndex];
            p.QueryBid(contract);
        }

        /// <summary>
        /// Called by a player to confirm a pass in bidding
        /// </summary>
        void Pass()
        {
            Player p = Table.players[Table.currentPlayerIndex];
            Debug.WriteLine(p.name + " passed");
            BridgeConsole.Log($"{Table.currentPlayer} passed");
            contract.numPassed += 1;

            Table.updateCurrentPlayer();
            MainWindow.instance.bid.Update(contract);

            // finished bidding? - move to contract
            if (!contract.hasOneBid && contract.numPassed == 4)
            {
                Prep_Hand();
                contract.numPassed = 0;
            }
            if (contract.hasOneBid && contract.numPassed == 3)
            {
                isBidding = false;
            }

            if (isBidding)
                Table.players[Table.currentPlayerIndex].QueryBid(contract);
            else
                MainWindow.instance.bid.EndBidding();
        }
        #endregion] Bidding


        /// <summary>
        /// Called to start the entire game. Won't run if the scoreboard has determined the game is over.
        /// </summary>
        public void Play()
        {
            if (!Table.scoreboard.matchOver)
            {
                BridgeConsole.Log("Started playing");
                Table.updateCurrentPlayer();
                Prep_Trick(Table.currentPlayerIndex);
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("Game Over!" + ((Table.scoreboard.t1total > Table.scoreboard.t2total) ? " You Win!" : " You Lose...") + "\nPlay Again?", "ISU Bridge", MessageBoxButton.YesNo);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        Table.initialize();
                        MainWindow.instance.scoreboardWindow.Update(Table.scoreboard);
                        Prep_Hand();
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
        private void Prep_Hand()
        {
            DealCards();
            for (int i = 0; i < 4; i++) { Table.players[i].tricksWonInHand = 0; }
            StartBidding();
            MainWindow.instance.displayAllCards();
        }

        /// <summary>
        /// Called after the previous trick is processed. updates variables neccessary to reset and prep for the next trick
        /// </summary>
        /// <param name="last_winner_index">player index of the winner of the previous trick</param>
        private void Prep_Trick(int last_winner_index)
        {
            for (int i = 0; i < 4; i++) { Table.cardsPlayed[i] = null; }
            Table.leadPlayerIndex = last_winner_index;
            Table.currentPlayerIndex = last_winner_index;

            // let player or dummy click cards if they won the contract
            MainWindow.instance.SetCardButtonsEnabled(0, Table.currentPlayerIndex == 0);
            MainWindow.instance.SetCardButtonsEnabled(2, Table.currentPlayerIndex == 2 && Table.currentPlayer.hand.Is_dummy);

            if (Table.currentPlayer.hand.Cards.Count > 0)
            {
                MainWindow.instance.displayAllCards();
                //if (Table.currentPlayerIndex != 0)
                if (Table.currentPlayerIndex % 2 == 1)
                    Table.currentPlayer.PickCard();
                else if (Table.currentPlayerIndex == 2 && !Table.currentPlayer.hand.Is_dummy)
                    Table.currentPlayer.PickCard();
            }
            else
            {
                Table.scoreboard.handOver();
                if (Table.scoreboard.matchOver)
                {
                    Play();
                }
                else
                {
                    Prep_Hand();
                }
            }

        }

        /// <summary>
        /// Determines the winner of a single trick and increments that player's trickswon variable
        /// </summary>
        private void Process_trick()
        {
            int high_scorer = -1;
            int best_score = 0;
            int count = 0;
            foreach (Card c in Table.cardsPlayed)
            {
                int card_score = 0;
                card_score += (c.Suit == Table.trumpSuit) ? 1000 : 0;
                card_score += (c.Suit == Table.cardsPlayed[Table.leadPlayerIndex].Suit) ? 100 : 0;
                card_score += c.Number;
                if (card_score > best_score) { best_score = card_score; high_scorer = count; }
                count++;
            }
            Table.players[high_scorer].tricksWonInHand++;
            Prep_Trick(high_scorer);
        }

        /// <summary>
        /// Called whenever a card is played. This is what ticks the game forward
        /// </summary>
        public void Update_Card_Played()
        {
            Table.updateCurrentPlayer();

            // update card clickability between cards played
            MainWindow.instance.displayAllCards();

            if (Table.currentPlayerIndex == Table.leadPlayerIndex) { Process_trick(); }
            else
            {
                var idx = Table.currentPlayerIndex;
                if (idx == 0 || (idx == 2 && contract.player % 2 == 0))
                    MainWindow.instance.SetCardButtonsEnabled(idx, true);
                else
                    Table.currentPlayer.PickCard();
            }
        }
    }
}
