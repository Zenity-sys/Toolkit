using System;using System.Collections.Generic;using System.Diagnostics;using System.Drawing;using System.IO;using System.Linq;using System.Threading;
namespace KavrosClueHelper
{
    public static class FollowRegressionTests
    {
        private static int checks;
        private static void Check(bool pass,string message){checks++;if(!pass)throw new Exception("FAILED: "+message);}
        private static int Oracle(KnotPuzzle p,int lane,int[] offsets,ref int tentative)
        {
            int best=Int32.MaxValue;
            if(lane==p.Lanes.Length)
            {
                if(!p.Locks.All(l=>KnotSolver.Equal(p.Lanes[l.A][KnotSolver.Mod(l.IndexA-offsets[l.A],p.Lanes[l.A].Length)],p.Lanes[l.B][KnotSolver.Mod(l.IndexB-offsets[l.B],p.Lanes[l.B].Length)])))return best;
                if(p.Locks.Any(l=>p.Lanes[l.A][KnotSolver.Mod(l.IndexA-offsets[l.A],p.Lanes[l.A].Length)].IsNot||p.Lanes[l.B][KnotSolver.Mod(l.IndexB-offsets[l.B],p.Lanes[l.B].Length)].IsNot)){tentative++;return best;}
                return offsets.Select((o,i)=>Math.Min(o,p.Lanes[i].Length-o)).Sum();
            }
            for(int i=0;i<p.Lanes[lane].Length;i++){offsets[lane]=i;best=Math.Min(best,Oracle(p,lane+1,offsets,ref tentative));}return best;
        }
        public static int Run(string report)
        {
            try
            {
                string assets=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets");var regions=new ScanVision(assets).Regions;ScanRegion prif=regions.Single(r=>r.Id==368);
                using(var original=new Bitmap(Path.Combine(assets,"test_assets","reported","prifddinas-minimap-native.png")))
                {
                    var reader=new MinimapPositionReader(assets);var watch=Stopwatch.StartNew();MinimapMatch first=reader.Find(original,prif,Point.Empty,CancellationToken.None);
                    File.AppendAllText(report,"Actual full capture: "+watch.ElapsedMilliseconds+" ms; "+reader.Diagnostics+"\r\n");
                    Check(first!=null,"actual Kavros Prifddinas minimap matches");
                    Check(new RectangleF(2205,3355,12,14).Contains(first.Position),"fit is at the visible central Prifddinas platform");
                    Check(first.Viewport.Left>=1615&&first.Viewport.Right==2048&&first.Viewport.Bottom>=376&&first.Viewport.Bottom<395,"Home Teleport anchors the actual minimap frame");
                    Check(first.PlayerPixel==new Point(1837,189),"actual white player marker is selected rather than scene dots");
                    Rectangle crop=Rectangle.Intersect(new Rectangle(Point.Empty,original.Size),Rectangle.Inflate(first.Viewport,5,5));
                    using(Bitmap clipped=original.Clone(crop,System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                    {
                        watch.Restart();MinimapMatch follow=reader.Find(clipped,prif,crop.Location,CancellationToken.None);
                        File.AppendAllText(report,"Actual cropped follow: "+watch.ElapsedMilliseconds+" ms; "+reader.Diagnostics+"\r\n");
                        Check(follow!=null&&CompassMath.Distance(follow.Position,first.Position)<2,"fresh cropped follow preserves world position");Check(follow.PlayerPixel==first.PlayerPixel,"cropped follow preserves client coordinates");
                        var session=new ScanSession();session.Observe(new ScanReading{Region=prif});session.PrepareRecheck();Check(!session.Position.HasValue,"slow acquisition never displays an old box");
                        DateTime now=DateTime.UtcNow;session.ApplyMatch(follow,now);Check(!session.Position.HasValue,"first fresh reading waits for confirmation");session.ApplyMatch(follow,now.AddMilliseconds(150));Check(session.PositionTracked&&session.Position.HasValue,"two fresh readings display the automatic box");session.Expire(now.AddSeconds(2));Check(session.Position.HasValue,"a short gap keeps the automatic box");
                        using(Bitmap mini=original.Clone(new Rectangle(1624,0,424,384),System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                        using(Bitmap terrain=(Bitmap)mini.Clone())
                        {
                            using(Graphics g=Graphics.FromImage(terrain))using(var brush=new SolidBrush(Color.FromArgb(140,145,150)))g.FillRectangle(brush,210,186,6,6);
                            foreach(Point shift in new[]{new Point(8,-4),new Point(16,-8),Point.Empty})using(Bitmap moved=(Bitmap)mini.Clone())
                            {
                                using(Graphics g=Graphics.FromImage(moved))
                                {
                                    g.SetClip(new Rectangle(5,5,414,374));g.DrawImageUnscaled(terrain,shift);g.ResetClip();
                                    foreach(Rectangle ui in new[]{new Rectangle(0,344,35,40),new Rectangle(0,0,35,35),new Rectangle(389,0,35,35),new Rectangle(389,344,35,40),new Rectangle(210,186,6,6)})g.DrawImage(mini,ui,ui,GraphicsUnit.Pixel);
                                }
                                watch.Restart();MinimapMatch movement=reader.Find(moved,prif,new Point(1624,0),CancellationToken.None);
                                double c=Math.Cos(first.Rotation)/first.PixelsPerTile,s=Math.Sin(first.Rotation)/first.PixelsPerTile;
                                PointF expected=new PointF((float)(first.Position.X-shift.X*c+shift.Y*s),(float)(first.Position.Y+shift.X*s+shift.Y*c));
                                File.AppendAllText(report,"Shifted actual terrain "+shift+": "+watch.ElapsedMilliseconds+" ms; "+reader.Diagnostics+"\r\n");
                                Check(movement!=null&&CompassMath.Distance(movement.Position,expected)<2.5,"actual terrain movement updates the world position");Check(watch.ElapsedMilliseconds<1200,"acquired movement stays fresh enough to display");
                            }
                        }
                    }
                    using(Bitmap mini=original.Clone(new Rectangle(1624,0,424,384),System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                    foreach(float scale in new[]{0.8F,1.25F})
                    using(Bitmap resized=Pixels.Crop(mini,new Rectangle(Point.Empty,mini.Size),new Size((int)Math.Round(mini.Width*scale),(int)Math.Round(mini.Height*scale))))
                    using(var moved=new Bitmap(920,720))
                    {
                        using(Graphics g=Graphics.FromImage(moved)){g.Clear(Color.FromArgb(80,65,45));g.DrawImageUnscaled(resized,90,110);}
                        reader.Reset();watch.Restart();MinimapMatch fit=reader.Find(moved,prif,Point.Empty,CancellationToken.None);File.AppendAllText(report,"Actual minimap resized "+scale+": "+watch.ElapsedMilliseconds+" ms; "+reader.Diagnostics+"\r\n");
                        Check(fit!=null&&CompassMath.Distance(fit.Position,first.Position)<4,"real minimap resized/moved: "+scale);Check(Math.Abs(CompassMath.Difference(fit.Rotation,first.Rotation))<0.15,"resized map retains the actual north direction");
                    }
                    using(Bitmap mini=original.Clone(new Rectangle(1624,0,424,384),System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                    {
                        var dot=new Point(213,189);
                        foreach(double turn in new[]{20.0,-25.0})using(var transformed=new Bitmap(424,384))
                        {
                            using(Graphics g=Graphics.FromImage(transformed)){g.Clear(Color.Black);g.TranslateTransform(dot.X,dot.Y);g.RotateTransform((float)turn);g.TranslateTransform(-dot.X,-dot.Y);g.DrawImageUnscaled(mini,0,0);}
                            double hint=first.Rotation-turn*Math.PI/180;string info;watch.Restart();MinimapMatch fit=new MinimapStructure().Find(transformed,assets,prif,dot,new Rectangle(0,0,424,384),first,CancellationToken.None,out info,hint);
                            File.AppendAllText(report,"Acquired camera turn "+turn+": "+watch.ElapsedMilliseconds+"ms; "+info+"\r\n");
                            Check(fit!=null&&CompassMath.Distance(fit.Position,first.Position)<3,"fresh north hint follows a camera turn without global reacquisition");
                            if(turn==20){MinimapMatch hiddenNorth=new MinimapStructure().Find(transformed,assets,prif,dot,new Rectangle(0,0,424,384),first,CancellationToken.None,out info,null);Check(hiddenNorth!=null&&CompassMath.Distance(hiddenNorth.Position,first.Position)<3,"local tracking follows a turn even when the north needle is obscured");}

                        }
                        using(var zoomed=new Bitmap(424,384))
                        {
                            using(Graphics g=Graphics.FromImage(zoomed)){g.Clear(Color.Black);g.TranslateTransform(dot.X,dot.Y);g.ScaleTransform(1.25F,1.25F);g.TranslateTransform(-dot.X,-dot.Y);g.DrawImageUnscaled(mini,0,0);}
                            string info;MinimapMatch fit=new MinimapStructure().Find(zoomed,assets,prif,dot,new Rectangle(0,0,424,384),first,CancellationToken.None,out info,first.Rotation);
                            File.AppendAllText(report,"Acquired zoom change: "+info+"\r\n");Check(fit!=null&&CompassMath.Distance(fit.Position,first.Position)<3,"local match adapts to a 25 percent zoom change");
                        }
                        reader.Reset();MinimapMatch acquired=reader.Find(mini,prif,new Point(1624,0),CancellationToken.None);
                        using(var obscured=new Bitmap(mini.Width,mini.Height))Check(reader.Find(obscured,prif,new Point(1624,0),CancellationToken.None)==null,"covered frame is never a live match");
                        watch.Restart();MinimapMatch resumed=reader.Find(mini,prif,new Point(1624,0),CancellationToken.None);Check(acquired!=null&&resumed!=null&&CompassMath.Distance(resumed.Position,acquired.Position)<2,"one missed frame preserves local seed");Check(watch.ElapsedMilliseconds<1000,"brief miss recovers with a local match");
                    }
                    reader.Reset();MinimapMatch wrong=reader.Find(original,regions.Single(r=>r.Id==371),Point.Empty,CancellationToken.None);File.AppendAllText(report,"Wrong region: "+reader.Diagnostics+"\r\n");Check(wrong==null,"Prifddinas pixels cannot become a Heart of Gielinor position");
                }
                EliteWalls(assets,report,regions);
                var random=new Random(5517);
                for(int trial=0;trial<70;trial++)
                {
                    var p=new KnotPuzzle{Lanes=Enumerable.Range(0,3).Select(l=>Enumerable.Range(0,6).Select(i=>new RuneValue(random.Next(3),random.Next(5)==0)).ToArray()).ToArray(),Locks=new[]{new KnotLock{A=0,B=1,IndexA=0,IndexB=2},new KnotLock{A=1,B=2,IndexA=3,IndexB=1},new KnotLock{A=0,B=2,IndexA=4,IndexB=5}}};
                    int tentative=0,expected=Oracle(p,0,new int[3],ref tentative);KnotSolution got=KnotSolver.Solve(p);
                    Check(expected!=Int32.MaxValue?got!=null&&got.Certain&&got.Cost==expected:tentative==1?got!=null&&!got.Certain:got==null,"optimized knot result matches independent exhaustive oracle "+trial);
                }
                var locks=new List<KnotLock>();for(int a=0;a<5;a++)for(int b=a+1;b<5;b++)locks.Add(new KnotLock{A=a,B=b,IndexA=a+b,IndexB=a+b+1});
                foreach(bool hidden in new[]{false,true})
                {
                    var p=new KnotPuzzle{Lanes=Enumerable.Range(0,5).Select(l=>Enumerable.Range(0,40).Select(i=>new RuneValue(0,hidden)).ToArray()).ToArray(),Locks=locks.ToArray()};
                    var watch=Stopwatch.StartNew();KnotSolution result=KnotSolver.Solve(p);File.AppendAllText(report,"Five 40-rune lanes, hidden="+hidden+": "+watch.ElapsedMilliseconds+" ms.\r\n");
                    Check(hidden?result==null:result!=null&&result.Certain&&result.Cost==0,"large ambiguous knot retains solution/ambiguity rules");Check(watch.ElapsedMilliseconds<1000,"uncertain knot does not enumerate millions of solutions");
                }
                File.AppendAllText(report,"V0.5.5 FOLLOW / KNOT PASS: "+checks+" assertions. Actual Kavros minimap, Home Teleport anchor, fresh cropped confirmation, resized/moved actual pixels, wrong-region rejection, elite structure locks, exhaustive knot oracle and worst-case search checks. No windows shown, desktop capture or game input.\r\n");return 0;
            }
            catch(Exception e){File.AppendAllText(report,e+"\r\n");return 1;}
        }
        private static bool PaleInk(Color c){int low=Math.Min(c.R,Math.Min(c.G,c.B)),high=Math.Max(c.R,Math.Max(c.G,c.B));return low>=185&&high-low<40;}
        private static void EliteWalls(string assets,string report,IList<ScanRegion> regions)
        {
            int[] ids={349,351,352,354,355,356,357,358,359,360,361,362,365,367};
            string realFull=null;
            foreach(string candidate in new[]{Path.Combine(assets,"test_assets","reported","brimhaven2-full.png"),"/workspace/brimhaven2-full.png","/workspace/brimhaven-minimap.png","/workspace/brimhaven2-minimap.png"})
                if(realFull==null&&File.Exists(candidate)&&candidate.IndexOf("full",StringComparison.Ordinal)>=0)realFull=candidate;
            ScanRegion brimhaven=regions.Single(r=>r.Id==356);
            if(realFull==null)File.AppendAllText(report,"Brimhaven real frames were not on this machine. Floor 0 is locked from a synthetic cut of the regenerated structure.\r\n");
            else
            {
                var reader=new MinimapPositionReader(assets);
                using(var frame=new Bitmap(realFull))
                {
                    var watch=Stopwatch.StartNew();MinimapMatch live=reader.Find(frame,brimhaven,Point.Empty,CancellationToken.None);
                    File.AppendAllText(report,"Brimhaven real frame "+realFull+": "+watch.ElapsedMilliseconds+" ms; "+reader.Diagnostics+"\r\n");
                    Check(live!=null&&live.Error<=3,"Brimhaven floor 0 locks from the real frame with error <= 3");
                    Check(Math.Abs(live.PlayerPixel.X-2343)<=40&&Math.Abs(live.PlayerPixel.Y-213)<=40,"the real Brimhaven frame uses the player dot");
                    Check(brimhaven.MapBounds.Contains(live.Position),"the real Brimhaven lock is on the dungeon map");
                }
            }
            var structure=new MinimapStructure();
            foreach(int id in ids)
            {
                ScanRegion source=regions.Single(r=>r.Id==id);
                foreach(int level in source.Levels)
                {
                    var region=new ScanRegion{Id=source.Id,Level=level,Tier=source.Tier,Name=source.Name,MapBounds=source.MapBounds,BaseRange=source.BaseRange,Spots=source.Spots,SpotLevels=source.SpotLevels};
                    string path=Path.Combine(assets,"scan",region.AssetKey+"-structure.png");
                    Check(File.Exists(path),region.AssetKey+" structure map exists");
                    int pale=0,filled=0,bestCount=-1;Point best=Point.Empty;int width,height;double sx,sy,worldX,worldY;
                    using(var image=new Bitmap(path))
                    {
                        width=image.Width;height=image.Height;var pixels=new Pixels(image);var mask=new bool[width*height];
                        for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(PaleInk(pixels.Get(x,y))){mask[y*width+x]=true;pale++;}
                        for(int y=1;y<height-1;y++)for(int x=1;x<width-1;x++){int i=y*width+x;if(mask[i]&&mask[i-1]&&mask[i+1]&&mask[i-width]&&mask[i+width])filled++;}
                        int rad=Math.Min(70,Math.Min(width,height)/2-2);
                        for(int y=rad;y<height-rad;y+=12)for(int x=rad;x<width-rad;x+=12)
                        {
                            int count=0;
                            for(int iy=-rad;iy<=rad;iy+=2)for(int ix=-rad;ix<=rad;ix+=2)if(ix*ix+iy*iy<rad*rad&&mask[(y+iy)*width+x+ix])count++;
                            if(count>bestCount){bestCount=count;best=new Point(x,y);}
                        }
                        Check(pale*1000>width*height*4,region.AssetKey+" has pale wall ink ("+pale+" px)");
                        Check(filled*2<pale,region.AssetKey+" pale ink stays on edges");
                        sx=width/region.MapBounds.Width;sy=height/region.MapBounds.Height;
                        worldX=region.MapBounds.Left+best.X/sx;worldY=region.MapBounds.Bottom-best.Y/sy;
                        using(var mini=new Bitmap(260,260))
                        {
                            for(int iy=0;iy<260;iy++)for(int ix=0;ix<260;ix++)
                            {
                                double wx=worldX+(ix-130)/2.0,wy=worldY-(iy-130)/2.0;
                                int rx=(int)Math.Round((wx-region.MapBounds.Left)*sx),ry=(int)Math.Round((region.MapBounds.Bottom-wy)*sy);
                                if(rx>=0&&ry>=0&&rx<width&&ry<height)mini.SetPixel(ix,iy,pixels.Get(rx,ry));
                            }
                            var watch=Stopwatch.StartNew();string info;MinimapMatch match=structure.Find(mini,assets,region,new Point(130,130),new Rectangle(0,0,260,260),null,CancellationToken.None,out info);
                            File.AppendAllText(report,region.AssetKey+" synthetic lock "+watch.ElapsedMilliseconds+" ms; "+info+"\r\n");
                            bool locked=match!=null&&match.Error<=3&&CompassMath.Distance(match.Position,new PointF((float)worldX,(float)worldY))<4;
                            Check(locked,region.AssetKey+" synthetic minimap locks at "+worldX.ToString("0")+", "+worldY.ToString("0")+" ("+info+")");
                        }
                    }
                }
            }
        }
    }
}
