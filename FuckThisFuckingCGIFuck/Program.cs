using System;
using System.Net;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Collections.Concurrent;
using HeatmapGenerator;
using Newtonsoft.Json;

namespace FuckThisFuckingCGIFuck
{
	class MainClass
	{
		// singlethreaded for rate limiting
		// it's not a bug, it's a feature!!!!!!!!1111111oneoneoneeleven #believe
		public static void Main(string[] args)
		{
			var listener = new HttpListener();
			listener.Prefixes.Add("http://localhost:5500/");
			listener.Start();
			while (true) {
				ProcessRequest(listener.GetContext());
			}
		}

		private static string MULTIPART_PREFIX = "multipart/form-data; boundary=";
		public static void ProcessRequest (HttpListenerContext context)
		{
			var req = context.Request;
		

			var multipart = new HttpMultipart(req.InputStream, req.ContentType.Substring(MULTIPART_PREFIX.Length), req.ContentEncoding);

			context.Response.ContentType = "application/json";

			var ele = multipart.ReadNextElement(100);



//
//			float posX, posY, scale;
//			if (Single.TryParse(req.Form["posX"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out posX)
//				&& Single.TryParse(req.Form["posY"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out posY)
//				&& Single.TryParse(req.Form["scale"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out scale)) {
//
//				var requestId = Guid.NewGuid().ToString();
//				var basepath = Path.Combine("/usr/share/nginx/www-demo/static/results", requestId);
//				Directory.CreateDirectory(basepath);
//				var files = new Heatmap(req.Files["demo"].InputStream, posX, posY, scale).Parse();
//				foreach (var item in files) {
//					item.Value.Save(Path.Combine(basepath, item.Key + ".png"), System.Drawing.Imaging.ImageFormat.Png);
//				}
//
//				try {
//					var map = Image.FromStream(req.Files["map"].InputStream);
//					if (map == null)
//						throw new ArgumentNullException("map");
//					if ((map.Width != 1024) || (map.Height != 1024)) {
//						context.Response.Output.WriteLine(JsonConvert.SerializeObject(new { result = "mapsize" }));
//						return;
//					}
//					map.Save(Path.Combine(basepath, "map.png"), System.Drawing.Imaging.ImageFormat.Png);
//				} catch (Exception) {
//					context.Response.Output.WriteLine(JsonConvert.SerializeObject(new { result = "mapformat" }));
//				}
//
//				context.Response.Output.WriteLine(JsonConvert.SerializeObject(new { result = "success", id = requestId }));
//			} else
//				throw new Exception("invalid form data (posx, posy or scale) " + req.Form.ToString());

			System.GC.Collect();
		}
	}
}
