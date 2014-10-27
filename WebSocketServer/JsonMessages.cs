using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ActuallyWorkingWebSockets;

namespace WSS
{
	public static class JsonMessages
	{
		public static Task SendObject(this WebSocketSession session, object o)
		{
			return session.SendTextMessage(JsonConvert.SerializeObject(o));
		}

		public static async Task<dynamic> ReceiveObject(this WebSocketSession session)
		{
			return JObject.Parse(await session.ReceiveTextMessage());
		}
	}
}

