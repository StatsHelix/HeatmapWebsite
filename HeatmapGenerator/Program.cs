using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace HeatmapGenerator
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var Database = new AbstractDatastore.MongoDatastore(Assembly.GetEntryAssembly().GetName().Name);
			Heatmap h = new Heatmap(Database,
				File.OpenRead("/home/moritz/Desktop/infe.dem"),
				-2200,
				4400,
				5.9f);

			var result = h.Parse();

			Database.Save<DemoAnalysis>(result);
		}
	}
}
