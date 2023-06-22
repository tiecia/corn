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

        private Random random;
        private BoxValue[][] grid;
        

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
                    grid[y][x] = (BoxValue)(values?.GetValue(random.Next(1, values.Length)) ?? default(BoxValue));
                }

            }
        }

        public string RenderToString()
        {
            StringBuilder sb = new();

            // header
            sb.AppendLine($"Bet: {Bet:n0} corn");
            sb.AppendLine();

            // slots grid
            for (int row = 0; row < Size; row++)
            {
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
                int matches = GetMatches();
                long winnings = GetWinnings();
                sb.AppendLine();
                string match = matches == 1 ? "match" : "matches";
                sb.AppendLine($"You had {matches:n0} {match} and won {winnings:n0} corn!");
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
        private int GetMatches()
        {
            int matches = 0;

            // check rows
            foreach (var row in grid)
            {
                if (row.All(box => box == row[0]))
                {
                    matches++;
                }
            }

            // check columns
            for (int col = 0; col < Size; col++)
            {
                if (grid.All(row => row[col] == grid[0][col]))
                {
                    matches++;
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
                matches++;
            if (forwardDiagMatch)
                matches++;

            return matches;
        }

        // get the winnings for the current board
        public long GetWinnings()
        {
            return GetMatches() * Bet;
        }

    }
}
