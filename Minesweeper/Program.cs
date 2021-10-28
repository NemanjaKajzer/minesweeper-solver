using MineField;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Minesweeper
{
    internal class Program
    {
        private static void Main()
        {
            var won = 0;
            var lost = 0;
            for (var i = 0; i < 100; i++)
            {
                CreateAndSolve(ref won, ref lost);

                Console.WriteLine("Won: " + won + " - Lost: " + lost);
            }

            Console.WriteLine("PCT: " + won + "%");

        }

        /// <summary>
        /// Creates MinesweeperField and tries to solve it
        /// </summary>
        /// <param name="won"></param>
        /// <param name="lost"></param>
        private static void CreateAndSolve(ref int won, ref int lost)
        {
            var field = new MineSweeperField(16, 16, 40);

            // TODO mark all the bombs until field.IsFinished is true, without being killed by a bomb

            var markedCells = new List<Cell>();
            var riskyCellsTupleList = new List<Tuple<double, List<Cell>>>();

            var allOpened = field[1, 0];

            try
            {
                // until all bombs are marked
                while (!field.IsFinished)
                {
                    var it = 0;
                    //do more iterations of this so we have more secure guesses
                    while (it < 20)
                    {
                        // analyze only edge cells
                        foreach (var openCell in allOpened.Where(cell => cell.AdjacentBombs > 0).ToList())
                        {
                            // cells adjacent to current cell that could contain a bomb
                            var possibleAdjacentBombCells =
                                GetAllPossibleAdjacentBombCells(markedCells, allOpened, field, openCell);

                            // number of adjacent bombs to current cell
                            var adjacentBombs = openCell.AdjacentBombs;

                            // number of cells adjacent to the current cell that are marked as bombs
                            var numberOfMarkedNeighbors =
                                GetNumberOfMarkedNeighbors(markedCells, field, openCell);

                            var oneCellProbability = (double)(adjacentBombs - numberOfMarkedNeighbors) /
                                                     possibleAdjacentBombCells.Count;

                            if (oneCellProbability == 0)
                            {
                                // open all neighbors that aren't open and add them to allOpened list
                                OpenAllNonMarkedNeighbors(ref allOpened, possibleAdjacentBombCells, ref field);
                            }
                            else if (oneCellProbability == 1)
                            {
                                // mark all neighbors that aren't open as bombs and add them to markedCells list
                                MarkAllNeighborsAsBombs(ref markedCells, ref field, possibleAdjacentBombCells);
                            }
                            else
                            {
                                // if inconclusive, add all adjacent cells that could contain a bomb to a tuple with the probability of each cell containing a bomb
                                AddRiskyCellTuple(ref riskyCellsTupleList, possibleAdjacentBombCells,
                                    oneCellProbability);
                            }

                            // remove all tuples that contain cells that were marked as risky, but later on were marked as bombs or opened
                            if (riskyCellsTupleList.Count > 0 && markedCells.Count > 0)
                            {
                                RemoveMarkedAndOpenedRiskyCells(ref riskyCellsTupleList, markedCells, allOpened);
                            }
                        }
                        allOpened = allOpened.Select(cell => field[cell.X, cell.Y].FirstOrDefault(x => x.Equals(cell))).ToList();
                        it++;
                    }

                    OpenLeastRiskyCell(ref allOpened, ref riskyCellsTupleList, ref field);
                }

                if (field.IsFinished)
                {
                    won++;
                    Console.WriteLine("WON");
                }
            }
            catch (Exception)
            {
                lost++;
                Console.WriteLine("BOOM");
            }
        }

        /// <summary>
        /// Gets all adjacent cells to current cell
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="field"></param>
        /// <returns> Cells from one row above and under and one column to the right and left (if they are inside the field) </returns>
        private static List<Cell> GetNeighbors(Cell cell, MineSweeperField field)
        {
            var xs = new List<int>() { cell.X - 1, cell.X, cell.X + 1 };
            var ys = new List<int>() { cell.Y - 1, cell.Y, cell.Y + 1 };

            var neighbors = new List<Cell>();

            foreach (var x in xs)
            {
                foreach (var y in ys)
                {
                    var neighbor = new Cell()
                    {
                        X = x,
                        Y = y
                    };

                    if (IsValidCell(neighbor, field) && !cell.Equals(neighbor))
                    {
                        neighbors.Add(neighbor);
                    }

                }
            }

            return neighbors;
        }

        /// <summary>
        /// Checks if cell is inside the field
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="field"></param>
        /// <returns> True if cell is inside the field, else false </returns>
        private static bool IsValidCell(Cell cell, MineSweeperField field)
        {
            return cell.X >= 0 && cell.X < field.Width && cell.Y >= 0 && cell.Y < field.Height;
        }

        /// <summary>
        /// Checks for all valid unopened adjacent cells that weren't marked as bombs
        /// </summary>
        /// <param name="markedCells"></param>
        /// <param name="allOpened"></param>
        /// <param name="field"></param>
        /// <param name="currentCell"></param>
        /// <returns> All fields that are adjacent to current cell and that have possibility of having a bomb </returns>
        private static List<Cell> GetAllPossibleAdjacentBombCells(List<Cell> markedCells, List<Cell> allOpened, MineSweeperField field, Cell currentCell)
        {
            return GetNeighbors(currentCell, field).Except(markedCells).Except(allOpened).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="markedCells"></param>
        /// <param name="field"></param>
        /// <param name="currentCell"></param>
        /// <returns> Number of adjacent fields that are marked as bombs </returns>
        private static int GetNumberOfMarkedNeighbors(List<Cell> markedCells, MineSweeperField field, Cell currentCell)
        {
            return GetNeighbors(currentCell, field).Where(neighbor => markedCells.Contains(neighbor)).ToList().Count;
        }

        /// <summary>
        /// Opens all unopened adjacent fields that aren't marked as bombs
        /// </summary>
        /// <param name="allOpened"></param>
        /// <param name="possibleAdjacentBombCells"></param>
        /// <param name="field"></param>
        private static void OpenAllNonMarkedNeighbors(ref List<Cell> allOpened, List<Cell> possibleAdjacentBombCells, ref MineSweeperField field)
        {
            foreach (var possibleAdjacentBombCell in possibleAdjacentBombCells)
            {
                var openedCells = field[possibleAdjacentBombCell.X, possibleAdjacentBombCell.Y];
                allOpened.Add(possibleAdjacentBombCell);
                allOpened.AddRange(openedCells.Except(allOpened));
            }
        }

        /// <summary>
        /// Marks every unopened adjacent cell as bomb
        /// </summary>
        /// <param name="markedCells"></param>
        /// <param name="field"></param>
        /// <param name="possibleAdjacentBombCells"></param>
        private static void MarkAllNeighborsAsBombs(ref List<Cell> markedCells, ref MineSweeperField field, List<Cell> possibleAdjacentBombCells)
        {
            try
            {
                foreach (var possibleAdjacentBombCell in possibleAdjacentBombCells)
                {
                    field.MarkAsBomb(possibleAdjacentBombCell.X, possibleAdjacentBombCell.Y);
                    markedCells.Add(possibleAdjacentBombCell);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void AddRiskyCellTuple(ref List<Tuple<double, List<Cell>>> riskyCellsTupleList,
            List<Cell> possibleAdjacentBombCells, double oneCellProbability)
        {
            if (possibleAdjacentBombCells.Any())
            {
                var entry = new Tuple<double, List<Cell>>(oneCellProbability,
                    possibleAdjacentBombCells);

                if (!riskyCellsTupleList.Contains(entry))
                {
                    riskyCellsTupleList.Add(entry);
                    // sort by probability of being a bomb (ascending)
                    riskyCellsTupleList.Sort((x, y) => x.Item1.CompareTo(y.Item1));
                }
            }
        }

        /// <summary>
        /// Removes all tuples that have been added to risky cells list but later on were marked as bombs or opened
        /// </summary>
        /// <param name="riskyTuples"></param>
        /// <param name="markedCells"></param>
        /// <param name="allOpened"></param>
        private static void RemoveMarkedAndOpenedRiskyCells(ref List<Tuple<double, List<Cell>>> riskyTuples,
            List<Cell> markedCells, List<Cell> allOpened)
        {
            riskyTuples.RemoveAll(x => markedCells.Intersect(x.Item2).Any());
            riskyTuples.RemoveAll(x => allOpened.Intersect(x.Item2).Any());
        }

        /// <summary>
        /// Opens the cell that has smallest probability of being a bomb (based on calculation made from some opened cell)
        /// </summary>
        /// <param name="allOpened"></param>
        /// <param name="riskyTuples"></param>
        /// <param name="field"></param>
        private static void OpenLeastRiskyCell(ref List<Cell> allOpened, ref List<Tuple<double, List<Cell>>> riskyTuples, ref MineSweeperField field)
        {
            if (riskyTuples.Any())
            {
                if (riskyTuples[0].Item2.Any())
                {
                    var opened = field[riskyTuples[0].Item2[0].X, riskyTuples[0].Item2[0].Y];
                    allOpened.AddRange(opened.Except(allOpened));
                }

                riskyTuples.RemoveAt(0);
            }
        }

    }
}
