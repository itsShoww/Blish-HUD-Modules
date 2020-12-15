using Microsoft.Xna.Framework;
namespace Nekres.Mumble_Info_Module
{
    internal static class PointExtension
    {
        public static bool IsInBounds(this Point p, Rectangle bounds) {
            return p.X < bounds.X + bounds.Width &&
                   p.X > bounds.X &&
                   p.Y < bounds.Y + bounds.Height &&
                   p.Y > bounds.Y;
        }
    }
}
