using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;

namespace KavrosClueHelper
{
    public static class MasterRegressionTests
    {
        private static int assertions;
        private static void Check(bool condition,string message) { assertions++; if(!condition) throw new Exception("FAILED: "+message); }
        private static bool ValidTower(int[] board,TowerPuzzle puzzle)
        {
            for(int i=0;i<5;i++)
            {
                int[] row=Enumerable.Range(0,5).Select(c=>board[i*5+c]).ToArray(),column=Enumerable.Range(0,5).Select(r=>board[r*5+i]).ToArray();
                if(!row.OrderBy(v=>v).SequenceEqual(new[]{1,2,3,4,5}) || !column.OrderBy(v=>v).SequenceEqual(new[]{1,2,3,4,5})) return false;
                if(TowerSolver.Visible(row)!=puzzle.Left[i] || TowerSolver.Visible(row.Reverse())!=puzzle.Right[i] || TowerSolver.Visible(column)!=puzzle.Top[i] || TowerSolver.Visible(column.Reverse())!=puzzle.Bottom[i]) return false;
            }
            return true;
        }
        public static int Run(string report)
        {
            try
            {
                string assets=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets"),fixtures=Path.Combine(assets,"test_assets","master");
                string render=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report)),"master-previews");Directory.CreateDirectory(render);
                PuzzleAnalysisEngine engine=new PuzzleAnalysisEngine(assets);
                long longest=0;
                foreach(MasterClue clue in MasterClues.All)
                {
                    Check(File.Exists(Path.Combine(assets,"master-maps",clue.MapFile)),"offline map exists for "+clue.Id);
                    Check(clue.Area.Width>0 && clue.Area.Height>0 && clue.Equipment.Length>0,"clue has a completion area and equipment");
                    using(Bitmap map=new Bitmap(Path.Combine(assets,"master-maps",clue.MapFile)))
                    using(Bitmap card=new Bitmap(460,471))
                    { using(Graphics g=Graphics.FromImage(card)) MasterClueDrawing.Draw(g,new Rectangle(0,0,460,471),clue,map); card.Save(Path.Combine(render,clue.Id+".png")); }
                    foreach(float factor in new[]{1F,0.8F,1.25F,1.5F,2F,0.6F})
                    using(Bitmap original=new Bitmap(Path.Combine(fixtures,clue.Id+".png")))
                    using(Bitmap scaled=Pixels.Crop(original,new Rectangle(Point.Empty,original.Size),new Size((int)Math.Round(original.Width*factor),(int)Math.Round(original.Height*factor))))
                    using(AnalysisResult result=engine.Analyze(scaled,clue.Id*100+(int)(factor*10),false,CancellationToken.None))
                    {
                        Check(result.Plan!=null && result.Plan.Puzzle.Kind==PuzzleKind.EmoteClue,"master clue found at "+factor+": "+clue.Id);
                        Check(result.Plan.Puzzle.Master.Id==clue.Id,"correct clue at "+factor+": "+clue.Id);
                        longest=Math.Max(longest,result.Milliseconds);
                        using(AnalysisResult cached=engine.Analyze(scaled,clue.Id*100+(int)(factor*10),false,CancellationToken.None))
                        { Check(cached.Unchanged,"unchanged clue reuses its plan");Check(cached.Preview==null,"unchanged clue avoids image allocation"); }
                    }
                }
                // Keep the game size/source constant while changing the clue,
                // then switch to a puzzle: this exercises automatic following.
                foreach(MasterClue clue in MasterClues.All)
                using(Bitmap original=new Bitmap(Path.Combine(fixtures,clue.Id+".png")))
                using(Bitmap game=new Bitmap(2560,1440))
                {
                    using(Graphics g=Graphics.FromImage(game)) { g.Clear(Color.FromArgb(34,57,79));g.DrawImageUnscaled(original,990,510); }
                    using(AnalysisResult result=engine.Analyze(game,3000,false,CancellationToken.None))
                    { Check(result.Plan!=null && result.Plan.Puzzle.Master!=null && result.Plan.Puzzle.Master.Id==clue.Id,"switches to next clue on a full-resolution game frame"); longest=Math.Max(longest,result.Milliseconds); }
                }
                engine.UseLegacy(true);
                using(Bitmap original=TestFixtures.LockboxImage("rev944")) using(Bitmap game=new Bitmap(2560,1440))
                {
                    using(Graphics g=Graphics.FromImage(game)) { g.Clear(Color.FromArgb(34,57,79));g.DrawImageUnscaled(original,990,510); }
                    using(AnalysisResult result=engine.Analyze(game,3000,false,CancellationToken.None)) Check(result.Plan!=null && result.Plan.Puzzle.Kind==PuzzleKind.Lockbox,"switches from master map back to puzzle guidance");
                engine.UseLegacy(false);
                }
                foreach(var example in new[]{new {Name="laugh-alternate.png",Id=236},new {Name="cheer-alternate.png",Id=233}})
                using(Bitmap image=new Bitmap(Path.Combine(fixtures,example.Name)))
                using(AnalysisResult result=engine.Analyze(image,example.Id,false,CancellationToken.None))
                    Check(result.Plan!=null && result.Plan.Puzzle.Master!=null && result.Plan.Puzzle.Master.Id==example.Id,"different screenshot of same clue still recognised: "+example.Name);
                foreach(string negative in new[]{"scan-prifddinas.png","scan-heart.png"})
                using(Bitmap image=new Bitmap(Path.Combine(fixtures,negative)))
                using(AnalysisResult result=engine.Analyze(image,4000,false,CancellationToken.None))
                    Check(result.Plan!=null&&result.Plan.Puzzle.Kind==PuzzleKind.Scan,"scan clue recognised: "+negative);
                using(Bitmap blank=new Bitmap(520,340))
                {
                    using(Graphics g=Graphics.FromImage(blank)) g.Clear(Color.FromArgb(220,193,145));
                    using(AnalysisResult result=engine.Analyze(blank,4001,false,CancellationToken.None)) Check(result.Plan==null,"blank parchment is not a recognised clue");
                }
                using(Bitmap idle=new Bitmap(800,600))
                {
                    using(Graphics g=Graphics.FromImage(idle)) g.Clear(Color.FromArgb(34,57,79));
                    using(AnalysisResult first=engine.Analyze(idle,4002,false,CancellationToken.None,true)) Check(first.Plan==null,"idle live frame has no puzzle");
                    int broad=engine.BroadSearches;
                    using(Graphics g=Graphics.FromImage(idle)) g.FillRectangle(Brushes.Blue,5,5,5,5);
                    using(AnalysisResult second=engine.Analyze(idle,4002,false,CancellationToken.None,true)) Check(second.Plan==null,"changed idle scene stays in lightweight search");
                    Check(engine.BroadSearches==broad,"idle polling does not repeat the expensive search on every capture");
                    using(Bitmap clue=new Bitmap(Path.Combine(fixtures,"236.png"))) using(Graphics g=Graphics.FromImage(idle)) g.DrawImageUnscaled(clue,100,100);
                    using(AnalysisResult next=engine.Analyze(idle,4002,false,CancellationToken.None,true)) Check(next.Plan!=null && next.Plan.Puzzle.Master!=null && next.Plan.Puzzle.Master.Id==236,"new clue is still found immediately while broad searches are throttled");
                    Check(engine.BroadSearches==broad,"new master recognition avoids broad image search");
                }
                TowerPuzzle[] expected={
                    new TowerPuzzle {Top=new[]{2,3,4,1,2},Bottom=new[]{3,1,2,3,2},Left=new[]{2,3,1,2,2},Right=new[]{2,1,3,3,2}},
                    new TowerPuzzle {Top=new[]{3,3,3,1,2},Bottom=new[]{3,2,1,2,3},Left=new[]{4,3,1,2,3},Right=new[]{2,1,4,2,3}}
                };
                int sample=0;
                foreach(string file in new[]{"towers-multiple-small.png","towers-multiple.png"})
                using(Bitmap image=new Bitmap(Path.Combine(fixtures,file)))
                using(AnalysisResult result=engine.Analyze(image,5000+sample,false,CancellationToken.None))
                {
                    TowerPuzzle clues=expected[sample++];
                    Check(result.Plan!=null && result.Plan.Puzzle.Kind==PuzzleKind.Towers,"reads supplied tower screenshot "+file);
                    Check(result.Plan.Puzzle.Towers.Key==clues.Key,"all twenty clues match supplied screenshot "+file);
                    Check(result.Plan.Towers!=null && result.Plan.Towers.Count>1,"multiple tower solutions are retained");
                    int[] selected=result.Plan.Towers[0];
                    Check(ValidTower(selected,clues),"selected answer independently satisfies every tower constraint");
                    Check(GuidanceDrawing.TowerMarks(result.Plan.Puzzle,selected,true).Count==25,"multiple-solution puzzle has all 25 guidance labels");
                    List<int[]> pinned=null;
                    foreach(int height in new[]{0,1,3,5})
                    {
                        int[] entered=new int[25];entered[0]=height;
                        engine.UseLegacy(true);
                        using(Bitmap changed=TestFixtures.TowerModal("rev944",false,false,entered,clues))
                        using(AnalysisResult reread=engine.Analyze(changed,6000+sample,false,CancellationToken.None))
                        {
                            Check(reread.Plan!=null && reread.Plan.Towers!=null,"partly filled ambiguous tower remains solvable");
                            Check(reread.Plan.Towers[0].SequenceEqual(selected),"selected tower answer stays fixed as entered heights change");
                            if(pinned==null) pinned=reread.Plan.Towers; else Check(Object.ReferenceEquals(pinned,reread.Plan.Towers),"ambiguous tower reuses cached solutions");
                        }
                        engine.UseLegacy(false);
                    }
                }
                File.AppendAllText(report,"MASTER / TOWER REGRESSIONS PASS: "+assertions+" assertions.\r\nAll eight supplied master clues, alternate captures, six sizes, 2560x1440 automatic switching, offline map cards, scan recognition and both supplied ambiguous Towers checked.\r\nLongest supported clue recognition in this run: "+longest+" ms.\r\nNo windows were shown or application input sent.\r\n");
                return 0;
            }
            catch(Exception ex) { File.AppendAllText(report,ex+"\r\n");return 1; }
        }
    }
}
