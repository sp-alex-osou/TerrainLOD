using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

namespace Terrain.ROAM
{
	class Node
	{
		public Node LeftNeighbor;
		public Node RightNeighbor;
		public Node BaseNeighbor;
		public Node LeftChild;
		public Node RightChild;
		public bool Split;
		public bool Visible;
		public float Error;
		public BoundingBox BoundingBox;
	}
}