
namespace WebAPI
{
	using System;
	using System.Web;
	using System.Web.UI;
	using Newtonsoft.Json;

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

				// TODO: invoke demo parser [API pls]

				context.Response.ContentType = "application/json";
				context.Response.Output.Write(JsonConvert.SerializeObject(new {
					FlashMap = "result/flashmap.png",
					DecoyMap = "result/decoymap.png",
					SmokeMap = "result/smokemap.png",
					HEMap = "result/hemap.png",
					MolotovMap = "result/molotovmap.png",
				}));
			} else
				throw new Exception("invalid form data (posx, posy or scale)");
		}
	}
}

