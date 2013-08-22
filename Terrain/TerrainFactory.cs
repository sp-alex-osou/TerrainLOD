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

namespace Terrain
{
	public static class TerrainFactory
	{
		public static ITerrain GetTerrain(TerrainType type, Game game)
		{
			ITerrain terrain = null;

			switch (type)
			{
				case TerrainType.Quadtree:
					terrain = new Quadtree.Terrain(game);
					break;
				case TerrainType.GeoMipMap:
					terrain = new GeoMipMap.Terrain(game);
					break;
				case TerrainType.ROAM:
					terrain = new ROAM.Terrain(game);
					break;
			}

			return terrain;
		}
	}
}