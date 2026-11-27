using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace KavrosClueHelper
{
    public static class LockboxLiveTests
    {
        // The attached 2560x1369 capture. Sixteen wizard hats, five bows, four swords.
        static readonly int[] Modern1440 = {
            2, 2, 1, 2, 0,
            1, 1, 0, 1, 0,
            2, 1, 2, 0, 2,
            2, 2, 2, 2, 2,
            2, 2, 2, 2, 2
        };
        // The 1920x1027 DarkScape capture. Eleven wizard hats, eight bows, six swords.
        static readonly int[] Darkscape1920 = {
            1, 2, 0, 1, 0,
            1, 1, 2, 2, 2,
            2, 0, 2, 1, 2,
            1, 2, 1, 1, 2,
            2, 0, 0, 2, 0
        };
        public static int Run(string report)
        {
            int count = 0;
            try
            {
                Action<bool, string> check = delegate(bool ok, string msg) { count++; if (!ok) throw new Exception(msg); };
                Clicks(check);
                float[] uiOrder = PuzzleDetector.SearchOrder(1369, 96);
                float[] lockOrder = PuzzleDetector.LockboxScaleOrder(uiOrder, 96);
                check(Math.Abs(uiOrder[0] - 1.25F) < 0.02F && Math.Abs(lockOrder[0] - 1F) < 0.02F, "a 100% DPI lockbox tries scale 1 before the 1.25 interface scale");
                float[] scaled = PuzzleDetector.LockboxScaleOrder(PuzzleDetector.SearchOrder(1369, 120), 120);
                check(Math.Abs(scaled[0] - PuzzleDetector.SearchOrder(1369, 120)[0]) < 0.02F, "a scaled display keeps the interface scale first for lockboxes");
                string assets = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
                string shot = Path.Combine(assets, "test_assets", "lockbox", "modern-1440.png");
                check(File.Exists(shot), "the 1440p lockbox capture is in the test assets");
                using (Bitmap image = new Bitmap(shot))
                {
                    check(image.Width == 2560 && image.Height == 1369, "the capture is the 2560x1369 client");
                    var detector = new PuzzleDetector(assets);
                    detector.ResetSearch();
                    Pixels pixels = new Pixels(image);
                    DetectedPuzzle found = null;
                    int ticks = 0;
                    string times = "";
                    for (; ticks < 5 && (found == null || found.Kind != PuzzleKind.Lockbox); ticks++)
                    {
                        Stopwatch watch = Stopwatch.StartNew();
                        found = detector.DetectBoards(image, pixels, true, true);
                        long elapsed = watch.ElapsedMilliseconds;
                        times += (times.Length == 0 ? "" : ",") + elapsed;
                        check(elapsed < 700, "live lockbox tick " + ticks + " took " + elapsed + " ms");
                    }
                    check(ticks == 1 && found != null && found.Kind == PuzzleKind.Lockbox, "the first 2560x1369 look finds the lockbox (" + times + " ms)");
                    check(found != null && found.Board != null && found.Board.SequenceEqual(Modern1440), "live polling reads all 25 symbols " + Board(found));
                    check(found != null && found.Confidence >= 0.90, "the 1440p read is confident enough to show");
                    var engine = new PuzzleAnalysisEngine(assets);
                    DetectedPuzzle analyzed = null;
                    int looks = 0;
                    string lookTimes = "";
                    for (; looks < 4 && analyzed == null; looks++)
                    {
                        Stopwatch watch = Stopwatch.StartNew();
                        using (AnalysisResult result = engine.Analyze(image, 1440, false, CancellationToken.None, true))
                        {
                            lookTimes += (lookTimes.Length == 0 ? "" : ",") + result.Milliseconds;
                            if (result.Plan != null && result.Plan.Puzzle != null && result.Plan.Puzzle.Kind == PuzzleKind.Lockbox)
                                analyzed = result.Plan.Puzzle;
                        }
                        check(watch.ElapsedMilliseconds < 700, "live analysis look " + looks + " took " + watch.ElapsedMilliseconds + " ms");
                    }
                    check(looks <= 3 && analyzed != null, "live analysis finds the 1440p lockbox in " + looks + " looks (" + lookTimes + " ms)");
                    check(analyzed != null && analyzed.Board != null && analyzed.Board.SequenceEqual(Modern1440), "live analysis reads all 25 symbols " + Board(analyzed));
                    int hats = 0, bows = 0, swords = 0;
                    if (analyzed != null && analyzed.Board != null)
                        foreach (int cell in analyzed.Board) { if (cell == 2) hats++; else if (cell == 1) bows++; else if (cell == 0) swords++; }
                    check(hats == 16 && bows == 5 && swords == 4, "the capture is 16 wizard hats, 5 bows and 4 swords (" + hats + "/" + bows + "/" + swords + ")");
                }
                string dark = Path.Combine(assets, "test_assets", "lockbox", "darkscape-1920.png");
                check(File.Exists(dark), "the DarkScape lockbox capture is in the test assets");
                using (Bitmap image = new Bitmap(dark))
                {
                    check(image.Width == 1920 && image.Height == 1027, "the DarkScape capture is the 1920x1027 client");
                    var modernOnly = new PuzzleDetector(assets);
                    DetectedPuzzle modernHit = modernOnly.DetectBoards(image, new Pixels(image), true, true);
                    check(modernHit == null || modernHit.Kind != PuzzleKind.Lockbox, "Modern does not spend the first look on the DarkScape lockbox");
                    var detector = new PuzzleDetector(assets);
                    detector.UseDarkscape(true);
                    detector.ResetSearch();
                    Pixels pixels = new Pixels(image);
                    DetectedPuzzle found = null;
                    int ticks = 0;
                    string times = "";
                    for (; ticks < 5 && (found == null || found.Kind != PuzzleKind.Lockbox); ticks++)
                    {
                        Stopwatch watch = Stopwatch.StartNew();
                        found = detector.DetectBoards(image, pixels, true, true);
                        long elapsed = watch.ElapsedMilliseconds;
                        times += (times.Length == 0 ? "" : ",") + elapsed;
                        check(elapsed < 700, "DarkScape lockbox tick " + ticks + " took " + elapsed + " ms");
                    }
                    check(ticks == 1 && found != null && found.Kind == PuzzleKind.Lockbox, "the first DarkScape look finds the lockbox (" + times + " ms)");
                    check(found != null && found.Board != null && found.Board.SequenceEqual(Darkscape1920), "DarkScape live polling reads all 25 symbols " + Board(found));
                    check(found != null && found.Confidence >= 0.95, "the DarkScape read is confident (" + (found == null ? "none" : found.Confidence.ToString("0.000")) + ")");
                    int hats = 0, bows = 0, swords = 0;
                    if (found != null && found.Board != null)
                        foreach (int cell in found.Board) { if (cell == 2) hats++; else if (cell == 1) bows++; else if (cell == 0) swords++; }
                    check(hats == 11 && bows == 8 && swords == 6, "the DarkScape capture is 11 wizard hats, 8 bows and 6 swords (" + hats + "/" + bows + "/" + swords + ")");
                }
                File.AppendAllText(report, "LOCKBOX LIVE PASS: " + count + " assertions.\r\n");
                return 0;
            }
            catch (Exception ex)
            {
                File.AppendAllText(report, "FAILED after " + count + " checks: " + ex.Message + "\r\n");
                return 1;
            }
        }
        static void Clicks(Action<bool, string> check)
        {
            Rectangle bounds = new Rectangle(100, 80, 198, 198);
            Rectangle grid = new Rectangle(108, 88, 182, 182);
            int[] start = (int[])LockboxSolver.Example.Clone();
            DetectedPuzzle shown = BoardAt(start, bounds, grid);
            int[] once = (int[])start.Clone();
            LockboxSolver.Click(once, 12, 1, 1);
            check(LockboxStep.AppliedClicks(start, once, 1) == 1 && LockboxStep.SkipConfirm(shown, BoardAt(once, bounds, grid), 1), "one forward click updates immediately");
            int[] twice = (int[])start.Clone();
            LockboxSolver.Click(twice, 0, 1, 1);
            LockboxSolver.Click(twice, 6, 1, 1);
            check(LockboxStep.AppliedClicks(start, twice, 1) == 2 && LockboxStep.SkipConfirm(shown, BoardAt(twice, bounds, grid), 1), "two forward clicks update immediately");
            int[] back = (int[])start.Clone();
            LockboxSolver.Click(back, 4, 1, -1);
            check(LockboxStep.AppliedClicks(start, back, -1) == 1 && LockboxStep.SkipConfirm(shown, BoardAt(back, bounds, grid), -1), "one reverse click updates immediately");
            check(LockboxStep.AppliedClicks(start, back, 1) == 2, "one reverse click is two forward clicks on that same tile");
            int[] backTwo = (int[])start.Clone();
            LockboxSolver.Click(backTwo, 24, 1, -1);
            LockboxSolver.Click(backTwo, 24, 1, -1);
            check(LockboxStep.AppliedClicks(start, backTwo, -1) == 2 && LockboxStep.SkipConfirm(shown, BoardAt(backTwo, bounds, grid), -1), "two reverse clicks on the same tile update immediately");
            int[] three = (int[])start.Clone();
            LockboxSolver.Click(three, 1, 1, 1);
            LockboxSolver.Click(three, 3, 1, 1);
            LockboxSolver.Click(three, 5, 1, 1);
            check(LockboxStep.AppliedClicks(start, three, 1) == 0 && !LockboxStep.SkipConfirm(shown, BoardAt(three, bounds, grid), 1), "three clicks still wait for confirmation");
            int[] corrupt = (int[])start.Clone();
            for (int i = 0; i < 25; i++) corrupt[i] = (corrupt[i] + 1) % 3;
            DetectedPuzzle bad = BoardAt(corrupt, bounds, grid);
            check(!LockboxStep.SkipConfirm(shown, bad, 1) && !LockboxStep.SkipConfirm(shown, bad, -1), "a corrupted frame is not a legal click");
            ChangeConfirmation confirm = new ChangeConfirmation();
            check(!confirm.Accept(shown.Key, bad.Key), "the first corrupted frame does not replace the board");
            check(confirm.Accept(shown.Key, bad.Key), "the same corrupted frame can replace the board once it repeats");
            check(!LockboxStep.SkipConfirm(shown, BoardAt(once, new Rectangle(400, 400, 198, 198), new Rectangle(408, 408, 182, 182)), 1), "a click on a different grid still confirms");
            check(LockboxStep.RereadMilliseconds <= 40, "a locked lockbox is read again within 40 ms");
        }
        static DetectedPuzzle BoardAt(int[] board, Rectangle bounds, Rectangle grid)
        {
            return new DetectedPuzzle { Kind = PuzzleKind.Lockbox, Board = board, Bounds = bounds, Grid = grid, Scale = 1F, Confidence = 0.95 };
        }
        static string Board(DetectedPuzzle puzzle)
        {
            if (puzzle == null || puzzle.Board == null) return "none";
            return string.Join(",", puzzle.Board.Select(v => v.ToString()).ToArray());
        }
    }
}
