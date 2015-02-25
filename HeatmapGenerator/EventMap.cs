using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace HeatmapGenerator
{
	public class EventMap
	{
		public List<Vector2> Points { get; private set; }

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
	}
}

