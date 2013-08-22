using System;
using System.Collections.Generic;
using System.Linq;

using Main.Properties;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using Camera;
using Terrain;
using Main.Components;

namespace Main
{
	public class Main : Game
	{
		GraphicsDeviceManager graphics;

		ITerrain terrain;
		ICamera camera;
		ICameraHandler cameraHandler;

		FPS fps;
		HUD hud;

		KeyboardState keyboardState;
		KeyboardState previousKeyboardState;

		bool fakeCamera;
		Vector3 fakeCameraPosition;
		BoundingFrustum fakeViewFrustum;
		Point resolution;

		public Main()
		{
			graphics = new GraphicsDeviceManager(this);

			graphics.SynchronizeWithVerticalRetrace = false;
			graphics.ApplyChanges();

			resolution = Settings.Default.WindowResolution;

			graphics.PreferredBackBufferWidth = resolution.X;
			graphics.PreferredBackBufferHeight = resolution.Y;

			Content.RootDirectory = "Content";

			camera = new FreeLookCamera(this);
			cameraHandler = new CameraHandler(this);
			fps = new FPS(this);
			hud = new HUD(this);

			Components.Add(camera);
			Components.Add(cameraHandler);
			Components.Add(fps);
			Components.Add(hud);

			Services.AddService(typeof(ICamera), camera);

			fakeViewFrustum = new BoundingFrustum(Matrix.Identity);
		}

		protected override void Initialize()
		{
			InitializeTerrain(Settings.Default.Terrain);

			base.Initialize();
		}

		private void InitializeTerrain(TerrainType type)
		{
			terrain = TerrainFactory.GetTerrain(type, this);

			terrain.LoadHeightmap(Settings.Default.Heightmap);		
			terrain.LoadTexture(Settings.Default.Texture);

			terrain.Bumpiness = Settings.Default.Bumpiness;
			terrain.FrustumCullingEnabled = Settings.Default.FrustumCullingEnabled;
			terrain.BruteForceEnabled = Settings.Default.BruteForceEnabled;
			terrain.TextureEnabled = Settings.Default.TextureEnabled;
			terrain.TextureResolution = Settings.Default.TextureResolution;
			terrain.HeightmapEnabled = Settings.Default.HeightmapEnabled;
			terrain.GeomorphEnabled = Settings.Default.GeomorphEnabled;
			terrain.LightEnabled = Settings.Default.LightEnabled;
			terrain.LightDiffuse = Settings.Default.LightDiffuse;
			terrain.LightDirection = Settings.Default.LightDirection;
			terrain.LightAmbient = Settings.Default.LightAmbient;
			terrain.LightSpecular = Settings.Default.LightSpecular;
			terrain.LightShininess = Settings.Default.LightShininess;
			terrain.Quality = Settings.Default.Quality;
			terrain.TerrainColor = Settings.Default.TerrainColor;
			terrain.BlockSize = Settings.Default.BlockSize;
			terrain.MinQuality = Settings.Default.MinQuality;

			terrain.Initialize();
		}

		private void ChangeTerrain(TerrainType type)
		{
			ITerrain previousTerrain = terrain;

			terrain = TerrainFactory.GetTerrain(type, this);

			terrain.Heightmap = previousTerrain.Heightmap;
			terrain.Texture = previousTerrain.Texture;
			terrain.Bumpiness = previousTerrain.Bumpiness;
			terrain.FrustumCullingEnabled = previousTerrain.FrustumCullingEnabled;
			terrain.BruteForceEnabled = previousTerrain.BruteForceEnabled;
			terrain.TextureEnabled = previousTerrain.TextureEnabled;
			terrain.TextureResolution = previousTerrain.TextureResolution;
			terrain.HeightmapEnabled = previousTerrain.HeightmapEnabled;
			terrain.GeomorphEnabled = previousTerrain.GeomorphEnabled;
			terrain.LightEnabled = previousTerrain.LightEnabled;
			terrain.LightDiffuse = previousTerrain.LightDiffuse;
			terrain.LightDirection = previousTerrain.LightDirection;
			terrain.LightAmbient = previousTerrain.LightAmbient;
			terrain.LightSpecular = previousTerrain.LightSpecular;
			terrain.LightShininess = previousTerrain.LightShininess;
			terrain.Quality = previousTerrain.Quality;
			terrain.TerrainColor = previousTerrain.TerrainColor;
			terrain.BlockSize = previousTerrain.BlockSize;
			terrain.MinQuality = previousTerrain.MinQuality;

			terrain.Initialize();
		}

