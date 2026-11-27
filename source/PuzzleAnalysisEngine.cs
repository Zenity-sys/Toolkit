using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace KavrosClueHelper
{
    public static class SliderBudget
    {
        // Work budget for the immediate slider route. Deterministic work units,
        // so every PC gets the same route for the same board.
        public const int Milliseconds = 5000;
    }
    public static class SearchCancellation
    {
        [ThreadStatic] public static CancellationToken Token;
        public static void Check() { Token.ThrowIfCancellationRequested(); }
    }
    // The worker publishes this. The UI tick must not read the worker's bitmap
    // and must not capture the window just to decide whether to cancel.
    internal static class IdleFrame
    {
        internal static volatile int Sheet = 1;
        internal static volatile bool Yield;
    }
    public sealed class PuzzlePlan
    {
        public DetectedPuzzle Puzzle;
        public Solution Lockbox;
        public List<int[]> Towers;
        public List<int> Sliding;
        public bool SlidingOptimal;
        public bool SlidingWholeRows;
        // Proven minimum for the board the route was solved from (0 = unknown).
        public int SlidingLowerBound;
        public KnotSolution Knot;
        public bool KnotMoving;
        public PuzzlePlan WithPuzzle(DetectedPuzzle puzzle)
        {
            var copy = (PuzzlePlan)MemberwiseClone();
            copy.Puzzle = puzzle;
            return copy;
        }
    }
    public sealed class AnalysisResult : IDisposable
    {
        public PuzzlePlan Plan;
        public Bitmap Preview;
        public Bitmap DiagnosticImage;
        public long Milliseconds;
        public bool Unchanged,Pending;
        // The slider frame is still up, but the last grid could not be read.
        // The next capture should be the whole window.
        public bool SearchWindow;
        // A towers grid is on screen but its edge clues are not readable yet.
        public Rectangle? TowerGrid;
        public void Dispose() { if (Preview != null) { Preview.Dispose(); Preview = null; } if(DiagnosticImage!=null) { DiagnosticImage.Dispose(); DiagnosticImage=null; } }
    }
    public sealed class PuzzleAnalysisEngine
    {
        public readonly SliderProofSession Proofs = new SliderProofSession();
        private readonly PuzzleDetector detector;
        private DetectedPuzzle last;
        private PuzzlePlan cached;
        private string solveKey;
        private readonly KnotFrameTracker knotFrames=new KnotFrameTracker();
        private ulong lastHash;
        private Color[] sliderFrame;
        private bool[] sliderMasked;
        private int sliderSearches;
        private Size lastSize;
        private Point? previousOrigin;
        private DateTime knotConfirmed;
        private DateTime boardSeen;
        private int sourceId = Int32.MinValue;
        private DateTime nextBroadSearch = DateTime.MinValue;
        // Lockbox and towers keep their last good plan through brief misses.
        // A different board is shown after it is read twice, unless the new
        // lockbox is one or two legal clicks from the board on screen.
        private readonly DetectionHold boardHold = new DetectionHold();
        private int towerWaits;
        private readonly ChangeConfirmation boardChange = new ChangeConfirmation();
        private bool HoldBoard(DateTime now)
        {
            return cached!=null&&last!=null&&SteadyPolicy.HoldsThroughMisses(last.Kind)&&boardHold.Miss(now);
        }
        private AnalysisResult Held(long elapsed){return new AnalysisResult{Plan=cached,Pending=true,Unchanged=true,Milliseconds=elapsed};}
        public int FullSearches { get; private set; }
        public int SlidingRouteReuses {get;private set;}
        public int SliderFastPlans {get;private set;}
        public long LastSliderAdvanceMilliseconds {get;private set;}
        public int BroadSearches { get { return detector.BroadSearches; } }
        public int BoardPasses { get { return detector.BoardPasses; } }
        public float LastFirstScale { get { return detector.LastFirstScale; } }
        public int ScrollFastPaths { get { return detector.ScrollFastPaths; } }
        public int UnchangedFrames { get; private set; }
        public Action<string> Progress;
        public PuzzleAnalysisEngine(string assets) { detector = new PuzzleDetector(assets);detector.Progress=message=>{if(Progress!=null)Progress(message);}; }
        public DetectedPuzzle ReadOnlySlider(Bitmap image,DetectedPuzzle previous,GuideInk ink=null){return detector.ReadAgain(image,previous,null,-1,ink);}
        // Rescan starts from the tiles on screen. The previous route must not be replayed.
        public void Reset()
        {
            last=null;cached=null;solveKey=null;sliderFrame=null;sliderMasked=null;lastHash=0;towerWaits=0;sliderSearches=0;
            boardHold.Reset();boardChange.Reset();knotFrames.Reset();
        }
        private Point SliderSample(DetectedPuzzle puzzle,int i)
        {
            int side=i/4;double along=(i%4+1)/5.0;
            int x=side<2?(side==0?puzzle.Grid.Left-(int)Math.Round(10*puzzle.Scale):puzzle.Grid.Right+(int)Math.Round(10*puzzle.Scale)):puzzle.Grid.Left+(int)Math.Round(puzzle.Grid.Width*along);
            int y=side>=2?(side==2?puzzle.Grid.Top-(int)Math.Round(10*puzzle.Scale):puzzle.Grid.Bottom+(int)Math.Round(10*puzzle.Scale)):puzzle.Grid.Top+(int)Math.Round(puzzle.Grid.Height*along);
            return new Point(x,y);
        }
        private Color[] SliderFrame(Pixels pixels,DetectedPuzzle puzzle,GuideInk ink)
        {
            var colors=new Color[16];
            sliderMasked=new bool[16];
            for(int i=0;i<16;i++)
            {
                Point sample=SliderSample(puzzle,i);
                if(ink!=null&&ink.Ignores(sample.X,sample.Y)){sliderMasked[i]=true;colors[i]=Color.Black;continue;}
                colors[i]=pixels.Get(sample.X,sample.Y);
            }
            return colors;
        }
        private bool VisibleSlider(Bitmap image,Pixels pixels,GuideInk ink)
        {
            if(last==null||cached==null||last.Kind!=PuzzleKind.Sliding||sliderFrame==null||!detector.SliderFrameVisible(image,last))return false;
            int seen=0,matched=0;
            for(int i=0;i<16;i++)
            {
                Point sample=SliderSample(last,i);
                if((sliderMasked!=null&&sliderMasked[i])||(ink!=null&&ink.Ignores(sample.X,sample.Y)))continue;
                seen++;
                Color fresh=pixels.Get(sample.X,sample.Y);
                if(Math.Abs(fresh.R-sliderFrame[i].R)+Math.Abs(fresh.G-sliderFrame[i].G)+Math.Abs(fresh.B-sliderFrame[i].B)<60)matched++;
            }
            return seen==0||matched*16>=seen*13;
        }
        private DetectedPuzzle ReadSliderNear(Bitmap image,DetectedPuzzle previous,int badge,GuideInk guide)
        {
            int[] delta={-4,-2,2,4};
            for(int axis=0;axis<2;axis++)for(int n=0;n<delta.Length;n++)
            {
                DetectedPuzzle shifted=previous.OffsetBy(new Point(axis==0?delta[n]:0,axis==1?delta[n]:0));
                if(!new Rectangle(Point.Empty,image.Size).Contains(shifted.Grid))continue;
                DetectedPuzzle read=detector.ReadAgain(image,shifted,null,badge,guide);
                if(read!=null&&SlidingSolver.IsSolvable(read.Board))return read;
            }
            return null;
        }
        private AnalysisResult PendingSlider(long elapsed)
        {return new AnalysisResult{Plan=cached,Pending=true,Unchanged=true,Milliseconds=elapsed};}
        public void UseLegacy(bool legacy) { detector.UseLegacy(legacy); }
        public void UseDarkscape(bool enabled) { detector.UseDarkscape(enabled); }
        public void UseClient(bool legacy, int resolution, int scaling, int resolutionManual, int scalingManual, int clientHeight)
        {
            detector.UseClient(legacy, resolution, scaling, resolutionManual, scalingManual, clientHeight);
        }
        public void UseClient(bool legacy, int resolution, int scaling, int resolutionManual, int scalingManual, int clientHeight, bool darkscape)
        {
            detector.UseClient(legacy, resolution, scaling, resolutionManual, scalingManual, clientHeight, darkscape);
        }
        public AnalysisResult Analyze(Bitmap image, int source, bool reverse, CancellationToken cancellation, bool livePolling = false,bool wholeRows=false, Point? captureOrigin=null, int dpi=96, GuideInk guide=null)
        {
            Stopwatch timer = Stopwatch.StartNew();
            SearchCancellation.Token = cancellation;
            // Treat the frame as a sheet until this look has classified it,
            // and clear a yield left over from the previous idle miss.
            IdleFrame.Sheet = 1;
            IdleFrame.Yield = false;
            detector.UseDpi(dpi);
            Bitmap decoded=null;
            try
            {
                cancellation.ThrowIfCancellationRequested();
                // Windows GDI+ can repeatedly decode a file-backed PNG during
                // small crop/resample operations. Decode it once for this pass.
                // Live screen captures are already memory bitmaps.
                if(image.RawFormat.Guid!=System.Drawing.Imaging.ImageFormat.MemoryBmp.Guid)
                {
                    decoded=new Bitmap(image.Width,image.Height,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using(Graphics g=Graphics.FromImage(decoded))g.DrawImage(image,new Rectangle(Point.Empty,image.Size),new Rectangle(Point.Empty,image.Size),GraphicsUnit.Pixel);
                    image=decoded;
                }
                bool geometryChanged = lastSize != image.Size || previousOrigin != captureOrigin;
                if (source == sourceId && geometryChanged && last != null && previousOrigin.HasValue && captureOrigin.HasValue)
                {
                    Point old = previousOrigin.Value, next = captureOrigin.Value;
                    var translated = last.OffsetBy(new Point(old.X - next.X, old.Y - next.Y));
                    if (new Rectangle(Point.Empty, image.Size).Contains(translated.Bounds))
                    {
                        last = translated;
                        if (cached != null) cached = cached.WithPuzzle(translated);
                        geometryChanged = false;
                    }
                }
                previousOrigin = captureOrigin;
                if (source != sourceId || geometryChanged)
                { // Reframing the same game must retain the slider route; otherwise
                  // the worker and visible preview can follow different plans.
                  bool keepSlider=source==sourceId&&cached!=null&&(cached.Puzzle.Kind==PuzzleKind.Sliding||cached.Puzzle.Kind==PuzzleKind.Knot);
                  last=null;if(!keepSlider){cached=null;solveKey=null;}boardHold.Reset();boardChange.Reset();sliderFrame=null; if(source!=sourceId)knotFrames.Reset();else knotFrames.Missing(DateTime.UtcNow); sourceId = source; lastSize = image.Size; nextBroadSearch=DateTime.MinValue; detector.ResetSearch(); }
                lastSize = image.Size;
                Pixels pixels = new Pixels(image);
                bool sameSolve = last != null && DetectionPolicy.CanTrack(cached) && solveKey == cached.Puzzle.Key + ":" + reverse+(cached.Puzzle.Kind==PuzzleKind.Knot?":"+cached.KnotMoving:"")+":"+wholeRows && !cached.KnotMoving && new Rectangle(Point.Empty,image.Size).Contains(last.Bounds);
                bool hashUnchanged = sameSolve && pixels.Hash(last.Bounds, guide) == lastHash;
                bool trackingBoard = last != null && DetectionPolicy.IsBoardPuzzle(last.Kind);
                // An unchanged board can be reused. A tracked text clue still
                // has to run the board search, because a puzzle can open
                // somewhere else on the same screen.
                if (trackingBoard && hashUnchanged)
                { boardSeen=DateTime.UtcNow; boardHold.Seen(); boardChange.Reset(); UnchangedFrames++; return new AnalysisResult { Plan = cached, Unchanged = true, Milliseconds = timer.ElapsedMilliseconds }; }
                int sliderBadge=-1;
                if(cached!=null&&cached.Sliding!=null&&cached.Sliding.Count>0&&last!=null&&last.Kind==PuzzleKind.Sliding)sliderBadge=cached.Sliding[0];
                DetectedPuzzle puzzle = trackingBoard ? detector.ReadAgain(image,last,pixels,sliderBadge,guide) : null;
                bool sliderLive=livePolling&&last!=null&&last.Kind==PuzzleKind.Sliding;
                bool sliderSeen=sliderLive&&(DateTime.UtcNow-boardSeen).TotalMilliseconds<1500;
                bool frameVisible=sliderLive&&VisibleSlider(image,pixels,guide);
                // The frame is still there and the board was read moments ago.
                // Keep the step on screen. Do not spend this frame on a board
                // search or the clue readers.
                if(puzzle==null&&frameVisible&&sliderSeen)return PendingSlider(timer.ElapsedMilliseconds);
                // The hold has expired and the frame is still there. Look a few
                // pixels either side at this scale before any full search, and
                // keep the board for two more misses after that.
                if(puzzle==null&&frameVisible&&!sliderSeen)
                {
                    puzzle=ReadSliderNear(image,last,sliderBadge,guide);
                    if(puzzle==null)
                    {
                        sliderSearches++;
                        if(sliderSearches<3)return PendingSlider(timer.ElapsedMilliseconds);
                        sliderSearches=0;
                        return new AnalysisResult{Plan=cached,Pending=true,SearchWindow=true,Milliseconds=timer.ElapsedMilliseconds};
                    }
                    sliderSearches=0;
                }
                if (puzzle == null && !trackingBoard)
                {
                    // The knot has no full-width title bar, so a scroll-shaped
                    // parchment must not hide it. The slice stays capped.
                    DetectedPuzzle knotFirst=detector.FindKnotBoard(image,pixels,45);
                    if(knotFirst!=null)puzzle=knotFirst;
                }
                if (puzzle == null && !trackingBoard)
                {
                    DetectedPuzzle scroll=detector.TryScroll(image,pixels);
                    if(scroll!=null)puzzle=scroll;
                    else if(detector.LastScrollFast)
                    {
                        // A knot's parchment can look like a scroll. Read it
                        // before this look gives up on boards.
                        DetectedPuzzle knot=detector.FindKnotBoard(image,pixels,45);
                        if(knot!=null)puzzle=knot;
                        else
                        {
                            // The frame is a scroll, but the sheet was not read.
                            // Maps and the other clue readers still run. The board
                            // search stays skipped, so sand behind a real scroll
                            // is not walked as a puzzle.
                            puzzle=detector.DetectClues(image,pixels,detector.LastScrollBounds);
                            if(puzzle==null&&cached!=null&&last!=null&&last.Kind==PuzzleKind.EmoteClue)
                                return new AnalysisResult{Plan=cached,Unchanged=true,Milliseconds=timer.ElapsedMilliseconds};
                            // The lockbox window can look like a scroll. The
                            // same resumable slice still runs, so the grid is
                            // not abandoned with the rest of the board search.
                            if(puzzle==null)puzzle=detector.FindLiveLockbox(image,pixels,PuzzleDetector.LiveLockboxSlice);
                            if(puzzle==null)
                                return new AnalysisResult{Preview=MakePreview(image,new Rectangle(Point.Empty,image.Size)),Milliseconds=timer.ElapsedMilliseconds};
                        }
                    }
                    else if(hashUnchanged && cached!=null && last!=null && (last.Kind==PuzzleKind.Scan || last.Kind==PuzzleKind.Compass))
                    {
                        // The sheet has not changed. Do not spend this look on
                        // the board search.
                        UnchangedFrames++;
                        return new AnalysisResult{Plan=cached,Unchanged=true,Milliseconds=timer.ElapsedMilliseconds};
                    }
                    else
                    {
                        // A tall narrow sheet is read on this look. Boards wait
                        // until it is gone, unless a text clue is already open
                        // and a puzzle may appear beside it.
                        bool narrowSheet;
                        DetectedPuzzle narrow=detector.ReadNarrowSheet(image,pixels,out narrowSheet);
                        // A clue or puzzle that is already open still has to be
                        // re-read. A fresh narrow sheet does not.
                        bool keepOpen=cached!=null && cached.Puzzle!=null && (cached.Puzzle.Kind==PuzzleKind.EmoteClue || DetectionPolicy.IsBoardPuzzle(cached.Puzzle.Kind));
                        if(narrow!=null && !keepOpen)puzzle=narrow;
                        else if(narrowSheet && !keepOpen)
                        {
                            // The sheet was not a scan. A compass can still sit
                            // outside that box. A bright side panel is not a
                            // clue, so the slider close button is still checked.
                            puzzle=detector.FindCompass(image,pixels);
                            if(puzzle==null)puzzle=detector.FindOpenSlider(image,pixels);
                            // A bright side panel is not a lockbox either. The
                            // same short slice used in live board search still
                            // runs, so a 1440p lockbox is not skipped.
                            if(puzzle==null)puzzle=detector.FindLiveLockbox(image,pixels,PuzzleDetector.LiveLockboxSlice);
                            if(puzzle==null)
                                return new AnalysisResult{Preview=MakePreview(image,new Rectangle(Point.Empty,image.Size)),Milliseconds=timer.ElapsedMilliseconds};
                        }
                        // This capture has no scroll. The tick may ask the look
                        // to stop before the slow text search. It never reads
                        // the bitmap itself.
                        if(puzzle==null && !keepOpen) IdleFrame.Sheet=0;
                    }
                }
                if (puzzle == null)
                {
                    bool broad=!livePolling || DateTime.UtcNow>=nextBroadSearch;
                    DetectedPuzzle board=detector.DetectBoards(image,pixels,broad,livePolling);
                    if(board!=null)
                    {
                        towerWaits=0;
                        if(last==null||trackingBoard)FullSearches++;
                        puzzle=board;
                    }
                    else if(livePolling&&detector.TowerGridCandidate.HasValue&&!(last!=null&&SteadyPolicy.HoldsThroughMisses(last.Kind))&&++towerWaits<=6)
                    {
                        // The towers grid is opening: re-read it straight away
                        // instead of spending this frame on the slow clue reader.
                        return new AnalysisResult{TowerGrid=detector.TowerGridCandidate,Milliseconds=timer.ElapsedMilliseconds};
                    }
                    else
                    {
                        if(!detector.TowerGridCandidate.HasValue)towerWaits=0;
                        // While a lockbox or towers board is held, a missed frame
                        // keeps the board instead of running the slow clue reader.
                        if(livePolling&&trackingBoard&&HoldBoard(DateTime.UtcNow))return Held(timer.ElapsedMilliseconds);
                        if(livePolling && broad) nextBroadSearch=DateTime.UtcNow.AddMilliseconds(100);
                        if(hashUnchanged)
                        { UnchangedFrames++; return new AnalysisResult { Plan = cached, Unchanged = true, Milliseconds = timer.ElapsedMilliseconds }; }
                        if(last!=null && !trackingBoard) puzzle=detector.ReadAgain(image,last,pixels);
                        // Keep the last slider step only for about 1.5s after a
                        // real read. A frame that stays visible longer is solved
                        // again. Clue readers stay off while that frame is up,
                        // so a stuck slider cannot become a scan or a text clue.
                        if(puzzle==null&&sliderSeen)return PendingSlider(timer.ElapsedMilliseconds);
                        if(puzzle==null&&frameVisible)puzzle=detector.DetectBoards(image,pixels,true,false);
                        else if(puzzle==null)
                        {
                            FullSearches++;
                            // The title bar is checked above on every pass.
                            // Master templates, map matching and OCR wait until
                            // several misses, or until 1.0, 1.25, 1.5 and 2
                            // have each been tried.
                            // A stale idle look yields here, between searches.
                            // The scan on the next capture is not cancelled,
                            // and a result already found on this look is kept.
                            if(!IdleFrame.Yield && detector.DueForHeavyClues())puzzle=detector.DetectClues(image,pixels);
                        }
                    }
                }
                cancellation.ThrowIfCancellationRequested();
                if (puzzle == null)
                {
                    if(sliderSeen&&frameVisible)return PendingSlider(timer.ElapsedMilliseconds);
                    if(livePolling&&cached!=null&&last!=null&&last.Kind==PuzzleKind.Knot&&(DateTime.UtcNow-knotConfirmed).TotalMilliseconds<800&&detector.KnotFrameVisible(image,last))
                        return new AnalysisResult{Plan=cached,Pending=true,Unchanged=true,Milliseconds=timer.ElapsedMilliseconds};
                    if(livePolling&&HoldBoard(DateTime.UtcNow))return Held(timer.ElapsedMilliseconds);
                    if(livePolling&&cached!=null&&last!=null&&last.Kind==PuzzleKind.Sliding&&(DateTime.UtcNow-boardSeen).TotalMilliseconds<1500)
                        return new AnalysisResult{Plan=cached,Pending=true,Unchanged=true,Milliseconds=timer.ElapsedMilliseconds};
                    last = null; cached = null; solveKey = null; knotFrames.Missing(DateTime.UtcNow);
                    return new AnalysisResult { Preview = MakePreview(image,new Rectangle(Point.Empty,image.Size)), Milliseconds = timer.ElapsedMilliseconds };
                }
                if (cached != null && cached.Puzzle.Kind == PuzzleKind.Sliding && !cached.SlidingOptimal && puzzle.Kind == PuzzleKind.Sliding && cached.Puzzle.Board != null && puzzle.Board != null && cached.Puzzle.Board.SequenceEqual(puzzle.Board))
                {
                    List<int> proven;
                    string error;
                    if (Proofs.Read(puzzle.Board, cached.Puzzle.Theme, cached.SlidingWholeRows, out proven, out error) && proven != null && cached.Sliding != null && proven.Count < cached.Sliding.Count && SliderStep.SameNextClick(puzzle.Board, cached.Sliding, proven))
                        cached = new PuzzlePlan { Puzzle = cached.Puzzle, Sliding = proven, SlidingWholeRows = cached.SlidingWholeRows, SlidingOptimal = true, SlidingLowerBound = cached.SlidingLowerBound };
                }
                if(livePolling&&cached!=null&&cached.Puzzle.Kind==PuzzleKind.Sliding&&puzzle.Kind==PuzzleKind.Sliding&&puzzle.Theme==cached.Puzzle.Theme)
                {
                    List<int> route;
                    bool sameBoard=cached.Puzzle.Board!=null&&puzzle.Board!=null&&cached.Puzzle.Board.SequenceEqual(puzzle.Board);
                    if(!sameBoard&&!SlidingRoute.Advance(cached.Puzzle.Board,cached.Sliding,puzzle.Board,out route))
                    {
                        // The tiles are not the next step of the old route. Solve this board.
                        cached=null;solveKey=null;
                    }
                }
                bool confidentKnot=puzzle.Kind==PuzzleKind.Knot&&puzzle.Knot!=null&&puzzle.Knot.Reliable;
                bool legalLockbox=puzzle.Kind==PuzzleKind.Lockbox&&LockboxStep.SkipConfirm(last,puzzle,reverse?-1:1);
                if(confidentKnot||legalLockbox)boardChange.Reset();
                else if(livePolling&&cached!=null&&last!=null&&SteadyPolicy.HoldsThroughMisses(last.Kind)&&puzzle.Kind==last.Kind&&puzzle.Bounds.IntersectsWith(last.Bounds))
                {
                    // A click changes the board for good; a misread lasts one
                    // frame. Show the new board only once it is read twice.
                    // A clean knot is not held here. Its tracker shows that
                    // frame at once and waits only while the ring is blurred.
                    if(!boardChange.Accept(last.Key,puzzle.Key)){boardHold.Seen();return Held(timer.ElapsedMilliseconds);}
                }
                else boardChange.Reset();
                bool knotMoving=false;
                if(puzzle.Kind==PuzzleKind.Knot)knotMoving=knotFrames.Observe(puzzle,DateTime.UtcNow,livePolling);
                else knotFrames.Reset();
                string key=puzzle.Key+":"+reverse+(puzzle.Kind==PuzzleKind.Knot?":"+knotMoving:"")+":"+wholeRows;
                PuzzlePlan plan;
                if (cached != null && key == solveKey)
                    plan = new PuzzlePlan { Puzzle = puzzle, Lockbox = cached.Lockbox, Towers = cached.Towers, Sliding = cached.Sliding, SlidingOptimal=cached.SlidingOptimal,SlidingWholeRows=cached.SlidingWholeRows,SlidingLowerBound=cached.SlidingLowerBound,Knot = cached.Knot,KnotMoving=knotMoving };
                else
                {
                    if(Progress!=null)Progress(puzzle.Kind==PuzzleKind.EmoteClue||puzzle.Kind==PuzzleKind.Compass?"Reading clue...":"Solving puzzle...");
                    plan = new PuzzlePlan { Puzzle = puzzle,KnotMoving=knotMoving };
                    if (puzzle.Kind == PuzzleKind.Lockbox) plan.Lockbox = LockboxSolver.Solve(puzzle.Board,reverse?-1:1);
                    if (puzzle.Kind == PuzzleKind.Towers) plan.Towers = puzzle.TowerSolutions;
                    if(puzzle.Kind==PuzzleKind.Sliding&&SlidingSolver.IsSolvable(puzzle.Board))
                    {
                        List<int> remaining;
                        plan.SlidingWholeRows=wholeRows;
                        if(cached!=null&&cached.Puzzle.Kind==PuzzleKind.Sliding&&cached.SlidingWholeRows==wholeRows&&cached.Puzzle.Theme==puzzle.Theme&&SlidingRoute.Advance(cached.Puzzle.Board,cached.Sliding,puzzle.Board,out remaining)&&SlidingRoute.Playable(puzzle.Board,remaining))
                        {
                            plan.Sliding=remaining;LastSliderAdvanceMilliseconds=timer.ElapsedMilliseconds;int consumed=cached.Sliding.Count-remaining.Count;List<int> spent;bool clicksMatch=false;
                            if(SlidingRoute.TryClickRoute(cached.Puzzle.Board,cached.Sliding.Take(consumed).ToList(),out spent))clicksMatch=SlidingRoute.CompletedClicks(cached.Puzzle.Board,cached.Sliding,consumed)==spent.Count;
                            plan.SlidingOptimal=cached.SlidingOptimal&&(!wholeRows||clicksMatch);SlidingRouteReuses++;
                        }
                        else
                        {
                            if(Progress!=null)Progress("Solving puzzle...");
                            SliderFastPlans++;
                            SlidingOptimization route=SlidingClickOptimizer.Quick(puzzle.Board);
                            plan.Sliding=route.Moves;plan.SlidingOptimal=route.Optimal;plan.SlidingLowerBound=route.LowerBound;
                            if(wholeRows&&route.Moves!=null&&route.Moves.Count>0&&!route.Optimal)Proofs.Start(puzzle.Board,puzzle.Theme,true,route.Moves);
                        }
                    }
                    if(puzzle.Kind==PuzzleKind.Knot&&!knotMoving)plan.Knot=KnotSolver.Solve(puzzle.Knot);
                }
                cancellation.ThrowIfCancellationRequested();
                if (!DetectionPolicy.CanTrack(plan))
                {
                    if(sliderSeen&&frameVisible)return PendingSlider(timer.ElapsedMilliseconds);
                    if(livePolling&&puzzle.Kind==(last==null?PuzzleKind.None:last.Kind)&&HoldBoard(DateTime.UtcNow))return Held(timer.ElapsedMilliseconds);
                    last = null; cached = null; solveKey = null; knotFrames.Missing(DateTime.UtcNow);
                    return new AnalysisResult { Preview = MakePreview(image,new Rectangle(Point.Empty,image.Size)), Milliseconds = timer.ElapsedMilliseconds };
                }
                last = puzzle; cached = plan; solveKey = key;sliderSearches=0;if(puzzle.Kind==PuzzleKind.Sliding)sliderFrame=SliderFrame(pixels,puzzle,guide);else {sliderFrame=null;sliderMasked=null;} lastHash = pixels.Hash(puzzle.Bounds, guide);
                if(puzzle.Kind==PuzzleKind.Knot&&!knotMoving)knotConfirmed=DateTime.UtcNow;
                if(DetectionPolicy.IsBoardPuzzle(puzzle.Kind))boardSeen=DateTime.UtcNow;
                boardHold.Seen();
                return new AnalysisResult { Plan = plan, Preview = MakePreview(image,puzzle.Bounds), Milliseconds = timer.ElapsedMilliseconds };
            }
            finally { if(decoded!=null)decoded.Dispose();SearchCancellation.Token = CancellationToken.None; }
        }
        private static Bitmap MakePreview(Bitmap image, Rectangle area)
        {
            double ratio = Math.Min(1,Math.Min(920.0/area.Width,444.0/area.Height));
            return Pixels.Crop(image,area,new Size(Math.Max(1,(int)(area.Width*ratio)),Math.Max(1,(int)(area.Height*ratio))));
        }
    }
}
