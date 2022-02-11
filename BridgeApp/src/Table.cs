using System.Collections.Generic;
using System;

namespace ISU_Bridge
{
    public static class Table
    {

        public static Card.Face TrumpSuit => Game.Instance.Contract.Suit;
        public static Card[] CardsPlayed { get; set; } = new Card[4];
        public static int CurrentPlayerIndex { get; set; }
        public static int LeadPlayerIndex { get; set; }
        public static Player CurrentPlayer => Players[CurrentPlayerIndex];
        public static List<Player> Players { get; private set; }
        public static Deck Deck { get; set; }
        public static Scoreboard Scoreboard { get; set; }
        public static Team NorthSouth { get; private set; }
        public static Team EastWest { get; private set; }
        public static List<Team> Teams { get; private set; } = new List<Team>() { NorthSouth, EastWest };
        public static int DealerIndex { get; private set; }
        public static Game Game => Game.Instance;

        public static void Initialize()
        {
            Players = new List<Player>
            {
                new Real_Player("North (YOU)"),
                new AI_Player("East"),
                new AI_Player("South"),
                new AI_Player("West")
            };

            NorthSouth = new Team(Players[0], Players[2]);
            EastWest = new Team(Players[1], Players[3]);
            Teams[0] = NorthSouth;
            Teams[1] = EastWest;
            Scoreboard = new Scoreboard();

            // Gives a random dealer for the first hand (equivalent to players picking a card, with highest card = dealer)
            Random random = new Random();
            DealerIndex = random.Next(0, 4);
        }

        /// <summary>
        /// Increments current player index.
        /// Brandon Watkins
        /// </summary>
        /// <returns>(int) Index of the new current player</returns>
        public static int NextPlayer()
        {
            return CurrentPlayerIndex = (CurrentPlayerIndex + 1) % 4;
        }

        /// <summary>
        /// Increments dealer's player index, and sets the current player to dealer.
        /// Brandon Watkins
        /// </summary>
        /// <returns>(int) Index of the new dealer</returns>
        public static int NextDealer() {
            DealerIndex = (DealerIndex + 1) % 4;
            CurrentPlayerIndex = DealerIndex;
            return DealerIndex;
        }
    }
}
