using DemoInfo;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using System.Globalization;
using System.IO;

namespace YoStrimmer.Analyzers
{
	class Replayer : IAnalysis
	{
		DemoParser parser;

		float mapX, mapY, scale;

		public double ReplaySpeed { get; set; }

		double timeSinceLastFrame = 0;

		Texture2D mapBackground;

		Texture2D playerTexture;

		public void Initialize(DemoParser parser, ContentManager Content)
		{
			ReplaySpeed = 2;
			this.parser = parser;
			string file = LoadBackgroundInfo();

			mapBackground = Content.Load<Texture2D>(file);
			playerTexture = Content.Load<Texture2D>("player");
			
		}

		public void Update(GameTime elapsedGameTime)
		{
			timeSinceLastFrame += elapsedGameTime.ElapsedGameTime.TotalSeconds * ReplaySpeed;

			while (timeSinceLastFrame > parser.TickTime)
			{
				parser.ParseNextTick();
				timeSinceLastFrame -= parser.TickTime;
			}
		}

		public void Draw(SpriteBatch spriteBatch, GameTime elapsedGameTime)
		{
			spriteBatch.Begin();

			spriteBatch.Draw(mapBackground, Vector2.Zero, Color.White);

			foreach (var player in parser.Players.Values)
			{
				if (player.IsAlive)
				{
					spriteBatch.Draw(playerTexture, MapPoint(player.Position), null, Color.White, MathHelper.PiOver2 - MathHelper.ToRadians(player.ViewDirectionX), new Vector2(32, 32), .5f, SpriteEffects.None, 1);
				}
			}

			spriteBatch.End();
		}
		private string LoadBackgroundInfo()
		{
			//Okay, set the background-image. 
			var lines = File.ReadAllLines(Path.Combine("overview_source", Path.GetFileName(parser.Map) + ".txt"));

			var file = lines
				.First(a => a.Contains("\"material\""))
				.Split('"')[3];

			if (!file.EndsWith("_radar"))
				file += "_radar";

			mapX = float.Parse(lines
				.First(a => a.Contains("\"pos_x\""))
				.Split('"')[3], CultureInfo.InvariantCulture);
			mapY = float.Parse(lines
				.First(a => a.Contains("\"pos_y\""))
				.Split('"')[3], CultureInfo.InvariantCulture);
			scale = float.Parse(lines
				.First(a => a.Contains("\"scale\""))
				.Split('"')[3], CultureInfo.InvariantCulture);

			return file;
		}


		public Vector2 MapPoint(Vector vec)
		{
			return new Vector2(
				((vec.X - mapX) / scale),
				((mapY - vec.Y) / scale)
			);
		}
	}
}
