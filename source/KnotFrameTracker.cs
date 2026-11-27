using System;
using System.Linq;
namespace KavrosClueHelper
{
    public sealed class KnotFrameTracker
    {
        public KnotPuzzle History {get;private set;}
        private string rawKey;
        private int frames;
        private DateTime seen;
        // Rune ids only. A flickering match marker is not a ring turning.
        public static string RuneKey(KnotPuzzle knot)
        {
            if(knot==null||knot.Lanes==null)return "";
            return String.Join("/",knot.Lanes.Select(lane=>lane==null?"?":String.Join(",",lane.Select(rune=>rune==null?"?":rune.Id.ToString()))));
        }
        public bool Observe(DetectedPuzzle puzzle,DateTime now,bool live)
        {
            string key=RuneKey(puzzle.Knot);
            bool same=rawKey==key&&(now-seen).TotalMilliseconds<1500;
            frames=same?frames+1:1;
            rawKey=key;seen=now;
            puzzle.Knot=KnotImageReader.Merge(puzzle.Knot,History);
            bool reliable=puzzle.Knot!=null&&puzzle.Knot.Reliable;
            // A clean reading is published on this frame, including the frame a
            // ring has just finished turning. Only a blurred mid-spin frame,
            // which is not reliable, waits for the same runes to come back.
            bool moving=live&&!reliable&&frames<2;
            if(!moving||History==null)History=puzzle.Knot;
            return moving;
        }
        public void Missing(DateTime now)
        {rawKey=null;frames=0;if((now-seen).TotalMilliseconds>1200)History=null;}
        public void Reset(){History=null;rawKey=null;frames=0;seen=DateTime.MinValue;}
    }
}
