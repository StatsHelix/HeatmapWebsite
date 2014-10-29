using System;
using System.IO;
using DemoInfo;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;
using AbstractDatastore;

namespace HeatmapGenerator
{
	public class Heatmap
	{
		DemoParser parser = null;

		EventMap CTFlashes = new EventMap();
		EventMap TFlashes = new EventMap();
		EventMap CTSmokes = new EventMap();
		EventMap TSmokes = new EventMap();
		EventMap CTNades = new EventMap();
		EventMap TNades = new EventMap();
		EventMap Fire = new EventMap();

		EventMap TKillOrigin = new EventMap();
		EventMap CTKillOrigin = new EventMap();
		EventMap TDeathPosition = new EventMap();
		EventMap CTDeathPosition = new EventMap();

		DemoAnalysis analysis = new DemoAnalysis();

		Bitmap TPaths = new Bitmap(1024, 1024);
		Bitmap CTPaths = new Bitmap(1024, 1024);
		Graphics TPathsG, CTPathsG;

		Bitmap TKills = new Bitmap(1024, 1024);
		Bitmap CTKills = new Bitmap(1024, 1024);
		Graphics TKillsG, CTKillsG;

		float mapX, mapY, scale;

		private readonly IDatastore Datastore;

		public Heatmap (IDatastore datastore, Stream demo) : this(datastore, demo, new DemoAnalysis())
		{ }

		public Heatmap (IDatastore datastore, Stream demo, DemoAnalysis analysis)
		{
			parser = new DemoParser (demo);
			TPathsG = Graphics.FromImage(TPaths);
			CTPathsG = Graphics.FromImage(CTPaths);
			TKillsG = Graphics.FromImage(TKills);
			CTKillsG = Graphics.FromImage(CTKills);


			parser.FlashNadeExploded += HandleFlashNadeExploded;
			parser.SmokeNadeStarted += HandleSmokeNadeStarted;
			parser.ExplosiveNadeExploded += HandleExplosiveNadeExploded;
			parser.FireNadeStarted += HandleFireNadeStarted;
			parser.PlayerKilled += HandlePlayerKilled;
			parser.TickDone += HandleTickDone;
			parser.RoundStart += HandleRoundStart;

			parser.MatchStarted += HandleMatchStarted;

			this.analysis = analysis;

			this.Datastore = datastore;
		}

		void HandleMatchStarted (object sender, MatchStartedEventArgs e)
		{
			analysis.Participants.ForEach(a => {
				a.Kills = 0;
				a.Deaths = 0;
			});
		}

		int roundNum = 0;
		void HandleRoundStart (object sender, RoundStartedEventArgs e)
		{
			foreach (var player in parser.Players.Values) {
				var p = GetParticipant(player);
				p.Name = player.Name;
				p.SteamID = player.SteamID;
			}

			var CurrentRound = new RoundEventMap(Datastore);
			CurrentRound.Maps = new Dictionary<string, EventMap>() {
				{ "TFlashes", 		TFlashes},
				{ "CTFlashes", 		CTFlashes},
				{ "CTSmokes",		CTSmokes },
				{ "TSmokes", 		TSmokes },
				{ "CTHEs", 			CTNades },
				{ "THEs", 			TNades },
				{ "BothTeamsFire", 	Fire},
				{ "TKillOrigin", 	TKillOrigin},
				{ "CTKillOrigin", 	CTKillOrigin},
				{ "TDeathPosition", TDeathPosition },
				{ "CTDeathPosition",CTDeathPosition },
			};

			CurrentRound.AddBitmap("TKills", TKills);
			CurrentRound.AddBitmap("CTKills", CTKills);
			CurrentRound.AddBitmap("TPaths", TPaths);
			CurrentRound.AddBitmap("CTPaths", CTPaths);

			analysis.Rounds.Add(CurrentRound);

			if (!analysis.IsFinished) {
				analysis.Progress = (double)parser.CurrrentTick / parser.Header.PlaybackTicks;
			}

			Datastore.Save(analysis);

			roundNum++;

			TFlashes = new EventMap();
			CTFlashes = new EventMap();
			CTSmokes  = new EventMap();
			TSmokes  = new EventMap();
			CTNades  = new EventMap();
			TNades  = new EventMap();
			Fire = new EventMap();
			TKillOrigin = new EventMap();
			CTKillOrigin = new EventMap();
			TDeathPosition = new EventMap();
			CTDeathPosition  = new EventMap();
		}

		SolidBrush TBrushSolid = new SolidBrush(Color.FromArgb(200, Color.OrangeRed));
		SolidBrush CTBrushSolid = new SolidBrush(Color.FromArgb(200, Color.CornflowerBlue));

