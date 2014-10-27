using System;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ActuallyWorkingWebSockets;

namespace WSS
{
	class MainClass
	{
		private static readonly int Slots = 3; // this is where we can tweak
		private static UploadQueue q;

		public static void Main(string[] args)
		{
			var contexts = new List<UploadWorkerContext>(Slots);
			for (int i = 0; i < Slots; i++)
				contexts.Add(new UploadWorkerContext());
			q = new UploadQueue(contexts);

			var server = new WebSocketServer(
				new IPEndPoint(IPAddress.Any, 5501)) { ClientHandler = HandleClient };
			var serverTask = server.RunAsync();
			Console.ReadKey(true);
			server.RequestShutdown();
			serverTask.Wait();
		}

		private static readonly TimeSpan ClientReadTimeout = TimeSpan.FromSeconds(1);
		private static async Task HandleClient(WebSocketSession session)
		{
			await session.SendTextMessage("ohai");
			Console.WriteLine("Upload request, getting ticket: {0}", await session.ReceiveTextMessage());
			using (var ticket = q.EnterQueue()) {
				var getContext = ticket.GetContext();
				var clientQuery = session.ReceiveObject();

				while (true) {
					var completed = await Task.WhenAny(getContext, clientQuery,
						Task.Delay(TimeSpan.FromSeconds(5)));
					if (completed == getContext) {
						var context = await getContext;
						// got context, upload starts
						await session.SendObject(new { Status = "ReadyForUpload" });
						//var uploadStream = await session.ReceiveBinaryMessage().WithTimeout(ClientReadTimeout);
						var response = await clientQuery.WithTimeout(ClientReadTimeout);
						Console.WriteLine("Got upload: {0}", response.Status);
						await session.SendObject(new { Status = "UploadComplete" });
						break;
					} else if (completed == clientQuery)
						// client query? wat? (we don't expect this)
						// TODO: handle cancellation and stuff
						throw new System.IO.InvalidDataException("protocol violation");
					else {
						// timeout, quick ping
						Debug.WriteLine("ping", session.GetHashCode().ToString());
						await session.Ping().WithTimeout(ClientReadTimeout);
						Debug.WriteLine("pong", session.GetHashCode().ToString());
					}
				}
			}
		}
	}
}
