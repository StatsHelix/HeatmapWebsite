using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace HeatmapGenerator
{
	public class EventMap
	{
		public List<Vector2> Points { get; private set; }

		static readonly Color[] ColorMapping = GenerateColorMap();

		static Color[] GenerateColorMap()
		{
			const int size = 20000;
			Color[] map = new Color[size];
			for (int i = 0; i < size; i++) {
				int of255 = (int)( i * ( 255.0 / size ) );

				map[i] = Color.FromArgb(Math.Min(of255, 230), of255, 255-of255, 0);
			}

			return map;
		}

		public EventMap()
		{
			Points = new List<Vector2> ();
		}

		public EventMap(List<Vector2> points)
		{
			this.Points = points;
		}

		public void AddPoint(Vector2 p)
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

		private int DistanceSquared(int x, int y, Vector2 p)
		{
			return ( x - p.X ) * ( x - p.X ) + ( y - p.Y ) * ( y - p.Y );
		}
	}
}

