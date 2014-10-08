using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoInfo
{
    class Program
    {
        static void Main(string[] args)
        {
            DemoParser parser = new DemoParser(File.OpenRead("C:/VPiBP.dem"));
            parser.ParseDemo(true);
        }
    }
}
