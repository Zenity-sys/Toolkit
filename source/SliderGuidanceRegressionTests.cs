using System;using System.Collections.Generic;using System.Drawing;using System.IO;using System.Linq;using System.Threading;
namespace KavrosClueHelper
{
 public static class SliderGuidanceRegressionTests
 {
  private static int checks;private static void Check(bool pass,string message){checks++;if(!pass)throw new Exception("FAILED: "+message);}
  public static int Run(string report)
  {
   try
   {
    string assets=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets");var engine=new PuzzleAnalysisEngine(assets);var detector=new PuzzleDetector(assets);
    engine.UseLegacy(true);detector.UseLegacy(true);
    using(var native=new Bitmap(Path.Combine(assets,"test_assets","reported","slider-native.png")))
    using(AnalysisResult baseline=engine.Analyze(native,150,false,CancellationToken.None,true))
    {
     Check(baseline.Plan!=null&&baseline.Plan.Puzzle.Kind==PuzzleKind.Sliding,"native slider remains recognizable with badge-safe features");
     var puzzle=baseline.Plan.Puzzle;var route=baseline.Plan.Sliding;Check(route!=null&&route.Count>4,"slider has a route for preview");
     var fresh=engine.ReadOnlySlider(native,puzzle);Check(fresh!=null&&fresh.Board.SequenceEqual(puzzle.Board),"fresh-frame validation reads the current board without replanning");
     Check(baseline.Plan.Sliding.SequenceEqual(route),"fresh-frame read leaves the active route intact");
     var stepWatch=System.Diagnostics.Stopwatch.StartNew();
     using(var animation=(Bitmap)native.Clone())
     {
      using(Graphics g=Graphics.FromImage(animation))for(int i=0;i<25;i++)g.FillRectangle(Brushes.Black,GuidanceDrawing.Cell(puzzle,i));
      using(AnalysisResult pending=engine.Analyze(animation,150,false,CancellationToken.None,true))
      {
       stepWatch.Stop();
       File.AppendAllText(report,"click -> next step shown (held through the slide): "+stepWatch.ElapsedMilliseconds+" ms\r\n");
       Check(pending.Pending&&pending.Plan!=null,"a framed animation retains the existing guide");
       Check(stepWatch.ElapsedMilliseconds<500,"the held slider step returns before the clue readers ("+stepWatch.ElapsedMilliseconds+" ms)");
      }
     }
     using(AnalysisResult recovered=engine.Analyze(native,150,false,CancellationToken.None,true))
     {
      Check(!recovered.Pending&&recovered.Plan!=null,"settled tiles resume immediately");
      File.AppendAllText(report,"click -> next step shown (settled re-read): "+recovered.Milliseconds+" ms\r\n");
     }
     detector.UseDpi(120);
     detector.UseLegacy(true);
     using(Bitmap modern=TestFixtures.SliderImage("castle",Enumerable.Range(0,25).Select(i=>i==23?24:i==24?23:i).ToArray(),"rev944"))
     using(Bitmap scaled=Pixels.ScaleNearest(modern,1.25F))
     {
      DetectedPuzzle at125=detector.Detect(scaled,true,true);
      Check(at125!=null&&at125.Kind==PuzzleKind.Sliding&&Math.Abs(at125.Scale-1.25F)<0.02F,"modern slider at Windows 125% is read at scale 1.25");
     }
     detector.UseDpi(96);
     string shot=Path.Combine(assets,"test_assets","reported","slider-2560x1351-dpi120.png");
     Check(File.Exists(shot),"125% legacy slider screenshot is installed");
     using(Bitmap reported=new Bitmap(shot))
     using(Bitmap restored=Pixels.ScaleNearest(reported,2560F/reported.Width))
     {
      detector.UseDpi(120);detector.ResetSearch();detector.UseLegacy(true);
      var watch=System.Diagnostics.Stopwatch.StartNew();
      DetectedPuzzle legacy=detector.Detect(restored,true,false);
      watch.Stop();
      File.AppendAllText(report,"125% legacy slider: "+(legacy==null?"not found":legacy.Kind+" scale "+legacy.Scale)+" in "+watch.ElapsedMilliseconds+" ms\r\n");
      Check(legacy!=null&&legacy.Kind==PuzzleKind.Sliding,"legacy slider at Windows 125% is detected");
      Check(PuzzleDetector.ResolutionBucket(restored.Height)==1440,"the 125% screenshot window is 1440p");
      detector.UseDpi(96);detector.UseLegacy(false);
     }
     using(var painted=(Bitmap)native.Clone())
     {
      using(Graphics g=Graphics.FromImage(painted))GuidanceDrawing.Draw(g,GuidanceDrawing.SliderMarks(puzzle,route));
      var read=detector.ReadAgain(painted,puzzle);Check(read!=null&&read.Board.SequenceEqual(puzzle.Board),"visible four-step badges do not change tile reading");
      using(AnalysisResult result=engine.Analyze(painted,150,false,CancellationToken.None,true))Check(result.Plan!=null&&result.Plan.Sliding.SequenceEqual(route),"overlay pixels do not restart or change the route");
     }
     using(var animation=(Bitmap)native.Clone())
     {
      using(Graphics g=Graphics.FromImage(animation))for(int i=0;i<25;i++)g.FillRectangle(Brushes.Black,GuidanceDrawing.Cell(puzzle,i));
      using(AnalysisResult pending=engine.Analyze(animation,150,false,CancellationToken.None,true))Check(pending.Pending&&pending.Plan!=null,"a framed animation retains the existing guide");
      using(AnalysisResult recovered=engine.Analyze(native,150,false,CancellationToken.None,true))Check(!recovered.Pending&&recovered.Plan!=null,"settled tiles resume immediately");
     }
     int[] board=(int[])puzzle.Board.Clone();
     using(var reference=new Bitmap(Path.Combine(assets,"data","sliders","sliders",puzzle.Theme+".png")))
     using(Bitmap blank=native.Clone(GuidanceDrawing.Cell(puzzle,Array.IndexOf(board,24)),System.Drawing.Imaging.PixelFormat.Format24bppRgb))
     for(int step=0;step<12;step++)
     {
      int move=route[step],empty=Array.IndexOf(board,24);board[empty]=board[move];board[move]=24;
      using(var frame=(Bitmap)native.Clone())
      {
       using(Graphics g=Graphics.FromImage(frame))for(int i=0;i<25;i++){if(board[i]==24)g.DrawImage(blank,GuidanceDrawing.Cell(puzzle,i));else g.DrawImage(reference,GuidanceDrawing.Cell(puzzle,i),new Rectangle(board[i]%5*49,board[i]/5*49,49,49),GraphicsUnit.Pixel);}
       var shown=GuidanceDrawing.SliderMarks(puzzle,route.Skip(step).ToList(),step+1);
       if(shown.Count>0){int cell=route[step];int gap=Array.IndexOf(board,24);shown[0].Colour=KavrosTheme.Gold;shown[0].Pulse=true;shown[0].ArrowX=gap%5-cell%5;shown[0].ArrowY=gap/5-cell/5;}
       using(Graphics g=Graphics.FromImage(frame))GuidanceDrawing.Draw(g,shown);
       using(Bitmap captured=step%2==0?frame.Clone(Rectangle.Intersect(new Rectangle(Point.Empty,frame.Size),Rectangle.Inflate(puzzle.Grid,60,60)),System.Drawing.Imaging.PixelFormat.Format24bppRgb):(Bitmap)frame.Clone())
       using(AnalysisResult result=engine.Analyze(captured,150,false,CancellationToken.None,true))
       {
        Check(result.Plan!=null&&!result.Pending&&result.Plan.Puzzle.Board.SequenceEqual(board),"each move is read beneath the still-visible overlay");
        Check(result.Plan.Sliding.SequenceEqual(route.Skip(step+1)),"each move advances the cached route");
        var next=GuidanceDrawing.SliderMarks(result.Plan.Puzzle,result.Plan.Sliding,step+2);Check(next.Count==4&&next[0].Text==(step+2).ToString(),"rolling preview replenishes four numbered steps");
       }
      }
     }
     Check(engine.SlidingRouteReuses>=12,"route survives alternating full/cropped captures without replanning");
     var pulse=new GuidanceMark{Box=new Rectangle(80,80,49,49),Text="1",Colour=KavrosTheme.Gold,Sliding=true,Pulse=true,ArrowX=1};
     var cover=new System.Collections.Generic.List<Rectangle>();GuidanceDrawing.AddOpaque(pulse,cover);
     Check(cover.Any(r=>r.Contains(70,100)),"the pulse cover includes the full ring");
     Check(cover.Any(r=>r.Contains(80+49+20,80+24)),"the arrow cover includes the gold shaft");
     using(AnalysisResult settled=engine.Analyze(native,150,false,CancellationToken.None,true))Check(settled.Plan!=null&&!settled.Pending,"the settled slider is readable before the hold expires");
     using(var closed=new Bitmap(native.Width,native.Height))
     {
      using(AnalysisResult soon=engine.Analyze(closed,150,false,CancellationToken.None,true))Check(soon.Pending&&soon.Plan!=null,"a slider that just closed keeps its step briefly");
      System.Threading.Thread.Sleep(1600);
      using(AnalysisResult gone=engine.Analyze(closed,150,false,CancellationToken.None,true))Check(gone.Plan==null&&!gone.Pending,"closing the actual puzzle clears guidance once the hold expires");
     }
     TimeCapture(report,"legacy",native,assets);
     WholeRowAdvance();
     CaptureOffsetMask(assets);
     TimeRealLoop(report,"legacy",native,assets);
    }
    detector.UseLegacy(true);
    foreach(float scale in new[]{0.8F,1F,1.25F,2F})
    using(Bitmap original=TestFixtures.SliderImage("castle",Enumerable.Range(0,25).Select(i=>i==23?24:i==24?23:i).ToArray(),"rev944"))
    using(Bitmap frame=Pixels.Crop(original,new Rectangle(Point.Empty,original.Size),new Size((int)Math.Round(original.Width*scale),(int)Math.Round(original.Height*scale))))
    {
     var puzzle=detector.Detect(frame);Check(puzzle!=null&&puzzle.Kind==PuzzleKind.Sliding,"slider fixture at scale "+scale);
     var marks=GuidanceDrawing.SliderMarks(puzzle,new[]{23,18,17,22});using(Graphics g=Graphics.FromImage(frame))GuidanceDrawing.Draw(g,marks);
     var read=detector.ReadAgain(frame,puzzle);Check(read!=null&&read.Board.SequenceEqual(puzzle.Board),"badges stay readable to the detector at scale "+scale);
    }
    Check(!GuidanceDrawing.HideForCapture(false,PuzzleKind.Sliding),"slider stays visible in screenshots");
    detector.UseLegacy(false);
    using(Bitmap modernOnly=TestFixtures.SliderImage("castle",Enumerable.Range(0,25).Select(i=>i==23?24:i==24?23:i).ToArray(),"modern"))
     Check(detector.Detect(modernOnly)!=null&&detector.Detect(modernOnly).Kind==PuzzleKind.Sliding,"Modern mode reads a modern slider");
    detector.UseLegacy(true);
    using(Bitmap legacyOnly=TestFixtures.SliderImage("castle",Enumerable.Range(0,25).Select(i=>i==23?24:i==24?23:i).ToArray(),"rev944"))
     Check(detector.Detect(legacyOnly)!=null&&detector.Detect(legacyOnly).Kind==PuzzleKind.Sliding,"Legacy mode reads a legacy slider");
    detector.UseLegacy(false);
    int[] modernBoard=Enumerable.Range(0,25).Select(i=>i==23?24:i==24?23:i).ToArray();
    using(Bitmap modern=TestFixtures.SliderImage("castle",modernBoard,"modern"))TimeCapture(report,"modern",modern,assets);
    int[] scrambled=Enumerable.Range(0,25).ToArray();
    var rng=new Random(88);
    for(int n=0;n<48;n++)
    {
     int blankAt=Array.IndexOf(scrambled,24);var options=SlidingSolver.Neighbours(blankAt).ToArray();int pick=options[rng.Next(options.Length)];
     scrambled[blankAt]=scrambled[pick];scrambled[pick]=24;
    }
    using(Bitmap modernLong=TestFixtures.SliderImage("castle",scrambled,"modern"))TimeRealLoop(report,"modern",modernLong,assets);
    TimeScrolls(report,assets);
    TimeFirstGuide(report,assets);
    float[] dpi125=PuzzleDetector.SearchOrder(1369,120);
    Check(Math.Abs(dpi125[0]-1.75F)<0.02F&&Math.Abs(dpi125[1]-1F)<0.02F,"1440p at 125% tries scale 1.75 then 1.0");
    Check(Math.Abs(PuzzleDetector.SearchOrder(1369,96)[0]-1.25F)<0.02F,"1440p at 100% starts at 1.25");
    DarkscapeSlider(assets,"darkscape-slider-snow.png","archers");
    DarkscapeSlider(assets,"darkscape-slider-house.png","v");
    File.AppendAllText(report,"V0.5.5 SLIDER GUIDANCE PASS: "+checks+" assertions. Visible badges, twelve consecutive moves, rolling four-step previews, animation retention, closure and scaled tile reading. No windows shown or game input/capture.\r\n");return 0;
   }
   catch(Exception e){File.AppendAllText(report,e+"\r\n");return 1;}
  }
  static void DarkscapeSlider(string assets,string file,string theme)
  {
   string path=Path.Combine(assets,"test_assets","darkscape",file);
   using(Bitmap image=new Bitmap(path))
   {
    var liveDetector=new PuzzleDetector(assets);
    using(AnalysisResult live=new PuzzleAnalysisEngine(assets).Analyze(image,1,false,CancellationToken.None,true))
     SliderSolved(live,liveDetector,image,file+" live",theme);
    var stillDetector=new PuzzleDetector(assets);
    using(AnalysisResult still=new PuzzleAnalysisEngine(assets).Analyze(image,1,false,CancellationToken.None,false))
     SliderSolved(still,stillDetector,image,file+" still",theme);
   }
  }
  static void SliderSolved(AnalysisResult result,PuzzleDetector detector,Bitmap image,string name,string theme)
  {
   Check(result.Plan!=null&&result.Plan.Puzzle!=null&&result.Plan.Puzzle.Kind==PuzzleKind.Sliding,name+" detects the slider");
   if(result.Plan==null||result.Plan.Puzzle==null)return;
   DetectedPuzzle puzzle=result.Plan.Puzzle;
   Check(puzzle.Theme==theme,name+" matches "+theme+" and saw "+puzzle.Theme);
   Check(result.Plan.Sliding!=null&&result.Plan.Sliding.Count>0,name+" has a route");
   Check(detector.SliderFrameVisible(image,puzzle),name+" frame stays visible");
   if(puzzle.Board==null||result.Plan.Sliding==null)return;
   int[] board=(int[])puzzle.Board.Clone();
   foreach(int move in result.Plan.Sliding)
   {
    int blank=Array.IndexOf(board,24);
    Check(blank>=0&&Math.Abs(blank/5-move/5)+Math.Abs(blank%5-move%5)==1,name+" route stays adjacent");
    board[blank]=board[move];board[move]=24;
   }
   Check(board.SequenceEqual(Enumerable.Range(0,25)),name+" route solves the board");
  }
  static void TimeCapture(string report,string label,Bitmap source,string assets)
  {
   var engine=new PuzzleAnalysisEngine(assets);
   if(label=="legacy")engine.UseLegacy(true);
   using(Bitmap opening=(Bitmap)source.Clone())
   using(AnalysisResult found=engine.Analyze(opening,180,false,CancellationToken.None,true))
   {
    Check(found.Plan!=null&&found.Plan.Puzzle.Kind==PuzzleKind.Sliding,label+" slider is read before the click");
    DetectedPuzzle puzzle=found.Plan.Puzzle;
    Check(found.Plan.Sliding!=null&&found.Plan.Sliding.Count>0,label+" slider has a route");
    int move=found.Plan.Sliding[0];
    int[] board=(int[])puzzle.Board.Clone();
    int empty=Array.IndexOf(board,24);
    var stale=GuidanceDrawing.SliderMarks(puzzle,new[]{move},1,1);
    stale[0].Colour=KavrosTheme.Gold;stale[0].Pulse=true;stale[0].ArrowX=empty%5-move%5;stale[0].ArrowY=empty/5-move/5;
    using(Bitmap reference=new Bitmap(Path.Combine(assets,"data","sliders","sliders",puzzle.Theme+".png")))
    using(Bitmap blank=source.Clone(GuidanceDrawing.Cell(puzzle,empty),System.Drawing.Imaging.PixelFormat.Format24bppRgb))
    {
     var watch=System.Diagnostics.Stopwatch.StartNew();
     long settled=0;
     foreach(double t in new[]{0.33,0.66,1.0})
     {
      using(Bitmap frame=(Bitmap)source.Clone())
      {
       using(Graphics g=Graphics.FromImage(frame))
       {
        DrawSlide(g,puzzle,board,move,t,reference,blank);
        GuidanceDrawing.Draw(g,stale);
       }
       GuideInk ink=GuideInk.ForSlider(frame.Width,frame.Height,stale,Point.Empty);
       var frameWatch=System.Diagnostics.Stopwatch.StartNew();
       using(AnalysisResult result=engine.Analyze(frame,180,false,CancellationToken.None,true,false,null,96,ink))
       {
        frameWatch.Stop();
        if(t<1)
        {
         Check(result.Plan!=null,"the guide stays up while the tile is sliding ("+label+")");
        }
        else
        {
         settled=frameWatch.ElapsedMilliseconds;
         int[] next=(int[])board.Clone();next[empty]=next[move];next[move]=24;
         Check(result.Plan!=null&&!result.Pending&&result.Plan.Puzzle.Board.SequenceEqual(next),label+" settled frame is read through the drawn guide");
         List<int> remaining;
         Check(SlidingRoute.Advance(board,found.Plan.Sliding,next,out remaining),label+" the known route advances");
        }
       }
      }
     }
     watch.Stop();
     File.AppendAllText(report,label+" click -> next step (guide drawn, capture mask, 3-frame slide): "+watch.ElapsedMilliseconds+" ms; settled frame "+settled+" ms\r\n");
     Check(watch.ElapsedMilliseconds<800,label+" click reaches the next step without a full rescan ("+watch.ElapsedMilliseconds+" ms)");
    }
   }
  }
  static void DrawSlide(Graphics g,DetectedPuzzle puzzle,int[] from,int move,double t,Bitmap reference,Bitmap blank)
  {
   int empty=Array.IndexOf(from,24);
   Rectangle fromCell=GuidanceDrawing.Cell(puzzle,move),toCell=GuidanceDrawing.Cell(puzzle,empty);
   for(int i=0;i<25;i++)
   {
    Rectangle cell=GuidanceDrawing.Cell(puzzle,i);
    if(i==move||i==empty)g.DrawImage(blank,cell);
    else g.DrawImage(reference,cell,new Rectangle(from[i]%5*49,from[i]/5*49,49,49),GraphicsUnit.Pixel);
   }
   int tile=from[move];
   int x=(int)Math.Round(fromCell.X+(toCell.X-fromCell.X)*t),y=(int)Math.Round(fromCell.Y+(toCell.Y-fromCell.Y)*t);
   g.DrawImage(reference,new Rectangle(x,y,fromCell.Width,fromCell.Height),new Rectangle(tile%5*49,tile/5*49,49,49),GraphicsUnit.Pixel);
  }
  static void WholeRowAdvance()
  {
   int[] goal=Enumerable.Range(0,25).ToArray();
   int[] board=(int[])goal.Clone();
   board[21]=24;board[22]=21;board[23]=22;board[24]=23;
   var route=new List<int>{22,23,24};
   List<int> followed;int consumed;int slow=SlidingClickOptimizer.SlowSolves;
   Check(SliderStep.Follow(board,route,goal,null,out followed,out consumed),"a whole-row step is the next board on the route");
   Check(consumed==3&&followed.Count==0,"the whole row is consumed without a new plan");
   Check(!SlidingSolver.Neighbours(21).Contains(24),"the blank moved more than one cell");
   Check(SlidingClickOptimizer.SlowSolves==slow,"whole-row advance does not solve");
  }
  static void CaptureOffsetMask(string assets)
  {
   using(Bitmap image=TestFixtures.SliderImage("castle",Enumerable.Range(0,25).Select(i=>i==23?24:i==24?23:i).ToArray(),"rev944"))
   {
    var detector=new PuzzleDetector(assets);
    detector.UseLegacy(true);
    DetectedPuzzle puzzle=detector.Detect(image);
    Check(puzzle!=null&&puzzle.Kind==PuzzleKind.Sliding,"offset fixture is a slider");
    int move=Array.IndexOf(puzzle.Board,24)==24?23:Array.IndexOf(puzzle.Board,23);
    var marks=GuidanceDrawing.SliderMarks(puzzle,new[]{move},1,1);
    marks[0].Colour=KavrosTheme.Gold;marks[0].Pulse=true;marks[0].ArrowX=1;
    Point captureOffset=new Point(640,280);
    foreach(GuidanceMark mark in marks)mark.Box=new Rectangle(mark.Box.X+captureOffset.X,mark.Box.Y+captureOffset.Y,mark.Box.Width,mark.Box.Height);
    GuideInk missed=GuideInk.ForSlider(image.Width,image.Height,marks,Point.Empty);
    GuideInk covered=GuideInk.ForSlider(image.Width,image.Height,marks,captureOffset);
    int x=marks[0].Box.X-captureOffset.X+3,y=marks[0].Box.Y-captureOffset.Y+3;
    Check(x>=0&&y>=0&&x<image.Width&&y<image.Height,"the badge sits on the cropped capture");
    Check(missed==null||!missed.Ignores(x,y),"a mask without CaptureOffset misses the guide");
    Check(covered!=null&&covered.Ignores(x,y),"a mask with CaptureOffset covers the guide");
   }
  }
  static void TimeRealLoop(string report,string label,Bitmap source,string assets)
  {
   int slow=SlidingClickOptimizer.SlowSolves;
   var engine=new PuzzleAnalysisEngine(assets);
   if(label=="legacy")engine.UseLegacy(true);
   var times=new List<long>();
   bool wholeRow=false;
   using(Bitmap scene=new Bitmap(source.Width+360,source.Height+240))
   {
    using(Graphics g=Graphics.FromImage(scene)){g.Clear(Color.FromArgb(24,28,32));g.DrawImage(source,180,120);}
    using(AnalysisResult found=engine.Analyze((Bitmap)scene.Clone(),150,false,CancellationToken.None,true,true,Point.Empty,96,null))
    {
     Check(found.Plan!=null&&found.Plan.Puzzle.Kind==PuzzleKind.Sliding,label+" slider is read on the full frame");
     Check(SlidingClickOptimizer.SlowSolves==slow,label+" the first route is shown without the slow solver");
     DetectedPuzzle puzzle=found.Plan.Puzzle;
     int[] board=(int[])puzzle.Board.Clone();
     var route=new List<int>(found.Plan.Sliding);
     List<int> clicks;
     Check(SlidingRoute.TryClickRoute(board,route,out clicks)&&clicks.Count>0,label+" route is playable");
     int gap=Array.IndexOf(board,24);
     using(Bitmap reference=new Bitmap(Path.Combine(assets,"data","sliders","sliders",puzzle.Theme+".png")))
     using(Bitmap blank=scene.Clone(GuidanceDrawing.Cell(puzzle,gap),System.Drawing.Imaging.PixelFormat.Format24bppRgb))
     {
      foreach(int click in clicks)
      {
       int blankAt=Array.IndexOf(board,24);
       int distance=blankAt/5==click/5?Math.Abs(click-blankAt):Math.Abs(click/5-blankAt/5);
       if(distance>1)wholeRow=true;
       var shown=GuidanceDrawing.SliderMarks(puzzle,new[]{click},1,1);
       shown[0].Colour=KavrosTheme.Gold;shown[0].Pulse=true;shown[0].ArrowX=blankAt%5-click%5;shown[0].ArrowY=blankAt/5-click/5;
       long pending=0;
       foreach(double t in new[]{0.33,0.66})
       {
        using(Bitmap frame=(Bitmap)scene.Clone())
        {
         using(Graphics g=Graphics.FromImage(frame)){DrawClick(g,puzzle,board,click,t,reference,blank);GuidanceDrawing.Draw(g,shown);}
         Rectangle crop=Rectangle.Intersect(new Rectangle(Point.Empty,frame.Size),Rectangle.Inflate(puzzle.Grid,36,36));
         using(Bitmap shot=frame.Clone(crop,System.Drawing.Imaging.PixelFormat.Format24bppRgb))
         {
          GuideInk ink=GuideInk.ForSlider(shot.Width,shot.Height,shown,crop.Location);
          var watch=System.Diagnostics.Stopwatch.StartNew();
          using(AnalysisResult result=engine.Analyze(shot,150,false,CancellationToken.None,true,true,crop.Location,96,ink))
          {watch.Stop();pending+=watch.ElapsedMilliseconds;Check(result.Plan!=null,label+" the guide stays up during a whole-row slide");}
         }
        }
       }
       int[] next=AfterClick(board,click);
       long settled=0;
       using(Bitmap frame=(Bitmap)scene.Clone())
       {
        using(Graphics g=Graphics.FromImage(frame)){DrawBoard(g,puzzle,next,reference,blank);GuidanceDrawing.Draw(g,shown);}
        Rectangle crop=Rectangle.Intersect(new Rectangle(Point.Empty,frame.Size),Rectangle.Inflate(puzzle.Grid,36,36));
        using(Bitmap shot=frame.Clone(crop,System.Drawing.Imaging.PixelFormat.Format24bppRgb))
        {
         GuideInk ink=GuideInk.ForSlider(shot.Width,shot.Height,shown,crop.Location);
         int badgeX=shown[0].Box.X-crop.X+3,badgeY=shown[0].Box.Y-crop.Y+3;
         Check(ink!=null&&ink.Ignores(badgeX,badgeY),label+" the cropped capture mask covers the badge");
         var watch=System.Diagnostics.Stopwatch.StartNew();
         using(AnalysisResult result=engine.Analyze(shot,150,false,CancellationToken.None,true,true,crop.Location,96,ink))
         {
          watch.Stop();settled=watch.ElapsedMilliseconds;
          Check(result.Plan!=null&&!result.Pending&&result.Plan.Puzzle.Board.SequenceEqual(next),label+" the settled click is read through the guide");
          List<int> followed;int consumed;int now=SlidingClickOptimizer.SlowSolves;
          Check(SliderStep.Follow(board,route,result.Plan.Puzzle.Board,result.Plan.Sliding,out followed,out consumed),label+" the shown route advances");
          Check(SlidingClickOptimizer.SlowSolves==now,label+" follow does not solve on the UI thread");
          route=followed;board=next;
         }
        }
       }
       times.Add(125+pending+150+settled);
      }
     }
    }
   }
   times.Sort();
   long median=times[times.Count/2],worst=times[times.Count-1];
   File.AppendAllText(report,label+" click -> next step over "+times.Count+" clicks (analysis, UI follow, guide at the crop offset, 125/75 ms cadence, whole-row frames, background proof): median "+median+" ms, worst "+worst+" ms"+(wholeRow?"; included a whole-row click":"")+"; slow solves "+(SlidingClickOptimizer.SlowSolves-slow)+"\r\n");
   Check(worst<1200,label+" worst click stays under 1.2 s ("+worst+" ms)");
   Check(SlidingClickOptimizer.SlowSolves==slow,label+" the puzzle never used the slow solver");
  }
  static int[] AfterClick(int[] board,int click)
  {
   int[] next=(int[])board.Clone();int blank=Array.IndexOf(next,24);
   int step=blank/5==click/5?Math.Sign(click-blank):5*Math.Sign(click/5-blank/5);
   while(blank!=click){int n=blank+step;next[blank]=next[n];next[n]=24;blank=n;}
   return next;
  }
  static void DrawBoard(Graphics g,DetectedPuzzle puzzle,int[] board,Bitmap reference,Bitmap blank)
  {
   for(int i=0;i<25;i++)
   {
    Rectangle cell=GuidanceDrawing.Cell(puzzle,i);
    if(board[i]==24)g.DrawImage(blank,cell);
    else g.DrawImage(reference,cell,new Rectangle(board[i]%5*49,board[i]/5*49,49,49),GraphicsUnit.Pixel);
   }
  }
  static void DrawClick(Graphics g,DetectedPuzzle puzzle,int[] from,int click,double t,Bitmap reference,Bitmap blank)
  {
   int blankAt=Array.IndexOf(from,24);
   int step=blankAt/5==click/5?Math.Sign(click-blankAt):5*Math.Sign(click/5-blankAt/5);
   var moving=new List<int>();
   for(int p=blankAt+step;;p+=step){moving.Add(p);if(p==click)break;}
   for(int i=0;i<25;i++)
   {
    if(i==blankAt||moving.Contains(i))continue;
    g.DrawImage(reference,GuidanceDrawing.Cell(puzzle,i),new Rectangle(from[i]%5*49,from[i]/5*49,49,49),GraphicsUnit.Pixel);
   }
   g.DrawImage(blank,GuidanceDrawing.Cell(puzzle,blankAt));
   foreach(int src in moving)
   {
    Rectangle fromCell=GuidanceDrawing.Cell(puzzle,src),toCell=GuidanceDrawing.Cell(puzzle,src-step);
    int tile=from[src];
    int x=(int)Math.Round(fromCell.X+(toCell.X-fromCell.X)*t),y=(int)Math.Round(fromCell.Y+(toCell.Y-fromCell.Y)*t);
    g.DrawImage(reference,new Rectangle(x,y,fromCell.Width,fromCell.Height),new Rectangle(tile%5*49,tile/5*49,49,49),GraphicsUnit.Pixel);
   }
  }
  static void TimeFirstGuide(string report,string assets)
  {
   int[] board=Enumerable.Range(0,25).Select(i=>i==23?24:i==24?23:i).ToArray();
   TimeOpen(report,assets,"modern 100%",board,"modern",1F,96);
   TimeOpen(report,assets,"legacy 100%",board,"rev944",1F,96);
   TimeOpen(report,assets,"modern 125%",board,"modern",1.25F,120);
   TimeOpen(report,assets,"legacy 125%",board,"rev944",1.25F,120);
   TimeOpen(report,assets,"modern 150%",board,"modern",1.5F,144);
   TimeOpen(report,assets,"legacy 150%",board,"rev944",1.5F,144);
   TimeOpen(report,assets,"modern 200%",board,"modern",2F,192);
   TimeOpen(report,assets,"legacy 200%",board,"rev944",2F,192);
  }
  static void TimeOpen(string report,string assets,string label,int[] board,string skin,float scale,int dpi)
  {
   var engine=new PuzzleAnalysisEngine(assets);
   engine.UseLegacy(skin!="modern");
   float expect=PuzzleDetector.SearchOrder(1369,dpi)[0];
   using(Bitmap widget=TestFixtures.SliderImage("castle",board,skin))
   using(Bitmap sized=Math.Abs(expect-1F)<0.01F?(Bitmap)widget.Clone():Pixels.ScaleNearest(widget,expect))
   using(Bitmap idle=new Bitmap(2560,1369))
   using(Bitmap scene=new Bitmap(2560,1369))
   {
    using(Graphics g=Graphics.FromImage(idle))g.Clear(Color.FromArgb(30,40,50));
    long idleMs=0;
    for(int i=0;i<9;i++)
    {
     using(Bitmap frame=(Bitmap)idle.Clone())
     {
      var watch=System.Diagnostics.Stopwatch.StartNew();
      using(AnalysisResult miss=engine.Analyze(frame,91,false,CancellationToken.None,true,true,Point.Empty,dpi,null))
      {watch.Stop();idleMs+=watch.ElapsedMilliseconds;Check(miss.Plan==null||miss.Plan.Puzzle.Kind!=PuzzleKind.Sliding,label+" idle frame has no slider");}
     }
    }
    using(Graphics g=Graphics.FromImage(scene)){g.Clear(Color.FromArgb(30,40,50));g.DrawImageUnscaled(sized,700,280);}
    System.Threading.Thread.Sleep(150);
    var open=System.Diagnostics.Stopwatch.StartNew();
    using(AnalysisResult found=engine.Analyze(scene,91,false,CancellationToken.None,true,true,Point.Empty,dpi,null))
    {
     open.Stop();
     File.AppendAllText(report,label+" first guide after 9 idle 2560x1369 frames: "+open.ElapsedMilliseconds+" ms (idle total "+idleMs+" ms), first scale "+engine.LastFirstScale.ToString("0.00")+"\r\n");
     Check(found.Plan!=null&&found.Plan.Puzzle.Kind==PuzzleKind.Sliding,label+" slider is found on the next frame");
     Check(Math.Abs(engine.LastFirstScale-expect)<0.05F,label+" starts at scale "+expect.ToString("0.00")+" after the rotation would have moved");
     Check(open.ElapsedMilliseconds<4000,label+" first guide stays under 4 s ("+open.ElapsedMilliseconds+" ms)");
    }
   }
  }
  static void TimeScrolls(string report,string assets)
  {
   Check(ParagraphVision.SkipClueSheet(new Rectangle(100,200,1288,824),2560),"a sand sheet is not sent to OCR or master matching");
   Check(ParagraphVision.SkipClueSheet(new Rectangle(2040,16,432,227),2560),"the minimap is not sent to OCR or master matching");
   Check(!ParagraphVision.SkipClueSheet(new Rectangle(922,490,520,320),2560),"a framed scroll is still read");
   Check(!ParagraphVision.SkipClueSheet(new Rectangle(2200,20,500,280),3440),"a clue on an ultrawide client is not treated as the minimap");
   Check(ParagraphVision.SkipClueSheet(new Rectangle(3000,16,400,220),3440),"the ultrawide minimap is still skipped");
   TimeOneScroll(report,"hamid-full-2560",Path.Combine(assets,"test_assets","reported","hamid-full-2560.png"),assets,false);
   TimeOneScroll(report,"rogues-yawn",Path.Combine(assets,"test_assets","reported","mysterious-rogues-yawn.png"),assets,true);
   using(Bitmap master=new Bitmap(Path.Combine(assets,"test_assets","master","242.png")))
   {
    var engine=new PuzzleAnalysisEngine(assets);
    using(AnalysisResult first=engine.Analyze(master,242,false,CancellationToken.None,true))
    {
     Check(first.Plan!=null&&first.Plan.Puzzle.Kind==PuzzleKind.EmoteClue,"a shown text clue is tracked");
     int boards=engine.BoardPasses;
     using(AnalysisResult again=engine.Analyze(master,242,false,CancellationToken.None,true))
     {
      Check(again.Plan!=null&&again.Plan.Puzzle.Kind==PuzzleKind.EmoteClue,"an unchanged clue stays up when no puzzle has opened");
      Check(engine.BoardPasses>boards,"a tracked text clue still runs the board search");
     }
     using(Bitmap scene=new Bitmap(2560,1369))
     {
      using(Graphics g=Graphics.FromImage(scene)){g.Clear(Color.FromArgb(30,40,50));g.DrawImageUnscaled(master,1500,420);}
      using(AnalysisResult shown=engine.Analyze(scene,77,false,CancellationToken.None,true,true,Point.Empty,96,null))
       Check(shown.Plan!=null&&shown.Plan.Puzzle.Kind==PuzzleKind.EmoteClue,"the clue is tracked on the full window");
      int[] one=Enumerable.Range(0,25).Select(i=>i==23?24:i==24?23:i).ToArray();
      float expect=PuzzleDetector.SearchOrder(1369,96)[0];
      using(Bitmap slider=TestFixtures.SliderImage("castle",one,"modern"))
      using(Bitmap sized=Pixels.ScaleNearest(slider,expect))
      using(Bitmap both=(Bitmap)scene.Clone())
      {
       using(Graphics g=Graphics.FromImage(both))g.DrawImageUnscaled(sized,180,160);
       System.Threading.Thread.Sleep(150);
       var watch=System.Diagnostics.Stopwatch.StartNew();
       using(AnalysisResult opened=engine.Analyze(both,77,false,CancellationToken.None,true,true,Point.Empty,96,null))
       {
        watch.Stop();
        File.AppendAllText(report,"tracked clue, slider opens elsewhere: "+watch.ElapsedMilliseconds+" ms, first scale "+engine.LastFirstScale.ToString("0.00")+"\r\n");
        Check(opened.Plan!=null&&opened.Plan.Puzzle.Kind==PuzzleKind.Sliding,"a puzzle opening elsewhere replaces the tracked clue");
        Check(Math.Abs(engine.LastFirstScale-expect)<0.02F,"that search starts at the 1440p scale");
       }
      }
     }
    }
   }
  }
  static void TimeOneScroll(string report,string label,string path,string assets,bool place)
  {
   Check(File.Exists(path),label+" fixture is installed");
   var engine=new PuzzleAnalysisEngine(assets);
   using(Bitmap raw=new Bitmap(path))
   using(Bitmap frame=new Bitmap(2560,1369))
   {
    using(Graphics g=Graphics.FromImage(frame))
    {
     g.Clear(Color.FromArgb(168,142,92));
     if(place)g.DrawImage(raw,560,300);else g.DrawImage(raw,0,0);
    }
    int boards=engine.BoardPasses;
    var watch=System.Diagnostics.Stopwatch.StartNew();
    using(AnalysisResult result=engine.Analyze(frame,77,false,CancellationToken.None,true,true,Point.Empty,96,null))
    {
     watch.Stop();
     Check(engine.ScrollFastPaths>=1,label+" takes the scroll fast path");
     Check(engine.BoardPasses==boards,label+" skips the board search");
     string card=result.Plan!=null&&result.Plan.Puzzle.Master!=null?" card "+result.Plan.Puzzle.Master.Id:" Windows OCR unavailable here, so this is locate plus the OCR attempt";
     var againWatch=System.Diagnostics.Stopwatch.StartNew();
     using(AnalysisResult again=engine.Analyze(frame,77,false,CancellationToken.None,true,true,Point.Empty,96,null))
     {
      againWatch.Stop();
      File.AppendAllText(report,label+" scroll open -> card: "+watch.ElapsedMilliseconds+" ms;"+card+"; next frame "+againWatch.ElapsedMilliseconds+" ms"+(again.Unchanged?"; unchanged":"")+"; board searches "+engine.BoardPasses+"\r\n");
      Check(engine.BoardPasses==boards,label+" second frame still skips the board search");
     }
     Check(watch.ElapsedMilliseconds<500,label+" scroll path stays under 500 ms ("+watch.ElapsedMilliseconds+" ms)");
    }
   }
  }
 }
}
