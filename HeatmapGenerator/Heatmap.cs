using System;
using System.IO;
using DemoInfo;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace HeatmapGenerator
{
	public class Heatmap
	{
		DemoParser parser = null;

		EventMap CTFlashes = new EventMap();
		EventMap TFlashes = new EventMap();

		float mapX, mapY, scale;

		public Heatmap (Stream demo, float posX, float posY, float scale)
		{
			parser = new DemoParser (demo);

			parser.FlashNadeExploded += HandleFlashNadeExploded;

			this.mapX = posX;
			this.mapY = posY;
			this.scale = scale;
		}

		public Dictionary<string, Image> Parse()
		{
			parser.ParseDemo(true);

			return new Dictionary<string, Image>() {
				{ "TFlashes", TFlashes.Draw(1024, 1024) },
				{ "CTFlashes", CTFlashes.Draw(1024, 1024) },
			};
		}

		void HandleFlashNadeExploded (object sender, FlashEventArgs e)
		{
			if (e.ThrownBy.Team == Team.CounterTerrorist)
				CTFlashes.AddPoint(MapPoint(e.Position));
			else
				TFlashes.AddPoint(MapPoint(e.Position));
		}

		public Point MapPoint(Vector vec)
		{
			return new Point(
				(int)((vec.X - mapX) / scale),
				(int)((mapY - vec.Y) / scale)
			);
		}
	}
}

