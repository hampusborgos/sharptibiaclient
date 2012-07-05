using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    class SkillPanel : UIVirtualFrame
    {
        protected ClientViewport Viewport;

        public SkillPanel()
        {
            Name = "Skills";

            Bounds.Width = 176;
            Bounds.Height = 200;
        }

        public void OnNewState(ClientState NewState)
        {
            Viewport = NewState.Viewport;
        }

        public override void Update(GameTime time)
        {
            base.Update(time);
        }

        private string FormatNumber(int n)
        {
            string s = n.ToString();
            int splits = s.Length / 3;
            if (splits == 0)
                return s;
            string o = "";
            while (true)
            {
                if (s.Length <= 3)
                {
                    o = s + o;
                    break;
                }

                string triplet = s.Substring(s.Length - 3, 3);
                s = s.Substring(0, s.Length - 3);
                o = "," + triplet + o;
            }
            return o;
        }

        private void DrawSkill(SpriteBatch Batch, int y, string name, int value)
        {
            int x = 5;
            y += 5;

            Vector2 left = new Vector2(ScreenClientBounds.X + x, ScreenClientBounds.Top + y);
            Batch.DrawString(Context.StandardFont, name, left, Color.LightGray, 0.0f, new Vector2(0.0f, 0.0f), 1.0f, SpriteEffects.None, 0.5f);
            
            string N = FormatNumber(value);
            Vector2 size = Context.StandardFont.MeasureString(N);
            Vector2 right = new Vector2(ScreenClientBounds.Right - size.X - x, ScreenClientBounds.Top + y);
            Batch.DrawString(Context.StandardFont, FormatNumber(value), right, Color.LightGray, 0.0f, new Vector2(0.0f, 0.0f), 1.0f, SpriteEffects.None, 0.5f);
        }

        protected override void DrawContent(SpriteBatch Batch)
        {
            if (Viewport != null)
            {
                try
                {
                    DrawSkill(Batch, 0, "Experience", Viewport.Player.Experience);
                    DrawSkill(Batch, 16, "Level", Viewport.Player.Skill["Level"]);
                    DrawSkill(Batch, 32, "Hitpoints", Viewport.Player.Health);
                    DrawSkill(Batch, 48, "Mana", Viewport.Player.Mana);
                    DrawSkill(Batch, 64, "Capacity", Viewport.Player.Capacity);
                    DrawSkill(Batch, 80, "Magic Level", Viewport.Player.Skill["MagicLevel"]);

                    DrawSkill(Batch, 112, "Fist Fighting", Viewport.Player.Skill["Fist"]);
                    DrawSkill(Batch, 128, "Club Fighting", Viewport.Player.Skill["Club"]);
                    DrawSkill(Batch, 144, "Sword Fighting", Viewport.Player.Skill["Sword"]);
                    DrawSkill(Batch, 160, "Axe Fighting", Viewport.Player.Skill["Axe"]);
                    DrawSkill(Batch, 176, "Distance Fighting", Viewport.Player.Skill["Dist"]);
                    DrawSkill(Batch, 192, "Shielding", Viewport.Player.Skill["Shield"]);
                    DrawSkill(Batch, 208, "Fishing", Viewport.Player.Skill["Fish"]);
                }
                catch (KeyNotFoundException)
                {
                }
            }
        }
    }
}
