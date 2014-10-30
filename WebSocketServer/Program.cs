using System;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using AbstractDatastore;
using ActuallyWorkingWebSockets;
using HeatmapGenerator;

namespace WSS
{
	class MainClass
	{
		private static readonly int Slots = 3; // this is where we can tweak
		private static UploadQueue q;
		private static IDatastore Database;

		public static void Main(string[] args)
		{
			#if !DEBUG
			Trace.Listeners.Add(new SyslogTrace.SyslogTraceListener("CSGO-A: WSS"));
			#else
			Trace.Listeners.Add(new ConsoleTraceListener(true));
			#endif

			Database = new MongoDatastore("DemoInfo");

			var contexts = new List<UploadWorkerContext>(Slots);
			for (int i = 0; i < Slots; i++)
				contexts.Add(new UploadWorkerContext { });
			q = new UploadQueue(contexts);

			var server = new WebSocketServer(
				new IPEndPoint(IPAddress.Any, 5501)) { ClientHandler = HandleClient };
			var serverTask = server.RunAsync();
			Console.ReadKey(true);
			server.RequestShutdown();
			serverTask.Wait();
		}

		private static readonly Dictionary<string, Func<WebSocketSession, dynamic, Task>> RequestHandlers =
			new Dictionary<string, Func<WebSocketSession, dynamic, Task>> {
			{ "UploadDemo", HandleUploadRequest },
		};
		private static readonly TimeSpan ClientReadTimeout = TimeSpan.FromSeconds(1);
		private static async Task HandleClient(WebSocketSession session)
		{
			while (true) {
				var request = await session.ReceiveObject().WithTimeout(ClientReadTimeout);
				await RequestHandlers[request.Status as string](session, request);
			}
		}

		private static async Task HandleUploadRequest(WebSocketSession session, dynamic request) {
			var alreadyUploaded = Database.GetFilenameByHash(request.MD5 as string);
			if (alreadyUploaded != null) {
				var analysis = Database.LoadBy<DemoAnalysis>("DemoFile", alreadyUploaded);
				await session.SendObject(new {
					Status = "AlreadyUploaded",
					Id = analysis.ID,
				});
			}

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
						var uploadInfo = await clientQuery.WithTimeout(ClientReadTimeout);
						var uploadStream = await session.ReceiveBinaryMessage().WithTimeout(ClientReadTimeout);

						var demoFileName = Guid.NewGuid().ToString() + ".dem";
						var dbStoreStream = Database.StoreStream(demoFileName);
						var tee = new TeeAndProgressStream(uploadStream, dbStoreStream); // upload to db WHILE PARSING :D
						tee.OnProgress += async (pos) => await session.SendObject(new { Status = "UploadProgress", Position = pos });
						var h = new Heatmap(Database, tee);
						var ana = h.ParseHeaderOnly();
						ana.DemoFile = demoFileName;
						Database.Save(ana);
						await session.SendObject(new {
							Status = "AnalysisStarted",
							Id = ana.ID
						});
						h.ParseTheRest();

						await session.SendObject(new { Status = "UploadComplete" });
						break;
					} else if (completed == clientQuery)
						// client query? wat? (we don't expect this)
						// TODO: handle cancellation and stuff
						throw new System.IO.InvalidDataException("protocol violation");
					else {
						// timeout, quick ping
						await session.Ping().WithTimeout(ClientReadTimeout);
						// still there. send them their current position.
						await session.SendObject(new { Status = "InQueue", QueuePosition = ticket.QueuePosition });
					}
				}
			}
		}
	}
}
