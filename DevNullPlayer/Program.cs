using System;
using System.IO;
using DemoInfo;

namespace DevNullPlayer
{
	class MainClass
	{
		public static void Main(string[] args)
		{
            foreach (var file in Directory.GetFiles(@"D:\Users\Moritz\Desktop\playtest2", "*.dem"))
            {
                Console.WriteLine("Parsing " + file);
                using (var input = File.OpenRead(args[0]))
                {
                    using (DemoParser p = new DemoParser(input))
                    {
                        p.ParseHeader();
                        p.ParseToEnd();
                    }
                }
            }
		}
	}
}
