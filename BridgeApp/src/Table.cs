using System.Collections.Generic;
using System;
using BridgeApp;
using System.Diagnostics;
using System.Timers;

namespace ISU_Bridge
{
    /// <summary>
    /// Holds all of the components for the game, and manages some of the legal play validation.
    /// Brandon Watkins
    /// </summary>
    public class Table: SimpleObservable
    {
        public static Card.Face TrumpSuit => Game.Instance.Contract.Suit;
        public static Card[] CardsPlayed { get; set; } = new Card[4] { NullCard.Instance, NullCard.Instance, NullCard.Instance, NullCard.Instance };
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
        // stopwatch is used to keep players from spamming method calls (ie. double clicking cards)
        private static Stopwatch stopwatch = Stopwatch.StartNew();
        private static Table _instance;
        public static Table Instance {
            get {
                if (_instance is null) _instance = new Table();
                return _instance;
            }
        }
        private List<(Card[] Cards, Player Player)> _tricks = new List<(Card[], Player)>();
        public List<(Card[] Cards, Player Player)> Tricks {
            get {
                return _tricks;
            }
            private set {
                _tricks = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public Table() : base()
        {

        }

        /// <summary>
        /// Sets up the players, team, scoreboard, and dealer.
        /// </summary>
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

            CardsPlayed = new Card[4] { NullCard.Instance, NullCard.Instance, NullCard.Instance, NullCard.Instance };

            // Gives a random dealer for the first hand (equivalent to players picking a card, with highest card = dealer)
            SetPlayer(DealerIndex = new Random().Next(0, 4));
        }

        /// <summary>
        /// Sets the CurrentPlayerIndex and notifies observers.
        /// Brandon Watkins
        /// </summary>
        /// <param name="playerIndex">(int) Index to set current player to</param>
        /// <returns>(int) The updated current player index</returns>
        public static int SetPlayer(int playerIndex)
        {
            CurrentPlayerIndex = playerIndex;
            Instance.NotifyObservers();
            return CurrentPlayerIndex;
        }

        /// <summary>
        /// Increments current player index.
        /// Brandon Watkins
        /// </summary>
        /// <returns>(int) Index of the new current player</returns>
        public static int NextPlayer()
        {
            return SetPlayer((CurrentPlayerIndex + 1) % 4);
        }

        /// <summary>
        /// Increments dealer's player index, and sets the current player to dealer.
        /// Brandon Watkins
        /// </summary>
        /// <returns>(int) Index of the new dealer</returns>
        public static int NextDealer() {
            return SetPlayer(DealerIndex = (DealerIndex + 1) % 4);
        }

        /// <summary>
        /// After lots of error-checking, adds the card to the played cards for the current trick.
        /// Brandon Watkins
        /// </summary>
        /// <param name="card">(Card) The card to play</param>
        /// <param name="playerIndex">(int) The index of the player playing the card</param>
        /// <returns>(bool) true if the card was played successfully</returns>
        public static bool SetCardPlayed(Card card, int playerIndex)
        {
            // Verifying the player hasn't already played a card during this trick.
            // There's a *Really* good chance that if this occurs, it occurs between tricks.
            // Addresses timer-related issues, as well as user double-clicking
            int cardsPlayed = CountCardsPlayed();
            // If player already played a card this trick
            if (CardsPlayed[playerIndex] != NullCard.Instance) {
                if (Game.debugging) BridgeConsole.Log(Players[playerIndex].Name + " tried playing a card, but already played a card this trick.");
                if (cardsPlayed == 4)
                {
                    if ((Instance.Tricks.Count > 0 && Instance.Tricks[Instance.Tricks.Count - 1].Player.Index == playerIndex) ||
                        CurrentPlayerIndex == playerIndex)
                    {
                        Game.UpdateCardPlayed();
                        return true;
                    }
                }
            }
            // If it isn't this player's turn
            else if (Instance.Tricks.Count > 0 && (Instance.Tricks[Instance.Tricks.Count - 1].Player.Index + cardsPlayed) % 4 != playerIndex)
            {
                if (Game.debugging) BridgeConsole.Log(Players[playerIndex].Name + " tried playing a card, but it isn't their turn.");
            }
            // Requiring that there's 800 ms between turns, and 1600ms between turns from separate tricks.
            // Need to extract this timer out to Game class, when I have time.
            else if (stopwatch.ElapsedMilliseconds < 850 / Game.computerSpeedMultiplier || 
                (stopwatch.ElapsedMilliseconds < 1700 / Game.computerSpeedMultiplier && cardsPlayed == 0))
            {
                if (Game.debugging) BridgeConsole.Log(Players[playerIndex].Name + " tried playing a card, but it's still on cooldown.");
                Game.ResumePlay();
                return false;
            }
            // Passed all checks, playing card.
            else
            {
                if (Game.debugging) BridgeConsole.Log("Elapsed time (ms): " + stopwatch.ElapsedMilliseconds.ToString());
                stopwatch.Restart();
                CardsPlayed[playerIndex] = card;
                return true;
            }

            // Failed checks, resetting and continuing.
            new DelayedFunction(150, DelayedResetPlayerAndContinue, Instance);

            return false;
        }

        /// <summary>
        /// Resets the current player, ideally based on the winner of the previous trick, and the number of cards played.
        /// Used when a player made an illegal play and broke the game/turn order.
        /// Brandon Watkins
        /// </summary>
        /// <param name="src">(object) Event source</param>
        /// <param name="e">(ElapsedEventtArgs) Event data</param>
        private static void DelayedResetPlayerAndContinue(Object src, ElapsedEventArgs e)
        {
            ResetCurrentPlayer();
            new DelayedFunction(150, DelayedResume, Instance);
        }

        /// <summary>
        /// Resumes/restarts the game.
        /// Used when something broke the flow of the game, and it needs to restart the player's turn.
        /// Brandon Watkins
        /// </summary>
        /// <param name="src">(object) Event source</param>
        /// <param name="e">(ElapsedEventArgs) Event data</param>
        private static void DelayedResume(Object src, ElapsedEventArgs e)
        {
            Game.ResumePlay();
        }

        /// <summary>
        /// Verifies that all 4 players have played a card for the current trick. 
        /// If so, it'll also add the cards to the list of previous tricks, and resets the cards to null
        /// Brandon Watkins
        /// </summary>
        /// <param name="winner">(Player) Candidate for the winning player</param>
        /// <returns>(bool) true if everyone played a card</returns>
        public static bool IsTrickComplete(Player winner)
        {
            if (!IsTrickLegal()) return false;

            Instance.Tricks.Add((new Card[] { CardsPlayed[0] , CardsPlayed[1] , CardsPlayed[2] , CardsPlayed[3] }, winner));
            for (int i = 0; i < 4; i++)
            {
                CardsPlayed[i] = NullCard.Instance;
            }
            return true;
        }

        /// <summary>
        /// Verifies that all 4 players have played a card for the current trick. 
        /// Brandon Watkins
        /// </summary>
        /// <returns>(bool) true if all 4 players played a card</returns>
        private static bool IsTrickLegal()
        {
            if (CardsPlayed.Length != 4)
            {
                if (Game.debugging) BridgeConsole.Log("Illegal trick had " + CardsPlayed.Length + " cards played.");
                return false;
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    if (CardsPlayed[i] == NullCard.Instance)
                    {
                        if (Game.debugging) BridgeConsole.Log("Illegal trick had null cards in CardsPlayed.");
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Resets the number of tricks won for all players.
        /// Brandon Watkins
        /// </summary>
        public static void ResetTricks()
        {
            Instance.Tricks = new List<(Card[], Player)>();
            for (int i = 0; i < 4; i++) Players[i].TricksWonInHand = 0;
        }

        /// <summary>
        /// Returns all cards played during the current trick, and starts it over.
        /// Use when there is an unrecoverable/unforeseen illegal play.
        /// Obviously not an ideal solution, this should be a last-ditch effort.
        /// Brandon Watkins
        /// </summary>
        private static void RestartTrick()
        {
            for (int i = 0; i < 4; i++)
            {
                // Return all cards, sort the players' hands again, and reset the cards played pile.
                if (CardsPlayed[i] != NullCard.Instance && !Players[i].Hand.Cards.Contains(CardsPlayed[i]))
                {
                    Players[i].Hand.Cards.Add(CardsPlayed[i]);
                    CardsPlayed[i] = NullCard.Instance;
                    Players[i].Hand.Sort(Game.Contract.Suit);
                }
            }
            SetPlayer(LeadPlayerIndex);
            Game.ResumePlay();
        }

        /// <summary>
        /// Resets the current player, based on number of cards played, and the winner of the last trick (dealer if none).
        /// Also resets the ClickedCard property for the human players, to keep from automatically "re-clicking" the same card after reset.
        /// Use whenever a player attempts to play out of turn, or a player attempts to play multiple cards.
        /// Brandon Watkins
        /// </summary>
        private static void ResetCurrentPlayer()
        {
            // To keep it from automatically "re-clicking" on the same card again, after the timer.
            if (Game.IsPlayersTurn())
            {
                // Resetting both, if South is the dummy hand. Otherwise it could run into issues
                //    when North clicks outside of turn, then south does, then north does again, and vice versa.
                if (Players[2].IsHuman) Players[2].ClickedCard = NullCard.Instance;
                Players[0].ClickedCard = NullCard.Instance;
            }

            int cardsPlayed = CountCardsPlayed();
            // If the winner of the last trick is known, reset to winner + # of cards played
            if (Instance.Tricks.Count > 0) SetPlayer((Instance.Tricks[Instance.Tricks.Count - 1].Player.Index + cardsPlayed) % 4);
            // If player is the only one to have played a card, just reset to the player after the dealer.
            else if (cardsPlayed == 0) SetPlayer((DealerIndex + cardsPlayed + 1) % 4);
            // Else reset the entire trick, to ensure turn order didn't get messed up. Obviously not ideal.
            else RestartTrick();
        }

        /// <summary>
        /// Simple counts the number of non-null cards in CardsPlayed.
        /// Brandon Watkins
        /// </summary>
        /// <returns>(int) Number of cards played this trick</returns>
        private static int CountCardsPlayed()
        {
            // Determine number of cards in CardsPlayed
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                if (CardsPlayed[i] != NullCard.Instance) count++;
            }
            return count;
        }
    }
}
