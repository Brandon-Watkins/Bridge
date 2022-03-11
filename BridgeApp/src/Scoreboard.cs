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
        public int[] T1GameScore { get; set; } = new int[3] { 0, 0, 0 };
        public int[] T2GameScore { get; set; } = new int[3] { 0, 0, 0 };
        public int T1Bonus { get; set; } = 0;
        public int T2Bonus { get; set; } = 0;
        public int T1Total { get; set; } = 0;
        public int T2Total { get; set; } = 0;
        
        public void HandOver()
        {
            _trumpSuit = Game.Instance.Contract.Suit;
            _tricksToWin = Game.Instance.Contract.NumTricks + 6;
            _doubled = Game.Instance.Contract.Doubled;
            _redoubled = Game.Instance.Contract.Redoubled;
            Table.NorthSouth.CalculateTricksWon();
            Table.EastWest.CalculateTricksWon();
            Team winner = DetermineWinnerOfHand();
            if (winner == _biddingTeam)
            {
                int addGameScore = CalculateGameScore();
                int addBonus = CalculateOverTricksAndBonus();
                if (winner == Table.NorthSouth)
                {
                    T1GameScore[_currentGame - 1] += addGameScore;
                    T1Bonus += addBonus;
                }
                else
                {
                    T2GameScore[_currentGame - 1] += addGameScore;
                    T2Bonus += addBonus;
                }
            }
            else
            {
                int addScore = CalculateUnderTricks();
                if (winner == Table.NorthSouth) T1Bonus += addScore;
                else T2Bonus += addScore;
            }
            T1Total = T1GameScore[0] + T1GameScore[1] + T1GameScore[2] + T1Bonus;
            T2Total = T2GameScore[0] + T2GameScore[1] + T2GameScore[2] + T2Bonus;
            MainWindow.Instance.ScoreboardWindow.Update(this);

            DetermineMatchOver(winner, (winner == Table.NorthSouth ? T1GameScore : T2GameScore)[_currentGame - 1]);
            DetermineGameOver();
        }

        public Team DetermineWinnerOfHand()
        {
            if (Table.Game.Contract.Player % 2 == 0)
            {
                _biddingTeam = Table.NorthSouth;
                if (_biddingTeam.TricksWon >= _tricksToWin) return Table.NorthSouth;
                else return Table.EastWest;
            }
            else
            {
                _biddingTeam = Table.EastWest;
                if (_biddingTeam.TricksWon >= _tricksToWin) return Table.EastWest;
                else return Table.NorthSouth;
            }
        }

        /// <summary>
        /// Calculates the game score ("Below the line").
        /// Brandon Watkins
        /// </summary>
        /// <returns>(int) "Below the line" game score</returns>
        public int CalculateGameScore()
        {
            int dif = _biddingTeam.TricksWon - 6;
            if (_trumpSuit == Card.Face.Diamonds || _trumpSuit == Card.Face.Clubs) return dif * (_redoubled ? 80 : _doubled ? 40 : 20);
            else if (_trumpSuit == Card.Face.Hearts || _trumpSuit == Card.Face.Spades) return dif * (_redoubled ? 120 : _doubled ? 60 : 30);
            else if (_trumpSuit == Card.Face.NoTrump && dif > 0) return (_redoubled ? 160 : _doubled ? 80 : 40) + ((dif - 1) * (_redoubled ? 120 : _doubled ? 60 : 30));

            return 0;
        }
        
        public int CalculateOverTricksAndBonus()
        {
            int addToScore = 0;
            // Small slam
            if (_tricksToWin == 12) addToScore = _biddingTeam.IsVulnerable ? 750 : 500;
            // Grand slam
            else if (_tricksToWin == 13) addToScore = _biddingTeam.IsVulnerable ? 1500 : 1000;

            // Making double/redouble
            addToScore += _doubled || _redoubled ? 50 : 0;

            // Bonus points for winning 2/2 or 2/3 games is in DetermineMatchOver().

            //calculate overtricks
            int dif = _biddingTeam.TricksWon - _tricksToWin;
            if (_trumpSuit == Card.Face.Diamonds || _trumpSuit == Card.Face.Clubs) addToScore += dif * 20;
            else if (_trumpSuit == Card.Face.Hearts || _trumpSuit == Card.Face.Spades || _trumpSuit == Card.Face.NoTrump) addToScore += dif * 30;
            
            return addToScore;
        }

        /// <summary>
        /// Calculates the under trick penalty points for the game.
        /// Brandon Watkins
        /// </summary>
        /// <returns>(int) Penalty points earned for this game</returns>
        public int CalculateUnderTricks()
        {
            int dif = _tricksToWin - _biddingTeam.TricksWon;
            int score = 0;

            if (_biddingTeam.IsVulnerable)
            {
                if (_redoubled) score = 400 + 600 * (dif - 1);
                else if (_doubled) score = 200 + 300 * (dif - 1);
                else score = 100 * dif;
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
                else score = 50 * dif;
            }

            return score;
        }

        public void DetermineGameOver()
        {
            if (T1GameScore[_currentGame - 1] >= 100)
            {
                Table.NorthSouth.IsVulnerable = true;
                _currentGame += 1;
            }
            else if (T2GameScore[_currentGame - 1] >= 100)
            {
                Table.EastWest.IsVulnerable = true;
                _currentGame += 1;
            }
        }

        /// <summary>
        /// Determines whether the winner has won 2 games. 
        /// If so, it also awards the bonus points for winning.
        /// Brandon Watkins
        /// </summary>
        /// <param name="winner">(Team) Winner of last/current hand</param>
        /// <param name="score">(int) Winner's game score</param>
        public void DetermineMatchOver(Team winner, int score)
        {
            if (winner.IsVulnerable && score >= 100)
            {
                // Bonus points for winning 2/2 or 2/3 games
                int bonusScore = _currentGame == 2 ? 700 : 500;
                if (winner == Table.NorthSouth)
                {
                    T1Bonus += bonusScore;
                    T1Total = T1GameScore[0] + T1GameScore[1] + T1GameScore[2] + T1Bonus;
                }
                else
                {
                    T2Bonus += bonusScore;
                    T2Total = T2GameScore[0] + T2GameScore[1] + T2GameScore[2] + T2Bonus;
                }
                MainWindow.Instance.ScoreboardWindow.Update(this);

                MatchOver = true;
            }
        }
    }
}