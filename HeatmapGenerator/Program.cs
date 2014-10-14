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
				File.OpenRead("D:\\Users\\Moritz\\Desktop\\VPiBP.dem"),
				-2287,
				3469,
				5.5f);

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
