using System;using System.Collections.Generic;using System.Diagnostics;using System.Linq;
namespace KavrosClueHelper
{
    // Whole-line actions have their own cost model. Counting tile shifts and
    // merely changing the number on the UI would not minimise mouse clicks.
    public static class SlidingClickOptimizer
    {
        private struct Key : IEquatable<Key>
        {public ulong A,B;public int Axis;public bool Equals(Key b){return A==b.A&&B==b.B&&Axis==b.Axis;}public override bool Equals(object b){return b is Key&&Equals((Key)b);}public override int GetHashCode(){return (int)(A^(A>>32)^B^(B>>32)^((ulong)Axis<<24));}}
        private sealed class Node {public int[] Board;public int Blank,Parent,Target,Depth,H,TileH,Axis;public Key Key;}
        private static Key Pack(int[] board,int axis)
        {var k=new Key{Axis=axis};for(int i=0;i<25;i++){int bit=i*5;ulong v=(ulong)board[i];if(bit<64){k.A|=v<<bit;if(bit>59)k.B|=v>>(64-bit);}else k.B|=v<<(bit-64);}return k;}
        public static int LowerBound(int[] board)
        {
            var x=new int[4];var y=new int[4];int h=0,v=0;
            for(int p=0;p<25;p++)if(board[p]!=24)
            {int gx=board[p]%5,gy=board[p]/5,px=p%5,py=p/5;h+=Math.Abs(gx-px);v+=Math.Abs(gy-py);for(int i=Math.Min(gx,px);i<Math.Max(gx,px);i++)x[i]++;for(int i=Math.Min(gy,py);i<Math.Max(gy,py);i++)y[i]++;}
            return Math.Max(x.Max()+y.Max(),(h+3)/4+(v+3)/4);
        }
        private static List<int> Expand(int[] initial,IEnumerable<int> clicks)
        {
            var route=new List<int>();int blank=Array.IndexOf(initial,24);
            foreach(int target in clicks){if(blank/5!=target/5&&blank%5!=target%5)throw new InvalidOperationException("Invalid whole-line click");int step=blank/5==target/5?Math.Sign(target-blank):5*Math.Sign(target-blank);while(blank!=target){blank+=step;route.Add(blank);}}
            return route;
        }
        private sealed class SearchResult {public List<int> Moves;public bool Exhausted;}
        private static SearchResult Search(int[] initial,int ceiling,int weight,int milliseconds)
        {
            var clock=Stopwatch.StartNew();var nodes=new List<Node>();var heap=new List<int>();var best=new Dictionary<Key,int>();
            Func<int,int,bool> before=(a,b)=>{var x=nodes[a];var y=nodes[b];int hx=weight==1000?x.H:Math.Max(x.H,(x.TileH+1)/2),hy=weight==1000?y.H:Math.Max(y.H,(y.TileH+1)/2);int f=1000*x.Depth+weight*hx,g=1000*y.Depth+weight*hy;return f<g||f==g&&(x.TileH<y.TileH||x.TileH==y.TileH&&x.H<y.H);};
            Action<int> push=id=>{heap.Add(id);int p=heap.Count-1;while(p>0){int q=(p-1)/2;if(!before(heap[p],heap[q]))break;int swap=heap[p];heap[p]=heap[q];heap[q]=swap;p=q;}};
            var root=new Node{Board=(int[])initial.Clone(),Blank=Array.IndexOf(initial,24),Parent=-1,Axis=2,H=LowerBound(initial),TileH=SliderPatterns.Bound(initial),Key=Pack(initial,2)};nodes.Add(root);best[root.Key]=0;push(0);int visits=0;
            while(heap.Count>0&&nodes.Count<300000)
            {
                if((++visits&255)==0){SearchCancellation.Check();if(clock.ElapsedMilliseconds>=milliseconds)return new SearchResult();}
                int id=heap[0],last=heap[heap.Count-1];heap.RemoveAt(heap.Count-1);if(heap.Count>0){heap[0]=last;int p=0;while(true){int a=p*2+1;if(a>=heap.Count)break;int b=a+1,c=b<heap.Count&&before(heap[b],heap[a])?b:a;if(!before(heap[c],heap[p]))break;int swap=heap[p];heap[p]=heap[c];heap[c]=swap;p=c;}}
                Node node=nodes[id];int known;if(!best.TryGetValue(node.Key,out known)||known!=node.Depth)continue;
                if(node.H==0){var clicks=new List<int>();for(int p=id;nodes[p].Parent>=0;p=nodes[p].Parent)clicks.Add(nodes[p].Target);clicks.Reverse();return new SearchResult{Moves=Expand(initial,clicks)};}
                if(node.Depth+node.H>=ceiling)continue;
                for(int axis=0;axis<2;axis++)
                {
                    if(node.Axis==axis)continue;
                    foreach(int sign in new[]{-1,1})
                    {
                        int step=sign*(axis==0?1:5),blank=node.Blank,steps=0;
                        for(int length=1;length<=4;length++)
                        {
                            int next=blank+step;if(next<0||next>=25||axis==0&&next/5!=blank/5)break;
                            node.Board[blank]=node.Board[next];node.Board[next]=24;blank=next;steps++;
                            Key key=Pack(node.Board,axis);int depth=node.Depth+1;
                            if(!best.TryGetValue(key,out known)||known>depth)
                            {int h=LowerBound(node.Board);if(depth+h<ceiling){int child=nodes.Count;nodes.Add(new Node{Board=(int[])node.Board.Clone(),Blank=blank,Parent=id,Target=blank,Depth=depth,H=h,TileH=SliderPatterns.Bound(node.Board),Axis=axis,Key=key});best[key]=depth;push(child);}}
                        }
                        while(steps-->0){int previous=blank-step;node.Board[blank]=node.Board[previous];node.Board[previous]=24;blank=previous;}
                    }
                }
            }
            return new SearchResult{Exhausted=heap.Count==0};
        }
        // A whole-row click moves one to four tiles. Two consecutive clicks on the
        // same axis can always be combined or removed. IDA* explores both axes
        // at the root and alternates thereafter, with no time or node cutoff.
        private sealed class LineProof
        {
            private readonly int[] board, path;
            private readonly Dictionary<Key, int> seen = new Dictionary<Key, int>();
            private int visits;
            public List<int> Result;

