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
	public class FPS : DrawableGameComponent
	{
		public float FrameRate { get; private set; }

		TimeSpan[] times = new TimeSpan[10];
		int index;
		TimeSpan sum;
		TimeSpan elapsed;

		public FPS(Game game)
			: base(game)
		{
		}

		public override void Initialize()
		{
			base.Initialize();
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			elapsed += gameTime.ElapsedGameTime;

			if (elapsed > TimeSpan.FromSeconds(0.2))
			{
				elapsed -= TimeSpan.FromSeconds(0.2);
				FrameRate = 1.0f / ((float)sum.TotalSeconds / times.Length);
			}
		}

		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);
			
			sum += gameTime.ElapsedGameTime - times[index];
			times[index++] = gameTime.ElapsedGameTime;

			if (index == times.Length)
				index = 0;
		}
	}
}