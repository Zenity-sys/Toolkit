using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace KavrosClueHelper
{
    public static class DiscoveryRegressionTests
    {
        private static int assertions;
        private static void Check(bool ok,string message) { assertions++; if(!ok) throw new Exception("FAILED: "+message); }
        private static Bitmap EmptyScene(Bitmap original)
        {
            Bitmap scene=(Bitmap)original.Clone();
            using(Graphics g=Graphics.FromImage(scene)) using(Brush brush=new SolidBrush(Color.FromArgb(21,38,49))) g.FillRectangle(brush,808,393,263,311);
            return scene;
        }
        private static bool ValidTower(int[] board,TowerPuzzle p)
        {
            for(int i=0;i<5;i++)
            {
                int[] row=board.Skip(i*5).Take(5).ToArray(),col=Enumerable.Range(0,5).Select(r=>board[r*5+i]).ToArray();
                if(row.Distinct().Count()!=5 || col.Distinct().Count()!=5 || TowerSolver.Visible(row)!=p.Left[i] || TowerSolver.Visible(row.Reverse())!=p.Right[i] || TowerSolver.Visible(col)!=p.Top[i] || TowerSolver.Visible(col.Reverse())!=p.Bottom[i]) return false;
            }
            return true;
        }
        public static int Run(string report)
        {
            try
            {
                // Prove the spatial grouping preserves the original first-match
                // and lower-error replacement rules, including moving groups.
                var indexed=new PatternMatches(240);
                var reference=new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<Point,int>>();
                var random=new Random(944041);
                for(int sample=0;sample<5000;sample++)
                {
                    Point point=new Point(random.Next(240),random.Next(180));int error=random.Next(2000);
                    int index=reference.FindIndex(m=>Math.Abs(m.Key.X-point.X)<5&&Math.Abs(m.Key.Y-point.Y)<5);
                    if(index<0)reference.Add(new System.Collections.Generic.KeyValuePair<Point,int>(point,error));
                    else if(error<reference[index].Value)reference[index]=new System.Collections.Generic.KeyValuePair<Point,int>(point,error);
                    indexed.Add(point,error);
                }
                Check(indexed.Items.SequenceEqual(reference),"indexed anchor grouping exactly matches the original grouping rules");
                string assets=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets"),fixtures=Path.Combine(assets,"test_assets");
                var engine=new PuzzleAnalysisEngine(assets);
                long fullTime=0,cachedTime=0,maxLive=0,openingTime=0;
                using(Bitmap original=new Bitmap(Path.Combine(fixtures,"capture","reported-full-game.png")))
                {
                    int[] expected={0,0,0,0,0,0,0,1,0,0,0,1,2,2,0,1,2,0,2,1,2,2,2,1,0};
                    using(AnalysisResult r=engine.Analyze(original,3200,false,CancellationToken.None))
                    {
                        Check(r.Plan!=null && r.Plan.Puzzle.Kind==PuzzleKind.Lockbox,"actual full game finds Lockbox instead of the stone floor as Towers");
                        Check(r.Plan.Puzzle.Board.SequenceEqual(expected),"all 25 actual full-scene symbols match the visual transcription");
                        Check(r.Plan.Puzzle.Grid.Contains(new Point(930,520)),"coordinates point to the real puzzle");
                        Check(!DetectionPolicy.NeedsConfirmation(r.Plan),"full scene has first-read guidance");
                        int[] board=(int[])expected.Clone();
                        for(int i=0;i<25;i++) LockboxSolver.Click(board,i,r.Plan.Lockbox.Clicks[i],1);
                        Check(board.All(v=>v==r.Plan.Lockbox.Target),"full-scene click instructions solve the actual puzzle");
                        fullTime=r.Milliseconds;
                    }
                    using(AnalysisResult cached=engine.Analyze(original,3200,false,CancellationToken.None))
                    { Check(cached.Unchanged && cached.Plan.Puzzle.Kind==PuzzleKind.Lockbox,"only the confirmed real puzzle is cached"); cachedTime=cached.Milliseconds; }
                    var search=new CaptureSearch();search.Choose(original.Size,null);
                    using(Bitmap floor=Pixels.Crop(original,new Rectangle(1540,290,260,320),new Size(260,320)))
                    using(AnalysisResult r=engine.Analyze(floor,3200,false,CancellationToken.None))
                    {
                        Check(r.Plan==null,"the reported patterned floor on its own is rejected");
                        Check(search.Complete(true,DetectionPolicy.CanTrack(r.Plan)),"a false region queues a full-window retry");
                        Check(search.Choose(original.Size,new Rectangle(1560,325,187,187))==new Rectangle(Point.Empty,original.Size),"the floor cannot keep capture locked to stale coordinates");
                    }
                    using(Bitmap empty=EmptyScene(original))
                    {
                        using(AnalysisResult r=engine.Analyze(empty,3200,false,CancellationToken.None)) Check(r.Plan==null,"full game scenery and unrelated panels are rejected when puzzle closes");
                        using(AnalysisResult r=engine.Analyze(empty,3200,false,CancellationToken.None)) Check(r.Plan==null && !r.Unchanged,"invalid detections are never cached as puzzle plans");
                        using(AnalysisResult r=engine.Analyze(original,3200,false,CancellationToken.None)) Check(r.Plan!=null && r.Plan.Puzzle.Kind==PuzzleKind.Lockbox,"real puzzle is reacquired on the same source after scenery rejection");
                        var live=new PuzzleAnalysisEngine(assets);
                        for(int i=0;i<3;i++)
                        {
                            using(AnalysisResult r=live.Analyze(empty,3201,false,CancellationToken.None,true))
                            { Check(r.Plan==null,"progressive live discovery does not accept scenery");maxLive=Math.Max(maxLive,r.Milliseconds); }
                            Thread.Sleep(150);
                        }
                        Stopwatch timer=Stopwatch.StartNew();bool found=false;
                        for(int i=0;i<8 && !found;i++)
                        {
                            using(AnalysisResult r=live.Analyze(original,3201,false,CancellationToken.None,true))
                            { maxLive=Math.Max(maxLive,r.Milliseconds);found=r.Plan!=null && r.Plan.Puzzle.Kind==PuzzleKind.Lockbox; }
                            if(!found) Thread.Sleep(150);
                        }
                        openingTime=timer.ElapsedMilliseconds;Check(found,"opening Lockbox during idle discovery is found without a manual scan");
                        // Keep the real scenery, floor and other game panels around each fixture.
                        engine.UseLegacy(true);
                        using(Bitmap tower=TestFixtures.TowerModal("rev944")) CheckScene(engine,empty,tower,PuzzleKind.Towers,report);
                        using(Bitmap knot=TestFixtures.KnotModal("rev944")) CheckScene(engine,empty,knot,PuzzleKind.Knot,report);
                        int[] slide=Enumerable.Range(0,25).ToArray();slide[24]=23;slide[23]=24;
                        using(Bitmap slider=TestFixtures.SliderImage("castle",slide,"rev944")) CheckScene(engine,empty,slider,PuzzleKind.Sliding,report);
                        engine.UseLegacy(false);
                        foreach(MasterClue clue in MasterClues.All)
                        using(Bitmap screenshot=new Bitmap(Path.Combine(fixtures,"master",clue.Id+".png")))
                        using(Bitmap scene=(Bitmap)empty.Clone())
                        {
                            using(Graphics g=Graphics.FromImage(scene)) g.DrawImageUnscaled(screenshot,750,330);
                            using(AnalysisResult r=engine.Analyze(scene,3202,false,CancellationToken.None))
                            { File.AppendAllText(report,"Master "+clue.Id+" in full game: "+r.Milliseconds+" ms; found "+(r.Plan==null?"none":r.Plan.Puzzle.Kind+" "+(r.Plan.Puzzle.Master==null?"":r.Plan.Puzzle.Master.Id.ToString()))+".\r\n");Check(r.Plan!=null && r.Plan.Puzzle.Master!=null && r.Plan.Puzzle.Master.Id==clue.Id,"all eight master clues switch correctly in real scenery: "+clue.Id); }
                        }
                    }
                }
                var detector=new PuzzleDetector(assets);
                using(Bitmap tower=TestFixtures.TowerImage())
                {
                    using(Graphics g=Graphics.FromImage(tower)) using(Brush brush=new SolidBrush(Color.FromArgb(15,32,40))) g.FillRectangle(brush,45,5,25,25);
                    Check(detector.ReadTowers(tower,new Rectangle(35,35,218,218),1,true)==null,"partial Tower clues are rejected even through the old allowPartial entry point");
                }
                var impossible=new TowerPuzzle{Top=Enumerable.Repeat(1,5).ToArray(),Bottom=Enumerable.Repeat(1,5).ToArray(),Left=Enumerable.Repeat(1,5).ToArray(),Right=Enumerable.Repeat(1,5).ToArray()};
                detector.UseLegacy(true);
                using(Bitmap tower=TestFixtures.TowerModal("rev944",false,false,null,impossible)) Check(detector.Detect(tower)==null,"twenty readable but contradictory Tower clues are rejected");
                detector.UseLegacy(false);
                foreach(string name in new[]{"towers-multiple.png","towers-multiple-small.png"})
                using(Bitmap tower=new Bitmap(Path.Combine(fixtures,"master",name)))
                using(AnalysisResult r=engine.Analyze(tower,3203,false,CancellationToken.None))
                { Check(r.Plan!=null && r.Plan.Towers!=null && r.Plan.Towers.Count>1,"multiple-solution Towers still accepted: "+name);Check(ValidTower(r.Plan.Towers[0],r.Plan.Puzzle.Towers),"every clue independently verifies the selected answer"); }
                Check(!DetectionPolicy.CanTrack(new PuzzlePlan{Puzzle=new DetectedPuzzle{Kind=PuzzleKind.Towers,Bounds=new Rectangle(0,0,200,200),Towers=TestFixtures.TowersExample()}}),"an incomplete plan cannot become a tracking region");
                File.AppendAllText(report,"V0.3.2 FULL-SCENE DISCOVERY PASS: "+assertions+" assertions.\r\nActual reported 2048x966 frame: "+fullTime+" ms; unchanged: "+cachedTime+" ms; longest live stage: "+maxLive+" ms; opening during idle: "+openingTime+" ms.\r\nReal floor negatives, all puzzle types, eight master clues, cache, ROI retry and multiple Tower answers checked. Timing includes image analysis/solve, not native Windows capture or display.\r\n");
                return 0;
            }
            catch(Exception ex) { File.AppendAllText(report,ex+"\r\n");return 1; }
        }
        private static void CheckScene(PuzzleAnalysisEngine engine,Bitmap background,Bitmap puzzle,PuzzleKind kind,string report)
        {
            using(Bitmap image=(Bitmap)background.Clone())
            {
                using(Graphics g=Graphics.FromImage(image)) g.DrawImageUnscaled(puzzle,700,300);
                using(AnalysisResult r=engine.Analyze(image,3202,false,CancellationToken.None))
                { Check(r.Plan!=null && r.Plan.Puzzle.Kind==kind,kind+" switches correctly in full game scenery");Check(DetectionPolicy.CanTrack(r.Plan),kind+" has validated tracking data");File.AppendAllText(report,kind+" in full game: "+r.Milliseconds+" ms.\r\n"); }
            }
        }
    }
}
