using CornBot.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CornBot.Models
{
    public class SlotMachine
    {

        private enum BoxValue
        {
            NONE,
            CORN,
            POPCORN,
            UNICORN,
        }

        public int Size { get; private set; }
        public int RevealProgress { get; set; }
        public long Bet { get; private set; }

        private readonly Random random;
        private readonly BoxValue[][] grid;
        

        public SlotMachine(int size, long bet, Random random)
        {
            this.Size = size;
            this.random = random;
            this.Bet = bet;
            grid = new BoxValue[size][];
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            Array? values = Enum.GetValues(typeof(BoxValue));

            for (int y = 0; y < Size; y++)
            {
                grid[y] = new BoxValue[Size];
                for (int x = 0; x < Size; x++)
                {
                    switch (random.Next(0, 5))
                    {
                        case 0:
                            grid[y][x] = BoxValue.CORN;
                            break;
                        case 1:
                        case 2:
                            grid[y][x] = BoxValue.UNICORN;
                            break;
                        case 3:
                        case 4:
                            grid[y][x] = BoxValue.POPCORN;
                            break;
                    }
                }

            }
        }

        public string RenderToString(long newCorn, int numberInDay)
        {
            StringBuilder sb = new();

            // header
            sb.AppendLine($"## **Cornucopia** ({numberInDay + 1}/3)");
            sb.AppendLine($"### Bet: {Bet:n0} corn");
            sb.AppendLine();

            // slots grid
            for (int row = 0; row < Size; row++)
            {
                sb.Append("# ");
                for (int col = 0; col < Size; col++)
                {
                    if (col >= RevealProgress)
                    {
                        sb.Append(Constants.LARGE_BLACK_SQUARE_EMOJI);
                    } else
                    {
                        sb.Append(GetBoxValue(row, col));
                    }
                }
                sb.AppendLine();
            }

            // footer if all the board has been revealed
            if (RevealProgress == Size)
            {
                int matches = GetMatches().Values.Sum();

                long winnings = GetWinnings();
                sb.AppendLine();
                sb.AppendLine();
                string lineStr = matches == 1 ? "line" : "lines";
                long absDifference = Math.Abs(winnings - Bet);
                if (winnings == Bet)
                    sb.AppendLine($"### You had {matches:n0} {lineStr} and your corn remained the same.");
                else if (winnings < Bet)
                    sb.AppendLine($"### You had {matches:n0} {lineStr} and lost {absDifference:n0} corn.");
                else
                    sb.AppendLine($"### You had {matches:n0} {lineStr} and won {absDifference:n0} corn!");
                sb.AppendLine();
                sb.AppendLine($"**You now have {newCorn} corn.**");
            }
            
            return sb.ToString();
        }

        // a function that returns a string representation of the box at the given coordinates
        private string GetBoxValue(int row, int col)
        {
            BoxValue value = grid[row][col];
            return value switch
            {
                BoxValue.CORN => Constants.CORN_EMOJI,
                BoxValue.POPCORN => Constants.POPCORN_EMOJI,
                BoxValue.UNICORN => Constants.UNICORN_EMOJI,
                _ => Constants.LARGE_BLACK_SQUARE_EMOJI,
            };
        }

        // a function that returns the number of rows, columns, and diagonals that have the same box value
        private Dictionary<BoxValue, int> GetMatches()
        {
            Dictionary<BoxValue, int> matches = new();
            matches[BoxValue.CORN] = 0;
            matches[BoxValue.UNICORN] = 0;
            matches[BoxValue.POPCORN] = 0;

            // check rows
            foreach (var row in grid)
            {
                if (row.All(box => box == row[0]))
                {
                    matches[row[0]]++;
                }
            }

            // check columns
            for (int col = 0; col < Size; col++)
            {
                if (grid.All(row => row[col] == grid[0][col]))
                {
                    matches[grid[0][col]]++;
                }
            }

            // check diagonals
            bool backDiagMatch = true;
            bool forwardDiagMatch = true;
            for (int pos = 0; pos < Size; pos++)
            {
                if (grid[pos][pos] != grid[0][0])
                    backDiagMatch = false;
                if (grid[pos][Size - pos - 1] != grid[0][Size - 1])
                    forwardDiagMatch = false;
            }
            if (backDiagMatch)
                matches[grid[0][0]]++;
            if (forwardDiagMatch)
                matches[grid[0][Size - 1]]++;

            return matches;
        }

        // get the winnings for the current board
        public long GetWinnings()
        {
            double multiplier = 0.0;
            var matches = GetMatches();

            multiplier += matches[BoxValue.CORN] * 3.0;
            multiplier += matches[BoxValue.UNICORN];
            multiplier += matches[BoxValue.POPCORN];

            multiplier = 0.2 + multiplier * 0.9;

            return (long) Math.Round(multiplier * Bet);
        }

    }
}
