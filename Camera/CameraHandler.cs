using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Camera.Properties;

namespace Camera
{
	public class CameraHandler : GameComponent, ICameraHandler
	{
		public float MovementSpeed { get; set; }
		public float MovementBoost { get; set; }
		public float RotationSpeed { get; set; }
		public float MouseSpeed { get; set; }

		KeyboardState previousKeyboardState;
		ICamera camera;
		int centerX, centerY;
		bool ignoreMouse;

		public CameraHandler(Game game) : base(game)
		{
			MovementSpeed = Settings.Default.MovementSpeed;
			RotationSpeed = Settings.Default.RotationSpeed;
			MouseSpeed = Settings.Default.MouseSpeed;
			MovementBoost = Settings.Default.MovementBoost;

			Game.Window.ClientSizeChanged += new EventHandler<EventArgs>(Window_ClientSizeChanged);
		}

		void Window_ClientSizeChanged(object sender, EventArgs e)
		{
			InitializeMouse();
		}

		public override void Initialize()
		{
			InitializeMouse();

			base.Initialize();
		}

		private void InitializeMouse()
		{
			centerX = Game.Window.ClientBounds.Width / 2;
			centerY = Game.Window.ClientBounds.Height / 2;

			Mouse.SetPosition(centerX, centerY);
		}

		public override void Update(GameTime gameTime)
		{
			camera = (ICamera)Game.Services.GetService(typeof(ICamera));

			if (camera == null || !Game.IsActive)
				return;

			float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

			HandleKeyboard(elapsed);
			HandleMouse(elapsed);

			base.Update(gameTime);
		}

		protected void HandleMouse(float elapsed)
		{
			if (previousKeyboardState.IsKeyDown(Keys.LeftShift) || ignoreMouse)
				return;

			float mouseSpeed = MouseSpeed * elapsed;

			MouseState mouseState = Mouse.GetState();

			if (mouseState.X != centerX)
				camera.Yaw((centerX - mouseState.X) * mouseSpeed);

			if (mouseState.Y != centerY)
				camera.Pitch((centerY - mouseState.Y) * mouseSpeed);

			Mouse.SetPosition(centerX, centerY);
		}

		protected void HandleKeyboard(float elapsed)
		{
			float movementSpeed = MovementSpeed * elapsed;
			float rotationSpeed = RotationSpeed * elapsed;

			Vector3 direction = Vector3.Zero;

			KeyboardState keyboardState = Keyboard.GetState();
			Keys[] keys = keyboardState.GetPressedKeys();

			foreach (Keys key in keys)
				switch (key)
				{
					case Keys.W: direction += camera.MoveForward; break;
					case Keys.S: direction -= camera.MoveForward; break;
					case Keys.D: direction += camera.MoveRight; break;
					case Keys.A: direction -= camera.MoveRight; break;
					case Keys.Y: direction += camera.MoveUp; break;
					case Keys.X: direction -= camera.MoveUp; break;
					case Keys.E: camera.Roll(rotationSpeed); break;
					case Keys.Q: camera.Roll(-rotationSpeed); break;
					case Keys.Up: camera.Pitch(rotationSpeed); break;
					case Keys.Down: camera.Pitch(-rotationSpeed); break;
					case Keys.Left: camera.Yaw(rotationSpeed); break;
					case Keys.Right: camera.Yaw(-rotationSpeed); break;
				}

			if (direction != Vector3.Zero)
			{
				Vector3 velocity = Vector3.Normalize(direction) * movementSpeed;

				if (keyboardState.IsKeyDown(Keys.LeftControl))
					velocity *= MovementBoost;

				camera.Position += velocity;
			}

			if (previousKeyboardState.IsKeyDown(Keys.LeftShift) && keyboardState.IsKeyUp(Keys.LeftShift))
				Mouse.SetPosition(centerX, centerY);

			if (previousKeyboardState.IsKeyDown(Keys.LeftAlt) && keyboardState.IsKeyUp(Keys.LeftAlt))
				ignoreMouse = !ignoreMouse;

			Game.IsMouseVisible = keyboardState.IsKeyDown(Keys.LeftShift) || ignoreMouse;

			previousKeyboardState = keyboardState;
		}
	}
}
