using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Flai.Mongo;

namespace HeatmapGenerator
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Heatmap h = new Heatmap(
				File.OpenRead("/home/moritz/Desktop/infe.dem"),
				-2200,
				4400,
				5.9f);

			var result = h.Parse();

			Database.Save<DemoAnalysis>(result);
		}
	}
}
