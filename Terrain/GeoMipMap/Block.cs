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

namespace Terrain.GeoMipMap
{
	class Block : DrawableGameComponent
	{
		public Vector3 Center { get; private set; }
		public Point Position { get; private set; }
		public Point Offset { get; private set; }
		public BoundingBox BoundingBox { get; set; }
		public int CurrentLevel { get; set; }
		public float Blending { get; set; }

		private int size;
		private int levels;
		private float[] MaxErrorSquared;
		private Terrain terrain;

		public Block(Terrain terrain, Point position, Point offset) : base(terrain.Game)
		{
			this.terrain = terrain;

			Offset = offset;
			Position = position;
		}

		public override void Initialize()
		{
			base.Initialize();

			size = terrain.BlockSize;
			levels = terrain.Levels;

			InitializeBoundingBox();
			InitializeErrors();

			Center = (BoundingBox.Min + BoundingBox.Max) / 2.0f;
		}

		private void InitializeErrors()
		{
			Point[] p = new Point[3];
			float error;
			float maxError = 0;

			//float T = (2.0f * Threshold) / VerticalResolution;
			//float A = NearPlaneDistance / (float)Math.Tan(FieldOfView / 2.0f);
			//float C = A / T;

			MaxErrorSquared = new float[levels];

			for (int i = 1; i < size - 1; i += 2)
				for (int j = 1; j < size - 1; j += 2)
				{											
					p[0] = new Point(i, j);
					p[1] = new Point(i - (i % 2), j - (j % 2));
					p[2] = new Point(i + (i % 2), j + (j % 2));

					for (int k = 0; k < 3; k++)
						p[k] = new Point(Offset.X + p[k].X, Offset.Y + p[k].Y);

					error = terrain.GetError(p[0], p[1], p[2]);

					if (Math.Abs(error) > maxError)
						maxError = Math.Abs(error);
				}

			for (int i = 1; i < levels; i++)
			{
				error = maxError * (int)Math.Pow(2, i - 1); 
				MaxErrorSquared[i] = error * error;
			}
		}

		private void InitializeBoundingBox()
		{
			float minHeight = float.MaxValue;
			float maxHeight = float.MinValue;

			for (int i = Offset.X; i < Offset.X + size; i++)
				for (int j = Offset.Y; j < Offset.Y + size; j++)
				{
					float height = terrain.GetHeight(new Point(i, j));

					if (height < minHeight)
						minHeight = height;

					if (height > maxHeight)
						maxHeight = height;
				}

			Vector3 min = new Vector3(Offset.X, minHeight, -Offset.Y - size);
			Vector3 max = new Vector3(Offset.X + size, maxHeight, -Offset.Y);

			BoundingBox = new BoundingBox(min, max);
		}

		public override void Update(GameTime gameTime)
		{
			float distanceSquared = (terrain.CameraPosition - Center).LengthSquared();
			float modifikation = terrain.Bumpiness * terrain.Quality;
			float modifikationSquared = modifikation * modifikation;

			for (int i = 0; i < terrain.Levels; i++)
				if (distanceSquared > MaxErrorSquared[i] * modifikationSquared)
					CurrentLevel = i;

			if (terrain.GeomorphEnabled && !terrain.BruteForceEnabled && CurrentLevel < levels - 1)
			{
				float D0 = MaxErrorSquared[CurrentLevel] * modifikationSquared;
				float D1 = MaxErrorSquared[CurrentLevel + 1] * modifikationSquared;

				Blending = (distanceSquared - D0) / (D1 - D0);
			}

			base.Update(gameTime);
		}
	}
}