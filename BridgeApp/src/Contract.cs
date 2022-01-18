using System;
using System.Collections.Generic;
using System.Text;

namespace ISU_Bridge
{
    public class Contract
    {
        public int player;
        public Card.face suit;
        public int numTricks;
        public int numPassed;
        public bool hasOneBid = false;

        public bool isValidBid(Card.face f, int num)
        {
            if (num < 1 || num > 7)
                return false;
            else if (num < numTricks)
                return false;
            else if (num == numTricks && f <= suit)
                return false;
            else
                return true;
        }
    }
}
