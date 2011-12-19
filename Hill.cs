using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ants
{
    public partial class Hill : Location, IComparable<Hill>, IEquatable<Hill>, ICloneable
    {
        public int Team;
        public bool IsVisible;
        public int[,] DistanceMap;
        public int[,] StochasticMap;
        public bool NeedRecalcDistanceMap;
        public bool Live;

        public int CompareTo(Hill other)
        {
            return GetHashCode().CompareTo(other.GetHashCode());
        }

        public bool Equals(Hill other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        public override int GetHashCode()
        {
            return (Team << 20) + (Y << 10) + X;
        }

        public new object Clone()
        {
            var result = (Hill)MemberwiseClone();
            result.DistanceMap = (int[,])DistanceMap.Clone();
            return result;
        }
    }
}