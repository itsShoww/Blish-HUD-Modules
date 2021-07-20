using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD.Modules.Managers;
using Nekres.Notes_Module.Effects;

namespace Nekres.Notes_Module.Controls
{
public class Book : BasicWindow
    {
        // TODO: Maybe add gw2's book sounds (opens, turn page)
        // TODO: Title background texture from the original.
        private readonly BitmapFont TitleFont = GameService.Content.GetFont(ContentService.FontFace.Menomonia, ContentService.FontSize.Size32, ContentService.FontStyle.Regular);
        private readonly Texture2D TurnPageSprite;

        private static int RIGHT_PADDING = 150;
        private static int TOP_PADDING = 120;
        private static int SHEET_OFFSET = 20;

        private bool MouseOverTurnPageLeft;
        private bool MouseOverTurnPageRight;

        private List<Page> Pages = new List<Page>();
        /// <summary>
        /// The currently open page of this book.
        /// </summary>
        public Page CurrentPage { get; private set; }
        /// <summary>
        /// Creates a panel that should act as Parent for Page controls to create a book UI.
        /// </summary>
        /// <param name="scale">Scale size to keep the sheet's aspect ratio.</param>
        public Book(ContentsManager contentsManager, int scale = 1) : base(
            contentsManager.GetTexture("1909321.png").Duplicate().GetRegion(0, 20, 680, 800),
            new Vector2(30, 15),
            new Rectangle(0, 25, 625, 800),
            new Thickness(0, 0, 0, 26),
            45,
            true)
        {
            Title = "";
            TurnPageSprite = TurnPageSprite ?? GameService.Content.GetTexture("1909317");
            this.SpriteBatchParameters = new SpriteBatchParameters(SpriteSortMode.Immediate,
                BlendState.Additive,
                null,
                null,
                null,
                AlphaMaskEffect.SharedInstance);
            OnResized(null);
        }
        protected override void OnResized(ResizedEventArgs e)
        {
            ContentRegion = new Rectangle(0, 40, this.Width, this.Height - 40);
            if (Pages == null || Pages.Count <= 0) return;

            foreach (Page page in this.Pages)
            {
                if (page == null) continue;
                page.Size = PointExtensions.ResizeKeepAspect(page.Size, ContentRegion.Width - RIGHT_PADDING, ContentRegion.Height - TOP_PADDING, true);
                page.Location = new Point((ContentRegion.Width - page.Size.X) / 2, (ContentRegion.Height - page.Size.Y) / 2 + SHEET_OFFSET);
            }

            base.OnResized(e);
        }
        protected override void OnChildAdded(ChildChangedEventArgs e)
        {
            if (e.ChangedChild is Page && !Pages.Any(x => x.Equals((Page)e.ChangedChild)))
            {
                Page page = (Page)e.ChangedChild;
                page.Size = PointExtensions.ResizeKeepAspect(page.Size, ContentRegion.Width - RIGHT_PADDING, ContentRegion.Height - TOP_PADDING, true);
                page.Location = new Point((ContentRegion.Width - page.Size.X) / 2 - 20, (ContentRegion.Height - page.Size.Y) / 2 + SHEET_OFFSET);
                page.PageNumber = Pages.Count + 1;
                Pages.Add(page);

                if (Pages.Count == 1) CurrentPage = page;
                if (page != CurrentPage) page.Hide();
            }

            base.OnChildAdded(e);
        }
        protected override void OnMouseMoved(MouseEventArgs e)
        {
            var relPos = this.RelativeMousePosition;

            Rectangle leftButtonBounds = new Rectangle(20, ContentRegion.Height / 2 + SHEET_OFFSET + 20, TurnPageSprite.Bounds.Width, TurnPageSprite.Bounds.Height);
            Rectangle rightButtonBounds = new Rectangle(ContentRegion.Width - TurnPageSprite.Bounds.Width - 70, ContentRegion.Height / 2 + SHEET_OFFSET + 20, TurnPageSprite.Bounds.Width, TurnPageSprite.Bounds.Height);

            this.MouseOverTurnPageLeft = leftButtonBounds.Contains(relPos);
            this.MouseOverTurnPageRight = rightButtonBounds.Contains(relPos);

            base.OnMouseMoved(e);
        }
        protected override void OnLeftMouseButtonPressed(MouseEventArgs e)
        {
            if (this.MouseOverTurnPageLeft)
            {
                TurnPage(Pages.IndexOf(CurrentPage) - 1);
            }
            else if (this.MouseOverTurnPageRight)
            {
                TurnPage(Pages.IndexOf(CurrentPage) + 1);
            }

            base.OnLeftMouseButtonPressed(e);
        }
        private void TurnPage(int index)
        {
            if (index < Pages.Count && index >= 0)
            {
                CurrentPage = Pages[index];

                foreach (Page other in Pages)
                {
                    other.Visible = other == CurrentPage;
                }
            }
        }
        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            base.PaintBeforeChildren(spriteBatch, bounds);

            Point titleSize = (Point)TitleFont.MeasureString(this.Title);
            Rectangle titleDest = new Rectangle((ContentRegion.Width - titleSize.X) / 2 - 20, ContentRegion.Top + (TOP_PADDING - titleSize.Y) / 2, titleSize.X, titleSize.Y);
            spriteBatch.DrawStringOnCtrl(this, Title, TitleFont, titleDest, Color.White, false, HorizontalAlignment.Left, VerticalAlignment.Top);

            Rectangle leftButtonBounds = new Rectangle(20, ContentRegion.Height / 2 + SHEET_OFFSET + 20, TurnPageSprite.Bounds.Width, TurnPageSprite.Bounds.Height);
            Rectangle rightButtonBounds = new Rectangle(ContentRegion.Width - TurnPageSprite.Bounds.Width - 70, ContentRegion.Height / 2 + SHEET_OFFSET + 20, TurnPageSprite.Bounds.Width, TurnPageSprite.Bounds.Height);

            if (!MouseOverTurnPageLeft)
            {
                spriteBatch.DrawOnCtrl(this, TurnPageSprite, leftButtonBounds, TurnPageSprite.Bounds, new Color(155, 155, 155, 150), 0, Vector2.Zero, SpriteEffects.FlipHorizontally);
            }
            else
            {
                spriteBatch.DrawOnCtrl(this, TurnPageSprite, leftButtonBounds, TurnPageSprite.Bounds, Color.White, 0, Vector2.Zero, SpriteEffects.FlipHorizontally);
            }

            if (!MouseOverTurnPageRight)
            {
                spriteBatch.DrawOnCtrl(this, TurnPageSprite, rightButtonBounds, TurnPageSprite.Bounds, new Color(155, 155, 155, 155));
            }
            else
            {
                spriteBatch.DrawOnCtrl(this, TurnPageSprite, rightButtonBounds, TurnPageSprite.Bounds, Color.White);
            }
        }
    }
}
