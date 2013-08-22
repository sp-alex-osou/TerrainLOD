using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

namespace Terrain.Quadtree
{
	struct Node
	{
		public float Blending;
		public Vector2 MidpointBlending;

		public float PopulatedError;
		public float RealError;

		public Vector2 Bounds;

		public bool Visible;		
	}
}