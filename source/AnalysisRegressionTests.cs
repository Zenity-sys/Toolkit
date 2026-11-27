using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KavrosClueHelper
{
    public static class AnalysisRegressionTests
    {
        private static int assertions;
        private static void Check(bool value,string message) { assertions++; if(!value) throw new Exception("FAILED: "+message); }
        public static int Run(string report)
        {
            try
            {
                string assets=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets");
                PuzzleAnalysisEngine engine=new PuzzleAnalysisEngine(assets);
                engine.UseLegacy(true);
                long initialTime,cachedTime;
                using(Bitmap tower=TestFixtures.TowerModal("rev944"))
                {
                    using(AnalysisResult first=engine.Analyze(tower,1,false,CancellationToken.None))
                    {
                        Check(first.Plan!=null && first.Plan.Puzzle.Kind==PuzzleKind.Towers,"old tower font and original number positions recognised");
                        Check(first.Plan.Puzzle.Towers.Key==TestFixtures.TowersExample().Key,"all twenty original-position tower clues read correctly");
                        Check(first.Plan.Towers!=null && first.Plan.Towers.Count==1,"legacy tower board solved");
                        initialTime=first.Milliseconds;
                    }
                    int searches=engine.FullSearches;
                    using(AnalysisResult same=engine.Analyze(tower,1,false,CancellationToken.None))
                    { Check(same.Unchanged,"unchanged puzzle takes cached path"); Check(engine.FullSearches==searches,"unchanged frame skips full image search"); Check(same.Preview==null,"unchanged frame avoids redundant preview allocation"); cachedTime=same.Milliseconds; }
                    using(CancellationTokenSource cancelled=new CancellationTokenSource())
                    {
                        cancelled.Cancel(); bool threw=false;
                        try { using(AnalysisResult ignored=engine.Analyze(tower,2,false,cancelled.Token)) {} } catch(OperationCanceledException) { threw=true; }
                        Check(threw,"cancelled analysis stops before image search");
                    }
                    int callingThread=Thread.CurrentThread.ManagedThreadId,analysisThread=callingThread;
                    using(AnalysisResult result=Task.Factory.StartNew(()=>{analysisThread=Thread.CurrentThread.ManagedThreadId;return engine.Analyze(tower,3,false,CancellationToken.None);}).GetAwaiter().GetResult())
                    { Check(analysisThread!=callingThread,"engine runs on background task without windows or input"); Check(result.Plan!=null,"engine recovers after cancellation"); }
                }
                // Full game resolution, with the old tower fixture centred.
                using(Bitmap solved=TestFixtures.TowerModal("rev944",true,true))
                using(AnalysisResult result=engine.Analyze(solved,8,false,CancellationToken.None))
                {
                    Check(result.Plan!=null && result.Plan.Puzzle.Kind==PuzzleKind.Towers,"grey completed tower hints remain readable");
                    Check(result.Plan.Puzzle.Towers.Key==TestFixtures.TowersExample().Key,"all grey clues read with original font");
                    Check(result.Plan.Puzzle.Towers.Cells.SequenceEqual(result.Plan.Towers[0]),"white entered tower heights read correctly");
                }
                foreach(double factor in new[]{0.8,1.25})
                using(Bitmap original=TestFixtures.TowerModal("rev944"))
                using(Bitmap scaled=Pixels.Crop(original,new Rectangle(Point.Empty,original.Size),new Size((int)(original.Width*factor),(int)(original.Height*factor))))
                using(AnalysisResult result=engine.Analyze(scaled,(int)(factor*100),false,CancellationToken.None))
                {
                    Check(result.Plan!=null && result.Plan.Puzzle.Kind==PuzzleKind.Towers,"scaled old tower grid found "+factor+(result.Plan==null?ProbeScaledTower(scaled,factor,assets):""));
                    Check(result.Plan.Puzzle.Towers.Key==TestFixtures.TowersExample().Key,"all scaled tower clues read "+factor+": "+result.Plan.Puzzle.Towers.Key);
                    Check(result.Plan.Towers!=null && result.Plan.Towers.Count==1,"scaled old tower solved "+factor);
                }
                using(Bitmap source=TestFixtures.TowerModal("rev944")) using(Bitmap full=new Bitmap(2560,1369))
                {
                    using(Graphics g=Graphics.FromImage(full)) { g.Clear(Color.FromArgb(50,66,90)); g.DrawImage(source,new Rectangle(900,420,source.Width,source.Height),new Rectangle(Point.Empty,source.Size),GraphicsUnit.Pixel); }
                    using(AnalysisResult result=engine.Analyze(full,4,false,CancellationToken.None))
                    { Check(result.Plan!=null && result.Plan.Puzzle.Kind==PuzzleKind.Towers,"tower found on full-resolution 2560x1369 frame"); Check(result.Plan.Towers.Count==1,"full-resolution tower solved"); File.AppendAllText(report,"\r\nFull-resolution tower analysis: "+result.Milliseconds+" ms.\r\n"); }
                }
                File.AppendAllText(report,"V0.2.2 ANALYSIS REGRESSIONS PASS: "+assertions+" assertions.\r\nLegacy initial analysis: "+initialTime+" ms; unchanged frame: "+cachedTime+" ms.\r\nAll tests ran without showing windows or sending application input.\r\n");
                return 0;
            }
            catch(Exception ex) { File.AppendAllText(report,ex+"\r\n"); return 1; }
        }
        private static string ProbeScaledTower(Bitmap image,double factor,string assets)
        {
            PuzzleDetector detector=new PuzzleDetector(assets);
            Rectangle grid=new Rectangle((int)Math.Round(121*factor),(int)Math.Round(113*factor),(int)Math.Round(218*factor),(int)Math.Round(218*factor));
            DetectedPuzzle result=detector.ReadTowers(image,grid,(float)factor,true);
            return "; direct read "+detector.LastTowerCluesRead+" clues; "+(result==null?"null":result.Towers.Key);
        }
    }
}
