using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using MongoDB.Bson;
using Flai.Mongo;
using System.IO;
using DemoInfo;

namespace HeatmapGenerator
{
	public class RoundEventMap
	{
		public Dictionary<string, EventMap> Maps { get; set; }
		public Dictionary<string, string> Bitmaps { get; set; }
		public List<Participant> Participants { get; set; }
		public DemoHeader Metadata { get; set; }
		public DateTime Uploaded { get; set; }

		public string Directory = Path.GetRandomFileName();

		public RoundEventMap()
		{
			Maps = new Dictionary<string, EventMap>();
			Bitmaps = new Dictionary<string, string>();
			Participants = new List<Participant>();
			this.Uploaded = DateTime.UtcNow;
		}

		public void AddBitmap(string name, Image image)
		{
			string fileName = Path.Combine(Directory, name + ".png");

			var stream = Database.StoreStream(fileName);
			image.Save(stream, ImageFormat.Png);
			stream.Close();

			Bitmaps.Add(name, fileName);
		}
	}
}

