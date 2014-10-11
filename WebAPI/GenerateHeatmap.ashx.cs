
namespace WebAPI
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Web;
	using System.Web.UI;
	using Newtonsoft.Json.Linq;
	using HeatmapGenerator;

	public class GenerateHeatmap : System.Web.IHttpHandler
	{
		public bool IsReusable { get { return false; } }

		public void ProcessRequest (HttpContext context)
		{
			var req = context.Request;
			var map = req.Files["map"];
			var mapData = new byte[map.ContentLength];
			int mapDataOffset = 0;
			while (mapDataOffset != mapData.Length) {
				var readData = map.InputStream.Read(mapData, mapDataOffset, mapData.Length);
				if (readData <= 0)
					// wtf? InputStream is shorter than ContentLength?
					throw new Exception("gr8 b8 m8 i r8 8/8 " + mapDataOffset + " " + mapData.Length);
				mapDataOffset += readData;
			}

			float posX, posY, scale;
			if (Single.TryParse(req.Form["posX"], out posX)
			    && Single.TryParse(req.Form["posY"], out posY)
			    && Single.TryParse(req.Form["scale"], out scale)) {

				var requestId = Guid.NewGuid().ToString();
				var basepath = Path.Combine("/usr/share/nginx/www-demo/static/results", requestId);
				Directory.CreateDirectory(basepath);
				var files = new Heatmap(req.Files["demo"].InputStream, posX, posY, scale).Parse();
				foreach (var item in files) {
					item.Value.Save(Path.Combine(basepath, item.Key + ".png"), System.Drawing.Imaging.ImageFormat.Png);
				}

				context.Response.ContentType = "application/json";
				var webBase = "/results/" + requestId + "/";
				context.Response.Output.WriteLine(new JObject(files.Select(pair => new JProperty(pair.Key, webBase + pair.Key + ".png"))));
			} else
				throw new Exception("invalid form data (posx, posy or scale)");
		}
	}
}

