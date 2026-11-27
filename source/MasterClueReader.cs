using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace KavrosClueHelper
{
    // The supported 944 clue paragraphs have fixed lettering. Their ink masks
    // ignore the parchment texture, frame, game background and scroll position.
    internal sealed class LegacyMasterClueReader
    {
        private sealed class Pattern
        {
            public MasterClue Clue;
            public float Scale;
            public Size Size;
            public Point[] Ink, Probes, Empty;
        }
        private readonly List<Pattern> patterns=new List<Pattern>();
        private int dpi=96;
        public void UseDpi(int value){dpi=value<96?96:value;}
        private IEnumerable<Pattern> Ordered()
        {
            if(dpi==96)return patterns;
            float target=dpi/96F;
            return patterns.OrderBy(p=>Math.Abs(p.Scale-target)).ThenBy(p=>p.Scale);
        }
        private static bool IsInk(Color p) { return p.R>40 && p.R<164 && p.G>25 && p.G<148 && p.B>15 && p.B<125 && p.R>p.G && p.R-p.G<55 && p.G>p.B && p.G-p.B<55; }
        private static bool IsPaper(Color p) { return p.R>155 && p.G>115 && p.B>65 && p.R-p.G>5 && p.R-p.G<90 && p.G-p.B>12 && p.G-p.B<100; }
        public LegacyMasterClueReader(string assets)
        {
            foreach(float scale in new[]{1F,0.8F,1.25F,1.5F,2F,0.6F,0.67F})
                foreach(MasterClue clue in MasterClues.All)
                    foreach(string file in new[]{Path.Combine(assets,"master-text",clue.Id+".png")}.Concat(Directory.GetFiles(Path.Combine(assets,"master-text"),clue.Id+"-*.png")))
                    using(Bitmap original=new Bitmap(file))
                    using(Bitmap mask=Pixels.Crop(original,new Rectangle(Point.Empty,original.Size),new Size((int)Math.Round(original.Width*scale),(int)Math.Round(original.Height*scale))))
                    {
                        Pixels pixels=new Pixels(mask); List<Point> ink=new List<Point>(), empty=new List<Point>();
                        for(int y=0;y<mask.Height;y++) for(int x=0;x<mask.Width;x++)
                        {
                            if(pixels.Get(x,y).A>=175) ink.Add(new Point(x,y));
                            else if(x%3==0 && y%3==0 && pixels.Get(x,y).A<5)
                            {
                                bool clear=true,near=false;
                                for(int dy=-3;dy<=3;dy++) for(int dx=-3;dx<=3;dx++)
                                    if(x+dx>=0 && y+dy>=0 && x+dx<mask.Width && y+dy<mask.Height && pixels.Get(x+dx,y+dy).A>50)
                                    { near=true; if(Math.Abs(dx)<=1 && Math.Abs(dy)<=1) clear=false; }
                                if(clear && near) empty.Add(new Point(x,y));
                            }
                        }
                        Point[] probes=Enumerable.Range(0,12).Select(i=>ink[i*(ink.Count-1)/11]).ToArray();
                        Point[] negatives=empty.Where((p,i)=>i%Math.Max(1,empty.Count/180)==0).Take(180).ToArray();
                        patterns.Add(new Pattern { Clue=clue,Scale=scale,Size=mask.Size,Ink=ink.ToArray(),Probes=probes,Empty=negatives });
                    }
        }
        public DetectedPuzzle Detect(Bitmap image,Pixels pixels,Rectangle? previous=null)
        {
            List<Rectangle> papers=previous.HasValue ? new List<Rectangle>{Rectangle.Intersect(previous.Value,new Rectangle(Point.Empty,image.Size))} : FindPapers(pixels).Where(r=>!ParagraphVision.SkipClueSheet(r,pixels.Width)).ToList();
            foreach(Rectangle paper in papers.OrderByDescending(r=>r.Width*r.Height).Take(8))
            {
                if(paper.Width<190 || paper.Height<90) continue;
                bool[] mask=new bool[paper.Width*paper.Height]; List<Point> points=new List<Point>();
                for(int y=0;y<paper.Height;y++) for(int x=0;x<paper.Width;x++)
                    if(IsInk(pixels.Get(paper.X+x,paper.Y+y))) { mask[y*paper.Width+x]=true; points.Add(new Point(x,y)); }
                bool[] near=(bool[])mask.Clone();
                foreach(Point point in points) for(int dy=-1;dy<=1;dy++) for(int dx=-1;dx<=1;dx++)
                    if(point.X+dx>=0 && point.Y+dy>=0 && point.X+dx<paper.Width && point.Y+dy<paper.Height) near[(point.Y+dy)*paper.Width+point.X+dx]=true;
                foreach(Pattern pattern in Ordered())
                {
                    if(pattern.Size.Width>paper.Width || pattern.Size.Height>paper.Height) continue;
                    Point anchor=pattern.Probes[0];
                    foreach(Point point in points)
                    {
                        int x=point.X-anchor.X,y=point.Y-anchor.Y;
                        if(x<0 || y<0 || x+pattern.Size.Width>paper.Width || y+pattern.Size.Height>paper.Height) continue;
                        int misses=0;
                        foreach(Point probe in pattern.Probes) { if(!near[(y+probe.Y)*paper.Width+x+probe.X] && ++misses>2) break; }
                        if(misses>2) continue;
                        int hits=0,neighbours=0;
                        foreach(Point pixel in pattern.Ink) { int index=(y+pixel.Y)*paper.Width+x+pixel.X; if(mask[index]) hits++; else if(near[index]) neighbours++; }
                        double positive=(hits+0.6*neighbours)/pattern.Ink.Length;
                        if(positive<0.80 || hits/(double)pattern.Ink.Length<0.55) continue;
                        int wrong=0;
                        foreach(Point pixel in pattern.Empty) if(mask[(y+pixel.Y)*paper.Width+x+pixel.X]) wrong++;
                        double negative=1-wrong/(double)Math.Max(1,pattern.Empty.Length);
                        if(negative<0.88) continue;
                        return new DetectedPuzzle { Kind=PuzzleKind.EmoteClue,Master=pattern.Clue,Bounds=paper,Grid=new Rectangle(paper.X+x,paper.Y+y,pattern.Size.Width,pattern.Size.Height),Scale=pattern.Scale,Confidence=(positive+negative)/2,Message="Master emote clue" };
                    }
                    SearchCancellation.Check();
                }
            }
            return null;
        }
        private static List<Rectangle> FindPapers(Pixels pixels)
        {
            List<Rectangle> regions=new List<Rectangle>();
            for(int y=8;y<pixels.Height-8;y+=12)
            {
                SearchCancellation.Check();
                int start=-1,last=-1,hits=0;
                for(int x=0;x<pixels.Width+40;x+=4)
                {
                    if(x<pixels.Width && IsPaper(pixels.Get(x,y))) { if(start<0) start=x; last=x; hits++; }
                    if(start>=0 && x-last>=36)
                    {
                        int width=last-start+4;
                        if(width>=200 && width<=1150 && hits*4>=width*0.6)
                        {
                            Rectangle run=Rectangle.FromLTRB(Math.Max(0,start-4),Math.Max(0,y-12),Math.Min(pixels.Width,last+8),Math.Min(pixels.Height,y+12));
                            int existing=regions.FindIndex(r=>y<=r.Bottom+12 && Rectangle.Intersect(new Rectangle(r.X,0,r.Width,1),new Rectangle(run.X,0,run.Width,1)).Width>=Math.Min(r.Width,run.Width)*0.85);
                            if(existing<0) regions.Add(run); else regions[existing]=Rectangle.Union(regions[existing],run);
                        }
                        start=-1;last=-1;hits=0;
                    }
                }
            }
            return regions.Where(r=>r.Height>=100 && r.Width>=200).ToList();
        }
    }
}
