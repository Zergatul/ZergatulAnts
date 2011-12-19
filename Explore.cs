using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ants
{
    public partial class Explore : ICloneable
    {
        public int NewCellCount;
        public int VisibleCellCount;

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
