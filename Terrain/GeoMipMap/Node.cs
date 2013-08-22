using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

namespace Terrain.GeoMipMap
{
	class Node
	{
		public List<Node> Children;
		public BoundingBox BoundingBox;
		public bool Visible;
		public Block Block;
	}
}
