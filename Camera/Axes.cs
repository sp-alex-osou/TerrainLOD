using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

namespace Camera
{
	class Axes
	{
		private Vector3[] axes = { Vector3.Right, Vector3.Up, Vector3.Forward };

		public Vector3 this[int i]
		{
			get { return axes[i]; }
			set { axes[i] = value; }
		}

		public int Length
		{
			get { return axes.Length; }
		}

		public Vector3 Right
		{
			get { return axes[0]; }
			set { axes[0] = Vector3.Normalize(value); }
		}
		
		public Vector3 Up 
		{
			get { return axes[1]; }
			set { axes[1] = Vector3.Normalize(value); }
		}

		public Vector3 Forward
		{
			get { return axes[2]; }
			set { axes[2] = Vector3.Normalize(value); }
		}
	}
}