            public LineProof(int[] start, int upper)
            {
                board = (int[])start.Clone();
                path = new int[upper + 1];
            }

            public bool Search(int upper)
            {
                int blank = Array.IndexOf(board, 24);
                for (int bound = LowerBound(board); bound < upper; bound++)
                {
                    SearchCancellation.Check();
                    if (ExactSlider.BoundChanged != null) ExactSlider.BoundChanged(bound);
                    seen.Clear();
                    if (Visit(blank, 2, 0, bound)) return true;
                }
                return false;
            }

            private bool Visit(int blank, int lastAxis, int depth, int bound)
            {
                if ((++visits & 255) == 0) SearchCancellation.Check();
                int h = LowerBound(board);
                if (depth + h > bound) return false;
                if (h == 0)
                {
                    Result = path.Take(depth).ToList();
                    return true;
                }
                if (depth + (SliderPatterns.Bound(board) + 3) / 4 > bound) return false;
                Key key = Pack(board, lastAxis);
                int known;
                if (seen.TryGetValue(key, out known) && known <= depth) return false;
                // Stopping cache insertions bounds memory, not the search.
                if (seen.Count < 180000) seen[key] = depth;
                for (int axis = 0; axis < 2; axis++)
                {
                    if (axis == lastAxis) continue;
                    for (int sign = -1; sign <= 1; sign += 2)
                    {
                        int step = sign * (axis == 0 ? 1 : 5);
                        int p = blank, next = p + step;
                        while (next >= 0 && next < 25 && (axis == 1 || next / 5 == p / 5))
                        {
                            board[p] = board[next];
                            board[next] = 24;
                            path[depth] = next;
                            if (Visit(next, axis, depth + 1, bound)) return true;
                            p = next;
                            next += step;
                        }
                        while (p != blank)
                        {
                            board[p] = board[p - step];
                            p -= step;
                        }
                        board[blank] = 24;
                    }
                }
                return false;
            }
        }

