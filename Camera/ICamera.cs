using System;

using Microsoft.Xna.Framework;

namespace Camera
{
	public interface ICamera : IGameComponent, IUpdateable
	{
		Vector3 Position { get; set; }

		Vector3 Forward { get; }
		Vector3 Up { get; }
		Vector3 Right { get; }

		Vector3 MoveForward { get; }
		Vector3 MoveUp { get; }
		Vector3 MoveRight { get; }

		BoundingFrustum ViewFrustum { get; }

		Matrix View { get; }
		Matrix Projection { get; }

		float FieldOfView { get; set; }
		float NearPlaneDistance { get; set; }
		float FarPlaneDistance { get; set; }

		void Look(Vector3 direction);
		void LookAt(Vector3 target);
		
		void Pitch(float angle);
		void Yaw(float angle);
		void Roll(float angle);

		void Rotate(Vector3 axis, float angle);
		void Rotate(Matrix rotation);
	}
}
