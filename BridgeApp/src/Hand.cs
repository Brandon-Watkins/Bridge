using System.Collections.Generic;
using System.Linq;

namespace ISU_Bridge
{
    public class Hand : SimpleObservable
    {

        public List<Card> Cards { get; set; }
        public bool IsDummy { get; set; }

        public Hand() : base()
        {
            Cards = new List<Card>();
    }

        /// <summary>
        /// Checks the cards in the hand to determine which cards are playable,
        /// then returns the list of playable cards.
        /// </summary>
        /// <returns>Cards that are currently playable</returns>
        public List<Card> PlayableCards()
        {
            if (Table.CardsPlayed[Table.LeadPlayerIndex] == NullCard.Instance) return Cards;
            List<Card> Output = new List<Card>();
            foreach (Card c in Cards) if (c.Suit == Table.CardsPlayed[Table.LeadPlayerIndex].Suit) Output.Add(c);
            if (Output.Count() == 0) return Cards;
            return Output;
        }

        /// <summary>
        /// Sorts the hand by suit, and then by ascending value.
        /// Brandon Watkins
        /// </summary>
        /// <param name="trump">(Card.Face) The trump suit (optional)</param>
        public void Sort(Card.Face trump = Card.Face.NoTrump)
        {
            List<Card> output = new List<Card>();
            IEnumerable<Card> diamonds = PullAndSortCardsOfSuit(Card.Face.Diamonds);
            IEnumerable<Card> clubs = PullAndSortCardsOfSuit(Card.Face.Clubs);
            IEnumerable<Card> hearts = PullAndSortCardsOfSuit(Card.Face.Hearts);
            IEnumerable<Card> spades = PullAndSortCardsOfSuit(Card.Face.Spades);
            IEnumerable<Card> trumpSuitCards = new List<Card>();
            List<IEnumerable<Card>> listOfLists = new List<IEnumerable<Card>> { diamonds, clubs, hearts, spades };

            // Pull the trump suit out of the list, to tack it onto the end, afterward
            if (trump == Card.Face.Spades)
            {
                trumpSuitCards = spades;
                listOfLists.Remove(spades);
            }
            else if (trump == Card.Face.Hearts)
            {
                trumpSuitCards = hearts;
                listOfLists.Remove(hearts);
            }
            else if (trump == Card.Face.Clubs)
            {
                trumpSuitCards = clubs;
                listOfLists.Remove(clubs);
            }
            else if (trump == Card.Face.Diamonds)
            {
                trumpSuitCards = diamonds;
                listOfLists.Remove(diamonds);
            }

            // Add the sorted sublists to the output list
            foreach (Card card in trumpSuitCards)
            {
                output.Add(card);
            }
            foreach(IEnumerable<Card> ienum in listOfLists)
            {
                foreach(Card card in ienum)
                {
                    output.Add(card);
                }
            }

            Cards = output;
        }

        /// <summary>
        /// Returns the cards matching the given suit, sorted by ascending value.
        /// Brandon Watkins
        /// </summary>
        /// <param name="suit">(Card.Face) The suit to sort</param>
        /// <returns>(<IEnumerable<Card>) Sorted list of cards matching the givern suit</Card></returns>
        public IEnumerable<Card> PullAndSortCardsOfSuit(Card.Face suit)
        {
            List<Card> temp = new List<Card>();

            foreach (Card c in Cards) { if (c.Suit == suit) { temp.Add(c); } }

            return temp.OrderBy(card => card.Number);
        }

        /// <summary>
        /// Returns string containing info for each card in the hand
        /// </summary>
        public override string ToString()
        {
            string s = "";
            foreach (Card c in Cards)
            {
                s += c.ToString() + "\n";
            }

            return s;
        }

        /// <summary>
        /// Removes the card from the hand
        /// </summary>
        /// <param name="playedCard">the card to remove</param>
        public void Play(Card playedCard)
        {
            Cards.Remove(playedCard);
            NotifyObservers();
        }
    }
}
