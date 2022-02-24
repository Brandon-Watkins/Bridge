using System.Collections.Generic;

namespace ISU_Bridge
{
    public class Contract
    {

        public int Player { get; set; }
        public Card.Face Suit { get; set; }
        public int NumTricks { get; set; }
        public int NumPassed { get; private set; }
        public bool HasOneBid { get; private set; } = false;
        public List<(string player, string bid)> BidHistory { get; private set; } = new List<(string, string)>();

        public Contract()
        {
            BidHistory = new List<(string, string)>();
        }
        
        /// <summary>
        /// 
        /// Brandon Watkins
        /// </summary>
        /// <param name="dealer"></param>
        /// <returns></returns>
        public Contract Reset(int dealer = -1)
        {
            //Player = dealer;
            if (dealer != -1) Player = dealer;
            else Player = 0;
            Suit = Card.Face.Hearts;
            NumTricks = 0;
            NumPassed = 0;
            HasOneBid = false;
            BidHistory = new List<(string, string)>();
            return this;
        }

        /// <summary>
        /// Updates the contract with the bid's information, and resets the "passed" counter.
        /// Brandon Watkins
        /// </summary>
        /// <param name="playerIndex">(int) Bidder's index</param>
        /// <param name="suit">(Card.Face) Bid's suit</param>
        /// <param name="bidValue">(int) Bid's value</param>
        /// <param name="playerName">(string) Bidder's name</param>
        /// <returns>(Contract) This contract</returns>
        public Contract Bid(int playerIndex, Card.Face suit, int bidValue, string playerName = "")
        {
            // Don't add additional bids if user managed to double-click quick enough.
            if (playerName != "" && BidHistory.Count > 0 && BidHistory[BidHistory.Count - 1].player == playerName) return this;

            Player = playerIndex;
            Suit = suit;
            NumTricks = bidValue;
            NumPassed = 0;
            HasOneBid = true;
            BidHistory.Add((playerName == "" ? playerIndex.ToString() : playerName, bidValue.ToString() + " " + 
                (suit == Card.Face.NoTrump ? "No Trump" : suit.ToString())));
            return this;
        }

        /// <summary>
        /// Updates the contract, increasing the number of "passes".
        /// Brandon Watkins
        /// </summary>
        /// <param name="playerName">(string) Passing player's name</param>
        /// <returns>(Contract) This contract</returns>
        public Contract Pass(string playerName = "")
        {
            // Don't add additional bids if user managed to double-click quick enough.
            if (playerName != "" && BidHistory.Count > 0 && BidHistory[BidHistory.Count - 1].player == playerName) return this;

            if (playerName != "") BidHistory.Add((playerName, "Passed"));
            NumPassed++;
            return this;
        }
    }
}
