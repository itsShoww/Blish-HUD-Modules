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
            angle -= 90;

            if (angle < -168.75)
                return Direction.West;
            else if (angle < -146.25)
                return Direction.WestNorthWest;
            else if (angle < -123.75)
                return Direction.NorthWest;
            else if (angle < -101.25)
                return Direction.NorthNorthWest;
            else if (angle < -78.75)
                return Direction.North;
            else if (angle < -56.25)
                return Direction.NorthNorthEast;
            else if (angle < -33.75)
                return Direction.NorthEast;
            else if (angle < -11.25)
                return Direction.EastNorthEast;
            else if (angle < 11.25)
                return Direction.East;
            else if (angle < 33.75)
                return Direction.EastSouthEast;
            else if (angle < 56.25)
                return Direction.SouthEast;
            else if (angle < 78.78)
                return Direction.SouthSouthEast;
            else if (angle < 101.25)
                return Direction.South;
            else if (angle < 123.75)
                return Direction.SouthSouthWest;
            else if (angle < 146.25)
                return Direction.SouthWest;
            else if (angle < 168.75)
                return Direction.WestSouthWest;
            else
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
