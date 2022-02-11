
namespace ISU_Bridge
{
    public class Card
    {

        public enum Face
        {
            Clubs,
            Diamonds,
            Hearts,
            Spades,
            NoTrump
        }

        private int _number;

        public Face Suit { get; set; }
        public int Number { get { return _number; } set { if (value > 1 && value < 15) _number = value; } }
        public Hand Owner { get; set; }
        public string ImageLocation { get; set; }

        /// <summary>
        /// Default Constructor for the Card Class
        /// </summary>
        public Card() { }

        /// <summary>
        /// Constructor for the card class
        /// </summary>
        /// <param name="num">Value of the Card, should be between 1 and 13</param>
        /// <param name="type">enumerated type for the suit of the card</param>
        public Card(int num, Face type, string loc)
        {
            Number = num;
            Suit = type;
            ImageLocation = loc;
        }

        public override string ToString()
        {
            return _number + Suit.ToString()[0].ToString();
        }
    }

    /// <summary>
    /// Not a playable card, just used as a null place holder. 
    /// If you use this, make sure you're checking if X == NullCard.Instance, or this will cause issues.
    /// Brandon Watkins
    /// </summary>
    public class NullCard : Card
    {

        private static NullCard _instance;
        public static NullCard Instance {
            get {
                if (_instance == null) return _instance = new NullCard();
                else return _instance;
            }
        }

        public NullCard() : base(2, Face.NoTrump, "")
        {
            
        }
    }
}
