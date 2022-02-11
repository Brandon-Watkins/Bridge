
namespace ISU_Bridge
{
    public class Contract
    {

        public int Player { get; set; }
        public Card.Face Suit { get; set; }
        public int NumTricks { get; set; }
        public int NumPassed { get; private set; }
        public bool HasOneBid { get; private set; } = false;
        
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
            return this;
        }

        /// <summary>
        /// 
        /// Brandon Watkins
        /// </summary>
        /// <param name="player"></param>
        /// <param name="suit"></param>
        /// <param name="bidValue"></param>
        /// <returns></returns>
        public Contract Bid(int playerIndex, Card.Face suit, int bidValue)
        {
            Player = playerIndex;
            Suit = suit;
            NumTricks = bidValue;
            NumPassed = 0;
            HasOneBid = true;
            return this;
        }

        /// <summary>
        /// 
        /// Brandon Watkins
        /// </summary>
        /// <returns></returns>
        public Contract Pass()
        {
            NumPassed++;
            return this;
        }
    }
}
