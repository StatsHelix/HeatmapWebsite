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

        string filename = null;

        public ReplayViewer()
        {
            InitializeComponent();

            drawingBitmap = new Bitmap(1024,1024);
            g = Graphics.FromImage(drawingBitmap);



            OpenFileDialog diag = new OpenFileDialog();
            diag.DefaultExt = "*.dem";
            diag.Filter = "CS:GO Demo (*.dem)|*.dem"; 
            //diag.FileName = "C:\\VPiBP.dem";
            diag.ShowDialog();
            

            if (!File.Exists(diag.FileName))
            {
                MessageBox.Show("No valid file specified. ");
                throw new InvalidDataException();
            }

            var reader = File.OpenRead(diag.FileName);

            parser = new DemoParser(reader);

            parser.ParseDemo(false);


            LoadBackgroundInfo();

            timer1.Enabled = true;

            parser.TickDone += parser_TickDone;
            parser.MatchStarted += parser_MatchStarted;
        }

        void parser_MatchStarted(object sender, MatchStarted e)
        {
            g.Clear(Color.Transparent);
            this.Text = "LIVE!";
        }


        private void LoadBackgroundInfo()
        {
            //Okay, set the background-image. 
            var lines = File.ReadAllLines("overviews\\" + parser.Map + ".txt");

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
            if (!parser.ParseNextTick())
                drawingBitmap.Save(Path.GetRandomFileName() + ".png", System.Drawing.Imaging.ImageFormat.Png);
        }
    
        int i = 0;

        static Color col = Color.FromArgb(5, Color.OrangeRed);
        SolidBrush brush = new SolidBrush(col);
        void parser_TickDone(object sender, TickDone e)
        {
            //g.Clear(Color.Transparent);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            foreach(var player in parser.Players)
            {
                var p = MapPoint(player.Position);
                var p2 = p;
                p.X -= 1;
                p.Y -= 1;

                p2.X += 20;

                g.FillEllipse(brush, new Rectangle(p, new Size(3, 3)));
                //g.DrawString(player.Name, new Font("Calibri", 20), new SolidBrush(Color.Green), p2);

            }


            if (i % 500 == 0)
            {
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Image = drawingBitmap;
            }

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
