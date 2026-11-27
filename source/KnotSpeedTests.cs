using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace KavrosClueHelper
{
    public static class KnotSpeedTests
    {
        public static void Verify(Action<bool, string> check, string assets, Bitmap settled, Rectangle body)
        {
            var engine = new PuzzleAnalysisEngine(assets);
            using (AnalysisResult first = engine.Analyze(settled, 6000, false, CancellationToken.None, true))
            {
                string scores = first.Plan == null || first.Plan.Puzzle.Knot == null ? "none" : first.Plan.Puzzle.Knot.MinRune.ToString("0.00") + "/" + first.Plan.Puzzle.Knot.MinTrack.ToString("0.00") + "/" + first.Plan.Puzzle.Knot.MinMargin.ToString("0.00");
                check(first.Plan != null && !first.Plan.KnotMoving && first.Plan.Knot != null, "Knot shows counts on the first still frame " + scores);
                check(first.Plan != null && !DetectionPolicy.NeedsConfirmation(first.Plan), "a solved still knot does not wait for another frame");
            }
            var knotReader = new KnotImageReader(assets);
            using (Bitmap crop = Pixels.Crop(settled, body, body.Size))
            {
                KnotPuzzle read = knotReader.Read(crop);
                int misses = knotReader.CacheMisses;
                string key = KnotFrameTracker.RuneKey(read);
                KnotPuzzle again = knotReader.Read(crop);
                check(read != null && read.Reliable && KnotFrameTracker.RuneKey(again) == key, "cached knot runes match the first read " + (read == null ? knotReader.LastError : read.MinRune.ToString("0.00")));
                check(knotReader.CacheHits >= 20 && knotReader.CacheMisses == misses, "unchanged knot tiles are not read again");
                using (Graphics paint = Graphics.FromImage(crop)) paint.FillRectangle(Brushes.Black, 90, 40, 12, 12);
                int hits = knotReader.CacheHits;
                knotReader.Read(crop);
                check(knotReader.CacheHits > hits && knotReader.CacheMisses > misses && knotReader.CacheMisses - misses < knotReader.CacheHits - hits, "a changed tile is re-read and the other tiles stay cached");
            }
        }
        public static int Run(string report)
        {
            int count = 0;
            try
            {
                Action<bool, string> check = (ok, msg) => { count++; if (!ok) throw new Exception(msg); };
                string assets = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
                var tracker = new KnotFrameTracker();
                DateTime now = DateTime.UtcNow;
                KnotPuzzle source = TestFixtures.KnotExample();
                Func<int, DetectedPuzzle> rotated = offset => new DetectedPuzzle { Kind = PuzzleKind.Knot, Knot = new KnotPuzzle { Colours = source.Colours, Locks = source.Locks, Buttons = source.Buttons, Lanes = source.Lanes.Select((lane, i) => lane.Select((rune, j) => lane[(j + offset + i) % lane.Length]).ToArray()).ToArray() } };
                check(tracker.Observe(rotated(0), now, true), "an uncertain first knot frame waits");
                check(!tracker.Observe(rotated(0), now.AddMilliseconds(125), true), "the repeated frame shows counts");
                var sureTracker = new KnotFrameTracker();
                DetectedPuzzle sure = rotated(0);
                sure.Knot.Reliable = true;
                check(!sureTracker.Observe(sure, now, true), "a reliable still knot shows counts on the first frame");
                DetectedPuzzle flicker = rotated(0);
                flicker.Knot.Lanes[0][0] = new RuneValue(flicker.Knot.Lanes[0][0].Id, !flicker.Knot.Lanes[0][0].IsNot);
                flicker.Knot.Reliable = true;
                check(!sureTracker.Observe(flicker, now.AddMilliseconds(40), true), "a match-marker flicker does not hide the counts");
                DetectedPuzzle turned = rotated(1);
                turned.Knot.Reliable = true;
                check(!sureTracker.Observe(turned, now.AddMilliseconds(80), true), "a reliable turn updates the count on the first frame");
                check(!sureTracker.Observe(turned, now.AddMilliseconds(120), true), "the stopped ring keeps that count");
                DetectedPuzzle blur = rotated(2);
                check(sureTracker.Observe(blur, now.AddMilliseconds(160), true), "an unreliable mid-spin frame waits");
                check(!sureTracker.Observe(blur, now.AddMilliseconds(200), true), "the same mid-spin frame shows counts once it repeats");
                var hidden = new PuzzlePlan { Puzzle = sure, KnotMoving = false };
                check(!DetectionPolicy.NeedsConfirmation(hidden), "a clean knot with hidden runes shows on the first frame");
                var spinning = new PuzzlePlan { Puzzle = sure, KnotMoving = true };
                check(DetectionPolicy.NeedsConfirmation(spinning), "a mid-spin knot still waits for another frame");
                CheckLive(check, assets, "modern-2560.png", "Modern 2560x1369");
                CheckLive(check, assets, "darkscape-1920.png", "DarkScape 1920x1027");
                CheckLive(check, assets, "darkscape-knot-1920.png", "DarkScape knot 1920x1027");
                var detector = new PuzzleDetector(assets);
                using (Bitmap image = TestFixtures.KnotModal())
                {
                    DetectedPuzzle clean = detector.Detect(image);
                    check(clean != null && clean.Kind == PuzzleKind.Knot, "Knot fixture");
                    using (Bitmap settled = TestFixtures.KnotModal()) Verify(check, assets, settled, clean.Bounds);
                }
                File.WriteAllText(report, "KNOT SPEED PASS: " + count + " assertions.\r\n");
            }
            catch (Exception ex)
            {
                File.WriteAllText(report, "FAIL after " + count + ": " + ex + "\r\n");
                return 1;
            }
            return 0;
        }
        static void CheckLive(Action<bool, string> check, string assets, string file, string name)
        {
            using (Bitmap image = new Bitmap(Path.Combine(assets, "test_assets", "knot", file)))
            {
                var detector = new PuzzleDetector(assets);
                var watch = Stopwatch.StartNew();
                DetectedPuzzle found = detector.FindKnotBoard(image, new Pixels(image), 45);
                long sweep = watch.ElapsedMilliseconds;
                check(found != null && found.Kind == PuzzleKind.Knot && found.Knot != null && found.Knot.Reliable && sweep < 180, name + " is found in one sweep " + sweep + "ms");
                watch.Restart();
                DetectedPuzzle again = detector.ReadAgain(image, found);
                long reread = watch.ElapsedMilliseconds;
                check(again != null && again.Kind == PuzzleKind.Knot && again.Knot != null && again.Knot.Reliable && reread < 80, name + " re-reads the locked crop " + reread + "ms");
                var engine = new PuzzleAnalysisEngine(assets);
                using (AnalysisResult first = engine.Analyze(image, 7000, false, CancellationToken.None, true))
                {
                    bool shown = first.Plan != null && first.Plan.Puzzle != null && first.Plan.Puzzle.Kind == PuzzleKind.Knot && !first.Plan.KnotMoving;
                    check(shown && !DetectionPolicy.NeedsConfirmation(first.Plan), name + " shows on the first look " + first.Milliseconds + "ms");
                    check(first.Milliseconds < 180, name + " finishes in one tick " + first.Milliseconds + "ms");
                }
            }
        }
    }
}
