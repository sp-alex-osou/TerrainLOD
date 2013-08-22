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

namespace Terrain.ROAM
{
	public class Terrain : BasicTerrain
	{
		private int levels;
		private Node rootLeft;
		private Node rootRight;
		private List<int> Indices;

		Point bottomLeft;
		Point bottomRight;
		Point topLeft;
		Point topRight;

		int vertices;
		int vertexCounter = 4;

		public Terrain(Game game)
			: base(game)
		{
			Indices = new List<int>();
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			Effect = Game.Content.Load<Effect>("Effects/ROAM");
		}

		public override void Initialize()
		{
			base.Initialize();

			InitializeTerrain();
		}

		private void InitializeTerrain()
		{
			levels = (int)Math.Log(Size - 1, 2) * 2 - 1;

			bottomLeft = new Point(0, 0);
			bottomRight = new Point(Size - 1, 0);
			topLeft = new Point(0, Size - 1);
			topRight = new Point(Size - 1, Size - 1);

			rootLeft = InitializeNode(bottomRight, topLeft, bottomLeft, 0);
			rootRight = InitializeNode(topLeft, bottomRight, topRight, 0);

			rootLeft.BaseNeighbor = rootRight;
			rootRight.BaseNeighbor = rootLeft;
		}

		private Node InitializeNode(Point p0, Point p1, Point a, int level)
		{
			Node node = new Node();

			Point p = GetMidpoint(p0, p1);

			node.Error = Math.Abs(GetError(p0, p, p1));
			node.BoundingBox = GetBoundingBox(p0, p1, a);

			if (level < levels)
			{
				node.LeftChild = InitializeNode(a, p0, p, level + 1);
				node.RightChild = InitializeNode(p1, a, p, level + 1);

				if (node.LeftChild.Error > node.Error)
					node.Error = node.LeftChild.Error;

				if (node.RightChild.Error > node.Error)
					node.Error = node.RightChild.Error;

				node.BoundingBox = BoundingBox.CreateMerged(node.LeftChild.BoundingBox, node.RightChild.BoundingBox);
			}
			else
				node.BoundingBox = GetBoundingBox(p0, p1, a);

			return node;
		}

		private BoundingBox GetBoundingBox(Point p0, Point p1, Point a)
		{
			float h1 = GetHeight(p0);
			float h2 = GetHeight(p1);
			float h3 = GetHeight(a);

			Vector3 min = new Vector3(GetMin(p0.X, p1.X, a.X), GetMin(h1, h2, h3), -GetMax(p0.Y, p1.Y, a.Y));
			Vector3 max = new Vector3(GetMax(p0.X, p1.X, a.X), GetMax(h1, h2, h3), -GetMin(p0.Y, p1.Y, a.Y));

			return new BoundingBox(min, max);
		}

		private float GetMin(float a, float b, float c)
		{
			return MathHelper.Min(MathHelper.Min(a, b), c);
		}

		private float GetMax(float a, float b, float c)
		{
			return MathHelper.Max(MathHelper.Max(a, b), c);
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (BruteForceEnabled)
				return;

			UpdateNode(rootLeft, bottomRight, topLeft, bottomLeft);
			UpdateNode(rootRight, topLeft, bottomRight, topRight);

			vertices = vertexCounter;
		}

		private void UpdateNode(Node node, Point p0, Point p1, Point a)
		{
			node.Visible = true;

			if (FrustumCullingEnabled && ViewFrustum.Contains(GetBoundingBox(node)) == ContainmentType.Disjoint)
			{
				node.Visible = false;
				return;
			}

			Point p = GetMidpoint(p0, p1);
			int width = Math.Abs(p0.X - p1.X) + Math.Abs(p0.Y - p1.Y);
			float distanceSquared = (CameraPosition - new Vector3(p.X, GetHeight(p) * Bumpiness, -p.Y)).LengthSquared();

			bool split = SplitCheck(node, width, distanceSquared);

			if (!node.Split && split)
			{
				SplitNode(node);
				vertexCounter++;
			}
			else if (node.Split && !split && CanMerge(node) && CanMerge(node.BaseNeighbor))
			{
				MergeNode(node);
				MergeNode(node.BaseNeighbor);
				vertexCounter--;
			}

			if (node.Split && node.LeftChild != null)
			{
				UpdateNode(node.LeftChild, a, p0, p);
				UpdateNode(node.RightChild, p1, a, p);
			}
		}

		private BoundingBox GetBoundingBox(Node node)
		{
			Vector3 min = node.BoundingBox.Min;
			Vector3 max = node.BoundingBox.Max;

			if (!HeightmapEnabled || Bumpiness == 0.0f)
			{
				min.Y = HeightOffset - 1.0f;
				max.Y = HeightOffset + 1.0f;
			}
			else
			{
				min.Y *= Bumpiness;
				max.Y *= Bumpiness;
			}

			if (min.Y > max.Y)
			{
				float y = min.Y;
				min.Y = max.Y;
				max.Y = y;
			}

			return new BoundingBox(min, max);
		}

