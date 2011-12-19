using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ants
{
    public partial class GameState : ICloneable
    {
        static GameState _instance = new GameState();
        public static GameState Instance { get { return _instance; } }
        public int Width;
        public int Height;
        public int TurnTime;
        public int LoadTime;
        public int ViewRadius2;
        public int AttackRadius2;
        public int SpawnRadius2;
        public int Players;
        public int TurnNumber;
        public Tile[,] Map;
        public bool[,] VisibleMap;
        public bool[,] NextTurnPreview;
        int[,] attackZone;
        int[,] attackZoneSimple;
        public List<Food> Foods;
        public List<MyAnt> MyAnts;
        public List<Location> MyAntPositions;
        public List<Ant> EnemyAnts;
        public List<Hill> Hills;
        public List<Hill> EnemyHills;
        List<Location> NewWater;
        public Location[] FullVision;
        public Location[] MoveVision;
        public Location[] FinalAttack;
        Location[] AttackRangeP1;
        List<int> PossibleDy;
        int SymmetryDy;
        bool[] PartInUse;
        DateTime turnStart;
        int queryCount;
        int ms;
        public int RemainMs
        {
            get
            {
                queryCount++;
                if (queryCount == 25)
                {
                    queryCount = 0;
                    ms = TurnTime - Convert.ToInt32((DateTime.Now - turnStart).TotalMilliseconds);
                }
                return ms;
            }
        }

        #region Main logic

        private GameState()
        {
        }

        public void Init(int width, int height, int turntime, int loadtime, int viewradius2, int attackradius2, int spawnradius2, int players)
        {
            this.Width = width;
            this.Height = height;
            this.TurnTime = turntime;
            this.LoadTime = loadtime;
            this.ViewRadius2 = viewradius2;
            this.AttackRadius2 = attackradius2;
            this.SpawnRadius2 = spawnradius2;
            this.Players = players;
            this.TurnNumber = 0;

            MyAnts = new List<MyAnt>();
            Foods = new List<Food>();
            EnemyAnts = new List<Ant>();
            Hills = new List<Hill>();
            EnemyHills = new List<Hill>();
            NewWater = new List<Location>();
            MyAntPositions = new List<Location>();

            Map = new Tile[Width, Height];
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < height; y++)
                    Map[x, y] = Tile.Unseen;
            VisibleMap = new bool[Width, Height];
            NextTurnPreview = new bool[Width, Height];
            attackZone = new int[Width, Height];
            attackZoneSimple = new int[Width, Height];

            PossibleDy = new List<int>();
            for (int dy = 1; dy < Height; dy++)
                if (dy * Players % Height == 0)
                    PossibleDy.Add(dy);
            SymmetryDy = -1;
            PartInUse = new bool[Players];

            PrepareVisionArrays();
        }

        public void StartNewTurn()
        {
            turnStart = DateTime.Now;
            queryCount = 0;
            ms = TurnTime;
            TurnNumber++;
            EnemyAnts.Clear();
            foreach (var food in Foods)
            {
                food.IsVisible = false;
                food.NeedRecalcDistanceMap = false;
            }
            foreach (var hill in EnemyHills)
                hill.IsVisible = false;
            foreach (var hill in Hills)
                hill.Live = false;
            foreach (var ant in MyAnts)
                ant.Live = false;
            NewWater.Clear();
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (Map[x, y] == Tile.Ant)
                        Map[x, y] = Tile.Land;
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    VisibleMap[x, y] = false;
                    NextTurnPreview[x, y] = false;
                }
        }

        public void EndTurnInput()
        {
            // Water
            CheckSymetryMap();
            // Foods
            for (int i = 0; i < Foods.Count; i++)
                if (!Foods[i].IsVisible && VisibleMap[Foods[i].X, Foods[i].Y])
                {
                    Map[Foods[i].X, Foods[i].Y] = Tile.Land;
                    Foods.RemoveAt(i);
                    i--;
                }
            foreach (var food in Foods)
                foreach (var water in NewWater)
                    if (food.DistanceMap[water.X, water.Y] > 0)
                    {
                        food.NeedRecalcDistanceMap = true;
                        break;
                    }
            foreach (var food in Foods)
                if (food.NeedRecalcDistanceMap)
                    PathFinding.FillArray(food, food.DistanceMap);

            // Enemy hills
            for (int i = 0; i < EnemyHills.Count; i++)
                if (!EnemyHills[i].IsVisible && VisibleMap[EnemyHills[i].X, EnemyHills[i].Y])
                {
                    Map[EnemyHills[i].X, EnemyHills[i].Y] = Tile.Land;
                    EnemyHills.RemoveAt(i);
                    i--;
                }
            foreach (var hill in EnemyHills)
                foreach (var water in NewWater)
                    if (hill.DistanceMap[water.X, water.Y] > 0)
                    {
                        hill.NeedRecalcDistanceMap = true;
                        break;
                    }
            foreach (var hill in EnemyHills)
                if (hill.NeedRecalcDistanceMap)
                    PathFinding.FillArray(hill, hill.DistanceMap);

            // My hills
            for (int i = Hills.Count - 1; i >= 0; i--)
                if (!Hills[i].Live && VisibleMap[Hills[i].X, Hills[i].Y])
                {
                    var oldHill = Hills[i];
                    Hills.RemoveAt(i);
                    foreach (var ant in MyAnts)
                        if (object.ReferenceEquals(ant.Home, oldHill))
                            if (Hills.Count > 0)
                                ant.Home = Hills.Where(x => x.DirectDistanceTo(ant) == Hills.Min(h => h.DirectDistanceTo(ant))).First();
                            else
                                ant.Home = null;
                }
            foreach (var hill in Hills)
                foreach (var water in NewWater)
                    if (hill.DistanceMap[water.X, water.Y] > 0)
                    {
                        hill.NeedRecalcDistanceMap = true;
                        break;
                    }
            foreach (var hill in Hills)
                if (hill.NeedRecalcDistanceMap)
                {
                    PathFinding.FillArray(hill, hill.DistanceMap);
                    for (int x = 0; x < Width; x++)
                        for (int y = 0; y < Height; y++)
                            hill.StochasticMap[x, y] = 0;
                }

            // My ants
            for (int i = MyAnts.Count - 1; i >= 0; i--)
                if (!MyAnts[i].Live)
                    MyAnts.RemoveAt(i);
            for (int i = 0; i < MyAnts.Count; i++)
                if (MyAnts[i].PointFromHill != null)
                    for (int j = 0; j < NewWater.Count; j++)
                        if (MyAnts[i].SpecificPath[NewWater[j].X, NewWater[j].Y] != -1)
                        {
                            PathFinding.FillArray(MyAnts[i].PointFromHill, MyAnts[i].SpecificPath, MyAnts[i]);
                            break;
                        }

            foreach (var ant in MyAnts)
                ant.ClearDirections();
            RefreshDirections();

            EnemyAnts.Sort();
        }

        public void EndTurn()
        {
            foreach (var ant in MyAnts)
            {
                ant.GoToEnemyHill = false;
                if (ant.Move && ant.Processed && ant.AttackEnemyHill)
                    if (ant.EnemyHillDirections[(int)ant.Direction])
                        ant.GoToEnemyHill = true;
            }
            foreach (var ant in MyAnts)
                if (ant.Move && ant.Processed)
                {
                    Shell.CommitStep(ant, ant.Direction);
                    MoveLocation(ant, ant.Direction);
                }
            MyAnts.Sort();
        }

        public object Clone()
        {
            var result = (GameState)MemberwiseClone();
            result.Map = (Tile[,])Map.Clone();
            result.Foods = new List<Food>(Foods.Select(f => (Food)f.Clone()));
            result.MyAnts = new List<MyAnt>(MyAnts.Select(a => (MyAnt)a.Clone()));
            result.MyAntPositions = new List<Location>(MyAntPositions.Select(ap => (Location)ap.Clone()));
            result.EnemyAnts = new List<Ant>(EnemyAnts.Select(a => (Ant)a.Clone()));
            result.Hills = new List<Hill>(Hills.Select(h => (Hill)h.Clone()));
            result.EnemyHills = new List<Hill>(EnemyHills.Select(h => (Hill)h.Clone()));
            return result;
        }

        #endregion

        #region Updating world methods

        public void AddAnt(int col, int row, int team)
        {
            if (team == 0)
            {
                var ant = new MyAnt { X = col, Y = row };
                var index = MyAnts.BinarySearch(ant);
                if (index < 0)
                {
                    MyAnts.Insert(~index, ant);
                    NewAnt(ant);
                    ant.Live = true;
                    ant.Home = Hills.Where(x => x.DirectDistanceTo(ant) == Hills.Min(h => h.DirectDistanceTo(ant))).First();
                }
                else
                {
                    ApplyAntVision(ant);
                    MyAnts[index].Live = true;
                }
            }
            else
            {
                var ant = new Ant { X = col, Y = row, Team = team };
                EnemyAnts.Add(ant);
            }
            Map[col, row] = Tile.Ant;
        }

        public void AddFood(int col, int row)
        {
            var food = new Food { X = col, Y = row };
            var index = Foods.BinarySearch(food);
            if (index < 0)
            {
                Foods.Insert(~index, food);
                index = ~index;
                food.DistanceMap = new int[Width, Height];
                food.NeedRecalcDistanceMap = true;
            }
            Foods[index].IsVisible = true;
            
            Map[col, row] = Tile.Food;
        }

        public void RemoveFood(int col, int row)
        {
            if (Map[col, row] == Tile.Food)
                Map[col, row] = Tile.Land;
            var index = Foods.BinarySearch(new Food { X = col, Y = row });
            if (index >= 0)
                Foods.RemoveAt(index);
        }

        public void AddWater(int col, int row)
        {
            if (Map[col, row] != Tile.Water)
            {
                Map[col, row] = Tile.Water;
                NewWater.Add(new Location { X = col, Y = row });
            }
        }

        public void AddDeadAnt(int col, int row)
        {
            // TODO
            /*var ant = new MyAnt { X = col, Y = row };
            var index = MyAnts.BinarySearch(ant);
            if (index >= 0)
                MyAnts.RemoveAt(index);*/
        }

        public void AddHill(int col, int row, int team)
        {
            var hill = new Hill { X = col, Y = row, Team = team };
            if (team != 0)
            {
                var index = EnemyHills.BinarySearch(hill);
                if (index < 0)
                {
                    EnemyHills.Insert(~index, hill);
                    hill.DistanceMap = new int[Width, Height];
                    hill.NeedRecalcDistanceMap = true;
                    hill.IsVisible = true;
                }
                else
                    EnemyHills[index].IsVisible = true;
            }
            else
                if (!Hills.Contains(hill))
                {
                    hill.DistanceMap = new int[Width, Height];
                    hill.StochasticMap = new int[Width, Height];
                    hill.NeedRecalcDistanceMap = true;
                    hill.Live = true;
                    Hills.Add(hill);
                }
                else
                    Hills.Where(h => h.Equals(hill)).First().Live = true;
        }

        #endregion

        #region Fog of war logic

        void NewAnt(MyAnt ant)
        {
            FillVisionMap(ant);
            foreach (var loc in FullVision)
            {
                var point = loc + ant;
                var tile = GetTile(point);
                if (tile == Tile.Unseen)
                    SetTile(point, Tile.Land);
            }
        }

        void ApplyAntVision(MyAnt ant)
        {
            FillVisionMap(ant);
            foreach (var loc in MoveVision)
            {
                var point = loc + ant;
                var tile = GetTile(point);
                if (tile == Tile.Unseen)
                    SetTile(point, Tile.Land);
            }
        }

        void FillVisionMap(Location ant)
        {
            foreach (var loc in FullVision)
            {
                var point = loc + ant;
                VisibleMap[point.X, point.Y] = true;
            }
        }

        void PrepareVisionArrays()
        {
            var viewRadius = Convert.ToInt32(Math.Floor(Math.Sqrt(ViewRadius2)));
            var list = new List<Location>();
            for (int x = -viewRadius; x <= viewRadius; x++)
                for (int y = -viewRadius; y <= viewRadius; y++)
                    if (x * x + y * y <= ViewRadius2)
                        list.Add(new Location { X = x, Y = y });
            FullVision = list.ToArray();

            list = new List<Location>();
            foreach (var loc in FullVision)
                foreach (var dLoc in PathFinding.Directions)
                {
                    var newLoc = loc.SimpleAdd(dLoc);
                    if (!FullVision.Contains(newLoc, Location.Comparer))
                    {
                        list.Add(loc);
                        break;
                    }
                }
            MoveVision = list.Distinct(Location.Comparer).ToArray();

            list = new List<Location>();
            for (int x = -AttackRadius2; x <= AttackRadius2; x++)
                for (int y = -AttackRadius2; y <= AttackRadius2; y++)
                    if (x * x + y * y <= AttackRadius2)
                    {
                        var loc = new Location { X = x, Y = y };
                        list.Add(loc);
                        for (int dir = 0; dir < 4; dir++)
                            list.Add(loc.SimpleAdd(PathFinding.Directions[dir]));
                    }
            AttackRangeP1 = list.Distinct(Location.Comparer).ToArray();

            list = new List<Location>();
            for (int x = -4; x <= 4; x++)
                for (int y = -4; y <= 4; y++)
                    if (x * x + y * y <= 16 && x * x + y * y >= 4)
                        list.Add(new Location { X = x, Y = y });
            FinalAttack = list.OrderBy(loc => loc.X * loc.X + loc.Y * loc.Y).ToArray();
        }

        #endregion

        #region Geometry

        public void MoveLocation(Location loc, Direction dir)
        {
            loc.Add(PathFinding.Directions[(int)dir]);
        }

        public void CheckLocation(Location loc)
        {
            if (loc.X < 0)
                loc.X += Width;
            if (loc.Y < 0)
                loc.Y += Height;
            if (loc.X >= Width)
                loc.X -= Width;
            if (loc.Y >= Height)
                loc.Y -= Height;
        }

        public void SetTile(Location loc, Tile tile)
        {
            Map[loc.X, loc.Y] = tile;
        }

        public Tile GetTile(Location loc)
        {
            return Map[loc.X, loc.Y];
        }

        public void RefreshDirections()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    attackZone[x, y] = -1;
                    attackZoneSimple[x, y] = 0;
                }
            for (int i = 0; i < EnemyAnts.Count; i++)
                for (int j = 0; j < AttackRangeP1.Length; j++)
                {
                    var loc = EnemyAnts[i] + AttackRangeP1[j];
                    attackZoneSimple[loc.X, loc.Y]++;
                    if (attackZoneSimple[loc.X, loc.Y] > 4)
                        attackZoneSimple[loc.X, loc.Y] = 4;
                }

            foreach (var ant in MyAnts)
                for (int i = 0; i < 4; i++)
                {
                    var newLoc = ant + PathFinding.Directions[i];
                    ant.FightDirections[i] = GetAttackLoc(newLoc);
                    if (ant.RightDirections[i])
                    {
                        if (Map[newLoc.X, newLoc.Y] == Tile.Water || Map[newLoc.X, newLoc.Y] == Tile.Food)
                            ant.RightDirections[i] = false;
                    }
                }
        }

        public int GetAttackLoc(Location loc)
        {
            if (attackZone[loc.X, loc.Y] == -1)
            {
                if (RemainMs < 200)
                    attackZone[loc.X, loc.Y] = attackZoneSimple[loc.X, loc.Y];
                else
                {
                    var map = new bool[9, 9];
                    for (int i = 0; i < AttackRangeP1.Length; i++)
                    {
                        int dx = Math.Sign(AttackRangeP1[i].X);
                        int dy = Math.Sign(AttackRangeP1[i].Y);

                        var newLoc = loc + AttackRangeP1[i];
                        int index = EnemyAnts.BinarySearch(new Ant(newLoc));
                        if (index >= 0)
                        {
                            List<int> order = new List<int> { 0, 1, 2, 3 };
                            order = order.OrderBy(dir =>
                            {
                                var nl = newLoc + PathFinding.Directions[dir];
                                return (nl.X - loc.X) * (nl.X - loc.X) +
                                       (nl.Y - loc.Y) * (nl.Y - loc.Y);
                            }).ToList();
                            #region Process dirs
                            for (int j = 0; j < 4; j++)
                            {
                                if (dx == 1 && order[j] == 1)
                                {
                                    var nl = newLoc + PathFinding.Directions[1];
                                    if (Map[nl.X, nl.Y] != Tile.Water && Map[nl.X, nl.Y] != Tile.Food)
                                        if (!map[4 + AttackRangeP1[i].X - 1, 4 + AttackRangeP1[i].Y])
                                        {
                                            map[4 + AttackRangeP1[i].X - 1, 4 + AttackRangeP1[i].Y] = true;
                                            break;
                                        }
                                }
                                if (dx == -1 && order[j] == 3)
                                {
                                    var nl = newLoc + PathFinding.Directions[3];
                                    if (Map[nl.X, nl.Y] != Tile.Water && Map[nl.X, nl.Y] != Tile.Food)
                                        if (!map[4 + AttackRangeP1[i].X + 1, 4 + AttackRangeP1[i].Y])
                                        {
                                            map[4 + AttackRangeP1[i].X + 1, 4 + AttackRangeP1[i].Y] = true;
                                            break;
                                        }
                                }
                                if (dy == 1 && order[j] == 0)
                                {
                                    var nl = newLoc + PathFinding.Directions[0];
                                    if (Map[nl.X, nl.Y] != Tile.Water && Map[nl.X, nl.Y] != Tile.Food)
                                        if (!map[4 + AttackRangeP1[i].X, 4 + AttackRangeP1[i].Y - 1])
                                        {
                                            map[4 + AttackRangeP1[i].X, 4 + AttackRangeP1[i].Y - 1] = true;
                                            break;
                                        }
                                }
                                if (dy == -1 && order[j] == 2)
                                {
                                    var nl = newLoc + PathFinding.Directions[2];
                                    if (Map[nl.X, nl.Y] != Tile.Water && Map[nl.X, nl.Y] != Tile.Food)
                                        if (!map[4 + AttackRangeP1[i].X, 4 + AttackRangeP1[i].Y + 1])
                                        {
                                            map[4 + AttackRangeP1[i].X, 4 + AttackRangeP1[i].Y + 1] = true;
                                            break;
                                        }
                                }
                            }
                            #endregion
                        }
                    }
                    int result = 0;
                    for (int x = 0; x < 9; x++)
                        for (int y = 0; y < 9; y++)
                            if (map[x, y])
                                if ((x - 4) * (x - 4) + (y - 4) * (y - 4) <= AttackRadius2)
                                    result++;
                    attackZone[loc.X, loc.Y] = result;
                }
            }
            return attackZone[loc.X, loc.Y];
        }

        void CheckSymetryMap()
        {
            /*if (SymmetryDy == -1)
            {
                int dx = Width / Players;
                for (int i = 0; i < Players; i++)
                    if (!PartInUse[i])
                    {
                        for (int x = i * dx; x < (i + 1) * dx; x++)
                            if (!PartInUse[i])
                                for (int y = 0; y < Height; y++)
                                    if (Map[x, y] != Tile.Unseen)
                                        PartInUse[i] = true;
                    }
                for (int i = 0; i < PossibleDy.Count; i++)
                {
                    bool ok = true;
                    for (int xd1 = 0; xd1 < Players - 1; xd1++)
                        if (PartInUse[xd1] && ok)
                            for (int xd2 = xd1 + 1; xd2 < Players; xd2++)
                                if (PartInUse[xd2])
                                    if (!CompareMapParts(xd1, xd2, dx, PossibleDy[i]))
                                    {
                                        ok = false;
                                        PossibleDy.RemoveAt(i);
                                        i--;
                                        break;
                                    }
                }
            }*/
        }

        bool CompareMapParts(int p1, int p2, int dx, int dy)
        {
            /*for (int x = p1 * dx; x < (p1 + 1) * dx; x++)
                for (int y = */
            return true;
        }

        #endregion

        #region Position calculation

        public void SaveMyAntsState()
        {
            MyAntPositions.Clear();
            foreach (var ant in MyAnts)
                MyAntPositions.Add(new Location(ant));
        }

        public void RestoreMyAntsState()
        {
            for (int i = 0; i < MyAnts.Count; i++)
                MyAnts[i].SetLocation(MyAntPositions[i]);
        }

        public double GetPosition()
        {
            return FoodNearness();
        }

        double FoodNearness()
        {
            var foodAntCorrespondence = new int[Foods.Count, MyAnts.Count];
            for (int antIndex = 0; antIndex < MyAnts.Count; antIndex++)
                for (int foodIndex = 0; foodIndex < Foods.Count; foodIndex++)
                    foodAntCorrespondence[foodIndex, antIndex] =
                        Foods[foodIndex].DistanceMap[MyAnts[antIndex].X, MyAnts[antIndex].Y];
            var usedFoods = new bool[Foods.Count];
            int unusedFoodsCount = Foods.Count;
            var usedAnts = new bool[MyAnts.Count];
            int unusedAntsCount = MyAnts.Count;

            double sum = 0;

            while (unusedAntsCount > 0 && unusedFoodsCount > 0)
            {
                int antIndex = -1;
                int foodIndex = -1;
                int min = int.MaxValue;
                for (int aIndex = 0; aIndex < MyAnts.Count; aIndex++)
                    if (!usedAnts[aIndex])
                        for (int fIndex = 0; fIndex < Foods.Count; fIndex++)
                            if (!usedFoods[fIndex])
                                if (foodAntCorrespondence[fIndex, aIndex] != -1 && foodAntCorrespondence[fIndex, aIndex] < min)
                                {
                                    antIndex = aIndex;
                                    foodIndex = fIndex;
                                    min = foodAntCorrespondence[fIndex, aIndex];
                                }
                usedAnts[antIndex] = true;
                usedFoods[foodIndex] = true;
                unusedAntsCount--;
                unusedFoodsCount--;

                sum += min;
            }

            return 1.0 / sum;
        }

        #endregion
    }
}