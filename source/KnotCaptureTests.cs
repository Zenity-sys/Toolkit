using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
namespace KavrosClueHelper
{
    public static class KnotCaptureTests
    {
        public static int Run(string report)
        {
            int count=0;
            try
            {
                Action<bool,string> check=(ok,msg)=>{count++;if(!ok)throw new Exception(msg);};
                string assets=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets");
                var detector=new PuzzleDetector(assets);
                using(Bitmap dark=new Bitmap(Path.Combine(assets,"test_assets","knot","darkscape-1920.png")))
                    Solve(detector,dark,"DarkScape 1920x1027",check);
                using(Bitmap swamp=new Bitmap(Path.Combine(assets,"test_assets","knot","darkscape-knot-1920.png")))
                    Solve(detector,swamp,"DarkScape knot 1920x1027",check);
                using(Bitmap modern=new Bitmap(Path.Combine(assets,"test_assets","knot","modern-2560.png")))
                {
                    Solve(detector,modern,"Modern 2560x1369",check);
                    using(Bitmap sand=new Bitmap(modern))
                    using(Graphics g=Graphics.FromImage(sand))
                    {
                        g.FillRectangle(Brushes.Black,1000,500,560,360);
                        detector.ResetSearch();
                        DetectedPuzzle none=detector.DetectBoards(sand,new Pixels(sand),true,true);
                        check(none==null||none.Kind!=PuzzleKind.Knot,"beach sand without the parchment is not a knot "+(none==null?"none":none.Kind.ToString()));
                    }
                }
                using(Bitmap modal=TestFixtures.KnotModal())
                {
                    var engine=new PuzzleAnalysisEngine(assets);
                    using(AnalysisResult first=engine.Analyze(modal,6000,false,CancellationToken.None,true))
                        check(first.Plan!=null&&!first.Plan.KnotMoving&&first.Plan.Knot!=null,"synthetic knot shows counts on the first still frame");
                }
                File.WriteAllText(report,"KNOT CAPTURE PASS: "+count+" assertions.\r\n");
                return 0;
            }
            catch(Exception ex)
            {
                File.WriteAllText(report,"FAIL after "+count+": "+ex+"\r\n");
                return 1;
            }
        }
        static void Solve(PuzzleDetector detector,Bitmap image,string name,Action<bool,string> check)
        {
            detector.ResetSearch();
            Pixels pixels=new Pixels(image);
            DetectedPuzzle found=null;int ticks=0;string times="";
            for(;ticks<2&&(found==null||found.Kind!=PuzzleKind.Knot);ticks++)
            {
                Stopwatch watch=Stopwatch.StartNew();
                found=detector.DetectBoards(image,pixels,true,true);
                times+=(times.Length==0?"":",")+watch.ElapsedMilliseconds;
            }
            check(found!=null&&found.Kind==PuzzleKind.Knot,name+" live polling finds the knot in 2 ticks ("+times+" ms) "+(found==null?"none":found.Kind.ToString()));
            if(found==null||found.Knot==null)return;
            check(found.Knot.Reliable,name+" first read is reliable "+found.Knot.MinRune.ToString("0.000")+"/"+found.Knot.MinTrack.ToString("0.000")+"/"+found.Knot.MinMargin.ToString("0.000"));
            KnotSolution solved=KnotSolver.Solve(found.Knot);
            if(solved!=null)
            {
                bool inRange=true;
                for(int i=0;i<solved.Offsets.Length;i++)if(Math.Abs(solved.Offsets[i])>=found.Knot.Lanes[i].Length)inRange=false;
                check(inRange&&solved.Offsets.Length==found.Knot.Lanes.Length,name+" click counts stay inside each ring "+string.Join(",",solved.Offsets.Select(v=>v.ToString()).ToArray()));
                bool met=true;
                foreach(KnotLock l in found.Knot.Locks)
                {
                    int a=KnotSolver.Mod(l.IndexA-solved.Offsets[l.A],found.Knot.Lanes[l.A].Length);
                    int b=KnotSolver.Mod(l.IndexB-solved.Offsets[l.B],found.Knot.Lanes[l.B].Length);
                    if(!KnotSolver.Equal(found.Knot.Lanes[l.A][a],found.Knot.Lanes[l.B][b]))met=false;
                }
                check(met,name+" rotations meet every lock");
            }
            var engine=new PuzzleAnalysisEngine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets"));
            using(AnalysisResult live=engine.Analyze(image,7000,false,CancellationToken.None,true))
            {
                bool shown=live.Plan!=null&&live.Plan.Puzzle!=null&&live.Plan.Puzzle.Kind==PuzzleKind.Knot&&!live.Plan.KnotMoving;
                string counts=live.Plan==null||live.Plan.Knot==null?"rotate once":string.Join(",",live.Plan.Knot.Offsets.Select(v=>v.ToString()).ToArray());
                check(shown,name+" shows the knot on the first still frame ("+counts+") "+live.Milliseconds+"ms");
            }
        }
    }
}
