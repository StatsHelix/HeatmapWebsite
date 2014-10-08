using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoInfo
{
    public class Player
    {
        public string Name { get; set; }
        public int SteamID { get; set; }
        public Vector Position { get; set; }
        public int EntityID { get; set; }

        public Team Team { get; set; }

    }
    public enum Team {
        Terrorist = 1,
        CounterTerrorist = 2,
        Spectator = 3
    }
}
