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
    class DemoParser
    {
        BinaryReader reader;
        public DemoHeader Header { get; private set; }

        public DataTableParser DataTables = new DataTableParser();
        StringTableParser StringTables = new StringTableParser();
        DemoPacketParser PacketParser;

        public Dictionary<int, PlayerInfo> Players = new Dictionary<int, PlayerInfo>();

        public List<CSVCMsg_CreateStringTable> stringTables = new List<CSVCMsg_CreateStringTable>();

        public DemoParser(Stream input)
        {
            reader = new BinaryReader(input);
            PacketParser = new DemoPacketParser(this);
        }

        public void ParseDemo()
        {
            ParseHeader();

            while (ParseTick())
            {
                
            }
        }

        private void ParseHeader()
        {
            var header = DemoHeader.ParseFrom(reader);

            if (header.Filestamp != "HL2DEMO")
                throw new Exception("Invalid File-Type - expecting HL2DEMO");

            if (header.Protocol != 4)
                throw new Exception("Invalid Demo-Protocol");
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
    }
}
