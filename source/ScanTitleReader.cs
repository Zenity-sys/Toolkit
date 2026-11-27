using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace KavrosClueHelper
{
    // Match the white letters separately from the tan parchment and its shadows.
    internal sealed class ScanTitleReader
    {
        private sealed class Glyphs
        {
            public int Width, Height, Count;
            public bool[] Ink;
        }
        private readonly Dictionary<int,Glyphs> titles = new Dictionary<int,Glyphs>();
        private readonly List<Glyphs> outside = new List<Glyphs>();
        private Glyphs different;
        public ScanTitleReader(string assets)
        {
            string directory = Path.Combine(assets, "scan-titles");
            if (!Directory.Exists(directory)) return;
            foreach (string file in Directory.GetFiles(directory, "*.png"))
            using (Bitmap image = new Bitmap(file))
            {
                var mask = Extract(new Pixels(image), new Rectangle(Point.Empty,image.Size));
                int id;
                string name=Path.GetFileNameWithoutExtension(file);
                if (Int32.TryParse(name,out id)) titles[id]=mask;
                else if (name=="different") different=mask;
                else if ((name=="too-far"||name=="nothing-scans")&&mask!=null) outside.Add(mask);
            }
        }
        private static bool White(Color c)
        { return Math.Min(c.R,Math.Min(c.G,c.B))>=192 && Math.Max(c.R,Math.Max(c.G,c.B))-Math.Min(c.R,Math.Min(c.G,c.B))<44; }
        private static Glyphs Extract(Pixels p,Rectangle box)
        {
            int x0=box.Right,y0=box.Bottom,x1=-1,y1=-1;
            for(int y=box.Top;y<box.Bottom;y++)for(int x=box.Left;x<box.Right;x++)if(White(p.Get(x,y)))
            { x0=Math.Min(x0,x); x1=Math.Max(x1,x); y0=Math.Min(y0,y); y1=Math.Max(y1,y); }
            if(x1<x0||y1<y0)return null;
            int w=x1-x0+1,h=y1-y0+1;
            var g=new Glyphs{Width=w,Height=h,Ink=new bool[w*h]};
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(g.Ink[y*w+x]=White(p.Get(x0+x,y0+y)))g.Count++;
            return g.Count<8?null:g;
        }
        private static double Score(Glyphs a,Glyphs b)
        {
            // Live title ink is about 89x8. A stored line with a descender is
            // about 11px, and log(87*8/(11*89)) is 0.34. 0.42 still rejects a
            // second text line (20px against 8px is 0.92).
            if(a==null||b==null||Math.Abs(Math.Log(a.Width*(double)b.Height/(a.Height*b.Width)))>.42)return 0;
            int both=0,total=0;
            for(int y=0;y<a.Height;y++)for(int x=0;x<a.Width;x++)
            {
                bool k=a.Ink[y*a.Width+x], v=b.Ink[(int)((y+.5)*b.Height/a.Height)*b.Width+(int)((x+.5)*b.Width/a.Width)];
                if(k)total++;if(v)total++;if(k&&v)both++;
            }
            return 2.0*both/Math.Max(1,total);
        }
        public ScanReading Read(Pixels pixels, Rectangle paper,List<ScanRegion> regions)
        {
            int left=paper.Left+paper.Width/10,right=paper.Right-paper.Width/10;
            int gap=Math.Max(5,paper.Height/30),first=-1,last=-1,body=-1,lines=0;
            for(int y=paper.Top+paper.Height/8;y<paper.Top+paper.Height*3/5;y++)
            {
                int count=0;for(int x=left;x<right;x++)if(White(pixels.Get(x,y)))count++;
                if(count<2)continue;
                if(first<0)first=y;
                if(body<0&&last>=0&&y-last>gap){body=y;lines=last;}
                last=y;
            }
            if(body<0||last-body<paper.Height/14)return null;
            Glyphs title=Extract(pixels,Rectangle.FromLTRB(left,first,right,lines+1));
            int winner=0;double best=0,runner=0;
            foreach(var entry in titles)
            {
                double score=Score(title,entry.Value);
                if(score>best){runner=best;best=score;winner=entry.Key;}else runner=Math.Max(runner,score);
            }
            if(best<.73||best-runner<.10)return null;
            ScanRegion region=regions.Find(r=>r.Id==winner);
            if(region==null)return null;
            Glyphs message=Extract(pixels,Rectangle.FromLTRB(left,body,right,body+Math.Max(5,(int)Math.Round(paper.Height*8.0/276))));
            return new ScanReading{Region=region,DifferentLevel=Score(message,different)>.73,Outside=Outside(pixels,paper)};
        }
        // Legacy scan text sits on the side parchment, under the place name.
        // The phrases are matched as white ink, the same way as the title.
        public bool Outside(Pixels pixels, Rectangle paper)
        {
            if(outside.Count==0||paper.Width<30||paper.Height<30)return false;
            foreach(Glyphs mask in outside)
            {
                if(mask==null||mask.Width>paper.Width||mask.Height>paper.Height)continue;
                for(int y=paper.Top;y+mask.Height<=paper.Bottom;y++)
                for(int x=paper.Left;x+mask.Width<=paper.Right;x++)
                    if(Place(pixels,mask,x,y)>=0.72)return true;
            }
            return false;
        }
        private static double Place(Pixels pixels, Glyphs mask, int x, int y)
        {
            int both=0,white=0;
            for(int yy=0;yy<mask.Height;yy++)for(int xx=0;xx<mask.Width;xx++)
            {
                bool ink=mask.Ink[yy*mask.Width+xx], on=White(pixels.Get(x+xx,y+yy));
                if(on)white++; if(ink&&on)both++;
            }
            return 2.0*both/Math.Max(1,mask.Count+white);
        }
    }
}
