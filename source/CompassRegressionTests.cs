using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace KavrosClueHelper
{
    public static class CompassRegressionTests
    {
        private static int assertions;
        private static void Check(bool ok,string message){assertions++;if(!ok)throw new Exception("FAILED: "+message);}
        public static Bitmap Fixture(string assets,double angle,bool solved=false)
        {
            Bitmap image=new Bitmap(300,350);
            using(Graphics g=Graphics.FromImage(image))
            {
                g.Clear(Color.FromArgb(28,42,50));g.FillRectangle(Brushes.Wheat,40,40,176,259);
                using(Bitmap north=new Bitmap(Path.Combine(assets,"compass","north.png")))g.DrawImageUnscaled(north,118,60);
                GraphicsState state=g.Save();g.TranslateTransform(40+89,40+137);
                if(solved)
                {using(Pen pen=new Pen(Color.FromArgb(95,95,95),6)){g.DrawLine(pen,-23,-23,21,21);g.DrawLine(pen,-24,18,20,-26);}}
                else
                {
                    g.RotateTransform((float)(-angle*180/Math.PI));
                    Point[] arrow={new Point(-70,-6),new Point(20,-6),new Point(20,-22),new Point(70,0),new Point(20,22),new Point(20,6),new Point(-70,6)};
                    g.FillPolygon(Brushes.Black,arrow);
                }
                g.Restore(state);
            }
            return image;
        }
        public static int Run(string report)
        {
            try
            {
                string assets=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets"),dir=Path.GetDirectoryName(Path.GetFullPath(report));
                var vision=new CompassVision(assets);
                foreach(string line in File.ReadAllLines(Path.Combine(assets,"compass","calibration-off.csv")))
                {
                    double[] row=line.Split(new[] { ',' }).Select(p=>Double.Parse(p,CultureInfo.InvariantCulture)).ToArray();
                    CompassReading read=vision.Calibrate(row[0],false);
                    double trueMid=CompassMath.Normalize(row[2]+CompassMath.Normalize(row[3]-row[2])/2);
                    Check(Math.Abs(CompassMath.Difference(read.Angle,trueMid))<=read.Error+0.00001,"no-AA calibration bin retains its true angle");
                }
                foreach(string line in File.ReadAllLines(Path.Combine(assets,"compass","calibration-msaa.csv")))
                {
                    double[] row=line.Split(new[] { ',' }).Select(p=>Double.Parse(p,CultureInfo.InvariantCulture)).ToArray();CompassReading read=vision.Calibrate(row[0],true);
                    Check(Math.Abs(CompassMath.Difference(read.Angle,Math.Atan2(-row[2],-row[1])))<0.00001,"MSAA calibration reproduces each reference sample");
                }
                foreach(double angle in new[]{0.0,Math.PI/4,Math.PI/2,Math.PI,Math.PI*1.5,Math.PI*1.75})
                using(Bitmap image=Fixture(assets,angle))
                {
                    DetectedPuzzle p=vision.Find(image,new Pixels(image));
                    Check(p!=null&&p.Kind==PuzzleKind.Compass,"anchored synthetic compass is recognised at "+angle);
                    Check(Math.Abs(CompassMath.Difference(p.Compass.Angle,angle))<Math.PI/12,"synthetic arrow quadrant/direction is correct");
                }
                using(Bitmap blank=Fixture(assets,0))
                {
                    using(Graphics g=Graphics.FromImage(blank))g.FillRectangle(Brushes.Wheat,44,92,171,171);
                    Check(vision.Find(blank,new Pixels(blank))==null,"a north letter on parchment without an arrow is rejected");
                    using(Graphics g=Graphics.FromImage(blank))g.FillRectangle(Brushes.Black,104,152,50,44);
                    Check(vision.Find(blank,new Pixels(blank))==null,"a black rectangle is not accepted as a compass arrow");
                    using(Graphics g=Graphics.FromImage(blank))g.FillRectangle(Brushes.Gray,104,152,50,44);
                    Check(vision.Find(blank,new Pixels(blank))==null,"a solid grey rectangle is not accepted as the arrived cross");
                }
                foreach(float scale in new[]{0.8F,8F/9F,1.25F,1.5F,2F})
                using(Bitmap original=Fixture(assets,0))
                using(Bitmap image=Pixels.Crop(original,new Rectangle(Point.Empty,original.Size),new Size((int)Math.Round(original.Width*scale),(int)Math.Round(original.Height*scale))))
                {DetectedPuzzle p=vision.Find(image,new Pixels(image));Check(p!=null&&p.Kind==PuzzleKind.Compass,"supported UI scale recognises the anchored compass: "+scale);}
                using(Bitmap solved=Fixture(assets,0,true))Check(vision.Find(solved,new Pixels(solved)).Compass.Solved,"X state recognised without inventing a direction");
                var session=new CompassSession(assets);Check(session.AllSpots.Count==486,"only the elite compass spot list is included");
                DateTime now=DateTime.UtcNow;PointF target=session.AllSpots[110];
                var a=new CompassPosition{Name="A",X=2400,Y=3300,Radius=0};var b=new CompassPosition{Name="B",X=3400,Y=3100,Radius=0};
                for(int i=0;i<3;i++)session.Observe(new CompassReading{Angle=CompassMath.Direction(a.Point,target),Error=0.002},now.AddMilliseconds(i*200));
                Check(session.Record(a,now.AddMilliseconds(400)),"stable reading can be recorded at an explicitly selected position");
                Check(session.Candidates.Contains(target),"first bearing keeps the true target");
                Check(!session.Record(a,now.AddMilliseconds(400)),"same position and direction cannot be duplicated");
                session.Observe(null,now.AddMilliseconds(500));
                Check(session.Stable(now.AddMilliseconds(500)),"one missed frame keeps the stable reading");
                for(int i=0;i<3;i++)session.Observe(new CompassReading{Angle=CompassMath.Direction(b.Point,target),Error=0.002},now.AddMilliseconds(600+i*200));
                Check(session.Record(b,now.AddMilliseconds(1000)),"second stable reading can be recorded");
                Check(session.Candidates.Contains(target)&&session.Candidates.Count<10,"two independent rays narrow the elite locations and retain the correct target");
                int count=session.Candidates.Count;session.Undo();Check(session.Candidates.Count>=count&&session.Bearings.Count==1,"undo recalculates candidates");
                session.NewClue();Check(session.Bearings.Count==0&&session.Candidates.Count==486&&!session.Stable(now),"new clue clears bearings and stale arrow data");
                for(int i=0;i<6;i++)session.Observe(new CompassReading{Angle=i*0.2,Error=0.002},now.AddMilliseconds(i*200));
                Check(!session.Stable(now.AddMilliseconds(1000)),"spinning or moving compass does not produce stable data");
                session.Observe(null,now);
                for(int i=0;i<3;i++)session.Observe(new CompassReading{Angle=0,Error=0.002},now.AddMilliseconds(i*200));
                Check(!session.Record(a,now.AddSeconds(3)),"stale readings cannot be recorded");
                session.NewClue();
                DateTime steady=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);
                double centre=1.2,step=0.6*Math.PI/180;
                for(int i=0;i<5;i++)session.Observe(new CompassReading{Angle=centre+(i%2==0?step:-step),Error=0.002},steady.AddMilliseconds(i*120));
                Check(session.Stable(steady.AddMilliseconds(480)),"+-0.6 deg jitter becomes stable");
                Check(session.Record(a,steady.AddMilliseconds(480))&&Math.Abs(CompassMath.Difference(session.Bearings[session.Bearings.Count-1].Angle,centre))<step,"record stores the circular mean of a jittered heading");
                session.NewClue();
                for(int i=0;i<2;i++)session.Observe(new CompassReading{Angle=0.4,Error=0.002},steady.AddMilliseconds(i*120));
                session.Observe(null,steady.AddMilliseconds(240));
                session.Observe(new CompassReading{Angle=0.4,Error=0.002},steady.AddMilliseconds(360));
                Check(session.Stable(steady.AddMilliseconds(360)),"one null frame doesn't reset");
                session.NewClue();
                for(int i=0;i<3;i++)session.Observe(new CompassReading{Angle=0.8,Error=0.002},steady.AddMilliseconds(i*120));
                Check(session.Stable(steady.AddMilliseconds(240+2000))&&!session.Stable(steady.AddMilliseconds(240+2100))&&!session.Record(a,steady.AddMilliseconds(240+2100)),"latch expires after about 2 seconds");
                Check(session.CanRecord(a,steady.AddMilliseconds(240))==session.Stable(steady.AddMilliseconds(240)),"Record accepts the same heading the button calls stable");
                var engine=new PuzzleAnalysisEngine(assets);
                using(Bitmap arrow=Fixture(assets,Math.PI/4))
                using(Bitmap game=new Bitmap(Path.Combine(assets,"test_assets","capture","reported-full-game.png")))
                {
                    using(Graphics g=Graphics.FromImage(game)){g.FillRectangle(Brushes.SlateGray,808,393,263,311);g.DrawImageUnscaled(arrow,500,280);}
                    using(AnalysisResult r=engine.Analyze(game,4004,false,CancellationToken.None)){Check(r.Plan!=null&&r.Plan.Puzzle.Kind==PuzzleKind.Compass,"compass discovery works in real full-game scenery");File.AppendAllText(report,"Full-scene synthetic compass analysis: "+r.Milliseconds+" ms.\r\n");}
                    using(AnalysisResult r=engine.Analyze(game,4004,false,CancellationToken.None))Check(r.Unchanged&&r.Plan.Puzzle.Compass!=null,"confirmed compass images use the cached path");
                }
                Check(FloatingPanelLayout.Place(new Rectangle(-1920,0,1920,1080),new Size(400,556),new Point(12,72),new Rectangle(-1920,0,3840,1080)).Location==new Point(-1908,72),"floating panel follows client coordinates on a negative monitor");
                Rectangle clamp=FloatingPanelLayout.Place(new Rectangle(0,0,800,600),new Size(400,556),new Point(900,900),new Rectangle(0,0,1920,1080));Check(clamp.Right<=800&&clamp.Bottom<=600,"panel is kept on the visible game area");
                Check(!FloatingPanelLayout.Visible(true,false,true,true,true,false,false),"minimising the game hides the panel");
                Check(!FloatingPanelLayout.Visible(true,false,true,false,false,false,false),"another window in front hides the panel");
                Check(!FloatingPanelLayout.Visible(true,false,false,false,false,false,false),"the panel stays hidden until its client is in front");
                Check(FloatingPanelLayout.Visible(true,false,true,false,false,true,false),"panel remains usable while its controls have focus");
                Check(!FloatingPanelLayout.Visible(true,true,true,false,true,false,false),"offline screenshot mode never overlays the live game");
                string prefFile=Path.Combine(dir,"test-overlay-preferences.txt");
                Check(!new OverlayPreferences(prefFile).BlurPreview,"live preview blur is off until the player turns it on");
                var preferences=new OverlayPreferences(prefFile);preferences.Offset=new Point(-45,106);preferences.BlurPreview=true;preferences.SavedPositions.Add(new CompassPosition{Name="Kavros test",X=3000,Y=3300,Radius=2});preferences.Save();
                var loaded=new OverlayPreferences(prefFile);Check(loaded.Offset==preferences.Offset&&loaded.SavedPositions.Single().Name=="Kavros test"&&loaded.BlurPreview,"panel position, blur and custom Kavros coordinates persist");File.Delete(prefFile);
                using(Bitmap small=new Bitmap(10,10))using(Bitmap grown=new Bitmap(40,40))using(Graphics up=Graphics.FromImage(grown))
                {PreviewStyle.DrawSharp(up,small,new Rectangle(0,0,40,40));Check(up.InterpolationMode==InterpolationMode.NearestNeighbor,"a sharp preview keeps pixels when scaling up");}
                using(Bitmap large=new Bitmap(80,80))using(Bitmap shrunk=new Bitmap(40,40))using(Graphics down=Graphics.FromImage(shrunk))
                {PreviewStyle.DrawSharp(down,large,new Rectangle(0,0,30,30));Check(down.InterpolationMode==InterpolationMode.HighQualityBicubic&&down.PixelOffsetMode==PixelOffsetMode.HighQuality,"a sharp preview stays smooth when scaling down");}
                File.AppendAllText(report,"V0.4.0 COMPASS / FLOATING PANEL CORE PASS: "+assertions+" assertions.\r\nSynthetic reader fixtures, both calibration tables, candidate filtering, stable-frame gating, resets, layout and persistence checked. Live Kavros compass pixels are not available in this test run.\r\n");return 0;
            }
            catch(Exception e){File.AppendAllText(report,e+"\r\n");return 1;}
        }
        public static int Render(string report)
        {
            try
            {
                string assets=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets"),dir=Path.GetDirectoryName(Path.GetFullPath(report));var session=new CompassSession(assets);var prefs=new OverlayPreferences(Path.Combine(dir,"unused-preview-preferences.txt"));
                DateTime now=DateTime.UtcNow.AddSeconds(-1);PointF target=session.AllSpots[110];
                foreach(var position in new[]{new CompassPosition{Name="Example A",X=2400,Y=3300,Radius=0},new CompassPosition{Name="Example B",X=3400,Y=3100,Radius=0},new CompassPosition{Name="Example C",X=3000,Y=3800,Radius=0}})
                {for(int i=0;i<3;i++)session.Observe(new CompassReading{Angle=CompassMath.Direction(position.Point,target),Error=0.002},now.AddMilliseconds(i*200));session.Record(position,now.AddMilliseconds(400));session.Observe(null,now);}
                using(var panel=new FloatingCluePanel(assets,session,prefs))
                {
                    panel.Location=new Point(0,0);panel.Show();panel.SetCompass(true);System.Windows.Forms.Application.DoEvents();Snapshot(panel,Path.Combine(dir,"compass-panel-preview.png"));
                    Check(panel.Controls.OfType<CompassMap>().Single().Visible&&!panel.Controls.OfType<MasterClueView>().Single().Visible,"compass map is visible while the master card is hidden");
                    Check(panel.Width==400&&panel.Height==556+WhatsNewLayout.PanelExtra,"compass controls fit the floating panel");
                    for(int i=0;i<3;i++)session.Observe(new CompassReading{Solved=true},DateTime.UtcNow.AddMilliseconds(-400+i*200));
                    panel.RefreshCompass(false);
                    Check(panel.Controls.OfType<System.Windows.Forms.Label>().Any(c=>c.Text.Contains("Dig at your current position")),"an X result takes priority over an older map target");
                    panel.SetPuzzle(new DetectedPuzzle{Kind=PuzzleKind.EmoteClue,Master=MasterClues.ById(236)},"");System.Windows.Forms.Application.DoEvents();Snapshot(panel,Path.Combine(dir,"master-panel-preview.png"));
                    Check(panel.Controls.OfType<MasterClueView>().Single().Visible&&!panel.Controls.OfType<CompassMap>().Single().Visible,"master card replaces the compass controls");
                    Check(panel.Height==480+WhatsNewLayout.PanelExtra,"master guidance switches into its compact map card");
                    panel.SetPuzzle(new DetectedPuzzle{Kind=PuzzleKind.Towers},"Corners show clicks left. Green 0 means complete.");Check(panel.Height==140+WhatsNewLayout.PanelExtra,"puzzle guidance uses a small panel");
                }
                File.AppendAllText(report,"V0.4.0 FLOATING PANEL RENDER PASS: map and master cards painted, mode sizing verified on an isolated virtual display. No user desktop or game input was used.\r\n");return 0;
            }catch(Exception e){File.AppendAllText(report,e+"\r\n");return 1;}
        }
        private static void Snapshot(System.Windows.Forms.Form form,string file)
        {
            // Called only by the explicit render test, on an isolated virtual
            // display in this build. DrawToBitmap paints hidden controls on Mono.
            using(Bitmap image=new Bitmap(form.Width,form.Height))
            {using(Graphics g=Graphics.FromImage(image))g.CopyFromScreen(form.Location,Point.Empty,image.Size);image.Save(file);}
        }
    }
}
