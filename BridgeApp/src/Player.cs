using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using BridgeApp;

namespace ISU_Bridge
{
    public abstract class Player
    {

        public enum Seat
        {
            North,
            East,
            South,
            West
        }

        public string Name { get; private set; }
        public Hand Hand { get; private set; }
        public Card ClickedCard { get; set; }
        public bool IsHuman { get; set; } = false;
        public int TricksWonInHand { get; set; } = 0;

        public int Index {
            get {
                return
                    Name.Contains("North") ? 0 :
                    Name == "East" ? 1 :
                    Name == "South" ? 2 :
                    Name == "West" ? 3 :
                    -1;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">(sttring) Player's name</param>
        public Player(string name)
        {
            Hand = new Hand();
            Name = name;
        }

        /// <summary>
        /// Asks the current player for their bid.
        /// </summary>
        /// <param name="c">(Contract) Contract being bidded on</param>
        public abstract void QueryBid(Contract c);

        /// <summary>
        /// Picks a card to play.
        /// </summary>
        public abstract void PickCard();

        /// <summary>
        /// Outputs the player's name
        /// </summary>
        /// <returns>(string) Player's name</returns>
        public override string ToString()
        {
            return Name == null ? "" : Name;
        }
    }


    public class AI_Player : Player
    {

        private static readonly Random random = new Random();
        private Contract contract;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">(string) Player's name</param>
        public AI_Player(string name) : base(name) { 
            IsHuman = false; 
        }

        /// <summary>
        /// Places a bid, if it's this player's turn.
        /// Brandon Watkins
        /// </summary>
        /// <param name="src">(object) Event source</param>
        /// <param name="e">(ElapsedEventArgs) Event data</param>
        private void Bid(object src, ElapsedEventArgs e)
        {
            // Need to double check that it's still this player's turn, as the timer was causing issues.
            if (!(contract is null) && contract.NumPassed < 4 && Table.CurrentPlayer == this) DetermineBid(contract);

            /*
                Various websites that I used and found especially helpful, when learning bidding rules and strategies.
                http://pi.math.cornell.edu/~belk/counting.htm 
                http://pi.math.cornell.edu/~belk/forcing.htm 
                http://pi.math.cornell.edu/~belk/opening.htm 
                http://pi.math.cornell.edu/~belk/preempt.htm 
                http://pi.math.cornell.edu/~belk/rebid.htm 
                http://pi.math.cornell.edu/~belk/respond.htm  
                http://pi.math.cornell.edu/~belk/respond2.htm 
                http://pi.math.cornell.edu/~belk/respond3.htm 
                http://pi.math.cornell.edu/~belk/respond4.htm 
                http://pi.math.cornell.edu/~belk/rminor.htm 
                http://pi.math.cornell.edu/~belk/compete.htm 
                https://pi.math.cornell.edu/~belk/compete3.htm 
                http://pi.math.cornell.edu/~belk/takeout.htm 
                https://www.acbl.org/learn/ 
                https://www.wikihow.com/Bid-in-Bridge 
                https://bicyclecards.com/how-to-play/bridge/ 
                https://en.wikipedia.org/wiki/Forcing_bid#:~:text=From%20Wikipedia%2C%20the%20free%20encyclopedia,over%20an%20intermediate%20opposing%20pass 
                http://pi.math.cornell.edu/~belk/4thSuit.htm 
                https://www.nofearbridge.com/bridge_bidding_cheat_sheet.pdf
                https://www.acbl.org/video-library/#foogallery-23268/i:18/f:Bidding 
                https://www.acbl.org/video-library/#foogallery-23268/i:69/f:Bidding 
                https://www.60secondbridge.com/lessons/beginners-bridge/#:~:text=Short%20suit%20distribution%20points%3A%205,partnership%20has%20reached%20a%20fit.
            */
        }

        /// <summary>
        /// Asks the player for their bid, after a randomized amount of time.
        /// Brandon Watkins
        /// </summary>
        /// <param name="c">(Contract) Contract being bidded on</param>
        public override void QueryBid(Contract c)
        {
            contract = c;
            new DelayedFunction(random.Next(600, 901), Bid, this);
        }

        /// <summary>
        /// Determines how good the hand by counting high valued cards
        /// Delaney wrote this function
        /// </summary>
        /// <returns></returns>
        private int CountHand()
        {
            int score = 0;
            foreach(Card c in Hand.Cards)
            {
                if (c.Number == 14) // if ace
                {
                    score += 4;
                }
                else if (c.Number == 13) // if king 
                {
                    score += 3;
                }
                else if (c.Number == 12) // if queen
                {
                    score += 2;
                }
                else if (c.Number == 11) // if jack 
                {
                    score += 1;
                }
            }
            return score;
        }

        /// <summary>
        /// Finds what suit is most abundant in given hand
        /// Delaney wrote this function
        /// </summary>
        /// <returns></returns>
        public (Card.Face, int) DetermineHighestSuit()
        {
            (Card.Face, int) diamonds = (Card.Face.Diamonds, 0);
            (Card.Face, int) hearts = (Card.Face.Hearts, 0);
            (Card.Face, int) spades = (Card.Face.Spades, 0);
            (Card.Face, int) clubs = (Card.Face.Clubs, 0);

            // Need to double check that it's still this player's turn, as the timer was causing issues.
            if (Table.CurrentPlayer != this) return clubs;
            
            foreach (Card c in Hand.Cards)
            {
                if (c.Suit == Card.Face.Diamonds)
                {
                    diamonds.Item2 += 1;
                }
                else if (c.Suit == Card.Face.Hearts)
                {
                    hearts.Item2 += 1;
                }
                else if (c.Suit == Card.Face.Spades)
                {
                    spades.Item2 += 1;
                }
                else if (c.Suit == Card.Face.Clubs)
                {
                    clubs.Item2 += 1;
                }
            }

            // find suit with max amount of cards in hand
            (Card.Face, int)[] suits = new (Card.Face, int)[] { diamonds, hearts, spades, clubs };
            int max = 0;
            int place = -1;
            for (int i = 0; i < 4; i++)
            {
                if(suits[i].Item2 > max)
                {
                    max = suits[i].Item2;
                    place = i;
                }
            }
            return place > -1 ? suits[place] : suits[0];
        }

        /// <summary>
        /// Finds the best bidding option
        /// Delaney wrote this function
        /// </summary>
        /// <param name="c"></param>
        public void DetermineBid(Contract c)
        {
            // Checking for empty hand, had this throw an exception earlier.
            if (Hand.Cards.Count < 1)
            {
                if (Game.debugging) BridgeConsole.Log(this.Name + " tried bidding with an empty hand.");
                return;
            }

            // Need to double check that it's still this player's turn, as the timer was causing issues.
            if (Table.CurrentPlayer != this) return;

            // https://www.wikihow.com/Bid-in-Bridge
            int score = CountHand();

            (Card.Face, int) high = DetermineHighestSuit();

            // If 0, they had no cards in hand.
            if (high.Item2 == 0) return;

            if (score < 13)
            {
                MainWindow.Instance.Bid.Pass();
            }
            else if (score >= 15 && score <= 17)
            {
                if (c.NumTricks > 1)
                {
                    MainWindow.Instance.Bid.Pass();
                }
                else if (high.Item2 < 4)
                {
                    MainWindow.Instance.Bid.Bid(Card.Face.NoTrump, 1);
                }
                else
                {
                    if (c.Suit >= high.Item1)
                    {
                        MainWindow.Instance.Bid.Pass();
                    }
                    else
                    {
                        MainWindow.Instance.Bid.Bid(high.Item1, 1);
                    }
                }
            }
            else if (score >= 22)
            {
                MainWindow.Instance.Bid.Bid(high.Item1, c.NumTricks+1);
            }
            else
            {
                if (c.NumTricks > 1 || c.Suit >= high.Item1)
                {
                    MainWindow.Instance.Bid.Pass();
                }
                else
                {
                    MainWindow.Instance.Bid.Bid(high.Item1, 1);
                }
            }
        }

        /// <summary>
        /// If player is user-controlled, plays the clicked-on card. Else the AI determines the best card to play.
        /// Brandon Watkins
        /// </summary>
        public override void PickCard()
        {
            new DelayedFunction(IsHuman ? 0 : random.Next(600, 901), PlayCard, this);
        }

        /// <summary>
        /// Finds and plays a card.
        /// </summary>
        /// <param name="src">(object) Event source</param>
        /// <param name="e">(ElapsedEventArgs) Event data</param>
        private void PlayCard(object src, ElapsedEventArgs e)
        {
            // Need to double check that it's still this player's turn, as the timer was causing issues.
            if (Table.CurrentPlayer != this) return;

            List<Card> options = Hand.PlayableCards();
            Card c = NullCard.Instance;
            // AI-controlled player chooses a card
            if (!IsHuman)
            {
                int index = DetermineBestCard(options);
                if (index == -1) return;
                c = options[index];
            }
            // Dummy (South) clicked a card
            else
            {
                bool found = false;
                foreach (Card card in options)
                {
                    if (ClickedCard == card) { found = true; }
                }
                if (found) c = ClickedCard;
                else if (Game.debugging) BridgeConsole.Log("User clicked on illegal card.");
            }
            // Verifying a (legal) card was picked. Attempting to play it.
            if (c != NullCard.Instance && Table.SetCardPlayed(c, this.Index))
            {
                Hand.Play(c);
                if (Game.debugging) BridgeConsole.Log(Table.CurrentPlayer.Name + " played " + c.ToString());
                Table.Game.UpdateCardPlayed();
            }
        }

        /// <summary>
        /// Finds a better card option than playing a random card
        /// written by Delaney
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public int DetermineBestCard(List<Card> cards)
        {
            // Need to double check that it's still this player's turn, as the timer was causing issues.
            if (Table.CurrentPlayer != this) return -1;

            (Card.Face, int) high = DetermineHighestSuit();

            // 0 means there are no cards in hand
            if (high.Item2 == 0) return - 1;

            int max = 0;
            int index = -1;
            for (int i = 0; i < cards.Count(); i++)
            {
                if (cards[i].Number > max)
                {
                    max = cards[i].Number;
                    index = i;
                }
                else if (cards[i].Number == max & Game.Instance.Contract.Suit != Card.Face.NoTrump)
                {
                    if (cards[i].Suit == Game.Instance.Contract.Suit || cards[i].Suit == high.Item1)
                    {
                        max = cards[i].Number;
                        index = i;
                    }
                }
                else if (cards[i].Number == max)
                {
                    if(cards[i].Suit == high.Item1)
                    {
                        max = cards[i].Number;
                        index = i;
                    }
                }
            }
            return index;
        }
    }


    public class Real_Player : Player
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">(string) Player's name</param>
        public Real_Player(string name) : base(name) { IsHuman = true; }

        public override void QueryBid(Contract c)
        {
            MainWindow.Instance.Bid.SetPlayerGUI(c);
        }

        public override void PickCard()
        {
            if (Table.CurrentPlayer != this)
            {
                if (Game.debugging) BridgeConsole.Log(Table.CurrentPlayer.Name + " tried playing on another user's turn.");
                return;
            }
            List<Card> playable = Hand.PlayableCards();
            bool found = false;
            foreach (Card c in playable) if (ClickedCard == c) found = true;
            if (found && Table.SetCardPlayed(ClickedCard, this.Index))
            {
                Hand.Play(ClickedCard);
                if (Game.debugging) BridgeConsole.Log(Table.CurrentPlayer.Name + " played " + ClickedCard.ToString());
                Table.Game.UpdateCardPlayed();
            }
        }
    }
}
