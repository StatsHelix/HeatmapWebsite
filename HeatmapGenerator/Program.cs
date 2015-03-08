using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading;
using AbstractDatastore;

namespace HeatmapGenerator
{
	class MainClass
	{
        static DemoAnalysis currentAna = null;


        static MongoDatastore Database = new MongoDatastore("DemoInfo");

		public static void Main (string[] args)
		{
            if (args.Length != 0 && args[0] == "--reparse")
            {
                Reparse();
                return;
            }
            if (args.Length != 0 && args[0] == "--massparse")
            {
                Massparse();
                return;
            }
            else if (args.Length != 0 && File.Exists(args[0]))
            {
                Heatmap h = new Heatmap(
                   Database,
                   File.OpenRead(args[0]),
                    null
                    );

                h.ParseHeaderOnly();
                var result = h.ParseTheRest();
                Database.Save<DemoAnalysis>(result);
            }
            else if (args.Length != 0 && Directory.Exists(args[0]))
            {
                List<string> urls = new List<string>();

                foreach (var file in Directory.GetFiles(args[0], "*.dem"))
                {
                    Console.WriteLine("Parsing file " + file);

                    Heatmap h = new Heatmap(
                       Database,
                       File.OpenRead(file),
                        null
                        );

                    h.ParseHeaderOnly();
                    var result = h.ParseTheRest();

                    result.DemoFile = Path.GetFileName(file);

                    Database.Save<DemoAnalysis>(result);


                    
                    Console.WriteLine("Saved as " + result.ID);

                    urls.Add(result.ID.ToString());
                }

                Console.WriteLine("URL: ");
                Console.WriteLine(string.Join(",", urls));
            }
            else 
            {
                Console.WriteLine("Args: Either --reparse, or a file. ");
            }

            
		}

        private static void Reparse()
        {
            foreach (var ana in Database.LoadAll<DemoAnalysis>())
            {
                Console.WriteLine("Analysis: " + ana.ID.ToString());
                Console.WriteLine("Opening file " + ana.DemoFile);

                Console.WriteLine("Old progress: " + ana.Progress);

                currentAna = ana;

                if (!ana.IsFinished)
                {
                    //Console.WriteLine("removing...");
                    //Database.RemoveByObjectID<DemoAnalysis>(ana.ID.AsObjectId);
                    continue;
                }

                if (ana.Version == 3)
                {
                    continue;
                }

                Thread t = new Thread(new ThreadStart(ParseDemo));
                t.IsBackground = true;
                t.Start();

                for (int i = 0; i < 30; i++)
                {
                    Console.Write(i + ",");

                    if (!t.IsAlive)
                        break;

                    System.Threading.Thread.Sleep(1000);
                }

                Console.Write("done! ");

                if (t.IsAlive)
                {
                    Console.Write("Killing!");
                    t.Abort();
                }

                Console.WriteLine();


                var result = currentAna;

                Console.WriteLine("Saving Analysis: " + result.ID.ToString());

                ana.Version = 3;
                Database.Save<DemoAnalysis>(result);
            }
        }


        private static void Massparse()
        {
            Dictionary<string, List<string>> maps = new Dictionary<string, List<string>>();

            foreach (var ana in Database.LoadAll<DemoAnalysis>())
            {
                if (!ana.IsFinished)
                {
                    continue;
                }

                if (!maps.ContainsKey(ana.Metadata.MapName)) {
                    maps[ana.Metadata.MapName] = new List<string>();
                }

                maps[ana.Metadata.MapName].Add(ana.ID.ToString());
            }

            foreach(var map in maps) {
                Console.WriteLine("[{0}](http://demo.ehvag.de/#{1})", map.Key, string.Join(",", map.Value));
            }
        }

        static void ParseDemo()
        {
            var ana = currentAna;

            ana.Participants.Clear();
            ana.Progress = 0;
            ana.Rounds = new System.Collections.Generic.List<RoundEventMap>();
            ana.IsFinished = false;

            Heatmap h = new Heatmap(
                Database,
                File.OpenRead(Path.Combine("demo/", ana.DemoFile)),
                ana.Overview,
                ana
            );


            h.ParseHeaderOnly();
            var result = h.ParseTheRest();

            currentAna = result;
        }
	}
}
