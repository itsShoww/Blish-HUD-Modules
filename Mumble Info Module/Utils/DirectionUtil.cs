using Gw2Sharp.Models;
using System;

namespace Nekres.Mumble_Info_Module
{
    public static class DirectionUtil
    {
        public static Direction IsFacing(Coordinates3 coordinates) {
            return GetDirectionFromAngle(Math.Atan2(coordinates.X, coordinates.Y) * 180 / Math.PI);
        }
        public static Direction GetDirectionFromAngle(double angle)
        {
            if (angle < -168.75)
                return Direction.South;
            if (angle < -146.25)
                return Direction.SouthSouthWest;
            if (angle < -123.75)
                return Direction.SouthWest;
            if (angle < -101.25)
                return Direction.WestSouthWest;
            if (angle < -78.75)
                return Direction.West;
            if (angle < -56.25)
                return Direction.WestNorthWest;
            if (angle < -33.75)
                return Direction.NorthWest;
            if (angle < -11.25)
                return Direction.NorthNorthWest;
            if (angle < 11.25)
                return Direction.North;
            if (angle < 33.75)
                return Direction.NorthNorthEast;
            if (angle < 56.25)
                return Direction.NorthEast;
            if (angle < 78.75)
                return Direction.EastNorthEast;
            if (angle < 101.25)
                return Direction.East;
            if (angle < 123.75)
                return Direction.EastSouthEast;
            if (angle < 146.25)
                return Direction.SouthEast;
            if (angle < 168.75)
                return Direction.SouthSouthEast;
            if (angle < 180)
                return Direction.South;
            return Direction.West;
        }

        public enum Direction
        {
            North,
            NorthNorthEast,
            NorthEast,
            EastNorthEast,
            East,
            EastSouthEast,
            SouthEast,
            SouthSouthEast,
            South,
            SouthSouthWest,
            SouthWest,
            WestSouthWest,
            West,
            WestNorthWest,
            NorthWest,
            NorthNorthWest
        }
    }
}
