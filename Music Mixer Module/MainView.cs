using Blish_HUD.Controls;
using Microsoft.Xna.Framework.Graphics;

namespace Nekres.Music_Mixer
{
    internal class MainView
    {
        private Texture2D _icon;
        private Panel _homePanel;

        public MainView(Texture2D icon) {
            _icon = icon;

            BuildHomePanel();
        }

        private void BuildHomePanel() {

        }
    }
}
