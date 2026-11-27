using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
namespace KavrosClueHelper
{
    public static class ScrollAreas
    {
        private static bool ModernBorder(Color c){return c.R<65&&c.G<100&&c.B<110&&c.G>=c.R&&c.B>=c.R;}
        // Legacy interface mode draws the same window in brown stone instead of slate blue.
        private static bool LegacyBorder(Color c){return c.R>=55&&c.R<=160&&c.G>=35&&c.G<=130&&c.B>=15&&c.B<=90&&c.R>=c.G-8&&c.G>=c.B&&(c.R-c.B)>=20&&(c.G-c.B)>=8;}
        private static bool ScanBorder(Pixels p,Rectangle r,bool? legacy)
        {
            int dark=0;foreach(double f in new[]{0.25,0.5,0.75})foreach(int side in new[]{-1,1})
            {
                int x=side<0?r.Left-12:r.Right+12,y=r.Top+(int)(r.Height*f);Color c=p.Get(x,y);
                bool match=legacy==true?LegacyBorder(c):legacy==false?ModernBorder(c):ModernBorder(c)||LegacyBorder(c);
                if(match)dark++;
            }
            return dark>=3;
        }
        // Locate tall parchment before searching glyphs. This avoids treating
        // bright scenery as text and avoids full-screen glyph searches.
        public static List<Rectangle> Find(Pixels p,bool tightScan=false,bool? legacyBorder=null)
        {
            List<Rectangle> regions=Collect(p);
            if(tightScan)return regions.Where(r=>r.Width>=85&&r.Width<=450&&r.Height>=r.Width*1.1&&r.Height<r.Width*3.2&&ScanBorder(p,r,legacyBorder)).OrderByDescending(r=>r.Width*r.Height).Take(3).Select(r=>Rectangle.Intersect(Rectangle.Inflate(r,20,10),new Rectangle(0,0,p.Width,p.Height))).ToList();
            return regions.Where(r=>r.Height>=100&&r.Height>=r.Width*1.05&&r.Height<r.Width*16).OrderByDescending(r=>r.Width*r.Height).Take(32).Select(r=>Rectangle.Intersect(Rectangle.Inflate(r,Math.Max(24,r.Height/2),40),new Rectangle(Point.Empty,new Size(p.Width,p.Height)))).ToList();
        }
        // A scan in the minigames panel is darker than a clue scroll and is
        // split by a shadow, so the strict beige run never reaches 140 px.
        // These boxes stay under the wide-scroll title search.
        public static List<Rectangle> NarrowPanels(Pixels p)
        {
            if(p==null)return new List<Rectangle>();
            return Collect(p).Where(r=>r.Width>=90&&r.Width<340&&r.Height>=180&&r.Height<=820&&r.Height>=r.Width*1.05&&r.Height<r.Width*4).OrderByDescending(r=>r.Width*r.Height).Take(3).Select(r=>Rectangle.Intersect(Rectangle.Inflate(r,12,16),new Rectangle(0,0,p.Width,p.Height))).ToList();
        }
        static List<Rectangle> Collect(Pixels p)
        {
            var regions=new List<Rectangle>();
            if(p==null)return regions;
            for(int y=4;y<p.Height-4;y+=8)
            {
                SearchCancellation.Check();int first=-1,last=-1,hits=0;
                for(int x=0;x<p.Width+20;x+=4)
                {
                    Color c=p.Get(x,y);
                    if(x<p.Width&&c.R>90&&c.G>70&&c.B>35&&c.R-c.G>-8&&c.R-c.G<80&&c.G-c.B>12)
                    {if(first<0)first=x;last=x;hits++;}
                    if(first>=0&&x-last>=16)
                    {
                        int width=last-first+4;
                        if(width>=24&&width<=650&&hits*4>=width*0.35)
                        {
                            Rectangle run=Rectangle.FromLTRB(first,Math.Max(0,y-8),Math.Min(p.Width,last+4),Math.Min(p.Height,y+8));
                            int at=regions.FindIndex(r=>y<=r.Bottom+Math.Max(64,r.Width/4)&&Math.Min(r.Width,run.Width)>=Math.Max(r.Width,run.Width)*0.4&&Rectangle.Union(r,run).Width<=650&&Rectangle.Intersect(new Rectangle(r.X,0,r.Width,1),new Rectangle(run.X,0,run.Width,1)).Width>=Math.Min(r.Width,run.Width)*0.7);
                            if(at<0)regions.Add(run);else regions[at]=Rectangle.Union(regions[at],run);
                        }
                        first=last=-1;hits=0;
                    }
                }
            }
            return regions;
        }
    }
}

