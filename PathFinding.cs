using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ants
{
    static class PathFinding
    {
        static int Limit = 1000;
        public static Location[] Directions = new[]
            {
                new Location { Y = -1 },
                new Location { X = -1 },
                new Location { Y = 1 },
                new Location { X = 1 }
            };
        public static Location[] Dir8 = new[]
            {
                new Location { X = 0, Y = 1 },
                new Location { X = 0, Y =-1 },
                new Location { X = 1, Y = 1 },
                new Location { X = 1, Y =-1 },
                new Location { X = 1, Y = 0 },
                new Location { X =-1, Y = 1 },
                new Location { X =-1, Y =-1 },
                new Location { X =-1, Y = 0 },

                new Location { X = 0, Y = 2 },
                new Location { X = 0, Y =-2 },
                new Location { X = 2, Y = 0 },
                new Location { X =-2, Y = 0 },

                new Location { X = 1, Y = 2 },
                new Location { X =-1, Y = 2 },
                new Location { X = 1, Y =-2 },
                new Location { X =-1, Y =-2 },
                new Location { X = 2, Y = 1 },
                new Location { X = 2, Y =-1 },
                new Location { X =-2, Y = 1 },
                new Location { X =-2, Y =-1 },

                new Location { X = 0, Y = 3 },
                new Location { X = 0, Y =-3 },
                new Location { X = 3, Y = 0 },
                new Location { X =-3, Y = 0 },
            };

        public static void FillArray(Location loc, int[,] map)
        {
            for (int x = 0; x < GameState.Instance.Width; x++)
                for (int y = 0; y < GameState.Instance.Height; y++)
                    map[x, y] = -1;
            map[loc.X, loc.Y] = 0;
            var lastStep = new List<Location> { loc };
            var stepForward = new List<Location> { loc };
            int currentStep = 0;
            while (stepForward.Count > 0 && currentStep < Limit)
            {
                currentStep++;
                lastStep.Clear();
                lastStep.AddRange(stepForward);
                stepForward.Clear();
                foreach (var point in lastStep)
                    foreach (var dLoc in Directions)
                    {
                        var newLoc = point + dLoc;
                        if (map[newLoc.X, newLoc.Y] == -1 && GameState.Instance.Map[newLoc.X, newLoc.Y] != Tile.Water)
                        {
                            stepForward.Add(newLoc);
                            map[newLoc.X, newLoc.Y] = currentStep;
                        }
                    }
            }
        }

        public static void FillArray(Location loc, int[,] map, Location destLoc)
        {
            for (int x = 0; x < GameState.Instance.Width; x++)
                for (int y = 0; y < GameState.Instance.Height; y++)
                    map[x, y] = -1;
            map[loc.X, loc.Y] = 0;
            var lastStep = new List<Location> { loc };
            var stepForward = new List<Location> { loc };
            int currentStep = 0;
            while (stepForward.Count > 0 && currentStep < Limit)
            {
                currentStep++;
                lastStep.Clear();
                lastStep.AddRange(stepForward);
                stepForward.Clear();
                foreach (var point in lastStep)
                    foreach (var dLoc in Directions)
                    {
                        var newLoc = point + dLoc;
                        if (map[newLoc.X, newLoc.Y] == -1 && GameState.Instance.Map[newLoc.X, newLoc.Y] != Tile.Water)
                        {
                            stepForward.Add(newLoc);
                            map[newLoc.X, newLoc.Y] = currentStep;
                        }
                    }
                if (map[destLoc.X, destLoc.Y] != -1)
                    break;
            }
        }

        public static void FillStochastic(Location loc, int[,] dMap, int[,] sMap)
        {
            int width = GameState.Instance.Width;
            int height = GameState.Instance.Height;

            var sortedPoints = new List<List<Location>>(100);

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    int distance = dMap[x, y];
                    if (distance < 0)
                        continue;
                    while (distance >= sortedPoints.Count)
                        sortedPoints.Add(new List<Location>(50));
                    sortedPoints[distance].Add(new Location { X = x, Y = y });
                }

            bool[] dirs = new bool[4];

            for (int i = sortedPoints.Count - 1; i >= 0; i--)
            {
                for (int j = 0; j < sortedPoints[i].Count; j++)
                {
                    var point = sortedPoints[i][j];
                    int xn;
                    int yn;
                    int total = 1;
                    for (int dir = 0; dir < 4; dir++)
                    {
                        xn = point.X + Directions[dir].X;
                        yn = point.Y + Directions[dir].Y;

                        if (xn >= width)
                            xn -= width;
                        if (xn < 0)
                            xn += width;
                        if (yn >= height)
                            yn -= height;
                        if (yn < 0)
                            yn += height;

                        if (dMap[xn, yn] > dMap[point.X, point.Y])
                        {
                            dirs[dir] = true;
                            total += sMap[xn, yn];
                        }
                        else
                            dirs[dir] = false;
                    }

                    for (int dir1 = 0, dir2 = 1; dir1 < 4; dir1++, dir2++)
                    {
                        if (dir2 == 4)
                            dir2 = 0;
                        if (dirs[dir1] && dirs[dir2])
                        {
                            xn = point.X + Directions[dir1].X + Directions[dir2].X;
                            yn = point.Y + Directions[dir1].Y + Directions[dir2].Y;

                            if (xn >= width)
                                xn -= width;
                            if (xn < 0)
                                xn += width;
                            if (yn >= height)
                                yn -= height;
                            if (yn < 0)
                                yn += height;

                            if (dMap[xn, yn] >= 0)
                                total -= sMap[xn, yn];
                        }
                    }
                    sMap[point.X, point.Y] = total;
                }
            }
        }

        public static int GetTotalLeafCount(Location loc, Hill hill)
        {
            int width = GameState.Instance.Width;
            int height = GameState.Instance.Height;
            var map = GameState.Instance.Map;

            bool[,] leafs = new bool[width, height];

            var lastStep = new List<Location>();
            var stepForward = new List<Location> { loc };
            leafs[loc.X, loc.Y] = true;
            while (stepForward.Count > 0)
            {
                lastStep.Clear();
                lastStep.AddRange(stepForward);
                stepForward.Clear();

                for (int i = 0; i < lastStep.Count; i++)
                {
                    for (int dir = 0; dir < 4; dir++)
                    {
                        var newLoc = lastStep[i] + Directions[dir];
                        //if (map[newLoc.X, newLoc.Y] != Tile.Unseen)
                            if (hill.DistanceMap[newLoc.X, newLoc.Y] > hill.DistanceMap[lastStep[i].X, lastStep[i].Y])
                                if (!leafs[newLoc.X, newLoc.Y])
                                {
                                    stepForward.Add(newLoc);
                                    leafs[newLoc.X, newLoc.Y] = true;
                                }
                    }
                }
            }

            int count = 0;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (leafs[x, y])
                        count++;

            return count;
        }
    }
}