// Compass geometry/calibration adapted from the MIT portions of Clue Trainer.
// Copyright (c) 2024 Lukas Gail. See THIRD_PARTY.txt.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;

namespace KavrosClueHelper
{
    public sealed class CompassReading
    {
        // Mathematical radians: east=0, north=pi/2.
        public double Angle, Error;
        public bool Solved;
        public int Pixels;
        public string Heading { get { return Solved ? "X detected - dig here" : CompassMath.Bearing(Angle).ToString("0.0",CultureInfo.InvariantCulture)+" degrees"; } }
    }
    public static class CompassMath
    {
        public static double Normalize(double a) { double v=a%(2*Math.PI);return v<0?v+2*Math.PI:v; }
        public static double Difference(double a,double b) { double d=Normalize(a-b);return d>Math.PI?d-2*Math.PI:d; }
        public static double Bearing(double a) { return Normalize(Math.PI/2-a)*180/Math.PI; }
        public static double Direction(PointF from,PointF to) { return Normalize(Math.Atan2(to.Y-from.Y,to.X-from.X)); }
        public static double Distance(PointF a,PointF b) { return Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y)); }
    }
    public sealed class CompassVision
    {
        public Action<string> Progress;
        private readonly Dictionary<float,ImagePattern> anchors=new Dictionary<float,ImagePattern>();
        private readonly Dictionary<float,Point[]> paperProbes=new Dictionary<float,Point[]>();
        private readonly double[][] off,msaa;
        private readonly double amplitude,phase,offset;
        private readonly float[] scales={0.8F,1F,8F/9F,1.25F,4F/3F,1.5F,2F};
        private int dpi=96;
        private float targetScale;
        public void UseDpi(int value){dpi=value<96?96:value;}
        public void UseTarget(float value){targetScale=value;}
        private IEnumerable<float> ScaleOrder()
        {
            float aim=targetScale>0.01F?targetScale:(dpi==96?0F:dpi/96F);
            if(aim<=0F)return scales;
            // The Auto guess stays first. Scale 1.0 is next, so a 100% client
            // is not stuck behind 1.25 and 1.5 on the full window.
            return scales.OrderBy(s=>ScaleRank(s,aim)).ThenBy(s=>Math.Abs(s-aim)).ThenBy(s=>s);
        }
        private static int ScaleRank(float scale,float aim)
        {
            if(Math.Abs(scale-aim)<0.02F)return 0;
            if(Math.Abs(scale-1F)<0.02F)return 1;
            return 2;
        }
        public CompassVision(string assets)
        {
            string dir=Path.Combine(assets,"compass");
            foreach(float scale in scales) using(Bitmap original=new Bitmap(Path.Combine(dir,"north.png")))
            using(Bitmap image=Pixels.Crop(original,new Rectangle(Point.Empty,original.Size),new Size((int)Math.Round(original.Width*scale),(int)Math.Round(original.Height*scale))))
            {
                anchors[scale]=new ImagePattern(image);var empty=new List<Point>();Pixels pixels=new Pixels(image);
                for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(pixels.Get(x,y).A<5)empty.Add(new Point(x,y));
                paperProbes[scale]=empty.Where((p,i)=>i%Math.Max(1,empty.Count/8)==0).Take(8).ToArray();
            }
            off=ReadTable(Path.Combine(dir,"calibration-off.csv")).Where(p=>p.Length>=4).ToArray();
            double[][] raw=ReadTable(Path.Combine(dir,"calibration-msaa.csv")).Where(p=>p.Length>=3).ToArray();
            if (off.Length==0 || raw.Length==0) throw new InvalidDataException("Compass calibration is empty.");
            msaa=raw.Select(p=>new[]{p[0],CompassMath.Difference(Math.Atan2(-p[2],-p[1]),p[0])}).OrderBy(p=>p[0]).ToArray();
            double max=msaa.Max(p=>p[1]),min=msaa.Min(p=>p[1]);amplitude=(max-min)/2;offset=(max+min)/2;
            double[] hills=msaa.Where(p=>Math.Abs(max-p[1])<amplitude/10).Select(p=>p[0]%(Math.PI/2)).ToArray();
            phase=Math.Atan2(hills.Sum(v=>Math.Sin(v)),hills.Sum(v=>Math.Cos(v)));
            foreach(double[] p in msaa) p[1]-=Baseline(p[0]);
        }
        private static double[][] ReadTable(string file)
        {
            var rows = new List<double[]>();
            foreach (string line in File.ReadAllLines(file))
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                string[] parts = line.Split(',');
                double[] row = new double[parts.Length];
                bool ok = parts.Length > 0;
                for (int i = 0; i < parts.Length && ok; i++) ok = Double.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out row[i]);
                if (ok) rows.Add(row);
            }
            return rows.ToArray();
        }
        private double Baseline(double a) { return amplitude*Math.Cos(4*(a-phase))+offset; }
        public CompassReading Calibrate(double raw,bool antialiasing)
        {
            raw=CompassMath.Normalize(raw);
            if(antialiasing)
            {
                int i=Array.FindLastIndex(msaa,p=>p[0]<=raw);if(i<0)i=msaa.Length-1;
                double[] a=msaa[i],b=msaa[(i+1)%msaa.Length];
                double t=CompassMath.Normalize(raw-a[0])/CompassMath.Normalize(b[0]-a[0]);
                return new CompassReading{Angle=CompassMath.Normalize(raw+a[1]*(1-t)+b[1]*t+Baseline(raw)),Error=Math.PI/360};
            }
            int lower=Array.FindLastIndex(off,p=>p[0]<=raw);if(lower<0)lower=off.Length-1;
            double[] below=off[lower],above=off[(lower+1)%off.Length];
            // Include neighboring bins, as the upstream calibration does.
            // Several reference rows can share a quantised reading. Retain all
            // of their true angles rather than arbitrarily choosing one row.
            int[] hits=Enumerable.Range(0,off.Length).Where(i=>raw>=off[i][0]-0.000001&&raw<=off[i][1]+0.000001).ToArray();
            if(hits.Length>0){below=off[(hits.First()-1+off.Length)%off.Length];above=off[(hits.Last()+1)%off.Length];}
            double from=below[3],size=CompassMath.Normalize(above[2]-from);
            return new CompassReading{Angle=CompassMath.Normalize(from+size/2),Error=Math.Max(Math.PI/720,size/2)};
        }
        public DetectedPuzzle Find(Bitmap image,Pixels pixels)
        {
            return Find(image,pixels,null);
        }
        public DetectedPuzzle Find(Bitmap image,Pixels pixels,Rectangle? clip)
        {
            Rectangle all=clip??new Rectangle(Point.Empty,image.Size);
            List<Rectangle> areas=clip.HasValue?new List<Rectangle>{all}:ScrollAreas.Find(pixels);
            foreach(float scale in (clip.HasValue?scales.OrderBy(s=>ScaleRank(s,1F)).ThenBy(s=>Math.Abs(s-1F)).ThenBy(s=>s):ScaleOrder())) foreach(Rectangle area in (clip.HasValue||scale==0.8F||scale==1F?new List<Rectangle>{all}:areas)) foreach(Point anchor in anchors[scale].Find(pixels,area,12,47,q=>NorthBackground(pixels,q,scale)))
            {
                Rectangle body=new Rectangle(anchor.X-(int)Math.Round(78*scale),anchor.Y-(int)Math.Round(20*scale),(int)Math.Round(176*scale),(int)Math.Round(259*scale));
                DetectedPuzzle best=null;
                // A resampled north glyph can match one physical pixel away
                // from the body origin. Refine that phase before reading ink.
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {
                    Rectangle shifted=body;shifted.Offset(dx,dy);
                    DetectedPuzzle result=Read(image,shifted,scale,pixels);
                    if(result!=null&&(best==null||result.Compass.Solved||result.Compass.Pixels>best.Compass.Pixels))best=result;
                }
                if(best!=null)return best;
            }
            return null;
        }
        private bool NorthBackground(Pixels pixels,Point anchor,float scale)
        {
            int misses=0;
            foreach(Point q in paperProbes[scale])
            {Color c=pixels.Get(anchor.X+q.X,anchor.Y+q.Y);if(!(c.R>110&&c.G>80&&c.R>c.G&&c.G>c.B)&&++misses>1)return false;}
            return true;
        }
        public DetectedPuzzle Read(Bitmap image,Rectangle body,float scale,Pixels supplied=null)
        {
            if(!anchors.ContainsKey(scale)) return null;
            Rectangle anchor=new Rectangle(body.X+(int)Math.Round(78*scale)-1,body.Y+(int)Math.Round(20*scale)-1,anchors[scale].Size.Width+3,anchors[scale].Size.Height+3);
            if(!new Rectangle(Point.Empty,image.Size).Contains(body))return null;
            Pixels full=supplied??new Pixels(image);
            if(anchors[scale].Find(full,anchor,1,47,q=>NorthBackground(full,q,scale)).Count==0) return null;
            using(Bitmap crop=Pixels.Crop(image,body,new Size(176,259)))
            {
                if(crop==null)return null;
                Pixels p=new Pixels(crop);
                int paper=0;
                foreach(Point q in new[]{new Point(23,67),new Point(149,67),new Point(20,210),new Point(149,210),new Point(89,45),new Point(89,230)})
                {Color c=p.Get(q.X,q.Y);if(c.R>85 && c.G>60 && c.R>c.G && c.G>c.B)paper++;}
                if(paper<3)return null;
                if(Progress!=null)Progress("Reading compass...");
                int arc=0;for(int x=34;x<146;x++)if(p.Error(x,235,Color.FromArgb(51,25,0))<38)arc++;
                if(arc>5)return null; // The Arc/master compass is outside this release.
                CompassReading reading=ReadArrow(p);
                if(reading==null)return null;
                if(!reading.Solved)reading.Error=Math.Max(reading.Error,Math.PI/180*(scale<1?1.0:0.5));
                return new DetectedPuzzle{Kind=PuzzleKind.Compass,Bounds=body,Grid=new Rectangle(body.X+(int)Math.Round(4*scale),body.Y+(int)Math.Round(52*scale),(int)Math.Round(171*scale),(int)Math.Round(171*scale)),Scale=scale,Compass=reading,Confidence=0.97};
            }
        }
        private CompassReading ReadArrow(Pixels p)
        {
            const int cx=89,cy=137,n=171;
            Point[] cross={new Point(-15,-15),new Point(-14,-14),new Point(-13,-13),new Point(-12,-12),new Point(12,12),new Point(11,11),new Point(10,10),new Point(9,9),new Point(13,-16),new Point(12,-15),new Point(11,-14),new Point(10,-13),new Point(-15,10),new Point(-14,9),new Point(-13,8),new Point(-12,7)};
            for(int oy=-9;oy<=9;oy++)for(int ox=-9;ox<=9;ox++)
            {
                int hits=0;
                foreach(Point q in cross)
                {
                    bool grey=false;
                    for(int sy=-1;sy<=1&&!grey;sy++)for(int sx=-1;sx<=1;sx++)
                    {Color c=p.Get(cx+ox+q.X+sx,cy+oy+q.Y+sy);if(c.R>=65&&c.R<165&&Math.Abs(c.R-c.G)<12&&Math.Abs(c.G-c.B)<12){grey=true;break;}}
                    if(grey)hits++;
                }
                if(hits>=15)
                {
                    int outside=0;
                    foreach(Point q in new[]{new Point(18,0),new Point(-18,0),new Point(0,18),new Point(0,-18)})
                    {Color c=p.Get(cx+ox+q.X,cy+oy+q.Y);if(!(c.R>=65&&c.R<165&&Math.Abs(c.R-c.G)<12&&Math.Abs(c.G-c.B)<12))outside++;}
                    if(outside>=3)return new CompassReading{Solved=true};
                }
            }
            var points=new List<Point>();var queue=new Queue<Point>();bool[] visited=new bool[n*n];bool aa=false;
            queue.Enqueue(new Point(85,85));
            while(queue.Count>0)
            {
                Point q=queue.Dequeue();if(q.X<0||q.Y<0||q.X>=n||q.Y>=n)continue;
                int index=q.Y*n+q.X;if(visited[index])continue;visited[index]=true;
                Color c=p.Get(cx-85+q.X,cy-85+q.Y);
                if(c.R>=5||c.G>=5||c.B>=5)
                {if(Math.Abs(q.X-85)<15&&Math.Abs(q.Y-85)<15&&c.R+c.G+c.B<250)aa=true;continue;}
                points.Add(q);
                for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)if(x!=0||y!=0)queue.Enqueue(new Point(q.X+x,q.Y+y));
            }
            if(points.Count<(aa?1650:2100)||points.Count>2350)return null;
            double mx=(points.Min(q=>q.X)+points.Max(q=>q.X))/2.0,my=(points.Min(q=>q.Y)+points.Max(q=>q.Y))/2.0;
            double dx=points.Average(q=>q.X)-mx,dy=points.Average(q=>q.Y)-my;
            if(dx*dx+dy*dy<0.3)return null;
            double initial=CompassMath.Normalize(Math.Atan2(-dy,dx)),sin=0,cos=0;
            foreach(Point q in points)
            {
                double x=q.X-mx,y=my-q.Y,w=x*x+y*y;if(w==0)continue;
                double angle=CompassMath.Normalize(Math.Atan2(y,x));
                if(Math.Abs(CompassMath.Difference(angle,initial))>Math.PI/2)angle=CompassMath.Normalize(angle-Math.PI);
                sin+=w*Math.Sin(angle);cos+=w*Math.Cos(angle);
            }
            CompassReading reading=Calibrate(Math.Atan2(sin,cos),aa);reading.Pixels=points.Count;return reading;
        }
    }
    public sealed class CompassPosition
    {
        public string Name;public float X,Y;public double Radius=1;
        public PointF Point {get{return new PointF(X,Y);}}
        public override string ToString(){return Name;}
    }
    public sealed class CompassBearing
    {
        public CompassPosition Position;public double Angle,Error;
    }
    public sealed class CompassSession
    {
        public readonly List<CompassBearing> Bearings=new List<CompassBearing>();
        public readonly List<PointF> AllSpots;
        public readonly List<CompassPosition> Positions;
        public List<PointF> Candidates {get;private set;}
        public CompassReading Current {get;private set;}
        public PointF? Intersection {get;private set;}
        private DateTime last;
        private const int Ring = 5;
        private const double OneDegree = Math.PI / 180;
        private const double MoveLimit = 2 * Math.PI / 180;
        private readonly double[] ringAngle = new double[Ring];
        private readonly double[] ringError = new double[Ring];
        private int ringCount;
        private bool outlier, haveStable;
        private double stableAngle, stableError;
        private DateTime stableAt;
        private List<CompassBearing> prior;
        private bool hidden;
        public bool HasPrevious { get { return prior != null; } }
        public bool HasHistory { get { return Bearings.Count != 0 || HasPrevious; } }

        private void ParkBearings()
        {
            if (Bearings.Count == 0 || HasPrevious) return;
            prior = new List<CompassBearing>(Bearings);
            Bearings.Clear();
            Recalculate();
        }

        private static bool SameBearing(CompassBearing a, CompassBearing b)
        {
            return CompassMath.Distance(a.Position.Point, b.Position.Point) < 0.1 &&
                Math.Abs(CompassMath.Difference(a.Angle, b.Angle)) < Math.PI / 360;
        }

        private void RestorePrevious()
        {
            var fresh = new List<CompassBearing>(Bearings);
            Bearings.Clear();
            Bearings.AddRange(prior);
            prior = null;
            foreach (var b in fresh)
                if (!Bearings.Any(old => SameBearing(old, b))) Bearings.Add(b);
            Recalculate();
        }

        public void ResumePrevious()
        {
            if (HasPrevious) RestorePrevious();
        }

        // Allow some extra error before contradicting an already triangulated
        // spot. A reading at or next to the target is directionally ambiguous.
        private static bool ClearlyMisses(PointF spot, CompassBearing b)
        {
            if (CompassMath.Distance(spot, b.Position.Point) < 12 + 3 * b.Position.Radius) return false;
            var relaxed = new CompassBearing {
                Position = new CompassPosition { X = b.Position.X, Y = b.Position.Y, Radius = b.Position.Radius + 5 },
                Angle = b.Angle, Error = b.Error + Math.PI / 36
            };
            return !Matches(spot, relaxed);
        }

        private PointF? previousTarget;

        private void AddReading(CompassBearing reading)
        {
            PointF? target = HasPrevious ? previousTarget : ConfirmedTarget();
            if (target.HasValue && ClearlyMisses(target.Value, reading))
            {
                // Even an uninterrupted capture can miss a very brief X.
                if (!HasPrevious) Bearings.Clear();
                prior = null;
                previousTarget = null;
                Generation++;
            }
            else if (!HasPrevious && Bearings.Any(b =>
                CompassMath.Distance(b.Position.Point, reading.Position.Point) < 0.1 &&
                Math.Abs(CompassMath.Difference(b.Angle, reading.Angle)) > Math.PI / 9))
            {
                previousTarget = target;
                ParkBearings();
            }
            Bearings.Add(reading);
            Recalculate();
            if (!HasPrevious) return;

            // A known target compatible with both sets lets normal teleport
            // triangulation resume on the player's next Record direction.
            bool compatible = Candidates.Any(p => prior.All(b => Matches(p, b)));
            bool independent = Bearings.Any(b => prior.Any(old => CompassMath.Distance(old.Position.Point, b.Position.Point) > 8 &&
                Math.Abs(Math.Sin(old.Angle - b.Angle)) > 0.12));
            // A single ray can point through two dig spots. Once an old session
            // had a crossing, only fresh triangulation (or Continue) restores it.
            if (compatible && (prior.Count == 1 && independent || ConfirmedTarget().HasValue)) RestorePrevious();
            else if (ConfirmedTarget().HasValue)
            {
                // Fresh independent lines now establish their own destination.
                prior = null;
                previousTarget = null;
                Generation++;
            }
        }

        private PointF? ConfirmedTarget()
        {
            if (Bearings.Count < 2 || Candidates.Count != 1 || !Intersection.HasValue) return null;
            PointF candidate = Candidates[0];
            return CompassMath.Distance(candidate, Intersection.Value) <= 12 ? (PointF?)candidate : null;
        }

        private bool wasSolved;
        public int Generation {get;private set;}
        public CompassSession(string assets)
        {
            string dir=Path.Combine(assets,"compass");
            AllSpots=ReadSpots(File.ReadAllLines(Path.Combine(dir,"elite-spots.csv")));
            Positions=ReadPositions(File.ReadAllLines(Path.Combine(dir,"positions.tsv")));
            Candidates=new List<PointF>(AllSpots);
        }
        internal static List<PointF> ReadSpots(IEnumerable<string> lines)
        {
            var spots = new List<PointF>();
            if (lines == null) return spots;
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                string[] p = line.Split(',');
                int x, y;
                if (p.Length < 2 || !Int32.TryParse(p[0].Trim(), out x) || !Int32.TryParse(p[1].Trim(), out y)) continue;
                spots.Add(new PointF(x, y));
            }
            return spots;
        }
        internal static List<CompassPosition> ReadPositions(IEnumerable<string> lines)
        {
            var list = new List<CompassPosition>();
            if (lines == null) return list;
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                string[] p = line.Split('\t');
                int x, y; double radius;
                if (p.Length < 4 || !Int32.TryParse(p[1].Trim(), out x) || !Int32.TryParse(p[2].Trim(), out y)) continue;
                if (!Double.TryParse(p[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out radius)) continue;
                list.Add(new CompassPosition { Name = LodestoneNames.Clean(p[0]), X = x, Y = y, Radius = radius });
            }
            return list;
        }
        public void Observe(CompassReading reading,DateTime now)
        {
            // A missed frame is not a new heading. One gap keeps the ring and the latch.
            if (reading == null) { hidden = true; return; }
            if (!reading.Solved && !wasSolved && hidden && (now-last).TotalMilliseconds >= 650 && Bearings.Count > 0 && !HasPrevious)
            {
                previousTarget = ConfirmedTarget();
                ParkBearings();
            }
            hidden = false;
            if(wasSolved&&!reading.Solved){NewClue();wasSolved=false;}
            Current=reading;last=now;
            if(reading.Solved){wasSolved=true;ringCount=0;outlier=false;haveStable=false;return;}
            wasSolved=false;
            if(ringCount>0)
            {
                double basis=haveStable?stableAngle:CircularMean();
                double delta=Math.Abs(CompassMath.Difference(basis,reading.Angle));
                // A move past about two degrees, and past this reading's own error, starts again.
                if(delta>MoveLimit&&delta>Tolerance(reading.Error)){ringCount=0;haveStable=false;outlier=false;}
            }
            Push(reading.Angle,reading.Error);
            double agreed;
            if(RingAgrees(out agreed)){haveStable=true;stableAngle=agreed;stableError=reading.Error;stableAt=now;outlier=false;}
            else if(ringCount>=3)
            {
                // One sample outside the window is dropped. A second one is the arrow turning.
                if(!outlier){ringCount--;outlier=true;}
                else
                {
                    double angle=reading.Angle,error=reading.Error;
                    ringCount=0;haveStable=false;outlier=false;Push(angle,error);
                }
            }
        }
        private static double Tolerance(double error){return Math.Max(OneDegree,2*error);}
        private double CircularMean()
        {
            double sin=0,cos=0;
            for(int i=0;i<ringCount;i++){sin+=Math.Sin(ringAngle[i]);cos+=Math.Cos(ringAngle[i]);}
            return CompassMath.Normalize(Math.Atan2(sin,cos));
        }
        private void Push(double angle,double error)
        {
            if(ringCount==Ring)
            {
                for(int i=1;i<Ring;i++){ringAngle[i-1]=ringAngle[i];ringError[i-1]=ringError[i];}
                ringCount=Ring-1;
            }
            ringAngle[ringCount]=angle;ringError[ringCount]=error;ringCount++;
        }
        private bool RingAgrees(out double mean)
        {
            mean=0;
            if(ringCount<3)return false;
            mean=CircularMean();
            for(int i=0;i<ringCount;i++)if(Math.Abs(CompassMath.Difference(ringAngle[i],mean))>Tolerance(ringError[i]))return false;
            return true;
        }
        public string DirectionText
        {
            get
            {
                if(Current!=null&&Current.Solved)return Current.Heading;
                if(ringCount<=0)return null;
                double angle=haveStable?stableAngle:CircularMean();
                return CompassMath.Bearing(angle).ToString("0.0",CultureInfo.InvariantCulture)+" degrees";
            }
        }
        public bool Stable(DateTime now){return haveStable&&(now-stableAt).TotalMilliseconds<=2000;}
        public bool CanRecord(CompassPosition position,DateTime now)
        {
            if(!Stable(now)||position==null||position.X<0||position.Y<0||position.X>6400||position.Y>12800||position.Radius<0||position.Radius>32)return false;
            if(Bearings.Any(b=>CompassMath.Distance(b.Position.Point,position.Point)<0.1&&Math.Abs(CompassMath.Difference(b.Angle,stableAngle))<Math.PI/360))return false;
            return true;
        }
        public bool Record(CompassPosition position,DateTime now)
        {
            if(!CanRecord(position,now))return false;
            AddReading(new CompassBearing{Position=new CompassPosition{Name=position.Name,X=position.X,Y=position.Y,Radius=position.Radius},Angle=stableAngle,Error=Math.Max(Math.PI/240,stableError+Math.PI/720)});
            return true;
        }
        public static bool Matches(PointF spot,CompassBearing b)
        {
            double distance=CompassMath.Distance(spot,b.Position.Point);if(distance<=b.Position.Radius+0.75)return true;
            double angle=CompassMath.Direction(b.Position.Point,spot),difference=Math.Abs(CompassMath.Difference(angle,b.Angle));
            return difference<Math.PI/2 && distance*Math.Sin(difference)<=distance*Math.Sin(b.Error)+b.Position.Radius+0.75;
        }
        private void Recalculate()
        {
            Candidates=AllSpots.Where(p=>Bearings.All(b=>Matches(p,b))).ToList();Intersection=null;
            if(Bearings.Count<2)return;
            double xx=0,xy=0,yy=0,bx=0,by=0;
            foreach(CompassBearing b in Bearings)
            {
                double nx=-Math.Sin(b.Angle),ny=Math.Cos(b.Angle),c=nx*b.Position.X+ny*b.Position.Y;
                xx+=nx*nx;xy+=nx*ny;yy+=ny*ny;bx+=nx*c;by+=ny*c;
            }
            double determinant=xx*yy-xy*xy;
            if(determinant<0.003*(xx+yy)*(xx+yy))return;
            PointF target=new PointF((float)((bx*yy-by*xy)/determinant),(float)((by*xx-bx*xy)/determinant));
            if(target.X<0||target.X>6400||target.Y<0||target.Y>12800)return;
            if(Bearings.All(b=>Matches(target,b)))Intersection=target;
        }
        public void Undo(){if(Bearings.Count>0)Bearings.RemoveAt(Bearings.Count-1);Recalculate();}
        public void NewClue(){Generation++;prior=null;previousTarget=null;hidden=false;Bearings.Clear();Current=null;ringCount=0;outlier=false;haveStable=false;wasSolved=false;Recalculate();}
        public string Summary
        {
            get
            {
                if(Current!=null&&Current.Solved)return "The compass shows X. Dig at your current position.";
                if (HasPrevious) return Bearings.Count == 0
                    ? "Compass reopened. Record a direction to check earlier lines, or select Continue if this is the same clue."
                    : "Earlier lines are on hold. Record from another lodestone to confirm this destination.";
                if(Bearings.Count==0)return "Select your actual location, stand still, then record the direction.";
                if(Bearings.Count>=2&&!Intersection.HasValue)return "The lines do not cross reliably. Take a third reading from a different lodestone.";
                if(Intersection.HasValue&&Candidates.Count!=1)return "Estimated crossing: "+Math.Round(Intersection.Value.X)+", "+Math.Round(Intersection.Value.Y)+". "+Candidates.Count+" standard spots match. Verify the compass X at the destination.";
                if(Candidates.Count==0)return "No standard spot matches. Check the recorded positions, or start a new clue.";
                if(Candidates.Count==1&&Bearings.Count>=2)return "One matching dig spot: "+Candidates[0].X+", "+Candidates[0].Y+". Travel there and verify X before digging.";
                return Candidates.Count+" possible dig spots. Take a reading from another known location.";
            }
        }
    }
}