        private static int ClickCount(int[] board,IList<int> moves)
        {
            List<int> clicks;
            return SlidingRoute.TryClickRoute(board,moves,out clicks)?clicks.Count:int.MaxValue;
        }
        private static bool Solves(int[] initial,IList<int> moves)
        {
            if(!SlidingRoute.Playable(initial,moves))return false;
            int[] board=(int[])initial.Clone();
            foreach(int next in moves){int blank=Array.IndexOf(board,24);board[blank]=board[next];board[next]=24;}
            return board.SequenceEqual(Enumerable.Range(0,25));
        }
        private static SlidingOptimization Fresh(int[] initial)
        {
            if(!SlidingSolver.IsSolvable(initial))return new SlidingOptimization{Moves=new List<int>(),LowerBound=0,Optimal=false};
            return new SlidingOptimization{Moves=SlidingSolver.Solve(initial),LowerBound=LowerBound(initial),Optimal=false};
        }
        internal static List<int> Prove(int[] initial, List<int> seed)
        {
            if(!SlidingSolver.IsSolvable(initial))return new List<int>();
            List<int> clicks;
            if(!SlidingRoute.TryClickRoute(initial,seed,out clicks))return SlidingSolver.Solve(initial);
            int cost=clicks.Count;
            var proof = new LineProof(initial, cost);
            List<int> proven=proof.Search(cost)?Expand(initial, proof.Result):seed;
            return Solves(initial,proven)?proven:SlidingSolver.Solve(initial);
        }

        public static int SlowSolves;
        // The first step the player sees. The greedy route is immediate.
        // A shorter proof, when one exists, is refined on a background thread.
        public static SlidingOptimization Quick(int[] initial)
        {
            SlidingOptimization route=Fresh(initial);
            if(route.Moves==null||route.Moves.Count==0)return route;
            int clicks=ClickCount(initial,route.Moves);
            route.Optimal=clicks==route.LowerBound&&Solves(initial,route.Moves);
            return route;
        }
        public static SlidingOptimization Solve(int[] initial,int milliseconds=2500)
        {
            SlowSolves++;
            if(!SlidingSolver.IsValid(initial)||!SlidingSolver.IsSolvable(initial))return new SlidingOptimization{Moves=new List<int>(),LowerBound=0,Optimal=false};
            var clock=Stopwatch.StartNew();SlidingOptimization seed=SlidingOptimizer.Solve(initial,Math.Min(1000,milliseconds/2));List<int> best=seed.Moves;int clicks=ClickCount(initial,best),lower=LowerBound(initial);
            if(clicks==lower&&Solves(initial,best))return new SlidingOptimization{Moves=best,LowerBound=lower,Optimal=true};
            if(clicks==int.MaxValue){best=Fresh(initial).Moves;clicks=ClickCount(initial,best);}
            best=SlidingClickRefiner.Refine(initial,best,Math.Min(350,Math.Max(0,milliseconds-(int)clock.ElapsedMilliseconds)));clicks=ClickCount(initial,best);
            int[] weights={5000,3500,2500,1800,1000};
            for(int i=0;i<weights.Length;i++)
            {
                int budget=(int)Math.Max(0,(milliseconds-200-clock.ElapsedMilliseconds)/(weights.Length-i));if(budget<1)break;
                SearchResult result=Search(initial,clicks==int.MaxValue?lower+8:clicks,weights[i],budget);
                if(result.Moves!=null){int count=ClickCount(initial,result.Moves);if(count<clicks){best=result.Moves;clicks=count;}if(weights[i]==1000&&Solves(initial,best))return new SlidingOptimization{Moves=best,LowerBound=clicks,Optimal=true};}
                else if(result.Exhausted&&Solves(initial,best))return new SlidingOptimization{Moves=best,LowerBound=clicks,Optimal=true};
            }
            best=SlidingClickRefiner.Refine(initial,best,(int)Math.Max(0,milliseconds-clock.ElapsedMilliseconds));clicks=ClickCount(initial,best);
            if(!Solves(initial,best))return Fresh(initial);
            return new SlidingOptimization{Moves=best,LowerBound=lower,Optimal=clicks==lower};
        }
    }
}
