using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

namespace Camera
{
	public class FreeLookCamera : BasicCamera
	{
		public override Vector3 MoveUp 
		{
			get { return Vector3.Up; } 
		}

		public override Vector3 MoveForward 
		{
			get { return Vector3.Normalize(new Vector3(Forward.X, 0.0f, Forward.Z)); }
		}

		public override Vector3 MoveRight
		{
			get { return Vector3.Normalize(new Vector3(Right.X, 0.0f, Right.Z)); }
		}

		public FreeLookCamera(Game game)
			: base(game)
		{
		}

		public override void Yaw(float angle)
		{
			Rotate(Vector3.Up, angle);
		}

		public override void Roll(float angle)
		{
		}

		public override void Rotate(Matrix rotation)
		{
			if (Vector3.Transform(Up, rotation).Y > 0.0f)
				base.Rotate(rotation);
		}
	}
}