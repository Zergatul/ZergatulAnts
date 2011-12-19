using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ants
{
    public partial class Food : Location, IEquatable<Food>, IComparable<Food>, ICloneable
    {
        public bool IsVisible;
        public int[,] DistanceMap;
        public bool NeedRecalcDistanceMap;

        public bool Equals(Food other)
        {
            return base.Equals(other);
        }

        public int CompareTo(Food other)
        {
            return base.CompareTo(other);
        }

        public new object Clone()
        {
            var result = (Food)MemberwiseClone();
            result.DistanceMap = (int[,])DistanceMap.Clone();
            return result;
        }
    }
}