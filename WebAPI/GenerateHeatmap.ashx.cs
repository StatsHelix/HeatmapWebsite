
namespace WebAPI
{
	using System;
	using System.IO;
	using System.Drawing;
	using System.Linq;
	using System.Web;
	using System.Web.UI;
	using Newtonsoft.Json;
	using HeatmapGenerator;

	public class GenerateHeatmap : System.Web.IHttpHandler
	{
		public bool IsReusable { get { return false; } }

		public void ProcessRequest (HttpContext context)
		{
			var req = context.Request;
			var map = Image.FromStream(req.Files["map"].InputStream);

			if ((map.Width != 1024) || (map.Height != 1024)) {
				context.Response.Output.WriteLine(JsonConvert.SerializeObject(new { result = "mapsize" }));
				return;
			}

			float posX, posY, scale;
			if (Single.TryParse(req.Form["posX"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out posX)
				&& Single.TryParse(req.Form["posY"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out posY)
				&& Single.TryParse(req.Form["scale"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out scale)) {

				var requestId = Guid.NewGuid().ToString();
				var basepath = Path.Combine("/usr/share/nginx/www-demo/static/results", requestId);
				Directory.CreateDirectory(basepath);
				var files = new Heatmap(req.Files["demo"].InputStream, posX, posY, scale).Parse();
				foreach (var item in files) {
					item.Value.Save(Path.Combine(basepath, item.Key + ".png"), System.Drawing.Imaging.ImageFormat.Png);
				}
				map.Save(Path.Combine(basepath, "map.png"), System.Drawing.Imaging.ImageFormat.Png);

				context.Response.ContentType = "application/json";
				context.Response.Output.WriteLine(JsonConvert.SerializeObject(new { result = "success", id = requestId }));
			} else
				throw new Exception("invalid form data (posx, posy or scale)");
		}
	}
}

