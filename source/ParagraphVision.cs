using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace KavrosClueHelper
{
    // Scale-independent line signatures. A match requires the whole paragraph,
    // including equipment lines, rather than accepting parchment or a title.
    public static class ParagraphVision
    {
        public sealed class Line
        {
            public Rectangle Bounds;
            public double[] Signature;
        }
        public static bool Ink(Color p)
        { return p.R>40&&p.R<164&&p.G>25&&p.G<148&&p.B>15&&p.B<125&&p.R>p.G&&p.R-p.G<55&&p.G>p.B&&p.G-p.B<55; }
        static bool Beige(Color c)
        { return c.R>155&&c.G>115&&c.B>65&&c.R-c.G>5&&c.R-c.G<90&&c.G-c.B>12&&c.G-c.B<100; }
        static bool Dark(Color c,int limit)
        { return (c.R+c.G+c.B)/3<limit; }
        static bool DarkAt(Pixels p,int x,int y)
        {
            if(x<0||y<0||x>=p.Width||y>=p.Height)return false;
            return Dark(p.Get(x,y),70);
        }
        public static List<Rectangle> FindPapers(Pixels p)
        {
            return CollectPapers(p,180,90,8);
        }
        // A compass or scan in a side panel is darker than clue parchment and
        // can be split by a shadow. Scroll areas still see that sheet. Wide
        // titled scrolls stay on FindScrolls, which asks for 340 px.
        public static List<Rectangle> NarrowSheets(Pixels p)
        {
            return ScrollAreas.NarrowPanels(p);
        }
        public static bool TallSheet(Pixels p)
        {
            if(p==null)return false;
            List<Rectangle> scrolls=FindScrolls(p);
            if(scrolls!=null && scrolls.Count>0)return true;
            return NarrowSheets(p).Count>0;
        }
        static List<Rectangle> CollectPapers(Pixels p,int minWidth,int minHeight,int take)
        {
            var regions=new List<Rectangle>();
            if(p==null)return regions;
            int columns=p.Width/4+2;
            bool[] prevDark=new bool[columns],rowDark=new bool[columns];
            for(int y=8;y<p.Height-8;y+=12)
            {
                SearchCancellation.Check();
                Array.Clear(rowDark,0,rowDark.Length);
                int start=-1,last=-1,hits=0,gapDark=-1;
                for(int x=0;x<p.Width+40;x+=4)
                {
                    Color c=p.Get(x,y);
                    bool paper=x<p.Width&&Beige(c);
                    bool dark=x<p.Width&&Dark(c,70);
                    int col=x/4;
                    if(dark&&col>=0&&col<rowDark.Length)rowDark[col]=true;
                    // A 7-10px frame is darker than the parchment and lines up
                    // on the row above. Sand on either side is the same beige
                    // as the sheet, so the old 36px gap never split them.
                    bool column=dark&&col>=0&&col<prevDark.Length&&prevDark[col];
                    if(paper)
                    {
                        if(start>=0&&gapDark>=0&&x-last>=4&&x-last<=20)
                        {
                            KeepRun(regions,p,y,start,last,hits,minWidth);
                            start=x;last=x;hits=1;gapDark=-1;
                        }
                        else { if(start<0)start=x;last=x;hits++;gapDark=-1; }
                    }
                    else
                    {
                        if(column)gapDark=x;
                        if(start>=0&&x-last>=36)
                        {
                            KeepRun(regions,p,y,start,last,hits,minWidth);
                            start=-1;last=-1;hits=0;gapDark=-1;
                        }
                    }
                }
                bool[] swap=prevDark;prevDark=rowDark;rowDark=swap;
            }
            return regions.Where(r=>r.Height>=minHeight&&r.Width>=minWidth).OrderByDescending(r=>r.Width*r.Height).Take(take).ToList();
        }
        static void KeepRun(List<Rectangle> regions,Pixels p,int y,int start,int last,int hits,int minWidth)
        {
            int width=last-start+4;
            if(width>1400){SplitWide(regions,p,y,start,last);return;}
            if(width>=minWidth&&hits*4>=width*0.6)AddRun(regions,p,y,start,last);
        }
        // A beach-wide beige strip can still hide a framed scroll. Pull the
        // sheet out between its two dark sides instead of dropping the row.
        static void SplitWide(List<Rectangle> regions,Pixels p,int y,int start,int last)
        {
            var borders=new List<int>();
            for(int x=start;x<=last;x+=4)
                if(DarkAt(p,x,y)&&DarkAt(p,x,y-12))
                    if(borders.Count==0||x-borders[borders.Count-1]>16)borders.Add(x);
            for(int i=0;i<borders.Count;i++)for(int j=i+1;j<borders.Count;j++)
            {
                int inner=borders[i]+4,outer=borders[j]-4,width=outer-inner+4;
                if(width<180||width>1400)continue;
                int hits=0,samples=0;
                for(int x=inner;x<=outer;x+=4){samples++;if(Beige(p.Get(x,y)))hits++;}
                if(samples>0&&hits*4>=width*0.6){AddRun(regions,p,y,inner,outer);return;}
            }
        }
        static void AddRun(List<Rectangle> regions,Pixels p,int y,int start,int last)
        {
            Rectangle run=Rectangle.FromLTRB(Math.Max(0,start-4),Math.Max(0,y-12),Math.Min(p.Width,last+8),Math.Min(p.Height,y+12));
            int existing=regions.FindIndex(r=>y<=r.Bottom+12&&Rectangle.Intersect(new Rectangle(r.X,0,r.Width,1),new Rectangle(run.X,0,run.Width,1)).Width>=Math.Min(r.Width,run.Width)*0.85);
            if(existing<0)regions.Add(run);else regions[existing]=Rectangle.Union(regions[existing],run);
        }
        // The mysterious-scroll title is a solid dark bar with parchment
        // directly under it and a thin dark border down both sides. Sand and
        // sandstone pass the beige test, so this is what finds the sheet.
        public static List<Rectangle> FindScrolls(Pixels p)
        {
            var found=new List<Rectangle>();
            if(p==null||p.Width<200||p.Height<160)return found;
            for(int y=16;y<p.Height-80&&found.Count==0;y+=12)
            {
                SearchCancellation.Check();
                int start=-1,last=-1,hits=0;
                for(int x=0;x<=p.Width;x+=8)
                {
                    bool ink=false;
                    if(x<p.Width)ink=Dark(p.Get(x,y),52);
                    if(ink){if(start<0)start=x;last=x;hits++;}
                    if(start>=0&&(x>=p.Width||(!ink&&x-last>=16)))
                    {
                        int width=last-start;
                        // A 2560 client draws a treasure map about 1300 wide, and a
                        // scaled UI is wider still. The dark title and frame checks
                        // below are what reject sand.
                        if(width>=340&&width<=1800&&hits*8>=width*0.7)ConsiderScroll(p,y,start,last,found);
                        start=-1;last=-1;hits=0;
                    }
                }
            }
            for(int i=0;i<found.Count;i++)for(int j=i+1;j<found.Count;j++)
            {
                Rectangle a=found[i],b=found[j];
                int overlap=Rectangle.Intersect(new Rectangle(a.X,0,a.Width,1),new Rectangle(b.X,0,b.Width,1)).Width;
                if(overlap>=Math.Min(a.Width,b.Width)*0.75&&Math.Abs(a.Top-b.Top)<80){found[i]=Rectangle.Union(a,b);found.RemoveAt(j--);}
            }
            return found;
        }
        // A DarkScape clue is a beige sheet in a brown frame, with the gold
        // close button on the top-right corner. The roof is as dark as the
        // frame, so this does not ask for an isolated modern title bar.
        public static List<Rectangle> FindDarkscapeScrolls(Pixels p)
        {
            var found=new List<Rectangle>();
            if(p==null)return found;
            foreach(Rectangle paper in FindPapers(p))
            {
                if(paper.Width<340||paper.Width>1400||paper.Height<180||paper.Height>900)continue;
                if(!DarkscapeFrame(p,paper))continue;
                found.Add(paper);
                if(found.Count==2)break;
            }
            return found;
        }
        static bool DarkscapeFrame(Pixels p,Rectangle r)
        {
            int topDark=0,topN=0,y=Math.Max(0,r.Top-6);
            for(int x=r.Left+12;x<r.Right-12;x+=8)
            {
                topN++;
                if(y<p.Height&&Dark(p.Get(x,y),70))topDark++;
            }
            if(topN==0||topDark*1.0/topN<0.40)return false;
            int sides=0,sideN=0;
            for(int yy=r.Top+24;yy<r.Bottom-16;yy+=28)
            {
                sideN++;
                if(DarkscapeSide(p,r.Left,yy,true)&&DarkscapeSide(p,r.Right,yy,false))sides++;
            }
            if(sideN==0||sides*1.0/sideN<0.5)return false;
            int gold=0;
            int gx0=Math.Max(0,r.Right-36),gx1=Math.Min(p.Width-1,r.Right+28);
            int gy0=Math.Max(0,r.Top-48),gy1=Math.Min(p.Height-1,r.Top+8);
            for(int yy=gy0;yy<=gy1;yy+=2)for(int x=gx0;x<=gx1;x+=2)
            {
                Color c=p.Get(x,yy);
                if(c.R>170&&c.G>70&&c.B<140&&c.R>c.B+50)gold++;
            }
            return gold>=3;
        }
        static bool DarkscapeSide(Pixels p,int edge,int y,bool left)
        {
            if(y<0||y>=p.Height)return false;
            int x0=left?Math.Max(0,edge-16):Math.Max(0,edge-4);
            int x1=left?Math.Min(p.Width-1,edge+4):Math.Min(p.Width-1,edge+16);
            int dark=0;
            for(int x=x0;x<=x1;x+=2)if(Dark(p.Get(x,y),80))dark++;
            return dark>=2;
        }
        static double PaperBand(Pixels p,int x0,int x1,int y)
        {
            if(y<0||y>=p.Height||x1<=x0)return 0;
            int good=0,n=0,left=x0+(x1-x0)/6,right=x1-(x1-x0)/6;
            for(int x=left;x<right;x+=8){n++;if(Beige(p.Get(x,y)))good++;}
            return n==0?0:good/(double)n;
        }
        static bool EdgeDark(Pixels p,int edge,int y,bool left,int reach)
        {
            if(y<0||y>=p.Height)return false;
            // The title ink can sit inside the border. Look outward as far as
            // that border, and only a little back into the bar.
            int x0=left?Math.Max(0,edge-reach):Math.Max(0,edge-24);
            int x1=left?Math.Min(p.Width-1,edge+24):Math.Min(p.Width-1,edge+reach);
            int dark=0;
            for(int x=x0;x<=x1;x+=2)if(Dark(p.Get(x,y),80))dark++;
            return dark>=2;
        }
        static bool DarkBand(Pixels p,int x0,int x1,int y)
        {
            if(y<0||y>=p.Height)return false;
            int dark=0,n=0,left=x0+(x1-x0)/8,right=x1-(x1-x0)/8;
            for(int x=left;x<right;x+=6){n++;if(Dark(p.Get(x,y),55))dark++;}
            return n>0&&dark*1.0/n>=0.55;
        }
        static void ConsiderScroll(Pixels p,int y,int x0,int x1,List<Rectangle> found)
        {
            // The title row itself has to be a solid dark bar. A sandy row
            // that is only just darker than the parchment is not a scroll.
            if(!DarkBand(p,x0,x1,y))return;
            double above=PaperBand(p,x0,x1,y-28),below=PaperBand(p,x0,x1,y+32);
            if(above>=0.30)return;
            if(below<0.72)return;
            // The world behind a map can be as dark as the title, so the
            // side borders and the bottom band are what separate a scroll
            // from sand. A sandy row never passes the solid dark-bar test.
            int reach=Math.Max(48,(x1-x0)/3),sideRows=0,sideHit=0;
            for(int dy=40;dy<=160;dy+=60)
            {
                int yy=y+dy;if(yy>=p.Height)break;
                sideRows++;
                if(EdgeDark(p,x0,yy,true,reach)&&EdgeDark(p,x1,yy,false,reach))sideHit++;
            }
            if(sideRows<2||sideHit<sideRows)return;
            int bottom=-1;
            for(int yy=y+16;yy<Math.Min(p.Height,y+1300);yy+=8)
            {
                bool sides=EdgeDark(p,x0,yy,true,reach)&&EdgeDark(p,x1,yy,false,reach);
                if(PaperBand(p,x0,x1,yy)>=0.55&&sides)continue;
                if(DarkBand(p,x0,x1,yy)&&yy-y>=150){bottom=yy;break;}
                if(!sides&&yy-y>80)
                {
                    for(int look=yy-16;look<=yy+16;look+=2)if(DarkBand(p,x0,x1,look)&&look-y>=150){bottom=look;break;}
                    break;
                }
            }
            if(bottom<0||bottom-y<150||bottom-y>1200)return;
            found.Add(Rectangle.FromLTRB(Math.Max(0,x0-6),Math.Max(0,y-6),Math.Min(p.Width,x1+10),Math.Min(p.Height,bottom+10)));
        }
        public static List<Line> Template(Bitmap image)
        {
            Pixels p=new Pixels(image);bool[] mask=new bool[p.Width*p.Height];
            for(int y=0;y<p.Height;y++)for(int x=0;x<p.Width;x++)mask[y*p.Width+x]=p.Get(x,y).A>=100;
            return Lines(mask,p.Width,p.Height,false);
        }
        public static List<Line> Read(Pixels pixels,Rectangle paper)
        {
            int w=paper.Width,h=paper.Height,margin=Math.Max(5,w/45);bool[] raw=new bool[w*h],mask=new bool[w*h];
            for(int y=2;y<h-2;y++)for(int x=margin;x<w-margin;x++)raw[y*w+x]=Ink(pixels.Get(paper.X+x,paper.Y+y));
            bool[] visited=new bool[raw.Length];var queue=new Queue<int>();var component=new List<int>();
            for(int index=0;index<raw.Length;index++)
            {
                if(!raw[index]||visited[index])continue;
                queue.Enqueue(index);visited[index]=true;component.Clear();int left=w,top=h,right=0,bottom=0;
                while(queue.Count>0)
                {
                    int v=queue.Dequeue(),x=v%w,y=v/w;component.Add(v);
                    left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        int nx=x+dx,ny=y+dy;if(nx<0||ny<0||nx>=w||ny>=h)continue;int n=ny*w+nx;
                        if(raw[n]&&!visited[n]){visited[n]=true;queue.Enqueue(n);}
                    }
                }
                if(component.Count>=2&&right-left<=Math.Max(45,w/6)&&bottom-top<=Math.Max(40,h/8)&&component.Count<=Math.Max(650,w*h/120))
                    foreach(int v in component)mask[v]=true;
            }
            return Lines(mask,w,h,true);
        }
        private static List<Line> Lines(bool[] mask,int w,int h,bool filter)
        {
            var spans=new List<Rectangle>();int first=-1,last=-1;
            for(int y=0;y<=h+3;y++)
            {
                int count=0;if(y<h)for(int x=0;x<w;x++)if(mask[y*w+x])count++;
                if(count>=Math.Max(3,filter?w/110:3)){if(first<0)first=y;last=y;}
                if(first>=0&&y-last>3)
                {
                    int top=Math.Max(0,first-2),bottom=Math.Min(h,last+3),left=w,right=-1,inkTop=h,inkBottom=-1;
                    for(int yy=top;yy<bottom;yy++)for(int x=0;x<w;x++)if(mask[yy*w+x]){left=Math.Min(left,x);right=Math.Max(right,x);inkTop=Math.Min(inkTop,yy);inkBottom=Math.Max(inkBottom,yy);}
                    Rectangle r=Rectangle.FromLTRB(left,inkTop,right+1,inkBottom+1);
                    if(right>=left&&r.Width>=25&&r.Height>=4&&r.Height<=48&&(!filter||Math.Abs((left+right)/2.0-w/2.0)<w*0.18))spans.Add(r);
                    first=last=-1;
                }
            }
            // Enlarged descenders can fall below the row-count threshold and
            // appear as a second short band. Rejoin touching ink bands before
            // checking paragraph line spacing.
            for(int i=1;i<spans.Count;i++)
                if(spans[i].Top<=spans[i-1].Bottom+Math.Max(2,spans[i-1].Height/8))
                {spans[i-1]=Rectangle.Union(spans[i-1],spans[i]);spans.RemoveAt(i);i--;}
            return spans.Select(r=>new Line{Bounds=r,Signature=Signature(mask,w,h,r)}).ToList();
        }
        private static double[] Signature(bool[] mask,int w,int h,Rectangle box)
        {
            const int sw=160,sh=20;double[] result=new double[sw*sh];
            for(int y=0;y<sh;y++)for(int x=0;x<sw;x++)
            {
                double px=box.Left+(x+0.5)*box.Width/sw-0.5,py=box.Top+(y+0.5)*box.Height/sh-0.5,total=0;
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {
                    double xx=px+dx*0.32,yy=py+dy*0.25;int ix=(int)Math.Floor(xx),iy=(int)Math.Floor(yy);double fx=xx-ix,fy=yy-iy;
                    for(int j=0;j<2;j++)for(int i=0;i<2;i++)
                        if(ix+i>=0&&iy+j>=0&&ix+i<w&&iy+j<h&&mask[(iy+j)*w+ix+i])total+=(i==0?1-fx:fx)*(j==0?1-fy:fy);
                }
                result[y*sw+x]=total/9;
            }
            return result;
        }
        public static double Similarity(Line a,Line b)
        {
            const int w=160,h=20;double best=0;
            for(int dy=-2;dy<=2;dy++)for(int dx=-3;dx<=3;dx++)
            {
                double dot=0,aa=0,bb=0;
                for(int y=2;y<h-2;y++)for(int x=3;x<w-3;x++)
                {double v=a.Signature[y*w+x],z=b.Signature[(y+dy)*w+x+dx];dot+=v*z;aa+=v*v;bb+=z*z;}
                best=Math.Max(best,dot/Math.Sqrt(Math.Max(1e-12,aa*bb)));
            }
            return best;
        }
        public static double Match(List<Line> template,List<Line> lines,int start)
        {
            if(!LayoutMatches(template,lines,start))return 0;
            double sum=0,min=1,tw=template.Max(l=>l.Bounds.Width),rw=lines.Skip(start).Take(template.Count).Max(l=>l.Bounds.Width);
            for(int i=0;i<template.Count;i++)
            {
                if(Math.Abs(template[i].Bounds.Width/tw-lines[start+i].Bounds.Width/rw)>0.085)return 0;
                double score=Similarity(template[i],lines[start+i]);sum+=score;min=Math.Min(min,score);
            }
            return min<0.65?0:sum/template.Count;
        }
        public static bool LayoutMatches(List<Line> template,List<Line> lines,int start)
        {
            if(template.Count<3||start+template.Count>lines.Count)return false;
            double tw=template.Max(l=>l.Bounds.Width),rw=lines.Skip(start).Take(template.Count).Max(l=>l.Bounds.Width),scale=rw/tw;
            double centre=lines.Skip(start).Take(template.Count).Average(l=>l.Bounds.Left+l.Bounds.Width/2.0);
            for(int i=0;i<template.Count;i++)
            {
                Rectangle t=template[i].Bounds,r=lines[start+i].Bounds;
                if(Math.Abs(t.Width/tw-r.Width/rw)>0.10||Math.Abs(r.Left+r.Width/2.0-centre)>Math.Max(5,rw*0.06))return false;
                if(Math.Abs(r.Top-lines[start].Bounds.Top-(t.Top-template[0].Bounds.Top)*scale)>Math.Max(4,scale*5))return false;
            }
            return true;
        }
        public static bool CompleteRows(List<Line> template,List<Line> lines,int start)
        {
            if(template.Count<3||start+template.Count>lines.Count)return false;
            int last=template.Count-1;double scale=(lines[start+last].Bounds.Top-lines[start].Bounds.Top)/(double)Math.Max(1,template[last].Bounds.Top-template[0].Bounds.Top);
            if(scale<0.45||scale>2.5)return false;
            for(int i=0;i<template.Count;i++)
                if(Math.Abs(lines[start+i].Bounds.Top-lines[start].Bounds.Top-(template[i].Bounds.Top-template[0].Bounds.Top)*scale)>Math.Max(3,scale*4))return false;
            return true;
        }
        // A full-window beige run can be the sand behind a scroll, or the
        // minimap. Neither is a clue sheet. A framed treasure map is kept
        // even when it is larger than that sand cap.
        public static bool SkipClueSheet(Rectangle r, int imageWidth)
        {
            return SkipClueSheet(r, imageWidth, false);
        }
        public static bool SkipClueSheet(Rectangle r, int imageWidth, bool framed)
        {
            if (!framed && (r.Width > 1100 || r.Height > 700)) return true;
            if (framed && (r.Width > 2200 || r.Height > 1600)) return true;
            // The minimap sits in the last ~700 px of a 1920-wide client.
            // Scale that band with the capture so an ultrawide clue further
            // left is still read. Cap it so a small map image is not treated
            // as a minimap.
            int band=(int)Math.Round(700.0*Math.Max(1,imageWidth)/2560.0);
            int cap=(int)(imageWidth*0.45);
            if(cap>0&&band>cap)band=cap;
            if (r.Top < 48 && r.Left > imageWidth-band && r.Width < 560 && r.Height < 320) return true;
            return false;
        }
        public static List<Rectangle> ClueSheets(Pixels p)
        {
            var result = new List<Rectangle>();
            if (p == null) return result;
            foreach (Rectangle scroll in FindScrolls(p)) if (!SkipClueSheet(scroll, p.Width, true)) result.Add(scroll);
            foreach (Rectangle paper in FindPapers(p))
            {
                if (SkipClueSheet(paper, p.Width) || paper.Width < 180 || paper.Height < 90) continue;
                bool same = false;
                foreach (Rectangle have in result)
                {
                    Rectangle hit = Rectangle.Intersect(have, paper);
                    int small = Math.Min(have.Width * have.Height, paper.Width * paper.Height);
                    if (hit.Width > 0 && small > 0 && hit.Width * hit.Height >= small * 0.55) { same = true; break; }
                }
                if (!same) result.Add(paper);
            }
            return result;
        }
    }
}

