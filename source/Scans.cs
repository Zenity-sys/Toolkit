using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
namespace KavrosClueHelper
{
    public sealed class ScanRegion
    {
        public int Id,BaseRange,Level;
        public string Name,Route;
        public string Tier="master";
        public List<PointF> Spots;
        public List<int> SpotLevels;
        public string[] Aliases;
        public int[] Levels {get{return SpotLevels==null?new[]{Level}:SpotLevels.Distinct().OrderBy(v=>v).ToArray();}}
        public string AssetKey {get{return Tier=="elite"?Id+"-"+Level:Id.ToString();}}
        public RectangleF MapBounds;
    }
    public sealed class ScanReading
    {
        public ScanRegion Region;
        public bool DifferentLevel;
        public bool Meerkat;
        public bool Outside;
        public static ScanReading FromText(ScanRegion region, string text)
        {
            string n=ReferenceClueReader.Normalize(text);
            var reading=new ScanReading{Region=region,DifferentLevel=n.Contains("different level"),Meerkat=n.Contains("meerkat"),Outside=n.Contains("too far")||n.Contains("nothing scans")};
            System.Text.RegularExpressions.Match paces=System.Text.RegularExpressions.Regex.Match(n,@"\b(\d{1,2}) pace");
            int value;
            if(region!=null&&paces.Success&&Int32.TryParse(paces.Groups[1].Value,out value)&&value==region.BaseRange+5)reading.Meerkat=true;
            return reading;
        }
    }
    internal static class ScanTables
    {
        public static bool TryBox(string text, out RectangleF box)
        {
            box = RectangleF.Empty;
            if (string.IsNullOrWhiteSpace(text)) return false;
            string[] parts = text.Trim().Split('\t');
            if (parts.Length < 4) return false;
            float left, bottom, right, top;
            if (!float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out left)) return false;
            if (!float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out bottom)) return false;
            if (!float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out right)) return false;
            if (!float.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out top)) return false;
            box = RectangleF.FromLTRB(left, bottom, right, top);
            return true;
        }
        public static bool TryInts(string text, int count, out int[] values)
        {
            values = null;
            if (string.IsNullOrWhiteSpace(text)) return false;
            string[] parts = text.Trim().Split('\t');
            if (parts.Length < count) return false;
            values = new int[count];
            for (int i = 0; i < count; i++) if (!int.TryParse(parts[i].Trim(), out values[i])) { values = null; return false; }
            return true;
        }
        public static List<PointF> ReadSpots(IEnumerable<string> lines)
        {
            var spots = new List<PointF>();
            if (lines == null) return spots;
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                string[] parts = line.Split('\t');
                int x, y;
                if (parts.Length < 2 || !int.TryParse(parts[0].Trim(), out x) || !int.TryParse(parts[1].Trim(), out y)) continue;
                spots.Add(new PointF(x, y));
            }
            return spots;
        }
        public static void ApplyRoutes(IList<ScanRegion> regions, IEnumerable<string> lines)
        {
            if (regions == null || lines == null) return;
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
                int tab = line.IndexOf('\t');
                if (tab <= 0) continue;
                int id;
                if (!int.TryParse(line.Substring(0, tab).Trim(), out id)) continue;
                for (int i = 0; i < regions.Count; i++)
                    if (regions[i] != null && regions[i].Id == id) { regions[i].Route = line.Substring(tab + 1).Replace("\\n", "\n"); break; }
            }
        }
    }
    public sealed class ScanVision
    {
        public Action<string> Progress;
        private sealed class Model {public ScanRegion Region;public float Scale;public ImagePattern Title,Level;public Size Source;public Point TitleOrigin;public Point[] Empty;}
        private readonly List<Model> models=new List<Model>();
        private string scanDir;
        private readonly NativeClueOcr ocr=new NativeClueOcr();
        private readonly ScanTitleReader titles;
        private Rectangle lastOcrArea;private ulong lastOcrHash;private DetectedPuzzle lastOcr;private bool ocrCached;
        public string LastText;
        public readonly List<ScanRegion> Regions=new List<ScanRegion>();
        private int dpi=96;
        private float targetScale;
        private bool added175;
        public bool? LegacyInterface;
        public void UseTarget(float value){targetScale=value;}
        public void UseDpi(int value)
        {
            if(value<96)value=96;
            dpi=value;
            if(dpi==96||added175)return;
            float target=targetScale>0.01F?targetScale:dpi/96F;
            float gap=Math.Abs(target-1.75F);
            if(gap<Math.Abs(target-1.5F)&&gap<Math.Abs(target-2F)){added175=true;AddScale(1.75F);}
        }
        private IEnumerable<Model> OrderedModels()
        {
            float aim=targetScale>0.01F?targetScale:(dpi==96?0F:dpi/96F);
            if(aim<=0F)return models;
            return models.OrderBy(m=>ScaleRank(m.Scale,aim)).ThenBy(m=>Math.Abs(m.Scale-aim)).ThenBy(m=>m.Scale);
        }
        private static int ScaleRank(float scale,float aim)
        {
            if(Math.Abs(scale-aim)<0.02F)return 0;
            if(Math.Abs(scale-1F)<0.02F)return 1;
            return 2;
        }
        private void AddScale(float scale)
        {
            if(string.IsNullOrEmpty(scanDir))return;
            foreach(int id in new[]{368,371})
            {
                ScanRegion region=null;
                for(int i=0;i<Regions.Count;i++)if(Regions[i].Id==id){region=Regions[i];break;}
                if(region==null)continue;
                try
                {
                    int[] meta;
                    if(!ScanTables.TryInts(File.ReadAllText(Path.Combine(scanDir,id+"-title.tsv")),4,out meta))continue;
                    using(Bitmap original=new Bitmap(Path.Combine(scanDir,id+"-title.png")))
                    using(Bitmap image=Pixels.Crop(original,new Rectangle(Point.Empty,original.Size),new Size((int)Math.Round(original.Width*scale),(int)Math.Round(original.Height*scale))))
                    using(Bitmap oldLevel=new Bitmap(Path.Combine(scanDir,id+"-level.png")))
                    using(Bitmap level=Pixels.Crop(oldLevel,new Rectangle(Point.Empty,oldLevel.Size),new Size((int)Math.Round(oldLevel.Width*scale),(int)Math.Round(oldLevel.Height*scale))))
                    {
                        Pixels mask=new Pixels(image);var empty=new List<Point>();
                        for(int yy=0;yy<image.Height;yy+=2)for(int xx=0;xx<image.Width;xx+=2)if(mask.Get(xx,yy).A<5)empty.Add(new Point(xx,yy));
                        models.Add(new Model{Region=region,Scale=scale,Title=new ImagePattern(image),Level=new ImagePattern(level),Source=new Size(meta[2],meta[3]),TitleOrigin=new Point(meta[0],meta[1]),Empty=empty.Where((q,i)=>i%Math.Max(1,empty.Count/24)==0).Take(24).ToArray()});
                    }
                }
                catch(Exception ex){CrashLog.Write(ex);}
            }
        }
        public ScanVision(string assets)
        {
            titles=new ScanTitleReader(assets);
            string dir=Path.Combine(assets,"scan");scanDir=dir;
            foreach(int id in new[]{368,371})
            {
                try
                {
                    RectangleF box; int[] meta;
                    if(!ScanTables.TryBox(File.ReadAllText(Path.Combine(dir,id+"-map.tsv")), out box)) continue;
                    if(!ScanTables.TryInts(File.ReadAllText(Path.Combine(dir,id+"-title.tsv")), 4, out meta)) continue;
                    var region=new ScanRegion{Id=id,Name=id==368?"Prifddinas":"Heart of Gielinor",BaseRange=id==368?30:49,Level=1,MapBounds=box,
                        Spots=ScanTables.ReadSpots(File.ReadAllLines(Path.Combine(dir,id+".tsv")))};
                    region.Aliases=new[]{region.Name};
                    Regions.Add(region);
                    foreach(float scale in new[]{1F,0.8F,1.05F,0.95F,1.25F,1.5F,2F,0.6F})
                    using(Bitmap original=new Bitmap(Path.Combine(dir,id+"-title.png")))
                    using(Bitmap image=Pixels.Crop(original,new Rectangle(Point.Empty,original.Size),new Size((int)Math.Round(original.Width*scale),(int)Math.Round(original.Height*scale))))
                    using(Bitmap oldLevel=new Bitmap(Path.Combine(dir,id+"-level.png")))
                    using(Bitmap level=Pixels.Crop(oldLevel,new Rectangle(Point.Empty,oldLevel.Size),new Size((int)Math.Round(oldLevel.Width*scale),(int)Math.Round(oldLevel.Height*scale))))
                    {
                        Pixels mask=new Pixels(image);var empty=new List<Point>();
                        for(int yy=0;yy<image.Height;yy+=2)for(int xx=0;xx<image.Width;xx+=2)if(mask.Get(xx,yy).A<5)empty.Add(new Point(xx,yy));
                        models.Add(new Model{Region=region,Scale=scale,Title=new ImagePattern(image),Level=new ImagePattern(level),Source=new Size(meta[2],meta[3]),TitleOrigin=new Point(meta[0],meta[1]),Empty=empty.Where((q,i)=>i%Math.Max(1,empty.Count/24)==0).Take(24).ToArray()});
                    }
                }
                catch(Exception ex){CrashLog.Write(ex);}
            }
            string elite=Path.Combine(assets,"data","elite-scans.xml");
            if(File.Exists(elite))foreach(XElement node in XDocument.Load(elite).Root.Elements("scan"))
            {
                try
                {
                    int id, range; float left, bottom, right, top;
                    if(!Int32.TryParse((string)node.Attribute("id"), out id) || !Int32.TryParse((string)node.Attribute("range"), out range)) continue;
                    if(!Single.TryParse((string)node.Attribute("left"), NumberStyles.Float, CultureInfo.InvariantCulture, out left)) continue;
                    if(!Single.TryParse((string)node.Attribute("bottom"), NumberStyles.Float, CultureInfo.InvariantCulture, out bottom)) continue;
                    if(!Single.TryParse((string)node.Attribute("right"), NumberStyles.Float, CultureInfo.InvariantCulture, out right)) continue;
                    if(!Single.TryParse((string)node.Attribute("top"), NumberStyles.Float, CultureInfo.InvariantCulture, out top)) continue;
                    var spotNodes=node.Elements("spot").ToArray();
                    var spots=new List<PointF>(); var levels=new List<int>(); bool spotsOk=spotNodes.Length>0;
                    foreach(XElement s in spotNodes)
                    {
                        int x, y, level;
                        if(!Int32.TryParse((string)s.Attribute("x"), out x) || !Int32.TryParse((string)s.Attribute("y"), out y) || !Int32.TryParse((string)s.Attribute("level"), out level)) { spotsOk=false; break; }
                        spots.Add(new PointF(x, y)); levels.Add(level);
                    }
                    if(!spotsOk) continue;
                    var region=new ScanRegion{Id=id,Tier="elite",Name=(string)node.Attribute("name"),BaseRange=range,
                        MapBounds=RectangleF.FromLTRB(left,bottom,right,top), Spots=spots, SpotLevels=levels};
                    region.Level=region.Levels[0];region.Aliases=Aliases(id,region.Name);Regions.Add(region);
                }
                catch(Exception ex){CrashLog.Write(ex);}
            }
            try { ScanTables.ApplyRoutes(Regions, File.ReadAllLines(Path.Combine(assets,"data","scan-routes.tsv"))); }
            catch(Exception ex){CrashLog.Write(ex);}
        }
        private static string[] Aliases(int id,string name)
        {
            var names=new Dictionary<int,string>{{36,"deep wilderness|wilderness"},{349,"desert"},{350,"isafdar|lletya"},{351,"varrock"},{352,"ardougne"},{353,"keldagrim"},{354,"menaphos"},{355,"piscatoris"},{356,"brimhaven dungeon"},{357,"taverley dungeon"},{358,"mos le harmless"},{359,"haunted woods"},{360,"kharazi jungle"},{361,"zanaris"},{362,"lumbridge swamp caves|caves beneath lumbridge swamp"},{363,"jatizso|neitiznot"},{364,"falador"},{365,"dorgesh kaan"},{366,"fremennik slayer dungeon|fremennik slayer dungeons"},{367,"wilderness crater|crater in the wilderness"}};
            string aliases;return (names.TryGetValue(id,out aliases)?aliases+"|":"").Split(new[]{'|'},StringSplitOptions.RemoveEmptyEntries).Concat(new[]{name}).ToArray();
        }
        private static bool ContainsRegion(string text,string alias)
        {
            string[] words=text.Replace("rn","m").Split(' '),expected=ReferenceClueReader.Normalize(alias).Replace("rn","m").Split(' ');
            for(int start=0;start+expected.Length<=words.Length;start++)
            {
                bool same=true;for(int i=0;i<expected.Length;i++)
                {string a=expected[i],b=words[start+i];if(a!=b&&(a.Length<5||b.Length<5||ReferenceClueReader.Similarity(a,b)<0.82)){same=false;break;}}
                if(same)return true;
            }
            return false;
        }
        public ScanRegion MatchEliteText(string raw)
        {
            string text=ReferenceClueReader.Normalize(raw);
            text=Regex.Replace(text,@"\b([0-9il])\s+([0-9il]) paces\b",m=>m.Groups[1].Value+m.Groups[2].Value+" paces");
            text=Regex.Replace(text,@"\b[0-9il]{1,2} paces\b",m=>m.Value.Replace('i','1').Replace('l','1'));
            if(!text.Contains("scan")||!text.Contains("range")||!text.Contains("orb")||!text.Contains("paces"))return null;
            var matches=Regions.Where(r=>Readable(r)&&RangeText(r,text)&&Named(r,text)).ToArray();
            return matches.Length==1?matches[0]:null;
        }
        internal ScanRegion MatchScrollTranscript(string raw)
        {
            ScanRegion exact=MatchEliteText(raw);if(exact!=null)return exact;
            string text=ReferenceClueReader.Normalize(raw);
            Match range=Regex.Match(text,@"\b([0-9il]{1,2})\s+pace[a-z]?\b");
            if(!range.Success)return MatchRegionOnly(text);
            int value;if(!Int32.TryParse(range.Groups[1].Value.Replace('i','1').Replace('l','1'),out value))return null;
            var matches=Regions.Where(r=>Readable(r)&&(r.BaseRange==value||r.BaseRange+5==value)&&Named(r,text)).ToArray();
            if(matches.Length==0)return MatchRegionOnly(text);
            return matches.Length==1?matches[0]:null;
        }
        private static bool Readable(ScanRegion r)
        {
            return r!=null&&r.Id!=358&&(r.Tier=="elite"||r.Tier=="master");
        }
        private static bool RangeText(ScanRegion r,string text)
        {
            return Regex.IsMatch(text,@"\b"+r.BaseRange+@" paces\b")||Regex.IsMatch(text,@"\b"+(r.BaseRange+5)+@" paces\b");
        }
        private static bool Named(ScanRegion r,string text)
        {
            if(r.Aliases!=null)foreach(string alias in r.Aliases)if(ContainsRegion(text,alias))return true;
            if(r.Id==367&&ContainsRegion(text,"wilderness volcano"))return true;
            if(r.Id==349&&ContainsRegion(text,"east of the elid and north of nardah"))return true;
            return false;
        }
        private static readonly string[] ScanStates={"orb glows","try scanning","right coordinate","in range of the coordinate","paces away"};
        private ScanRegion MatchRegionOnly(string text)
        {
            // A named scroll that says you are too far away, or quotes the orb
            // range, is still that scan. A place name on its own is not.
            bool tooFar=text.Contains("nothing scans")||text.Contains("range of this orb")||text.Contains("too far");
            if(!tooFar&&!ScanStates.Any(s=>text.Contains(s)))return null;
            int best=0;ScanRegion winner=null;bool tie=false;
            foreach(ScanRegion r in Regions.Where(Readable))
            {
                var names=(r.Aliases??new string[0]).Concat(r.Id==367?new[]{"wilderness volcano"}:new string[0]).Concat(r.Id==349?new[]{"east of the elid and north of nardah"}:new string[0]);
                int length=names.Where(a=>ContainsRegion(text,a)).Select(a=>ReferenceClueReader.Normalize(a).Length).DefaultIfEmpty(0).Max();
                if(length>best){best=length;winner=r;tie=false;}else if(length>0&&length==best)tie=true;
            }
            return tie?null:winner;
        }
        public DetectedPuzzle Find(Bitmap image,Pixels p){return Find(image,p,null,true);}
        public DetectedPuzzle Find(Bitmap image,Pixels p,Rectangle? clip){return Find(image,p,clip,true);}
        // finishElite is false when the caller still wants the compass read
        // before OCR. The default path never gives up on a box before OCR.
        internal DetectedPuzzle Find(Bitmap image,Pixels p,Rectangle? clip,bool finishElite)
        {
            if(image==null||p==null)return null;
            if(clip.HasValue)
            {
                DetectedPuzzle named=ReadTitleBox(p,clip.Value);
                if(named!=null)return named;
            }
            // Search each title once. Overlapping scenery regions otherwise
            // repeat the same white-pixel candidates hundreds of times.
            // A parchment box is searched on its own before the full window.
            List<Rectangle> areas=new List<Rectangle>();
            if(clip.HasValue)
            {
                Rectangle box=Rectangle.Intersect(clip.Value,new Rectangle(Point.Empty,image.Size));
                if(box.Width>=80&&box.Height>=80)areas.Add(box);
            }
            if(areas.Count==0)areas.Add(new Rectangle(Point.Empty,image.Size));
            // A parchment box is small, so scale 1.0 is tried before the Auto guess.
            IEnumerable<Model> ordered=clip.HasValue?models.OrderBy(m=>ScaleRank(m.Scale,1F)).ThenBy(m=>Math.Abs(m.Scale-1F)).ThenBy(m=>m.Scale):OrderedModels();
            foreach(Model model in ordered)
            {
            if(clip.HasValue&&Math.Abs(model.Scale-1F)>0.02F)continue;
            foreach(Rectangle area in areas)
            foreach(Point hit in model.Title.Find(p,area,8,35,q=>PaperAround(p,q,model)))
            {
                float s=model.Scale;Rectangle body=new Rectangle(hit.X-(int)Math.Round(model.TitleOrigin.X*s),hit.Y-(int)Math.Round(model.TitleOrigin.Y*s),(int)Math.Round(model.Source.Width*s),(int)Math.Round(model.Source.Height*s));
                body=Rectangle.Intersect(body,new Rectangle(Point.Empty,image.Size));
                if(body.Width<90||body.Height<120)continue;
                int paperPixels=0,samples=0;
                for(int yy=body.Top+body.Height/10;yy<body.Top+body.Height*9/10;yy+=4)
                for(int xx=body.Left+body.Width*17/100;xx<body.Left+body.Width*83/100;xx+=4)
                {Color c=p.Get(xx,yy);samples++;if(c.R>40&&c.G>35&&c.B>20&&c.B<165&&c.R-c.G>-10&&c.R-c.G<75&&c.G-c.B>9)paperPixels++;}
                if(paperPixels<samples*0.72)continue;
                if(Progress!=null)Progress("Reading scan clue...");
                int white=0;
                for(int y=hit.Y+(int)(25*s);y<Math.Min(body.Bottom,hit.Y+(int)(94*s));y++)
                for(int x=Math.Max(body.X,hit.X-(int)(50*s));x<Math.Min(body.Right,hit.X+(int)(135*s));x++)
                {Color c=p.Get(x,y);if(c.R>185&&c.G>185&&c.B>185&&Math.Abs(c.R-c.G)<25)white++;}
                if(white<65*s*s)continue;
                var reading=new ScanReading{Region=model.Region,DifferentLevel=model.Level.Find(p,body,1,35).Count>0,Outside=titles.Outside(p,body)};
                return new DetectedPuzzle{Kind=PuzzleKind.Scan,Scan=reading,Bounds=body,Scale=s,Confidence=0.95,Message="Scan clue"};
            }
            }
            if(clip.HasValue)return finishElite?ReadEliteBox(image,p,clip.Value):null;
            return ReadElite(image,p);
        }
        internal DetectedPuzzle ReadTitleBox(Pixels p,Rectangle box)
        {
            if(p==null||box.Width<40||box.Height<40)return null;
            ScanReading found=titles.Read(p,box,Regions);
            if(found==null)return null;
            return new DetectedPuzzle{Kind=PuzzleKind.Scan,Scan=found,Bounds=box,Scale=1,Confidence=.97,Message="Scan clue"};
        }
        internal DetectedPuzzle FindNativeTitle(Pixels p)
        {
            foreach(Rectangle area in ScrollAreas.Find(p,true,LegacyInterface)){
                ScanReading found=titles.Read(p,area,Regions);
                if(found!=null)return new DetectedPuzzle{Kind=PuzzleKind.Scan,Scan=found,Bounds=area,Scale=1,Confidence=.97};
            }
            return null;
        }
        private DetectedPuzzle ReadElite(Bitmap image,Pixels p)
        {
            if(!Regions.Any(Readable))return null;
            foreach(Rectangle area in ScrollAreas.Find(p,true,LegacyInterface).Take(3))
            {
                DetectedPuzzle found=ReadEliteBox(image,p,area);
                if(found!=null)return found;
            }
            return null;
        }
        internal DetectedPuzzle ReadEliteBox(Bitmap image,Pixels p,Rectangle area)
        {
            if(image==null||p==null||!Regions.Any(Readable))return null;
            area=Rectangle.Intersect(area,new Rectangle(Point.Empty,image.Size));
            if(area.Width<40||area.Height<40)return null;
            try
            {
                // One budget for every recognition attempt. A failure, including
                // a timeout, is no result. The UI tick does not cancel this call.
                Stopwatch clock=Stopwatch.StartNew();
                ulong hash=p.Hash(area);if(ocrCached&&hash==lastOcrHash&&area==lastOcrArea&&lastOcr!=null)return lastOcr;
                ScanReading title=titles.Read(p,area,Regions);
                if(title!=null)return new DetectedPuzzle{Kind=PuzzleKind.Scan,Scan=title,Bounds=area,Scale=1,Confidence=.97};
                int bright=0;for(int y=area.Top;y<area.Bottom;y+=2)for(int x=area.Left;x<area.Right;x+=2){Color c=p.Get(x,y);if(c.R>190&&c.G>190&&c.B>190&&Math.Abs(c.R-c.B)<35)bright++;}
                if(bright<30)return null;
                if(Progress!=null)Progress("Reading scan clue...");
                double scale=Math.Min(1,1600.0/Math.Max(area.Width,area.Height));
                using(Bitmap crop=Pixels.Crop(image,area,new Size((int)(area.Width*scale),(int)(area.Height*scale))))
                {
                    int left=OcrBudget(clock); if(left<50)return null;
                    LastText=ocr.Read(crop,left);string originalText=LastText;ScanRegion originalMatch=MatchScrollTranscript(LastText);
                    if(originalMatch!=null)
                    {lastOcrHash=hash;lastOcrArea=area;ocrCached=true;lastOcr=new DetectedPuzzle{Kind=PuzzleKind.Scan,Scan=ScanReading.FromText(originalMatch,LastText),Bounds=area,Scale=1,Confidence=0.96};return lastOcr;}
                    using(Bitmap large=Pixels.Crop(crop,new Rectangle(Point.Empty,crop.Size),new Size(crop.Width*2,crop.Height*2)))
                    {
                        left=OcrBudget(clock); if(left<50)return null;
                        LastText=ocr.Read(large,left);ScanRegion scaledMatch=MatchScrollTranscript(LastText);
                        if(scaledMatch!=null){lastOcrHash=hash;lastOcrArea=area;ocrCached=true;lastOcr=new DetectedPuzzle{Kind=PuzzleKind.Scan,Scan=ScanReading.FromText(scaledMatch,LastText),Bounds=area,Scale=1,Confidence=0.96};return lastOcr;}
                        originalText+=" UPSCALED: "+LastText;
                    }
                    // Scan scrolls use white text on tan paper. Isolate the ink
                    // before OCR so the parchment texture cannot become letters.
                    var cp=new Pixels(crop);using(Graphics g=Graphics.FromImage(crop))g.Clear(Color.White);
                    for(int y=0;y<crop.Height;y++)for(int x=0;x<crop.Width;x++){Color c=cp.Get(x,y);if(c.R>185&&c.G>185&&c.B>185&&Math.Max(c.R,Math.Max(c.G,c.B))-Math.Min(c.R,Math.Min(c.G,c.B))<45)crop.SetPixel(x,y,Color.Black);}
                    left=OcrBudget(clock); if(left<50)return null;
                    LastText=ocr.Read(crop,left);string maskedText=LastText;ScanRegion region=MatchScrollTranscript(LastText);
                    if(region==null)LastText="ORIGINAL: "+originalText+" MASK: "+maskedText;lastOcrHash=hash;ocrCached=true;lastOcr=region==null?null:new DetectedPuzzle{Kind=PuzzleKind.Scan,Scan=ScanReading.FromText(region,originalText+" "+maskedText),Bounds=area,Scale=1,Confidence=0.96};
                    if(lastOcr!=null)return lastOcr;
                }
            }
            catch(Exception ex)
            {
                CrashLog.Write(ex);
                return null;
            }
            return null;
        }
        private static int OcrBudget(Stopwatch clock)
        {
            int left=2500-(int)clock.ElapsedMilliseconds;
            return left<1?1:left;
        }
        private static bool PaperAround(Pixels p,Point q,Model model)
        {
            float s=model.Scale;int left=q.X-(int)(model.TitleOrigin.X*s),top=q.Y-(int)(model.TitleOrigin.Y*s),good=0;
            Color above=p.Get(q.X+model.Title.Size.Width/2,q.Y-Math.Max(2,(int)Math.Round(5*s)));
            if(above.B>=170||above.G-above.B<8||above.R<70)return false;
            foreach(Point sample in new[]{new Point(65,55),new Point(model.Source.Width-65,55),new Point(75,model.Source.Height-65),new Point(model.Source.Width-75,model.Source.Height-65),new Point(model.Source.Width/2,45),new Point(model.Source.Width/2,model.Source.Height-45)})
            {
                Color c=p.Get(left+(int)(sample.X*s),top+(int)(sample.Y*s));
                if(c.R>90&&c.G>75&&c.B>35&&c.R-c.G>-8&&c.R-c.G<75&&c.G-c.B>10)good++;
            }
            if(good<3)return false;
            int wrong=0;
            foreach(Point empty in model.Empty){Color c=p.Get(q.X+empty.X,q.Y+empty.Y);if(c.R>185&&c.G>185&&c.B>185&&++wrong>model.Empty.Length/4)return false;}
            return true;
        }
    }
    public sealed class ScanSession
    {
        public ScanRegion Region;
        public PointF? Position;
        public PointF? LastKnown;
        public bool Meerkat,MeerkatLocked,DifferentLevel,Outside;
        public bool AutoTracking=true,PositionTracked;
        public const string ManualHint="Right-click your spot on the map.";
        public string TrackingStatus=ManualHint;
        private MinimapMatch pending;
        private MinimapMatch farFix;
        private DateTime farAt;
        private DateTime lastMatch;
        private DateTime lastConfirmed;
        private int confirmations;
        private int misses;
        // Recent movement, used to estimate where the player is while the
        // minimap cannot be matched for a few frames.
        private readonly MotionEstimate motion=new MotionEstimate();
        private readonly DetectionHold trackHold=new DetectionHold();
        private static readonly TimeSpan LostTime=TimeSpan.FromSeconds(2);
        public const int LostFrames=8;
        public const string FindingHint="Finding your position on the minimap...";
        public const string LostHint="Grey marker shows your last position. Finding you again automatically.";
        public const string OutsideNote="Too far away for this scan.";
        // The too-far line is a note on top of the normal status. It does not replace tracking.
        public string Noted(string status)
        {
            if(!Outside||string.IsNullOrEmpty(status))return status??"";
            if(status.IndexOf(OutsideNote,StringComparison.Ordinal)>=0)return status;
            return OutsideNote+" "+status;
        }
        public string FloorLine()
        {
            string status=AutoTracking?Noted(TrackingStatus):Position.HasValue?"Manual position set. Right-click the map to move it.":"Right-click your position on the map.";
            return (DifferentLevel?"Different level. Check the marked floor. ":"")+"Map level "+(Region==null?0:Region.Level)+". "+status;
        }
        public readonly HashSet<int> Visited=new HashSet<int>();
        public void NoteCoverage()
        {
            if(Region==null||!PositionTracked||!Position.HasValue)return;
            for(int i=0;i<Region.Spots.Count;i++) if(InRange(Region.Spots[i])) Visited.Add(i);
        }
        public int Range {get{return Region==null?0:Region.BaseRange+(Meerkat?5:0);}}
        public void Observe(ScanReading reading)
        {
            bool changed=Region==null||reading.Region==null||Region.Id!=reading.Region.Id;
            if(changed)
            {
                Position=null;LastKnown=null;PositionTracked=false;pending=null;farFix=null;confirmations=0;misses=0;motion.Reset();trackHold.Reset();                MeerkatLocked=false;
                Visited.Clear();
                Meerkat=reading.Meerkat;
                TrackingStatus=AutoTracking?FindingHint:ManualHint;
            }
            else if(!MeerkatLocked&&reading.Meerkat)Meerkat=true;
            Region=reading.Region;DifferentLevel=reading.DifferentLevel;Outside=reading.Outside;
        }
        public bool InRange(PointF spot,int factor=1){return (!AutoTracking||PositionTracked)&&!DifferentLevel&&Position.HasValue&&Math.Max(Math.Abs(spot.X-Position.Value.X),Math.Abs(spot.Y-Position.Value.Y))<=Range*factor;}
        public void Clear(){Region=null;Position=null;LastKnown=null;DifferentLevel=false;Outside=false;PositionTracked=false;pending=null;farFix=null;motion.Reset();trackHold.Reset();Visited.Clear();}
        public void SetAutomatic(bool enabled){AutoTracking=enabled;Position=null;LastKnown=null;PositionTracked=false;pending=null;farFix=null;misses=0;motion.Reset();trackHold.Reset();TrackingStatus=enabled?FindingHint:"Right-click your spot on the map.";}
        public void SetManual(PointF position){SetAutomatic(false);Position=position;TrackingStatus="Manual position set. Right-click the map to move it.";}
        public void PrepareRecheck(){if(LastKnown.HasValue)return;Position=null;PositionTracked=false;pending=null;TrackingStatus="Minimap found. Checking your current position...";}
        public void ApplyMatch(MinimapMatch match,DateTime now)
        {
            if(!AutoTracking)return;
            if(match==null)
            {
                farFix=null;
                if(PositionTracked&&LastKnown.HasValue)
                {
                    // Keep following through missed frames, moving the marker
                    // along the recent heading. Only several misses in a row,
                    // over a couple of seconds, count as lost.
                    trackHold.Miss(now);
                    bool lost=(trackHold.Misses>=LostFrames&&(now-lastMatch)>=LostTime)||(now-lastMatch).TotalSeconds>=12;
                    if(!lost)
                    {
                        PointF? estimate=motion.Estimate(now);
                        Position=estimate.HasValue?estimate:LastKnown;
                        TrackingStatus="Following your minimap automatically.";return;
                    }
                    PositionTracked=false;Position=null;pending=null;confirmations=0;misses=trackHold.Misses;
                    TrackingStatus=LostHint;return;
                }
                confirmations=0;
                PositionTracked=false;
                if((now-lastMatch).TotalMilliseconds>650)Position=null;
                misses++;
                TrackingStatus=LastKnown.HasValue?LostHint:FindingHint;
                return;
            }
            trackHold.Seen();
            if(PositionTracked&&LastKnown.HasValue)
            {
                double gap=Math.Max(0.05,(now-lastMatch).TotalSeconds);
                double allowed=14+gap*24;
                if(CompassMath.Distance(match.Position,LastKnown.Value)>allowed)
                {
                    // One far reading is kept. The same far spot on the next
                    // frame is a teleport, so the marker locks there.
                    bool again=farFix!=null&&(now-farAt).TotalSeconds<2&&CompassMath.Distance(match.Position,farFix.Position)<=18;
                    if(!again)
                    {
                        farFix=match;farAt=now;
                        Position=LastKnown;TrackingStatus="Following your minimap automatically.";
                        return;
                    }
                    farFix=null;pending=match;lastMatch=now;misses=0;confirmations=2;
                    Position=match.Position;LastKnown=match.Position;PositionTracked=true;lastConfirmed=now;
                    motion.Reset();motion.Observe(match.Position,now);
                    TrackingStatus="Following your minimap automatically.";
                    NoteCoverage();
                    return;
                }
                farFix=null;
                PointF held=LastKnown.Value;
                var smoothed=new PointF(held.X+(match.Position.X-held.X)*0.7F,held.Y+(match.Position.Y-held.Y)*0.7F);
                pending=match;lastMatch=now;misses=0;Position=smoothed;LastKnown=smoothed;PositionTracked=true;lastConfirmed=now;
                motion.Observe(smoothed,now);
                TrackingStatus="Following your minimap automatically.";
                NoteCoverage();
                return;
            }
            misses=0;
            if(LastKnown.HasValue&&CompassMath.Distance(match.Position,LastKnown.Value)<=20+Math.Max(0,(now-lastMatch).TotalSeconds)*MotionEstimate.MaxSpeed)
            {
                // Back where we lost the player (or a short run from there):
                // pick up again at once, without a confirmation frame.
                farFix=null;pending=match;lastMatch=now;confirmations=2;Position=match.Position;LastKnown=match.Position;PositionTracked=true;lastConfirmed=now;
                motion.Reset();motion.Observe(match.Position,now);
                TrackingStatus="Following your minimap automatically.";
                NoteCoverage();
                return;
            }
            bool consistent=pending!=null&&(now-lastMatch).TotalSeconds<2&&CompassMath.Distance(match.Position,pending.Position)<=18;
            confirmations=consistent?confirmations+1:1;consistent=consistent&&confirmations>=2;
            pending=match;lastMatch=now;
            PositionTracked=consistent;Position=consistent?(PointF?)match.Position:null;
            if(consistent){LastKnown=match.Position;lastConfirmed=now;motion.Reset();motion.Observe(match.Position,now);}
            TrackingStatus=consistent?"Following your minimap automatically.":"Position found. Confirming the next frame...";
            NoteCoverage();
        }
        public void Expire(DateTime now)
        {if(AutoTracking&&Position.HasValue&&(now-lastMatch).TotalSeconds>12){Position=null;PositionTracked=false;pending=null;TrackingStatus=LastKnown.HasValue?LostHint:FindingHint;}}

        public string Summary {get{return Region==null?"":Region.Name+" | "+Range+" paces"+(Meerkat?" (meerkat)":"")+" | Dig locations: level "+Region.Level+"\n"+FloorLine()+(AutoTracking&&!PositionTracked&&LastKnown.HasValue?"\nGrey marker: last confirmed "+Math.Max(0,(int)(DateTime.UtcNow-lastConfirmed).TotalSeconds)+"s ago.":"");}}

    }
    public sealed class ScanMap : Control
    {
        private readonly string assets;private Bitmap background;private int loaded;
        private readonly List<CompassPosition> lodestones=new List<CompassPosition>();
        private float zoom=1;
        private PointF pan;
        private Point? drag;
        private Point mouseDown;
        public readonly ScanSession Session;
        public event Action PositionChanged;
        public ScanMap(string directory,ScanSession session)
        {
            assets=directory;Session=session;DoubleBuffered=true;TabStop=true;
            string file=Path.Combine(directory,"compass","positions.tsv");
            if(File.Exists(file))foreach(string line in File.ReadAllLines(file))
            {
                string[] p=line.Split('\t');int x,y;
                if(p.Length>=3&&Int32.TryParse(p[1],out x)&&Int32.TryParse(p[2],out y)&&x>=2032&&x<=4079&&y>=2544&&y<=4079)
                    lodestones.Add(new CompassPosition{Name=LodestoneNames.Clean(p[0]),X=x,Y=y});
            }
        }
        private RectangleF View
        {
            get {
                RectangleF r=Session.Region.MapBounds;
                float aspect=Math.Max(1,Width)/(float)Math.Max(1,Height);
                float w=Math.Max(r.Width,r.Height*aspect)/zoom,h=w/aspect;
                float limitX=Math.Max(0,(r.Width-w)/2),limitY=Math.Max(0,(r.Height-h)/2);
                pan.X=Math.Max(-limitX,Math.Min(limitX,pan.X));
                pan.Y=Math.Max(-limitY,Math.Min(limitY,pan.Y));
                return new RectangleF(r.Left+(r.Width-w)/2+pan.X,r.Top+(r.Height-h)/2+pan.Y,w,h);
            }
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);if(Session.Region==null)return;Focus();
            if(e.Button==MouseButtons.Left){mouseDown=e.Location;drag=e.Location;Capture=true;}
            if(e.Button==MouseButtons.Right){Session.SetManual(World(e.Location));if(PositionChanged!=null)PositionChanged();Invalidate();}
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);if(!drag.HasValue||Session.Region==null)return;
            RectangleF view=View;
            pan.X-=(e.X-drag.Value.X)*view.Width/Math.Max(1,Width);
            pan.Y+=(e.Y-drag.Value.Y)*view.Height/Math.Max(1,Height);
            drag=e.Location;Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            bool click=drag.HasValue&&Math.Abs(e.X-mouseDown.X)+Math.Abs(e.Y-mouseDown.Y)<5;
            drag=null;Capture=false;
            if(!click||e.Button!=MouseButtons.Left||Session.Region==null)return;
            CompassPosition stone=HitLodestone(e.Location);
            if(stone==null)return;
            Session.SetManual(new PointF(stone.X,stone.Y));
            if(PositionChanged!=null)PositionChanged();
            Invalidate();
        }
        private CompassPosition HitLodestone(Point point)
        {
            CompassPosition best=null;float bestDistance=14;
            foreach(CompassPosition stone in lodestones)
            {
                PointF p=ScreenPoint(stone.Point);
                float distance=Math.Abs(p.X-point.X)+Math.Abs(p.Y-point.Y);
                if(distance<bestDistance){bestDistance=distance;best=stone;}
            }
            return best;
        }
        protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)drag=null;}
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);if(Session.Region==null)return;
            PointF anchor=World(e.Location);
            zoom=(float)Math.Max(0.15,Math.Min(8,zoom*Math.Pow(1.35,e.Delta/120.0)));
            PointF after=World(e.Location);pan.X+=anchor.X-after.X;pan.Y+=anchor.Y-after.Y;Invalidate();
        }
        private PointF ScreenPoint(PointF p){RectangleF r=View;return new PointF((p.X-r.Left)/r.Width*Width,Height-(p.Y-r.Top)/r.Height*Height);}
        private PointF World(Point p){RectangleF r=View;return new PointF((float)Math.Round(r.Left+p.X*r.Width/Width),(float)Math.Round(r.Top+(Height-p.Y)*r.Height/Height));}
        public void RefreshMap(){Invalidate();}
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g=e.Graphics;g.Clear(Color.FromArgb(18,30,38));if(Session.Region==null)return;
            ScanRegion region=Session.Region;
            int mapId=region.Id*4+region.Level;
            if(loaded!=mapId){if(background!=null)background.Dispose();using(Bitmap loadedImage=new Bitmap(Path.Combine(assets,"scan",region.AssetKey+".png")))background=Pixels.Crop(loadedImage,new Rectangle(Point.Empty,loadedImage.Size),loadedImage.Size);loaded=mapId;zoom=1;pan=PointF.Empty;drag=null;}
            PointF tl=ScreenPoint(new PointF(region.MapBounds.Left,region.MapBounds.Bottom)),br=ScreenPoint(new PointF(region.MapBounds.Right,region.MapBounds.Top));
            g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
            Bitmap world=WorldMap.Get(assets);
            PointF worldTop=ScreenPoint(new PointF(2032,4080)),worldBottom=ScreenPoint(new PointF(4080,2544));
            g.DrawImage(world,RectangleF.FromLTRB(worldTop.X,worldTop.Y,worldBottom.X,worldBottom.Y));
            g.DrawImage(background,RectangleF.FromLTRB(tl.X,tl.Y,br.X,br.Y));
            PointF? displayed=Session.Position??(Session.AutoTracking?Session.LastKnown:null);
            bool oldPosition=Session.AutoTracking&&!Session.PositionTracked;
            if(displayed.HasValue)
            {
                PointF centre=displayed.Value;
                foreach(int factor in new[]{2,1})
                {
                    PointF a=ScreenPoint(new PointF(centre.X-Session.Range*factor,centre.Y+Session.Range*factor)),b=ScreenPoint(new PointF(centre.X+Session.Range*factor,centre.Y-Session.Range*factor));
                    RectangleF box=RectangleF.FromLTRB(a.X,a.Y,b.X,b.Y);
                    using(Pen pen=new Pen(Session.AutoTracking&&!Session.PositionTracked?Color.Silver:factor==1?Color.Lime:Color.Orange,2)){if(factor==2||Session.AutoTracking&&!Session.PositionTracked)pen.DashStyle=DashStyle.Dash;g.DrawRectangle(pen,box.X,box.Y,box.Width,box.Height);}
                    if(factor==1)using(Brush fill=new SolidBrush(oldPosition?Color.FromArgb(20,170,170,170):Color.FromArgb(30,40,255,100)))g.FillRectangle(fill,box);
                }

            }
            for(int i=0;i<region.Spots.Count;i++){if(region.SpotLevels!=null&&region.SpotLevels[i]!=region.Level)continue;PointF spot=region.Spots[i],p=ScreenPoint(spot);bool here=Session.InRange(spot);bool seen=Session.Visited.Contains(i)&&!here;g.FillEllipse(Brushes.Black,p.X-5,p.Y-5,10,10);g.FillEllipse(seen?Brushes.Gray:here?Brushes.Lime:Session.InRange(spot,2)?Brushes.Orange:Brushes.Cyan,p.X-3,p.Y-3,6,6);}
            // Keep the player above candidate markers, including at a dig spot.
            foreach(CompassPosition stone in lodestones)
            {
                PointF p=ScreenPoint(stone.Point);
                if(p.X<-16||p.Y<-16||p.X>Width+16||p.Y>Height+16)continue;
                LodestoneIcons.Draw(g,p,stone.Name,false,assets);
            }
            if(displayed.HasValue){PointF p=ScreenPoint(displayed.Value);g.FillEllipse(Brushes.Black,p.X-7,p.Y-7,14,14);g.FillEllipse(oldPosition?Brushes.Silver:Brushes.Yellow,p.X-4,p.Y-4,8,8);g.DrawEllipse(Pens.White,p.X-6,p.Y-6,12,12);}
            LodestoneIcons.DrawCompass(g,Font,new Rectangle(0,0,Width,Height),assets);
        }
        protected override void Dispose(bool disposing){if(disposing&&background!=null)background.Dispose();base.Dispose(disposing);}
    }
}

