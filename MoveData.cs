using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ants
{
    public class MoveData : Location
    {
        public Direction Direction;
        public int Order;
        public bool Critical;
        public bool DeathForFood;
        public bool AttackEnemyHill;

        public MoveData(Location loc, Direction dir)
        {
            this.X = loc.X;
            this.Y = loc.Y;
            this.Direction = dir;
        }
    }
}