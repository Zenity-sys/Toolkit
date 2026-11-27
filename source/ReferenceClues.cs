// Reference clue data: Clue Trainer, Copyright (c) 2024 Lukas Gail, MIT.
// See THIRD_PARTY.txt and assets/data/LICENSE-ClueTrainer.txt.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace KavrosClueHelper
{
    public sealed class ReferenceClue
    {
        public MasterClue Card;
        public string Type;
        public string[] Text,Anagrams;
    }
    public sealed class ReferenceClueReader
    {
        public readonly List<ReferenceClue> Clues=new List<ReferenceClue>();
        private readonly NativeClueOcr ocr=new NativeClueOcr();
        private readonly Dictionary<int,MapSignature> maps=new Dictionary<int,MapSignature>();
        private readonly Dictionary<int,MapSignature> variants=new Dictionary<int,MapSignature>();
        private readonly List<KeyValuePair<string,Point>> lodestones=new List<KeyValuePair<string,Point>>();
        private static readonly Regex Coordinates=new Regex(@"^(\d{1,2}) degrees? (\d{1,2}) minutes? (north|south) (\d{1,2}) degrees? (\d{1,2}) minutes? (east|west)$",RegexOptions.CultureInvariant);
        private ulong cachedHash;private Size cachedSize;private Rectangle cachedPaper;private ReferenceClue cached;
        private bool hasCache;
        private readonly Dictionary<ulong,ReferenceClue> recent=new Dictionary<ulong,ReferenceClue>();
        private readonly Queue<ulong> recentOrder=new Queue<ulong>();
        public Action<string> Progress;
        public string LastText,LastError,MapScores;
        private static int Number(XElement e,string key,int fallback,ref bool ok)
        {
            XAttribute a=e.Attribute(key);
            if(a==null)return fallback;
            int value;
            if(Int32.TryParse(a.Value,NumberStyles.Integer,CultureInfo.InvariantCulture,out value))return value;
            ok=false;return fallback;
        }
        public ReferenceClueReader(string assets)
        {
            foreach(string row in File.ReadAllLines(Path.Combine(assets,"compass","positions.tsv")))
            {
                string[] cols=row.Split('\t');int x,y;
                if(cols.Length>=3&&cols[0].IndexOf("lodestone",StringComparison.OrdinalIgnoreCase)>=0&&Int32.TryParse(cols[1],out x)&&Int32.TryParse(cols[2],out y))lodestones.Add(new KeyValuePair<string,Point>(cols[0],new Point(x,y)));
            }
            foreach(XElement e in XDocument.Load(Path.Combine(assets,"data","reference-clues.xml")).Root.Elements("clue"))
            {
                try
                {
                    bool ok=true;
                    int id=Number(e,"id",0,ref ok),floor=Number(e,"level",0,ref ok),x=Number(e,"x",0,ref ok),y=Number(e,"y",0,ref ok),width=Number(e,"width",0,ref ok),height=Number(e,"height",0,ref ok);
                    if(!ok)throw new InvalidDataException("Skipped a reference clue with an unreadable number.");
                    var card=new MasterClue{Id=id,Tier=(string)e.Attribute("tier"),Reference=true,Location=(string)e.Attribute("heading"),Emote=(string)e.Attribute("action"),Travel=(string)e.Element("details"),Equipment=new string[0],Floor=floor,Area=new Rectangle(x,y,width,height)};
                    var c=new ReferenceClue{Card=card,Type=(string)e.Attribute("type"),Text=e.Elements("text").Select(t=>t.Value).ToArray(),Anagrams=e.Elements("anagram").Select(t=>t.Value).ToArray()};Clues.Add(c);
                    string file=Path.Combine(assets,"reference-maps",card.Id+".png");
                    if(c.Type=="map"&&File.Exists(file))using(Bitmap image=new Bitmap(file))
                    {var p=new Pixels(image);Rectangle paper=Papers(p,false).FirstOrDefault();if(!paper.IsEmpty){MapSignature s=MapSignature.Read(p,paper);if(s!=null)maps[card.Id]=s;}}
                    string variant=Path.Combine(assets,"reference-map-variants",card.Id+".png");
                    if(c.Type=="map"&&File.Exists(variant))using(Bitmap image=new Bitmap(variant))
                    {MapSignature s=MapSignature.Read(new Pixels(image),new Rectangle(Point.Empty,image.Size));if(s!=null)variants[card.Id]=s;}
                }
                catch(Exception ex){CrashLog.Write(ex);}
            }
        }
        private static List<Rectangle> Papers(Pixels p,bool trim=true)
        {
            bool ignored;
            return Papers(p,trim,out ignored);
        }
        private static List<Rectangle> Papers(Pixels p,bool trim,out bool scrollFirst)
        {
            scrollFirst=false;
            var scrolls=ParagraphVision.FindScrolls(p);
            var papers=ParagraphVision.FindPapers(p);
            for(int i=0;i<papers.Count;i++)
            {
                Rectangle r=papers[i];int top=r.Top,bottom=r.Bottom;
                while(top>0&&r.Bottom-top<r.Width&&PaperSides(p,r,top-1))top--;
                while(bottom<p.Height&&bottom-top<r.Width&&PaperSides(p,r,bottom))bottom++;
                papers[i]=Rectangle.FromLTRB(r.Left,top,r.Right,bottom);
            }
            for(int i=0;i<papers.Count;i++)for(int j=i+1;j<papers.Count;j++)
            {Rectangle a=papers[i],b=papers[j];if(Math.Abs(a.Left-b.Left)<20&&Math.Abs(a.Right-b.Right)<20&&Math.Max(a.Top,b.Top)-Math.Min(a.Bottom,b.Bottom)<Math.Min(a.Width,b.Width)/2){papers[i]=Rectangle.Union(a,b);papers.RemoveAt(j--);}}
            var candidates=new List<Rectangle>();
            foreach(Rectangle r in papers.OrderByDescending(box=>box.Width*box.Height))
            {
                candidates.Add(r);
                if(!trim)continue;
                int left=r.Left,right=r.Right;
                while(left<right-70&&!PaperColumn(p,r,left))left++;
                while(right>left+70&&!PaperColumn(p,r,right-1))right--;
                if(right-left>r.Width*.68&&(left!=r.Left||right!=r.Right))candidates.Add(Rectangle.FromLTRB(left,r.Top,right,r.Bottom));
            }
            var framed=new List<Rectangle>();
            var plain=new List<Rectangle>();
            foreach(Rectangle scroll in scrolls)
            {
                Rectangle sheet=TrimToPaper(p,scroll);
                if(sheet.Width>=180&&sheet.Height>=90)
                {
                    if(framed.Count==0)scrollFirst=true;
                    framed.Add(sheet);
                }
            }
            foreach(Rectangle r in candidates)
            {
                if(ParagraphVision.SkipClueSheet(r,p.Width))continue;
                if(!HasMysteriousFrame(p,r)){plain.Add(r);continue;}
                Rectangle sheet=TrimToPaper(p,r);
                bool same=false;
                foreach(Rectangle have in framed)if(SameSheet(have,sheet)){same=true;break;}
                if(!same)framed.Add(sheet);
            }
            framed.AddRange(plain);
            return framed;
        }
        static bool SameSheet(Rectangle a,Rectangle b)
        {
            Rectangle hit=Rectangle.Intersect(a,b);
            if(hit.Width<=0||hit.Height<=0)return false;
            int small=Math.Min(a.Width*a.Height,b.Width*b.Height);
            return small>0&&hit.Width*hit.Height>=small*0.55;
        }
        // The current clue window is a dark "MYSTERIOUS CLUE SCROLL" frame
        // around parchment. Scene roofs are the same colour, so a framed
        // sheet is read before any other beige region.
        private static bool HasMysteriousFrame(Pixels p,Rectangle r)
        {
            if(r.Width<220||r.Height<140||r.Top<8)return false;
            int band=0,bandN=0;
            int y0=Math.Max(0,r.Top-22);
            for(int x=r.Left+r.Width/6;x<r.Right-r.Width/6;x+=6)
            {
                bandN++;
                for(int y=y0;y<Math.Min(p.Height,r.Top+10);y++)
                {
                    Color c=p.Get(x,y);if((c.R+c.G+c.B)/3<48){band++;break;}
                }
            }
            int sides=0,sideN=0;
            for(int y=r.Top+r.Height/5;y<r.Bottom-r.Height/5;y+=8)
            {
                sideN++;
                // The frame is only 7-10px and can sit behind a bright bevel,
                // so look across that band instead of one pixel 8px outside.
                if(FrameSide(p,r.Left,y,true)&&FrameSide(p,r.Right,y,false))sides++;
            }
            return bandN>0&&band*1.0/bandN>=0.55&&sideN>0&&sides*1.0/sideN>=0.45;
        }
        static bool FrameSide(Pixels p,int edge,int y,bool left)
        {
            int x0=left?Math.Max(0,edge-18):Math.Max(0,edge-2);
            int x1=left?Math.Min(p.Width-1,edge+2):Math.Min(p.Width-1,edge+18);
            int dark=0;
            for(int x=x0;x<=x1;x+=2){Color c=p.Get(x,y);if((c.R+c.G+c.B)/3<80)dark++;}
            return dark>=2;
        }
        private static Rectangle TrimToPaper(Pixels p,Rectangle r)
        {
            int top=r.Top;
            while(top<r.Bottom-90&&!PaperRow(p,r,top))top++;
            return top==r.Top?r:Rectangle.FromLTRB(r.Left,top,r.Right,r.Bottom);
        }
        private static bool PaperRow(Pixels p,Rectangle r,int y)
        {
            int good=0,n=0;
            for(int x=r.Left+r.Width/8;x<r.Right-r.Width/8;x+=8)
            {n++;Color c=p.Get(x,y);if(c.R>155&&c.G>115&&c.B>65&&c.R-c.G>5&&c.R-c.G<90&&c.G-c.B>12&&c.G-c.B<100)good++;}
            return n>0&&good*1.0/n>=0.7;
        }
        public Rectangle? PrimaryPaper(Pixels p)
        {
            List<Rectangle> papers=Papers(p);
            if(papers.Count==0)return null;
            return papers[0];
        }
        private static bool PaperColumn(Pixels p,Rectangle r,int x)
        {
            int good=0;
            for(int i=1;i<12;i++){Color c=p.Get(x,r.Top+r.Height*i/12);if(c.R>150&&c.G>105&&c.B>55&&c.R-c.G>3&&c.R-c.G<95&&c.G-c.B>10&&c.G-c.B<105)good++;}
            return good>=8;
        }
        private static bool PaperSides(Pixels p,Rectangle r,int y)
        {
            int good=0;foreach(double f in new[]{0.06,0.10,0.90,0.94})
            {Color c=p.Get(r.Left+(int)(r.Width*f),y);if(c.R>155&&c.G>115&&c.B>65&&c.R-c.G>5&&c.R-c.G<90&&c.G-c.B>12&&c.G-c.B<100)good++;}
            return good>=3;
        }
        private static string Letters(string value)
        {
            char[] chars=Regex.Replace(value??"","[^a-z]","").ToCharArray();
            Array.Sort(chars);
            return new string(chars);
        }
        public static string Normalize(string text)
        {
            string t=Regex.Replace(text??"",@"\|(?=\s*[A-Za-z])","I").Replace("\uFB01","fi").Replace("\uFB02","fl").ToLowerInvariant().Replace('\u2019','\'');
            t=Regex.Replace(t,"[^a-z0-9]+"," ").Trim();
            return Regex.Replace(t,"(?<=[a-z])0(?=[a-z])","o");
        }
        static string KeepKnownWords(string text, IEnumerable<string> dictionary)
        {
            var known=new HashSet<string>(dictionary);
            var kept=new List<string>();
            foreach(string word in text.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries))
                if(known.Contains(word)||word.All(char.IsDigit))kept.Add(word);
            return kept.Count==0?text:String.Join(" ",kept.ToArray());
        }
        static bool LetterBagWindow(string normalized, string bag)
        {
            if(bag==null||bag.Length<4)return false;
            string[] words=normalized.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
            for(int i=0;i<words.Length;i++)
            {
                string joined="";
                for(int j=i;j<words.Length&&joined.Length<=bag.Length;j++)
                {
                    joined+=words[j];
                    if(joined.Length==bag.Length&&Letters(joined)==bag)return true;
                    if(joined.Length>bag.Length)break;
                }
            }
            return false;
        }
        static double WindowScore(string hay, string needle)
        {
            if(hay.Length==0||needle.Length==0)return 0;
            if(hay==needle||hay.Contains(needle))return 1;
            double best=Similarity(hay,needle);
            string[] words=hay.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
            string[] wanted=needle.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
            int count=wanted.Length;
            for(int width=Math.Max(1,count-1);width<=count+3;width++)
                for(int i=0;i+width<=words.Length;i++)
                {
                    var slice=new string[width];
                    Array.Copy(words,i,slice,0,width);
                    best=Math.Max(best,Similarity(String.Join(" ",slice),needle));
                }
            return best;
        }
        internal static double Similarity(string a,string b)
        {
            if(a==b)return 1;if(a.Length==0||b.Length==0)return 0;
            if(Math.Abs(a.Length-b.Length)>Math.Max(a.Length,b.Length)*0.25)return 0;
            int[] row=Enumerable.Range(0,b.Length+1).ToArray();
            for(int i=1;i<=a.Length;i++){int before=row[0];row[0]=i;for(int j=1;j<=b.Length;j++){int old=row[j];row[j]=Math.Min(Math.Min(row[j]+1,row[j-1]+1),before+(a[i-1]==b[j-1]?0:1));before=old;}}
            return 1-row[b.Length]/(double)Math.Max(a.Length,b.Length);
        }
        public ReferenceClue MatchText(string raw)
        {
            string text=Normalize(raw);
            string letters=Letters(text);
            // A bare anagram such as "R AK MI" is shorter than eight characters
            // after spaces are collapsed, so it still has to be matched.
            if(text.Length<8 && letters.Length<4)return null;
            var pieces=new List<string>();
            foreach(ReferenceClue clue in Clues)
            {
                foreach(string part in clue.Text)pieces.Add(part);
                foreach(string part in clue.Anagrams){pieces.Add(part);pieces.Add(Normalize(part).Replace(" ",""));}
            }
            var dictionary=pieces.SelectMany(t=>Normalize(t).Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries));
            string cleaned=KeepKnownWords(text,dictionary);
            double best=0,runner=0;ReferenceClue winner=null;
            foreach(ReferenceClue c in Clues)
            {
                if(c.Type=="map"||c.Card.Id==900001)continue;
                double score=c.Text.Max(t=>WindowScore(cleaned,Normalize(t)));
                if(c.Type=="coordinates")
                {
                    string nums=String.Join(",",Regex.Matches(text,@"\d+|north|south|east|west").Cast<Match>().Select(m=>m.Value.TrimStart('0').PadLeft(1,'0')));
                    bool exact=c.Text.Any(t=>nums==String.Join(",",Regex.Matches(Normalize(t),@"\d+|north|south|east|west").Cast<Match>().Select(m=>m.Value.TrimStart('0').PadLeft(1,'0'))));
                    if(!exact)score=0;
                }
                if(c.Type=="anagram")
                {
                    string flat=text.Replace(" ","");
                    double anagram=0;
                    foreach(string a in c.Anagrams)
                    {
                        string n=Normalize(a);
                        string nFlat=n.Replace(" ","");
                        // Spacing is not part of the anagram. "ARC OLINE" and
                        // "ARCOLINE" are the same name, including after the intro.
                        // A packed name has to be long enough that it is not an
                        // accidental piece of a normal sentence. An exact clue
                        // line still outranks a letter-bag hit.
                        bool packed=nFlat.Length>=6&&(flat==nFlat||flat.EndsWith(nFlat)||flat.Contains(nFlat));
                        if(nFlat.Length>0&&(text==n||text.EndsWith(" "+n)||packed))anagram=1;
                        else anagram=Math.Max(anagram,WindowScore(text,n));
                        if(anagram<1&&LetterBagWindow(text,Letters(n)))anagram=Math.Max(anagram,0.96);
                    }
                    if(anagram>=0.9)score=Math.Max(score,anagram);
                    else if(!flat.Contains("anagram"))score=0;
                }
                if(score>best){runner=best;best=score;winner=c;}else runner=Math.Max(runner,score);
            }
            bool confident=winner!=null&&(best>=0.90&&best-runner>=0.035||best>=0.80&&best-runner>=0.20);
            if(confident)return winner;
            Match coordinate=Coordinates.Match(text);
            if(!coordinate.Success||lodestones.Count==0)return null;
            int lat=Int32.Parse(coordinate.Groups[1].Value),lm=Int32.Parse(coordinate.Groups[2].Value);
            int lon=Int32.Parse(coordinate.Groups[4].Value),om=Int32.Parse(coordinate.Groups[5].Value);
            if(lm>=60||om>=60||lat>60||lon>60)return null;
            int x=2440+(coordinate.Groups[6].Value=="east"?1:-1)*(32*lon+(int)Math.Ceiling(om*8/15.0));
            int y=3161+(coordinate.Groups[3].Value=="north"?1:-1)*(32*lat+(int)Math.Ceiling(lm*8/15.0));
            if(x<2032||x>4079||y<2544||y>4079)return null;
            var start=lodestones.OrderBy(l=>Math.Pow(l.Value.X-x,2)+Math.Pow(l.Value.Y-y,2)).First();
            string dir=new[]{"east","north-east","north","north-west","west","south-west","south","south-east"}[((int)Math.Round(Math.Atan2(y-start.Value.Y,x-start.Value.X)*4/Math.PI)+8)%8];
            return new ReferenceClue{Type="coordinates",Card=new MasterClue{Id=-(x*8192+y+1),Tier="coordinate",Reference=true,Equipment=new string[0],Emote="Dig",Area=new Rectangle(x,y,1,1),Location="Dig "+dir+" of "+start.Key,Travel="Start at the "+start.Key+". The dig spot lies "+dir+" of the lodestone. Follow the pin on the surface map.\n\nCalculated tile "+x+", "+y+"."}};
        }
        private DetectedPuzzle Result(ReferenceClue clue,Rectangle paper)
        {return clue==null?null:new DetectedPuzzle{Kind=PuzzleKind.EmoteClue,Master=clue.Card,Bounds=paper,Scale=1,Confidence=0.96,Message=clue.Card.Tier+" "+clue.Type};}
        public DetectedPuzzle ReadSheet(Bitmap image,Pixels pixels,Rectangle scroll)
        {
            Rectangle paper=TrimToPaper(pixels,scroll);
            if(paper.Width<180||paper.Height<90)paper=scroll;
            if(ParagraphVision.SkipClueSheet(paper,pixels.Width,true))return null;
            return ReadPaper(image,pixels,paper);
        }
        public DetectedPuzzle Detect(Bitmap image,Pixels pixels)
        {
            bool scrollFirst;
            foreach(Rectangle paper in Papers(pixels,true,out scrollFirst).Take(3))
            {
                // FindScrolls already proved this is a titled sheet. The frame
                // can sit further out than the title ink, so do not re-test it
                // with the small-sheet cap.
                bool framed=scrollFirst||HasMysteriousFrame(pixels,paper);
                scrollFirst=false;
                if(ParagraphVision.SkipClueSheet(paper,pixels.Width,framed))continue;
                DetectedPuzzle found=ReadPaper(image,pixels,paper);
                if(found!=null)return found;
            }
            return null;
        }
        DetectedPuzzle ReadPaper(Bitmap image,Pixels pixels,Rectangle paper)
        {
            SearchCancellation.Check();ulong hash=pixels.Hash(paper);
            if(hasCache&&cached!=null&&cachedSize==image.Size&&cachedPaper==paper&&cachedHash==hash)return Result(cached,paper);
            ReferenceClue known;if(recent.TryGetValue(hash,out known)&&known!=null)return Result(known,paper);
            ReferenceClue found=null;
            MapSignature signature=MapSignature.Read(pixels,paper);
            if(signature!=null)
            {
                var ranked=maps.Concat(variants).GroupBy(m=>m.Key).Select(group=>new{Id=group.Key,Score=group.Max(m=>m.Value.Compare(signature))}).OrderByDescending(v=>v.Score).Take(2).ToArray();
                MapScores=paper+" "+signature.Info+" "+String.Join(" / ",ranked.Select(v=>v.Id+":"+v.Score.ToString("0.000")));
                if(ranked.Length>0&&ranked[0].Id!=900001&&ranked[0].Score>=0.83&&(ranked.Length<2||ranked[0].Score-ranked[1].Score>=0.07))found=Clues.First(c=>c.Card.Id==ranked[0].Id);
            }
            if(found==null)
            {
                if(Progress!=null)Progress("Reading clue...");
                int mx=Math.Max(4,paper.Width/10),my=Math.Max(4,paper.Height/8);
                Rectangle inner=new Rectangle(paper.X+mx,paper.Y+my,Math.Max(8,paper.Width-2*mx),Math.Max(8,paper.Height-2*my));
                double scale=Math.Min(2,2000.0/Math.Max(inner.Width,inner.Height));
                using(Bitmap crop=Pixels.Crop(image,inner,new Size((int)(inner.Width*scale),(int)(inner.Height*scale))))
                {LastText=ocr.Read(crop);LastError=ocr.Unavailable;found=MatchText(LastText);}
            }
            cachedHash=hash;cachedSize=image.Size;cachedPaper=paper;cached=found;hasCache=found!=null;
            if(found!=null){recent[hash]=found;recentOrder.Enqueue(hash);while(recentOrder.Count>24)recent.Remove(recentOrder.Dequeue());}
            return Result(found,paper);
        }
    }
    internal sealed class MapSignature
    {
        private double[] values;private double aspect; public string Info;
        public static MapSignature Read(Pixels p,Rectangle paper)
        {
            int w=paper.Width,h=paper.Height,left=w,right=0,top=h,bottom=0,count=0;
            bool[] ink=new bool[w*h];
            for(int y=h/9;y<h-h/9;y++)for(int x=w/9;x<w-w/9;x++)
            {Color c=p.Get(paper.X+x,paper.Y+y);if(c.R<145&&c.R>30&&c.G<133&&c.B<110&&c.R>=c.G&&c.R-c.G<55&&c.G>=c.B){ink[y*w+x]=true;left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);count++;}}
            if(count<250||right-left<35||bottom-top<35)return null;
            int width=right-left+1,height=bottom-top+1;var v=new double[48*36];
            for(int y=top;y<=bottom;y++)for(int x=left;x<=right;x++)if(ink[y*w+x])v[Math.Min(35,(y-top)*36/height)*48+Math.Min(47,(x-left)*48/width)]++;
            double cell=width*height/(48.0*36);for(int i=0;i<v.Length;i++)v[i]=Math.Min(1,v[i]/Math.Max(1,cell));
            var smooth=new double[v.Length];for(int y=1;y<35;y++)for(int x=1;x<47;x++)for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)smooth[y*48+x]+=v[(y+dy)*48+x+dx]*(dx==0?2:1)*(dy==0?2:1)/16;
            v=smooth;
            return new MapSignature{values=v,aspect=width/(double)height,Info=left+","+top+":"+width+"x"+height};
        }
        public double Compare(MapSignature b)
        {
            if(Math.Abs(Math.Log(aspect/b.aspect))>0.16)return 0;
            double best=0;
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
            {double dot=0,aa=0,bb=0;for(int y=1;y<35;y++)for(int x=1;x<47;x++){double a=values[y*48+x],v=b.values[(y+dy)*48+x+dx];dot+=a*v;aa+=a*a;bb+=v*v;}best=Math.Max(best,dot/Math.Max(1e-9,Math.Sqrt(aa*bb)));}
            return best;
        }
    }
}
