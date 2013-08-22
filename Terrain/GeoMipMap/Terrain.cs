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
	class Terrain : BasicTerrain
	{
		public int Levels { get; private set; }
		public int BlockCount { get; private set; }

		private Block[,] blocks;
		private Indices indices;
		private Node root;

		public Terrain(Game game) : base(game)
		{
			indices = new Indices(Game, this);
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			Effect = Game.Content.Load<Effect>("Effects/GeoMipMap");
		}

		public override void Initialize()
		{
			base.Initialize();

			Levels = (int)Math.Log((BlockSize - 1), 2) + 1;

			InitializeBlocks();
			InitializeQuadtree();

			indices.Initialize();
		}

		private void InitializeBlocks()
		{
			BlockCount = (Size - 1) / (BlockSize - 1);

			blocks = new Block[BlockCount, BlockCount];

			for (int i = 0; i < BlockCount; i++)
				for (int j = 0; j < BlockCount; j++)
					blocks[i, j] = new Block(this, new Point(i, j), new Point(i * (BlockSize - 1), j * (BlockSize - 1)));
			
			foreach (Block block in blocks)
				block.Initialize();
		}

		private void InitializeQuadtree()
		{
			root = InitializeNode(BlockCount, 0, 0);
		}

		private Node InitializeNode(int count, int x, int y)
		{
			Node node = new Node();

			if (count > 1)
			{
				node.Children = new List<Node>();

				for (int i = 0; i < 2; i++)
					for (int j = 0; j < 2; j++ )
						node.Children.Add(InitializeNode(count / 2, x + i * count / 2, y + j * count / 2));

				foreach (Node child in node.Children)
					node.BoundingBox = BoundingBox.CreateMerged(node.BoundingBox, child.BoundingBox);
			}
			else
			{
				node.Block = blocks[x, y];
				node.BoundingBox = node.Block.BoundingBox;
			}

			return node;
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (BruteForceEnabled)
				return;

			UpdateNode(root, FrustumCullingEnabled, gameTime);
		}

		private void UpdateNode(Node node, bool frustumCulling, GameTime gameTime)
		{
			node.Visible = true;

			if (frustumCulling)
			{
				ContainmentType result = ViewFrustum.Contains(GetBoundingBox(node));

				if (result == ContainmentType.Disjoint)
					node.Visible = false;
				else if (result == ContainmentType.Contains)
					frustumCulling = false;
			}

			if (!node.Visible)
				return;

			if (node.Children != null)
				foreach (Node child in node.Children)
					UpdateNode(child, frustumCulling, gameTime);

			if (node.Block != null)
				node.Block.Update(gameTime);
		}

		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			if (BruteForceEnabled)
				return;

			Effect.Parameters["BlockSize"].SetValue(BlockSize);
			Effect.Parameters["Levels"].SetValue(Levels);

			Effect.CurrentTechnique.Passes[0].Apply();
			DrawNode(root);

			Triangles = TriangleCounter;
			DrawCalls = DrawCallCounter;

			TriangleCounter = 0;
			DrawCallCounter = 0;
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

		private void DrawNode(Node node)
		{
			if (!node.Visible)
				return;

			if (node.Children != null)
				foreach (Node child in node.Children)
					DrawNode(child);
			else
				DrawBlock(node.Block);
		}

		private void DrawBlock(Block block)
		{
			int[] neighborLevels = GetNeighborLevels(block);
			int[] blockIndices = indices.GetIndices(block.CurrentLevel, neighborLevels).ToArray();

			if (blockIndices.Length == 0)
				return;

			Effect.Parameters["Level"].SetValue(block.CurrentLevel);
			Effect.Parameters["Blending"].SetValue(block.Blending);
			Effect.Parameters["NeighborLevels"].SetValue(neighborLevels);
			Effect.Parameters["NeighborBlendings"].SetValue(GetNeighborBlendings(block));
			Effect.Parameters["Offset"].SetValue(new Vector2(block.Offset.X, block.Offset.Y));
			Effect.CurrentTechnique.Passes[0].Apply();

			IndexBuffer.SetData<int>(blockIndices, 0, blockIndices.Length, SetDataOptions.Discard);

			int size = (int)(BlockSize / Math.Pow(2, block.CurrentLevel) + 1);
			int primitiveCount = blockIndices.Length / 3;

			GraphicsDevice.DrawIndexedPrimitives(
				PrimitiveType.TriangleList, GetIndex(block.Offset), 0, size * size, 0, primitiveCount);

			TriangleCounter += primitiveCount;
			DrawCallCounter++;
		}

		private int[] GetNeighborLevels(Block block)
		{
			int x = block.Position.X;
			int y = block.Position.Y;

			return new int[] {
				(y < BlockCount - 1) ? blocks[x, y + 1].CurrentLevel : block.CurrentLevel,
				(x < BlockCount - 1) ? blocks[x + 1, y].CurrentLevel : block.CurrentLevel,
				(y > 0) ? blocks[x, y - 1].CurrentLevel : block.CurrentLevel,
				(x > 0) ? blocks[x - 1, y].CurrentLevel : block.CurrentLevel
			};
		}

		private float[] GetNeighborBlendings(Block block)
		{
			int x = block.Position.X;
			int y = block.Position.Y;

			return new float[] {
				(y < BlockCount - 1) ? blocks[x, y + 1].Blending : block.Blending,
				(x < BlockCount - 1) ? blocks[x + 1, y].Blending : block.Blending,
				(y > 0) ? blocks[x, y - 1].Blending : block.Blending,
				(x > 0) ? blocks[x - 1, y].Blending : block.Blending
			};
		}

		protected override void OnTerrainChanged(object sender, EventArgs args)
		{
			base.OnTerrainChanged(sender, args);

			InitializeBlocks();
			InitializeQuadtree();

			indices.Initialize();
		}
	}
}