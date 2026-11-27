// Celtic layout interpretation and compatibility geometry adapted from
// Clue Trainer, Copyright (c) 2024 Lukas Gail (MIT). See THIRD_PARTY.txt.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace KavrosClueHelper
{
    public enum PuzzleKind { None, Lockbox, Towers, Sliding, Knot, EmoteClue, Compass, Scan }
    public sealed class DetectedPuzzle
    {
        public PuzzleKind Kind;
        public Rectangle Bounds, Grid;
        public float Scale;
        public int[] Board;
        public TowerPuzzle Towers;
        public List<int[]> TowerSolutions;
        public KnotPuzzle Knot;
        public MasterClue Master;
        public CompassReading Compass;
        public ScanReading Scan;
        public string Theme, Message;
        public double Confidence;
        // Bounds and Grid live in capture coordinates. Solver data are board-relative.
        public DetectedPuzzle OffsetBy(Point offset)
        {
            var translated = (DetectedPuzzle)MemberwiseClone();
            translated.Bounds.Offset(offset);
            translated.Grid.Offset(offset);
            return translated;
        }
        public string Key
        {
            get
            {
                if (Kind == PuzzleKind.Towers) return Towers == null || Towers.Top == null || Towers.Bottom == null || Towers.Left == null || Towers.Right == null ? "T:?" : "T:" + Towers.Key;
                if (Kind == PuzzleKind.EmoteClue) return Master == null ? "M:?" : "M:" + Master.Id;
                if (Kind == PuzzleKind.Compass) return Compass == null ? "C:?" : "C:"+Compass.Solved+":"+Math.Round(Compass.Angle*10000);
                if (Kind == PuzzleKind.Scan) return Scan == null || Scan.Region == null ? "S:?" : "S:"+Scan.Region.Id;
                if (Board != null) return Kind + ":" + String.Join(",", Board);
                if (Knot != null && Knot.Lanes != null) return "K:" + String.Join("/", Knot.Lanes.Select(l => l == null ? "?" : String.Join(",", l.Select(r => r == null ? "?" : (r.IsNot ? "!" : "") + r.Id))));
                return Kind.ToString();
            }
        }
    }

    public sealed class Pixels
    {
        public readonly int Width, Height;
        private readonly byte[] data;
        private int[] colourHead, colourNext, colourAt;
        public Pixels(Bitmap bitmap)
        {
            Width = bitmap.Width; Height = bitmap.Height;
            data = new byte[Math.Max(0, Width * Height * 4)];
            // Screen captures and asset PNGs are already 24-bit or 32-bit. Reading
            // those bytes avoids a second full-frame bitmap on every scan.
            if (Width > 0 && Height > 0 && !TryCopyLocked(bitmap)) CopyThroughBitmap(bitmap);
        }
        private bool TryCopyLocked(Bitmap bitmap)
        {
            PixelFormat format = bitmap.PixelFormat;
            bool argb = format == PixelFormat.Format32bppArgb;
            bool opaque32 = format == PixelFormat.Format32bppRgb;
            bool rgb24 = format == PixelFormat.Format24bppRgb;
            if (!argb && !opaque32 && !rgb24) return false;
            BitmapData bits = null;
            try
            {
                bits = bitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, format);
                if (bits.Stride <= 0) return false;
                if (rgb24) Expand24(bits);
                else Copy32(bits, opaque32);
                return true;
            }
            catch (Exception) { return false; }
            finally { if (bits != null) try { bitmap.UnlockBits(bits); } catch (Exception) { } }
        }
        private void Expand24(BitmapData bits)
        {
            int stride = bits.Stride;
            byte[] row = new byte[stride];
            for (int y = 0; y < Height; y++)
            {
                Marshal.Copy(IntPtr.Add(bits.Scan0, y * stride), row, 0, stride);
                int dst = y * Width * 4, src = 0;
                for (int x = 0; x < Width; x++)
                {
                    data[dst] = row[src]; data[dst + 1] = row[src + 1]; data[dst + 2] = row[src + 2]; data[dst + 3] = 255;
                    dst += 4; src += 3;
                }
            }
        }
        private void Copy32(BitmapData bits, bool forceOpaque)
        {
            int stride = bits.Stride;
            if (!forceOpaque && stride == Width * 4)
            {
                Marshal.Copy(bits.Scan0, data, 0, data.Length);
                return;
            }
            byte[] row = new byte[stride];
            for (int y = 0; y < Height; y++)
            {
                Marshal.Copy(IntPtr.Add(bits.Scan0, y * stride), row, 0, stride);
                Buffer.BlockCopy(row, 0, data, y * Width * 4, Width * 4);
                if (forceOpaque) for (int x = 0; x < Width; x++) data[y * Width * 4 + x * 4 + 3] = 255;
            }
        }
        private void CopyThroughBitmap(Bitmap bitmap)
        {
            using (Bitmap copy = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(copy)) g.DrawImage(bitmap, new Rectangle(0, 0, Width, Height), new Rectangle(0, 0, Width, Height), GraphicsUnit.Pixel);
                BitmapData bits = copy.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try { Copy32(bits, false); }
                finally { copy.UnlockBits(bits); }
            }
        }
        public Color Get(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return Color.Black;
            int i = (y * Width + x) * 4;
            return Color.FromArgb(data[i + 3], data[i + 2], data[i + 1], data[i]);
        }
        public bool KnotPaper(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return false;
            int i = (y * Width + x) * 4;
            int b = data[i], g = data[i + 1], r = data[i + 2];
            return r >= 138 && r <= 190 && g >= 125 && g <= 175 && b >= 68 && b <= 115 && r > g && g > b && r - g >= 6 && r - g <= 32 && r - b >= 40 && r - b <= 95;
        }
        public bool KnotDark(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return false;
            int i = (y * Width + x) * 4;
            int b = data[i], g = data[i + 1], r = data[i + 2];
            int high = r > g ? r : g;
            if (b > high) high = b;
            return high < 80;
        }
        public bool KnotInk(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height || KnotPaper(x, y)) return false;
            int i = (y * Width + x) * 4;
            int b = data[i], g = data[i + 1], r = data[i + 2];
            int high = r > g ? r : g;
            if (b > high) high = b;
            int low = r < g ? r : g;
            if (b < low) low = b;
            return high - low >= 18 && high >= 40;
        }
        public int Error(int x, int y, Color b)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return 765;
            int i = (y * Width + x) * 4;
            return Math.Abs(data[i + 2] - b.R) + Math.Abs(data[i + 1] - b.G) + Math.Abs(data[i] - b.B);
        }
        public static bool TowerCell(Color c)
        {
            // Modern cells are dark slate, with blue at least as strong as red.
            if (c.R < 80 && c.G < 100 && c.B < 110 && c.G >= c.R && c.B >= c.R) return true;
            // Legacy cells are dark brown stone. Red leads and blue stays behind it.
            if (c.R >= 45 && c.R <= 100 && c.G >= 35 && c.G <= 90 && c.B >= 18 && c.B <= 75 && c.R >= c.G - 4 && c.G + 8 >= c.B && c.R - c.B >= 12 && c.R - c.B <= 45) return true;
            // DarkScape cells are a near-black warm brown, around RGB(26, 15, 9).
            return c.R >= 10 && c.R <= 50 && c.G >= 5 && c.G <= 40 && c.B <= 30 && c.R > c.G && c.G >= c.B && c.R - c.B >= 8;
        }
        public bool GridPixel(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return false;
            int i = (y * Width + x) * 4, b = data[i], g = data[i+1], r = data[i+2];
            int low = Math.Min(r, Math.Min(g,b)), high = Math.Max(r, Math.Max(g,b));
            // DarkScape's top border is a little dimmer than the other grid lines.
            return low >= 100 && high - low <= 75;
        }
        public ulong Hash(Rectangle region) { return Hash(region, null); }
        public ulong Hash(Rectangle region, GuideInk ink)
        {
            region = Rectangle.Intersect(region, new Rectangle(0,0,Width,Height));
            ulong h = 1469598103934665603UL;
            unchecked
            {
                for (int y = region.Top; y < region.Bottom; y++)
                    for (int x = region.Left; x < region.Right; x++)
                    {
                        if (ink != null && ink.Ignores(x, y)) continue;
                        int i=(y*Width+x)*4; h=(h^data[i])*1099511628211UL; h=(h^data[i+1])*1099511628211UL; h=(h^data[i+2])*1099511628211UL;
                    }
            }
            return h;
        }
        public IEnumerable<Point> AnchorCandidates(Rectangle origins, Point anchor, Color colour, int tolerance)
        {
            if(origins.Width*origins.Height<160000)
            {
                for(int y=origins.Top;y<origins.Bottom;y++)
                { if((y&15)==0) SearchCancellation.Check(); for(int x=origins.Left;x<origins.Right;x++) if(Error(x+anchor.X,y+anchor.Y,colour)<=tolerance) yield return new Point(x,y); }
                yield break;
            }
            if(colourHead==null)
            {
                // One array per channel bucket, filled in scanline order, so a full-window
                // search does not allocate a list for every occupied colour.
                colourHead=new int[4096]; colourNext=new int[Width*Height]; colourAt=new int[Width*Height];
                int[] tail=new int[4096];
                for(int i=0;i<4096;i++) colourHead[i]=tail[i]=-1;
                int count=0;
                for(int y=0;y<Height;y++)
                {
                    if((y&31)==0) SearchCancellation.Check();
                    for(int x=0;x<Width;x++)
                    {
                        int p=(y*Width+x)*4,key=((data[p+2]>>4)<<8)|((data[p+1]>>4)<<4)|(data[p]>>4),id=count++;
                        colourAt[id]=p; colourNext[id]=-1;
                        if(colourHead[key]<0) colourHead[key]=id; else colourNext[tail[key]]=id;
                        tail[key]=id;
                    }
                }
            }
            int visited=0;
            for(int r=Math.Max(0,colour.R-tolerance)>>4;r<=Math.Min(255,colour.R+tolerance)>>4;r++)
            for(int g=Math.Max(0,colour.G-tolerance)>>4;g<=Math.Min(255,colour.G+tolerance)>>4;g++)
            for(int b=Math.Max(0,colour.B-tolerance)>>4;b<=Math.Min(255,colour.B+tolerance)>>4;b++)
            {
                for(int id=colourHead[(r<<8)|(g<<4)|b]; id>=0; id=colourNext[id])
                {
                    int p=colourAt[id];
                    if((visited++&4095)==0) SearchCancellation.Check();
                    if(Math.Abs(data[p+2]-colour.R)+Math.Abs(data[p+1]-colour.G)+Math.Abs(data[p]-colour.B)>tolerance) continue;
                    int pixel=p/4,x=pixel%Width-anchor.X,y=pixel/Width-anchor.Y;
                    if(x>=origins.Left && y>=origins.Top && x<origins.Right && y<origins.Bottom) yield return new Point(x,y);
                }
            }
        }
        public static Bitmap Crop(Bitmap image, Rectangle region, Size size)
        {
            if (!new Rectangle(Point.Empty, image.Size).Contains(region) || region.Width <= 0 || region.Height <= 0) return null;
            Bitmap result = new Bitmap(size.Width, size.Height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = region.Size == size ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBilinear;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(image, new Rectangle(Point.Empty, size), region, GraphicsUnit.Pixel);
            }
            return result;
        }
        // Blocky scale for the legacy interface. Bilinear copies smear the
        // pixel-art corners and the close button at 125%, 150% and 200%.
        public static Bitmap ScaleNearest(Bitmap image, float scale)
        {
            int width = Math.Max(1, (int)Math.Round(image.Width * scale));
            int height = Math.Max(1, (int)Math.Round(image.Height * scale));
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(image, new Rectangle(Point.Empty, result.Size), new Rectangle(Point.Empty, image.Size), GraphicsUnit.Pixel);
            }
            return result;
        }
        public static double ColourSimilarity(Color a, Color b)
        { return (Math.Max(0, 1 - Math.Abs(a.R - b.R) / 128.0) + Math.Max(0, 1 - Math.Abs(a.G - b.G) / 128.0) + Math.Max(0, 1 - Math.Abs(a.B - b.B) / 128.0)) / 3; }
        public double[] Fingerprint(Rectangle rectangle, int columns, int rows, bool hsl)
        {
            double[] result = new double[columns * rows * 3];
            for (int row = 0; row < rows; row++) for (int column = 0; column < columns; column++)
            {
                int left = rectangle.X + column * rectangle.Width / columns, right = rectangle.X + (column + 1) * rectangle.Width / columns;
                int top = rectangle.Y + row * rectangle.Height / rows, bottom = rectangle.Y + (row + 1) * rectangle.Height / rows;
                double r = 0, g = 0, b = 0, n = 0;
                for (int y = top; y < bottom; y++) for (int x = left; x < right; x++) { Color p = Get(x, y); r += p.R; g += p.G; b += p.B; n++; }
                r /= Math.Max(n, 1); g /= Math.Max(n, 1); b /= Math.Max(n, 1);
                int i = (row * columns + column) * 3;
                if (!hsl) { result[i] = r; result[i + 1] = g; result[i + 2] = b; }
                else
                {
                    r /= 256; g /= 256; b /= 256;
                    double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), range = max - min, hue = 0;
                    if (range > 0) { if (max == r) hue = (6 + (g - b) / range) % 6; else if (max == g) hue = (b - r) / range + 2; else hue = (r - g) / range + 4; }
                    result[i] = Math.Round(hue / 6 * 255); result[i + 1] = Math.Round(range * 255); result[i + 2] = Math.Round((max + min) * 127.5);
                }
            }
            return result;
        }
        public static double Similarity(double[] a, double[] b, bool hsl)
        {
            if (a.Length != b.Length) return 0;
            double score = 0;
            for (int i = 0; i < a.Length; i += 3)
            {
                if (hsl)
                {
                    double hue = Math.Abs(a[i] - b[i]); if (hue > 128) hue = 255 - hue;
                    score += Math.Max(0, 1 - hue / 128) * Math.Max(0, 1 - Math.Abs(a[i + 1] - b[i + 1]) / 255) * Math.Max(0, 1 - Math.Abs(a[i + 2] - b[i + 2]) / 255);
                }
                else score += (Math.Max(0, 1 - Math.Abs(a[i] - b[i]) / 128) + Math.Max(0, 1 - Math.Abs(a[i + 1] - b[i + 1]) / 128) + Math.Max(0, 1 - Math.Abs(a[i + 2] - b[i + 2]) / 128)) / 3;
            }
            double mean = score / (a.Length / 3); return hsl ? Math.Pow(mean, 3) : mean;
        }
    }

    internal sealed class PatternMatches
    {
        public readonly List<KeyValuePair<Point,int>> Items=new List<KeyValuePair<Point,int>>();
        private readonly Dictionary<long,List<int>> buckets=new Dictionary<long,List<int>>();
        private readonly int columns;
        public PatternMatches(int width){columns=Math.Max(1,(width+4)/5);}
        private long Key(Point p){return (long)(p.Y/5)*columns+p.X/5;}
        private void Insert(long key,int index)
        {
            List<int> values;if(!buckets.TryGetValue(key,out values))buckets[key]=values=new List<int>();values.Add(index);
        }
        public void Add(Point point,int error)
        {
            int nearby=-1,cx=point.X/5,cy=point.Y/5;
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
            {
                List<int> values;if(!buckets.TryGetValue((long)(cy+dy)*columns+cx+dx,out values))continue;
                foreach(int index in values)
                {
                    Point existing=Items[index].Key;
                    if(Math.Abs(existing.X-point.X)<5&&Math.Abs(existing.Y-point.Y)<5&&(nearby<0||index<nearby))nearby=index;
                }
            }
            if(nearby<0){Insert(Key(point),Items.Count);Items.Add(new KeyValuePair<Point,int>(point,error));}
            else if(error<Items[nearby].Value)
            {
                long before=Key(Items[nearby].Key),after=Key(point);
                if(before!=after){buckets[before].Remove(nearby);Insert(after,nearby);}
                Items[nearby]=new KeyValuePair<Point,int>(point,error);
            }
        }
    }

    public sealed class ImagePattern
    {
        private readonly Point[] points;
        private readonly Color[] colours;
        public readonly Size Size;
        public ImagePattern(Bitmap bitmap)
        {
            Size = bitmap.Size;
            List<Point> positions = new List<Point>();
            Pixels pixels = new Pixels(bitmap);
            int step = bitmap.Width * bitmap.Height <= 225 ? 1 : 2;
            for (int y = 0; y < bitmap.Height; y += step) for (int x = 0; x < bitmap.Width; x += step)
                if (pixels.Get(x, y).A > 240) positions.Add(new Point(x, y));
            if (positions.Count < 4)
                for (int y = 0; y < bitmap.Height; y++) for (int x = 0; x < bitmap.Width; x++)
                    if (pixels.Get(x, y).A > 150 && !positions.Contains(new Point(x, y))) positions.Add(new Point(x, y));
            points = positions.OrderByDescending(p => { Color c = pixels.Get(p.X, p.Y); return Math.Max(c.R, Math.Max(c.G, c.B)) - Math.Min(c.R, Math.Min(c.G, c.B)); }).Take(64).ToArray();
            colours = points.Select(p => pixels.Get(p.X, p.Y)).ToArray();
        }
        public List<Point> Find(Pixels image, Rectangle region, int maximum, int tolerance = 47, Func<Point,bool> candidateGuard = null)
        {
            PatternMatches matches = new PatternMatches(image.Width);
            if (points.Length < 2) return new List<Point>();
            int right = Math.Min(region.Right, image.Width) - Size.Width, bottom = Math.Min(region.Bottom, image.Height) - Size.Height;
            int left=Math.Max(0,region.Left),top=Math.Max(0,region.Top);
            if(right<left || bottom<top) return new List<Point>();
            int quickTolerance = tolerance == 47 ? 70 : tolerance * 2;
            foreach(Point candidate in image.AnchorCandidates(Rectangle.FromLTRB(left,top,right+1,bottom+1),points[0],colours[0],quickTolerance))
            {
                int x=candidate.X,y=candidate.Y;
                if(candidateGuard!=null&&!candidateGuard(candidate))continue;
                if (image.Error(x + points[points.Length / 2].X, y + points[points.Length / 2].Y, colours[points.Length / 2]) > quickTolerance) continue;
                int error = 0;
                for (int i = 0; i < points.Length; i++) { error += image.Error(x + points[i].X, y + points[i].Y, colours[i]); if (error > points.Length * tolerance) break; }
                if (error <= points.Length * tolerance)
                {
                    matches.Add(new Point(x,y),error);
                }
            }
            return matches.Items.OrderBy(m => m.Value).Take(maximum).Select(m => m.Key).ToList();
        }
    }

    public sealed class DigitReader
    {
        private sealed class Glyph { public int Value, Width, MinY, MaxY, TopOffset; public int[] Data; public double Expected, Energy; }
        private readonly List<Glyph> glyphs;
        public DigitReader(string assetDirectory)
        {
            glyphs = new List<Glyph>();
            foreach (string filename in new[] { "legacy-digits.txt", "digits.txt" })
            {
                string path = Path.Combine(assetDirectory, filename);
                if (!File.Exists(path)) continue;
                foreach (string line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                    string[] parts = line.Split(new[] { '|' });
                    int value, width;
                    if (parts.Length < 3 || !Int32.TryParse(parts[0].Trim(), out value) || !Int32.TryParse(parts[1].Trim(), out width)) continue;
                    string[] raw = parts[2].Split(new[] { ',' });
                    int[] data = new int[raw.Length];
                    bool parsed = raw.Length > 0;
                    for (int n = 0; n < raw.Length && parsed; n++) parsed = Int32.TryParse(raw[n].Trim(), out data[n]);
                    if (!parsed) continue;
                    int[] ys = data.Where((v,i) => i % 3 == 1).ToArray();
                    if (ys.Length == 0) continue;
                    glyphs.Add(new Glyph { Value = value, Width = width, Data = data, MinY = ys.Min(), MaxY = ys.Max(), TopOffset = filename.StartsWith("legacy") ? 6 : 8, Expected = data.Where((v,i) => i % 3 == 2).Sum() / 255.0, Energy=data.Where((v,i)=>i%3==2).Sum(v=>v/255.0*(v/255.0)) });
                }
            }
        }
        public int Read(Pixels image, Point centre, bool clue, double minimum = 0.68)
        {
            double best = 0; int value = 0;
            // Glyph data uses top-of-font coordinates. Tower numbers are 10px
            // high inside a 17px line. Try small shifts around the cell centre.
            for(int style=0;style<(clue?2:1);style++)
            {
            // Reuse the same sampled pixels across every glyph and alignment.
            const int width=32, stride=33;
            double[] field=new double[width*width],sum=new double[stride*stride],squares=new double[stride*stride];
            for(int y=0;y<width;y++) for(int x=0;x<width;x++)
            {
                double intensity=Intensity(image.Get(centre.X-16+x,centre.Y-16+y),clue?style:2);
                field[y*width+x]=intensity;
                int index=(y+1)*stride+x+1;
                sum[index]=intensity+sum[index-1]+sum[index-stride]-sum[index-stride-1];
                squares[index]=intensity*intensity+squares[index-1]+squares[index-stride]-squares[index-stride-1];
            }
            for (int dy = -3; dy <= 3; dy++) for (int dx = -3; dx <= 3; dx++) foreach (Glyph glyph in glyphs)
            {
                int left = 16 - glyph.Width / 2 + dx, top = 16 - glyph.TopOffset + dy;
                int a=(top+glyph.MinY)*stride+left,b=a+glyph.Width,c=(top+glyph.MaxY+1)*stride+left,d=c+glyph.Width;
                double hit = 0, bright = sum[d]-sum[b]-sum[c]+sum[a], energy = Math.Max(0,squares[d]-squares[b]-squares[c]+squares[a]);
                for (int i = 0; i < glyph.Data.Length; i += 3)
                {
                    double light = field[(top+glyph.Data[i+1])*width+left+glyph.Data[i]];
                    hit += Math.Min(1, light) * glyph.Data[i + 2] / 255.0;
                }
                double dice=2*hit/Math.Max(1,glyph.Expected+bright);
                double cosine=energy>0 ? hit/Math.Sqrt(glyph.Energy*energy) : 0;
                double score=0.7*cosine+0.3*dice;
                if (score > best) { best = score; value = glyph.Value; }
            }
            if(best>=minimum) return value;
            }
            return best >= minimum ? value : 0;
        }
        private static double Intensity(Color p,int style)
        {
            int low=Math.Min(p.R,Math.Min(p.G,p.B)),high=Math.Max(p.R,Math.Max(p.G,p.B));
            if(style==0) return p.R>80 && p.G>60 && p.R>p.G*1.05 && p.G>p.B*1.15 ? Math.Min(1,Math.Min(p.R/255.0,p.G/205.0)) : 0;
            if(style==1) return low>=50 && high<=175 && high-low<=16 ? Math.Min(1,low/102.0) : 0;
            return low>90 && high-low<65 ? low/255.0 : 0;
        }
    }

    public sealed class SliderImageReader
    {
        private sealed class Reference { public string Name; public double[][] Tiles,FullTiles; }
        private readonly List<Reference> references = new List<Reference>();
        private readonly List<double[]> blanks = new List<double[]>(),fullBlanks=new List<double[]>();
        // The upper-left four-by-four bins are reserved for guidance badges.
        // Both references and captures use the remaining pixels, so the overlay
        // can stay visible even where capture exclusion is unavailable.
        private static double[] TileFingerprint(Pixels pixels,Rectangle tile)
        {return pixels.Fingerprint(tile,7,7,true).Where((v,i)=>i/3%7>=4||i/3/7>=4).ToArray();}
        public SliderImageReader(string assetDirectory)
        {
            string folder = Path.Combine(assetDirectory, "data", "sliders", "sliders");
            foreach (string file in Directory.GetFiles(folder, "*.png"))
            {
                using (Bitmap image = new Bitmap(file))
                {
                    Pixels p = new Pixels(image);
                    if (Path.GetFileNameWithoutExtension(file) == "blanktiles")
                    { for (int x = 0; x + 49 <= image.Width; x += 49) {blanks.Add(TileFingerprint(p,new Rectangle(x, 0, 49, 49)));fullBlanks.Add(p.Fingerprint(new Rectangle(x,0,49,49),7,7,true));}; continue; }
                    if (image.Width != 245 || image.Height != 245) continue;
                    references.Add(new Reference { Name = Path.GetFileNameWithoutExtension(file), Tiles = Enumerable.Range(0, 25).Select(i => TileFingerprint(p,new Rectangle(i % 5 * 49, i / 5 * 49, 49, 49))).ToArray(),FullTiles=Enumerable.Range(0,25).Select(i=>p.Fingerprint(new Rectangle(i%5*49,i/5*49,49,49),7,7,true)).ToArray() });
                }
            }
        }
        public DetectedPuzzle Read(Bitmap source,Rectangle region,float scale)
        {
            return Read(source, region, scale, -1, -1);
        }
        // badgeTile is the cell the guide is numbering. arrowTile is the blank
        // that cell's arrow points at. Passing them replaces the colour guess,
        // which treated the guide's own brown cover as a badge and then kept
        // the previous board.
        public DetectedPuzzle Read(Bitmap source,Rectangle region,float scale,int badgeTile,int arrowTile,GuideInk ink=null)
        {
            if(ink!=null)
            {
                DetectedPuzzle masked=ReadCore(source,region,scale,false,-1,-1,ink);
                if(masked!=null)return masked;
            }
            if(badgeTile>=0)
            {
                DetectedPuzzle guided=ReadCore(source,region,scale,false,badgeTile,arrowTile,null);
                if(guided!=null)return guided;
            }
            bool marked=false;
            using(Bitmap normal=Pixels.Crop(source,region,new Size(273,273)))
            {
                if(normal==null)return null;var pixels=new Pixels(normal);
                for(int tile=0;tile<25&&!marked;tile++)
                {
                    int colour=0,dark=0;for(int y=0;y<26;y++)for(int x=0;x<26;x++)
                    {Color c=pixels.Get(tile%5*56+x,tile/5*56+y);if(c.R<10&&c.G<10&&c.B<10)dark++;if(c.G>240&&c.R<15&&(c.B<15||c.B>240)||c.R>240&&c.G>140&&c.G<230&&c.B<15||Math.Abs(c.R-147)<15&&Math.Abs(c.G-112)<15&&Math.Abs(c.B-219)<15)colour++;}
                    marked=colour>=3&&dark>=30;
                }
            }
            DetectedPuzzle result=ReadCore(source,region,scale,marked,-1,-1,null);
            if(marked&&(result==null||!SlidingSolver.IsSolvable(result.Board)))return ReadCore(source,region,scale,false,-1,-1,null)??result;
            return result;
        }
        private static string rememberedTheme;
        private static int[] rememberedBoard;
        private static int[] RememberBoard(string theme, int[] read, bool safe)
        {
            if (read == null) return null;
            if (rememberedBoard != null && rememberedTheme == theme)
            {
                int diffs = 0, p = -1, q = -1;
                for (int i = 0; i < 25; i++) if (rememberedBoard[i] != read[i]) { diffs++; if (p < 0) p = i; else q = i; }
                // A real slide swaps the blank with one tile. Two picture tiles
                // swapping is a misread of a near-identical pair, so keep the
                // previous board. A blank swap is the move the player just made.
                if (diffs == 2 && rememberedBoard[p] != 24 && rememberedBoard[q] != 24 && rememberedBoard[p] == read[q] && rememberedBoard[q] == read[p]) return (int[])rememberedBoard.Clone();
            }
            if (!safe || rememberedBoard == null) { rememberedTheme = theme; rememberedBoard = (int[])read.Clone(); }
            return read;
        }
        private static bool[] GuideSkip(bool ring)
        {
            bool[] skip = new bool[49];
            for (int row = 0; row < 7; row++) for (int col = 0; col < 7; col++)
                skip[row * 7 + col] = (col < 4 && row < 4) || (ring && (row == 0 || col == 0 || row == 6 || col == 6));
            return skip;
        }
        private static double SimilaritySkip(double[] a, double[] b, bool[] skip)
        {
            if (a == null || b == null || a.Length != b.Length || skip == null) return 0;
            double score = 0; int n = 0;
            for (int bin = 0; bin < skip.Length && bin * 3 + 2 < a.Length; bin++)
            {
                if (skip[bin]) continue;
                int i = bin * 3;
                double hue = Math.Abs(a[i] - b[i]); if (hue > 128) hue = 255 - hue;
                score += Math.Max(0, 1 - hue / 128) * Math.Max(0, 1 - Math.Abs(a[i + 1] - b[i + 1]) / 255) * Math.Max(0, 1 - Math.Abs(a[i + 2] - b[i + 2]) / 255);
                n++;
            }
            return n == 0 ? 0 : Math.Pow(score / n, 3);
        }
        static bool Covered(GuideInk ink, Rectangle region, int sampleWidth, int sampleHeight, int x, int y)
        {
            int sx0 = region.X + x * region.Width / sampleWidth, sy0 = region.Y + y * region.Height / sampleHeight;
            int sx1 = region.X + (x + 1) * region.Width / sampleWidth, sy1 = region.Y + (y + 1) * region.Height / sampleHeight;
            if (sx1 <= sx0) sx1 = sx0 + 1;
            if (sy1 <= sy0) sy1 = sy0 + 1;
            for (int sy = sy0; sy < sy1; sy++) for (int sx = sx0; sx < sx1; sx++) if (ink.Ignores(sx, sy)) return true;
            return false;
        }
        static double[] MaskedFingerprint(Pixels crop, Rectangle cell, GuideInk ink, Rectangle region, out bool[] empty)
        {
            const int columns = 7, rows = 7;
            double[] result = new double[columns * rows * 3];
            empty = new bool[columns * rows];
            for (int row = 0; row < rows; row++) for (int column = 0; column < columns; column++)
            {
                int left = cell.X + column * cell.Width / columns, right = cell.X + (column + 1) * cell.Width / columns;
                int top = cell.Y + row * cell.Height / rows, bottom = cell.Y + (row + 1) * cell.Height / rows;
                double r = 0, g = 0, b = 0, n = 0;
                for (int y = top; y < bottom; y++) for (int x = left; x < right; x++)
                {
                    if (Covered(ink, region, crop.Width, crop.Height, x, y)) continue;
                    Color sample = crop.Get(x, y); r += sample.R; g += sample.G; b += sample.B; n++;
                }
                int bin = row * columns + column;
                if (n < 1) { empty[bin] = true; continue; }
                r /= n; g /= n; b /= n;
                r /= 256; g /= 256; b /= 256;
                double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), range = max - min, hue = 0;
                if (range > 0) { if (max == r) hue = (6 + (g - b) / range) % 6; else if (max == g) hue = (b - r) / range + 2; else hue = (r - g) / range + 4; }
                int i = bin * 3;
                result[i] = Math.Round(hue / 6 * 255); result[i + 1] = Math.Round(range * 255); result[i + 2] = Math.Round((max + min) * 127.5);
            }
            return result;
        }
        private DetectedPuzzle ReadCore(Bitmap source, Rectangle region, float scale,bool safe,int badgeTile,int arrowTile,GuideInk ink)
        {
            using (Bitmap crop = Pixels.Crop(source, region, new Size(273, 273)))
            {
                if (crop == null) return null;
                Pixels p = new Pixels(crop);
                bool guided = badgeTile >= 0 && ink == null;
                bool masked = ink != null;
                bool[] quiet = guided ? GuideSkip(false) : null;
                bool[] ring = guided ? GuideSkip(true) : null;
                double[][] tiles = new double[25][];
                bool[][] skips = (guided || masked) ? new bool[25][] : null;
                for (int i = 0; i < 25; i++)
                {
                    Rectangle cell = new Rectangle(i % 5 * 56, i / 5 * 56, 49, 49);
                    if (masked) { bool[] empty; tiles[i] = MaskedFingerprint(p, cell, ink, region, out empty); skips[i] = empty; }
                    else if (!guided) tiles[i] = safe ? TileFingerprint(p, cell) : p.Fingerprint(cell, 7, 7, true);
                    else { tiles[i] = p.Fingerprint(cell, 7, 7, true); skips[i] = (i == badgeTile || i == arrowTile) ? ring : quiet; }
                }
                double best = 0; int[] bestMap = null; string theme = null; double minScore = 0; double[,] bestScores = null;
                foreach (Reference reference in references)
                {
                    double[,] scores = new double[25, 25];
                    for (int i = 0; i < 25; i++) for (int j = 0; j < 25; j++)
                    {
                        double[][] bank = safe ? reference.Tiles : reference.FullTiles;
                        List<double[]> blankBank = safe ? blanks : fullBlanks;
                        if (guided || masked)
                        {
                            double tileScore = SimilaritySkip(tiles[i], reference.FullTiles[j], skips[i]);
                            double blankScore = j == 24 ? fullBlanks.Select(b => SimilaritySkip(tiles[i], b, skips[i])).DefaultIfEmpty(0).Max() : 0;
                            scores[i, j] = j == 24 ? Math.Max(tileScore, blankScore) : tileScore;
                        }
                        else scores[i, j] = j == 24 ? Math.Max(Pixels.Similarity(tiles[i], bank[j], true), blankBank.Select(b => Pixels.Similarity(tiles[i], b, true)).DefaultIfEmpty(0).Max()) : Pixels.Similarity(tiles[i], bank[j], true);
                    }
                    int[] assignment = Assign(scores);
                    double score = Enumerable.Range(0, 25).Average(i => scores[i, assignment[i]]);
                    if (score > best) { best = score; bestMap = assignment; theme = reference.Name; minScore = Enumerable.Range(0, 25).Min(i => scores[i, assignment[i]]); bestScores = scores; }
                }
                if (best < 0.73 || minScore < 0.40) return null;
                // Some pictures contain near-identical tiles (tree 10/14 and castle 5/9
                // differ by ~1-2% of pixels). A single swap of such a pair gives an
                // impossible parity. Real boards are always solvable, so repair it by
                // swapping the pair whose exchange costs the least similarity.
                if (!SlidingSolver.IsSolvable(bestMap))
                {
                    double cheapest = Double.MaxValue; int a = -1, b = -1;
                    for (int i = 0; i < 25; i++) for (int k = i + 1; k < 25; k++)
                    {
                        double loss = bestScores[i, bestMap[i]] + bestScores[k, bestMap[k]] - bestScores[i, bestMap[k]] - bestScores[k, bestMap[i]];
                        if (loss < cheapest) { cheapest = loss; a = i; b = k; }
                    }
                    if (cheapest < 0.05) { int swap = bestMap[a]; bestMap[a] = bestMap[b]; bestMap[b] = swap; }
                }
                if (guided || masked) { rememberedTheme = theme; rememberedBoard = (int[])bestMap.Clone(); }
                else bestMap = RememberBoard(theme, bestMap, safe);
                return new DetectedPuzzle { Kind = PuzzleKind.Sliding, Bounds = region, Grid = region, Scale = scale, Board = bestMap, Theme = theme, Confidence = best };
            }
        }
        // Hungarian assignment enforces a one-to-one map of the 25 tiles.
        private static int[] Assign(double[,] scores)
        {
            int n = 25; double[] u = new double[n + 1], v = new double[n + 1]; int[] p = new int[n + 1], way = new int[n + 1];
            for (int i = 1; i <= n; i++)
            {
                p[0] = i; int j0 = 0; double[] min = Enumerable.Repeat(Double.PositiveInfinity, n + 1).ToArray(); bool[] used = new bool[n + 1];
                do
                {
                    used[j0] = true; int i0 = p[j0], j1 = 0; double delta = Double.PositiveInfinity;
                    for (int j = 1; j <= n; j++) if (!used[j]) { double cur = 1 - scores[i0 - 1, j - 1] - u[i0] - v[j]; if (cur < min[j]) { min[j] = cur; way[j] = j0; } if (min[j] < delta) { delta = min[j]; j1 = j; } }
                    for (int j = 0; j <= n; j++) if (used[j]) { u[p[j]] += delta; v[j] -= delta; } else min[j] -= delta;
                    j0 = j1;
                } while (p[j0] != 0);
                do { int j1 = way[j0]; p[j0] = p[j1]; j0 = j1; } while (j0 != 0);
            }
            int[] assignment = new int[n]; for (int j = 1; j <= n; j++) assignment[p[j] - 1] = j - 1; return assignment;
        }
    }

    public sealed class PuzzleDetector
    {
        private sealed class OldFrame
        {
            public ImagePattern TopLeft, Close, BottomLeft;
            public Point TopLeftOffset, CloseOffset, BottomOffset;
        }
        private readonly string assets;
        private readonly DigitReader digits;
        private readonly SliderImageReader sliders;
        private readonly KnotImageReader knots;
        private readonly MasterClueReader masterClues;
        private readonly ReferenceClueReader referenceClues;
        private readonly CompassVision compass;
        private readonly ScanVision scans;
        private readonly List<KeyValuePair<int, double[]>> modernLockboxReferences = new List<KeyValuePair<int, double[]>>();
        private readonly List<KeyValuePair<int, double[]>> darkscapeLockboxReferences = new List<KeyValuePair<int, double[]>>();
        private readonly Dictionary<float, ImagePattern[]> patterns = new Dictionary<float, ImagePattern[]>();
        private readonly Dictionary<float, ImagePattern[]> lockboxPatterns = new Dictionary<float, ImagePattern[]>();
        private readonly Dictionary<float, ImagePattern> darkscapeClose = new Dictionary<float, ImagePattern>();
        private readonly Dictionary<float, OldFrame[]> oldFrames = new Dictionary<float, OldFrame[]>();
        private readonly Dictionary<float, ImagePattern[]> oldSliderAnchors = new Dictionary<float, ImagePattern[]>();
        private readonly Dictionary<float, OldFrame[]> blockyFrames = new Dictionary<float, OldFrame[]>();
        private readonly Dictionary<float, ImagePattern[]> blockySliders = new Dictionary<float, ImagePattern[]>();
        private int activeDpi = 96;
        private bool legacyInterface;
        private bool darkscapeInterface;
        private int resolutionChoice;
        private int scalingChoice;
        private int resolutionManual = 1080;
        private int scalingManual = 100;
        private int clientHeight;
        private readonly float[] scales = { 1F, 0.8F, 8F/9F, 2F/3F, 0.5F, 1.25F, 1.5F, 1.75F, 2F, 2.25F, 2.5F, 3F };
        // Nearest of 1080, 1440 and 2160. A 2560x1369 window is 1440p.
        public static int ResolutionBucket(int pixelHeight)
        {
            if (pixelHeight <= 0) return 0;
            int best = 1080, bestDist = Math.Abs(pixelHeight - 1080);
            int mid = Math.Abs(pixelHeight - 1440);
            if (mid < bestDist) { best = 1440; bestDist = mid; }
            int tall = Math.Abs(pixelHeight - 2160);
            if (tall < bestDist) best = 2160;
            return best;
        }
        public static string ResolutionName(int bucket)
        {
            if (bucket >= 2000) return "4K";
            if (bucket >= 1300) return "1440p";
            return "1080p";
        }
        public static int ScalingPercent(int dpi)
        {
            if (dpi <= 0) return 0;
            float exact = dpi / 96F * 100F;
            int best = 100;
            float bestDist = Math.Abs(exact - 100F);
            int[] choices = { 125, 150, 200 };
            for (int i = 0; i < choices.Length; i++)
            {
                float dist = Math.Abs(exact - choices[i]);
                if (dist < bestDist) { best = choices[i]; bestDist = dist; }
            }
            return best;
        }
        // Resolution over 1080, times the Windows scale. A manual dropdown
        // replaces the part it names. Auto with no window uses the last manual choice.
        public static float TargetScale(int pixelHeight, int dpi, int resolution, int scaling, int resolutionManual, int scalingManual)
        {
            int bucket = resolution;
            if (bucket != 1080 && bucket != 1440 && bucket != 2160)
            {
                bucket = ResolutionBucket(pixelHeight);
                if (bucket == 0) bucket = resolutionManual == 1440 || resolutionManual == 2160 ? resolutionManual : 1080;
            }
            float scale = bucket / 1080F;
            if (scaling == 100 || scaling == 125 || scaling == 150 || scaling == 200) scale *= scaling / 100F;
            else if (dpi > 0) scale *= dpi / 96F;
            else scale *= (scalingManual == 125 || scalingManual == 150 || scalingManual == 200 ? scalingManual : 100) / 100F;
            return scale;
        }
        public static float[] SearchOrder(int pixelHeight)
        {
            return SearchOrder(pixelHeight, 96);
        }
        public static float[] SearchOrder(int pixelHeight, int dpi)
        {
            return SearchOrder(pixelHeight, dpi, 0, 0, 1080, 100);
        }
        public static float[] SearchOrder(int pixelHeight, int dpi, int resolution, int scaling, int resolutionManual, int scalingManual)
        {
            float[] classic = { 1F, 0.8F, 8F/9F, 2F/3F, 0.5F, 1.25F, 1.5F, 2F, 1.75F, 2.25F, 2.5F, 3F };
            float target = TargetScale(pixelHeight, dpi <= 0 ? 0 : dpi, resolution, scaling, resolutionManual, scalingManual);
            if (Math.Abs(target - 1F) < 0.02F) return classic;
            return PlaceScale(classic, target);
        }
        // The interface scale comes first. 1.0 is next, so a 1080p puzzle is
        // still found on the second try at 125%, 150% and 200%.
        static float[] PlaceScale(float[] classic, float target)
        {
            var ordered = classic.OrderBy(s => Math.Abs(s - target)).ThenBy(s => s).ToList();
            if (Math.Abs(ordered[0] - 1F) > 0.02F)
            {
                for (int i = 0; i < ordered.Count; i++) if (Math.Abs(ordered[i] - 1F) < 0.001F) { float one = ordered[i]; ordered.RemoveAt(i); ordered.Insert(1, one); break; }
            }
            return ordered.ToArray();
        }
        public int LastTowerCluesRead;
        private string towerSolveKey;
        private List<int[]> towerSolutions;
        public int BroadSearches { get; private set; }
        public int BoardPasses { get; private set; }
        public int ScrollFastPaths { get; private set; }
        public bool LastScrollFast { get; private set; }
        public Rectangle LastScrollBounds { get; private set; }
        public float LastFirstScale { get; private set; }
        private readonly System.Collections.Generic.HashSet<float> triedScales = new System.Collections.Generic.HashSet<float>();
        // Set when a dark towers grid with some edge clues is visible but the
        // clues are not all readable yet (the panel is still opening).
        public Rectangle? TowerGridCandidate { get; private set; }
        public Action<string> Progress;
        public PuzzleDetector(string directory)
        {
            assets = directory; digits = new DigitReader(directory); sliders = new SliderImageReader(directory); knots = new KnotImageReader(directory); masterClues=new MasterClueReader(directory); compass=new CompassVision(directory);
            masterClues.Progress=message=>{if(Progress!=null)Progress(message);};
            referenceClues=new ReferenceClueReader(directory);referenceClues.Progress=message=>{if(Progress!=null)Progress(message);};
            scans=new ScanVision(directory);
            compass.Progress=scans.Progress=message=>{if(Progress!=null)Progress(message);};
            // DarkScape tiles use the same three symbols on a warm brown background.
            // Idle and highlighted copies both map back to melee, range, or mage.
            string[] lockNames = { "melee", "range", "mage", "melee_active", "range_active", "mage_active", "darkscape_melee", "darkscape_range", "darkscape_mage", "darkscape_melee_active", "darkscape_range_active", "darkscape_mage_active" };
            for (int i = 0; i < lockNames.Length; i++) using (Bitmap icon = new Bitmap(Path.Combine(directory, "alt1anchors", "lockbox_tiles", lockNames[i] + ".png")))
            {
                List<KeyValuePair<int, double[]>> bucket = i < 6 ? modernLockboxReferences : darkscapeLockboxReferences;
                bucket.Add(new KeyValuePair<int, double[]>(i % 3, new Pixels(icon).Fingerprint(new Rectangle(0, 0, 30, 30), 6, 6, false)));
            }
            foreach (float scale in scales)
            {
                lockboxPatterns[scale] = new[]{"melee","range","mage","darkscape_melee","darkscape_range","darkscape_mage"}.Select(name => {
                    using(Bitmap original=new Bitmap(Path.Combine(assets,"alt1anchors","lockbox_tiles",name+".png")))
                    using(Bitmap resized=Pixels.Crop(original,new Rectangle(Point.Empty,original.Size),new Size((int)Math.Round(30*scale),(int)Math.Round(30*scale)))) return new ImagePattern(resized);
                }).ToArray();
                string[] names = { "modal/top_left.png", "modal/top_right.png", "modal/bot_left.png", "sliders/eoc_x.png" };
                patterns[scale] = names.Select(name => { using (Bitmap original = new Bitmap(Path.Combine(assets, "alt1anchors", name))) using (Bitmap scaled = Pixels.Crop(original, new Rectangle(Point.Empty, original.Size), new Size((int)Math.Round(original.Width * scale), (int)Math.Round(original.Height * scale)))) return new ImagePattern(scaled); }).ToArray();
                using (Bitmap close = new Bitmap(Path.Combine(assets, "alt1anchors", "darkscape", "close.png")))
                using (Bitmap closeScaled = Pixels.Crop(close, new Rectangle(Point.Empty, close.Size), new Size(Math.Max(8, (int)Math.Round(close.Width * scale)), Math.Max(8, (int)Math.Round(close.Height * scale)))))
                    darkscapeClose[scale] = new ImagePattern(closeScaled);
                oldFrames[scale] = new[] {
                    new OldFrame { TopLeft = OldPattern("eoctopleft.png", scale), Close = OldPattern("eocx.png", scale), BottomLeft = OldPattern("eocbotleft.png", scale), TopLeftOffset = new Point(4,29), CloseOffset = new Point(10,24), BottomOffset = new Point(3,7) },
                    new OldFrame { TopLeft = OldPattern("legacytopleft.png", scale), Close = OldPattern("legacyx.png", scale), BottomLeft = OldPattern("legacybotleft.png", scale), TopLeftOffset = new Point(4,29), CloseOffset = new Point(19,20), BottomOffset = new Point(6,-2) }
                };
                oldSliderAnchors[scale] = new[] { OldPattern("slide.png", scale), OldPattern("slidelegacy.png", scale), OldPattern("slide944-native.png", scale) };
            }
        }
        private ImagePattern OldPattern(string name, float scale)
        {
            using (Bitmap image = new Bitmap(Path.Combine(assets, "alt1anchors", "rev944", name)))
            using (Bitmap resized = Pixels.Crop(image, new Rectangle(Point.Empty, image.Size), new Size((int)Math.Round(image.Width * scale), (int)Math.Round(image.Height * scale)))) return new ImagePattern(resized);
        }
        public void UseLegacy(bool legacy)
        {
            UseClient(legacy, resolutionChoice, scalingChoice, resolutionManual, scalingManual, clientHeight, false);
        }
        public void UseDarkscape(bool enabled)
        {
            UseClient(enabled ? false : legacyInterface, resolutionChoice, scalingChoice, resolutionManual, scalingManual, clientHeight, enabled);
        }
        public void UseClient(bool legacy, int resolution, int scaling, int manualResolution, int manualScaling, int height)
        {
            UseClient(legacy, resolution, scaling, manualResolution, manualScaling, height, false);
        }
        public void UseClient(bool legacy, int resolution, int scaling, int manualResolution, int manualScaling, int height, bool darkscape)
        {
            bool skin = darkscape && !legacy;
            if (legacy != legacyInterface || skin != darkscapeInterface || resolution != resolutionChoice || scaling != scalingChoice || manualResolution != resolutionManual || manualScaling != scalingManual || height != clientHeight)
                nextSearchScale = 0;
            if (skin != darkscapeInterface) { lockScaleCursor = 0; lockIconCursor = 0; }
            legacyInterface = legacy;
            darkscapeInterface = skin;
            resolutionChoice = resolution;
            scalingChoice = scaling;
            if (manualResolution == 1080 || manualResolution == 1440 || manualResolution == 2160) resolutionManual = manualResolution;
            if (manualScaling == 100 || manualScaling == 125 || manualScaling == 150 || manualScaling == 200) scalingManual = manualScaling;
            clientHeight = height;
            ApplyReaders();
        }
        private int ProfileHeight(int imageHeight)
        {
            return clientHeight > 0 ? clientHeight : imageHeight;
        }
        private void ApplyReaders()
        {
            float target = 0F;
            if (clientHeight > 0 || resolutionChoice != 0 || scalingChoice != 0 || activeDpi > 96)
                target = TargetScale(ProfileHeight(0), activeDpi, resolutionChoice, scalingChoice, resolutionManual, scalingManual);
            if (compass != null) { compass.UseDpi(activeDpi); compass.UseTarget(target); }
            if (scans != null) { scans.UseDpi(activeDpi); scans.LegacyInterface = legacyInterface; scans.UseTarget(target); }
            if (masterClues != null) masterClues.UseDpi(activeDpi);
        }
        public void UseDpi(int dpi)
        {
            if (dpi < 96) dpi = 96;
            if (dpi != activeDpi) { activeDpi = dpi; nextSearchScale = 0; }
            ApplyReaders();
        }
        private ImagePattern BlockyPattern(string name, float scale)
        {
            using (Bitmap image = new Bitmap(Path.Combine(assets, "alt1anchors", "rev944", name)))
            using (Bitmap resized = Pixels.ScaleNearest(image, scale)) return new ImagePattern(resized);
        }
        private OldFrame[] BlockyFrames(float scale)
        {
            OldFrame[] ready;
            if (blockyFrames.TryGetValue(scale, out ready)) return ready;
            ready = new[] {
                new OldFrame { TopLeft = BlockyPattern("eoctopleft.png", scale), Close = BlockyPattern("eocx.png", scale), BottomLeft = BlockyPattern("eocbotleft.png", scale), TopLeftOffset = new Point(4,29), CloseOffset = new Point(10,24), BottomOffset = new Point(3,7) },
                new OldFrame { TopLeft = BlockyPattern("legacytopleft.png", scale), Close = BlockyPattern("legacyx.png", scale), BottomLeft = BlockyPattern("legacybotleft.png", scale), TopLeftOffset = new Point(4,29), CloseOffset = new Point(19,20), BottomOffset = new Point(6,-2) }
            };
            blockyFrames[scale] = ready;
            return ready;
        }
        private ImagePattern[] BlockySliders(float scale)
        {
            ImagePattern[] ready;
            if (blockySliders.TryGetValue(scale, out ready)) return ready;
            ready = new[] { BlockyPattern("slide.png", scale), BlockyPattern("slidelegacy.png", scale), BlockyPattern("slide944-native.png", scale) };
            blockySliders[scale] = ready;
            return ready;
        }
        public DetectedPuzzle Detect(Bitmap screenshot, bool allowBroadSearch = true, bool progressive = false)
        {
            return Detect(screenshot, new Pixels(screenshot), allowBroadSearch, progressive);
        }
        internal DetectedPuzzle Detect(Bitmap screenshot, Pixels pixels, bool allowBroadSearch, bool progressive)
        {
            DetectedPuzzle board=DetectBoards(screenshot,pixels,allowBroadSearch,progressive);
            if(board!=null) return board;
            return DetectClues(screenshot,pixels);
        }
        internal DetectedPuzzle DetectBoards(Bitmap screenshot, Pixels pixels, bool allowBroadSearch, bool progressive)
        {
            BoardPasses++;
            TowerGridCandidate = null;
            float[] order=SearchOrder(ProfileHeight(screenshot.Height), activeDpi, resolutionChoice, scalingChoice, resolutionManual, scalingManual);
            if(!progressive)
            {
                DetectedPuzzle fastTower = FindTowerGrid(screenshot, pixels, int.MaxValue, true);
                if (fastTower != null) return fastTower;
                DetectedPuzzle lockbox=FindLockboxGrid(screenshot,pixels,order,int.MaxValue,true);
                if(lockbox!=null) return lockbox;
                if(!allowBroadSearch) return null;
                DetectedPuzzle knot=FindKnotBoard(screenshot,pixels,80);
                if(knot!=null) return knot;
                BroadSearches++;
                foreach(float scale in order)
                {
                    DetectedPuzzle match=FindAtScale(screenshot,pixels,scale,null,int.MaxValue);
                    if(match!=null) return match;
                }
                return null;
            }
            // Towers is a short resumable slice at the front of the live frame.
            // The scale search and the lockbox search were using the whole
            // 180 ms, so this scan never started. The slice stays capped, and
            // the 180 ms budget for those other searches starts after it.
            const int towerSlice=40;
            DetectedPuzzle tower=FindTowerGrid(screenshot,pixels,towerSlice,false);
            if(tower!=null) return tower;
            // The knot window has no full-width title bar, so it is not part of
            // the scale search. A short slice keeps it from eating the budget
            // Towers, Lockbox and the sliders still use.
            DetectedPuzzle liveKnot=FindKnotBoard(screenshot,pixels,45);
            if(liveKnot!=null) return liveKnot;
            // Lockbox used to run only after the scale search. On a 1440p
            // client that search uses the whole 180 ms, so the grid never
            // started. A short resumable slice runs first, the same way
            // Towers and the knot do. The 40 ms tower slice and the 180 ms
            // budget are unchanged.
            DetectedPuzzle liveBox=FindLockboxGrid(screenshot,pixels,order,LiveLockboxSlice,false);
            if(liveBox!=null) return liveBox;
            var timer=System.Diagnostics.Stopwatch.StartNew();
            const int budget=180;
            if(allowBroadSearch)
            {
                BroadSearches++;
                // Every pass starts at the scale this client should be using.
                // The rotation only adds one more scale, and only while the
                // frame budget still has time left.
                float expected=ExpectedScale(ProfileHeight(screenshot.Height));
                LastFirstScale=expected;
                triedScales.Add(expected);
                DetectedPuzzle match=FindAtScale(screenshot,pixels,expected,null,int.MaxValue);
                if(match!=null)return match;
                float extra=NextExtraScale(order,expected);
                if(timer.ElapsedMilliseconds<120)
                {
                    triedScales.Add(extra);
                    match=FindAtScale(screenshot,pixels,extra,timer,budget);
                    if(match!=null)return match;
                }
            }
            return null;
        }
        internal DetectedPuzzle FindCompass(Bitmap image,Pixels pixels)
        {
            if(image==null||pixels==null)return null;
            return compass.Find(image,pixels);
        }
        internal DetectedPuzzle ReadNarrowSheet(Bitmap image,Pixels pixels,out bool visible)
        {
            visible=false;
            if(image==null||pixels==null)return null;
            List<Rectangle> boxes=ParagraphVision.NarrowSheets(pixels);
            visible=false;
            foreach(Rectangle box in boxes)if(TitleInk(pixels,box)){visible=true;break;}
            if(!visible)return null;
            // Glyphs, scale 1.0, the compass, then OCR. Same look, no throttle.
            DetectedPuzzle boxed=ReadParchment(image,pixels);
            if(boxed!=null)return boxed;
            return scans.FindNativeTitle(pixels);
        }
        static bool TitleInk(Pixels p,Rectangle box)
        {
            int white=0,bottom=box.Top+Math.Max(8,box.Height/2);
            for(int y=box.Top;y<bottom&&y<p.Height;y+=2)
            for(int x=box.Left;x<box.Right&&x<p.Width;x+=2)
            {Color c=p.Get(x,y);if(c.R>185&&c.G>185&&c.B>185)white++;}
            return white>=12;
        }
        DetectedPuzzle ReadParchment(Bitmap image,Pixels pixels)
        {
            foreach(Rectangle box in ParagraphVision.NarrowSheets(pixels))
            {
                // Title glyphs and the scale-1 models. OCR waits until the
                // compass has had the same box, so a compass is not delayed.
                DetectedPuzzle scan=scans.Find(image,pixels,box,false);
                if(scan!=null)return scan;
                DetectedPuzzle direction=compass.Find(image,pixels,box);
                if(direction!=null)return direction;
                DetectedPuzzle elite=scans.ReadEliteBox(image,pixels,box);
                if(elite!=null)return elite;
            }
            return null;
        }
        internal DetectedPuzzle DetectClues(Bitmap screenshot, Pixels pixels)
        {
            DetectedPuzzle boxed=ReadParchment(screenshot,pixels);if(boxed!=null)return boxed;
            DetectedPuzzle scanTitle=scans.FindNativeTitle(pixels);if(scanTitle!=null)return scanTitle;
            DetectedPuzzle emote=masterClues.Detect(screenshot,pixels); if(emote!=null) return emote;
            DetectedPuzzle reference=referenceClues.Detect(screenshot,pixels);if(reference!=null)return reference;
            DetectedPuzzle direction=compass.Find(screenshot,pixels);
            if(direction!=null) return direction;
            return scans.Find(screenshot,pixels);
        }
        // The titled sheet was found but not read. Run the same clue readers
        // on that sheet. The full-window scan search stays off this path:
        // it looks for scenery, and it is what makes a scroll frame slow.
        internal DetectedPuzzle DetectClues(Bitmap screenshot, Pixels pixels, Rectangle sheet)
        {
            DetectedPuzzle scanTitle=scans.FindNativeTitle(pixels);if(scanTitle!=null)return scanTitle;
            // The title run can be tighter than the writing. Search that sheet
            // first, then the whole window, so a real paragraph is not missed.
            DetectedPuzzle emote=masterClues.Detect(screenshot,pixels,sheet);
            if(emote==null)emote=masterClues.Detect(screenshot,pixels);
            if(emote!=null) return emote;
            DetectedPuzzle reference=referenceClues.Detect(screenshot,pixels);if(reference!=null)return reference;
            return compass.Find(screenshot,pixels,sheet);
        }
        private int nextSearchScale;
        private int towerScanY = 25;
        private int knotScanY = 24;
        private int lockScaleCursor;
        private int lockIconCursor;
        private int heavyGate;
        public void ResetSearch() { nextSearchScale=0; towerScanY=25; knotScanY=24; lockScaleCursor=0; lockIconCursor=0; triedScales.Clear(); heavyGate=0; }
        // The chosen resolution and scaling pick the first template. The other
        // scales stay in the order, so a near miss is still readable.
        public float ExpectedScale(int pixelHeight)
        {
            float target=TargetScale(pixelHeight, activeDpi, resolutionChoice, scalingChoice, resolutionManual, scalingManual);
            float best=scales[0],bestDist=Math.Abs(best-target);
            foreach(float scale in scales)
            {
                float dist=Math.Abs(scale-target);
                if(dist<bestDist-0.0001F||(Math.Abs(dist-bestDist)<=0.0001F&&scale<best)){best=scale;bestDist=dist;}
            }
            return best;
        }
        float NextExtraScale(float[] order,float expected)
        {
            for(int n=0;n<order.Length;n++)
            {
                float scale=order[nextSearchScale%order.Length];
                nextSearchScale=(nextSearchScale+1)%order.Length;
                if(Math.Abs(scale-expected)>0.02F)return scale;
            }
            return order[0];
        }
        public bool DueForHeavyClues()
        {
            bool covered=true;
            foreach(float scale in new[]{1F,1.25F,1.5F,2F})
            {
                bool seen=false;
                foreach(float tried in triedScales)if(Math.Abs(tried-scale)<0.02F){seen=true;break;}
                if(!seen){covered=false;break;}
            }
            // The first look, and every fourth look after that, still reads a
            // clue. Resetting the counter on a hit would make the next pass
            // look like the first and run the readers again.
            heavyGate++;
            bool due=covered||heavyGate%4==1;
            if(due)triedScales.Clear();
            if(heavyGate>=4)heavyGate=0;
            return due;
        }
        internal DetectedPuzzle TryScroll(Bitmap image, Pixels pixels)
        {
            LastScrollFast = false;
            LastScrollBounds = Rectangle.Empty;
            // DarkScape parchment has a brown frame and a gold close button, not
            // the modern full-width title bar. Each mode searches only its own.
            var scrolls = darkscapeInterface ? ParagraphVision.FindDarkscapeScrolls(pixels) : ParagraphVision.FindScrolls(pixels);
            if (scrolls == null || scrolls.Count == 0) return null;
            LastScrollFast = true;
            LastScrollBounds = scrolls[0];
            ScrollFastPaths++;
            return referenceClues.ReadSheet(image, pixels, scrolls[0]);
        }
        private DetectedPuzzle FindAtScale(Bitmap screenshot,Pixels pixels,float scale,System.Diagnostics.Stopwatch clock,int budget)
        {
            if(clock!=null&&clock.ElapsedMilliseconds>=budget)return null;
            // The settings pick one interface. Legacy keeps the old frames.
            // DarkScape uses its brown close button and does not scan the
            // modern blue corners.
            if(legacyInterface)return FindOldInterface(screenshot,pixels,scale,clock,budget);
            if(darkscapeInterface)return FindDarkscape(screenshot,pixels,scale,clock,budget);
            return FindModern(screenshot,pixels,scale,clock,budget);
        }
        private DetectedPuzzle FindModern(Bitmap screenshot,Pixels pixels,float scale,System.Diagnostics.Stopwatch clock,int budget)
        {
            if(clock!=null&&clock.ElapsedMilliseconds>=budget)return null;
            Rectangle all=new Rectangle(Point.Empty,screenshot.Size);
            ImagePattern[] p = patterns[scale];
            List<Point> lefts = p[0].Find(pixels, all, 12);
            foreach (Point left in lefts)
            {
                if(clock!=null&&clock.ElapsedMilliseconds>=budget)return null;
                Rectangle rightBand=Rectangle.FromLTRB(left.X+(int)(130*scale),Math.Max(0,left.Y-(int)(8*scale)),Math.Min(screenshot.Width,left.X+(int)(820*scale)),Math.Min(screenshot.Height,left.Y+p[1].Size.Height+(int)(9*scale)));
                List<Point> rights=p[1].Find(pixels,rightBand,18);
                Point? right = rights.Where(r => r.X > left.X + 130 * scale && r.X < left.X + 800 * scale && Math.Abs(r.Y - left.Y) < 8 * scale).Select(r => (Point?)r).FirstOrDefault();
                if (!right.HasValue) continue;
                Rectangle bottomSearch = Rectangle.FromLTRB(Math.Max(0, left.X - 2), left.Y + (int)(120 * scale), Math.Min(screenshot.Width, left.X + (int)(12 * scale)), Math.Min(screenshot.Height, left.Y + (int)(500 * scale)));
                Point? bottom = p[2].Find(pixels, bottomSearch, 12, scale < 1 ? 100 : 47).Select(r => (Point?)r).FirstOrDefault();
                if (!bottom.HasValue) continue;
                Rectangle body = Rectangle.FromLTRB(left.X + (int)Math.Round(5 * scale), left.Y + (int)Math.Round(31 * scale), right.Value.X + (int)Math.Round(12 * scale), bottom.Value.Y + (int)Math.Round(3 * scale));
                DetectedPuzzle result = ReadModal(screenshot, body, scale);
                if (result != null) return result;
            }
            foreach (Point close in p[3].Find(pixels, all, 15))
            {
                DetectedPuzzle slider = ReadSliderGrid(screenshot, close, scale, 298, 14); if (slider != null) return slider;
            }
            return null;
        }
        private DetectedPuzzle FindDarkscape(Bitmap screenshot, Pixels pixels, float scale, System.Diagnostics.Stopwatch clock, int budget)
        {
            if(clock!=null&&clock.ElapsedMilliseconds>=budget)return null;
            Rectangle all=new Rectangle(Point.Empty, screenshot.Size);
            ImagePattern close;
            if(darkscapeClose.TryGetValue(scale, out close))
            {
                foreach(Point point in close.Find(pixels, all, 15))
                {
                    DetectedPuzzle slider=ReadSliderGrid(screenshot, point, scale, 298, 14);
                    if(slider!=null)return slider;
                }
            }
            // The puzzle window's gold close glyph is shared with the modern
            // client. The blue corner templates are not searched here.
            ImagePattern[] modern;
            if(!patterns.TryGetValue(scale, out modern)||modern.Length<4)return null;
            foreach(Point point in modern[3].Find(pixels, all, 15))
            {
                if(clock!=null&&clock.ElapsedMilliseconds>=budget)return null;
                DetectedPuzzle slider=ReadSliderGrid(screenshot, point, scale, 298, 14);
                if(slider!=null)return slider;
            }
            return null;
        }
        // One close-button search at the scale this client should be using.
        // A side panel with bright text is not a clue, and this is what still
        // finds the slider on that frame. It does not walk the other scales.
        internal DetectedPuzzle FindOpenSlider(Bitmap image, Pixels pixels)
        {
            if (image == null || pixels == null) return null;
            float scale = ExpectedScale(ProfileHeight(image.Height));
            Rectangle all = new Rectangle(Point.Empty, image.Size);
            if (!legacyInterface)
            {
                if (darkscapeInterface)
                {
                    DetectedPuzzle brownSlider = FindDarkscape(image, pixels, scale, null, int.MaxValue);
                    return brownSlider;
                }
                ImagePattern[] modern;
                if (!patterns.TryGetValue(scale, out modern) || modern.Length < 4) return null;
                foreach (Point close in modern[3].Find(pixels, all, 15))
                {
                    DetectedPuzzle slider = ReadSliderGrid(image, close, scale, 298, 14);
                    if (slider != null) return slider;
                }
                return null;
            }
            ImagePattern[] anchors;
            if (!oldSliderAnchors.TryGetValue(scale, out anchors)) return null;
            foreach (ImagePattern anchor in anchors) foreach (Point close in anchor.Find(pixels, all, 15))
            {
                DetectedPuzzle slider = ReadSliderGrid(image, close, scale, 297, 15);
                if (slider != null) return slider;
            }
            return null;
        }
        private DetectedPuzzle ReadSliderGrid(Bitmap image, Point close, float scale, int left, int down)
        {
            Rectangle grid = new Rectangle(close.X - (int)Math.Round(left * scale), close.Y + (int)Math.Round(down * scale), (int)Math.Round(273 * scale), (int)Math.Round(273 * scale));
            return sliders.Read(image, grid, scale);
        }
        private DetectedPuzzle FindOldInterface(Bitmap image, Pixels pixels, float scale, System.Diagnostics.Stopwatch clock, int budget)
        {
            if(clock!=null&&clock.ElapsedMilliseconds>=budget)return null;
            if (activeDpi != 96)
            {
                DetectedPuzzle blocky = MatchOldInterface(image, pixels, scale, BlockyFrames(scale), BlockySliders(scale), clock, budget);
                if (blocky != null) return blocky;
            }
            return MatchOldInterface(image, pixels, scale, oldFrames[scale], oldSliderAnchors[scale], clock, budget);
        }
        private DetectedPuzzle MatchOldInterface(Bitmap image, Pixels pixels, float scale, OldFrame[] frames, ImagePattern[] sliderAnchors, System.Diagnostics.Stopwatch clock, int budget)
        {
            Rectangle all = new Rectangle(Point.Empty, image.Size);
            foreach (OldFrame frame in frames)
            {
                if(clock!=null&&clock.ElapsedMilliseconds>=budget)return null;
                List<Point> lefts = frame.TopLeft.Find(pixels, all, 12);
                foreach (Point left in lefts)
                {
                    if(clock!=null&&clock.ElapsedMilliseconds>=budget)return null;
                    int closeY = left.Y + (int)Math.Round((frame.TopLeftOffset.Y - frame.CloseOffset.Y) * scale);
                    Rectangle closeBand=Rectangle.FromLTRB(left.X+(int)(120*scale),Math.Max(0,closeY-(int)(4*scale)),Math.Min(image.Width,left.X+(int)(820*scale)),Math.Min(image.Height,closeY+frame.Close.Size.Height+(int)(5*scale)));
                    List<Point> closes=frame.Close.Find(pixels,closeBand,20);
                    foreach (Point close in closes.Where(c => c.X > left.X + 125 * scale && c.X < left.X + 800 * scale && Math.Abs(c.Y - closeY) <= Math.Max(2, 3 * scale)))
                    {
                        int bottomX = left.X + (int)Math.Round((frame.TopLeftOffset.X - frame.BottomOffset.X) * scale);
                        Rectangle search = Rectangle.FromLTRB(Math.Max(0, bottomX - 3), left.Y + (int)(110 * scale), Math.Min(image.Width, bottomX + frame.BottomLeft.Size.Width + 4), Math.Min(image.Height, left.Y + (int)(500 * scale)));
                        foreach (Point bottom in frame.BottomLeft.Find(pixels, search, 12, scale < 1 ? 100 : 47).Where(b => Math.Abs(b.X - bottomX) <= Math.Max(2, 3 * scale)))
                        {
                            Rectangle body = Rectangle.FromLTRB(left.X + (int)Math.Round(frame.TopLeftOffset.X * scale), left.Y + (int)Math.Round(frame.TopLeftOffset.Y * scale), close.X + (int)Math.Round((frame.CloseOffset.X + 1) * scale), bottom.Y + (int)Math.Round((frame.BottomOffset.Y + 1) * scale));
                            DetectedPuzzle puzzle = ReadModal(image, body, scale); if (puzzle != null) return puzzle;
                        }
                    }
                }
            }
            foreach (ImagePattern anchor in sliderAnchors) foreach (Point close in anchor.Find(pixels, all, 15))
            {
                Rectangle grid = new Rectangle(close.X - (int)Math.Round(297 * scale), close.Y + (int)Math.Round(15 * scale), (int)Math.Round(273 * scale), (int)Math.Round(273 * scale));
                DetectedPuzzle puzzle = sliders.Read(image, grid, scale); if (puzzle != null) return puzzle;
            }
            return null;
        }
        public string Diagnose(Bitmap image)
        {
            Pixels pixels = new Pixels(image); Rectangle all = new Rectangle(Point.Empty, image.Size);
            string result = String.Join(" / ", scales.Select(s => s + ":" + String.Join(";", patterns[s].Select(p => String.Join(",", p.Find(pixels, all, 8))))));
            result += " OLD " + String.Join(" / ", scales.Select(s => s + ":" + String.Join(";", oldFrames[s].Select(f => "TL=" + String.Join(",", f.TopLeft.Find(pixels, all, 5)) + " X=" + String.Join(",", f.Close.Find(pixels, all, 5)) + " BL=" + String.Join(",", f.BottomLeft.Find(pixels, all, 5, s < 1 ? 100 : 47))))));
            return result;
        }
        public string DiagnoseKnot(Bitmap image, Rectangle body)
        { using (Bitmap crop = Pixels.Crop(image, body, body.Size)) { knots.Read(crop); return knots.LastError; } }
        public bool SliderFrameVisible(Bitmap image,DetectedPuzzle previous)
        {
            if(previous==null||previous.Kind!=PuzzleKind.Sliding)return false;
            Pixels pixels=new Pixels(image);Rectangle search=Rectangle.Intersect(new Rectangle(Point.Empty,image.Size),Rectangle.Inflate(previous.Bounds,70,70));
            if(!legacyInterface)
            {
                ImagePattern[] modern;
                if(!patterns.TryGetValue(previous.Scale,out modern)||modern.Length<4)return false;
                return modern[3].Find(pixels,search,1,47).Count>0;
            }
            ImagePattern[] anchors;if(!oldSliderAnchors.TryGetValue(previous.Scale,out anchors))return false;
            if(anchors.Any(a=>a.Find(pixels,search,1,47).Count>0))return true;
            if(activeDpi==96)return false;
            return BlockySliders(previous.Scale).Any(a=>a.Find(pixels,search,1,47).Count>0);
        }
        public DetectedPuzzle ReadAgain(Bitmap screenshot, DetectedPuzzle previous, Pixels pixels = null, int badgeTile = -1, GuideInk guideInk = null)
        {
            if (previous == null || !new Rectangle(Point.Empty, screenshot.Size).Contains(previous.Bounds)) return null;
            if (previous.Kind == PuzzleKind.EmoteClue || previous.Kind == PuzzleKind.Scan)
                pixels = pixels ?? new Pixels(screenshot);
            if (previous.Kind == PuzzleKind.EmoteClue)
                return masterClues.Detect(screenshot, pixels, Rectangle.Inflate(previous.Bounds,15,15)) ?? referenceClues.Detect(screenshot, pixels);
            if(previous.Kind==PuzzleKind.Compass) return compass.Read(screenshot,previous.Bounds,previous.Scale);
            if(previous.Kind==PuzzleKind.Scan)return scans.Find(screenshot,pixels);
            if (previous.Kind == PuzzleKind.Sliding)
            {
                int arrow = -1;
                if (badgeTile >= 0 && previous.Board != null) { int blank = Array.IndexOf(previous.Board, 24); if (blank >= 0) arrow = blank; }
                return sliders.Read(screenshot, previous.Grid, previous.Scale, badgeTile, arrow, guideInk);
            }
            if (previous.Kind == PuzzleKind.Towers)
            {
                DetectedPuzzle towers=ReadTowers(screenshot, previous.Grid, previous.Scale);
                return towers!=null?towers:HoldTowers(screenshot, previous);
            }
            if (previous.Kind == PuzzleKind.Lockbox)
            {
                DetectedPuzzle box=ReadLockboxGrid(screenshot,previous.Grid,previous.Scale,previous.Bounds,0.82);
                if(box!=null)return box;
                Pixels ink=pixels??new Pixels(screenshot);
                return LockboxSurface(ink, previous.Grid.Location, previous.Scale)?previous:null;
            }
            if (previous.Kind == PuzzleKind.Knot)
                return ReadLockedKnot(screenshot, previous);
            return ReadModal(screenshot, previous.Bounds, previous.Scale);
        }
        public bool KnotFrameVisible(Bitmap image,DetectedPuzzle previous)
        {
            if(previous==null||previous.Kind!=PuzzleKind.Knot)return false;
            Pixels pixels=new Pixels(image);Rectangle body=previous.Bounds;
            if(body.Width>=240&&body.Height>=150&&KnotBorder(pixels,body.Left,body.Top,body.Right-1,body.Bottom-1)&&KnotSpan(pixels,body.Left,body.Right-1,(body.Top+body.Bottom)/2))return true;
            if(!oldFrames.ContainsKey(previous.Scale))return false;
            Rectangle header=Rectangle.Intersect(new Rectangle(body.Left-20,body.Top-40,body.Width+45,48),new Rectangle(Point.Empty,image.Size));
            if(legacyInterface)return oldFrames[previous.Scale].Any(f=>f.Close.Find(pixels,header,1,35).Count>0);
            return patterns[previous.Scale][1].Find(pixels,header,1,35).Count>0;
        }
        private DetectedPuzzle ReadModal(Bitmap image, Rectangle body, float scale)
        {
            if (body.Width < 100 || body.Height < 100 || !new Rectangle(Point.Empty, image.Size).Contains(body)) return null;
            if (body.Width > 350 * scale)
            {
                Rectangle grid = new Rectangle(body.X + (int)Math.Round(41 * scale), body.Y + (int)Math.Round(38 * scale), (int)Math.Round(218 * scale), (int)Math.Round(218 * scale));
                DetectedPuzzle towers = ReadTowers(image, grid, scale); if (towers != null) return towers;
                  int paper=0;
                  for(int yy=1;yy<=5;yy++)for(int xx=1;xx<=5;xx++)
                  {Color colour=image.GetPixel(body.X+xx*body.Width/6,body.Y+yy*body.Height/6);if(colour.R>155&&colour.G>115&&colour.B>65&&colour.R>colour.G&&colour.G>colour.B)paper++;}
                  if(paper>=18)
                  {
                      DetectedPuzzle master=masterClues.Detect(image,new Pixels(image),body);
                      if(master!=null)return master;
                  }
                using (Bitmap normalised = Pixels.Crop(image, body, new Size((int)Math.Round(body.Width / scale), (int)Math.Round(body.Height / scale))))
                {
                    KnotPuzzle knot = normalised == null ? null : knots.Read(normalised);
                    if (knot != null) return new DetectedPuzzle { Kind = PuzzleKind.Knot, Bounds = body, Grid = body, Scale = scale, Knot = knot, Confidence = 0.9 };
                }
            }
            else
            {
                Rectangle grid = new Rectangle(body.X + (int)Math.Round(42 * scale), body.Y + (int)Math.Round(17 * scale), (int)Math.Round(182 * scale), (int)Math.Round(182 * scale));
                return ReadLockboxGrid(image,grid,scale,body,0.82);
            }
            return null;
        }
        private DetectedPuzzle ReadLockboxGrid(Bitmap image,Rectangle grid,float scale,Rectangle bounds,double threshold)
        {
            using(Bitmap normalised=Pixels.Crop(image,grid,new Size(182,182)))
            {
                if(normalised==null) return null;
                Pixels pixels=new Pixels(normalised); int[] board=new int[25]; double minimum=1;
                // Check distant cells first, so an isolated inventory icon is rejected quickly.
                foreach(int i in new[]{0,24,4,20,12}.Concat(Enumerable.Range(0,25).Where(i=>i!=0 && i!=24 && i!=4 && i!=20 && i!=12)))
                {
                    double[] fingerprint=pixels.Fingerprint(new Rectangle(i%5*38,i/5*38,30,30),6,6,false);
                    double[] brightness=Enumerable.Range(0,36).Select(n=>(fingerprint[n*3]+fingerprint[n*3+1]+fingerprint[n*3+2])/3).ToArray();
                    if(brightness.Max()-brightness.Min()<45) return null;
                    var best=ActiveLockboxReferences().Select(r=>new {Value=r.Key,Score=Pixels.Similarity(fingerprint,r.Value,false)}).OrderByDescending(m=>m.Score).First();
                    if(best.Score<threshold) return null;
                    board[i]=best.Value; minimum=Math.Min(minimum,best.Score);
                }
                return new DetectedPuzzle {Kind=PuzzleKind.Lockbox,Bounds=bounds,Grid=grid,Scale=scale,Board=board,Confidence=minimum};
            }
        }
        internal const int LiveLockboxSlice = 160;
        internal DetectedPuzzle FindLiveLockbox(Bitmap image, Pixels pixels, int budgetMs)
        {
            if (image == null || pixels == null) return null;
            float[] order = SearchOrder(ProfileHeight(image.Height), activeDpi, resolutionChoice, scalingChoice, resolutionManual, scalingManual);
            return FindLockboxGrid(image, pixels, order, budgetMs, false);
        }
        // At 100% DPI the lockbox icons match the scale-1 templates even on a
        // 1440p window. The interface order tries 1.25 first, and that full-window
        // pass was using the whole first slice before scale 1 started.
        internal static float[] LockboxScaleOrder(float[] order, int dpi)
        {
            if (order == null || order.Length == 0 || dpi > 96) return order;
            int at = -1;
            for (int i = 0; i < order.Length; i++) if (Math.Abs(order[i] - 1F) < 0.001F) { at = i; break; }
            if (at <= 0) return order;
            float[] next = new float[order.Length];
            next[0] = order[at];
            int n = 1;
            for (int i = 0; i < order.Length; i++) if (i != at) next[n++] = order[i];
            return next;
        }
        private DetectedPuzzle FindLockboxGrid(Bitmap image,Pixels pixels,float[] order,int budgetMs,bool complete)
        {
            order = LockboxScaleOrder(order, activeDpi);
            Rectangle all=new Rectangle(Point.Empty,image.Size);
            var clock=System.Diagnostics.Stopwatch.StartNew();
            int start=complete?0:lockScaleCursor;
            int iconFrom=complete?0:lockIconCursor;
            if(start<0||start>=order.Length){start=0;iconFrom=0;}
            for(int n=0;n<order.Length;n++)
            {
                int scaleIndex=(start+n)%order.Length;
                if(clock.ElapsedMilliseconds>=budgetMs)
                {
                    if(!complete){lockScaleCursor=scaleIndex;lockIconCursor=n==0?iconFrom:0;}
                    return null;
                }
                float scale=order[scaleIndex];
                ImagePattern[] icons;
                if(!lockboxPatterns.TryGetValue(scale,out icons))continue;
                int themeCount=3;
                int themeBase=darkscapeInterface?3:0;
                if(icons.Length<themeBase+themeCount)continue;
                int firstIcon=n==0?iconFrom:0;
                if(firstIcon<0||firstIcon>=themeCount)firstIcon=0;
                for(int i=firstIcon;i<themeCount;i++)
                {
                    if(!complete&&clock.ElapsedMilliseconds>=budgetMs){lockScaleCursor=scaleIndex;lockIconCursor=i;return null;}
                    foreach(Point point in icons[themeBase+i].Find(pixels,all,40))
                    {
                        if(!LockboxSurface(pixels,point,scale))continue;
                        DetectedPuzzle best=null;
                        foreach(Point phase in new[]{new Point(0,0),new Point(-1,-1),new Point(0,-1),new Point(1,-1),new Point(-1,0),new Point(1,0),new Point(-1,1),new Point(0,1),new Point(1,1)})
                        {
                            Rectangle grid=new Rectangle(point.X+phase.X,point.Y+phase.Y,(int)Math.Round(182*scale),(int)Math.Round(182*scale));
                            Rectangle bounds=Rectangle.Intersect(Rectangle.Inflate(grid,8,8),all);
                            DetectedPuzzle candidate=ReadLockboxGrid(image,grid,scale,bounds,0.90);
                            if(candidate!=null&&candidate.Confidence>=0.95){if(!complete){lockScaleCursor=0;lockIconCursor=0;}return candidate;}
                            if(candidate!=null && (best==null || candidate.Confidence>best.Confidence)) best=candidate;
                        }
                        if(best!=null){if(!complete){lockScaleCursor=0;lockIconCursor=0;}return best;}
                    }
                    SearchCancellation.Check();
                }
            }
            if(!complete){lockScaleCursor=0;lockIconCursor=0;}
            return null;
        }
        private List<KeyValuePair<int, double[]>> ActiveLockboxReferences()
        {
            return darkscapeInterface ? darkscapeLockboxReferences : modernLockboxReferences;
        }
        private bool LockboxSurface(Pixels p,Point origin,float scale)
        {
            int size=(int)Math.Round(182*scale);
            if(origin.X<0||origin.Y<0||origin.X+size>p.Width||origin.Y+size>p.Height)return false;
            foreach(int cell in new[]{24,4,20})
            {
                Rectangle tile=new Rectangle(origin.X+(int)Math.Round(cell%5*38*scale),origin.Y+(int)Math.Round(cell/5*38*scale),(int)Math.Round(30*scale),(int)Math.Round(30*scale));
                double[] fp=p.Fingerprint(tile,6,6,false);double low=255,high=0;
                for(int i=0;i<36;i++){double value=(fp[i*3]+fp[i*3+1]+fp[i*3+2])/3;low=Math.Min(low,value);high=Math.Max(high,value);}
                if(high-low<30||!ActiveLockboxReferences().Any(r=>Pixels.Similarity(fp,r.Value,false)>=0.80))return false;
            }
            return true;
        }
        public DetectedPuzzle ReadTowers(Bitmap image, Rectangle grid, float scale, bool allowPartial = false)
        {
            LastTowerCluesRead=0;
            int pad = (int)Math.Round(35 * scale);
            Rectangle area = Rectangle.Inflate(grid, pad, pad);
            using (Bitmap normalised = Pixels.Crop(image, area, new Size(288, 288)))
            {
                if (normalised == null) return null;
                Pixels p = new Pixels(normalised);
                // The pale grid lines of scenery are not dark blue puzzle cells.
                int dark=0;
                for(int cell=0;cell<25;cell++) foreach(Point offset in new[]{new Point(10,10),new Point(8,21),new Point(10,31),new Point(31,31)})
                {
                    Color colour=p.Get(35+cell%5*44+offset.X,35+cell/5*44+offset.Y);
                    if(Pixels.TowerCell(colour)) dark++;
                }
                if(dark<85) return null;
                TowerPuzzle puzzle = new TowerPuzzle { Top = new int[5], Bottom = new int[5], Left = new int[5], Right = new int[5] };
                // A thin DarkScape clue at a full-size client scores just under 0.68.
                double minimum=scale<0.72F ? 0.52 : 0.62;
                for (int i = 0; i < 5; i++)
                {
                    int centre = 35 + i * 44 + 21;
                    puzzle.Top[i] = digits.Read(p, new Point(centre, 18), true, minimum);
                    puzzle.Bottom[i] = digits.Read(p, new Point(centre, 270), true, minimum);
                    puzzle.Left[i] = digits.Read(p, new Point(18, centre), true, minimum);
                    puzzle.Right[i] = digits.Read(p, new Point(270, centre), true, minimum);
                }
                int readable = puzzle.Top.Concat(puzzle.Bottom).Concat(puzzle.Left).Concat(puzzle.Right).Count(v => v != 0);
                LastTowerCluesRead=readable;
                // A partial digit match is not evidence of a Towers puzzle.
                // Stone floors can contain a convincing grid and several glyph-like marks.
                // It is only remembered as a hint to re-read this area quickly.
                if (readable != 20) { if (readable >= 4) TowerGridCandidate = area; return null; }
                if (towerSolveKey != puzzle.Key)
                {
                    towerSolutions = TowerSolver.Solve(puzzle, false);
                    towerSolveKey = puzzle.Key;
                }
                if (towerSolutions.Count == 0) return null;
                for (int i = 0; i < 25; i++) puzzle.Cells[i] = digits.Read(p, new Point(35 + i % 5 * 44 + 21, 35 + i / 5 * 44 + 21), false);
                return new DetectedPuzzle { Kind = PuzzleKind.Towers, Bounds = area, Grid = grid, Scale = scale, Towers = puzzle, TowerSolutions = towerSolutions, Confidence = 1 };
            }
        }
        // A right-click menu covers some cells. Keep the last complete board
        // while the dark grid is still there, and return null once it is gone.
        private DetectedPuzzle HoldTowers(Bitmap image, DetectedPuzzle previous)
        {
            if(previous==null||previous.Towers==null||previous.TowerSolutions==null)return null;
            int pad=(int)Math.Round(35*previous.Scale);
            Rectangle area=Rectangle.Inflate(previous.Grid, pad, pad);
            if(!new Rectangle(Point.Empty, image.Size).Contains(area))return null;
            using(Bitmap normalised=Pixels.Crop(image, area, new Size(288, 288)))
            {
                if(normalised==null)return null;
                Pixels p=new Pixels(normalised);
                int dark=0;
                for(int cell=0;cell<25;cell++)
                {
                    Color colour=p.Get(35+cell%5*44+21, 35+cell/5*44+21);
                    if(Pixels.TowerCell(colour))dark++;
                }
                if(dark<8)return null;
                return new DetectedPuzzle{Kind=PuzzleKind.Towers,Bounds=area,Grid=previous.Grid,Scale=previous.Scale,Towers=previous.Towers,TowerSolutions=previous.TowerSolutions,Confidence=previous.Confidence};
            }
        }
        private static bool KnotFrameRow(Pixels pixels, int left, int right, int y)
        {
            return pixels.KnotDark(left - 3, y) && pixels.KnotDark(right + 3, y) && pixels.KnotPaper(left, y) && pixels.KnotPaper(right, y);
        }
        // The paper run on one row, starting from a pixel that was paper nearby.
        private static bool KnotLip(Pixels pixels, int y, int hint, out int left, out int right)
        {
            left = right = hint;
            if (y < 0 || y >= pixels.Height) return false;
            int x = hint;
            if (!pixels.KnotPaper(x, y))
            {
                int found = -1;
                for (int d = 1; d <= 24 && found < 0; d++)
                {
                    if (pixels.KnotPaper(hint - d, y)) found = hint - d;
                    else if (pixels.KnotPaper(hint + d, y)) found = hint + d;
                }
                if (found < 0) return false;
                x = found;
            }
            while (x > 1 && pixels.KnotPaper(x - 1, y)) x--;
            left = x;
            while (x < pixels.Width - 2 && pixels.KnotPaper(x + 1, y)) x++;
            right = x;
            int length = right - left + 1;
            return length >= 240 && length <= 1600;
        }
        private static bool KnotSpan(Pixels pixels, int left, int right, int y)
        {
            if (y < 0 || y >= pixels.Height || right <= left) return false;
            int hits = 0, n = 0;
            for (int x = left; x <= right; x += 8)
            {
                n++;
                if (pixels.KnotPaper(x, y)) hits++;
            }
            return n > 0 && hits * 10 >= n * 3;
        }
        private static bool KnotBorder(Pixels pixels, int left, int top, int right, int bottom)
        {
            int dark = 0, n = 0;
            for (int i = 1; i <= 5; i++)
            {
                int x = left + (right - left) * i / 6;
                int y = top + (bottom - top) * i / 6;
                if (pixels.KnotDark(x, top - 3)) dark++;
                n++;
                if (pixels.KnotDark(x, bottom + 3)) dark++;
                n++;
                if (pixels.KnotDark(left - 3, y)) dark++;
                n++;
                if (pixels.KnotDark(right + 3, y)) dark++;
                n++;
            }
            return n > 0 && dark * 3 >= n * 2;
        }
        private static bool KnotCentre(Pixels pixels, Rectangle body)
        {
            int ink = 0, n = 0;
            int x0 = body.Left + body.Width / 5, x1 = body.Right - body.Width / 5;
            int y0 = body.Top + body.Height / 5, y1 = body.Bottom - body.Height / 5;
            for (int y = y0; y < y1; y += 6) for (int x = x0; x < x1; x += 6)
            {
                n++;
                if (pixels.KnotInk(x, y)) ink++;
            }
            // Blank parchment has no coloured tracks. A knot's diamond does.
            return n > 0 && ink * 25 >= n;
        }
        // The sampled end of a run can sit a few pixels inside the parchment.
        // Walk out to the first non-paper pixel. A real knot has its dark frame there.
        private static bool KnotEdgeDark(Pixels pixels, int edge, int direction, int y)
        {
            for (int i = 1; i <= 16; i++)
            {
                int x = edge + direction * i;
                if (pixels.KnotPaper(x, y)) continue;
                return pixels.KnotDark(x, y);
            }
            return false;
        }
        internal DetectedPuzzle FindKnotBoard(Bitmap image, Pixels pixels, int budgetMs)
        {
            if (image == null || pixels == null) return null;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var seen = new List<Rectangle>();
            int yStart = knotScanY;
            if (yStart < 24 || yStart >= pixels.Height - 80) yStart = 24;
            int reads = 0, rows = 0;
            // The parchment is hundreds of pixels tall, so a coarse row step
            // still lands inside it. Unframed tan runs are skipped before any
            // rune read, so sand cannot eat the slice.
            for (int y = yStart; y < pixels.Height - 60; y += 12)
            {
                if ((++rows & 3) == 0)
                {
                    SearchCancellation.Check();
                    if (clock.ElapsedMilliseconds >= budgetMs) { knotScanY = y; return null; }
                }
                int runStart = -1, runLast = -1;
                for (int x = 8; x < pixels.Width; x += 8)
                {
                    bool on = pixels.KnotPaper(x, y);
                    if (on) { if (runStart < 0) runStart = x; runLast = x; }
                    if (runStart >= 0 && ((!on && x - runLast > 16) || x >= pixels.Width - 8))
                    {
                        while (runStart > 1 && pixels.KnotPaper(runStart - 1, y)) runStart--;
                        while (runLast < pixels.Width - 2 && pixels.KnotPaper(runLast + 1, y)) runLast++;
                        int length = runLast - runStart + 1;
                        if (length >= 240 && length <= 1600)
                        {
                            int spanLeft = runStart, spanRight = runLast, spanY = y;
                            bool framed = KnotEdgeDark(pixels, spanLeft, -1, spanY) && KnotEdgeDark(pixels, spanRight, 1, spanY);
                            // The full-width parchment can be only a few pixels
                            // tall, between these rows, once the info button and
                            // the arrows cut the line. A run that meets one frame
                            // is enough reason to look at the rows in between.
                            if (!framed && (KnotEdgeDark(pixels, runStart, -1, y) || KnotEdgeDark(pixels, runLast, 1, y)))
                            {
                                for (int dy = -8; dy <= 8 && !framed; dy++)
                                {
                                    if (dy == 0) continue;
                                    int yy = y + dy;
                                    int lipLeft, lipRight;
                                    if (yy < 24 || yy >= pixels.Height - 60) continue;
                                    if (!KnotLip(pixels, yy, runStart, out lipLeft, out lipRight)) continue;
                                    if (!KnotEdgeDark(pixels, lipLeft, -1, yy) || !KnotEdgeDark(pixels, lipRight, 1, yy)) continue;
                                    spanLeft = lipLeft; spanRight = lipRight; spanY = yy; framed = true;
                                }
                            }
                            if (!framed) { runStart = -1; continue; }
                            bool tried;
                            DetectedPuzzle found = ReadKnotSpan(image, pixels, spanLeft, spanRight, spanY, seen, out tried);
                            if (found != null) { knotScanY = 24; return found; }
                            if (tried) reads++;
                            if (reads >= 2 || clock.ElapsedMilliseconds >= budgetMs) { knotScanY = y; return null; }
                        }
                        runStart = -1;
                    }
                }
            }
            knotScanY = 24;
            return null;
        }
        private DetectedPuzzle ReadLockedKnot(Bitmap image, DetectedPuzzle previous)
        {
            if (previous == null) return null;
            Rectangle body = previous.Bounds;
            DetectedPuzzle read = ReadKnotBody(image, body);
            if (read != null) return read;
            // A few pixels of movement stay inside this crop. This is not a
            // search of the rest of the client.
            Rectangle slack = Rectangle.Intersect(Rectangle.Inflate(body, 16, 16), new Rectangle(Point.Empty, image.Size));
            if (slack.Width < 240 || slack.Height < 150 || slack == body) return null;
            return ReadKnotBody(image, slack);
        }
        private DetectedPuzzle ReadKnotSpan(Bitmap image, Pixels pixels, int runStart, int runLast, int y, List<Rectangle> seen, out bool tried)
        {
            tried = false;
            int top = y, bottom = y;
            while (top > 8 && KnotSpan(pixels, runStart, runLast, top - 4)) top -= 4;
            while (bottom < pixels.Height - 8 && KnotSpan(pixels, runStart, runLast, bottom + 4)) bottom += 4;
            while (top > 1 && KnotSpan(pixels, runStart, runLast, top - 1)) top--;
            while (bottom < pixels.Height - 2 && KnotSpan(pixels, runStart, runLast, bottom + 1)) bottom++;
            int width = runLast - runStart + 1, height = bottom - top + 1;
            // A wide diamond drops the paper percentage before the frame ends.
            // Follow the dark sides for the rest of the window.
            if (height > 0 && width / (float)height > 1.9F)
            {
                while (top > 1 && KnotFrameRow(pixels, runStart, runLast, top - 1)) top--;
                while (bottom < pixels.Height - 2 && KnotFrameRow(pixels, runStart, runLast, bottom + 1)) bottom++;
                height = bottom - top + 1;
            }
            if (width < 240 || height < 150) return null;
            float aspect = width / (float)height;
            if (aspect < 1.35F || aspect > 1.9F) return null;
            Rectangle body = new Rectangle(runStart, top, width, height);
            foreach (Rectangle prior in seen)
            {
                Rectangle hit = Rectangle.Intersect(prior, body);
                if (hit.Width > 0 && hit.Height > 0 && hit.Width * hit.Height * 2 > body.Width * body.Height) return null;
            }
            if (!KnotBorder(pixels, body.Left, body.Top, body.Right - 1, body.Bottom - 1)) return null;
            if (!KnotCentre(pixels, body)) return null;
            seen.Add(body);
            tried = true;
            return ReadKnotBody(image, body);
        }
        private DetectedPuzzle ReadKnotBody(Bitmap image, Rectangle body)
        {
            if (body.Width < 240 || body.Height < 150 || !new Rectangle(Point.Empty, image.Size).Contains(body)) return null;
            float scale = 1F;
            Size size = body.Size;
            if (body.Width < 420 || body.Width > 620)
            {
                scale = body.Width / 494F;
                size = new Size(494, Math.Max(180, (int)Math.Round(body.Height / scale)));
            }
            using (Bitmap crop = Pixels.Crop(image, body, size))
            {
                if (crop == null) return null;
                KnotPuzzle knot = knots.Read(crop);
                if (knot == null) return null;
                return new DetectedPuzzle { Kind = PuzzleKind.Knot, Bounds = body, Grid = body, Scale = scale, Knot = knot, Confidence = 0.9 };
            }
        }
        private DetectedPuzzle FindTowerGrid(Bitmap image, Pixels pixels, int budgetMs, bool complete)
        {
            List<Rectangle> examined = new List<Rectangle>();
            var clock=System.Diagnostics.Stopwatch.StartNew();
            int yStart=complete?25:towerScanY;
            if(yStart<25)yStart=25;
            for (int y = yStart; y < pixels.Height - 135; y++)
            {
                if ((y & 15) == 0)
                {
                    SearchCancellation.Check();
                    if(clock.ElapsedMilliseconds>=budgetMs){if(!complete)towerScanY=y;return null;}
                }
                int start = -1, lastWhite = -1;
                for (int x = 20; x < pixels.Width; x++)
                {
                    bool white = pixels.GridPixel(x,y);
                    if (white) { if (start < 0) start = x; lastWhite = x; }
                    if (start >= 0 && ((!white && x-lastWhite > 3) || x == pixels.Width-1))
                    {
                        int length = lastWhite - start + 1;
                        if (length >= 110 && length <= 640)
                        {
                            float scale = (length - 1) / 218F;
                            float nearest = scales.OrderBy(s => Math.Abs(s-scale)).First();
                            if (Math.Abs(scale-nearest) < 0.015F) scale = nearest;
                            Rectangle grid = new Rectangle(start, y, length - 1, length - 1);
                            if (grid.Bottom + (int)(30*scale) < pixels.Height && !examined.Any(r => Math.Abs(r.X-grid.X)<4 && Math.Abs(r.Y-grid.Y)<4 && Math.Abs(r.Width-grid.Width)<4))
                            {
                                  int blue=0;
                                  foreach(int cell in new[]{0,4,12,20,24})
                                  {Color c=pixels.Get(grid.X+(int)Math.Round((cell%5*44+10)*scale),grid.Y+(int)Math.Round((cell/5*44+10)*scale));if(Pixels.TowerCell(c))blue++;}
                                  if(blue<3){start=-1;continue;}
                                bool lines = true;
                                for (int row = 0; row <= 5 && lines; row++)
                                {
                                    int line = (int)Math.Round((length-1)*row/5.0);
                                    lines = GridLine(pixels, start,y+line,length,true,row==5 && scale<0.75F) && GridLine(pixels,start+line,y,length,false);
                                }
                                if (lines)
                                {
                                    examined.Add(grid);
                                    DetectedPuzzle result = ReadTowers(image,grid,scale); if (result != null) return result;
                                }
                            }
                        }
                        start = -1;
                    }
                }
            }
            if(!complete)towerScanY=25;
            return null;
        }
        private static bool GridLine(Pixels pixels, int x, int y, int length, bool horizontal, bool allowDim=false)
        {
            for (int offset=-2;offset<=2;offset++)
            {
                int hits=0,total=0;
                for(int i=2;i<length-2;i+=3)
                {
                    int px=x+(horizontal?i:offset),py=y+(horizontal?offset:i); total++;
                    bool onLine=pixels.GridPixel(px,py);
                    if(!onLine && allowDim) { Color colour=pixels.Get(px,py); int low=Math.Min(colour.R,Math.Min(colour.G,colour.B)),high=Math.Max(colour.R,Math.Max(colour.G,colour.B)); onLine=low>=45 && high-low<45; }
                    if(onLine) hits++;
                }
                if(total>0 && hits/(double)total>=0.68) return true;
            }
            return false;
        }
    }
}