		protected override void Update(GameTime gameTime)
		{
			keyboardState = Keyboard.GetState();

			if (keyboardState.IsKeyDown(Keys.Escape))
				this.Exit();

			if (IsKeyTyped(Keys.F1))
				ChangeTerrain(TerrainType.Quadtree);

			if (IsKeyTyped(Keys.F2))
				ChangeTerrain(TerrainType.GeoMipMap);

			if (IsKeyTyped(Keys.F3))
				ChangeTerrain(TerrainType.ROAM);

			if (IsKeyTyped(Keys.D0))
				ToggleFullscreen();

			if (IsKeyTyped(Keys.Tab))
				ToggleFillMode();

			if (IsKeyTyped(Keys.G))
				terrain.GeomorphEnabled = !terrain.GeomorphEnabled;

			if (IsKeyTyped(Keys.H))
				terrain.HeightmapEnabled = !terrain.HeightmapEnabled;

			if (IsKeyTyped(Keys.T))
				terrain.TextureEnabled = !terrain.TextureEnabled;

			if (IsKeyTyped(Keys.L))
				terrain.LightEnabled = !terrain.LightEnabled;

			if (IsKeyTyped(Keys.B))
				terrain.BruteForceEnabled = !terrain.BruteForceEnabled;

			if (IsKeyTyped(Keys.F))
				terrain.FrustumCullingEnabled = !terrain.FrustumCullingEnabled;

			if (IsKeyTyped(Keys.O))
				fakeCamera = !fakeCamera;

			if (IsKeyTyped(Keys.C))
				GC.Collect();

			if (IsKeyTyped(Keys.I))
				hud.Visible = !hud.Visible;

			if (IsKeyTyped(Keys.D1))
				terrain.LoadHeightmap("Heightmaps/Heightmap1");

			if (IsKeyTyped(Keys.D2))
				terrain.LoadHeightmap("Heightmaps/Heightmap2");

			if (IsKeyTyped(Keys.D3))
				terrain.LoadHeightmap("Heightmaps/Heightmap3");

			if (IsKeyTyped(Keys.D4))
				terrain.LoadHeightmap("Heightmaps/Heightmap4");

			if (IsKeyTyped(Keys.D5))
			{
				terrain.LoadHeightmap("Heightmaps/PugetSound");
				terrain.LoadTexture("Textures/PugetSound");
				terrain.TextureResolution = 1;
				terrain.Bumpiness = 100;
			}

			if (IsKeyTyped(Keys.D6))
				terrain.LoadTexture("Textures/Rock");

			if (IsKeyTyped(Keys.D7))
				terrain.LoadTexture("Textures/Sand");

			if (IsKeyTyped(Keys.D8))
				terrain.LoadTexture("Textures/Desert");

			if (IsKeyTyped(Keys.D9))
				terrain.LoadTexture("Textures/Grass");

			if (IsKeyTyped(Keys.F10))
				terrain.Bumpiness += 10.0f;

			if (IsKeyTyped(Keys.F9))
				terrain.Bumpiness -= 10.0f;

			if (IsKeyTyped(Keys.F12))
				terrain.TextureResolution *= 2.0f;

			if (IsKeyTyped(Keys.F11))
				terrain.TextureResolution *= 0.5f;

			if (IsKeyTyped(Keys.OemPlus))
				terrain.Quality *= 2.0f;

			if (IsKeyTyped(Keys.OemMinus))
				terrain.Quality /= 2.0f;

			previousKeyboardState = keyboardState;

			base.Update(gameTime);

			if (!fakeCamera)
			{
				fakeCameraPosition = camera.Position;
				fakeViewFrustum.Matrix = camera.ViewFrustum.Matrix;
			}

			terrain.ViewFrustum = fakeViewFrustum;
			terrain.CameraPosition = fakeCameraPosition;

			terrain.Update(gameTime);

			hud.FPS = (int)Math.Round(fps.FrameRate);
			hud.Size = terrain.Size;
			hud.Quality = terrain.Quality;
			hud.DrawCalls = terrain.DrawCalls;
			hud.Triangles = terrain.Triangles;
			hud.Bumpiness = terrain.Bumpiness;

			switch (terrain.GetTerrainType())
			{
				case TerrainType.Basic: hud.Method = "Basic"; break;
				case TerrainType.GeoMipMap: hud.Method = "GeoMipMap"; break;
				case TerrainType.Quadtree: hud.Method = "Roettger"; break;
				case TerrainType.ROAM: hud.Method = "ROAM"; break;
			}
		}

		private bool IsKeyTyped(Keys key)
		{
			return keyboardState.IsKeyDown(key) && previousKeyboardState.IsKeyUp(key);
		}

		private void ToggleFullscreen()
		{
			Point windowResolution = Settings.Default.WindowResolution;
			Point fullscreenResolution = Settings.Default.FullscreenResolution;

			resolution = (graphics.IsFullScreen) ? windowResolution : fullscreenResolution;

			graphics.PreferredBackBufferWidth = resolution.X;
			graphics.PreferredBackBufferHeight = resolution.Y;

			graphics.ToggleFullScreen();
		}

		private void ToggleFillMode()
		{
			FillMode fillMode = GraphicsDevice.RasterizerState.FillMode;

			GraphicsDevice.RasterizerState = new RasterizerState() { FillMode = (fillMode == FillMode.Solid) ? FillMode.WireFrame : FillMode.Solid };
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Settings.Default.BackgroundColor);

			terrain.World = Matrix.Identity;
			terrain.View = camera.View;
			terrain.Projection = camera.Projection;
			terrain.Draw(gameTime);

			base.Draw(gameTime);
		}
	}
}
