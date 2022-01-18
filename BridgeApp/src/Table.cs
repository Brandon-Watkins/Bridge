using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Class created by Delaney Moore unless stated otherwise by function/variable

namespace ISU_Bridge
{
    public static class Table
    {
        // th
        static private List<Player> _players;
        static private Deck _deck;
        static private Scoreboard _scoreboard;

        //dm
        // private  variables
        static private Card.face _trumpSuit;
        static private Card[] _cardsPlayed = new Card[4];
        static private Contract _winningBid;//maybe stored in game class, tyler creates a singleton
        static private List<string> _currentBids = new List<string>();
        static private int _northSouthScore = 0;
        static private int _eastWestScore = 0;
        static private int _northSouthGamesWon = 0;
        static private int _eastWestGamesWon = 0;
        static private int _currentPlayerIndex; //index of players 
        static private int _leadPlayerIndex; //index of person who lead the hand
        static private Team _northSouth;
        static private Team _eastWest;
        static private List<Team> _teams = new List<Team>() { northSouth, eastWest };


        static public void initialize()
        {
            players = new List<Player>();
            players.Add(new Real_Player("North (YOU)"));
            players.Add(new AI_Player("East"));
            players.Add(new AI_Player("South"));
            players.Add(new AI_Player("West"));

            _northSouth = new Team(players[0], players[2]);
            _eastWest = new Team(players[1], players[3]);
            _teams[0] = _northSouth;
            _teams[1] = _eastWest;
            _scoreboard = new Scoreboard();
        }
        // public variables
        static public Card.face trumpSuit
        {
            get { return Game.instance.contract.suit; }
        }
        static public Card[] cardsPlayed
        {
            get { return _cardsPlayed; }
            set { _cardsPlayed = value; }
        }
        static public Contract winningBid
        {
            get { return _winningBid; }
            set { _winningBid = value; }
        }
        static public List<string> currentBids
        {
            get { return _currentBids; }
            set { _currentBids = value; }
        }
        static public int northSouthScore
        {
            get { return _northSouthScore; }
            set { _northSouthScore = value; }
        }
        static public int eastWestScore
        {
            get { return _eastWestScore; }
            set { _eastWestScore = value; }
        }
        static public int northSouthGamesWon
        {
            get { return _northSouthGamesWon; }
            set { _northSouthGamesWon = value; }
        }
        static public int eastWestGamesWon
        {
            get { return _eastWestGamesWon; }
            set { _eastWestGamesWon = value; }
        }
        static public int currentPlayerIndex
        {
            get { return _currentPlayerIndex; }
            set { _currentPlayerIndex = value; }
        }
        static public int leadPlayerIndex
        {
            get { return _leadPlayerIndex; }
            set { _leadPlayerIndex = value; }
        }
        static public Player currentPlayer
        {
            get { return players[currentPlayerIndex]; }
        }
        static public List<Player> players
        {
            get { return _players; }
            set { _players = value; }
        }
        static public Deck deck
        {
            get { return _deck; }
            set { _deck = value; }
        }
        static public Scoreboard scoreboard
        {
            get { return _scoreboard; }
            set { _scoreboard = value; }
        }
        static public Team northSouth
        {
            get { return _northSouth; }
            set { _northSouth = value; }
        }
        static public Team eastWest
        {
            get { return _eastWest; }
            set { _eastWest = value; }
        }
        static public List<Team> teams
        {
            get { return _teams; }
            set { _teams = value; }
        }
        //functions

        static public Game game { get { return Game.instance; } }

        static public int updateCurrentPlayer()
        {
            if (currentPlayerIndex == 3)
            {
                currentPlayerIndex = 0;
                return 0;
            }
            else
            {
                currentPlayerIndex += 1;
                return currentPlayerIndex;
            }
        }


        /*static public bool determineBid(string input)
        {
            if (input != "PASS") // What should our pass look like, empty? pass?
            {
                _winningBid = input;
                _passBidCount = 0;
                return false;
            }
            else
            {
                _passBidCount += 1;
                if (_passBidCount == 3)
                {
                    _passBidCount = 0;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }*/
    }
}
