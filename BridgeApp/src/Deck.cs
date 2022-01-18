using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISU_Bridge
{
    public class Deck
    {

        private List<Card> cards;
        public List<Card> Cards { get { return cards; } set { cards = value; } }

        public Deck() { }

        /// <summary>
        /// Builds the deck, shuffles, and then deals it
        /// </summary>
        /// <param name="hands">The hands that are being dealt to</param>
        public void Initialize(List<Hand> hands)
        {
            Build();
            Shuffle();
            Deal(hands);
        }

        /// <summary>
        /// Shuffles the deck and deals out
        /// </summary>
        /// <param name="hands">The hands being dealt to</param>
        public void Reset(List<Hand> hands)
        {
            Shuffle();
            Deal(hands);
        }

        /// <summary>
        /// Empties the deck, then builds a standard deck of cards.
        /// </summary>
        public void Build()
        {
            cards = new List<Card>();
            for (int i = 2; i < 15; i++)
            {
                cards.Add(new Card(i, Card.face.Clubs, (i+"Clubs.png")));
                cards.Add(new Card(i, Card.face.Diamonds, (i+"Diamonds.png")));
                cards.Add(new Card(i, Card.face.Hearts, (i+"Hearts.png")));
                cards.Add(new Card(i, Card.face.Spades,(i+"Spades.png")));
            }
        }

        /// <summary>
        /// Randomizes the order of cards in the deck
        /// </summary>
        public void Shuffle()
        {
            int n = cards.Count();
            Random r = new Random();
            while (n > 1)
            {
                n--;
                int k = r.Next(n + 1);
                Card c = Cards[k];
                Cards[k] = Cards[n];
                Cards[n] = c;
            }
        }

        /// <summary>
        /// Function to distribute the cards in the deck as evenly as possible among the provided list of hands.
        /// </summary>
        /// <param name="hands">The hands that cards are intended to be dealt to</param>
        public void Deal(List<Hand> hands)
        {
            foreach (Hand h in hands) { h.Cards.Clear(); }
            int n = cards.Count();
            while (n > 1)
            {
                foreach (Hand h in hands)
                {
                    n--;
                    if (n >= 0)
                    {
                        Cards[n].Owner = h;
                        h.Cards.Add(Cards[n]); // th
                    }
                }
            }
            foreach (Hand h in hands) { h.Sort(); }
        }
    }
}
