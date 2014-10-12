using System;
using System.IO;
using DemoInfo;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;

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

		Bitmap TPaths = new Bitmap(1024, 1024);
		Bitmap CTPaths = new Bitmap(1024, 1024);
		Graphics TPathsG, CTPathsG;


		Bitmap TKills = new Bitmap(1024, 1024);
		Bitmap CTKills = new Bitmap(1024, 1024);
		Graphics TKillsG, CTKillsG;

		float mapX, mapY, scale;

		public Heatmap (Stream demo, float posX, float posY, float scale)
		{
			parser = new DemoParser (demo);
			TPathsG = Graphics.FromImage(TPaths);
			CTPathsG = Graphics.FromImage(CTPaths);
			TKillsG = Graphics.FromImage(TKills);
			CTKillsG = Graphics.FromImage(CTKills);


			parser.MatchStarted += (object sender, MatchStartedEventArgs e) => {
				parser.FlashNadeExploded += HandleFlashNadeExploded;
				parser.SmokeNadeStarted += HandleSmokeNadeStarted;
				parser.ExplosiveNadeExploded += HandleExplosiveNadeExploded;
				parser.FireNadeStarted += HandleFireNadeStarted;
				parser.PlayerKilled += HandlePlayerKilled;
				parser.TickDone += HandleTickDone;
			};

			this.mapX = posX;
			this.mapY = posY;
			this.scale = scale;
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

				g.FillRectangle(b, new Rectangle(p, rectSize));

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

			Point p1 = MapPoint(e.Killer.Position), p2 = MapPoint(e.DeathPerson.Position);
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			g.DrawLine(new Pen(b, 1.5f), p1, p2);
			g.FillEllipse(b, p1.X - 2, p1.Y - 2, 5, 5);
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
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

		public Dictionary<string, Image> Parse()
		{
			parser.ParseDemo(true);

			return new Dictionary<string, Image>() {
				{ "TFlashes", TFlashes.Draw(1024, 1024) },
				{ "CTFlashes", CTFlashes.Draw(1024, 1024) },
				{ "CTSmokes", CTSmokes.Draw(1024, 1024) },
				{ "TSmokes", TSmokes.Draw(1024, 1024) },
				{ "CTHEs", CTNades.Draw(1024, 1024) },
				{ "THEs", TNades.Draw(1024, 1024) },
				{ "BothTeamsFire", Fire.Draw(1024, 1024) },
				{ "TKillOrigin", TKillOrigin.Draw(1024, 1024) },
				{ "CTKillOrigin", CTKillOrigin.Draw(1024, 1024) },
				{ "TDeathPosition", TDeathPosition.Draw(1024, 1024) },
				{ "CTDeathPosition", CTDeathPosition.Draw(1024, 1024) },
				{ "TPaths", TPaths},
				{ "CTPaths", CTPaths },
				{ "TKills", TKills},
				{ "CTKills", CTKills },
			};

		}

		public Point MapPoint(Vector vec)
		{
			return new Point(
				(int)((vec.X - mapX) / scale),
				(int)((mapY - vec.Y) / scale)
			);
		}
	}
}

