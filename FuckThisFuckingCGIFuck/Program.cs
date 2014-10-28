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
			Database = new MongoDatastore(Assembly.GetEntryAssembly().GetName().Name);

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


			switch (req.Url.AbsolutePath) {
			case "/upload":
				HandleDemoUpload(context, req);
				break;
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

		static void HandleDemoUpload(HttpListenerContext context, HttpListenerRequest req)
		{
			var multipart = new HttpMultipart(req.InputStream, req.ContentType.Substring(MULTIPART_PREFIX.Length), req.ContentEncoding, "demo");
			multipart.ReadBoundary();
			var eMapImage = multipart.ReadNextElement(5 * 1024 * 1024);
			var ePosX = multipart.ReadNextElement(256);
			var ePosY = multipart.ReadNextElement(256);
			var eScale = multipart.ReadNextElement(256);
			// else no watermark checked, demo is now
			var eDemo = multipart.ReadNextElement(256);
			// shall be null
			if (eMapImage.Name != "map")
				throw new Exception("emapimg invalid");
			if (ePosX.Name != "posX")
				throw new Exception("eposx invalid");
			if (ePosY.Name != "posY")
				throw new Exception("eposy invalid");
			if (eScale.Name != "scale")
				throw new Exception("escale invalid");
			if (eDemo != null)
				throw new Exception("lolwat");
			context.Response.ContentType = "application/json";
			var writer = new StreamWriter(context.Response.OutputStream);
			writer.AutoFlush = true;
			Image map;
			try {
				using (var stream = new MemoryStream(eMapImage.Data))
					map = Image.FromStream(stream);
				if (map == null)
					throw new ArgumentNullException("map");
				if (( map.Width != 1024 ) || ( map.Height != 1024 )) {
					writer.WriteLine(JsonConvert.SerializeObject(new {
						result = "mapsize"
					}));
					context.Response.Close();
					return;
				}
			}
			catch (Exception) {
				writer.WriteLine(JsonConvert.SerializeObject(new {
					result = "mapformat"
				}));
				context.Response.Close();
				return;
			}
			float posX, posY, scale;
			if (Single.TryParse(ePosX.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out posX) && Single.TryParse(ePosY.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out posY) && Single.TryParse(eScale.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out scale)) {
				var demoFileName = Guid.NewGuid().ToString() + ".dem";
				Database.StoreFile(req.InputStream, demoFileName);
				var s = Database.RetrieveFile(demoFileName);
				Heatmap h = new Heatmap(Database, s, posX, posY, scale);
				var ana = h.ParseHeaderOnly();
				ana.DemoFile = demoFileName;
				Database.Save(ana);
				writer.WriteLine(JsonConvert.SerializeObject(new {
					result = "success",
					id = ana.ID
				}));
				context.Response.Close();
				h.ParseTheRest();
			}
			else
				throw new Exception("invalid form data (posx, posy or scale) ");
		}

		static void HandleFile(HttpListenerContext context, HttpListenerRequest req)
		{
			if (req.QueryString["path"] != null && !Database.FileExists(req.QueryString["path"])) {
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
		static JsonWriterSettings jsonSettings = new JsonWriterSettings(false, Encoding.UTF8, GuidRepresentation.Standard, true, " ", Environment.NewLine, JsonOutputMode.Strict, null);
		#else
		static JsonWriterSettings jsonSettings = new JsonWriterSettings(false, Encoding.UTF8, GuidRepresentation.Standard, false, "", "", JsonOutputMode.Strict, null);
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
				var writer = new StreamWriter(context.Response.OutputStream);
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
