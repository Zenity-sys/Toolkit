using System;using System.Collections.Generic;using System.Drawing;using System.IO;using System.Linq;
namespace KavrosClueHelper
{
 public sealed class MinimapAnchor
 {
  public struct Mark {public Rectangle Box;public bool Legacy;public double Score;}
  private sealed class Model {public ImagePattern Pattern;public Size Size;public Point[] Points;public double[] Values;public double Energy;public Point[] Blue;public bool Legacy;}
  public bool Strong {get;private set;}
  public bool? InterfaceLegacy;
  private readonly List<Model> models=new List<Model>();
  public MinimapAnchor(string assets)
  {
   Add(Path.Combine(assets,"minimap","home.png"),false);
   Add(Path.Combine(assets,"minimap","legacy-h.png"),true);
  }
  private void Add(string file,bool legacy)
  {
   if(!File.Exists(file))return;
   using(var original=new Bitmap(file))foreach(float scale in new[]{1F,0.8F,1.25F,1.5F,2F,0.6F,4F/3F})
   using(Bitmap image=Pixels.Crop(original,new Rectangle(Point.Empty,original.Size),new Size(Math.Max(8,(int)Math.Round(original.Width*scale)),Math.Max(8,(int)Math.Round(original.Height*scale)))))
   {
    var points=new List<Point>();var values=new List<double>();var p=new Pixels(image);
    int y0=legacy?0:(int)(image.Height*0.23),y1=legacy?image.Height:(int)(image.Height*0.77),x0=legacy?0:(int)(image.Width*0.23),x1=legacy?image.Width:(int)(image.Width*0.77);
    for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++){Color c=p.Get(x,y);points.Add(new Point(x,y));values.Add(Math.Min(c.R,Math.Min(c.G,c.B)));}
    if(points.Count<4)continue;
    double mean=values.Average();double[] centred=values.Select(v=>v-mean).ToArray();models.Add(new Model{Pattern=new ImagePattern(image),Size=image.Size,Points=points.ToArray(),Values=centred,Energy=centred.Sum(v=>v*v),Blue=points.Where(q=>{Color c=p.Get(q.X,q.Y);return c.B>c.R+16&&c.B>c.G+10;}).ToArray(),Legacy=legacy});
   }
  }
  private static double Glyph(Pixels image,Point hit,Model model)
  {
   double sum=0,squares=0,product=0;
   for(int i=0;i<model.Points.Length;i++){Point p=model.Points[i];Color c=image.Get(hit.X+p.X,hit.Y+p.Y);double v=Math.Min(c.R,Math.Min(c.G,c.B));sum+=v;squares+=v*v;product+=v*model.Values[i];}
   double energy=squares-sum*sum/model.Points.Length;int blue=model.Blue.Count(p=>{Color c=image.Get(hit.X+p.X,hit.Y+p.Y);return c.B>c.R+8&&c.B>c.G+5;});
   // The modern home button is a blue ring. The legacy H button is grey, so a grey template is not rejected for having no blue.
   bool blueOk=model.Blue.Length<4||blue>=model.Blue.Length*0.60;
   return energy<=0||!blueOk?0:product/Math.Sqrt(energy*model.Energy);
  }
  public List<Mark> Marks(Bitmap image)
  {
   var candidates=new List<Mark>();var pixels=new Pixels(image);
   foreach(Model model in models){if(InterfaceLegacy.HasValue&&model.Legacy!=InterfaceLegacy.Value)continue;foreach(Point hit in model.Pattern.Find(pixels,new Rectangle(Point.Empty,image.Size),1000,100))
   {double score=Glyph(pixels,hit,model);if(score>=(model.Legacy?0.85:0.60))candidates.Add(new Mark{Box=new Rectangle(hit,model.Size),Legacy=model.Legacy,Score=score});}}
   Strong=candidates.Count>0&&candidates.Max(c=>c.Score)>=0.90;
   var result=new List<Mark>();foreach(Mark candidate in candidates.Where(c=>c.Score>=candidates.Max(v=>v.Score)-0.08).OrderByDescending(c=>c.Score))
   {Rectangle box=candidate.Box;if(!result.Any(r=>Rectangle.Inflate(r.Box,r.Box.Width/2,r.Box.Height/2).Contains(new Point(box.Left+box.Width/2,box.Top+box.Height/2))))result.Add(candidate);}
   return result;
  }
  public List<Rectangle> Find(Bitmap image){return Marks(image).Select(m=>m.Box).ToList();}
  public static bool ContainsHome(Rectangle view,Rectangle home)
  {
   double scale=home.Width/22.0;return home.Left>=view.Left-4*scale&&home.Left-view.Left<30*scale&&home.Bottom<=view.Bottom+4*scale&&view.Bottom-home.Bottom<22*scale;
  }
  // The legacy H button sits on the top-right rim of the round orb, not at the bottom-left.
  public static bool ContainsLegacy(Rectangle view,Rectangle mark)
  {
   int cx=mark.Left+mark.Width/2,cy=mark.Top+mark.Height/2;
   return cx>=view.Right-view.Width*0.30&&cx<=view.Right+view.Width*0.22&&cy>=view.Top-view.Height*0.08&&cy<=view.Top+view.Height*0.30;
  }
  public static MinimapViewport LegacyViewport(int width,int height,Rectangle mark)
  {
   // Offsets measured from the H button to the orb centre on a 1440p Kavros shot. The button is 30px wide there.
   double s=mark.Width/30.0;
   int cx=mark.Left+mark.Width/2+(int)Math.Round(-107*s),cy=mark.Top+mark.Height/2+(int)Math.Round(43*s),radius=(int)Math.Round(102*s);
   Rectangle bounds=Rectangle.Intersect(new Rectangle(0,0,width,height),Rectangle.FromLTRB(cx-radius,cy-radius,cx+radius,cy+radius));
   if(bounds.Width<60||bounds.Height<60||bounds.Width>1024||bounds.Height>1024)return null;
   return new MinimapViewport{Bounds=bounds,FrameSides=2};
  }
  public static MinimapViewport FromCentre(int width,int height,Point centre,Rectangle home)
  {
   double scale=home.Width/22.0;int left=(int)Math.Round(home.Left-7*scale),bottom=(int)Math.Round(home.Bottom+8*scale);
   if(centre.X-left<30||bottom-centre.Y<30)return null;
   Rectangle bounds=Rectangle.Intersect(new Rectangle(0,0,width,height),Rectangle.FromLTRB(left,2*centre.Y-bottom,2*centre.X-left,bottom));
   if(bounds.Width<60||bounds.Height<60||bounds.Width>1024||bounds.Height>1024)return null;
   return new MinimapViewport{Bounds=bounds,FrameSides=2};
  }
  private static bool Frame(int colour)
  {int r=(colour>>16)&255,g=(colour>>8)&255,b=colour&255;return r<12&&g<12&&b<12||r<65&&g<100&&b<115&&g>=r+3&&b>=r+4;}
  public static MinimapViewport Viewport(int width,int height,Rectangle home,Func<int,int,int> pixel)
  {
   double scale=home.Width/22.0;int expectedLeft=(int)Math.Round(home.Left-7*scale),expectedBottom=(int)Math.Round(home.Bottom+8*scale);
   int left=-1,top=-1,length=0,start=Math.Max(0,home.Top-home.Height/2),margin=Math.Max(3,(int)Math.Ceiling(4*scale));
   for(int x=Math.Max(0,expectedLeft-margin);x<=Math.Min(width-1,expectedLeft+margin);x++)
   {
    int bad=0,last=start,hits=0;
    for(int y=start;y>=Math.Max(0,start-1024);y--){if(Frame(pixel(x,y))){last=y;hits++;bad=0;}else if(++bad>3)break;}
    int run=start-last+1;if(run>=60&&hits>run*0.85&&run>length){length=run;left=x;top=last;}
   }
   if(left<0)return null;int right=-1,bottom=-1,best=0;
   for(int y=Math.Max(0,expectedBottom-margin);y<=Math.Min(height-1,expectedBottom+margin);y++)
   {
    int begin=Math.Min(width-1,left+home.Width*2),last=begin,bad=0,hits=0;
    for(int x=begin;x<Math.Min(width,left+1024);x++){if(Frame(pixel(x,y))){last=x;hits++;bad=0;}else if(++bad>3)break;}
    int run=last-begin+1;if(run<30||hits<run*0.85)continue;
    int score=run*10-Math.Abs(y-expectedBottom);if(score>best){best=score;right=last+1;bottom=y+1;}
   }
   if(right<0||bottom-top<60||right-left<60||bottom-top>1024||right-left>1024)return null;
   return new MinimapViewport{Bounds=Rectangle.FromLTRB(left,top,right,bottom),FrameSides=3};
  }
 }
}
