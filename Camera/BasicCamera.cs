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

using Camera.Properties;

namespace Camera
{
	public class BasicCamera : GameComponent, ICamera
	{
		public Vector3 Position { get; set; }

		public Vector3 Up
		{
			get { return axes.Up; }
			protected set { axes.Up = value; }
		}

		public Vector3 Forward
		{
			get { return axes.Forward; }
			protected set { axes.Forward = value; }
		}

		public Vector3 Right
		{
			get { return axes.Right; }
			protected set { axes.Right = value; }
		}

		public virtual Vector3 MoveForward { get { return Forward; } }
		public virtual Vector3 MoveRight { get { return Right; } }
		public virtual Vector3 MoveUp { get { return Up; } }

		public BoundingFrustum ViewFrustum { get; protected set; }

		public Matrix Projection { get; protected set; }
		public Matrix View { get; protected set; }

		public float FieldOfView { get; set; }
		public float NearPlaneDistance { get; set; }
		public float FarPlaneDistance { get; set; }

		private Axes axes;

		public BasicCamera(Game game)
			: base(game)
		{
			FieldOfView = Settings.Default.FieldOfView;
			NearPlaneDistance = Settings.Default.NearPlaneDistance;
			FarPlaneDistance = Settings.Default.FarPlaneDistance;

			ViewFrustum = new BoundingFrustum(Matrix.Identity);

			axes = new Axes();
			
		}

		public virtual void Pitch(float angle)
		{
			Rotate(Right, angle);
		}

		public virtual void Yaw(float angle)
		{
			Rotate(Up, angle);
		}

		public virtual void Roll(float angle)
		{
			Rotate(Forward, angle);
		}

		public void Rotate(Vector3 axis, float angle)
		{
			Rotate(Matrix.CreateFromAxisAngle(axis, angle));
		}

		public virtual void Rotate(Matrix rotation)
		{
			for (int i = 0; i < axes.Length; i++)
				axes[i] = Vector3.Transform(axes[i], rotation);

			AdjustAxes();
		}

		public void LookAt(Vector3 target)
		{
			Look(target - Position);
		}

		public virtual void Look(Vector3 direction)
		{
			Forward = direction;
			AdjustAxes();
		}

		protected void AdjustAxes()
		{
			Up = Vector3.Cross(Right, Forward);
			Right = Vector3.Cross(Forward, Up);
		}

		public override void Update(GameTime gameTime)
		{
			Projection = Matrix.CreatePerspectiveFieldOfView(
				MathHelper.ToRadians(FieldOfView),
				Game.GraphicsDevice.Viewport.AspectRatio,
				NearPlaneDistance,
				FarPlaneDistance);

			View = Matrix.CreateLookAt(Position, Position + Forward, Up);

			ViewFrustum.Matrix = View * Projection;

			base.Update(gameTime);
		}
	}
}