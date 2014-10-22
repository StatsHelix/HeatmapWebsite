#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.GamerServices;
using DemoInfo;
using System.IO;
using YoStrimmer.Analyzers;
#endregion

namespace YoStrimmer
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class GameUI : Game
	{
		GraphicsDeviceManager graphics;
		SpriteBatch spriteBatch;

		IAnalysis currentAnalysis = null;

		public GameUI()
			: base()
		{
			graphics = new GraphicsDeviceManager(this);

			graphics.PreferredBackBufferWidth = 1024;
			graphics.PreferredBackBufferHeight = 1024;
			Content.RootDirectory = "Content";
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			// TODO: Add your initialization logic here

			base.Initialize();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			// Create a new SpriteBatch, which can be used to draw textures.
			spriteBatch = new SpriteBatch(GraphicsDevice);
			this.IsMouseVisible = true;
			this.Window.AllowUserResizing = false;

			DemoParser parser = new DemoParser(File.OpenRead("G:\\demoinfogo\\infe.dem"));
			parser.ParseDemo(false);
			currentAnalysis = new Replayer();
			currentAnalysis.Initialize(parser, Content);

			// TODO: use this.Content to load your game content here
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// all content.
		/// </summary>
		protected override void UnloadContent()
		{
			// TODO: Unload any non ContentManager content here
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
			if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
				Exit();

			currentAnalysis.Update(gameTime);

			base.Update(gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);

			currentAnalysis.Draw(spriteBatch, gameTime);

			base.Draw(gameTime);
		}
	}
}
