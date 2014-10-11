using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace HeatmapGenerator
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Heatmap h = new Heatmap(
				File.OpenRead("/home/moritz/.steam/steam/SteamApps/common/Counter-Strike Global Offensive/csgo/replays/match730_003023670552775622658_1146278938_900.dem"),
				-3230.0f,
				1713.0f,
				5.0f);

			h.Parse();
		}
	}
}
