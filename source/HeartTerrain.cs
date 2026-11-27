using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace KavrosClueHelper
{
    // The Heart has disconnected platforms over a nearly flat blue background.
    // Use their outlines even when the live minimap's textures and tint differ.
    internal sealed class HeartTerrain
    {
        private byte[] terrain;
        private byte[] boxTerrain,trackTerrain;
        private int width,height,boxWidth,boxHeight,trackWidth,trackHeight;
        private RectangleF bounds,boxBounds,trackBounds;
        private bool boxReady,trackReady;
        private sealed class Fit { public double X,Y,Scale,Angle,Error; }
        private struct Sample { public double X,Y; public byte Land; }

        private bool Load(string assets,ScanRegion region,bool track)
        {
            if(track?trackReady:boxReady){Use(track);return terrain!=null;}
            string file=track?Path.Combine(assets,"scan",region.Id+"-track.png"):Path.Combine(assets,"scan",region.AssetKey+".png");
            if(!File.Exists(file)){if(track)trackReady=true;return false;}
            RectangleF area=region.MapBounds;
            if(track)
            {
                RectangleF parsed;
                string table=Path.Combine(assets,"scan",region.Id+"-track-map.tsv");
                if(!File.Exists(table)||!ScanTables.TryBox(File.ReadAllText(table),out parsed)){trackReady=true;return false;}
                area=parsed;
            }
            int w=(int)Math.Ceiling(area.Width),h=(int)Math.Ceiling(area.Height);
            if(w<8||h<8||w>1600||h>1600){if(track)trackReady=true;else boxReady=true;return false;}
            byte[] grid=new byte[w*h];
            using(var im=new Bitmap(file))
            {
                var p=new Pixels(im);
                for(int y=0;y<h;y++)for(int x=0;x<w;x++){
                    Color c=p.Get((int)((x+.5)*im.Width/w),(int)((y+.5)*im.Height/h));
                    grid[y*w+x]=(byte)(Math.Abs(c.R-120)+Math.Abs(c.G-138)+Math.Abs(c.B-166)<42?0:1);
                }
            }
            if(track){trackTerrain=grid;trackWidth=w;trackHeight=h;trackBounds=area;trackReady=true;}
            else{boxTerrain=grid;boxWidth=w;boxHeight=h;boxBounds=area;boxReady=true;}
            Use(track);
            return true;
        }
        private void Use(bool track)
        {
            if(track){terrain=trackTerrain;width=trackWidth;height=trackHeight;bounds=trackBounds;}
            else{terrain=boxTerrain;width=boxWidth;height=boxHeight;bounds=boxBounds;}
        }
        private double Error(List<Sample> samples,double x,double y,double scale,double angle,double limit)
        {
            double c=Math.Cos(angle)/scale,s=Math.Sin(angle)/scale;int errors=0;
            int max=(int)(limit*samples.Count);
            foreach(Sample v in samples){
                int rx=(int)Math.Floor(x+v.X*c-v.Y*s),ry=(int)Math.Floor(y+v.X*s+v.Y*c);
                if(rx<0||ry<0||rx>=width||ry>=height||terrain[ry*width+rx]!=v.Land)errors++;
                if(errors>max)return 1;
            }
            return errors/(double)samples.Count;
        }
        public MinimapMatch Find(Bitmap image,string assets,ScanRegion region,Point dot,Rectangle view,MinimapMatch previous,CancellationToken token,double? north)
        {
            if(!Load(assets,region,false))return null;
            MinimapMatch hit=Search(image,dot,view,previous,token,north);
            if(hit!=null||previous!=null)return hit;
            if(!Load(assets,region,true))return null;
            return Search(image,dot,view,null,token,north);
        }
        private MinimapMatch Search(Bitmap image,Point dot,Rectangle view,MinimapMatch previous,CancellationToken token,double? north)
        {
            var p=new Pixels(image);
            int radius=Math.Min(155,Math.Min(Math.Min(dot.X-view.Left,view.Right-dot.X),Math.Min(dot.Y-view.Top,view.Bottom-dot.Y))-9);
            if(radius<30)return null;
            int step=Math.Max(2,radius/10);var points=new List<Sample>();
            for(int dy=-radius;dy<=radius;dy+=step)for(int dx=-radius;dx<=radius;dx+=step){
                if(dx*dx+dy*dy>radius*radius||dx*dx+dy*dy<100)continue;
                Color v=p.Get(dot.X+dx,dot.Y+dy);
                bool empty=Math.Abs(v.R-49)+Math.Abs(v.G-65)+Math.Abs(v.B-90)<24;
                points.Add(new Sample{X=dx,Y=dy,Land=(byte)(empty?0:1)});
            }
            int water=points.Count(v=>v.Land==0);if(water<points.Count*.15||water>points.Count*.83)return null;
            var sparse=points.Where((v,i)=>i%4==0).ToList();var seeds=new List<Fit>();
            double px=previous==null?0:previous.Position.X-bounds.Left,py=previous==null?0:bounds.Bottom-previous.Position.Y;
            double[] scales=previous==null?new[]{.8,1.0,1.25,1.5,1.8,2.1,2.5,3,3.5,4.2,5,6,7.2,8.5,10}:new[]{previous.PixelsPerTile*.82,previous.PixelsPerTile,previous.PixelsPerTile*1.2};
            double[] angles;
            if(previous!=null)angles=Enumerable.Range(-3,7).Select(i=>previous.Rotation+i*Math.PI/16).ToArray();
            else if(north.HasValue)angles=Enumerable.Range(-3,7).Select(i=>north.Value+i*Math.PI/12).ToArray();
            else angles=Enumerable.Range(0,24).Select(i=>i*Math.PI/12).ToArray();
            int jump=previous==null?(width>400?8:4):2;
            foreach(double scale in scales)foreach(double a in angles){
                token.ThrowIfCancellationRequested();
                for(double y=previous==null?1:Math.Max(0,py-20);y<(previous==null?height:Math.Min(height,py+21));y+=jump)
                for(double x=previous==null?1:Math.Max(0,px-20);x<(previous==null?width:Math.Min(width,px+21));x+=jump){
                    if(terrain[(int)y*width+(int)x]==0)continue;
                    if(Error(sparse,x,y,scale,a,.21)>.21)continue;
                    double error=Error(points,x,y,scale,a,.18);
                    if(error>.18)continue;
                    var nearest=seeds.Find(v=>Math.Abs(v.X-x)<5&&Math.Abs(v.Y-y)<5&&Math.Abs(v.Scale-scale)<.5&&Math.Abs(CompassMath.Difference(v.Angle,a))<.25);
                    if(nearest!=null){if(nearest.Error<=error)continue;seeds.Remove(nearest);}
                    seeds.Add(new Fit{X=x,Y=y,Scale=scale,Angle=a,Error=error});
                    if(seeds.Count>32)seeds.Remove(seeds.OrderByDescending(v=>v.Error).First());
                }
            }
            foreach(var seed in seeds){
                double x=seed.X,y=seed.Y,angle=seed.Angle,scale=seed.Scale;
                int fine=previous==null&&width>400?5:3;
                for(int dy=-fine;dy<=fine;dy++)for(int dx=-fine;dx<=fine;dx++)for(int da=-2;da<=2;da++)for(int ds=-2;ds<=2;ds++){
                    double k=scale*(1+ds*.04),a=angle+da*Math.PI/60,e=Error(points,x+dx,y+dy,k,a,seed.Error);
                    if(e<seed.Error){seed.X=x+dx;seed.Y=y+dy;seed.Scale=k;seed.Angle=a;seed.Error=e;}
                }
            }
            var best=seeds.OrderBy(v=>v.Error).FirstOrDefault();
            if(best==null||best.Error>.095||seeds.Any(v=>Math.Abs(v.X-best.X)+Math.Abs(v.Y-best.Y)>10&&v.Error<best.Error+.038))return null;
            return new MinimapMatch{Position=new PointF((float)(bounds.Left+best.X),(float)(bounds.Bottom-best.Y)),PlayerPixel=dot,Viewport=view,PixelsPerTile=best.Scale,Rotation=best.Angle,Error=best.Error};
        }
    }
}
