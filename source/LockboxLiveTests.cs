using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
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
                    if (analyzed != null)
                    {
                        Rectangle modernBox = GuidanceDrawing.Cell(analyzed, 0);
                        check(analyzed.TileSize == 0 && modernBox.Width == 30 && modernBox.Height == 30 && modernBox.X == analyzed.Grid.X && modernBox.Y == analyzed.Grid.Y, "Modern lockbox boxes stay on the 30px icon grid");
                    }
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
                    if (found != null) Centre(check, image, found);
                    if (found != null) HoverClick(check, assets, image, found);
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
        static void Centre(Action<bool, string> check, Bitmap image, DetectedPuzzle found)
        {
            check(found.TileSize == 32 && found.TileInsetY == 3 && found.TileInsetX == 0, "DarkScape lockbox boxes use the 32px tile");
            for (int i = 0; i < 25; i++)
            {
                int originX = found.Grid.X + (int)Math.Round(i % 5 * 38 * found.Scale);
                int originY = found.Grid.Y + (int)Math.Round(i / 5 * 38 * found.Scale);
                int borderX = -1, borderY = -1;
                for (int y = originY; y < originY + 12 && y < image.Height; y++)
                {
                    int run = 0;
                    for (int x = Math.Max(0, originX - 2); x < originX + 36 && x < image.Width; x++) if (TileBorder(image.GetPixel(x, y))) run++;
                    if (run >= 28) { borderY = y; break; }
                }
                for (int x = Math.Max(0, originX - 2); x < originX + 8 && x < image.Width; x++)
                {
                    int run = 0;
                    for (int y = originY; y < originY + 40 && y < image.Height; y++) if (TileBorder(image.GetPixel(x, y))) run++;
                    if (run >= 16) { borderX = x; break; }
                }
                Rectangle box = GuidanceDrawing.Cell(found, i);
                int boxX = box.X + box.Width / 2, boxY = box.Y + box.Height / 2;
                check(borderX >= 0 && borderY >= 0 && box.Width == 32 && box.Height == 32 && Math.Abs(boxX - (borderX + 16)) <= 1 && Math.Abs(boxY - (borderY + 16)) <= 1, "DarkScape lockbox cell " + i + " is centred on the tile (" + box.X + "," + box.Y + " " + box.Width + "x" + box.Height + " vs border " + borderX + "," + borderY + ")");
            }
        }
        static bool TileBorder(Color c)
        {
            return c.R >= 95 && c.R <= 125 && c.G >= 95 && c.G <= 125 && c.B >= 105 && c.B <= 140 && Math.Abs(c.R - c.G) <= 12;
        }
        static void HoverClick(Action<bool, string> check, string assets, Bitmap image, DetectedPuzzle found)
        {
            Solution before = LockboxSolver.Solve(found.Board, 1);
            int index = -1;
            for (int i = 0; i < 25; i++)
            {
                if (before.Clicks[i] <= 0) continue;
                if (index < 0) index = i;
                if (Cross(i).Contains(4) || Cross(i).Contains(20) || Cross(i).Contains(24)) { index = i; break; }
            }
            check(index >= 0 && before.Clicks[index] > 0, "the DarkScape board has a tile that still needs a click");
            if (index < 0) return;
            using (Bitmap hover = Copy(image))
            {
                PaintCross(hover, assets, found, found.Board, index);
                var fresh = new PuzzleDetector(assets);
                fresh.UseDarkscape(true);
                DetectedPuzzle opened = null;
                for (int tick = 0; tick < 5 && (opened == null || opened.Board == null); tick++)
                    opened = fresh.DetectBoards(hover, new Pixels(hover), true, true);
                check(opened != null && opened.Board != null && opened.Board.SequenceEqual(found.Board), "a highlighted DarkScape cross still reads all 25 symbols " + Board(opened));
            }
            int[] next = (int[])found.Board.Clone();
            LockboxSolver.Click(next, index, 1, 1);
            using (Bitmap clicked = Copy(image))
            {
                PaintCross(clicked, assets, found, next, index);
                var reader = new PuzzleDetector(assets);
                reader.UseDarkscape(true);
                DetectedPuzzle read = reader.ReadAgain(clicked, found);
                check(read != null && read.Board != null && read.Board.SequenceEqual(next), "the highlighted click is read on frame 1 " + Board(read));
                check(read != null && LockboxStep.SkipConfirm(found, read, 1), "a DarkScape click counts down without a second frame");
                if (read != null && read.Board != null)
                {
                    Solution after = LockboxSolver.Solve(read.Board, 1);
                    check(after.Clicks[index] == before.Clicks[index] - 1, "the clicked tile's number counts down on frame 1 (" + before.Clicks[index] + " to " + after.Clicks[index] + ")");
                }
            }
        }
        static Bitmap Copy(Bitmap image)
        {
            Bitmap copy = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(copy)) g.DrawImage(image, 0, 0);
            return copy;
        }
        static int[] Cross(int index)
        {
            var cells = new List<int> { index };
            int row = index / 5, column = index % 5;
            if (row > 0) cells.Add(index - 5);
            if (row < 4) cells.Add(index + 5);
            if (column > 0) cells.Add(index - 1);
            if (column < 4) cells.Add(index + 1);
            return cells.ToArray();
        }
        static void PaintCross(Bitmap image, string assets, DetectedPuzzle found, int[] symbols, int clicked)
        {
            string[] names = { "melee", "range", "mage" };
            int[] cells = Cross(clicked);
            for (int n = 0; n < cells.Length; n++)
            {
                int cell = cells[n];
                int sampleX = found.Grid.X + (int)Math.Round(cell % 5 * 38 * found.Scale);
                int sampleY = found.Grid.Y + (int)Math.Round(cell / 5 * 38 * found.Scale);
                int tileX = sampleX + (int)Math.Round(found.TileInsetX * found.Scale);
                int tileY = sampleY + (int)Math.Round(found.TileInsetY * found.Scale);
                int tile = Math.Max(1, (int)Math.Round((found.TileSize > 0 ? found.TileSize : 32) * found.Scale));
                for (int y = 0; y < tile; y++) for (int x = 0; x < tile; x++)
                {
                    int px = tileX + x, py = tileY + y;
                    if (px < 0 || py < 0 || px >= image.Width || py >= image.Height) continue;
                    double t = (x + y) / (double)Math.Max(1, tile * 2 - 2);
                    image.SetPixel(px, py, Color.FromArgb(33 + (int)Math.Round(16 * t), 43 + (int)Math.Round(27 * t), 77 + (int)Math.Round(16 * t)));
                }
                string path = Path.Combine(assets, "alt1anchors", "lockbox_tiles", names[symbols[cell]] + ".png");
                using (Bitmap icon = new Bitmap(path))
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.DrawImage(icon, new Rectangle(sampleX, sampleY, icon.Width, icon.Height), new Rectangle(0, 0, icon.Width, icon.Height), GraphicsUnit.Pixel);
                }
                for (int x = 0; x < tile; x++)
                {
                    Paint(image, tileX + x, tileY, Color.White);
                    Paint(image, tileX + x, tileY + tile - 1, Color.White);
                }
                for (int y = 0; y < tile; y++)
                {
                    Paint(image, tileX, tileY + y, Color.White);
                    Paint(image, tileX + tile - 1, tileY + y, Color.White);
                }
            }
            int cursorX = found.Grid.X + (int)Math.Round((clicked % 5 * 38 + 12) * found.Scale);
            int cursorY = found.Grid.Y + (int)Math.Round((clicked / 5 * 38 + 8) * found.Scale);
            for (int y = 0; y < 14; y++) for (int x = 0; x <= y && x < 9; x++)
            {
                Color ink = (x == 0 || x == y || y == 13) ? Color.FromArgb(12, 12, 12) : Color.FromArgb(248, 248, 248);
                Paint(image, cursorX + x, cursorY + y, ink);
            }
        }
        static void Paint(Bitmap image, int x, int y, Color colour)
        {
            if (x >= 0 && y >= 0 && x < image.Width && y < image.Height) image.SetPixel(x, y, colour);
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
