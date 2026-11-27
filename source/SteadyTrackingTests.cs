using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
namespace KavrosClueHelper
{
    // Pure logic checks for the shared "keep the last good result" tracking.
    // They need no game window, so they run anywhere the program runs.
    public static class SteadyTrackingTests
    {
        public static int Run(string report)
        {
            var log=new StringBuilder();int count=0;
            Action<bool,string> check=(ok,msg)=>{count++;if(!ok)throw new Exception(msg);log.AppendLine("ok "+msg);};
            try
            {
                DateTime t=new DateTime(2026,1,1,12,0,0,DateTimeKind.Utc);
                var hold=new DetectionHold();
                check(hold.Miss(t),"one missed frame keeps the result");
                check(hold.Miss(t.AddMilliseconds(50))&&hold.Miss(t.AddMilliseconds(100))&&hold.Miss(t.AddMilliseconds(150)),"four quick misses are not enough time");
                check(!hold.Miss(t.AddMilliseconds(750)),"several misses over 0.7 s close it");
                hold.Reset();
                for(int i=0;i<3;i++)hold.Miss(t.AddMilliseconds(i*400));
                check(hold.Miss(t.AddMilliseconds(1150))==false,"slow misses close after four frames");
                hold.Reset();hold.Miss(t);hold.Miss(t.AddMilliseconds(400));hold.Seen();
                check(hold.Miss(t.AddMilliseconds(900))&&hold.Misses==1,"a good frame restarts the miss count");

                var change=new ChangeConfirmation();
                check(change.Accept(null,"A"),"first reading is shown at once");
                check(change.Accept("A","A"),"same reading is kept");
                check(!change.Accept("A","B"),"one different frame does not replace the board");
                check(change.Accept("A","B"),"the same new board twice in a row replaces it");
                check(!change.Accept("A","B")&&!change.Accept("A","C")&&change.Accept("A","C"),"alternating misreads never replace it");
                check(!change.Accept("A","B")&&change.Accept("A","A")&&!change.Accept("A","B"),"returning to the shown board clears the candidate");

                var a=new DetectedPuzzle{Kind=PuzzleKind.Lockbox,Board=new int[25],Bounds=new Rectangle(100,100,300,300),Grid=new Rectangle(120,120,250,250)};
                var b=new DetectedPuzzle{Kind=PuzzleKind.Lockbox,Board=new int[25],Bounds=new Rectangle(101,99,300,301),Grid=new Rectangle(121,120,250,250)};
                check(SteadyPolicy.SameState(a,b,2),"a 1 px jitter on the same board needs no redraw");
                b.Board[3]=1;check(!SteadyPolicy.SameState(a,b,2),"a clicked board is redrawn");
                b.Board[3]=0;b.Bounds=new Rectangle(140,100,300,300);check(!SteadyPolicy.SameState(a,b,2),"a moved board is redrawn");
                check(SteadyPolicy.HoldsThroughMisses(PuzzleKind.Lockbox)&&SteadyPolicy.HoldsThroughMisses(PuzzleKind.Towers)&&SteadyPolicy.HoldsThroughMisses(PuzzleKind.Knot)&&!SteadyPolicy.HoldsThroughMisses(PuzzleKind.Sliding),"lockbox, towers and the knot hold through a miss");
                int[] solved = new int[25];
                for (int i = 0; i < 25; i++) solved[i] = i;
                int[] moved = (int[])solved.Clone();
                moved[23] = 24; moved[24] = 23;
                check(SliderStep.OneLegalMove(solved, moved), "one adjacent tile is a legal slider step");
                check(!SliderStep.OneLegalMove(solved, solved), "an unchanged board is not a new step");
                int[] jump = (int[])solved.Clone();
                jump[20] = 24; jump[24] = 20;
                check(!SliderStep.OneLegalMove(solved, jump), "a two-tile jump is not one click");
                var shown = new DetectedPuzzle { Kind = PuzzleKind.Sliding, Board = solved, Bounds = new Rectangle(40, 50, 300, 300), Grid = new Rectangle(60, 80, 250, 250) };
                var next = new DetectedPuzzle { Kind = PuzzleKind.Sliding, Board = moved, Bounds = new Rectangle(41, 50, 300, 301), Grid = new Rectangle(61, 80, 250, 250) };
                check(SliderStep.SkipConfirm(shown, next), "the same place and one legal move skips the confirm wait");
                next.Bounds = new Rectangle(90, 50, 300, 300);
                check(!SliderStep.SkipConfirm(shown, next), "a slider that moved on screen still waits");
                next.Bounds = shown.Bounds; next.Grid = shown.Grid;
                int[] two = (int[])solved.Clone();
                int blankAt = 24;
                foreach (int step in new[] { 23, 22 }) { two[blankAt] = two[step]; two[step] = 24; blankAt = step; }
                next.Board = two;
                check(!SliderStep.OneLegalMove(solved, two), "a whole-row slide is more than one swap");
                check(SliderStep.SkipConfirm(shown, next, new[] { 23, 22 }), "a whole-row step of the known route skips the confirm wait");
                check(!SliderStep.SkipConfirm(shown, next), "a multi-tile change still waits when it is not the route");
                check(SliderStep.Reread(true, false, 2) == 0 && SliderStep.Reread(false, true, 2) == 0, "a matching or continued read is shown");
                check(SliderStep.Reread(false, false, 0) == 1 && SliderStep.Reread(false, false, 1) == 1 && SliderStep.Reread(false, false, 2) == 2, "a bad slider re-read is kept twice and then reset");

                var motion=new MotionEstimate();
                check(!motion.Estimate(t).HasValue,"no estimate before any position");
                motion.Observe(new PointF(100,100),t);motion.Observe(new PointF(101,100),t.AddMilliseconds(250));motion.Observe(new PointF(102,100),t.AddMilliseconds(500));
                PointF e=motion.Estimate(t.AddMilliseconds(750)).Value;
                check(e.X>102.4&&e.X<103.2&&Math.Abs(e.Y-100)<0.01,"estimate follows recent movement ("+e+")");
                PointF far=motion.Estimate(t.AddSeconds(30)).Value;
                check(far.X<102+MotionEstimate.MaxSpeed*1.3,"estimate stops after a short time ("+far+")");
                motion.Reset();motion.Observe(new PointF(0,0),t);motion.Observe(new PointF(50,0),t.AddMilliseconds(250));
                check(motion.Velocity.X<=MotionEstimate.MaxSpeed+0.01,"a teleport is not treated as running speed");

                var region=new ScanRegion{Id=1,Name="Test",BaseRange=20,Level=0,Spots=new List<PointF>{new PointF(140,100)},SpotLevels=new List<int>{0}};
                var scan=new ScanSession();scan.Observe(new ScanReading{Region=region});
                check(scan.TrackingStatus==ScanSession.FindingHint,"automatic tracking asks for no manual step");
                scan.ApplyMatch(new MinimapMatch{Position=new PointF(100,100)},t);
                check(!scan.Position.HasValue,"first acquisition still waits for a second frame");
                for(int i=1;i<=4;i++)scan.ApplyMatch(new MinimapMatch{Position=new PointF(100+i,100)},t.AddMilliseconds(250*i));
                check(scan.PositionTracked,"consistent frames follow the player");
                PointF last=scan.LastKnown.Value;
                scan.ApplyMatch(null,t.AddMilliseconds(1250));
                check(scan.PositionTracked&&scan.Position.Value.X>last.X,"a missed frame moves the marker along the heading");
                for(int i=2;i<=7;i++)scan.ApplyMatch(null,t.AddMilliseconds(1000+250*i));
                check(scan.PositionTracked&&scan.Position.HasValue,"seven misses are not yet lost");
                scan.ApplyMatch(null,t.AddMilliseconds(3200));
                check(!scan.PositionTracked&&!scan.Position.HasValue&&scan.LastKnown==last,"eight misses over two seconds count as lost; the last position stays");
                check(scan.TrackingStatus==ScanSession.LostHint,"lost status reacquires automatically");
                scan.ApplyMatch(new MinimapMatch{Position=new PointF(last.X+3,last.Y)},t.AddMilliseconds(3500));
                check(scan.PositionTracked&&scan.Position.HasValue,"a match near the last position picks up again at once");
                scan.ApplyMatch(new MinimapMatch{Position=new PointF(last.X+300,last.Y)},t.AddMilliseconds(3750));
                check(scan.PositionTracked&&CompassMath.Distance(scan.Position.Value,new PointF(last.X+3,last.Y))<10,"a far misread does not move the marker");
                scan.ApplyMatch(new MinimapMatch{Position=new PointF(last.X+304,last.Y)},t.AddMilliseconds(4000));
                check(scan.PositionTracked&&CompassMath.Distance(scan.Position.Value,new PointF(last.X+304,last.Y))<2,"a far fix seen twice in a row relocks as a teleport");
                scan.Observe(new ScanReading{Region=region});
                check(scan.PositionTracked,"re-reading the same scan clue keeps tracking");
                foreach(ScanRegion sample in new[]{
                    new ScanRegion{Id=356,Name="Brimhaven Dungeon",BaseRange=14,Level=0,Tier="elite",Route="Fastest: climb the dungeon.\nAlternatively: use the rope.",Spots=new List<PointF>{new PointF(2700,3200)},SpotLevels=new List<int>{0,2}},
                    new ScanRegion{Id=368,Name="Prifddinas",BaseRange=30,Level=0,Tier="master",Route="Fastest: enter Prifddinas.",Spots=new List<PointF>{new PointF(2200,3400)},SpotLevels=new List<int>{0}}})
                {
                    var outside=new ScanSession();
                    outside.Observe(new ScanReading{Region=sample,Outside=true});
                    check(!outside.Position.HasValue&&outside.TrackingStatus==ScanSession.FindingHint,"an out-of-range "+sample.Name+" scan still looks for the minimap");
                    string card=sample.Route+"\n"+outside.FloorLine();
                    check(card.StartsWith(sample.Route),"the step-by-step guide stays on an out-of-range "+sample.Name+" scan");
                    check(outside.FloorLine().Contains(ScanSession.OutsideNote)&&outside.FloorLine().Contains(ScanSession.FindingHint),"the "+sample.Name+" card shows the too-far note and the finding line");
                    outside.ApplyMatch(new MinimapMatch{Position=sample.Spots[0]},t.AddSeconds(20));
                    outside.ApplyMatch(new MinimapMatch{Position=new PointF(sample.Spots[0].X+1,sample.Spots[0].Y)},t.AddSeconds(20.25));
                    check(outside.PositionTracked&&outside.Position.HasValue,"an out-of-range "+sample.Name+" scan locks a live minimap match");
                    check(outside.FloorLine().Contains(ScanSession.OutsideNote)&&outside.FloorLine().Contains("Following your minimap automatically."),"a locked out-of-range "+sample.Name+" scan shows the position with the too-far note");
                    PointF locked=outside.Position.Value;
                    outside.Observe(new ScanReading{Region=sample,Outside=true});
                    check(outside.Outside&&outside.PositionTracked&&CompassMath.Distance(outside.Position.Value,locked)<1,"reading the too-far "+sample.Name+" scroll again does not drop the lock");
                    outside.Observe(new ScanReading{Region=sample,Outside=false});
                    check(!outside.Outside&&outside.PositionTracked&&CompassMath.Distance(outside.Position.Value,locked)<1,"coming into range on "+sample.Name+" keeps the same lock");
                    check(outside.FloorLine().IndexOf(ScanSession.OutsideNote,StringComparison.Ordinal)<0,"the too-far note goes away once "+sample.Name+" is in range");
                    check(sample.Levels.Length>=1&&sample.Level==0,"the scan floor is unchanged by tracking");
                    check(outside.AutoTracking&&outside.FloorLine().IndexOf("Right-click your position on the map",StringComparison.Ordinal)<0,"the right-click line stays hidden while "+sample.Name+" follows the minimap");
                    outside.SetAutomatic(false);
                    check(!outside.AutoTracking&&outside.FloorLine().Contains("Right-click your position on the map"),"the right-click line appears only when "+sample.Name+" auto-follow is off");
                }
                File.WriteAllText(report,log.ToString()+"Steady tracking checks passed: "+count+Environment.NewLine);
                return 0;
            }
            catch(Exception ex)
            {
                File.WriteAllText(report,log.ToString()+"FAILED after "+count+" checks: "+ex.Message+Environment.NewLine);
                return 1;
            }
        }
    }
}
