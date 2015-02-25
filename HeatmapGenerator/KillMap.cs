using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeatmapGenerator
{
    public class KillMap
    {
        public List<Kill> Kills { get; private set; }

        public KillMap()
        {
            Kills = new List<Kill>();
        }


        public void AddPoint(Kill k)
        {
            Kills.Add(k);
        }
    }

    public class Kill
    {
        public string Weapon { get; set; }
        public int Headshot { get; set; }
        public Vector2 VictimPosition { get; set; }
        public Vector2 KillerPosition { get; set; }
        public int KillerTeam { get; set; }
        public int VictimTeam { get; set; }
    }
}
