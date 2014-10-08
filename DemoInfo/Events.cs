using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoInfo
{
    public class HeaderParsed : EventArgs
    {
        public DemoHeader Header { get; private set; }

        public HeaderParsed(DemoHeader header)
        {
            this.Header = header;
        }
    }

    public class TickDone : EventArgs
    {
    }

    public class MatchStarted : EventArgs
    { }
}
