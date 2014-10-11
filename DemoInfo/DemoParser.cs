using DemoInfo.DP;
using DemoInfo.DT;
using DemoInfo.Messages;
using DemoInfo.ST;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoInfo
{
    public class DemoParser
    {
        #region Events
        /// <summary>
        /// Called once when the Header of the demo is parsed
        /// </summary>
        public event EventHandler<HeaderParsedEventArgs> HeaderParsed;

        public event EventHandler<MatchStartedEventArgs> MatchStarted;

        public event EventHandler<TickDoneEventArgs> TickDone;

		public event EventHandler<PlayerKilledEventArgs> PlayerKilled;

		public event EventHandler<WeaponFiredEventArgs> WeaponFired;
        #endregion

        #region Information
        public string Map
        {
            get { return Header.MapName; }
        }

        #endregion

        BinaryReader reader;
        public DemoHeader Header { get; private set; }

        internal DataTableParser DataTables = new DataTableParser();
        StringTableParser StringTables = new StringTableParser();

		public Dictionary<int, Player> Players = new Dictionary<int, Player>();

        internal Dictionary<int, Entity> entites = new Dictionary<int, Entity>();

        public Dictionary<int, PlayerInfo> RawPlayers = new Dictionary<int, PlayerInfo>();

        public List<CSVCMsg_CreateStringTable> stringTables = new List<CSVCMsg_CreateStringTable>();

        public DemoParser(Stream input)
        {
            reader = new BinaryReader(input);
        }

        public void ParseDemo(bool fullParse)
        {
            ParseHeader();

            if (HeaderParsed != null)
                HeaderParsed(this, new HeaderParsedEventArgs(Header));

            if(fullParse)
            {
                while (ParseNextTick())
                {
                }
            }
                
        }

		List<string> types = new List<string>();

        public bool ParseNextTick()
        {

			bool b = ParseTick();

			foreach (var type in entites.Values.Where(a => !types.Contains(a.ServerClass.Name))) {
				types.Add (type.ServerClass.Name);

				Console.WriteLine ("##" + type.ServerClass.Name);
			}


            foreach (var entity in entites.Values.Where(a => a.ServerClass.Name == "CCSPlayer"))
            {
				if(entity.Properties.ContainsKey("m_vecOrigin") && entity.Properties.ContainsKey("m_iHealth") && RawPlayers.ContainsKey(entity.ID - 1))
                {
					if (!Players.ContainsKey (entity.ID))
						Players [entity.ID] = new Player ();

					Player p = Players [entity.ID];


                    p.EntityID = entity.ID;
                    p.Position = (Vector)entity.Properties["m_vecOrigin"];
					p.HP = (int)entity.Properties ["m_iHealth"];
                    p.Name = RawPlayers[entity.ID - 1].Name;
                    p.SteamID = RawPlayers[entity.ID - 1].FriendsID;
                    p.Team = (Team)entity.Properties["m_iTeamNum"];

					if(entity.Properties.ContainsKey("m_angEyeAngles[1]"))
						p.ViewDirectionX = (float)entity.Properties ["m_angEyeAngles[1]"] + 90;

					if(entity.Properties.ContainsKey("m_angEyeAngles[0]"))
						p.ViewDirectionY = (float)entity.Properties ["m_angEyeAngles[0]"];



					if (p.IsAlive) {
						p.LastAlivePosition = p.Position;
					}
                }
			}

			foreach (var entity in entites.Values.Where(a => a.ServerClass.Name == "CCSPlayerResource"))
			{

			}

            if (b)
            {
                if (TickDone != null)
                    TickDone(this, new TickDoneEventArgs());
            }

            return b;
        }

        private void ParseHeader()
        {
            var header = DemoHeader.ParseFrom(reader);

            if (header.Filestamp != "HL2DEMO")
                throw new Exception("Invalid File-Type - expecting HL2DEMO");

            if (header.Protocol != 4)
                throw new Exception("Invalid Demo-Protocol");

            Header = header;
        }

        private bool ParseTick()
        {
            DemoCommand command = (DemoCommand)reader.ReadByte();

			int TickNum = reader.ReadInt32();
            int playerSlot = reader.ReadByte();

            switch (command)
            {
                case DemoCommand.Synctick:
                    break;
                case DemoCommand.Stop:
                    return false;
                case DemoCommand.ConsoleCommand:
                    reader.ReadVolvoPacket();
                    break;
                case DemoCommand.DataTables:
                    DataTables.ParsePacket(reader.ReadVolvoPacket());
                    break;
                case DemoCommand.StringTables:
					StringTables.ParsePacket(reader.ReadVolvoPacket(), this);
                    break;
                case DemoCommand.UserCommand:
                    reader.ReadInt32();
                    reader.ReadVolvoPacket();
                    break;
                case DemoCommand.Signon:
                case DemoCommand.Packet:
                    ParseDemoPacket();
                    break;
                default:
                    throw new Exception("Can't handle Demo-Command " + command);
            }

            return true;
        }

        private void ParseDemoPacket()
        {
            CommandInfo info = CommandInfo.Parse(reader);
            int SeqNrIn = reader.ReadInt32();
            int SeqNrOut = reader.ReadInt32();

            var packet = reader.ReadVolvoPacket();

			DemoPacketParser.ParsePacket(packet, this);
        }

        #region EventCaller
        internal void RaiseMatchStarted()
        {
			if(MatchStarted != null)
            	MatchStarted(this, new MatchStartedEventArgs());
        }

		internal void RaisePlayerKilled(PlayerKilledEventArgs kill)
		{
			if (PlayerKilled != null)
				PlayerKilled (this, kill);
		}

		internal void RaiseWeaponFired(WeaponFiredEventArgs fire)
		{
			if (WeaponFired != null)
				WeaponFired (this, fire);
		}
        #endregion
    }
}
