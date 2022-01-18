using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BridgeApp;

//Delaney wrote this class unless labeled otherwise

namespace ISU_Bridge
{
    public class Scoreboard
    {
        // https://www.247bridge.com/


        public int tricksToWin;
        public List<int> tricksWon = new List<int>();
        public Card.face trumpSuit;
        public bool gameOver = false;
        public bool matchOver = false;
        public int currentGame = 1;
        public bool doubled = false;
        public bool redoubled = false;
        public Team biddingTeam;
        public int[] t1gamescore = new int[3] {0, 0, 0};
        public int[] t2gamescore = new int[3] {0, 0, 0};
        public int t1bonus = 0;
        public int t2bonus = 0;
        public int t1total = 0;
        public int t2total = 0;
        
        public void handOver()
        {
            trumpSuit = Game.instance.contract.suit;
            tricksToWin = Game.instance.contract.numTricks;
            Table.teams[0].calculateTricksWon();
            Table.teams[1].calculateTricksWon();
            int winner = determineWinnerOfHand();
            if (Table.teams[winner] == biddingTeam)
            {
                int addGameScore = calculateGameScore();
                int addBonus = calculateOverTricksAndBonus(biddingTeam.tricksWon, Table.teams[winner]);
                //Table.teams[winner].overallScore += addBonus + addGameScore;
                //Table.teams[winner].gameScore += addGameScore;
                if (winner == 0)
                {
                    t1gamescore[currentGame-1] += addGameScore;
                    t1bonus += addBonus;
                }
                else
                {
                    t2gamescore[currentGame - 1] += addGameScore;
                    t2bonus += addBonus;
                }
            }
            else
            {
                int addScore = calculateUnderTricks(biddingTeam.tricksWon, biddingTeam.isVulnerable);
                //Table.teams[winner].overallScore += addScore;
                if (winner == 0)
                {
                    t1bonus += addScore;
                }
                else
                {
                    t2bonus += addScore;
                }
            }
            t1total = t1gamescore[0] + t1gamescore[1] + t1gamescore[2] + t1bonus;
            t2total = t2gamescore[0] + t2gamescore[1] + t2gamescore[2] + t2bonus;
            MainWindow.instance.scoreboardWindow.Update(this);
            if (winner == 0)
            {
                determineMatchOver(Table.teams[winner], t1gamescore[currentGame-1]);
            }
            else
            {
                determineMatchOver(Table.teams[winner], t2gamescore[currentGame - 1]);
            }
            determineGameOver();
        }
        public int determineWinnerOfHand()
        {
            if (Table.game.contract.player == 0 || Table.game.contract.player == 2)
            {
                biddingTeam = Table.teams[0];
                if (biddingTeam.tricksWon >= tricksToWin + 6)
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
                biddingTeam = Table.teams[1];
                if (biddingTeam.tricksWon >= tricksToWin + 6)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }
        public int calculateGameScore()
        {
            int addToScore = 0;
            int add = 0;
            if (trumpSuit == Card.face.Diamonds || trumpSuit == Card.face.Clubs)
            {
                add = tricksToWin * 20;
                addToScore += add;
            }
            else if (trumpSuit == Card.face.Hearts || trumpSuit == Card.face.Spades)
            {
                add = tricksToWin * 30;
                addToScore += add;
            }
            else if (trumpSuit == Card.face.NoTrump)
            {
                addToScore += 40;
                add = 30 * (tricksToWin - 1);
                addToScore += add;
            }
            return addToScore;
        }
        
        public int calculateOverTricksAndBonus(int biddingTeamsTricks, Team winner)
        {
            int addToScore = 0;
            //small slam
            if (tricksToWin == 12)
            {
                if (winner.isVulnerable == true)
                {
                    addToScore += 750;
                }
                else
                {
                    addToScore += 500;
                }
            }
            //grand slam
            else if (tricksToWin == 13)
            {
                if (winner.isVulnerable == true)
                {
                    addToScore += 1000;
                }
                else
                {
                    addToScore += 1500;
                }
            }
            
            //calculate overtricks
            if (biddingTeamsTricks > tricksToWin+6)
            {
                int dif = biddingTeamsTricks - tricksToWin - 6;
                if (dif > 0 & (trumpSuit == Card.face.Diamonds || trumpSuit == Card.face.Clubs))
                {
                    addToScore += dif * 20;
                }
                else if (dif > 0 & (trumpSuit == Card.face.Hearts || trumpSuit == Card.face.Spades || trumpSuit == Card.face.NoTrump))
                {
                    addToScore += dif * 30;
                }
            }
            return addToScore;
        }

        public int calculateUnderTricks(int biddingTeamsTricks, bool vulnerable)
        {
            int dif = tricksToWin + 6 - biddingTeamsTricks;
            int addToScore = 0;
            if (vulnerable == true)
            {
                if (doubled == true)
                {
                    addToScore += 200;
                    addToScore += 300 * (dif - 1);
                }
                else if (redoubled == true)
                {
                    addToScore += 400;
                    addToScore += 600 * (dif - 1);
                }
                else
                {
                    addToScore += 100 * dif;
                }
            }
            else // not vulnerable
            {
                if (doubled == true)
                {
                    addToScore += 100;
                    addToScore += 200 * (dif - 1);
                }
                else if (redoubled == true)
                {
                    addToScore += 200;
                    addToScore += 400 * (dif - 1);
                }
                else
                {
                    addToScore += 50 * dif;
                }
            }
            return addToScore;
        }

        public void determineGameOver()
        {
            if (t1gamescore[currentGame-1] >= 100)
            {
                gameOver = true;
                Table.northSouth.isVulnerable = true;
                currentGame += 1;
            }
            else if (t2gamescore[currentGame - 1] >= 100)
            {
                gameOver = true;
                Table.eastWest.isVulnerable = true;
                currentGame += 1;
            }
            else
            {
                gameOver = false;
            }
        }
        public void determineMatchOver(Team winner, int score)
        {
            if (winner.isVulnerable == true && score >= 100)
            {
                matchOver = true;
            }
        }



        
    }
}
