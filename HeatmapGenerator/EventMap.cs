using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace HeatmapGenerator
{
	public class EventMap
	{
		public List<Point> Points { get; private set; }

		static readonly Color[] ColorMapping = GenerateColorMap();

		static Color[] GenerateColorMap()
		{
			const int size = 6000;
			Color[] map = new Color[size];
			for (int i = 0; i < size; i++) {
				int of255 = (int)( i * ( 255.0 / size ) );

				map[i] = Color.FromArgb(Math.Max(of255 - 50, 0), of255, 30, 255 - of255);
			}

			return map;
		}

		public EventMap()
		{
			Points = new List<Point> ();
		}

		public EventMap(List<Point> points)
		{
			this.Points = points;
		}

		public void AddPoint(Point p)
		{
			Points.Add (p);
		}

		public Image Draw(int width, int height)
		{
			Bitmap b = new Bitmap (width, height);

			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					b.SetPixel (x, y, GetColor(CalculateStrength(x, y)));
				}
			}

			return b;
		}

		private Color GetColor(int stength)
		{
			return ColorMapping[Math.Min(stength, ColorMapping.Length - 1)];
		}
			
		private int CalculateStrength(int x, int y)
		{
			return Points.Sum (a => Math.Max (2000 - DistanceSquared (x, y, a), 0));
		}

		private int DistanceSquared(int x, int y, Point p)
		{
			return ( x - p.X ) * ( x - p.X ) + ( y - p.Y ) * ( y - p.Y );
		}
	}
}

