using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
namespace KavrosClueHelper
{
    public sealed class MasterClueReader
    {
        private readonly Dictionary<MasterClue,List<ParagraphVision.Line>> patterns=new Dictionary<MasterClue,List<ParagraphVision.Line>>();
        private readonly Dictionary<MasterClue,List<List<ParagraphVision.Line>>> variants=new Dictionary<MasterClue,List<List<ParagraphVision.Line>>>();
        private readonly LegacyMasterClueReader legacy;
        public string LastScores;
        public Action<string> Progress;
        public MasterClueReader(string assets)
        {
            legacy=new LegacyMasterClueReader(assets);
            foreach(MasterClue clue in MasterClues.All)
                {
                    var models=new List<List<ParagraphVision.Line>>();
                    foreach(string file in new[]{Path.Combine(assets,"master-text",clue.Id+".png")}.Concat(Directory.GetFiles(Path.Combine(assets,"master-text"),clue.Id+"-*.png")))
                        using(Bitmap image=new Bitmap(file))models.Add(ParagraphVision.Template(image));
                    patterns.Add(clue,models[0]);variants.Add(clue,models);
                }
        }
        public void UseDpi(int value){legacy.UseDpi(value);}
        public DetectedPuzzle Detect(Bitmap image,Pixels pixels,Rectangle? previous=null)
        {
            var papers=previous.HasValue?new List<Rectangle>{Rectangle.Intersect(previous.Value,new Rectangle(Point.Empty,image.Size))}:ParagraphVision.ClueSheets(pixels);
            foreach(Rectangle paper in papers)
            {
                if(paper.Width<180||paper.Height<90)continue;
                List<ParagraphVision.Line> lines=ParagraphVision.Read(pixels,paper);double best=0,second=0;MasterClue winner=null;
                if(lines.Count>=3&&Progress!=null)Progress("Reading clue...");
                var scores=new List<string>();
                foreach(var pair in patterns)
                {
                    double value=0;
                    foreach(var model in variants[pair.Key])for(int start=0;start+model.Count<=lines.Count;start++)value=Math.Max(value,ParagraphVision.Match(model,lines,start));
                    scores.Add(pair.Key.Id+":"+value.ToString("0.000"));
                    if(value>best){second=best;best=value;winner=pair.Key;}else second=Math.Max(second,value);
                }
                LastScores="lines="+lines.Count+" "+String.Join(" ",scores);
                if(winner!=null&&best>=0.78&&best-second>=0.045)
                    return new DetectedPuzzle{Kind=PuzzleKind.EmoteClue,Master=winner,Bounds=paper,Scale=1,Confidence=Math.Min(0.99,0.93+(best-0.80)*0.3),Message="Master emote clue"};
                SearchCancellation.Check();
            }
            // The exact matcher uses its own parchment regions. Scenery can
            // merge with the wider regions used by line normalisation, so a
            // failed line extraction must not suppress this independent read.
            {
                DetectedPuzzle precise=legacy.Detect(image,pixels,previous);
                if(precise!=null)
                {
                    if(precise.Confidence>=0.97)return precise;
                    Rectangle text=Rectangle.Intersect(Rectangle.Inflate(precise.Grid,5,5),new Rectangle(Point.Empty,image.Size));
                    List<ParagraphVision.Line> lines=ParagraphVision.Read(pixels,text);
                    if(variants[precise.Master].Any(model=>Enumerable.Range(0,Math.Max(0,lines.Count-model.Count+1)).Any(start=>ParagraphVision.CompleteRows(model,lines,start))))return precise;
                }
            }
            return null;
        }
    }
}

