using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
namespace KavrosClueHelper
{
    public static class DarkscapeTowersTests
    {
        private static readonly int[] Top=new[]{2,3,3,1,2};
        private static readonly int[] Bottom=new[]{3,3,2,2,1};
        private static readonly int[] Left=new[]{2,1,2,3,4};
        private static readonly int[] Right=new[]{2,4,2,3,1};
        public static int Run(string report)
        {
            int count=0;
            try
            {
                Action<bool,string> check=(ok,msg)=>{count++;if(!ok)throw new Exception(msg);};
                check(Pixels.TowerCell(Color.FromArgb(26,15,9)),"DarkScape tower cell RGB(26, 15, 9) is a cell");
                check(Pixels.TowerCell(Color.FromArgb(18,12,6))&&Pixels.TowerCell(Color.FromArgb(40,24,12)),"DarkScape cell colour keeps a little headroom");
                check(Pixels.TowerCell(Color.FromArgb(32,48,64)),"modern slate cells still count");
                check(Pixels.TowerCell(Color.FromArgb(70,52,30)),"legacy brown cells still count");
                check(!Pixels.TowerCell(Color.FromArgb(60,30,10))&&!Pixels.TowerCell(Color.FromArgb(26,26,9))&&!Pixels.TowerCell(Color.FromArgb(8,5,3))&&!Pixels.TowerCell(Color.FromArgb(200,180,40)),"colours outside the DarkScape cell range are not tower cells");
                string assets=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets");
                string folder=Path.Combine(assets,"test_assets","darkscape");
                var detector=new PuzzleDetector(assets);
                foreach(string name in new[]{"darkscape-towers-1.png","darkscape-towers-2.png"})
                {
                    using(Bitmap shot=new Bitmap(Path.Combine(folder,name)))
                    {
                        CheckShot(detector,shot,name,check);
                        using(Bitmap client=new Bitmap(1920,1027))
                        {
                            using(Graphics g=Graphics.FromImage(client))
                            {
                                g.InterpolationMode=InterpolationMode.NearestNeighbor;
                                g.PixelOffsetMode=PixelOffsetMode.Half;
                                g.DrawImage(shot,new Rectangle(0,0,1920,1027));
                            }
                            CheckShot(detector,client,name+" at 1920x1027",check);
                        }
                    }
                }
                File.AppendAllText(report,"DARKSCAPE TOWERS PASS: "+count+" assertions.\r\n");
                return 0;
            }
            catch(Exception ex)
            {
                File.AppendAllText(report,"FAILED after "+count+" checks: "+ex.Message+"\r\n");
                return 1;
            }
        }
        private static void CheckShot(PuzzleDetector detector,Bitmap shot,string name,Action<bool,string> check)
        {
            DetectedPuzzle found=detector.Detect(shot,true,false);
            TowerPuzzle puzzle=found==null?null:found.Towers;
            string got=puzzle==null||puzzle.Top==null?"none":String.Join(",",puzzle.Top)+" / "+String.Join(",",puzzle.Bottom)+" / "+String.Join(",",puzzle.Left)+" / "+String.Join(",",puzzle.Right);
            check(found!=null&&found.Kind==PuzzleKind.Towers&&puzzle!=null,"DarkScape towers detected on "+name+" ("+got+")");
            check(puzzle!=null&&puzzle.Top.SequenceEqual(Top)&&puzzle.Bottom.SequenceEqual(Bottom)&&puzzle.Left.SequenceEqual(Left)&&puzzle.Right.SequenceEqual(Right),"DarkScape clues on "+name+": "+got);
            check(puzzle!=null&&puzzle.Cells!=null&&puzzle.Cells.Length==25&&puzzle.Cells.All(v=>v==0),"empty DarkScape cells stay blank on "+name+" ("+(puzzle==null||puzzle.Cells==null?"none":String.Join(",",puzzle.Cells))+")");
            check(found!=null&&found.TowerSolutions!=null&&found.TowerSolutions.Count>0,"DarkScape towers solve on "+name);
            using(Bitmap covered=new Bitmap(shot))
            {
                using(Graphics g=Graphics.FromImage(covered))
                {
                    g.FillRectangle(Brushes.Black,found.Grid.Left-48,found.Grid.Top-48,found.Grid.Width+96,40);
                    g.FillRectangle(Brushes.Black,found.Grid.Left-48,found.Grid.Bottom+4,found.Grid.Width+96,40);
                    g.FillRectangle(Brushes.Black,found.Grid.Left-48,found.Grid.Top,40,found.Grid.Height);
                    g.FillRectangle(Brushes.Black,found.Grid.Right+4,found.Grid.Top,40,found.Grid.Height);
                }
                DetectedPuzzle held=detector.ReadAgain(covered,found);
                check(held!=null&&held.Towers!=null&&held.Towers.Key==found.Towers.Key&&detector.LastTowerCluesRead!=20,"DarkScape towers stay held when a menu covers the clues on "+name);
            }
        }
    }
}
