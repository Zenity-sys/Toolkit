using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace KavrosClueHelper
{
    public sealed class MinimapMatch
    {
        public PointF Position;
        public Point PlayerPixel;
        public Rectangle Viewport;
        public double PixelsPerTile,Rotation,Error;
    }

    // Screenshot-only matching. No process memory, input hooks or game control.
    // Compare terrain across several rings; icons and player/NPC dots are outliers.
    public sealed class MinimapPositionReader
    {
        private sealed class Surface
        {
            public readonly int Width,Height;
            public readonly int[] Data;
            public Surface(Bitmap image)
            {
                Width=image.Width;Height=image.Height;Data=new int[Width*Height];
                using(Bitmap copy=new Bitmap(Width,Height,PixelFormat.Format32bppArgb))
                {
                    using(Graphics g=Graphics.FromImage(copy))g.DrawImageUnscaled(image,0,0);
                    BitmapData bits=copy.LockBits(new Rectangle(0,0,Width,Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
                    try{for(int y=0;y<Height;y++)Marshal.Copy(IntPtr.Add(bits.Scan0,y*bits.Stride),Data,y*Width,Width);}
                    finally{copy.UnlockBits(bits);}
                }
            }
            public int Get(int x,int y){return x<0||y<0||x>=Width||y>=Height?0:Data[y*Width+x];}
        }
        private struct Sample {public int X,Y,Colour;}
        private sealed class Trial
        {
            public double X,Y,Scale,Angle,Error;
        }
        private Surface reference;
        private RectangleF world,searchWorld;
        private int regionId;
        private int regionLevel=Int32.MinValue,misses;
        private PointF velocity;
        private DateTime seeded=DateTime.MinValue;
        private bool wideNext;
        public void ForceWide(){wideNext=true;}
        private readonly string assets;
        private readonly MinimapAnchor anchor;
        private readonly MinimapOrientation orientation;
        private MinimapMatch previous;
        private bool previousStructure;
        private readonly MinimapStructure structure=new MinimapStructure();
        private readonly HeartTerrain heart=new HeartTerrain();
        private int searchBatch;
        public string Diagnostics {get;private set;}
        public MinimapPositionReader(string directory){assets=directory;anchor=new MinimapAnchor(directory);orientation=new MinimapOrientation(directory);}
        public void UseInterface(bool legacy){anchor.InterfaceLegacy=legacy;}
        public void Reset(){previous=null;previousStructure=false;searchBatch=0;misses=0;velocity=PointF.Empty;seeded=DateTime.MinValue;}
        public void KeepSeed(MinimapMatch match)
        {
            if(match==null)return;
            if(previous==null)previous=match;
            if(seeded==DateTime.MinValue)seeded=DateTime.UtcNow;
        }
        private void Remember(MinimapMatch match)
        {
            previous=match;
            seeded=DateTime.UtcNow;
        }
        public void SetReference(Bitmap image,RectangleF bounds,int id)
        {reference=new Surface(image);world=searchWorld=bounds;regionId=id;regionLevel=Int32.MinValue;Reset();}
        private void Load(ScanRegion region)
        {
            if(reference!=null&&region.Id==regionId&&(regionLevel==Int32.MinValue||regionLevel==region.Level)){regionLevel=region.Level;return;}
            string referenceFile=Path.Combine(assets,"scan",region.Id+"-track.png");
            RectangleF bounds=region.MapBounds;
            if(File.Exists(referenceFile))
            {
                try
                {
                    RectangleF parsed;
                    if(ScanTables.TryBox(File.ReadAllText(Path.Combine(assets,"scan",region.Id+"-track-map.tsv")), out parsed)) bounds=parsed;
                }
                catch(Exception ex){CrashLog.Write(ex);}
            }
            else referenceFile=Path.Combine(assets,"scan",region.AssetKey+".png");
            using(Bitmap image=new Bitmap(referenceFile))SetReference(image,bounds,region.Id);
            regionLevel=region.Level;
            searchWorld=RectangleF.Intersect(world,region.MapBounds);
        }
        private static bool White(int c,bool soft=false)
        {int r=(c>>16)&255,g=(c>>8)&255,b=c&255;int minimum=soft?205:230;return r>=minimum&&g>=minimum&&b>=minimum&&Math.Max(r,Math.Max(g,b))-Math.Min(r,Math.Min(g,b))<20;}
        private static bool Terrain(int c)
        {
            int r=(c>>16)&255,g=(c>>8)&255,b=c&255;
            return Math.Min(r,Math.Min(g,b))>=18&&Math.Max(r,Math.Max(g,b))<=225&&!(r>170&&g>145&&b<100)&&!(b>r*1.5&&b>g*1.2);
        }
        private static List<Point> PlayerDots(Surface image,bool soft=false)
        {
            var dots=new List<Point>();bool[] seen=new bool[image.Data.Length];var queue=new Queue<int>();
            for(int y=28;y<image.Height-28;y++)for(int x=28;x<image.Width-28;x++)
            {
                int index=y*image.Width+x;if(seen[index]||!White(image.Data[index],soft))continue;
                int left=x,right=x,top=y,bottom=y,count=0;queue.Enqueue(index);seen[index]=true;
                while(queue.Count>0)
                {
                    int v=queue.Dequeue(),vx=v%image.Width,vy=v/image.Width;count++;
                    left=Math.Min(left,vx);right=Math.Max(right,vx);top=Math.Min(top,vy);bottom=Math.Max(bottom,vy);
                    foreach(int next in new[]{v-1,v+1,v-image.Width,v+image.Width})
                        if(next>=0&&next<seen.Length&&!seen[next]&&White(image.Data[next],soft)){seen[next]=true;queue.Enqueue(next);}
                }
                int w=right-left+1,h=bottom-top+1;
                if(w>=2&&h>=2&&w<=7&&h<=7&&Math.Abs(w-h)<=2&&count>=w*h*0.7)
                    dots.Add(new Point((left+right)/2,(top+bottom)/2));
            }
            // Most layouts put the minimap here, but every candidate remains
            // searchable in later batches when the minimap has been moved.
            return dots.OrderBy(p=>Math.Abs(p.X-image.Width*0.88)+Math.Abs(p.Y-image.Height*0.18)).ToList();
        }
        private static List<Sample> Samples(Surface image,Point dot,Rectangle bounds,int density)
        {
            var values=new List<Sample>();
            int cap=bounds.Width>=350&&bounds.Height>=300?180:94;
            int rx=Math.Min(cap,Math.Min(dot.X-bounds.Left,bounds.Right-1-dot.X)-7),ry=Math.Min(cap,Math.Min(dot.Y-bounds.Top,bounds.Bottom-1-dot.Y)-7);
            if(rx<18||ry<18)return values;
            for(int gy=-density;gy<=density;gy++)for(int gx=-density;gx<=density;gx++)
            {
                if(gx*gx+gy*gy>density*density)continue;
                int x=(int)Math.Round(gx*rx/(double)density),y=(int)Math.Round(gy*ry/(double)density);
                if(x*x+y*y<8*8)continue;
                int colour=image.Get(dot.X+x,dot.Y+y);if(Terrain(colour))values.Add(new Sample{X=x,Y=y,Colour=colour});
            }
            return values.OrderBy(v=>unchecked((uint)((v.X+2048)*73856093)^(uint)((v.Y+2048)*19349663))).ToList();
        }
        private static bool Textured(List<Sample> samples)
        {
            if(samples.Count<45)return false;
            double mean=samples.Average(p=>(double)(((p.Colour>>16)&255)+((p.Colour>>8)&255)+(p.Colour&255))/3);
            return samples.Average(p=>Math.Pow((((p.Colour>>16)&255)+((p.Colour>>8)&255)+(p.Colour&255))/3.0-mean,2))>180;
        }
        private double Score(List<Sample> samples,double x,double y,double scale,double angle,double cutoff)
        {
            double cos=Math.Cos(angle)/scale,sin=Math.Sin(angle)/scale,sx=reference.Width/world.Width,sy=reference.Height/world.Height,total=0;int valid=0;
            for(int i=0;i<samples.Count;i++)
            {
                Sample p=samples[i];
                double wx=x+p.X*cos-p.Y*sin,wy=y-p.X*sin-p.Y*cos;
                int rx=(int)Math.Round((wx-world.Left)*sx),ry=(int)Math.Round((world.Bottom-wy)*sy);
                if(rx<0||ry<0||rx>=reference.Width||ry>=reference.Height){continue;}
                int c=reference.Data[ry*reference.Width+rx],v=p.Colour;
                total+=Math.Min(65,(Math.Abs(((c>>16)&255)-((v>>16)&255))+Math.Abs(((c>>8)&255)-((v>>8)&255))+Math.Abs((c&255)-(v&255)))/3.0);valid++;
                if(total>cutoff*samples.Count)return 100;
            }
            return valid<samples.Count*0.60||valid<30?100:total/valid;
        }
        private static void Keep(List<Trial> best,Trial trial,int limit)
        {
            Trial same=best.FirstOrDefault(t=>Math.Abs(t.X-trial.X)<7&&Math.Abs(t.Y-trial.Y)<7&&Math.Abs(t.Scale-trial.Scale)<0.4&&Math.Abs(CompassMath.Difference(t.Angle,trial.Angle))<Math.PI/12);
            if(same!=null){if(same.Error<=trial.Error)return;best.Remove(same);}
            best.Add(trial);best.Sort((a,b)=>a.Error.CompareTo(b.Error));if(best.Count>limit)best.RemoveAt(best.Count-1);
        }
        private Trial Refine(Trial seed,List<Sample> samples,CancellationToken token)
        {
            Trial best=seed;
            foreach(double da in new[]{-Math.PI/12,-Math.PI/24,0,Math.PI/24,Math.PI/12})
            foreach(double ds in new[]{-0.5,0,0.5})
            {
                double scale=seed.Scale+ds;if(scale<0.5)continue;
                for(double y=seed.Y-4;y<=seed.Y+4;y+=1)for(double x=seed.X-4;x<=seed.X+4;x+=1)
                {
                    token.ThrowIfCancellationRequested();double error=Score(samples,x,y,scale,seed.Angle+da,best.Error);
                    if(error<best.Error)best=new Trial{X=x,Y=y,Scale=scale,Angle=seed.Angle+da,Error=error};
                }
            }
            Trial centre=best;
            foreach(double da in new[]{-Math.PI/90,-Math.PI/180,0,Math.PI/180,Math.PI/90})foreach(double ds in new[]{-0.125,-0.0625,0,0.0625,0.125})
                for(double y=centre.Y-1;y<=centre.Y+1;y+=0.5)for(double x=centre.X-1;x<=centre.X+1;x+=0.5)
                {
                    double error=Score(samples,x,y,centre.Scale+ds,centre.Angle+da,best.Error);
                    if(error<best.Error)best=new Trial{X=x,Y=y,Scale=centre.Scale+ds,Angle=centre.Angle+da,Error=error};
                }
            return best;
        }
        private List<Trial> Search(List<Sample> samples,CancellationToken token,bool local,bool adapt=false)
        {
            var best=new List<Trial>();List<Sample> coarse=samples.Where((s,i)=>i%Math.Max(1,samples.Count/40)==0).Take(40).ToList();
            double left=searchWorld.Left+3,right=searchWorld.Right-3,bottom=searchWorld.Top+3,top=searchWorld.Bottom-3,step=2;
            double[] scales={1,1.5,2,2.5,3,4,5,6,8,10,12,16,0.75};double[] angles=Enumerable.Range(0,24).Select(i=>i*Math.PI/12).ToArray();
            if(local&&previous!=null)
            {
                // After missed frames the player has kept moving: look further
                // along the recent heading and widen the local window a little.
                double elapsed=seeded==DateTime.MinValue?0.2:Math.Min(4,(DateTime.UtcNow-seeded).TotalSeconds);
                double run=Math.Max(MotionEstimate.MaxSpeed,Math.Abs(velocity.X)+Math.Abs(velocity.Y));
                double radius=Math.Min(140,Math.Max(28,18+run*elapsed+6*misses));
                double cx=previous.Position.X+velocity.X*(misses+1),cy=previous.Position.Y+velocity.Y*(misses+1);
                left=cx-radius;right=cx+radius;bottom=cy-radius;top=cy+radius;step=2;
                if(!adapt){scales=new[]{Math.Max(0.5,previous.PixelsPerTile-0.35),previous.PixelsPerTile,previous.PixelsPerTile+0.35};
                angles=new[]{previous.Rotation-Math.PI/18,previous.Rotation,previous.Rotation+Math.PI/18};}
            }
            foreach(double scale in scales)foreach(double angle in angles)
            {
                double searchStep=step;
                token.ThrowIfCancellationRequested();
                for(double y=bottom;y<=top;y+=searchStep)for(double x=left;x<=right;x+=searchStep)
                {
                    double cutoff=best.Count<32?35:Math.Min(35,best[best.Count-1].Error);
                    double error=Score(coarse,x,y,scale,angle,cutoff);
                    if(error<cutoff)Keep(best,new Trial{X=x,Y=y,Scale=scale,Angle=angle,Error=error},32);
                }
            }
            var refined=new List<Trial>();
            foreach(Trial trial in best)
            {
                trial.Error=Score(samples,trial.X,trial.Y,trial.Scale,trial.Angle,100);
                Keep(refined,Refine(trial,samples,token),32);
            }
            return refined;
        }
        public MinimapMatch Find(Bitmap capture,ScanRegion region,Point captureOffset,CancellationToken token)
        {
            Load(region);var image=new Surface(capture);var marks=anchor.Marks(capture);var homes=marks.Where(m=>!m.Legacy).Select(m=>m.Box).ToList();var legacy=marks.Where(m=>m.Legacy).Select(m=>m.Box).ToList();var dots=PlayerDots(image,homes.Concat(legacy).Any(h=>h.Width<20));Diagnostics="Home anchors: "+homes.Count+"; Legacy anchors: "+legacy.Count+"; Player dots: "+dots.Count;
            bool jumped=false;
            if(previous!=null)
            {
                Point expected=new Point(previous.PlayerPixel.X-captureOffset.X,previous.PlayerPixel.Y-captureOffset.Y);
                dots=dots.OrderBy(p=>Math.Abs(p.X-expected.X)+Math.Abs(p.Y-expected.Y)).ToList();
                if(dots.Count>0)jumped=Math.Abs(dots[0].X-expected.X)+Math.Abs(dots[0].Y-expected.Y)>110;
            }
            var homeViews=homes.Select(h=>MinimapAnchor.Viewport(image.Width,image.Height,h,image.Get)).Concat(legacy.Select(h=>MinimapAnchor.LegacyViewport(image.Width,image.Height,h))).Where(v=>v!=null).ToList();
            var candidates=dots.Select(dot=>new{Dot=dot,View=homeViews.Where(v=>v.Bounds.Contains(dot)&&Math.Abs(dot.X-v.Bounds.Left-v.Bounds.Width/2)<v.Bounds.Width*0.12&&Math.Abs(dot.Y-v.Bounds.Top-v.Bounds.Height/2)<v.Bounds.Height*0.12).OrderBy(v=>Math.Abs(dot.X-v.Bounds.Left-v.Bounds.Width/2)+Math.Abs(dot.Y-v.Bounds.Top-v.Bounds.Height/2)).FirstOrDefault()??MinimapViewport.Find(image.Width,image.Height,dot,image.Get)??(homes.Count==1&&dots.Count==1?MinimapAnchor.FromCentre(image.Width,image.Height,dot,homes[0]):null)??(legacy.Count==1&&dots.Count==1?MinimapAnchor.LegacyViewport(image.Width,image.Height,legacy[0]):null)}).Where(v=>v.View!=null).ToList();
            if(homes.Count>0||legacy.Count>0){var anchored=candidates.Where(v=>homes.Any(h=>MinimapAnchor.ContainsHome(v.View.Bounds,h))||legacy.Any(h=>MinimapAnchor.ContainsLegacy(v.View.Bounds,h))).OrderBy(v=>Math.Abs(v.Dot.X-(v.View.Bounds.Left+v.View.Bounds.Width/2))+Math.Abs(v.Dot.Y-(v.View.Bounds.Top+v.View.Bounds.Height/2))).Take(1).ToList();if(anchored.Count>0)candidates=anchored;}
            else if(previous==null)candidates=candidates.OrderByDescending(v=>v.View.FrameSides).ToList();
            int skip=previous==null&&candidates.Count>1?(searchBatch++)%candidates.Count:0;
            foreach(var candidate in candidates.Skip(skip).Take(1))
            {
                Point dot=candidate.Dot;Rectangle viewport=candidate.View.Bounds;
                token.ThrowIfCancellationRequested();
                if(region.Id==371&&homes.Count>0)
                {
                    double? north=homes.Where(h=>MinimapAnchor.ContainsHome(viewport,h)).Select(h=>orientation.Read(capture,viewport,h)).FirstOrDefault(v=>v.HasValue);
                    bool globalHeart=jumped||misses>=2||wideNext;
                    var fit=heart.Find(capture,assets,region,dot,viewport,globalHeart?null:previous,token,north);
                    if(fit!=null){
                        Rectangle clipped=fit.Viewport;clipped.Offset(captureOffset);fit.Viewport=clipped;
                        fit.PlayerPixel=new Point(dot.X+captureOffset.X,dot.Y+captureOffset.Y);
                        Remember(fit);previousStructure=false;misses=0;Diagnostics+="; Heart platform match";return fit;
                    }
                }
                bool trusted=previousStructure||anchor.Strong;
                if(previousStructure||homes.Count>0&&previous==null)
                {
                    double? north=homes.Where(h=>MinimapAnchor.ContainsHome(viewport,h)).Select(h=>orientation.Read(capture,viewport,h)).FirstOrDefault(v=>v.HasValue);Diagnostics+="; north "+(north.HasValue?north.Value.ToString():"unknown");
                    trusted=trusted||north.HasValue;
                    string info;MinimapMatch fit=structure.Find(capture,assets,region,dot,viewport,previousStructure?previous:null,token,out info,north);Diagnostics+="; "+info;
                    if(fit==null&&previousStructure&&misses>=2){fit=structure.Find(capture,assets,region,dot,viewport,null,token,out info,north);Diagnostics+="; reacquire "+info;}
                    if(fit!=null){
                    Rectangle box=fit.Viewport;box.Offset(captureOffset);fit.Viewport=box;fit.PlayerPixel=new Point(fit.PlayerPixel.X+captureOffset.X,fit.PlayerPixel.Y+captureOffset.Y);
                    Remember(fit);previousStructure=true;misses=0;return fit;}
                }
                var samples=Samples(image,dot,viewport,9);
                Diagnostics+="; dot "+dot+" viewport "+viewport+" samples "+samples.Count;if(!Textured(samples))continue;
                double maxError=trusted?10:22,margin=trusted?5:2.5;
                bool local=previous!=null&&misses<2&&!wideNext&&!jumped;if(wideNext)wideNext=false;
                var trials=Search(samples,token,local);
                Trial winner=trials.FirstOrDefault(),runner=winner==null?null:trials.Skip(1).FirstOrDefault(t=>CompassMath.Distance(new PointF((float)t.X,(float)t.Y),new PointF((float)winner.X,(float)winner.Y))>9);
                bool valid=winner!=null&&winner.Error<=maxError&&(runner==null||runner.Error>=winner.Error+margin);
                if(!valid&&previous!=null&&misses<2&&!jumped)
                {
                    trials=Search(samples,token,true,true);winner=trials.FirstOrDefault();
                    runner=winner==null?null:trials.Skip(1).FirstOrDefault(t=>CompassMath.Distance(new PointF((float)t.X,(float)t.Y),new PointF((float)winner.X,(float)winner.Y))>9);
                    valid=winner!=null&&winner.Error<=maxError&&(runner==null||runner.Error>=winner.Error+margin);
                }
                if(winner!=null)Diagnostics+=" best "+winner.X+","+winner.Y+" scale "+winner.Scale+" error "+winner.Error.ToString("0.00")+" runner "+(runner==null?"none":runner.Error.ToString("0.00"));
                if(!valid)continue;
                // Independent, denser pixels validate the winning coarse match.
                var dense=Samples(image,dot,viewport,13);double error=Score(dense,winner.X,winner.Y,winner.Scale,winner.Angle,100);
                if(error>winner.Error+4||viewport.Width<200||viewport.Height<200)
                {winner.Error=error;winner=Refine(winner,dense,token);error=winner.Error;}
                double sparse=Score(samples,winner.X,winner.Y,winner.Scale,winner.Angle,100);
                Diagnostics+=" dense "+error.ToString("0.00");
                if(!Textured(dense)||error>(trusted?12:23)||Math.Abs(error-sparse)>5)continue;
                Rectangle clientViewport=viewport;clientViewport.Offset(captureOffset);
                var nextPos=new PointF((float)winner.X,(float)winner.Y);
                if(previous!=null)velocity=new PointF(Math.Max(-10,Math.Min(10,nextPos.X-previous.Position.X)),Math.Max(-10,Math.Min(10,nextPos.Y-previous.Position.Y)));
                Remember(new MinimapMatch{Viewport=clientViewport,Position=nextPos,PlayerPixel=new Point(dot.X+captureOffset.X,dot.Y+captureOffset.Y),PixelsPerTile=winner.Scale,Rotation=winner.Angle,Error=error});
                previousStructure=false;misses=0;return previous;
            }
            // A single obscured frame must not discard the local search seed.
            // Nothing is returned as a live match until new pixels validate it.
            if(++misses>=12){velocity=PointF.Empty;}return null;
        }
    }
}
