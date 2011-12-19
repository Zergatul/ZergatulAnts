using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ants
{
    public partial class Location : IEquatable<Location>, IComparable<Location>, ICloneable
    {
        public int X;
        public int Y;

        public Location()
        {
        }

        public Location(Location loc)
        {
            this.X = loc.X;
            this.Y = loc.Y;
        }

        public void Add(Location loc)
        {
            X += loc.X;
            Y += loc.Y;
            GameState.Instance.CheckLocation(this);
        }

        public Location SimpleAdd(Location loc)
        {
            return new Location { X = this.X + loc.X, Y = this.Y + loc.Y };
        }

        public int DirectDistanceTo(Location loc)
        {
            return
                Math.Min(Math.Abs(X - loc.X), Math.Abs(GameState.Instance.Width - Math.Abs(X - loc.X))) +
                Math.Min(Math.Abs(Y - loc.Y), Math.Abs(GameState.Instance.Height - Math.Abs(Y - loc.Y)));
        }

        public override int GetHashCode()
        {
            return (Y << 10) + X;
        }

        public bool Equals(Location other)
        {
            return GetHashCode().Equals(other.GetHashCode());
        }

        public int CompareTo(Location other)
        {
            return GetHashCode().CompareTo(other.GetHashCode());
        }

        public static Location operator +(Location loc1, Location loc2)
        {
            var result = new Location { X = loc1.X + loc2.X, Y = loc1.Y + loc2.Y };
            GameState.Instance.CheckLocation(result);
            return result;
        }

        public override string ToString()
        {
            return string.Format("[ x: {0}, y: {1} ]", X, Y);
        }

        public object Clone()
        {
            return (Location)MemberwiseClone();
        }

        public static IEqualityComparer<Location> Comparer = new LocationComparer();

        private class LocationComparer : IEqualityComparer<Location>
        {
            public bool Equals(Location x, Location y)
            {
                return x.X == y.X && x.Y == y.Y;
            }

            public int GetHashCode(Location obj)
            {
                return obj.Y * 1024 + obj.X;
            }
        }

    }
}