using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ants
{
    public partial class Ant : Location, IComparable<Ant>, IEquatable<Ant>, ICloneable
    {
        public int Team;

        public Ant()
        { }

        public Ant(Location loc)
        {
            this.X = loc.X;
            this.Y = loc.Y;
        }

        public int CompareTo(Ant other)
        {
            return GetHashCode().CompareTo(other.GetHashCode());
        }

        public bool Equals(Ant other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        public override int GetHashCode()
        {
            return (Y << 10) + X;
        }

        public new object Clone()
        {
            return (Ant)MemberwiseClone();
        }
    }
}