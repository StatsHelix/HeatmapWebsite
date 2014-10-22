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
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

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

		Dictionary<string, SoundEffect> Sounds = new Dictionary<string, SoundEffect>();


		ContentManager Content;

		List<Player> LastFire = new List<Player>(20);

		public void Initialize(DemoParser parser, ContentManager content)
		{
			this.parser = parser;
			string file = LoadBackgroundInfo();

			parser.WeaponFired += parser_WeaponFired;

			this.Content = content;

			mapBackground = this.Content.Load<Texture2D>(file);
			playerTexture = this.Content.Load<Texture2D>("player");
			
		}

		void parser_WeaponFired(object sender, WeaponFiredEventArgs e)
		{
			if (!Sounds.ContainsKey(e.Weapon.OriginalString))
				AddSound(e.Weapon.OriginalString);

			if (ReplaySpeed <= 1.1)
			{
				var instance = Sounds[e.Weapon.OriginalString].CreateInstance();
				var emitter = new AudioEmitter();
				emitter.Position = new Vector3(e.Shooter.Position.X, 0, e.Shooter.Position.Y);

				AudioListener listen = new AudioListener();
				listen.Position = new Vector3(512, 0, 512);
				instance.Apply3D(listen, emitter);
				instance.Play();
				LastFire.Add(e.Shooter);
			}
		}


		public void AddSound(string weapon)
		{
			if (weapon.Contains("knife"))
			{
				Sounds[weapon] = Content.Load<SoundEffect>("sound/weapons/knife_slash1");
			}
			else if(weapon == "awp")
			{
				Sounds[weapon] = Content.Load<SoundEffect>("sound/weapons/awp1");
			}
			else if(weapon == "glock")
			{
				Sounds[weapon] = Content.Load<SoundEffect>("sound/weapons/glock18-1");
			}
			else
			{
				Sounds[weapon] = Content.Load<SoundEffect>("sound/weapons/" + weapon + "-1");
			}

		}

		public void Update(GameTime elapsedGameTime)
		{
			KeyboardState kbState = Keyboard.GetState();

			ReplaySpeed = 1;

			ReplaySpeed *= kbState.IsKeyDown(Keys.LeftControl) ? 10f : 1f;

			if(ReplaySpeed < 1.1)
				ReplaySpeed *= kbState.IsKeyDown(Keys.LeftShift) ? 50f : 1f;

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
					float scale = LastFire.Contains(player) ? .6f : .5f;

					spriteBatch.Draw(
						playerTexture,
						MapPoint(player.Position), 
						null,
						player.Team == Team.Terrorist ? Color.DarkOrange : Color.CornflowerBlue,
						MathHelper.PiOver2 - MathHelper.ToRadians(player.ViewDirectionX),
						new Vector2(32, 32),
						scale,
						SpriteEffects.None,
						1);
				}
			}

			spriteBatch.End();

			LastFire.Clear();
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
