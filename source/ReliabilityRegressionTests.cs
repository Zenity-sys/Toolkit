using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
namespace KavrosClueHelper
{
    public static class ReliabilityRegressionTests
    {
        private static int count;
        private static void Check(bool value,string description){count++;if(!value)throw new Exception("FAILED: "+description);}
        private static Bitmap Mini(string assets,ScanRegion region,Size size,PointF position,double scale,double angle,bool round)
        {
            var result=new Bitmap(size.Width,size.Height);double cos=Math.Cos(angle)/scale,sin=Math.Sin(angle)/scale;
            float[] box=File.ReadAllText(Path.Combine(assets,"scan",region.Id+"-track-map.tsv")).Trim().Split(new[]{'\t'}).Select(v=>Single.Parse(v,CultureInfo.InvariantCulture)).ToArray();
            using(var reference=new Bitmap(Path.Combine(assets,"scan",region.Id+"-track.png")))
                for(int y=0;y<size.Height;y++)for(int x=0;x<size.Width;x++)
                {
                    int dx=x-size.Width/2,dy=y-size.Height/2;double wx=position.X+dx*cos-dy*sin,wy=position.Y-dx*sin-dy*cos;
                    int mx=(int)Math.Round((wx-box[0])/(box[2]-box[0])*reference.Width),my=(int)Math.Round((box[3]-wy)/(box[3]-box[1])*reference.Height);
                    bool edge=x<5||y<5||x>=size.Width-5||y>=size.Height-5||round&&(dx*dx/(double)Math.Pow(size.Width/2.0-5,2)+dy*dy/(double)Math.Pow(size.Height/2.0-5,2)>1);
                    result.SetPixel(x,y,edge?Color.FromArgb(18,30,38):mx>=0&&my>=0&&mx<reference.Width&&my<reference.Height?reference.GetPixel(mx,my):Color.FromArgb(80,99,120));
                }
            using(Graphics g=Graphics.FromImage(result))g.FillRectangle(Brushes.White,size.Width/2-1,size.Height/2-1,3,3);
            return result;
        }
        private static void Handles(Control control){IntPtr unused=control.Handle;foreach(Control child in control.Controls)Handles(child);}
        public static int Run(string report)
        {
            try
            {
                string assets=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets"),directory=Path.GetDirectoryName(Path.GetFullPath(report));
                var engine=new PuzzleAnalysisEngine(assets);
                using(var original=new Bitmap(Path.Combine(assets,"test_assets","reported","edgeville-native.png")))
                {
                    foreach(float scale in new[]{0.8F,1F,1.1F,1.25F})
                    using(Bitmap resized=Pixels.Crop(original,new Rectangle(Point.Empty,original.Size),new Size((int)Math.Round(original.Width*scale),(int)Math.Round(original.Height*scale))))
                    using(AnalysisResult result=engine.Analyze(resized,90,false,CancellationToken.None,true))
                    {Check(result.Plan!=null&&result.Plan.Puzzle.Master!=null&&result.Plan.Puzzle.Master.Id==239,"reported Edgeville paragraph at scale "+scale);File.AppendAllText(report,"Edgeville "+scale+": "+result.Milliseconds+" ms.\r\n");}
                    using(Bitmap incomplete=(Bitmap)original.Clone())
                    {using(Graphics g=Graphics.FromImage(incomplete))using(Brush paper=new SolidBrush(Color.FromArgb(220,190,140)))g.FillRectangle(paper,335,526,310,18);using(AnalysisResult result=engine.Analyze(incomplete,91,false,CancellationToken.None))Check(result.Plan==null,"missing equipment line does not confirm an Edgeville clue");}
                }
                engine.UseLegacy(true);
                using(var image=new Bitmap(Path.Combine(assets,"test_assets","reported","slider-native.png")))
                using(AnalysisResult result=engine.Analyze(image,100,false,CancellationToken.None))
                {
                    Check(result.Plan!=null&&result.Plan.Puzzle.Kind==PuzzleKind.Sliding,"reported sliding box is detected");
                    DetectedPuzzle puzzle=result.Plan.Puzzle;Check(puzzle.Theme=="tuska"&&Array.IndexOf(puzzle.Board,24)==3,"real Tuska tiles and blank are identified");
                    int[] board=(int[])puzzle.Board.Clone();var route=result.Plan.Sliding;Check(route!=null&&route.Count>0,"real slider has a legal route");
                    foreach(int move in route){int blank=Array.IndexOf(board,24);Check(SlidingSolver.Neighbours(blank).Contains(move),"slider route uses adjacent moves");board[blank]=board[move];board[move]=24;}
                    Check(board.SequenceEqual(Enumerable.Range(0,25)),"real sliding route reaches the complete picture");
                    var marks=GuidanceDrawing.SliderMarks(puzzle,route);Check(marks.Count>=2&&marks.Count<=4&&marks.Select(m=>m.Box).Distinct().Count()==marks.Count,"lookahead badges never overlap");
                    using(Bitmap preview=(Bitmap)image.Clone()){using(Graphics g=Graphics.FromImage(preview))GuidanceDrawing.Draw(g,marks);preview.Save(Path.Combine(directory,"slider-guidance-preview.png"));}
                    board=(int[])puzzle.Board.Clone();foreach(int move in route.Take(3)){int blank=Array.IndexOf(board,24);board[blank]=board[move];board[move]=24;}
                    using(Bitmap next=(Bitmap)image.Clone())using(Bitmap reference=new Bitmap(Path.Combine(assets,"data","sliders","sliders","tuska.png")))
                    using(Bitmap blankTile=image.Clone(GuidanceDrawing.Cell(puzzle,3),System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                    {
                        using(Graphics g=Graphics.FromImage(next))for(int cell=0;cell<25;cell++)
                        {if(board[cell]==24)g.DrawImage(blankTile,GuidanceDrawing.Cell(puzzle,cell));else g.DrawImage(reference,GuidanceDrawing.Cell(puzzle,cell),new Rectangle(board[cell]%5*49,board[cell]/5*49,49,49),GraphicsUnit.Pixel);}
                        using(AnalysisResult advanced=engine.Analyze(next,100,false,CancellationToken.None))
                        {Check(advanced.Plan!=null&&advanced.Plan.Sliding.SequenceEqual(route.Skip(3)),"several clicks advance the existing route");Check(engine.SlidingRouteReuses>0,"route is reused without resolving every frame");}
                    }
                    using(var panel=new FloatingCluePanel(assets,new CompassSession(assets),new OverlayPreferences(Path.Combine(directory,"unused-reliability-prefs.txt"))))
                    {panel.SetPuzzle(puzzle,"Click 1 then 2 then 3 in order.");Handles(panel);Check(panel.Height==330+WhatsNewLayout.PanelExtra,"finished-picture guide fits the slider card");using(var preview=new Bitmap(panel.Width,panel.Height)){panel.DrawToBitmap(preview,new Rectangle(Point.Empty,panel.Size));preview.Save(Path.Combine(directory,"slider-panel-preview.png"));Check(preview.GetPixel(3,90).ToArgb()==Color.FromArgb(18,23,32).ToArgb(),"slider guide background stays opaque");}}
                }
                var knotTracker=new KnotFrameTracker();DateTime knotTime=DateTime.UtcNow;KnotPuzzle source=TestFixtures.KnotExample();
                Func<int,DetectedPuzzle> rotated=offset=>new DetectedPuzzle{Kind=PuzzleKind.Knot,Knot=new KnotPuzzle{Colours=source.Colours,Locks=source.Locks,Buttons=source.Buttons,Lanes=source.Lanes.Select((lane,i)=>lane.Select((r,j)=>lane[(j+offset+i)%lane.Length]).ToArray()).ToArray()}};
                Check(knotTracker.Observe(rotated(0),knotTime,true),"an uncertain first knot frame waits for stability");
                Check(!knotTracker.Observe(rotated(0),knotTime.AddMilliseconds(125),true),"settled knot frame enables guidance");
                var sureTracker=new KnotFrameTracker();DetectedPuzzle sure=rotated(0);sure.Knot.Reliable=true;
                Check(!sureTracker.Observe(sure,knotTime,true),"a reliable still knot shows counts on the first frame");
                DetectedPuzzle flicker=rotated(0);flicker.Knot.Lanes[0][0]=new RuneValue(flicker.Knot.Lanes[0][0].Id,!flicker.Knot.Lanes[0][0].IsNot);flicker.Knot.Reliable=true;
                Check(!sureTracker.Observe(flicker,knotTime.AddMilliseconds(40),true),"a match-marker flicker does not hide the counts");
                DetectedPuzzle turned=rotated(1);turned.Knot.Reliable=true;
                Check(!sureTracker.Observe(turned,knotTime.AddMilliseconds(80),true),"a reliable turn updates the count on the first frame");
                Check(!sureTracker.Observe(turned,knotTime.AddMilliseconds(120),true),"the stopped ring keeps that count");
                DetectedPuzzle blur=rotated(2);
                Check(sureTracker.Observe(blur,knotTime.AddMilliseconds(160),true),"an unreliable mid-spin frame waits");
                Check(!sureTracker.Observe(blur,knotTime.AddMilliseconds(200),true),"the same mid-spin frame shows counts once it repeats");
                foreach(int offset in new[]{3,7,2,6})Check(knotTracker.Observe(rotated(offset),knotTime.AddMilliseconds(140+offset),true),"rapid multi-rotation knot updates defer stale guidance");
                knotTracker.Missing(knotTime.AddMilliseconds(300));Check(knotTracker.History!=null,"brief animation miss retains settled rune memory");
                Check(knotTracker.Observe(rotated(5),knotTime.AddMilliseconds(400),true),"recovered knot requires fresh confirmation");
                Check(!knotTracker.Observe(rotated(5),knotTime.AddMilliseconds(525),true),"latest multi-rotation state becomes settled");
                KnotPuzzle latest=knotTracker.History;KnotSolution solved=KnotSolver.Solve(latest);Check(solved!=null,"recovered knot remains solvable");
                foreach(KnotLock link in latest.Locks)Check(KnotSolver.Equal(latest.Lanes[link.A][KnotSolver.Mod(link.IndexA-solved.Offsets[link.A],latest.Lanes[link.A].Length)],latest.Lanes[link.B][KnotSolver.Mod(link.IndexB-solved.Offsets[link.B],latest.Lanes[link.B].Length)]),"recovered knot instructions match current runes");
                knotTracker.Missing(knotTime.AddSeconds(3));Check(knotTracker.History==null,"closed knot eventually clears its history");
                var regions=new ScanVision(assets).Regions;var reader=new MinimapPositionReader(assets);
                foreach(var item in new[]{
                    new{Id=368,Size=new Size(96,96),Location=new Point(30,40),Position=new PointF(2208,3360),Scale=2.7,Angle=0.0,Round=false},
                    new{Id=368,Size=new Size(160,110),Location=new Point(420,270),Position=new PointF(2240,3390),Scale=3.0,Angle=Math.PI/6,Round=false},
                    new{Id=371,Size=new Size(480,180),Location=new Point(600,40),Position=new PointF(3190,6970),Scale=4.0,Angle=0.0,Round=false},
                    new{Id=368,Size=new Size(360,360),Location=new Point(40,300),Position=new PointF(2208,3360),Scale=2.5,Angle=Math.PI/6,Round=true},
                    new{Id=371,Size=new Size(640,420),Location=new Point(440,300),Position=new PointF(3220,7000),Scale=8.0,Angle=-73*Math.PI/180,Round=false}})
                {
                    ScanRegion region=regions.Single(r=>r.Id==item.Id);reader.Reset();
                    using(Bitmap mini=Mini(assets,region,item.Size,item.Position,item.Scale,item.Angle,item.Round))
                    using(var scene=new Bitmap(1120,780))
                    {
                        using(Graphics g=Graphics.FromImage(scene)){g.Clear(Color.FromArgb(105,85,60));g.DrawImageUnscaled(mini,item.Location);}
                        var watch=System.Diagnostics.Stopwatch.StartNew();MinimapMatch match=reader.Find(scene,region,Point.Empty,CancellationToken.None);
                        File.AppendAllText(report,"Minimap "+item.Size+" circle="+item.Round+": "+(match==null?"NONE":match.Position.ToString())+"; "+watch.ElapsedMilliseconds+" ms; "+reader.Diagnostics+"\r\n");
                        Check(match!=null&&CompassMath.Distance(match.Position,item.Position)<3,"resized/moved minimap localizes: "+item.Size+" "+reader.Diagnostics);
                        Check(match.Viewport.Contains(match.PlayerPixel)&&match.Viewport.Width<=item.Size.Width+15&&match.Viewport.Height<=item.Size.Height+15,"capture follows the discovered viewport");
                        Rectangle crop=Rectangle.Intersect(new Rectangle(Point.Empty,scene.Size),Rectangle.Inflate(match.Viewport,4,4));
                        using(Bitmap clipped=scene.Clone(crop,System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                        {match=reader.Find(clipped,region,crop.Location,CancellationToken.None);Check(match!=null&&CompassMath.Distance(match.Position,item.Position)<3,"cropped tracking preserves position after resize");}
                    }
                }
                File.AppendAllText(report,"V0.5.4 RELIABILITY PASS: "+count+" assertions. Real Edgeville/Tuska captures, legal full slider route, lookahead/route reuse, slider panel rendering, rectangular/circular minimaps from 96x96 to 640x420 at varied positions/zooms/rotations. Live scan minimap validation remains outstanding. No windows shown or game input/capture.\r\n");return 0;
            }
            catch(Exception e){File.AppendAllText(report,e+"\r\n");return 1;}
        }
    }
}
