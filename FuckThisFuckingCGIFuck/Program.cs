using System;
using System.Net;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Collections.Concurrent;
using System.Reflection;
using HeatmapGenerator;
using Newtonsoft.Json;
using System.Globalization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.IO;
using System.Text;
using AbstractDatastore;

namespace FuckThisFuckingCGIFuck
{
	class MainClass
	{
		private static MongoDatastore Database;

		// singlethreaded for rate limiting
		// it's not a bug, it's a feature!!!!!!!!1111111oneoneoneeleven #believe
		public static void Main(string[] args)
		{
			Database = new MongoDatastore("DemoInfo");

			var listener = new HttpListener();
			listener.Prefixes.Add("http://localhost:5500/");
			listener.Start();

			for (int i = 0; i < int.Parse(args[0]); i++) {
				Thread t = new Thread(new ParameterizedThreadStart(Work));
				t.IsBackground = true;
				t.CurrentCulture = CultureInfo.InvariantCulture;
				t.CurrentUICulture = CultureInfo.InvariantCulture;
				t.Name = "Webserver-Listen-Slave #"+(i+1);
				t.Start((object)listener);
			}

			Thread.Sleep(int.MaxValue);
		}

		public static void Work(object data)
		{
			var listener = (HttpListener)data;
			while (true) {
                HttpListenerContext ctx = null;
				try
				{
                    ctx = listener.GetContext();
					HandleRequest(ctx);
				}
				catch(Exception e)
				{
					Console.WriteLine(e);
                    try
                    {
                        if(ctx != null)
                            ctx.Response.Abort();
                    }
                    catch
                    {}
				}
			}
		}

		private static string MULTIPART_PREFIX = "multipart/form-data; boundary=";
		static void HandleRequest(HttpListenerContext context)
		{
			var req = context.Request;
			context.Response.ContentType = "application/json";
			context.Response.KeepAlive = false;

			#if DEBUG
			context.Response.AddHeader("Access-Control-Allow-Origin", "*");
			#endif

			if (req.HttpMethod == "OPTIONS")
			{
				context.Response.Close();
				return;
			}

			switch (req.Url.AbsolutePath) {
			case "/file":
				HandleFile(context, req);
				break;
			case "/fileExists":
				HandleFileExists(context, req);
				break;
			case "/analysis":
				HandleAnalysis(context, req);
				break;
			default:
				Handle404(context, req);
				break;
			}
		}

		static void HandleFile(HttpListenerContext context, HttpListenerRequest req)
		{
			if (req.QueryString["path"] == null || !Database.FileExists(req.QueryString["path"])) {
				Write404("db://" + req.QueryString["path"], context, req);
				return;
			}

			context.Response.ContentType = Database.GetFileType(req.QueryString["path"]);
			context.Response.ContentLength64 = Database.GetFileSize(req.QueryString["path"]);

			Database.RetrieveFile(req.QueryString["path"]).CopyTo(context.Response.OutputStream);

			context.Response.OutputStream.Flush();

			context.Response.Close();
		}

		static void HandleFileExists(HttpListenerContext context, HttpListenerRequest req)
		{
			var writer = new StreamWriter(context.Response.OutputStream);

			string resultFile = Database.GetFilenameByHash(context.Request.QueryString["md5"]);
			var analysis = Database.LoadBy<DemoAnalysis>("DemoFile", resultFile);

			if(resultFile == null)
			{
				writer.WriteLine(JsonConvert.SerializeObject(new {
					result = "notFound",
				}));
			}
			else
			{
				if (analysis == null) {
					writer.WriteLine(JsonConvert.SerializeObject(new {
						result = "found",
						fileName = resultFile
				}));
				} else {
					writer.WriteLine(JsonConvert.SerializeObject(new {
						result = "found",
						fileName = resultFile,
						analysisResult = analysis
					}));
				}
			}


			writer.Flush();
			context.Response.Close();
		}

		#if DEBUG
		static JsonWriterSettings jsonSettings = new JsonWriterSettings { CloseOutput = false, GuidRepresentation = GuidRepresentation.Standard, Indent = true, IndentChars = " ", NewLineChars = Environment.NewLine, OutputMode = JsonOutputMode.Strict };
		#else
		static JsonWriterSettings jsonSettings = new JsonWriterSettings { CloseOutput = false, GuidRepresentation = GuidRepresentation.Standard, Indent = false, IndentChars = "", NewLineChars = "", OutputMode = JsonOutputMode.Strict };
		#endif
		static void HandleAnalysis(HttpListenerContext context, HttpListenerRequest req)
		{
			if (req.QueryString["id"] == null) {
				Write404("analysis://[no analysis given]", context, req);
				return;
			}

			var analysis = Database.LoadByObjectID<DemoAnalysis>(req.QueryString["id"]).ToBsonDocument();
			analysis.Remove("DemoFile");

			if (analysis == null) {
				Write404("analysis://" + req.QueryString["id"], context, req);
			} else {
				var writer = new StreamWriter(context.Response.OutputStream, Encoding.UTF8);
				writer.Write(analysis.ToJson(jsonSettings));
				writer.Flush();
				context.Response.Close();
			}
		}


		static void Handle404(HttpListenerContext context, HttpListenerRequest req)
		{
			Write404(req.RawUrl, context, req);
		}

		static void Write404(string fileName, HttpListenerContext context, HttpListenerRequest req)
		{
			var writer = new StreamWriter(context.Response.OutputStream);
			context.Response.StatusCode = 404;

			writer.WriteLine(JsonConvert.SerializeObject(new {
				result = "error",
				error = new {
					HTTPError = 404,
					Message = "Page \""+fileName+"\" not found"
				}
			}));

			writer.Flush();
			context.Response.Close();
		}
	}
}
