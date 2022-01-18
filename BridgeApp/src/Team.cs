using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISU_Bridge
{
    public class Team
    {
        public Team(Player p1, Player p2)
        {
            _players.Add(p1);
            _players.Add(p2);
        }
        //dm
        private bool _isVulnerable;
        private int _gameScore;
        private List<Player> _players = new List<Player>(); 
        //dm
        public int tricksWon = 0;
        public int overallScore = 0;
        public bool isVulnerable
        {
            get { return _isVulnerable; }
            set { _isVulnerable = value; }
        }
        public int gameScore
        {
            get { return _gameScore; }
            set { _gameScore = value; }
        }
        public List<Player> players
        {
            get { return _players; }
            set { _players = value; }
        }

        // functions 
        public void calculateTricksWon()
        {
            tricksWon = players[0].tricksWonInHand + players[1].tricksWonInHand;
        }
    }
}
