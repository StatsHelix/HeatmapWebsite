using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace HeatmapGenerator
{
	public class Vector2
	{
		public int X { get; set; }
		public int Y { get; set; }

		public Vector2()
		{
			
		}

		public Vector2(int x, int y)
		{
			X = x;
			Y = y;
		}

		public Point ToPoint()
		{
			return new Point(X, Y);
		}
	}

}

