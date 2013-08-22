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

using Terrain.Properties;

using Vertex = Microsoft.Xna.Framework.Graphics.VertexPositionNormalTexture;

namespace Terrain
{
	public class BasicTerrain : DrawableGameComponent, ITerrain
	{
		public Texture2D Heightmap 
		{
			get { return _heightmap; }
			
			set 
			{
				if (value != _heightmap)
					heightmapChanged = true;

				_heightmap = value;

				if (_heightmap != null)
					Size = _heightmap.Width;
			}
		}
		public int Size
		{
			get { return _size; }

			private set
			{
				if (value > 0 && value != _size)
					sizeChanged = true;

				_size = (value > 0) ? value : _size;
			}
		}
		public float Quality 
		{ 
			get { return _quality; }
			set { _quality = (value > 0.0f) ? value : _quality; } 
		}

		public virtual int BlockSize { get; set; }
		public virtual float MinQuality { get; set; }

		public Texture2D Texture { get; set; }
		public float TextureResolution { get; set; }
		public float Bumpiness { get; set; }
		public Color TerrainColor { get; set; }

		public bool TextureEnabled { get; set; }
		public bool BruteForceEnabled { get; set; }
		public bool FrustumCullingEnabled { get; set; }
		public bool LightEnabled { get; set; }
		public bool GeomorphEnabled { get; set; }
		public bool HeightmapEnabled { get; set; }

		public Vector3 LightDirection { get; set; }
		public Vector3 LightDiffuse { get; set; }
		public Vector3 LightAmbient { get; set; }
		public Vector3 LightSpecular { get; set; }
		public float LightShininess { get; set; }

		public Matrix World { get; set; }
		public Matrix View { get; set; }
		public Matrix Projection { get; set; }

		public Vector3 CameraPosition { get; set; }
		public BoundingFrustum ViewFrustum { get; set; }

		public int Triangles { get; protected set; }
		public int DrawCalls { get; protected set; }

		protected Effect Effect { get; set; }

		protected float HeightOffset { get; private set; }
		protected float AvgHeight { get; private set; }
		protected float[,] HeightData { get; private set; }
		protected float[,] GeomorphData { get; private set; }
		protected VertexDeclaration VertexDeclaration { get; private set; }
		protected DynamicIndexBuffer IndexBuffer { get; private set; }
		protected DynamicVertexBuffer VertexBuffer { get; private set; }
		protected IndexBuffer BasicIndexBuffer { get; private set; }

		protected int DrawCallCounter { get; set; }
		protected int TriangleCounter { get; set; }

		private Texture2D _heightmap;
		private int _size;
		private float _quality;

		private bool heightmapChanged;
		private bool sizeChanged;
		private Color[] heights;
		private Texture2D NormalMap;

		public event EventHandler TerrainChanged;

		public BasicTerrain(Game game) : base(game)
		{
			Bumpiness = 0.0f;
			TextureResolution = 1.0f;
			Quality = 1.0f;
			Size = 513;

			TerrainChanged += new EventHandler(OnTerrainChanged);
		}

		public override void Initialize()
		{
			base.Initialize();

			GraphicsDevice.DeviceReset += new EventHandler<EventArgs>(GraphicsDevice_DeviceReset);

			VertexDeclaration = Vertex.VertexDeclaration;

			InitializeHeightData();
			InitializeNormalMap();
			InitializeBuffers();
		}

		private void InitializeHeightData()
		{
			HeightData = new float[Size, Size];
			GeomorphData = new float[Size, Size];

			if (Heightmap == null)
				return;

			heights = new Color[Size * Size];
			Heightmap.GetData<Color>(heights);

			HeightOffset = -heights[0].R / 255.0f;

			for (int i = 0; i < Size; i++)
				for (int j = 0; j < Size; j++)
				{
					HeightData[i, j] = (heights[GetIndex(i, j)].R / 255.0f) + HeightOffset;
					AvgHeight += HeightData[i, j];
				}

			AvgHeight /= Size * Size;
		}

