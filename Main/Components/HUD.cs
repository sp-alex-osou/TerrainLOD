using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace Main.Components
{
	public class HUD : DrawableGameComponent
	{
		public Color Color { get; set; }

		public int FPS { get; set; }
		public string Method { get; set; }
		public int Size { get; set; }
		public float Quality { get; set; }
		public int Triangles { get; set; }
		public int DrawCalls { get; set; }
		public float Bumpiness { get; set; }

		SpriteBatch spriteBatch;
		SpriteFont spriteFont;

		public HUD(Game game)
			: base(game)
		{
			Color = Color.White;
		}

		public override void Initialize()
		{
			base.Initialize();

			spriteBatch = new SpriteBatch(GraphicsDevice);
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			spriteFont = Game.Content.Load<SpriteFont>("Fonts/HUD");
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
		}

		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			string method = string.Format("Method: {0}", Method);
			string fps = string.Format("FPS: {0}", FPS);
			string triangles = string.Format("Triangles: {0}", Triangles);
			string drawcalls = string.Format("Draw Calls: {0}", DrawCalls);
			string size = string.Format("Size: {0}x{0}", Size);
			string quality = string.Format("Quality: {0}", Quality);
			string bumpiness = string.Format("Bumpiness: {0}", Bumpiness);

			RasterizerState rasterizerState = GraphicsDevice.RasterizerState;

			GraphicsDevice.RasterizerState = new RasterizerState { FillMode = FillMode.Solid };

			spriteBatch.Begin();

			spriteBatch.DrawString(spriteFont, fps, new Vector2(10, 10), Color);
			spriteBatch.DrawString(spriteFont, method, new Vector2(10, 25), Color);
			spriteBatch.DrawString(spriteFont, size, new Vector2(10, 40), Color);
			spriteBatch.DrawString(spriteFont, quality, new Vector2(10, 55), Color);
			spriteBatch.DrawString(spriteFont, triangles, new Vector2(10, 70), Color);
			spriteBatch.DrawString(spriteFont, drawcalls, new Vector2(10, 85), Color);
			spriteBatch.DrawString(spriteFont, bumpiness, new Vector2(10, 100), Color);

			spriteBatch.End();

			GraphicsDevice.RasterizerState = rasterizerState;
		}
	}
}