		private bool SplitCheck(Node node, int width, float distanceSquared)
		{
			float error = node.Error;

			if (node.BaseNeighbor != null && node.BaseNeighbor.BaseNeighbor == node)
				error = MathHelper.Max(error, node.BaseNeighbor.Error);

			float maxDistance = error * Bumpiness * Quality * width;
			float maxDistanceSquared = maxDistance * maxDistance;

			return distanceSquared < maxDistanceSquared;
		}

		private void SplitNode(Node node)
		{
			if (node == null || node.Split)
				return;

			node.Split = true;

			if (node.Visible && node.LeftChild != null)
			{
				node.LeftChild.Visible = true;
				node.RightChild.Visible = true;
			}

			if (node.BaseNeighbor != null && node.BaseNeighbor.BaseNeighbor != node)
			{
				SplitNode(node.BaseNeighbor);
				vertexCounter++;
			}

			SplitNode(node.BaseNeighbor);
			
			if (node.LeftNeighbor != null)
			{
				if (node.LeftNeighbor.BaseNeighbor == node)
					node.LeftNeighbor.BaseNeighbor = node.LeftChild;
				else
					node.LeftNeighbor.RightNeighbor = node.LeftChild;
			}

			if (node.RightNeighbor != null)
			{
				if (node.RightNeighbor.BaseNeighbor == node)
					node.RightNeighbor.BaseNeighbor = node.RightChild;
				else
					node.RightNeighbor.LeftNeighbor = node.RightChild;
			}

			if (node.LeftChild != null)
			{
				node.LeftChild.BaseNeighbor = node.LeftNeighbor;
				node.RightChild.BaseNeighbor = node.RightNeighbor;

				node.LeftChild.LeftNeighbor = node.RightChild;
				node.RightChild.RightNeighbor = node.LeftChild;

				if (node.BaseNeighbor != null)
				{
					node.RightChild.LeftNeighbor = node.BaseNeighbor.LeftChild;
					node.LeftChild.RightNeighbor = node.BaseNeighbor.RightChild;
				}
			}
		}

		private void MergeNode(Node node)
		{
			if (node == null)
				return;

			node.Split = false;

			if (node.LeftChild != null)
			{
				node.LeftNeighbor = node.LeftChild.BaseNeighbor;
				node.RightNeighbor = node.RightChild.BaseNeighbor;
			}

			if (node.LeftNeighbor != null)
			{
				if (node.LeftNeighbor.BaseNeighbor == node.LeftChild)
					node.LeftNeighbor.BaseNeighbor = node;
				else
					node.LeftNeighbor.RightNeighbor = node;
			}

			if (node.RightNeighbor != null)
			{
				if (node.RightNeighbor.BaseNeighbor == node.RightChild)
					node.RightNeighbor.BaseNeighbor = node;
				else
					node.RightNeighbor.LeftNeighbor = node;
			}
		}

		private bool CanMerge(Node node)
		{
			return (node == null || node.LeftChild == null || (!node.LeftChild.Split && !node.RightChild.Split));
		}

		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			if (BruteForceEnabled)
				return;

			Indices.Clear();

			CollectIndices(rootLeft, bottomRight, topLeft, bottomLeft);
			CollectIndices(rootRight, topLeft, bottomRight, topRight);

			if (Indices.Count == 0)
				return;

			int[] indices = Indices.ToArray();

			IndexBuffer.SetData<int>(indices, 0, indices.Length, SetDataOptions.Discard);

			Effect.CurrentTechnique.Passes[0].Apply();

			int primitiveCount = indices.Length / 3;
			GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices, 0, primitiveCount);
			DrawCallCounter++;
			TriangleCounter += primitiveCount;

			Triangles = TriangleCounter;
			DrawCalls = DrawCallCounter;

			TriangleCounter = 0;
			DrawCallCounter = 0;
		}

		private void CollectIndices(Node node, Point p0, Point p1, Point a)
		{
			if (node != null && !node.Visible)
				return;

			if (node == null || !node.Split)
			{
				Indices.Add(GetIndex(p0));
				Indices.Add(GetIndex(a));
				Indices.Add(GetIndex(p1));
			}
			else
			{
				Point p = GetMidpoint(p0, p1);

				CollectIndices(node.LeftChild, a, p0, p);
				CollectIndices(node.RightChild, p1, a, p);
			}
		}

		private Point GetMidpoint(Point p0, Point p1)
		{
			return new Point((p0.X + p1.X) / 2, (p0.Y + p1.Y) / 2);
		}

		protected override void OnTerrainChanged(object sender, EventArgs args)
		{
			base.OnTerrainChanged(sender, args);

			InitializeTerrain();
		}
	}
}