using System;
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

            foreach (var ana in Database.LoadAll<DemoAnalysis>())
            {
                Console.WriteLine("Analysis: " + ana.ID.ToString());
                Console.WriteLine("Opening file " + ana.DemoFile);

                Console.WriteLine("Old progress: " + ana.Progress);

                currentAna = ana;

                if (!ana.IsFinished || ana.Version == 2)
                {
                    Console.WriteLine("skipping...");
                    continue;
                }

                Thread t = new Thread(new ThreadStart(ParseDemo));
                t.IsBackground = true;
                t.Start();

                for(int i = 0; i < 30; i++)
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

                ana.Version = 2;
                Database.Save<DemoAnalysis>(result);
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
