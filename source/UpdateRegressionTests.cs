using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
namespace KavrosClueHelper
{
    public static class UpdateRegressionTests
    {
        private static int count;
        private static void DarkPanel(PuzzleAnalysisEngine engine,string assets,string report)
        {
            const int px=480,py=40,pw=255,ph=322;
            using(Bitmap image=new Bitmap(900,640,PixelFormat.Format32bppArgb))
            {
                for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)image.SetPixel(x,y,Color.FromArgb(18,28,36));
                for(int y=py;y<py+ph;y++)for(int x=px;x<px+pw;x++)image.SetPixel(x,y,Color.FromArgb(148,122,70));
                for(int y=py;y<py+ph;y++)for(int x=px+96;x<px+110;x++)image.SetPixel(x,y,Color.FromArgb(110,100,45));
                for(int y=py+28;y<py+40;y++)for(int x=px;x<px+pw;x++)
                {
                    if(x>=px+96&&x<px+110)continue;
                    image.SetPixel(x,y,Color.FromArgb(176,150,96));
                }
                using(Bitmap title=new Bitmap(Path.Combine(assets,"scan","368-title.png")))
                using(Graphics g=Graphics.FromImage(image))g.DrawImage(title,px+98,py+84);
                int hitX=px+98,hitY=py+84;
                for(int y=hitY+30;y<hitY+70;y+=2)for(int x=hitX-20;x<hitX+80;x+=2)
                    if(x>=px&&x<px+pw&&y>=py&&y<py+ph)image.SetPixel(x,y,Color.White);
                Pixels pixels=new Pixels(image);
                Check(ParagraphVision.FindPapers(pixels).All(r=>r.Width<140||r.Height<190),"strict beige does not see the darker split panel");
                Check(ParagraphVision.NarrowSheets(pixels).Count>0,"scroll areas see the darker narrow panel");
                Check(ParagraphVision.FindScrolls(pixels).Count==0,"the narrow panel is not a wide titled scroll");
                int boards=engine.BoardPasses;
                using(AnalysisResult found=engine.Analyze(image,368,false,CancellationToken.None,true))
                {
                    File.AppendAllText(report,"dark panel scan: "+(found.Plan==null?"none":found.Plan.Puzzle.Kind.ToString())+" in "+found.Milliseconds+" ms, boards "+engine.BoardPasses+"\r\n");
                    Check(found.Plan!=null&&found.Plan.Puzzle.Kind==PuzzleKind.Scan&&found.Plan.Puzzle.Scan!=null&&found.Plan.Puzzle.Scan.Region!=null&&found.Plan.Puzzle.Scan.Region.Id==368,"darker narrow panel is a Prifddinas scan on the first look");
                    Check(engine.BoardPasses==boards,"the first look does not run the board search");
                }
            }
            using(Bitmap wide=new Bitmap(800,600,PixelFormat.Format32bppArgb))
            {
                for(int y=40;y<440;y++)for(int x=40;x<540;x++)wide.SetPixel(x,y,Color.FromArgb(176,150,96));
                Check(ScrollAreas.NarrowPanels(new Pixels(wide)).Count==0,"a wide sheet is not treated as a narrow scan panel");
            }
        }
        private static void ElitePanel(string assets,string report)
        {
            var named=new ScanVision(assets);
            ScanRegion tooFar=named.MatchScrollTranscript("Deep Wilderness. You are too far away and nothing scans.");
            Check(tooFar!=null&&tooFar.Id==36,"a named too-far line is Deep Wilderness");
            ScanRegion orb=named.MatchScrollTranscript("Deep Wilderness. The scan range of this orb is 99 paces.");
            Check(orb!=null&&orb.Id==36,"a named orb range is Deep Wilderness");
            Check(named.MatchScrollTranscript("This scroll will work in Prifddinas.")==null,"a place name without paces is not a scan");
            const int px=480,py=40,pw=255,ph=322;
            var engine=new PuzzleAnalysisEngine(assets);
            using(Bitmap image=new Bitmap(900,640,PixelFormat.Format32bppArgb))
            {
                for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)image.SetPixel(x,y,Color.FromArgb(18,28,36));
                for(int y=py;y<py+ph;y++)for(int x=px;x<px+pw;x++)image.SetPixel(x,y,Color.FromArgb(148,122,70));
                for(int y=py;y<py+ph;y++)for(int x=px+96;x<px+110;x++)image.SetPixel(x,y,Color.FromArgb(110,100,45));
                // Title ink is drawn from the real in-game letters (Taverley's D,
                // the wilderness line), at the live 89x8 size. 36.png is not pasted on.
                using(Bitmap dungeon=new Bitmap(Path.Combine(assets,"scan-titles","356.png")))
                using(Bitmap wild=new Bitmap(Path.Combine(assets,"scan-titles","367.png")))
                using(Bitmap far=new Bitmap(Path.Combine(assets,"scan-titles","too-far.png")))
                using(Bitmap none=new Bitmap(Path.Combine(assets,"scan-titles","nothing-scans.png")))
                {
                    int x=px+40,y=py+48;
                    BlitInk(image,dungeon,61,0,7,8,x,y); x+=8;
                    BlitInk(image,wild,23,0,6,8,x,y); x+=7;
                    BlitInk(image,wild,23,0,6,8,x,y); x+=7;
                    DrawPee(image,x,y); x+=9;
                    BlitInk(image,wild,0,0,58,8,x,y);
                    StampWhite(image,far,px+36,py+80);
                    StampWhite(image,none,px+36,py+96);
                }
                using(Bitmap stored=new Bitmap(Path.Combine(assets,"scan-titles","36.png")))
                    Check(stored.Width==89&&stored.Height==8,"Deep Wilderness glyph is the live 89x8 title");
                for(int y=py+112;y<py+120;y++)for(int x=px+40;x<px+120;x+=2)image.SetPixel(x,y,Color.White);
                int boards=engine.BoardPasses;
                var watch=System.Diagnostics.Stopwatch.StartNew();
                using(AnalysisResult found=engine.Analyze(image,36,false,CancellationToken.None,true))
                {
                    watch.Stop();
                    File.AppendAllText(report,"deep wilderness panel: "+(found.Plan==null?"none":found.Plan.Puzzle.Kind.ToString())+" in "+found.Milliseconds+" ms, boards "+engine.BoardPasses+"\r\n");
                    Check(found.Plan!=null&&found.Plan.Puzzle.Kind==PuzzleKind.Scan&&found.Plan.Puzzle.Scan!=null&&found.Plan.Puzzle.Scan.Region!=null&&found.Plan.Puzzle.Scan.Region.Id==36,"darker narrow panel elite scan is Deep Wilderness on the first look");
                    Check(found.Plan.Puzzle.Scan.Outside,"the too-far body is out of range");
                    Check(engine.BoardPasses==boards,"the first elite look does not run the board search");
                }
                int after=engine.BoardPasses;
                using(AnalysisResult again=engine.Analyze(image,36,false,CancellationToken.None,true))
                {
                    File.AppendAllText(report,"deep wilderness cached: "+again.Milliseconds+" ms, unchanged "+again.Unchanged+", boards "+engine.BoardPasses+"\r\n");
                    Check(again.Unchanged&&again.Plan!=null&&again.Plan.Puzzle.Kind==PuzzleKind.Scan&&again.Plan.Puzzle.Scan.Region.Id==36,"cached scan returns without a new read");
                    Check(engine.BoardPasses==after,"cached scan does not run the board search");
                }
            }
        }
        private static void BlitInk(Bitmap image,Bitmap source,int sx,int sy,int width,int height,int left,int top)
        {
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                Color c=source.GetPixel(sx+x,sy+y);
                if(c.R>185&&c.G>185&&c.B>185&&left+x<image.Width&&top+y<image.Height)image.SetPixel(left+x,top+y,Color.White);
            }
        }
        // Lowercase p is not in a captured title. It uses the same 8px grid as the real letters.
        private static void DrawPee(Bitmap image,int left,int top)
        {
            string[] rows=new[]{".....",".....","#....","#....","####.","#...#","#...#","####."};
            for(int y=0;y<rows.Length;y++)for(int x=0;x<rows[y].Length;x++)if(rows[y][x]=='#')image.SetPixel(left+x,top+y,Color.White);
        }
        private static void StampWhite(Bitmap image,Bitmap glyph,int left,int top)
        {
            for(int y=0;y<glyph.Height;y++)for(int x=0;x<glyph.Width;x++)
            {
                Color c=glyph.GetPixel(x,y);
                if(c.R>185&&c.G>185&&c.B>185&&left+x<image.Width&&top+y<image.Height)image.SetPixel(left+x,top+y,Color.White);
            }
        }
        private static void Check(bool condition,string message){count++;if(!condition)throw new Exception("FAILED: "+message);}
        public static int Run(string report)
        {
            try
            {
                string assets=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets"),root=Path.Combine(assets,"test_assets","reported");
                var init=System.Diagnostics.Stopwatch.StartNew();var engine=new PuzzleAnalysisEngine(assets);
                File.AppendAllText(report,"Reader initialisation: "+init.ElapsedMilliseconds+" ms.\r\n");
                DarkPanel(engine,assets,report);
                ElitePanel(assets,report);
                foreach(var expected in new[]{
                    new{File="lumbridge-native.png",Kind=PuzzleKind.EmoteClue,Id=242},
                    new{File="compass-edgeville.png",Kind=PuzzleKind.Compass,Id=0},
                    new{File="compass-arrived.png",Kind=PuzzleKind.Compass,Id=0},
                    new{File="scan-prif-native.png",Kind=PuzzleKind.Scan,Id=368}})
                using(Bitmap file=new Bitmap(Path.Combine(root,expected.File)))
                using(Bitmap memory=new Bitmap(file.Width,file.Height,PixelFormat.Format32bppArgb))
                {
                    using(Graphics g=Graphics.FromImage(memory))g.DrawImageUnscaled(file,0,0);
                    using(AnalysisResult r=engine.Analyze(memory,100+count,false,CancellationToken.None,true))
                    {
                        File.AppendAllText(report,expected.File+": "+(r.Plan==null?"none":r.Plan.Puzzle.Kind.ToString())+" in "+r.Milliseconds+" ms.\r\n");
                        Check(r.Plan!=null&&r.Plan.Puzzle.Kind==expected.Kind,"recognises real screenshot "+expected.File);
                        if(expected.Id==242)Check(r.Plan.Puzzle.Master.Id==242,"Lumbridge identity is correct");
                        if(expected.Id==368)Check(r.Plan.Puzzle.Scan.Region.Id==368,"Prifddinas identity is correct");
                        if(expected.File=="compass-edgeville.png")Check(!r.Plan.Puzzle.Compass.Solved&&CompassMath.Bearing(r.Plan.Puzzle.Compass.Angle)>20&&CompassMath.Bearing(r.Plan.Puzzle.Compass.Angle)<50,"real Edgeville arrow points north-east");
                        if(expected.File=="compass-arrived.png")Check(r.Plan.Puzzle.Compass.Solved,"real arrived cross produces no false bearing");
                    }
                }
                var master=new DetectedPuzzle{Kind=PuzzleKind.EmoteClue,Master=MasterClues.ById(242),Bounds=new Rectangle(30,40,500,350)};
                foreach(string background in new[]{Path.Combine(root,"compass-edgeville.png"),Path.Combine(assets,"test_assets","capture","reported-full-game.png")})
                foreach(Point location in new[]{new Point(30,250),new Point(750,330)})
                using(Bitmap scene=new Bitmap(background))using(Bitmap clue=new Bitmap(Path.Combine(root,"lumbridge-native.png")))
                {
                    using(Graphics g=Graphics.FromImage(scene)){g.FillRectangle(Brushes.DarkSlateGray,1095,814,230,scene.Height-814);g.FillRectangle(Brushes.DarkSlateGray,808,393,263,311);g.DrawImageUnscaled(clue,location);}
                    using(AnalysisResult r=engine.Analyze(scene,1000+count,false,CancellationToken.None,true))
                    {File.AppendAllText(report,"New Lumbridge in "+Path.GetFileName(background)+" at "+location+": "+(r.Plan==null?"none":r.Plan.Puzzle.Kind.ToString())+" in "+r.Milliseconds+" ms.\r\n");Check(r.Plan!=null&&r.Plan.Puzzle.Master!=null&&r.Plan.Puzzle.Master.Id==242,"new Lumbridge capture recognised inside full-game scenery");}
                }
                using(Bitmap incomplete=new Bitmap(Path.Combine(root,"lumbridge-native.png")))
                {
                    using(Graphics g=Graphics.FromImage(incomplete))using(Brush paper=new SolidBrush(Color.FromArgb(220,190,140)))g.FillRectangle(paper,150,207,210,24);
                    using(AnalysisResult r=engine.Analyze(incomplete,999,false,CancellationToken.None,true))Check(r.Plan==null,"a missing equipment line cannot confirm a partial master paragraph");
                }
                foreach(float factor in new[]{0.8F,1.05F,1.1F,1.25F,1.5F})
                using(Bitmap original=new Bitmap(Path.Combine(root,"lumbridge-native.png")))
                using(Bitmap scaled=Pixels.Crop(original,new Rectangle(Point.Empty,original.Size),new Size((int)Math.Round(original.Width*factor),(int)Math.Round(original.Height*factor))))
                using(AnalysisResult r=engine.Analyze(scaled,200+count,false,CancellationToken.None,true))Check(r.Plan!=null&&r.Plan.Puzzle.Master!=null&&r.Plan.Puzzle.Master.Id==242,"new Lumbridge capture stays readable at scale "+factor);
                var held=new ClueRetention();held.Accept(master);held.Missing();Check(held.Display(null)==master,"closing scroll keeps the last solve");
                Check(FloatingCluePanel.TitlePill(false,true,PuzzleKind.EmoteClue)=="Scroll closed"&&FloatingCluePanel.TitlePill(false,false,PuzzleKind.EmoteClue)=="Clue found","a saved solve is marked closed, not Found");
                var same=new DetectedPuzzle{Kind=PuzzleKind.EmoteClue,Master=MasterClues.ById(242),Bounds=master.Bounds};held.Accept(same);Check(held.Display(same)==same&&held.Saved.Key==master.Key,"reading the same clue again keeps its solve");
                var next=new DetectedPuzzle{Kind=PuzzleKind.EmoteClue,Master=MasterClues.ById(246),Bounds=master.Bounds};held.Accept(next);held.Missing();Check(held.Display(null)==next,"a new clue step replaces the saved solve");
                held.Clear();Check(held.Display(null)==null,"Clear wipes the saved solve after the scroll closes");
                held.Accept(master);held.Missing();
                held.Accept(new DetectedPuzzle{Kind=PuzzleKind.Lockbox,Board=new int[25]});Check(held.Display(null)==null,"new puzzle replaces saved master");
                held.Accept(master);held.Clear(master);Check(held.Display(master)==null,"Clear dismisses visible master until it is closed");
                held.Missing();held.Accept(master);Check(held.Display(master)==master,"reopening dismissed clue restores recognition");
                held.Reset();Check(held.Saved==null,"new game source clears saved guidance");

                var compass=new CompassSession(assets);PointF target=new PointF(3150,3600);
                var a=new CompassPosition{Name="Edgeville test",X=3067,Y=3505,Radius=0};
                var b=new CompassPosition{Name="Varrock test",X=3214,Y=3376,Radius=0};
                DateTime now=DateTime.UtcNow;
                // Keep this geometric intersection test on a continuously visible clue.
                // Hidden and back-to-back compasses have their own recovery tests.
                foreach(var p in new[]{a,b}){for(int i=0;i<3;i++)compass.Observe(new CompassReading{Angle=CompassMath.Direction(p.Point,target),Error=0.002},now.AddMilliseconds(i*125));Check(compass.Record(p,now.AddMilliseconds(250)),"lodestone bearing accepted");now=now.AddSeconds(1);}
                Check(compass.Intersection.HasValue&&CompassMath.Distance(compass.Intersection.Value,target)<0.01,"two rays place X at independently chosen target");
                compass.Undo();Check(!compass.Intersection.HasValue,"one ray cannot claim an X");
                compass.NewClue();Check(compass.Bearings.Count==0&&!compass.Intersection.HasValue,"new clue clears lines and X");
                foreach(var p in new[]{a,b}){for(int i=0;i<3;i++)compass.Observe(new CompassReading{Angle=0.4,Error=0.002},now.AddMilliseconds(i*125));compass.Record(p,now.AddMilliseconds(250));compass.Observe(null,now);now=now.AddSeconds(1);}
                Check(!compass.Intersection.HasValue,"parallel rays do not invent an intersection");
                Check(compass.Positions.Any(p=>p.Name=="Prifddinas lodestone"),"Prifddinas is in lodestone list");
                using(var map=new CompassMap(assets)){map.Size=new Size(380,260);map.SetSession(compass,true);using(var bitmap=new Bitmap(380,260))using(var g=Graphics.FromImage(bitmap)){typeof(CompassMap).GetMethod("OnPaint",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(map,new object[]{new PaintEventArgs(g,new Rectangle(0,0,380,260))});bitmap.Save(Path.Combine(Path.GetDirectoryName(report),"compass-map-preview.png"));}}
                using(var panel=new FloatingCluePanel(assets,compass,new OverlayPreferences(Path.Combine(Path.GetDirectoryName(report),"unused-panel-prefs.txt"))))
                {
                    panel.SetPuzzle(master,"");Check(panel.Height==480+WhatsNewLayout.PanelExtra,"master card retains its complete layout");panel.SetReading("Reading clue...");Check(panel.ReadingStage=="Reading clue...","loading state is exposed");panel.SetReading(null);Check(panel.ReadingStage==null,"loading state clears");panel.SetCompass(false);Check(panel.Height==556+WhatsNewLayout.PanelExtra,"compass controls fit the panel");
                }

                var scanVision=new ScanVision(assets);
                foreach(var data in new[]{new{File="scan-prifddinas.png",Id=368,Spots=18,Range=30},new{File="scan-heart.png",Id=371,Spots=26,Range=49}})
                using(Bitmap image=new Bitmap(Path.Combine(assets,"test_assets","master",data.File)))
                {
                    DetectedPuzzle p=scanVision.Find(image,new Pixels(image));Check(p!=null&&p.Scan.Region.Id==data.Id,"scan region detected: "+data.File);
                    Check(p.Scan.Region.Spots.Count==data.Spots,"known scan locations loaded");
                    Check(p.Scan.DifferentLevel,"different-level message recognised");
                    var scan=new ScanSession();scan.Observe(p.Scan);scan.SetManual(scan.Region.Spots[0]);Check(!scan.InRange(scan.Region.Spots[0]),"different floor does not mark a dig spot reachable");scan.DifferentLevel=false;Check(scan.InRange(scan.Region.Spots[0]),"range includes player's tile");
                    Check(scan.Range==data.Range,"base scan range preserved");
                    scan.Meerkat=true;Check(scan.Range==data.Range+5,"meerkat adds exactly five paces");
                    scan.Observe(p.Scan);Check(scan.Range==data.Range+5,"re-reading does not apply the bonus twice");Check(scan.DifferentLevel,"new scan text refreshes the floor warning");scan.DifferentLevel=false;
                    PointF pos=scan.Position.Value;Check(scan.InRange(new PointF(pos.X+scan.Range,pos.Y+scan.Range)),"square range includes the diagonal boundary");
                    Check(!scan.InRange(new PointF(pos.X+scan.Range+1,pos.Y)),"one tile outside is not marked in range");
                    using(var map=new ScanMap(assets,scan)){map.Size=new Size(380,260);using(var bitmap=new Bitmap(380,260))using(var g=Graphics.FromImage(bitmap)){typeof(ScanMap).GetMethod("OnPaint",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(map,new object[]{new PaintEventArgs(g,new Rectangle(0,0,380,260))});bitmap.Save(Path.Combine(Path.GetDirectoryName(report),"scan-"+data.Id+"-preview.png"));}}
                    held.Accept(p);held.Missing();Check(held.Display(null)==null,"a closed scan is not shown as found");
                }
                // Real game scenery with the compass removed must remain negative.
                using(Bitmap image=new Bitmap(Path.Combine(root,"compass-edgeville.png")))
                {
                    using(Graphics g=Graphics.FromImage(image))g.FillRectangle(Brushes.DarkSlateGray,1095,814,230,image.Height-814);
                    using(AnalysisResult r=engine.Analyze(image,900,false,CancellationToken.None,true))Check(r.Plan==null,"game scenery without a clue produces no false positive");
                }
                File.AppendAllText(report,"V0.5.0 UPDATE REGRESSIONS PASS: "+count+" assertions. Real screenshot reads, retained cards, compass intersections, parallel-ray rejection, scan regions, range boundaries and offline scan map rendering checked. No windows shown or game input sent.\r\n");
                return 0;
            }
            catch(Exception e){File.AppendAllText(report,e+"\r\n");return 1;}
        }
    }
}

