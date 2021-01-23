using Microsoft.Xna.Framework;
using System;

namespace Nekres.Mumble_Info_Module
{
    internal static class RectangleExtensions
    {
        public static Rectangle Union(this Rectangle value1, Rectangle value2)
        {
            int x = Math.Min (value1.X, value2.X);
            int y = Math.Min (value1.Y, value2.Y);
            return new Rectangle(x, y,
                                 Math.Max (value1.Right, value2.Right) - x,
                                     Math.Max (value1.Bottom, value2.Bottom) - y);
        }
        public static void Union(ref Rectangle value1, ref Rectangle value2, out Rectangle result)
        {
            result.X = Math.Min (value1.X, value2.X);
            result.Y = Math.Min (value1.Y, value2.Y);
            result.Width = Math.Max (value1.Right, value2.Right) - result.X;
            result.Height = Math.Max (value1.Bottom, value2.Bottom) - result.Y;
        }
    }
}
