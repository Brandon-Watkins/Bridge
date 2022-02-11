using System.Collections.Generic;
using BridgeApp;

//Delaney wrote this class unless labeled otherwise

namespace ISU_Bridge
{
    public class Scoreboard
    {

        private int _tricksToWin;
        private Card.Face _trumpSuit;
        private int _currentGame = 1;
        private bool _doubled = false;
        private bool _redoubled = false;
        private Team _biddingTeam;

        public bool MatchOver { get; set; } = false;
        public int[] T1gamescore { get; set; } = new int[3] { 0, 0, 0 };
        public int[] T2gamescore { get; set; } = new int[3] { 0, 0, 0 };
        public int T1bonus { get; set; } = 0;
        public int T2bonus { get; set; } = 0;
        public int T1total { get; set; } = 0;
        public int T2total { get; set; } = 0;
        
        public void HandOver()
        {
            _trumpSuit = Game.Instance.Contract.Suit;
            _tricksToWin = Game.Instance.Contract.NumTricks;
            Table.Teams[0].CalculateTricksWon();
            Table.Teams[1].CalculateTricksWon();
            int winner = DetermineWinnerOfHand();
            if (Table.Teams[winner] == _biddingTeam)
            {
                int addGameScore = CalculateGameScore();
                int addBonus = CalculateOverTricksAndBonus(_biddingTeam.TricksWon, Table.Teams[winner]);
                if (winner == 0)
                {
                    T1gamescore[_currentGame - 1] += addGameScore;
                    T1bonus += addBonus;
                }
                else
                {
                    T2gamescore[_currentGame - 1] += addGameScore;
                    T2bonus += addBonus;
                }
            }
            else
            {
                int addScore = CalculateUnderTricks(_biddingTeam.TricksWon, _biddingTeam.IsVulnerable);
                if (winner == 0)
                {
                    T1bonus += addScore;
                }
                else
                {
                    T2bonus += addScore;
                }
            }
            T1total = T1gamescore[0] + T1gamescore[1] + T1gamescore[2] + T1bonus;
            T2total = T2gamescore[0] + T2gamescore[1] + T2gamescore[2] + T2bonus;
            MainWindow.Instance.ScoreboardWindow.Update(this);
            if (winner == 0)
            {
                DetermineMatchOver(Table.Teams[winner], T1gamescore[_currentGame - 1]);
            }
            else
            {
                DetermineMatchOver(Table.Teams[winner], T2gamescore[_currentGame - 1]);
            }
            DetermineGameOver();
        }

        public int DetermineWinnerOfHand()
        {
            if (Table.Game.Contract.Player % 2 == 0)
            {
                _biddingTeam = Table.Teams[0];
                if (_biddingTeam.TricksWon >= _tricksToWin + 6)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            else
            {
                _biddingTeam = Table.Teams[1];
                if (_biddingTeam.TricksWon >= _tricksToWin + 6)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        public int CalculateGameScore()
        {
            if (_trumpSuit == Card.Face.Diamonds || _trumpSuit == Card.Face.Clubs)
            {
                return _tricksToWin * 20;
            }
            else if (_trumpSuit == Card.Face.Hearts || _trumpSuit == Card.Face.Spades)
            {
                return _tricksToWin * 30;
            }
            else if (_trumpSuit == Card.Face.NoTrump)
            {
                return 40 + 30 * (_tricksToWin - 1);
            }
            return 0;
        }
        
        public int CalculateOverTricksAndBonus(int biddingTeamsTricks, Team winner)
        {
            int addToScore = 0;
            //small slam
            if (_tricksToWin == 12)
            {
                if (winner.IsVulnerable)
                {
                    addToScore = 750;
                }
                else
                {
                    addToScore = 500;
                }
            }
            //grand slam
            else if (_tricksToWin == 13)
            {
                if (winner.IsVulnerable)
                {
                    addToScore = 1000;
                }
                else
                {
                    addToScore = 1500;
                }
            }
            
            //calculate overtricks
            if (biddingTeamsTricks > _tricksToWin + 6)
            {
                int dif = biddingTeamsTricks - _tricksToWin - 6;
                if (_trumpSuit == Card.Face.Diamonds || _trumpSuit == Card.Face.Clubs)
                {
                    addToScore += dif * 20;
                }
                else if (_trumpSuit == Card.Face.Hearts || _trumpSuit == Card.Face.Spades || _trumpSuit == Card.Face.NoTrump)
                {
                    addToScore += dif * 30;
                }
            }
            return addToScore;
        }

        /// <summary>
        /// Calculates the under trick points for the game.
        /// Brandon Watkins
        /// </summary>
        /// <param name="biddingTeamsTricks">(int) Tricks won by the declaring team</param>
        /// <param name="vulnerable">(bool) True if the declaring team has won a game</param>
        /// <returns>(intt) Points earned for this game</returns>
        public int CalculateUnderTricks(int biddingTeamsTricks, bool vulnerable)
        {
            int dif = _tricksToWin + 6 - biddingTeamsTricks;
            int score = 0;

            if (vulnerable)
            {
                if (_redoubled)
                {
                    score = 400;
                    score += 600 * (dif - 1);
                }
                else if (_doubled)
                {
                    score = 200;
                    score += 300 * (dif - 1);
                }
                else
                {
                    score = 100 * dif;
                }
            }
            else
            {
                if (_redoubled)
                {
                    score = 200;
                    if (dif >= 2) score += 400;
                    if (dif >= 3) score += 400;
                    if (dif > 3) score += 600 * (dif - 3);
                }
                else if (_doubled)
                {
                    score = 100;
                    if (dif >= 2) score += 200;
                    if (dif >= 3) score += 200;
                    if (dif > 3) score += 300 * (dif - 3);
                }
                else
                {
                    score = 50 * dif;
                }
            }

            return score;
        }

        public void DetermineGameOver()
        {
            if (T1gamescore[_currentGame - 1] >= 100)
            {
                Table.NorthSouth.IsVulnerable = true;
                _currentGame += 1;
            }
            else if (T2gamescore[_currentGame - 1] >= 100)
            {
                Table.EastWest.IsVulnerable = true;
                _currentGame += 1;
            }
        }

        public void DetermineMatchOver(Team winner, int score)
        {
            if (winner.IsVulnerable && score >= 100)
            {
                MatchOver = true;
            }
        }
    }
}