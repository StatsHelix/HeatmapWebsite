using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using MongoDB.Bson;
using AbstractDatastore;
using System.IO;
using DemoInfo;

namespace HeatmapGenerator
{
	public class RoundEventMap
	{
		public Dictionary<string, EventMap> Maps { get; set; }
		public Dictionary<string, string> Bitmaps { get; set; }
		public List<Participant> Participants { get; set; }

		public string Directory = Path.GetRandomFileName();

		private readonly IDatastore Datastore;

		public RoundEventMap(IDatastore datastore)
		{
			Datastore = datastore;

			Maps = new Dictionary<string, EventMap>();
			Bitmaps = new Dictionary<string, string>();
			Participants = new List<Participant>();
		}

		public void AddBitmap(string name, Image image)
		{
			string fileName = Path.Combine(Directory, name + ".png");

			using (var stream = Datastore.StoreStream(fileName, "image/png"))
				image.Save(stream, ImageFormat.Png);

			Bitmaps.Add(name, fileName);
		}
	}
}