		private void InitializeNormalMap()
		{
			int size = Size - 1;

			NormalMap = new Texture2D(GraphicsDevice, size, size, true, SurfaceFormat.Color);
			Color[] normals = new Color[size * size];

			for (int i = 0; i < size; i++)
				for (int j = 0; j < size; j++)
					normals[i + j * size] = (Heightmap != null) ? GetNormal(i, j) : new Color(Vector3.Up);

			NormalMap.SetData<Color>(normals);
		}

		private void InitializeBuffers()
		{
			Vertex[] vertices = new Vertex[Size * Size];

			for (int i = 0; i < Size; i++)
				for (int j = 0; j < Size; j++)
					vertices[i + j * Size].Position = new Vector3(i, 0.0f, -j);

			int[] indices = new int[(Size - 1) * (Size - 1) * 6];
			int count = 0;

			for (int i = 0; i < Size - 1; i++)
				for (int j = 0; j < Size - 1; j++)
				{
					indices[count++] = GetIndex(i, j);
					indices[count++] = GetIndex(i, j + 1);
					indices[count++] = GetIndex(i + 1, j);

					indices[count++] = GetIndex(i, j + 1);
					indices[count++] = GetIndex(i + 1, j + 1);
					indices[count++] = GetIndex(i + 1, j);
				}

			int maxIndices = (Size - 1) * (Size - 1) * 6;

			VertexBuffer = new DynamicVertexBuffer(GraphicsDevice, typeof(Vertex), vertices.Length, BufferUsage.None);
			IndexBuffer = new DynamicIndexBuffer(GraphicsDevice, typeof(int), maxIndices, BufferUsage.None);

			BasicIndexBuffer = new IndexBuffer(GraphicsDevice, typeof(int), maxIndices, BufferUsage.None);

			VertexBuffer.SetData<Vertex>(vertices);
			BasicIndexBuffer.SetData<int>(indices);
		}

		public override void Update(GameTime gameTime)
		{
			if (heightmapChanged)
			{
				InitializeHeightData();
				InitializeNormalMap();

				if (sizeChanged)
					InitializeBuffers();

				sizeChanged = false;
				heightmapChanged = false;

				TerrainChanged(this, null);
			}

			base.Update(gameTime);
		}

		public override void Draw(GameTime gameTime)
		{
			SetupGraphicsDevice();
			SetupEffect();

			base.Draw(gameTime);

			if (BruteForceEnabled)
			{
				DrawTerrain();

				Triangles = TriangleCounter;
				DrawCalls = DrawCallCounter;

				TriangleCounter = 0;
				DrawCallCounter = 0;
			}
		}

		private void DrawTerrain()
		{
			GraphicsDevice.Indices = BasicIndexBuffer;

			Effect.CurrentTechnique.Passes[0].Apply();

			int primitiveCount = (Size - 1) * (Size - 1) * 2;

			GraphicsDevice.DrawIndexedPrimitives(
				PrimitiveType.TriangleList, 0, 0, Size * Size, 0, primitiveCount);
			DrawCallCounter++;
			TriangleCounter += primitiveCount;
		}

		public void LoadHeightmap(string assetName)
		{
			Heightmap = Game.Content.Load<Texture2D>(assetName);
		}

		public void LoadTexture(string assetName)
		{
			Texture = Game.Content.Load<Texture2D>(assetName);
		}

		public float GetHeight(Point p)
		{
			return HeightData[p.X, p.Y];
		}

		public float GetGeomorph(Point p)
		{
			return GeomorphData[p.X, p.Y];
		}

		public bool Exists(Point p)
		{
			return p.X >= 0 && p.X < Size && p.Y >= 0 && p.Y < Size;
		}

		public int GetIndex(Point p)
		{
			return GetIndex(p.X, p.Y);
		}

		public int GetIndex(int x, int y)
		{
			return x + y * Size;
		}

