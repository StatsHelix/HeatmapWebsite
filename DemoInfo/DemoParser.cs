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
        public event EventHandler<HeaderParsed> HeaderParsed;

        public event EventHandler<MatchStarted> MatchStarted;

        public event EventHandler<TickDone> TickDone;
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
        DemoPacketParser PacketParser;

        public List<Player> Players = new List<Player>();

        internal Dictionary<int, Entity> entites = new Dictionary<int, Entity>();

        public Dictionary<int, PlayerInfo> RawPlayers = new Dictionary<int, PlayerInfo>();

        public List<CSVCMsg_CreateStringTable> stringTables = new List<CSVCMsg_CreateStringTable>();

        public DemoParser(Stream input)
        {
            reader = new BinaryReader(input);
            PacketParser = new DemoPacketParser(this);
        }

        public void ParseDemo(bool fullParse)
        {
            ParseHeader();

            if (HeaderParsed != null)
                HeaderParsed(this, new HeaderParsed(Header));

            if(fullParse)
            {
                while (ParseNextTick())
                {
                }
            }
                
        }

        public bool ParseNextTick()
        {

            bool b = ParseTick();
            Players.Clear();
            foreach (var entity in entites.Values.Where(a => a.ServerClass.Name == "CCSPlayer"))
            {
                if(entity.Properties.ContainsKey("m_vecOrigin"))
                {
                    Player p = new Player();
                    p.EntityID = entity.ID;
                    p.Position = (Vector)entity.Properties["m_vecOrigin"];
                    
                    //p.Name = RawPlayers[entity.ID - 1].Name;
                    //p.SteamID = RawPlayers[entity.ID - 1].FriendsID;
                    //p.Team = (Team)entity.Properties["m_iTeamNum"];
                    Players.Add(p);
                }
            }

            if (b)
            {
                if (TickDone != null)
                    TickDone(this, new TickDone());
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

            int a;
            switch (command)
            {
                case DemoCommand.Synctick:
                    break;
                case DemoCommand.Stop:
                    return false;
                    break;
                case DemoCommand.ConsoleCommand:
                    reader.ReadVolvoPacket();
                    break;
                case DemoCommand.DataTables:
                    DataTables.ParsePacket(reader.ReadVolvoPacket());
                    break;
                case DemoCommand.StringTables:
                    StringTables.ParsePacket(reader.ReadVolvoPacket());
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

            PacketParser.ParsePacket(packet);
        }

        #region EventCaller
        internal void RaiseMatchStarted()
        {
            MatchStarted(this, new MatchStarted());
        }
        #endregion
    }
}
