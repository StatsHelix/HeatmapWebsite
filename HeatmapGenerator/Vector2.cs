using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DemoInfo;

namespace HeatmapGenerator
{
	public class Vector2
	{
        public int X { get; set; }
		public int Y { get; set; }
		public string ID { get; set; }

		public Vector2()
		{
			
		}

		public Vector2(int x, int y, Player p)
		{
			X = x;
			Y = y;
            ID = GetSteamID(p.SteamID);
		}

        public Vector2(int x, int y)
		{
			X = x;
			Y = y;
            ID = null;
		}

        public static string GetSteamID(Int64 communityID)
        {
            communityID = communityID - 76561197960265728;
            Int64 authServer = communityID % 2;
            communityID = communityID - authServer;
            Int64 authID = communityID / 2;
            return Convert.ToString(authID, 16);
        }
	}

}

