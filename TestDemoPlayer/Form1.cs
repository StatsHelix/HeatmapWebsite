using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DemoInfo;
using System.Globalization;

namespace TestDemoPlayer
{
    public partial class ReplayViewer : Form
    {
		DemoParser parser = null;

        float mapX, mapY, scale;

        Bitmap drawingBitmap;
        Graphics g;

        public ReplayViewer()
        {
            InitializeComponent();

            drawingBitmap = new Bitmap(1024,1024);
            g = Graphics.FromImage(drawingBitmap);



            OpenFileDialog diag = new OpenFileDialog();
            diag.DefaultExt = "*.dem";
            diag.Filter = "CS:GO Demo (*.dem)|*.dem"; 
			diag.FileName = "~/.steam/steam/SteamApps/common/Counter-Strike Global Offensive/csgo/replays/";
            diag.ShowDialog();
            

            if (!File.Exists(diag.FileName))
            {
                MessageBox.Show("No valid file specified. ");
				this.Close ();
				Environment.Exit (0);
            }

            var reader = File.OpenRead(diag.FileName);

            parser = new DemoParser(reader);

            parser.ParseDemo(false);


            LoadBackgroundInfo();

            timer1.Enabled = true;

            parser.TickDone += parser_TickDone;
            parser.MatchStarted += parser_MatchStarted;
			parser.PlayerKilled += HandlePlayerKilled;
        }

        void HandlePlayerKilled (object sender, PlayerKilled e)
        {
			this.Text =  (
				String.Format(
					"{0} ({1}hp) got killed by {2} ({3}hp) with an {4} ({5})", 
					e.DeathPerson.Name, 
					e.DeathPerson.HP, 
					e.Killer.Name, 
					e.Killer.HP, 
					e.Weapon.Weapon.ToString(),
					e.Headshot ? "headshot" : "bodyshot"
				)
			);
        }

        void parser_MatchStarted(object sender, MatchStarted e)
        {
            g.Clear(Color.Transparent);
            this.Text = "LIVE!";
        }


        private void LoadBackgroundInfo()
        {
            //Okay, set the background-image. 
			var lines = File.ReadAllLines(Path.Combine("overviews", parser.Map + ".txt"));

            var file = lines
                .First(a => a.Contains("\"material\""))
                .Split('"')[3];

            if (File.Exists(file + "_radar_spectate.png"))
                file += "_radar_spectate.png";
            else if (File.Exists(file + "_radar.png"))
                file += "_radar.png";
            else
                file += ".png";

            mapX = float.Parse(lines
                .First(a => a.Contains("\"pos_x\""))
                .Split('"')[3], CultureInfo.InvariantCulture);
            mapY = float.Parse(lines
                .First(a => a.Contains("\"pos_y\""))
                .Split('"')[3], CultureInfo.InvariantCulture);
            scale = float.Parse(lines
                .First(a => a.Contains("\"scale\""))
                .Split('"')[3], CultureInfo.InvariantCulture);

            Bitmap image = new Bitmap(file);

            pictureBox1.BackgroundImage = image;
            pictureBox1.BackgroundImageLayout = ImageLayout.Zoom;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
			if (!parser.ParseNextTick ()) {
				timer1.Enabled = false;
				this.Close ();
			}
        }
    
        int i = 0;

		static Color col1 = Color.FromArgb(255, Color.OrangeRed);
		static Color col2 = Color.FromArgb(255, Color.CornflowerBlue);
		SolidBrush brush1 = new SolidBrush(col1);
		SolidBrush brush2 = new SolidBrush(col2);
        void parser_TickDone(object sender, TickDone e)
        {
			if (i++ % 16 != 0)
			{
				return;
			}

            g.Clear(Color.Transparent);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			foreach(var player in parser.Players.Values)
            {
				var p = MapPoint(player.LastAlivePosition);
				var endPoint = p;
				var p2 = p;
				var p3 = p;
				p2.X -= 7;
				p2.Y -= 7;

				p3.X += 20;

				Brush brush = null;

				if (player.IsAlive) {
					brush = player.Team == Team.Terrorist ? brush1 : brush2;
				} else {
					brush = new SolidBrush (Color.Red);
				}

				endPoint.X += (int)(Math.Sin ((player.ViewDirectionX / 360) * 2 * Math.PI) * 20);
				endPoint.Y += (int)(Math.Cos ((player.ViewDirectionX / 360) * 2 * Math.PI) * 20);

				g.FillEllipse(brush , new Rectangle(p2, new Size(15, 15)));
				g.DrawString(player.Name + " | " + player.HP.ToString(), new Font(FontFamily.GenericSansSerif, 14), brush, p3);
				g.DrawLine (new Pen (brush, 3), p, endPoint);

            }


                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Image = drawingBitmap;

        }

        public Point MapPoint(Vector vec)
        {
            return new Point(
                (int)((vec.X - mapX) / scale),
                (int)((mapY - vec.Y) / scale)
            );
        }
    }
}
