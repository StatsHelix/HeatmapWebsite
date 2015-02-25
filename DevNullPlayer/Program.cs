using System;
using System.IO;
using DemoInfo;

namespace DevNullPlayer
{
	class MainClass
	{
		public static void Main(string[] args)
		{
            using (var input = File.OpenRead(args[0]))
            {
                DemoParser p = new DemoParser(input);
                p.ParseHeader();
                p.ParseToEnd();
            }
		}
	}
}
