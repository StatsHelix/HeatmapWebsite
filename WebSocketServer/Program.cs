using System;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Bson.IO;

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
				Debug.WriteLine(((object)request).ToString(), "HandleClient: received request");
				await RequestHandlers[(string)request.Status](session, request);
			}
		}

		private static readonly JsonWriterSettings MongoJsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
		private static async Task HandleUploadRequest(WebSocketSession session, dynamic request) {
			var alreadyUploaded = Database.GetFilenameByHash((string)request.MD5);
			if (alreadyUploaded != null) {
				var analysis = Database.LoadBy<DemoAnalysis>("DemoFile", alreadyUploaded);
				var doc = new BsonDocument();
				doc["Status"] = "AlreadyUploaded";
				doc["Analysis"] = analysis.ToBsonDocument();
				await session.SendTextMessage(doc.ToJson(MongoJsonSettings));
				return;
			}

			using (var ticket = q.EnterQueue()) {
				var getContext = ticket.GetContext();
				var clientQuery = session.ReceiveObject();

				while (true) {
					var completed = await Task.WhenAny(getContext, clientQuery,
						Task.Delay(TimeSpan.FromSeconds(5)));
					if (completed == getContext) {
						await getContext; // possible future use
						// got context, upload starts
						await session.SendObject(new { Status = "ReadyForUpload" });
						await clientQuery.WithTimeout(ClientReadTimeout); // possible future use
						Debug.WriteLine("omfg getting stream now");
						var uploadStream = await session.ReceiveBinaryMessage().WithTimeout(ClientReadTimeout);
						Debug.WriteLine("SHIT SHIT SHIT GOT THE STREAM EVERYTHING IS AWESOME");

						var demoFileName = Guid.NewGuid().ToString() + ".dem";
						using (var dbStoreStream = Database.StoreStream(demoFileName)) {
							var tee = new TeeStream(uploadStream, dbStoreStream); // upload to db WHILE PARSING :D
							var h = new Heatmap(Database, tee);
							h.OnRoundAnalysisFinished += async (analysis) => {
								var doc = new BsonDocument();
								doc["Status"] = "AnalysisProgress";
								doc["Analysis"] = analysis.ToBsonDocument();
								await session.SendTextMessage(doc.ToJson(MongoJsonSettings));
							};
							var ana = h.ParseHeaderOnly();
							ana.DemoFile = demoFileName;
							Database.Save(ana);

							var asDoc = new BsonDocument();
							asDoc["Status"] = "AnalysisStarted";
							asDoc["Analysis"] = ana.ToBsonDocument();
							await session.SendTextMessage(asDoc.ToJson(MongoJsonSettings));

							h.ParseTheRest();
						}

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