		public float GetError(Point a, Point b, Point c)
		{
			return GetHeight(b) - (GetHeight(a) + GetHeight(c)) / 2.0f;
		}

		public TerrainType GetTerrainType()
		{
			if (this is Quadtree.Terrain)
				return TerrainType.Quadtree;
			else if (this is GeoMipMap.Terrain)
				return TerrainType.GeoMipMap;
			else if (this is ROAM.Terrain)
				return TerrainType.ROAM;
			else
				return TerrainType.Basic;
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			Effect = Game.Content.Load<Effect>("Effects/BasicEffect");
		}

		protected float GetDistance(Vector3 v1, Vector3 v2)
		{
			return (Math.Abs(v1.X - v2.X) + Math.Abs(v1.Y - v2.Y) + Math.Abs(v1.Z - v2.Z));
		}

		private void SetupGraphicsDevice()
		{
			GraphicsDevice.SetVertexBuffer(VertexBuffer);
			GraphicsDevice.Indices = IndexBuffer;
			GraphicsDevice.RasterizerState = RasterizerState.CullNone;
		}

		private void SetupEffect()
		{
			Effect.Parameters["World"].SetValue(World);
			Effect.Parameters["WorldViewProjection"].SetValue(World * View * Projection);
			Effect.Parameters["CameraPosition"].SetValue(CameraPosition);
			Effect.Parameters["Size"].SetValue(Size);
			Effect.Parameters["TerrainColor"].SetValue(TerrainColor.ToVector4());
			Effect.Parameters["Normalmap"].SetValue(NormalMap);
			Effect.Parameters["Bumpiness"].SetValue(Bumpiness);

			Effect.Parameters["HeightmapEnabled"].SetValue(HeightmapEnabled && Heightmap != null);
			Effect.Parameters["TextureEnabled"].SetValue(TextureEnabled && Texture != null);
			Effect.Parameters["LightingEnabled"].SetValue(LightEnabled);
			Effect.Parameters["GeomorphEnabled"].SetValue(GeomorphEnabled);
			Effect.Parameters["BruteForceEnabled"].SetValue(BruteForceEnabled);

			if (Texture != null && TextureEnabled)
			{
				Effect.Parameters["Texture"].SetValue(Texture);
				Effect.Parameters["TextureRepeat"].SetValue(TextureResolution * (Size - 1) / Texture.Width);
			}

			if (Heightmap != null && HeightmapEnabled)
			{
				Effect.Parameters["Heightmap"].SetValue(Heightmap);
				Effect.Parameters["HeightOffset"].SetValue(HeightOffset);
			}

			if (LightEnabled)
			{
				Effect.Parameters["LightDirection"].SetValue(LightDirection);
				Effect.Parameters["LightDiffuse"].SetValue(LightDiffuse);
				Effect.Parameters["LightAmbient"].SetValue(LightAmbient);
				Effect.Parameters["LightSpecular"].SetValue(LightSpecular);
				Effect.Parameters["LightShininess"].SetValue(LightShininess);
			}
		}

		private Color GetNormal(int x, int y)
		{
			byte ay = heights[GetIndex((x < Size - 1) ? x + 1 : 0, y)].R;
			byte by = heights[GetIndex(x, (y < Size - 1) ? y + 1 : 0)].R;
			byte cy = heights[GetIndex((x > 0) ? x - 1 : Size - 1, y)].R;
			byte dy = heights[GetIndex(x, (y > 0) ? y - 1 : Size - 1)].R;

			return new Color(GetNormal((cy - ay)), 255, GetNormal(by - dy));
		}

		private byte GetNormal(int delta)
		{
			return (byte)((255 + delta) / 2);
		}

		private void GraphicsDevice_DeviceReset(object sender, EventArgs e)
		{
			InitializeBuffers();
		}

		virtual protected void OnTerrainChanged(object sender, EventArgs args)
		{
		}
	}
}