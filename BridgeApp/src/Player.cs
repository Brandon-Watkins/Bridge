using System;
using System.Collections.Generic;
using System.Linq;
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
        public bool IsHuman { get; set; }
        public int TricksWonInHand { get; set; } = 0;

        public int Index {
            get {
                for (int i = 0; i < Table.Players.Count; i++)
                {
                    if (Table.Players[i] == this)
                    {
                        return i;
                    }
                }

                // error
                return -1;
            }
        }
        public Player(string name)
        {
            Hand = new Hand();
            Name = name;
        }

        public abstract void QueryBid(Contract c);

        public abstract void PickCard();

        public override string ToString()
        {
            return Name == null ? "" : Name;
        }
    }

    public class AI_Player : Player
    {

        public AI_Player(string name) : base(name) { IsHuman = false; }

        public override void QueryBid(Contract c)
        {
            DetermineBid(c);
        }

        /// <summary>
        /// Determines how good the hand by counting high valued cards
        /// Delaney wrote this function
        /// </summary>
        /// <returns></returns>
        private int CountHand()
        {
            int score = 0;
            foreach(Card c in this.Hand.Cards)
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
            return suits[place];
        }

        /// <summary>
        /// Finds the best bidding option
        /// Delaney wrote this function
        /// </summary>
        /// <param name="c"></param>
        public void DetermineBid(Contract c)
        {
            // https://www.wikihow.com/Bid-in-Bridge
            int score = CountHand();
            (Card.Face, int) high = DetermineHighestSuit();
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
            List<Card> options = Hand.PlayableCards();
            Card c = NullCard.Instance;
            if (!IsHuman)
            {
                int index = DetermineBestCard(options);
                c = options[index];
            }
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
            if (c != NullCard.Instance)
            {
                Hand.Play(c);
                Table.CardsPlayed[Table.CurrentPlayerIndex] = c;
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
            (Card.Face, int) high = DetermineHighestSuit();
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

        public Real_Player(string name) : base(name) { IsHuman = true; }

        public override void QueryBid(Contract c)
        {
            MainWindow.Instance.Bid.SetPlayerGUI(c);
        }

        public override void PickCard()
        {
            List<Card> playable = Hand.PlayableCards();
            bool found = false;
            foreach (Card c in playable)
            {
                if (ClickedCard == c) { found = true; }
            }
            if (found)
            {
                Hand.Play(ClickedCard);
                Table.CardsPlayed[Index] = ClickedCard;
                BridgeConsole.Log(Table.CurrentPlayer.Name + " played " + ClickedCard.ToString());
                Table.Game.UpdateCardPlayed();
            }
        }
    }
}
