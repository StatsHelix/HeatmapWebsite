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


			Font f = new Font(FontFamily.GenericSansSerif, 20, FontStyle.Bold);
			SolidBrush brush = new SolidBrush(Color.CornflowerBlue);
			foreach(var dic in h.Parse())
			{
				Graphics g = Graphics.FromImage(dic.Value);

				g.DrawString("Created with demo.ehvag.de", f, brush, 5, 5);

				dic.Value.Save(dic.Key + ".png", ImageFormat.Png);
			}
		}
	}
}
