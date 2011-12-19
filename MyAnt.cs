using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ants
{
    public partial class MyAnt : Location, IEquatable<MyAnt>, IComparable<MyAnt>, ICloneable
    {
        public bool Move;
        public Direction Direction;
        public bool[] RightDirections;
        public int[] FightDirections;
        public Food Food;
        public int FoodDistance;
        public Hill EnemyHill;
        public int EnemyHillDistance;
        public Explore[] Exploration;
        public bool Live;
        public Location PointFromHill;
        public int[,] SpecificPath;
        public Hill Home;
        public bool Processed;
        public Hill Defend;
        public int DefendDistance;
        public bool GoToEnemyHill;
        public bool[] EnemyHillDirections;
        public bool AttackEnemyHill;

        public MyAnt()
        {
            RightDirections = new bool[4];
            FightDirections = new int[4];
            Exploration = new Explore[4];
            for (int i = 0; i < 4; i++)
                Exploration[i] = new Explore();
            EnemyHillDirections = new bool[4];
        }

        public MyAnt(Location loc)
        {
            X = loc.X;
            Y = loc.Y;
        }

        public void SetLocation(Location loc)
        {
            X = loc.X;
            Y = loc.Y;
        }

        public void ClearDirections()
        {
            RightDirections[0] = true;
            RightDirections[1] = true;
            RightDirections[2] = true;
            RightDirections[3] = true;
        }

        public void FillNextTurnPreview()
        {
            var newLoc = this + PathFinding.Directions[(int)Direction];
            GameState.Instance.NextTurnPreview[newLoc.X, newLoc.Y] = true;
        }

        public bool Equals(MyAnt other)
        {
            return base.Equals(other);
        }

        public int CompareTo(MyAnt other)
        {
            return base.CompareTo(other);
        }

        public new object Clone()
        {
            var result = (MyAnt)MemberwiseClone();
            result.RightDirections = (bool[])RightDirections.Clone();
            if (Food != null)
                result.Food = (Food)Food.Clone();
            if (EnemyHill != null)
                result.EnemyHill = (Hill)EnemyHill.Clone();
            result.Exploration = Exploration.Select(e => (Explore)e.Clone()).ToArray();
            return result;
        }
    }
}