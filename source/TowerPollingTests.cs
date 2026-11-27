using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
namespace KavrosClueHelper
{
    public static class TowerPollingTests
    {
        public static int Run(string report)
        {
            int count=0;
            try
            {
                Action<bool,string> check=(ok,msg)=>{count++;if(!ok)throw new Exception(msg);};
                string assets=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets");
                var detector=new PuzzleDetector(assets);
                using(Bitmap modal=TestFixtures.TowerModal())
                {
                    Poll(detector,Frame(1920,1027,modal,640,280),TestFixtures.TowersExample(),"1920x1027 modern",check);
                    Poll(detector,Frame(2560,1369,modal,1072,588),TestFixtures.TowersExample(),"2560x1369 modern",check);
                    Poll(detector,Frame(3840,2160,modal,1600,1600),TestFixtures.TowersExample(),"3840x2160 modern near the bottom",check);
                }
                using(Bitmap shot=new Bitmap(Path.Combine(assets,"test_assets","darkscape","darkscape-towers-1.png")))
                {
                    TowerPuzzle clues=new TowerPuzzle{Top=new[]{2,3,3,1,2},Bottom=new[]{3,3,2,2,1},Left=new[]{2,1,2,3,4},Right=new[]{2,4,2,3,1}};
                    Poll(detector,Frame(1920,1027,shot,0,0),clues,"1920x1027 DarkScape",check);
                    Poll(detector,Frame(2560,1369,shot,200,180),clues,"2560x1369 DarkScape",check);
                }
                using(Bitmap box=TestFixtures.LockboxImage())
                using(Bitmap frame=Frame(2560,1369,box,900,400))
                {
                    detector.ResetSearch();
                    Pixels pixels=new Pixels(frame);
                    DetectedPuzzle found=null;int ticks=0;string times="";
                    for(;ticks<3 && (found==null||found.Kind!=PuzzleKind.Lockbox);ticks++)
                    {
                        Stopwatch watch=Stopwatch.StartNew();
                        found=detector.DetectBoards(frame,pixels,true,true);
                        times+=(times.Length==0?"":",")+watch.ElapsedMilliseconds;
                    }
                    check(found!=null&&found.Kind==PuzzleKind.Lockbox,"live polling still finds Lockbox on a 2560x1369 frame in "+ticks+" ticks ("+times+" ms)");
                }
                File.AppendAllText(report,"TOWER POLLING PASS: "+count+" assertions.\r\n");
                return 0;
            }
            catch(Exception ex)
            {
                File.AppendAllText(report,"FAILED after "+count+" checks: "+ex.Message+"\r\n");
                return 1;
            }
        }
        private static Bitmap Frame(int width,int height,Bitmap puzzle,int x,int y)
        {
            Bitmap frame=new Bitmap(width,height);
            using(Graphics g=Graphics.FromImage(frame))
            {
                g.Clear(Color.FromArgb(24,28,32));
                g.DrawImageUnscaled(puzzle,x,y);
            }
            return frame;
        }
        private static void Poll(PuzzleDetector detector,Bitmap frame,TowerPuzzle clues,string name,Action<bool,string> check)
        {
            using(frame)
            {
                detector.ResetSearch();
                Pixels pixels=new Pixels(frame);
                DetectedPuzzle found=null;int ticks=0;string times="";
                for(;ticks<2 && found==null;ticks++)
                {
                    Stopwatch watch=Stopwatch.StartNew();
                    found=detector.DetectBoards(frame,pixels,true,true);
                    times+=(times.Length==0?"":",")+watch.ElapsedMilliseconds;
                }
                string got=found==null||found.Towers==null?"none":String.Join(",",found.Towers.Top);
                check(found!=null&&found.Kind==PuzzleKind.Towers,"live polling detects Towers on "+name+" in "+ticks+" ticks ("+times+" ms) "+got);
                check(found!=null&&found.Towers!=null&&found.Towers.Top.SequenceEqual(clues.Top)&&found.Towers.Bottom.SequenceEqual(clues.Bottom)&&found.Towers.Left.SequenceEqual(clues.Left)&&found.Towers.Right.SequenceEqual(clues.Right),"live polling reads the "+name+" clues");
                check(found!=null&&found.TowerSolutions!=null&&found.TowerSolutions.Count>0,"live polling solves "+name);
            }
        }
    }
}
