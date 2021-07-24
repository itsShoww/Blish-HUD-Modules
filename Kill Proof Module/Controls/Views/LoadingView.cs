using System;
using System.Threading.Tasks;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using Microsoft.Xna.Framework;

namespace Nekres.Kill_Proof_Module.Controls.Views
{
    public class LoadingView : IView
    {
        public LoadingView()
        {
        }

        public async Task<bool> DoLoad(IProgress<string> progress)
        {
            return true;
        }

        public void DoBuild(Panel buildPanel)
        {
            buildPanel.CanScroll = false;

            var pageLoading = new LoadingSpinner
            {
                Parent = buildPanel
            };

            pageLoading.Location = new Point(buildPanel.Size.X / 2 - pageLoading.Size.X / 2, buildPanel.Size.Y / 2 - pageLoading.Size.Y / 2);
        }

        public void DoUnload()
        {
        }

        public event EventHandler<EventArgs> Loaded;
        public event EventHandler<EventArgs> Built;
        public event EventHandler<EventArgs> Unloaded;
    }
}
