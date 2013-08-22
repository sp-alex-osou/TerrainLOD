using System;

using Microsoft.Xna.Framework;

namespace Camera
{
	public interface ICameraHandler : IGameComponent, IUpdateable
	{
		float MouseSpeed { get; set; }
		float MovementSpeed { get; set; }
		float RotationSpeed { get; set; }
	}
}
