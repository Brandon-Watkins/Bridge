using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISU_Bridge
{
    public class Card
    {
        public static face[] suits
        {
            get
            {
                // https://stackoverflow.com/questions/3816718/how-to-get-an-array-of-all-enum-values-in-c
                if (_suits == null)
                    _suits = (face[])Enum.GetValues(typeof(face));

                return _suits;
            }
        }
        private static face[] _suits;

        public enum face
        {
            Clubs,
            Diamonds,
            Hearts,
            Spades,
            NoTrump
        }
        private face suit;
        private int number;
        private Hand owner;
        private string imageLocation;

        public face Suit { get { return suit; } set { suit = value; } }
        public int Number { get { return number; } set { if (value > 1 && value < 15) number = value; } }
        public Hand Owner { get { return owner; } set { owner = value; } }
        public string ImageLocation { get { return imageLocation; } set { imageLocation = value; } }

        /// <summary>
        /// Default Constructor for the Card Class
        /// </summary>
        public Card() { }

        /// <summary>
        /// Constructor for the card class
        /// </summary>
        /// <param name="num">Value of the Card, should be between 1 and 13</param>
        /// <param name="type">enumerated type for the suit of the card</param>
        public Card(int num, face type, string loc)
        {
            Number = num;
            Suit = type;
            imageLocation = loc;
        }


        public override string ToString() // th
        {
            //return number + " of " + suit.ToString();
            return number + suit.ToString()[0].ToString();
        }
    }
}
