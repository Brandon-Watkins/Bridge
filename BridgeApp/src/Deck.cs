using System;
using System.Collections.Generic;
using System.Linq;

namespace ISU_Bridge
{
    public class Deck
    {
        private List<Card> _cards;

        public Deck() { }

        /// <summary>
        /// Builds the deck, shuffles, and then deals it
        /// </summary>
        /// <param name="hands">The hands that are being dealt to</param>
        public void Initialize(List<Hand> hands)
        {
            foreach (Hand hand in hands) hand.IsDummy = false;

            Build();
            Shuffle();
            Deal(hands);
        }

        /// <summary>
        /// Empties the deck, then builds a standard deck of cards.
        /// </summary>
        public void Build()
        {
            _cards = new List<Card>();
            for (int i = 2; i < 15; i++)
            {
                _cards.Add(new Card(i, Card.Face.Clubs, (i+"Clubs.png")));
                _cards.Add(new Card(i, Card.Face.Diamonds, (i+"Diamonds.png")));
                _cards.Add(new Card(i, Card.Face.Hearts, (i+"Hearts.png")));
                _cards.Add(new Card(i, Card.Face.Spades, (i+"Spades.png")));
            }
        }

        /// <summary>
        /// Randomizes the order of cards in the deck
        /// </summary>
        public void Shuffle()
        {
            int n = _cards.Count();
            Random r = new Random();
            while (n > 1)
            {
                n--;
                int k = r.Next(n + 1);
                Card c = _cards[k];
                _cards[k] = _cards[n];
                _cards[n] = c;
            }
        }

        /// <summary>
        /// Function to distribute the cards in the deck as evenly as possible among the provided list of hands.
        /// </summary>
        /// <param name="hands">The hands that cards are intended to be dealt to</param>
        public void Deal(List<Hand> hands)
        {
            foreach (Hand h in hands) { h.Cards.Clear(); }
            int n = _cards.Count();
            while (n > 1)
            {
                foreach (Hand h in hands)
                {
                    n--;
                    if (n >= 0) h.Cards.Add(_cards[n]);
                }
            }
            // Initial sorting (not taking trump into account, wait until after bids)
            foreach (Hand h in hands) h.Sort();
        }
    }
}
