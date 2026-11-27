using System;
using System.Drawing;
using System.Linq;

namespace KavrosClueHelper
{
    public sealed class CaptureSearch
    {
        public const int MissesBeforeSearch=4;
        private bool wide=true;
        private int misses;
        private Size size;
        public static Rectangle VisibleClient(Rectangle client, Rectangle desktop)
        {
            Rectangle visible = Rectangle.Intersect(client, desktop);
            if (visible.Width <= 0 || visible.Height <= 0) return Rectangle.Empty;
            visible.Offset(-client.X, -client.Y);
            return visible;
        }
        public void Reset() { wide=true; misses=0; size=Size.Empty; }
        public void ForceFullWindow() { wide=true; misses=0; }
        public Rectangle Choose(Size client, Rectangle? located)
        {
            Rectangle full=new Rectangle(Point.Empty,client);
            bool resized=size.Width>0&&size!=client; size=client;
            if(resized){wide=true;misses=0;}
            if(wide || !located.HasValue) { wide=false; return full; }
            Rectangle roi=Rectangle.Intersect(Rectangle.Inflate(located.Value,80,80),full);
            return roi.Width>=100 && roi.Height>=100 ? roi : full;
        }
        // A cropped miss keeps the known puzzle region. Only several confident
        // misses, with the puzzle actually gone, go back to the full window.
        public bool Complete(bool cropped,bool found)
        {
            if(!cropped||found){if(found)misses=0;return false;}
            if(++misses<MissesBeforeSearch)return false;
            misses=0;wide=true;return true;
        }
    }
    public static class GameVisibility
    {
        public static Point[] Samples(Rectangle area)
        {
            Point[] points=new Point[9];
            for(int y=0;y<3;y++) for(int x=0;x<3;x++) points[y*3+x]=new Point(area.Left+(2*x+1)*area.Width/6,area.Top+(2*y+1)*area.Height/6);
            return points;
        }
        public static bool Any(Rectangle area,Func<Point,bool> belongsToGame)
        { foreach(Point point in Samples(area)) if(belongsToGame(point)) return true; return false; }
        public static bool Clear(Rectangle area,Func<Point,bool> belongsToGame)
        {
            Point[] points=Samples(area); if(!belongsToGame(points[4])) return false;
            int visible=0; foreach(Point point in points) if(belongsToGame(point)) visible++;
            return visible>=7;
        }
    }
    public static class DetectionPolicy
    {
        // Only confirmed puzzle data may become a cached/tracked capture region.
        public static bool CanTrack(PuzzlePlan plan)
        {
            if(plan==null || plan.Puzzle==null) return false;
            DetectedPuzzle p=plan.Puzzle;
            if(p.Bounds.Width<=0 || p.Bounds.Height<=0) return false;
            if(p.Kind==PuzzleKind.Towers)
                return p.Towers!=null && new[]{p.Towers.Top,p.Towers.Bottom,p.Towers.Left,p.Towers.Right}.All(side=>side!=null && side.Length==5 && side.All(v=>v>=1 && v<=5)) && plan.Towers!=null && plan.Towers.Count>0;
            if(p.Kind==PuzzleKind.Lockbox) return p.Board!=null && p.Board.Length==25 && p.Board.All(v=>v>=0 && v<=2) && plan.Lockbox!=null;
            if(p.Kind==PuzzleKind.Sliding) return SlidingSolver.IsSolvable(p.Board) && plan.Sliding!=null;
            if(p.Kind==PuzzleKind.EmoteClue) return p.Master!=null;
            if(p.Kind==PuzzleKind.Compass) return p.Compass!=null;
            if(p.Kind==PuzzleKind.Scan)return p.Scan!=null&&p.Scan.Region!=null;
            // A framed, traced knot may need a ring rotation to reveal hidden runes.
            return p.Kind==PuzzleKind.Knot && p.Knot!=null;
        }
        public static bool IsBoardPuzzle(PuzzleKind kind)
        {
            return kind==PuzzleKind.Sliding||kind==PuzzleKind.Lockbox||kind==PuzzleKind.Towers||kind==PuzzleKind.Knot;
        }
        public static bool NeedsConfirmation(PuzzlePlan plan)
        {
            if(plan==null || plan.Puzzle==null) return true;
            DetectedPuzzle p=plan.Puzzle;
            if(p.Kind==PuzzleKind.Towers) return plan.Towers==null || plan.Towers.Count==0;
            if(p.Kind==PuzzleKind.Lockbox) return plan.Lockbox==null || p.Confidence<0.90;
            if(p.Kind==PuzzleKind.Sliding) return plan.Sliding==null || p.Confidence<0.90;
            if(p.Kind==PuzzleKind.EmoteClue) return p.Confidence<0.92;
            if(p.Kind==PuzzleKind.Compass) return false; // Stable-angle confirmation happens before recording a bearing.
            if(p.Kind==PuzzleKind.Scan)return false;
            // A clean knot reading is shown immediately, even when one more
            // turn is needed before the clicks are known. A blurred frame waits.
            if(p.Kind==PuzzleKind.Knot) return plan.KnotMoving;
            return true;
        }
    }
}
