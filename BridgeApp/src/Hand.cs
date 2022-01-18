using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISU_Bridge
{
    public class Hand
    {
        private List<Card> cards;
        private bool is_dummy;
        //private Player owner;
        public List<Card> Cards { get { return cards; } set { cards = value; } }
        public bool Is_dummy { get { return is_dummy; } set { is_dummy = value; } }
        //public Player Owner { get { return owner; } set { owner = value; } }
        public Hand()
        {
            cards = new List<Card>();
        }
        //public Hand(Player p) { Owner = p; }

        /// <summary>
        /// Checks the cards in the hand to determine which cards are playable,
        /// then returns the list of playable cards.
        /// </summary>
        /// <returns>Cards that are currently playable</returns>
        public List<Card> Analyze()
        {
            if (Table.cardsPlayed[Table.leadPlayerIndex] == null) { return Cards; }
            List<Card> Output = new List<Card>();
            foreach (Card c in Cards) { if (c.Suit == Table.cardsPlayed[Table.leadPlayerIndex].Suit) { Output.Add(c); } }
            if (Output.Count() == 0) { return cards; }
            return Output;
        }

        /// <summary>
        /// Sorts the cards by suit and then by value
        /// </summary>
        public void Sort()
        {
            List<Card> output = new List<Card>();
            List<Card> temp = new List<Card>();

            // Pull all Spades
            foreach (Card c in Cards) { if (c.Suit == Card.face.Spades) { temp.Add(c); } }
            temp.OrderBy(card => card.Number);
            foreach (Card c in temp) { output.Add(c); }
            temp.Clear();

            // Pull all Hearts
            foreach (Card c in Cards) { if (c.Suit == Card.face.Hearts) { temp.Add(c); } }
            temp.OrderBy(card => card.Number);
            foreach (Card c in temp) { output.Add(c); }
            temp.Clear();

            // Pull all Clubs
            foreach (Card c in Cards) { if (c.Suit == Card.face.Clubs) { temp.Add(c); } }
            temp.OrderBy(card => card.Number);
            foreach (Card c in temp) { output.Add(c); }
            temp.Clear();

            // Pull all Diamonds
            foreach (Card c in Cards) { if (c.Suit == Card.face.Diamonds) { temp.Add(c); } }
            temp.OrderBy(card => card.Number);
            foreach (Card c in temp) { output.Add(c); }
            temp.Clear();


            // Update Cards
            Cards = output;
        }

        /// <summary>
        /// Returns the number of cards in the hand with the given suit
        /// </summary>
        public int NumOfSuit(Card.face f)
        {
            int count = 0;
            foreach (Card c in Cards)
                if (c.Suit == f)
                    count++;
            return count;
        }

        /// <summary>
        /// Returns string containing info for each card in the hand
        /// </summary>
        public override string ToString()
        {
            string s = "";
            foreach (Card c in cards)
                s += c.ToString() + "\n";
            return s;
        }

        /// <summary>
        /// Removes the card from the hand
        /// </summary>
        /// <param name="playedCard">the card to remove</param>
        public void Play(Card playedCard)
        {
            cards.Remove(playedCard);
        }
    }
}
