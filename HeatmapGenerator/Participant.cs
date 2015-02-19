using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using MongoDB.Bson;
using System.IO;
using DemoInfo;

namespace HeatmapGenerator
{
	public class Participant
	{
		public int IngameID { get; set; }
		public string Name { get; set; }
        public string PointID { get; set; }
        public long SteamID { get; set; }
		public int Kills { get; set; }
		public int Deaths { get; set; }
		public Team StartingTeam { get; set; }

		public Participant(Player player)
		{
			this.IngameID = player.EntityID;
			this.Name = player.Name;
            this.PointID = Vector2.GetSteamID(player.SteamID);
            this.SteamID = player.SteamID;
			this.StartingTeam = player.Team;
		}

		public Participant()
		{}
	}

}

