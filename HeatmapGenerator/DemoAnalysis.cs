using System;
using System.Collections.Generic;
using System.IO;
using DemoInfo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HeatmapGenerator
{
	public class DemoAnalysis
	{
		[BsonId]
		public BsonObjectId ID { get; set; }

		public List<Participant> Participants { get; set; }
		public List<RoundEventMap> Rounds { get; set; }
		public DemoHeader Metadata;

		public DemoAnalysis()
		{
			Participants = new List<Participant>();
			Rounds = new List<RoundEventMap>();
		}

		public static DemoAnalysis Create(Stream DemoStream, float posX, float posY, float scale)
		{
			Heatmap h = new Heatmap(DemoStream, posX, posY, scale);


			return h.Parse();
		}
	}
}

