using System.Collections.Generic;

namespace ISU_Bridge
{
    public class Team
    {

        private List<Player> _players = new List<Player>();

        public int TricksWon { get; set; } = 0;
        public bool IsVulnerable { get; set; }

        public Team(Player p1, Player p2)
        {
            _players.Add(p1);
            _players.Add(p2);
        }

        public void CalculateTricksWon()
        {
            TricksWon = _players[0].TricksWonInHand + _players[1].TricksWonInHand;
        }
    }
}
