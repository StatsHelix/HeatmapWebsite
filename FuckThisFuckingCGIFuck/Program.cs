﻿using System;
using System.Net;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Collections.Concurrent;
using HeatmapGenerator;
using Newtonsoft.Json;
using System.Globalization;

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
			var multipart = new HttpMultipart(req.InputStream, req.ContentType.Substring(MULTIPART_PREFIX.Length), req.ContentEncoding, "demo");
			multipart.ReadBoundary();
			var eMapImage = multipart.ReadNextElement(5 * 1024 * 1024);
			var ePosX = multipart.ReadNextElement(256);
			var ePosY = multipart.ReadNextElement(256);
			var eScale = multipart.ReadNextElement(256);
			var eWatermark = multipart.ReadNextElement(256);
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
			if (eWatermark.Name != "watermark")
				throw new Exception("ewatermark invalid");
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
				var requestId = Guid.NewGuid().ToString();
				var basepath = Path.Combine("/usr/share/nginx/www-demo/static/results", requestId);
				Directory.CreateDirectory(basepath);
				var files = new Heatmap(req.InputStream, posX, posY, scale).Parse();
				foreach (var item in files) {
					item.Value.Save(Path.Combine(basepath, item.Key + ".png"), System.Drawing.Imaging.ImageFormat.Png);
				}
				map.Save(Path.Combine(basepath, "map.png"), System.Drawing.Imaging.ImageFormat.Png);
				writer.WriteLine(JsonConvert.SerializeObject(new {
					result = "success",
					id = requestId
				}));
				context.Response.Close();
			}
			else
				throw new Exception("invalid form data (posx, posy or scale) ");
		}
	}
}
