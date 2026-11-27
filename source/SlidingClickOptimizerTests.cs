using System;using System.Collections.Generic;using System.Diagnostics;using System.Drawing;using System.IO;using System.Linq;using System.Text;using System.Threading;
namespace KavrosClueHelper
{
    public static class SlidingClickOptimizerTests
    {
        private static IEnumerable<int[]> Next(int[] source)
        {
            int blank=Array.IndexOf(source,24);
            foreach(int direction in new[]{-5,5,-1,1})
            {int[] b=(int[])source.Clone();int p=blank;for(int i=0;i<4;i++){int n=p+direction;if(n<0||n>=25||Math.Abs(direction)==1&&n/5!=p/5)break;b[p]=b[n];b[n]=24;p=n;yield return (int[])b.Clone();}}
        }
        private static bool Finishes(int[] source,IList<int> path)
        {int[] b=(int[])source.Clone();foreach(int n in path){int blank=Array.IndexOf(b,24);if(!SlidingSolver.Neighbours(blank).Contains(n))return false;b[blank]=b[n];b[n]=24;}return b.SequenceEqual(Enumerable.Range(0,25));}
        public static int Run(string report)
        {
            var log=new StringBuilder();int checks=0;Action<bool,string> check=(v,m)=>{checks++;if(!v)throw new Exception(m);};
            try
            {
                int[] goal=Enumerable.Range(0,25).ToArray();var queue=new Queue<int[]>();queue.Enqueue(goal);var depths=new Dictionary<string,int>{{String.Join(",",goal),0}};var samples=new List<int[]>();
                while(queue.Count>0)
                {int[] b=queue.Dequeue();int depth=depths[String.Join(",",b)];check(SlidingClickOptimizer.LowerBound(b)<=depth,"Click lower bound agrees with independent BFS");if(depth==4){if(samples.Count<16)samples.Add(b);continue;}foreach(int[] next in Next(b)){string key=String.Join(",",next);if(!depths.ContainsKey(key)){depths[key]=depth+1;queue.Enqueue(next);}}}
                foreach(int[] b in samples)
                {var result=SlidingClickOptimizer.Solve(b);check(result.Optimal&&SlidingRoute.ClickRoute(b,result.Moves).Count==4,"Shortest click route proven against BFS");check(Finishes(b,result.Moves),"Click route reaches solved state");}
                string assets=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets");
                using(var image=new Bitmap(Path.Combine(assets,"test_assets","reported","slider-native.png")))
                {
                    var detector=new PuzzleDetector(assets);detector.UseLegacy(true);DetectedPuzzle puzzle=detector.Detect(image);var watch=Stopwatch.StartNew();var result=SlidingClickOptimizer.Solve(puzzle.Board);int clicks=SlidingRoute.ClickRoute(puzzle.Board,result.Moves).Count;
                    log.AppendLine("Supplied screenshot: "+clicks+" whole-line clicks, "+result.Moves.Count+" tile shifts, "+watch.ElapsedMilliseconds+"ms; shortest proven="+result.Optimal);
                    check(Finishes(puzzle.Board,result.Moves),"Supplied screenshot click route is valid");check(clicks<85,"Global click search improves the compressed tile route");
                    var engine=new PuzzleAnalysisEngine(assets);engine.UseLegacy(true);using(AnalysisResult first=engine.Analyze(image,1600,false,CancellationToken.None,true,true))
                    {check(first.Plan!=null&&first.Plan.SlidingWholeRows&&Finishes(puzzle.Board,first.Plan.Sliding),"Live analysis uses the selected click cost model");using(AnalysisResult again=engine.Analyze(image,1600,false,CancellationToken.None,true,true))check(again.Unchanged&&again.Plan.SlidingWholeRows,"Unchanged line route is cached");}
                }
                int[] two=(int[])goal.Clone();two[22]=24;two[23]=22;two[24]=23;
                var proofEngine=new PuzzleAnalysisEngine(assets);
                proofEngine.UseLegacy(true);
                using(Bitmap line=TestFixtures.SliderImage("castle",two,"rev944"))
                using(AnalysisResult first=proofEngine.Analyze(line,1800,false,CancellationToken.None,true,true))
                {
                    check(first.Plan!=null&&first.Plan.SlidingWholeRows&&first.Plan.SlidingOptimal&&SlidingRoute.ClickRoute(two,first.Plan.Sliding).Count==1,"A two-tile line is proven one click, with the correct cost model");
                    int[] partial=(int[])two.Clone();partial[22]=22;partial[23]=24;
                    using(Bitmap animation=TestFixtures.SliderImage("castle",partial,"rev944"))
                    using(AnalysisResult midway=proofEngine.Analyze(animation,1800,false,CancellationToken.None,true,true))
                        check(midway.Plan!=null&&!midway.Plan.SlidingOptimal&&midway.Plan.Sliding.Count==1,"An intermediate line animation cannot inherit an invalid shortest-click proof");
                    var switchEngine=new PuzzleAnalysisEngine(assets);
                    switchEngine.UseLegacy(true);
                    using(AnalysisResult original=switchEngine.Analyze(line,1801,false,CancellationToken.None,true,true))
                    using(AnalysisResult switched=switchEngine.Analyze(line,1801,false,CancellationToken.None,true,false))
                        check(switched.Plan!=null&&!switched.Plan.SlidingWholeRows&&switched.Plan.Sliding.Count==2,"Changing the cost model rebuilds the correct route");
                }
                log.AppendLine("PASS: "+checks+" click-optimiser assertions.");File.WriteAllText(report,log.ToString());return 0;
            }
            catch(Exception e){log.AppendLine("FAIL after "+checks+": "+e);File.WriteAllText(report,log.ToString());return 1;}
        }
    }
}
