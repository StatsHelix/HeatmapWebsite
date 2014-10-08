using DemoInfo.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoInfo.DP.Handler
{
    class GameEventHandler : IMessageParser
    {
        public bool CanHandleMessage(ProtoBuf.IExtensible message)
        {
            return message is CSVCMsg_GameEventList || message is CSVCMsg_GameEvent;
        }

        List<CSVCMsg_GameEventList.descriptor_t> descriptors = null;

        List<string> occuredEvents = new List<string>();

        public void ApplyMessage(ProtoBuf.IExtensible message, DemoParser parser)
        {
            if (message is CSVCMsg_GameEventList)
            {
                descriptors = ((CSVCMsg_GameEventList)message).descriptors;
                return;
            }

            var rawEvent = (CSVCMsg_GameEvent)message;

            var eventDescriptor = descriptors[rawEvent.eventid];

            if (!occuredEvents.Contains(eventDescriptor.name))
            {
                occuredEvents.Add(eventDescriptor.name);
                Debug.WriteLine(eventDescriptor.name);
            }

            if (eventDescriptor.name == "round_announce_match_start")
                parser.RaiseMatchStarted();

            if (eventDescriptor.name == "hltv_status")
                return;

        }

        public int GetPriority()
        {
            return 0;
        }
    }
}
