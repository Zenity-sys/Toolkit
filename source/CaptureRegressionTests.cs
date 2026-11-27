using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace KavrosClueHelper
{
    public static class CaptureRegressionTests
    {
        private static int assertions;
        private static void Check(bool condition,string message) { assertions++; if(!condition) throw new Exception("FAILED: "+message); }
        private static Bitmap Game(Bitmap puzzle,Point where)
        {
            Bitmap image=new Bitmap(2560,1440);
            using(Graphics g=Graphics.FromImage(image)) { g.Clear(Color.FromArgb(34,57,79));g.DrawImageUnscaled(puzzle,where); }
            return image;
        }
        public static int Run(string report)
        {
            try
            {
                string assets=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets");
                string input=Path.Combine(assets,"test_assets","capture","reported-lockbox.png");
                int[] board={0,1,1,0,1, 0,0,0,1,2, 0,1,1,2,0, 0,0,2,0,0, 0,1,1,1,0};
                PuzzleAnalysisEngine engine=new PuzzleAnalysisEngine(assets);
                CaptureSearch search=new CaptureSearch();
                long foundTime=0,retryTime=0;
                using(Bitmap lockbox=new Bitmap(input))
                {
                    using(AnalysisResult image=engine.Analyze(lockbox,3101,false,CancellationToken.None))
                    {
                        Check(image.Plan!=null && image.Plan.Puzzle.Kind==PuzzleKind.Lockbox,"reported screenshot is a lockbox");
                        Check(image.Plan.Puzzle.Board.SequenceEqual(board),"all 25 supplied lockbox symbols are correct");
                        Check(!DetectionPolicy.NeedsConfirmation(image.Plan),"confident lockbox displays on the first successful read");
                        int[] solved=(int[])board.Clone();
                        for(int i=0;i<25;i++) LockboxSolver.Click(solved,i,image.Plan.Lockbox.Clicks[i],1);
                        Check(solved.All(v=>v==image.Plan.Lockbox.Target),"reported lockbox click plan solves the actual board");
                    }
                    using(Bitmap first=Game(lockbox,new Point(200,180)))
                    using(Bitmap moved=Game(lockbox,new Point(1510,820)))
                    {
                        Rectangle full=new Rectangle(Point.Empty,first.Size);
                        Check(search.Choose(first.Size,null)==full,"new live source starts with a full-window capture");
                        Rectangle previous;
                        using(AnalysisResult found=engine.Analyze(first,3102,false,CancellationToken.None,true))
                        { Check(found.Plan!=null && found.Plan.Puzzle.Kind==PuzzleKind.Lockbox,"lockbox found on the full 2560x1440 game image");previous=found.Plan.Puzzle.Bounds;foundTime=found.Milliseconds; }
                        // Keep the absolute board fixed while the live capture moves.
                        // The second crop has the same size as the first but a different origin.
                        var framed = new PuzzleAnalysisEngine(assets);
                        int fullSearches;
                        using (AnalysisResult located = framed.Analyze(first, 720, false, CancellationToken.None, true, false, Point.Empty))
                        {
                            Check(located.Plan != null, "initial full capture for translated follow");
                            fullSearches = framed.FullSearches;
                        }
                        foreach (Point origin in new[] {new Point(130, 100), new Point(150, 110)})
                        {
                            Rectangle areaOnClient = new Rectangle(origin, new Size(640, 580));
                            using (Bitmap region = first.Clone(areaOnClient, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                            using (AnalysisResult following = framed.Analyze(region, 720, false, CancellationToken.None, true, false, origin))
                            {
                                Check(following.Unchanged && following.Plan.Puzzle.Board.SequenceEqual(board), "first crop reuses only a pixel-confirmed board");
                                Check(following.Plan.Puzzle.OffsetBy(origin).Bounds == previous, "overlay maps back to the exact same game coordinates");
                                Check(framed.FullSearches == fullSearches, "capture origin changes avoid an unnecessary discovery pass");
                            }
                        }
                        Rectangle cropped=search.Choose(moved.Size,previous);
                        Check(cropped!=full,"following a recognised puzzle uses its region");
                        using(Bitmap oldArea=Pixels.Crop(moved,cropped,cropped.Size))
                        using(AnalysisResult missed=engine.Analyze(oldArea,3102,false,CancellationToken.None,true))
                        {
                            Check(missed.Plan==null,"old puzzle area contains only scenery after puzzle moves");
                            Check(!search.Complete(true,false),"one confident miss keeps the known puzzle region");
                        }
                        Check(search.Choose(moved.Size,previous)!=full,"one miss does not abandon the puzzle region");
                        for(int i=1;i<CaptureSearch.MissesBeforeSearch-1;i++)Check(!search.Complete(true,false),"miss "+(i+1)+" still watches the puzzle region");
                        Check(search.Complete(true,false),"several confident misses return to a full-window search");
                        Check(search.Choose(moved.Size,previous)==full,"after several misses the next capture searches the whole window");
                        using(AnalysisResult reacquired=engine.Analyze(moved,3102,false,CancellationToken.None,true))
                        {
                            Check(reacquired.Plan!=null && reacquired.Plan.Puzzle.Kind==PuzzleKind.Lockbox,"moved puzzle is found on the immediate full-window retry");
                            Check(reacquired.Plan.Puzzle.Grid.X>1500 && reacquired.Plan.Puzzle.Grid.Y>800,"new guidance uses the new puzzle coordinates");
                            Check(reacquired.Plan.Puzzle.Board.SequenceEqual(board),"reacquired board still matches all 25 symbols");
                            retryTime=reacquired.Milliseconds;
                        }
                        search.Reset();
                        Check(search.Choose(first.Size,new Rectangle(20,40,220,240))==full,"resuming live reading discards screenshot/example coordinates");
                        search.Choose(first.Size,previous);
                        Size resized=new Size(1920,1080);
                        Check(search.Choose(resized,previous)==new Rectangle(Point.Empty,resized),"a resized client forces fresh full-window discovery");
                        search.ForceFullWindow();
                        Check(search.Choose(first.Size,previous)==full,"Scan now always starts at full-window scope");
                    }
                }
                Rectangle desktop = new Rectangle(-1920, 0, 3840, 1080);
                Check(CaptureSearch.VisibleClient(new Rectangle(-2020,-60,900,600),desktop)==new Rectangle(100,60,800,540),"negative monitor and clipped top-left preserve client offsets");
                Check(CaptureSearch.VisibleClient(new Rectangle(1800,1000,400,300),desktop)==new Rectangle(0,0,120,80),"right/bottom clipping preserves client origin");
                Check(CaptureSearch.VisibleClient(new Rectangle(2000,1100,400,300),desktop).IsEmpty,"fully off-screen window produces no capture");
                Rectangle area=new Rectangle(100,100,200,200);
                Rectangle panel=new Rectangle(170,170,100,100);
                Check(GameVisibility.Any(area,p=>true) && GameVisibility.Clear(area,p=>true),"an unobstructed background game is readable without a foreground requirement");
                Check(GameVisibility.Any(area,p=>!panel.Contains(p)),"a partially visible client can still be searched");
                Check(!GameVisibility.Clear(area,p=>!panel.Contains(p)),"a covered puzzle centre cannot supply guidance from another window");
                Check(!GameVisibility.Any(area,p=>false),"a completely covered game is not captured");
                engine.UseLegacy(true);
                using(Bitmap source=TestFixtures.LockboxImage("classic"))
                using(Bitmap game=Game(source,new Point(1193,677)))
                using(AnalysisResult legacy=engine.Analyze(game,3103,false,CancellationToken.None)) Check(legacy.Plan!=null && legacy.Plan.Puzzle.Kind==PuzzleKind.Lockbox,"indexed anchor search also finds the classic frame");
                File.AppendAllText(report,"V0.3.1 CAPTURE / DISCOVERY REGRESSIONS PASS: "+assertions+" assertions.\r\nSupplied lockbox, first-read guidance, scenery-only stale region, immediate full-window reacquisition, source reset, resizing, occlusion and indexed frame discovery checked without windows or input.\r\n2560x1440 reported lockbox: "+foundTime+" ms; moved puzzle reacquisition: "+retryTime+" ms (analysis only).\r\n");
                return 0;
            }
            catch(Exception ex) { File.AppendAllText(report,ex+"\r\n");return 1; }
        }
    }
}
