using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using MongoDB.Bson;
using AbstractDatastore;
using System.IO;
using DemoInfo;
using MongoDB.Bson.Serialization.Attributes;

namespace HeatmapGenerator
{
    [BsonIgnoreExtraElements]

	public class RoundEventMap
	{
		public Dictionary<string, EventMap> Maps { get; set; }

        public KillMap Kills { get; set; }

        public int CTScore { get; set; }
        public int TScore { get; set; }

		private readonly IDatastore Datastore;


		public RoundEventMap(IDatastore datastore)
		{
			Datastore = datastore;

			Maps = new Dictionary<string, EventMap>();
		}
	}
}

