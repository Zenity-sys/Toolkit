using System;using System.Collections.Generic;using System.Diagnostics;using System.Drawing;using System.IO;using System.Linq;using System.Text;
namespace KavrosClueHelper
{
    public static class SlidingOptimizerTests
    {
        private static int[] Apply(int[] board,IEnumerable<int> route){int[] b=(int[])board.Clone();foreach(int p in route){int blank=Array.IndexOf(b,24);if(!SlidingSolver.Neighbours(blank).Contains(p))throw new Exception("Illegal route");b[blank]=b[p];b[p]=24;}return b;}
        public static int Run(string report)
        {
            var log=new StringBuilder();int checks=0;Action<bool,string> check=(ok,message)=>{checks++;if(!ok)throw new Exception(message);};
            try
            {
                check(SliderPatterns.Count==12,"All additive pattern tables pass their checksums");
                int[] goal=Enumerable.Range(0,25).ToArray();var queue=new Queue<int[]>();queue.Enqueue(goal);var depth=new Dictionary<string,int>{{String.Join(",",goal),0}};var samples=new List<int[]>();
                while(queue.Count>0)
                {int[] b=queue.Dequeue();int d=depth[String.Join(",",b)];if(d==8){if(samples.Count<20)samples.Add(b);continue;}foreach(int n in SlidingSolver.Neighbours(Array.IndexOf(b,24))){int[] c=Apply(b,new[]{n});string key=String.Join(",",c);if(!depth.ContainsKey(key)){depth[key]=d+1;queue.Enqueue(c);}}}
                foreach(var item in depth){int[] state=item.Key.Split(',').Select(Int32.Parse).ToArray();check(SliderPatterns.Bound(state)<=item.Value,"Pattern bound never exceeds independent BFS distance");}
                foreach(int[] b in samples){SlidingOptimization r=SlidingOptimizer.Solve(b);check(r.Optimal&&r.Moves.Count==8,"Optimal route agrees with independent BFS");check(Apply(b,r.Moves).SequenceEqual(goal),"Optimal route reaches goal");}
                string assets=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets");
                using(var native=new Bitmap(Path.Combine(assets,"test_assets","reported","slider-native.png")))
                {
                    var nativeDetector=new PuzzleDetector(assets);nativeDetector.UseLegacy(true);DetectedPuzzle puzzle=nativeDetector.Detect(native);check(puzzle!=null&&puzzle.Kind==PuzzleKind.Sliding,"Supplied native slider is recognised");
                    List<int> old=SlidingRoute.Compact(puzzle.Board,SlidingSolver.Solve(puzzle.Board));var watch=Stopwatch.StartNew();SlidingOptimization route=SlidingOptimizer.Solve(puzzle.Board);
                    check(route.Moves.Count<=old.Count&&Apply(puzzle.Board,route.Moves).SequenceEqual(goal),"Supplied screenshot uses a shorter valid route");
                    var clicks=SlidingRoute.ClickRoute(puzzle.Board,route.Moves);int[] grouped=(int[])puzzle.Board.Clone();
                    foreach(int target in clicks){int blank=Array.IndexOf(grouped,24);check(blank/5==target/5||blank%5==target%5,"Grouped click is aligned with the gap");int step=blank/5==target/5?Math.Sign(target-blank):5*Math.Sign(target-blank);while(blank!=target){int next=blank+step;grouped[blank]=grouped[next];grouped[next]=24;blank=next;}}
                    check(grouped.SequenceEqual(goal),"Grouped clicks reach the same goal");
                    log.AppendLine("Supplied Kavros slider: "+old.Count+" -> "+route.Moves.Count+" moves ("+clicks.Count+" clicks if whole-row moves are supported); "+watch.ElapsedMilliseconds+"ms; lower bound="+route.LowerBound+"; shortest proven="+route.Optimal);
                }
                var random=new Random(6018);int oldTotal=0,newTotal=0;
                for(int trial=0;trial<12;trial++)
                {int[] b=(int[])goal.Clone();int last=-1;for(int i=0;i<160;i++){int blank=Array.IndexOf(b,24);int[] options=SlidingSolver.Neighbours(blank).Where(p=>p!=last).ToArray();int n=options[random.Next(options.Length)];b=Apply(b,new[]{n});last=blank;}
                 List<int> old=SlidingRoute.Compact(b,SlidingSolver.Solve(b));var watch=Stopwatch.StartNew();SlidingOptimization r=SlidingOptimizer.Solve(b);oldTotal+=old.Count;newTotal+=r.Moves.Count;
                 check(r.Moves.Count<=old.Count,"Optimisation never lengthens route");check(Apply(b,r.Moves).SequenceEqual(goal),"Optimised hard route reaches goal");check(!r.Optimal||r.Moves.Count>=r.LowerBound,"Optimal flag is consistent");log.AppendLine("Board "+trial+": "+old.Count+" -> "+r.Moves.Count+" moves; "+watch.ElapsedMilliseconds+"ms; shortest proven="+r.Optimal);}
                check(newTotal<oldTotal,"Routes improve across hard fixtures");log.AppendLine("PASS: "+checks+" optimiser assertions; total moves "+oldTotal+" -> "+newTotal);File.WriteAllText(report,log.ToString());return 0;
            }
            catch(Exception e){log.AppendLine("FAIL after "+checks+": "+e);File.WriteAllText(report,log.ToString());return 1;}
        }
    }
}
