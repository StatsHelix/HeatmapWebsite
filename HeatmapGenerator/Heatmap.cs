using System;
using System.IO;
using DemoInfo;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;
using AbstractDatastore;
using System.Globalization;

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

        EventMap CTHoldingPosition = new EventMap();
        EventMap THoldingPosition = new EventMap();

        EventMap CTGoodFlashes = new EventMap();
        EventMap TGoodFlashes = new EventMap();

        EventMap CTBadFlashes = new EventMap();
        EventMap TBadFlashes = new EventMap();

        KillMap Kills = new KillMap();

		DemoAnalysis analysis;

		double mapX, mapY, scale;

		private readonly IDatastore Datastore;

		public event Action<DemoAnalysis> OnRoundAnalysisFinished;

        bool afterFirstKill = true;

        public int roundStartTick = -1;

		public Heatmap (IDatastore datastore, Stream demo, Overview overview) : this(datastore, demo, overview, new DemoAnalysis())
		{ }

		public Heatmap (IDatastore datastore, Stream demo, Overview overview, DemoAnalysis analysis)
		{
			parser = new DemoParser (demo);

			parser.FlashNadeExploded += HandleFlashNadeExploded;
			parser.SmokeNadeStarted += HandleSmokeNadeStarted;
			parser.ExplosiveNadeExploded += HandleExplosiveNadeExploded;
			parser.FireNadeStarted += HandleFireNadeStarted;
			parser.PlayerKilled += HandlePlayerKilled;
			parser.TickDone += HandleTickDone;
			parser.RoundStart += HandleRoundStart;
            parser.FreezetimeEnded += parser_FreezetimeEnded;

			parser.MatchStarted += HandleMatchStarted;

			this.analysis = analysis;
			this.analysis.Overview = overview;

			this.Datastore = datastore;
		}

        void parser_FreezetimeEnded(object sender, FreezetimeEndedEventArgs e)
        {
            afterFirstKill = false;
            roundStartTick = parser.CurrentTick;
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
			foreach (var player in parser.PlayingParticipants) {
				var p = GetParticipant(player);
			}



			var CurrentRound = new RoundEventMap(Datastore);
            CurrentRound.CTScore = parser.CTScore;
            CurrentRound.TScore = parser.TScore;
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
				{ "THoldingPosition", THoldingPosition },
				{ "CTHoldingPosition", CTHoldingPosition },
				{ "TGoodFlashes", TGoodFlashes },
				{ "CTGoodFlashes", CTGoodFlashes },
				{ "TBadFlashes", TBadFlashes },
				{ "CTBadFlashes", CTBadFlashes },
			};

            CurrentRound.Kills = Kills;


			analysis.Rounds.Add(CurrentRound);

			if (!analysis.IsFinished) {
				analysis.Progress = (double)parser.CurrentTick / parser.Header.PlaybackTicks;
			}

			if (OnRoundAnalysisFinished != null)
				OnRoundAnalysisFinished(analysis);
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
            CTDeathPosition = new EventMap();
            CTHoldingPosition = new EventMap();
            THoldingPosition = new EventMap();
            CTGoodFlashes = new EventMap();
            TGoodFlashes = new EventMap();
            CTBadFlashes = new EventMap();
            TBadFlashes = new EventMap();

            Kills = new KillMap();
        }

		void HandleTickDone (object sender, TickDoneEventArgs e)
        {
            float timeInRound = (parser.CurrentTick - roundStartTick) * parser.TickTime;
            if(parser.CurrentTick % Math.Round((parser.TickRate / 2), 0) == 0) //all 500 ms
            {
                foreach(var player in parser.PlayingParticipants.Where(a => a.SteamID != 0 && a.IsAlive))
                {
                    if(!afterFirstKill && timeInRound > 15) //15 sec freezetime + 10 sec running
                    {
                        if (player.Team == Team.CounterTerrorist)
                            CTHoldingPosition.AddPoint(MapPoint(player));
                        else
                            THoldingPosition.AddPoint(MapPoint(player));
                    }
                }
            }


            if (parser.CurrentTick % Math.Round((parser.TickRate / 4), 0) == 0) //all 250 ms
            {
                foreach (var player in parser.PlayingParticipants.Where(a => a.SteamID != 0 && a.IsAlive))
                {
                    /* if (player.Team == Team.CounterTerrorist)
                        CTPosition.AddPoint(MapPoint(player));
                    else
                        TPosition.AddPoint(MapPoint(player)); */
                }
            }

		}

		void HandlePlayerKilled (object sender, PlayerKilledEventArgs e)
		{
            if (e.Killer == null || e.DeathPerson == null)
                return;

			if (e.Killer.Team == Team.CounterTerrorist)
                CTKillOrigin.AddPoint(MapPoint(e.Killer));
			else
                TKillOrigin.AddPoint(MapPoint(e.Killer));

			if (e.DeathPerson.Team == Team.CounterTerrorist)
                CTDeathPosition.AddPoint(MapPoint(e.DeathPerson));
			else
                TDeathPosition.AddPoint(MapPoint(e.DeathPerson));

            Kills.AddPoint(new Kill()
            {
                Headshot = e.Headshot ? 1 : 0,
                Weapon = e.Weapon.Weapon.ToString(),
                KillerPosition = e.Killer != null && e.Killer.IsAlive ? MapPoint(e.Killer) : null,
                VictimPosition = e.DeathPerson != null && e.DeathPerson.IsAlive ? MapPoint(e.DeathPerson) : null,
                KillerTeam = e.Killer != null ? (int)e.Killer.Team : -1,
                VictimTeam = e.DeathPerson != null ? (int)e.DeathPerson.Team : -1,

            });

            afterFirstKill = true;

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
				CTNades.AddPoint(MapPoint(e.Position, e.ThrownBy));
			else
                TNades.AddPoint(MapPoint(e.Position, e.ThrownBy));
		}

		void HandleSmokeNadeStarted (object sender, SmokeEventArgs e)
		{
			if (e.ThrownBy == null)
				return;

			if (e.ThrownBy.Team == Team.CounterTerrorist)
                CTSmokes.AddPoint(MapPoint(e.Position, e.ThrownBy));
			else
                TSmokes.AddPoint(MapPoint(e.Position, e.ThrownBy));
		}

		void HandleFlashNadeExploded (object sender, FlashEventArgs e)
		{
			if (e.ThrownBy == null)
				return;

            int flashedCTs = e.FlashedPlayers.Count(a => a.IsAlive && a.Team == Team.CounterTerrorist);
            int flashedTs = e.FlashedPlayers.Count(a => a.IsAlive && a.Team == Team.Terrorist);

            if (e.ThrownBy.Team == Team.CounterTerrorist)
            {
                CTFlashes.AddPoint(MapPoint(e.Position, e.ThrownBy));

                if(flashedTs > 0)
                    CTGoodFlashes.AddPoint(MapPoint(e.Position, e.ThrownBy));
                else
                    CTBadFlashes.AddPoint(MapPoint(e.Position, e.ThrownBy));
            }
            else
            {
                TFlashes.AddPoint(MapPoint(e.Position, e.ThrownBy));

                if (flashedCTs > 0)
                    TGoodFlashes.AddPoint(MapPoint(e.Position, e.ThrownBy));
                else
                    TBadFlashes.AddPoint(MapPoint(e.Position, e.ThrownBy));
            }

		}

		public DemoAnalysis ParseHeaderOnly()
		{
            parser.ParseHeader();
			analysis.Metadata = parser.Header;
			SetCoordinates(analysis.Metadata.MapName);

			return analysis;
		}

		private void SetCoordinates(string mapName)
		{
			if(this.analysis.Overview != null)
			{
				this.mapX = this.analysis.Overview.PosX;
				this.mapY = this.analysis.Overview.PosY;
				this.scale = this.analysis.Overview.Scale;
				return;
			}

			string[] lines = File.ReadAllLines(Path.Combine("maps", mapName + ".txt"));

			foreach(string line in lines)
			{
				var data = line.Split('"');

				if (data.Length > 2)
				{

					if (data[1] == "pos_x")
					{
						this.mapX = double.Parse(data[3], CultureInfo.InvariantCulture);
					}
					else if (data[1] == "pos_y")
					{
						this.mapY = double.Parse(data[3], CultureInfo.InvariantCulture);
					}
					else if (data[1] == "scale")
					{
						this.scale = double.Parse(data[3], CultureInfo.InvariantCulture);
					}
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

        public Vector2 MapPoint(Vector vec, Player p)
        {
            return new Vector2(
                (int)((vec.X - mapX) / scale),
                (int)((mapY - vec.Y) / scale),
                p
            );
        }
        public Vector2 MapPoint(Player p)
        {
            return new Vector2(
                (int)((p.Position.X - mapX) / scale),
                (int)((mapY - p.Position.Y) / scale),
                p
            );
        }

		private Participant GetParticipant(Player player)
		{
			var p = analysis.Participants.FirstOrDefault(a => a.SteamID == player.SteamID);

            if (p == null && player.SteamID != 0)
				analysis.Participants.Add( p = new Participant(player) );

			return p;
		}

		public IFormatProvider CutureInfo { get; set; }
	}
}

