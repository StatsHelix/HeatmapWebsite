using System;
using System.Collections.Generic;
using System.IO;
using DemoInfo;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using AbstractDatastore;

namespace HeatmapGenerator
{
	public class DemoAnalysis
	{
		[BsonId]
		public BsonObjectId ID { get; set; }

		public List<Participant> Participants { get; set; }
		public List<RoundEventMap> Rounds { get; set; }
		public DemoHeader Metadata;

		public string DemoFile { get; set; }

		public double Progress { get; set; }
		public bool IsFinished { get; set; }
		public DateTime Uploaded { get; set; }

		public DemoAnalysis()
		{
			Participants = new List<Participant>();
			Rounds = new List<RoundEventMap>();
			this.Uploaded = DateTime.UtcNow;
		}
	}
}

