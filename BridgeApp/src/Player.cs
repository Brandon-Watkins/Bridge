using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BridgeApp;
using System.Windows;

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

        public Player(string name)
        {
            hand = new Hand();
            this.name = name;
        }

        public string name;
        public Hand hand;
        public Player teammate;
        public Card ClickedCard;
        public bool isHuman;
        public readonly Random r = new Random();

        //can comment this out and change
        public int tricksWonInHand = 0;

        public int index
        {
            get
            {
                for (int i = 0; i < Table.players.Count; i++)
                    if (Table.players[i] == this)
                        return i;

                // error
                return -1;
            }
        }

        public abstract void QueryBid(Contract c);
        public abstract void PickCard();
    }

    public class AI_Player : Player
    {
        public AI_Player(string name) : base(name) { isHuman = false; }

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
            foreach(Card c in this.hand.Cards)
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
        /// Finds which suit is the least abundan in given hand
        /// Delaney wrote this function
        /// </summary>
        /// <returns></returns>
        public (Card.face, int) DetermineLowestSuit()
        {
            (Card.face, int) diamonds = (Card.face.Diamonds, 0);
            (Card.face, int) hearts = (Card.face.Hearts, 0);
            (Card.face, int) spades = (Card.face.Spades, 0);
            (Card.face, int) clubs = (Card.face.Clubs, 0);
            foreach (Card c in this.hand.Cards)
            {
                if (c.Suit == Card.face.Diamonds)
                {
                    diamonds.Item2 += 1;
                }
                else if (c.Suit == Card.face.Hearts)
                {
                    hearts.Item2 += 1;
                }
                else if (c.Suit == Card.face.Spades)
                {
                    spades.Item2 += 1;
                }
                else if (c.Suit == Card.face.Clubs)
                {
                    clubs.Item2 += 1;
                }
            }
            // find suit with min amount of cards in hand
            (Card.face, int)[] suits = new (Card.face, int)[] { diamonds, hearts, spades, clubs };
            int min = 100;
            int place = -1;
            for (int i = 0; i < 4; i++)
            {
                if (suits[i].Item2 < min)
                {
                    min = suits[i].Item2;
                    place = i;
                }
            }
            return suits[place];
        }
        /// <summary>
        /// Finds what suit is most abundant in given hand
        /// Delaney wrote this function
        /// </summary>
        /// <returns></returns>
        public (Card.face, int) DetermineHighestSuit()
        {
            (Card.face, int) diamonds = (Card.face.Diamonds, 0);
            (Card.face, int) hearts = (Card.face.Hearts, 0);
            (Card.face, int) spades = (Card.face.Spades, 0);
            (Card.face, int) clubs = (Card.face.Clubs, 0);
            
            foreach (Card c in this.hand.Cards)
            {
                if (c.Suit == Card.face.Diamonds)
                {
                    diamonds.Item2 += 1;
                }
                else if (c.Suit == Card.face.Hearts)
                {
                    hearts.Item2 += 1;
                }
                else if (c.Suit == Card.face.Spades)
                {
                    spades.Item2 += 1;
                }
                else if (c.Suit == Card.face.Clubs)
                {
                    clubs.Item2 += 1;
                }
            }
            // find suit with max amount of cards in hand
            (Card.face, int)[] suits = new (Card.face, int)[] { diamonds, hearts, spades, clubs };
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
            (Card.face, int) high = DetermineHighestSuit();
            if (score < 13)
            {
                MainWindow.instance.bid.Pass();
            }
            else if (score >= 15 && score <= 17)
            {
                if (c.numTricks > 1)
                {
                    MainWindow.instance.bid.Pass();
                }
                else if (high.Item2 < 4)
                {
                    if (c.numTricks > 1)
                    {
                        MainWindow.instance.bid.Pass();
                    }
                    else
                    {
                        MainWindow.instance.bid.Bid(Card.face.NoTrump, 1);
                    }
                }
                else
                {
                    if (c.numTricks > 1 || c.suit >= high.Item1)
                    {
                        MainWindow.instance.bid.Pass();
                    }
                    else
                    {
                        MainWindow.instance.bid.Bid(high.Item1, 1);
                    }
                }
            }
            else if (score >= 22)
            {
                MainWindow.instance.bid.Bid(high.Item1, c.numTricks+1);
            }
            else
            {
                if(c.numTricks > 1 || c.suit >= high.Item1)
                {
                    MainWindow.instance.bid.Pass();
                }
                else
                {
                    MainWindow.instance.bid.Bid(high.Item1, 1);
                }
            }
        }
        //Tyler wrote Pick Card function
        public override void PickCard()
        {
            List<Card> options = this.hand.Analyze();
            //int index = r.Next(options.Count);
            int index = determineBestCard(options);
            Card playMe = options[index];
            this.hand.Play(playMe);
            ISU_Bridge.Table.cardsPlayed[Table.currentPlayerIndex] = playMe;
            BridgeConsole.Log(Table.currentPlayer.name + " played " + playMe.ToString());
            Table.game.Update_Card_Played();
        }
        /// <summary>
        /// Finds a better card option than playing a random card
        /// written by Delaney
        /// </summary>
        /// <param name="cards"></param>
        /// <returns></returns>
        public int determineBestCard(List<Card> cards)
        {
            (Card.face, int) high = DetermineHighestSuit();
            int max = 0;
            int index = -1;
            Card.face suit = cards[0].Suit;
            for (int i = 0; i < cards.Count(); i++)
            {
                if (cards[i].Number > max)
                {
                    max = cards[i].Number;
                    index = i;
                }
                else if (cards[i].Number == max & Game.instance.contract.suit != Card.face.NoTrump)
                {
                    if (cards[i].Suit == Game.instance.contract.suit || cards[i].Suit == high.Item1)
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
        //Tyler wrote Play Dummy function
        public void PlayDummy()
        {
            List<Card> playable = this.hand.Analyze();
            bool found = false;
            foreach (Card c in playable)
            {
                if (ClickedCard == c) { found = true; }
            }
            if (found)
            {
                this.hand.Play(ClickedCard);
                ISU_Bridge.Table.cardsPlayed[index] = ClickedCard;
                BridgeConsole.Log(Table.currentPlayer.name + " played " + ClickedCard.ToString());
                Table.game.Update_Card_Played();
            }
        }
    }
    
    public class Real_Player : Player
    {
        public Real_Player(string name) : base(name) { isHuman = true; }

        public override void QueryBid(Contract c)
        {
            MainWindow.instance.bid.SetPlayerGUI(c);
        }

        public override void PickCard()
        {
            List<Card> playable = this.hand.Analyze();
            bool found = false;
            foreach (Card c in playable)
            {
                if (ClickedCard == c) { found = true; }
            }
            if (found)
            {
                this.hand.Play(ClickedCard);
                ISU_Bridge.Table.cardsPlayed[index] = ClickedCard;
                BridgeConsole.Log(Table.currentPlayer.name + " played " + ClickedCard.ToString());
                Table.game.Update_Card_Played();
            }
        }
        public void PlayForDummy()
        {

        }
    }
}
