using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace Terrain.Quadtree
{
	class Terrain : BasicTerrain
	{
		public Node[,] Quadtree { get; protected set; }
		public Point Root { get; protected set; }

		public override float MinQuality
		{
			get { return base.MinQuality; }
			set { base.MinQuality = (value > 2.0f) ? value : base.MinQuality; }
		}

		enum Location { SE, SW, NE, NW };

		public Terrain(Game game) : base(game) 
		{
			MinQuality = 3.0f;
		}

		protected override void OnTerrainChanged(object sender, EventArgs args)
		{
			base.OnTerrainChanged(sender, args);

			InitializeQuadtree();
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			Effect = Game.Content.Load<Effect>("Effects/Quadtree");
		}

		public override void Initialize()
		{
			base.Initialize();

			InitializeQuadtree();
		}

		private void InitializeQuadtree()
		{
			Quadtree = new Node[Size, Size];
			Root = new Point(Size / 2, Size / 2);

			InitializeErrors(3);
			InitializeBounds(3);
			PopulateErrors(3);
		}

		private void InitializeErrors(int size)
		{
			for (int i = size / 2; i < Size; i += size - 1)
				for (int j = size / 2; j < Size; j += size - 1)
					InitializeErrors(new Point(i, j), size);

			if (size < Size)
				InitializeErrors(size * 2 - 1);
		}

		private void InitializeErrors(Point p, int size)
		{
			Point[] midpoints = GetMidpoints(p, size);
			float[] errors = GetErrors(p, size);

			for (int i = 0; i < 4; i++)
				Quadtree[midpoints[i].X, midpoints[i].Y].RealError = errors[i];

			Quadtree[p.X, p.Y].RealError = errors[4];
		}

		private void InitializeBounds(int size)
		{
			for (int i = size / 2; i < Size; i += size - 1)
				for (int j = size / 2; j < Size; j += size - 1)
					InitializeBounds(new Point(i, j), size);

			if (size < Size)
				InitializeBounds(size * 2 - 1);
		}

		private void InitializeBounds(Point p, int size)
		{
			Point[] children = GetChildren(p, size);
			Vector2 bounds = GetBounds(p);

			if (size > 3)
				for (int i = 0; i < 4; i++)
					bounds = MergeBounds(bounds, GetNode(children[i]).Bounds);
			else
				for (int i = -1; i <= 1; i++)
					for (int j = -1; j <= 1; j++)
						bounds = MergeBounds(bounds, GetBounds(new Point(p.X + i, p.Y + j)));

			Quadtree[p.X, p.Y].Bounds = bounds;
		}

		private void PopulateErrors(int size)
		{
			for (int i = size / 2; i < Size; i += size - 1)
				for (int j = size / 2; j < Size; j += size - 1)
					PopulateErrors(new Point(i, j), size);

			if (size < Size)
				PopulateErrors(size * 2 - 1);
		}

		private void PopulateErrors(Point p, int size)
		{
			Point[] midpoints = GetMidpoints(p, size);
			Point[] edges = GetEdges(p, size);

			float K = MinQuality / (2 * (MinQuality - 2));

			float error = Math.Abs(GetNode(p).RealError);

			for (int i = 0; i < 4; i++)
				error = Math.Max(Math.Abs(GetNode(midpoints[i]).RealError), error);

			if (size > 3)
				for (int i = 0; i < 4; i++)
					error = Math.Max(GetNode(midpoints[i]).PopulatedError * K, error);

			Quadtree[p.X, p.Y].PopulatedError = error;

			for (int i = 0; i < 4; i++)
				Quadtree[edges[i].X, edges[i].Y].PopulatedError = Math.Max(GetNode(edges[i]).PopulatedError, error);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (BruteForceEnabled)
				return;

			UpdateNode(Root, Size, FrustumCullingEnabled);

			if (GeomorphEnabled)
				UpdateGeomorph(Root, Size);
		}

		private void UpdateNode(Point p, int size, bool frustumCulling)
		{
			UpdateBlending(p, size);

			if (!IsEnabled(p))
				return;

			if (GeomorphEnabled)
				UpdateMidpointBlending(p, size);

			Quadtree[p.X, p.Y].Visible = true;

			if (frustumCulling)
			{
				ContainmentType result = ViewFrustum.Contains(GetBoundingBox(p, size));

				if (result == ContainmentType.Disjoint)
					Quadtree[p.X, p.Y].Visible = false;
				else if (result == ContainmentType.Contains)
					frustumCulling = false;
			}

			if (size == 3 || !IsVisible(p))
				return;

			Point[] children = GetChildren(p, size);

			for (int i = 0; i < 4; i++)
				UpdateNode(children[i], size / 2 + 1, frustumCulling);
		}

		private void UpdateBlending(Point p, int size)
		{
			float l = GetDistance(CameraPosition, new Vector3(p.X, AvgHeight, -p.Y));
			float d = size - 1;
			float error = Math.Abs(GetNode(p).PopulatedError * Bumpiness);

			float f = l / (d * MinQuality * Math.Max(Quality * error, 1.0f));

			Quadtree[p.X, p.Y].Blending = MathHelper.Clamp(3 * (1 - f), 0.0f, 1.0f);
		}

		private void UpdateMidpointBlending(Point p, int size)
		{
			Point[] midpoints = GetMidpoints(p, size);
			Point[] neighbors = GetNeighbors(p, size);

			for (int i = 0; i < 4; i++)
			{
				Vector2 midpointBlending = GetNode(midpoints[i]).MidpointBlending;

				if (i < 2 || !Exists(neighbors[i]))
					midpointBlending.X = GetNode(p).Blending;
				if (i >= 2 || !Exists(neighbors[i]))
					midpointBlending.Y = GetNode(p).Blending;

				Quadtree[midpoints[i].X, midpoints[i].Y].MidpointBlending = midpointBlending;
			}
		}

		private void UpdateGeomorph(Point p, int size)
		{
			if (!IsEnabled(p) || !IsVisible(p))
				return;

			Point[] midpoints = GetMidpoints(p, size);
			Point[] children = GetChildren(p, size);

			for (int i = 0; i < 4; i++)
			{
				Node node = GetNode(midpoints[i]);
				float geomorph = node.RealError * (1.0f - Math.Min(node.MidpointBlending.X, node.MidpointBlending.Y));
				GeomorphData[midpoints[i].X, midpoints[i].Y] = geomorph;
			}

			GeomorphData[p.X, p.Y] = GetNode(p).RealError * (1.0f - GetNode(p).Blending);

			if (size > 3)
				for (int i = 0; i < 4; i++)
					UpdateGeomorph(children[i], size / 2 + 1);
		}

		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			if (BruteForceEnabled)
				return;

			Effect.CurrentTechnique.Passes[0].Apply();
			DrawNode(Root, Size);

			Triangles = TriangleCounter;
			DrawCalls = DrawCallCounter;

			TriangleCounter = 0;
			DrawCallCounter = 0;
		}

		private void DrawNode(Point p, int size)
		{
			if ((!IsEnabled(p) || !IsVisible(p)))
				return;

			if (GeomorphEnabled)
				SetGeomorph(p, size);
 
			//DrawTriangleFan(p, size);

			if (size == 3)
				return;

			Point[] children = GetChildren(p, size);

			for (int i = 0; i < 4; i++)
				DrawNode(children[i], size / 2 + 1);
		}

		//private void DrawTriangleFan(Point p, int size)
		//{
		//   Node node = GetNode(p);

		//   Point[] children = GetChildren(p, size);
		//   Point[] neighbors = GetNeighbors(p, size);
		//   Point[] midpoints = GetMidpoints(p, size);
		//   Point[] edges = GetEdges(p, size);
		//   int[] indices = new int[10];
		//   int count = 1;

		//   for (int i = 0; i < 4; i++)
		//   {
		//      if (size == 3 || !IsEnabled(children[i]))
		//      {
		//         if (!Exists(neighbors[i]) || IsEnabled(neighbors[i]))
		//            indices[count++] = GetIndex(midpoints[i]);

		//         indices[count++] = GetIndex(edges[i]);

		//         if ((size > 3 && IsEnabled(children[(i + 1) % 4])) || i == 3)
		//         {
		//            if (i < 3 || !Exists(neighbors[0]) || IsEnabled(neighbors[0]))
		//               indices[count++] = GetIndex(midpoints[(i + 1) % 4]);
		//            else
		//               indices[count++] = GetIndex(edges[0]);

		//            indices[0] = GetIndex(p);

		//            IndexBuffer.SetData<int>(indices, 0, count, SetDataOptions.Discard);
		//            int primitiveCount = count - 2;
		//            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleFan, 0, 0, count, 0, primitiveCount);
		//            DrawCallCounter++;
		//            TriangleCounter += primitiveCount;

		//            count = 1;
		//         }
		//      }
		//   }
		//}

		private BoundingBox GetBoundingBox(Point p, int size)
		{
			Node node = GetNode(p);

			Vector3 min = new Vector3(p.X - size / 2, HeightOffset - 1.0f, -p.Y - size / 2);
			Vector3 max = new Vector3(p.X + size / 2, HeightOffset + 1.0f, -p.Y + size / 2);

			if (HeightmapEnabled && Bumpiness != 0.0f)
			{
				min.Y = ((Bumpiness > 0.0f) ? node.Bounds.X : node.Bounds.Y) * Bumpiness;
				max.Y = ((Bumpiness > 0.0f) ? node.Bounds.Y : node.Bounds.X) * Bumpiness;
			}

			Vector3 v = (max - min) * 0.2f;

			return new BoundingBox(min - v, max + v);
		}

		private void SetGeomorph(Point p, int size)
		{
			Point[] midpoints = GetMidpoints(p, size);
			Point[] edges = GetEdges(p, size);
			float[] geomorph = new float[9];

			for (int i = 0; i < 4; i++)
				geomorph[i] = GetGeomorph(midpoints[i]);

			geomorph[4] = GetGeomorph(p);

			for (int i = 0; i < 4; i++)
				geomorph[i + 5] = GetGeomorph(edges[i]);

			Effect.Parameters["Morphing"].SetValue(geomorph);
			Effect.Parameters["Center"].SetValue(new Vector2(p.X, p.Y));
			Effect.CurrentTechnique.Passes[0].Apply();
		}

		private Vector2 MergeBounds(Vector2 b1, Vector2 b2)
		{
			return new Vector2(Math.Min(b1.X, b2.X), Math.Max(b1.Y, b2.Y));
		}

		private Vector2 GetBounds(Point p)
		{
			float error = GetNode(p).RealError;
			float height = GetHeight(p);

			float minHeight = (error > 0) ? height - error : height;
			float maxHeight = (error < 0) ? height - error : height;

			return new Vector2(minHeight, maxHeight);
		}

		private float[] GetErrors(Point p, int size)
		{
			Point[] midpoints = GetMidpoints(p, size);
			Point[] edges = GetEdges(p, size);
			Location location = GetLocation(p, size);
			float[] errors = new float[5];

			for (int i = 0; i < 4; i++)
				errors[i] = GetError(edges[(i + 3) % 4], midpoints[i], edges[i]);

			if (p == Root)
				errors[4] = (GetError(edges[0], p, edges[2]) + GetError(edges[1], p, edges[3])) / 2.0f;
			else if (location == Location.SW || location == Location.NE)
				errors[4] = GetError(edges[0], p, edges[2]);
			else
				errors[4] = GetError(edges[1], p, edges[3]);

			return errors;
		}

		private Location GetLocation(Point p, int size)
		{
			int x = p.X % ((size - 1) * 2);
			int y = p.Y % ((size - 1) * 2);

			if (x < size && y < size)
				return Location.SW;
			else if (x < size)
				return Location.NW;
			else if (y < size)
				return Location.SE;
			else
				return Location.NE;
		}

		private Point[] GetNeighbors(Point p, int size)
		{
			return new Point[] {
				new Point(p.X, p.Y + size - 1), 
				new Point(p.X + size - 1, p.Y),
				new Point(p.X, p.Y - size + 1),
				new Point(p.X - size + 1, p.Y) };
		}

		private Point[] GetChildren(Point p, int size)
		{
			return new Point[] {
				new Point(p.X + size / 4, p.Y + size / 4),
				new Point(p.X + size / 4, p.Y - size / 4),
				new Point(p.X - size / 4, p.Y - size / 4),
				new Point(p.X - size / 4, p.Y + size / 4) };
		}

		private Point[] GetMidpoints(Point p, int size)
		{
			return new Point[] {
				new Point(p.X, p.Y + size / 2),
				new Point(p.X + size / 2, p.Y),
				new Point(p.X, p.Y - size / 2),
				new Point(p.X - size / 2, p.Y) };
		}

		private Point[] GetEdges(Point p, int size)
		{
			return new Point[] {
				new Point(p.X + size / 2, p.Y + size / 2),
				new Point(p.X + size / 2, p.Y - size / 2),
				new Point(p.X - size / 2, p.Y - size / 2),
				new Point(p.X - size / 2, p.Y + size / 2) };
		}

		private bool IsEnabled(Point p)
		{
			return Quadtree[p.X, p.Y].Blending > 0.0f;
		}

		private bool IsVisible(Point p)
		{
			return Quadtree[p.X, p.Y].Visible;
		}

		private Node GetNode(Point p)
		{
			return Quadtree[p.X, p.Y];
		}
	}
}