		SolidBrush TBrush = new SolidBrush(Color.FromArgb(30, Color.OrangeRed));
		SolidBrush CTBrush = new SolidBrush(Color.FromArgb(30, Color.CornflowerBlue));
		Size rectSize = new Size(3, 3);
		void HandleTickDone (object sender, TickDoneEventArgs e)
		{
			foreach (var player in parser.Players.Values.Where(a => a.IsAlive)) {
				Brush b = null;
				Graphics g = null;
				if (player.Team == Team.CounterTerrorist) {
					b = CTBrush;
					g = CTPathsG;
				} else {
					b = TBrush;
					g = TPathsG;
				}

				var p = MapPoint(player.Position);
				p.X -= 1;
				p.Y -= 1;

				g.FillRectangle(b, new Rectangle(p.ToPoint(), rectSize));
			}
		}

		void HandlePlayerKilled (object sender, PlayerKilledEventArgs e)
		{
			if (e.Killer.Team == Team.CounterTerrorist)
				CTKillOrigin.AddPoint(MapPoint(e.Killer.Position));
			else
				TKillOrigin.AddPoint(MapPoint(e.Killer.Position));

			if (e.DeathPerson.Team == Team.CounterTerrorist)
				CTDeathPosition.AddPoint(MapPoint(e.DeathPerson.Position));
			else
				TDeathPosition.AddPoint(MapPoint(e.DeathPerson.Position));

			Graphics g = e.Killer.Team == Team.CounterTerrorist ? CTKillsG : TKillsG;
			Brush b = e.Killer.Team == Team.CounterTerrorist ? CTBrushSolid : TBrushSolid;

			Point p1 = MapPoint(e.Killer.Position).ToPoint(), p2 = MapPoint(e.DeathPerson.Position).ToPoint();
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			g.DrawLine(new Pen(b, 1.5f), p1, p2);
			g.FillEllipse(b, p1.X - 3, p1.Y - 3, 7, 7);
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

			GetParticipant(e.Killer).Kills++;
			GetParticipant(e.DeathPerson).Deaths++;
		}

		void HandleFireNadeStarted (object sender, FireEventArgs e)
		{
			Fire.AddPoint(MapPoint(e.Position));
		}

		void HandleExplosiveNadeExploded (object sender, GrenadeEventArgs e)
		{
			if (e.ThrownBy == null)
				return;

			if (e.ThrownBy.Team == Team.CounterTerrorist)
				CTNades.AddPoint(MapPoint(e.Position));
			else
				TNades.AddPoint(MapPoint(e.Position));
		}

		void HandleSmokeNadeStarted (object sender, SmokeEventArgs e)
		{
			if (e.ThrownBy == null)
				return;

			if (e.ThrownBy.Team == Team.CounterTerrorist)
				CTSmokes.AddPoint(MapPoint(e.Position));
			else
				TSmokes.AddPoint(MapPoint(e.Position));
		}

		void HandleFlashNadeExploded (object sender, FlashEventArgs e)
		{
			if (e.ThrownBy == null)
				return;

			if (e.ThrownBy.Team == Team.CounterTerrorist)
				CTFlashes.AddPoint(MapPoint(e.Position));
			else
				TFlashes.AddPoint(MapPoint(e.Position));
		}

		public DemoAnalysis ParseHeaderOnly()
		{
			parser.ParseDemo(false);

			analysis.Metadata = parser.Header;
			SetCoordinates(analysis.Metadata.MapName);

			return analysis;
		}

		private void SetCoordinates(string mapName)
		{
			string[] lines = File.ReadAllLines(Path.Combine("maps", mapName + ".txt"));

			foreach(string line in lines)
			{
				var data = line.Split('"');

				if(data[1] == "pos_x")
				{
					this.mapX = float.Parse(data[3]);
				}
				else if(data[1] == "pos_y")
				{
					this.mapY = float.Parse(data[3]);
				}
				else if (data[1] == "scale")
				{
					this.scale = float.Parse(data[3]);
				}

			}
		}

		public DemoAnalysis ParseTheRest()
		{
			analysis.Metadata = parser.Header;

			while (parser.ParseNextTick());

			analysis.IsFinished = true;
			analysis.Progress = 1.0;

			HandleRoundStart(null, new RoundStartedEventArgs());

			return analysis;
		}


		public Vector2 MapPoint(Vector vec)
		{
			return new Vector2(
				(int)((vec.X - mapX) / scale),
				(int)((mapY - vec.Y) / scale)
			);
		}

		private Participant GetParticipant(Player player)
		{
			var p = analysis.Participants.FirstOrDefault(a => a.IngameID == player.EntityID);

			if (p == null)
				analysis.Participants.Add( p = new Participant(player) );

			return p;
		}
	}
}

