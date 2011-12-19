using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ants
{
    public enum Direction
    {
        North, West, South, East
    }

    public static class DirectionExtender
    {
        public static char ToChar(this Direction dir)
        {
            if (dir == Direction.East)
                return 'e';
            if (dir == Direction.North)
                return 'n';
            if (dir == Direction.South)
                return 's';
            if (dir == Direction.West)
                return 'w';
            return default(char);
        }

        public static Direction Opposide(this Direction dir)
        {
            if (dir == Direction.East)
                return Direction.West;
            if (dir == Direction.West)
                return Direction.East;
            if (dir == Direction.South)
                return Direction.North;
            if (dir == Direction.North)
                return Direction.South;
            throw new InvalidOperationException("Invalid direction.");
        }

        public static Direction FromChar(char ch)
        {
            for (int i = 0; i < 4; i++)
                if (((Direction)i).ToChar() == ch)
                    return (Direction)i;
            throw new InvalidCastException("Could not convert from char to Direction.");
        }
    }
}