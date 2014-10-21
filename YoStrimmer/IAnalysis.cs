using DemoInfo;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YoStrimmer
{
	interface IAnalysis
	{
		void Update(GameTime elapsedGameTime);
		void Draw(SpriteBatch spriteBatch, GameTime elapsedGameTime);
		void Initialize(DemoParser parser, ContentManager content);
	}
}
