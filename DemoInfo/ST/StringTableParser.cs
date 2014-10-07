using demoinfosharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoInfo.ST
{
    class StringTableParser
    {
        public void ParsePacket(byte[] data)
        {
            BitArrayStream reader = new BitArrayStream(data);

            int numTables = reader.ReadByte();

            for (int i = 0; i < numTables; i++)
            {
                string tableName = reader.ReadString();

                ParseStringTable(reader, tableName == "userinfo");
            }
        }

        public void ParseStringTable(BitArrayStream reader, bool isUserInfo)
        {
            int numStrings = (int)reader.ReadInt(16);


            for (int i = 0; i < numStrings; i++)
            {
                string stringName = reader.ReadString();

                if (stringName.Length >= 100)
                    throw new Exception("Roy said I should throw this.");

                if (reader.ReadBit())
                {
                    int userDataSize = (int)reader.ReadInt(16);

                    byte[] data = reader.ReadBytes(userDataSize);

                    if (isUserInfo && userDataSize == PlayerInfo.SizeOf)
                    {
                        //read the user-info
                        throw new NotImplementedException("Not implemented here.");
                    }
                }
            }

            // Client side stuff
	        if ( reader.ReadBit() )
	        {
		        int numstrings = (int)reader.ReadInt(16);
		        for ( int i = 0 ; i < numstrings; i++ )
		        {
			        string stringname = reader.ReadString();

			        if ( reader.ReadBit() )
			        {
				        int userDataSize = ( int )reader.ReadInt(16);

				        reader.ReadBytes( userDataSize );

			        }
			        else
			        {
			        }
		        }
	        }
        }
    }
}
