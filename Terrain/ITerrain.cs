using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using System;

namespace Terrain
{
	public interface ITerrain : IGameComponent, IDrawable, IUpdateable
	{
		Texture2D Texture { get; set; }
		Texture2D Heightmap { get; set; }

		bool FrustumCullingEnabled { get; set; }
		bool HeightmapEnabled { get; set; }
		bool TextureEnabled { get; set; }
		bool LightEnabled { get; set; }
		bool BruteForceEnabled { get; set; }
		bool GeomorphEnabled { get; set; }

		Matrix World { get; set; }
		Matrix View { get; set; }
		Matrix Projection { get; set; }

		Vector3 CameraPosition { get; set; }
		BoundingFrustum ViewFrustum { get; set; }

		float TextureResolution { get; set; }
		float Bumpiness { get; set; }
		float Quality { get; set; }
		

		Vector3 LightDirection { get; set; }
		Vector3 LightDiffuse { get; set; }
		Vector3 LightSpecular { get; set; }
		Vector3 LightAmbient { get; set; }
		float LightShininess { get; set; }

		Color TerrainColor { get; set; }

		int Size { get; }

		int BlockSize { get; set; }
		float MinQuality { get; set; }

		void LoadHeightmap(string assetName);		
		void LoadTexture(string assetName);

		TerrainType GetTerrainType();
		int Triangles { get; }
		int DrawCalls { get; }
	}
}
