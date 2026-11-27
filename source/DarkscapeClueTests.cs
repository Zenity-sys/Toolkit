using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace KavrosClueHelper
{
    public static class DarkscapeClueTests
    {
        public static int Run(string report)
        {
            int count = 0;
            try
            {
                Action<bool, string> check = delegate(bool ok, string msg) { count++; if (!ok) throw new Exception(msg); };
                string assets = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
                string shot = Path.Combine(assets, "test_assets", "lockbox", "darkscape-map-1920.png");
                check(File.Exists(shot), "the DarkScape treasure map capture is in the test assets");
                using (Bitmap image = new Bitmap(shot))
                {
                    check(image.Width == 1920 && image.Height == 1027, "the treasure map capture is 1920x1027");
                    Pixels pixels = new Pixels(image);
                    check(ParagraphVision.FindScrolls(pixels).Count == 0, "the modern title-bar search does not claim this brown frame");
                    var framed = ParagraphVision.FindDarkscapeScrolls(pixels);
                    check(framed.Count > 0, "DarkScape finds the brown parchment frame");
                    var reader = new ReferenceClueReader(assets);
                    DetectedPuzzle found = reader.Detect(image, pixels);
                    check(found != null && found.Master != null && found.Master.Id == 64, "the parchment matches East of the Ranging Guild (" + (found == null || found.Master == null ? "none" : found.Master.Id + " " + found.Master.Location) + ") " + (reader.MapScores ?? ""));
                    check(found == null || found.Master == null || found.Master.Id != 63, "the DarkScape Ranging Guild map is not the Champions' Guild river");
                    MasterClue champions = reader.Clues.Single(c => c.Card.Id == 63).Card;
                    check(champions.Area.X == 3166 && champions.Area.Y == 3360 && champions.Location.IndexOf("Champions' Guild", StringComparison.Ordinal) >= 0, "clue 63 stays the river west of the Champions' Guild");
                    var detector = new PuzzleDetector(assets);
                    detector.UseDarkscape(true);
                    DetectedPuzzle scroll = detector.TryScroll(image, pixels);
                    check(scroll != null && scroll.Master != null && scroll.Master.Id == 64 && scroll.Master.Id != 63, "the first DarkScape scroll look reads the Ranging Guild map");
                    var engine = new PuzzleAnalysisEngine(assets);
                    engine.UseDarkscape(true);
                    using (AnalysisResult live = engine.Analyze(image, 1027, false, CancellationToken.None, true))
                    {
                        DetectedPuzzle puzzle = live.Plan == null ? null : live.Plan.Puzzle;
                        check(puzzle != null && puzzle.Master != null && puzzle.Master.Id == 64 && puzzle.Master.Id != 63, "live polling matches the DarkScape map on the first look (" + (puzzle == null || puzzle.Master == null ? "none" : puzzle.Master.Id.ToString()) + ", " + live.Milliseconds + " ms)");
                        check(live.Milliseconds < 900, "the DarkScape map look stays inside the frame budget (" + live.Milliseconds + " ms)");
                    }
                    var modern = new PuzzleAnalysisEngine(assets);
                    using (AnalysisResult missed = modern.Analyze(image, 1027, false, CancellationToken.None, true))
                    {
                        DetectedPuzzle puzzle = missed.Plan == null ? null : missed.Plan.Puzzle;
                        check(puzzle == null || puzzle.Master == null || (puzzle.Master.Id != 63 && puzzle.Master.Id != 64), "modern mode does not read the brown frame on the first look");
                    }
                    using (Bitmap official = new Bitmap(Path.Combine(assets, "reference-maps", "64.png")))
                    {
                        DetectedPuzzle self = reader.Detect(official, new Pixels(official));
                        check(self != null && self.Master != null && self.Master.Id == 64, "the official Ranging Guild map matches itself (" + (reader.MapScores ?? "") + ")");
                    }
                    using (Bitmap river = new Bitmap(Path.Combine(assets, "reference-maps", "63.png")))
                    {
                        DetectedPuzzle riverMatch = reader.Detect(river, new Pixels(river));
                        check(riverMatch != null && riverMatch.Master != null && riverMatch.Master.Id == 63, "the Champions' Guild river map still matches itself (" + (reader.MapScores ?? "") + ")");
                    }
                }
                string slider = Path.Combine(assets, "test_assets", "darkscape", "darkscape-slider-house.png");
                if (File.Exists(slider))
                {
                    using (Bitmap image = new Bitmap(slider))
                    {
                        var engine = new PuzzleAnalysisEngine(assets);
                        engine.UseDarkscape(true);
                        using (AnalysisResult live = engine.Analyze(image, 1, false, CancellationToken.None, true))
                        {
                            DetectedPuzzle puzzle = live.Plan == null ? null : live.Plan.Puzzle;
                            check(puzzle != null && puzzle.Kind == PuzzleKind.Sliding, "DarkScape mode still finds the brown slider (" + (puzzle == null ? "none" : puzzle.Kind.ToString()) + ")");
                        }
                    }
                }
                File.AppendAllText(report, "DARKSCAPE CLUES PASS: " + count + " assertions.\r\n");
                return 0;
            }
            catch (Exception ex)
            {
                File.AppendAllText(report, "FAILED after " + count + " checks: " + ex.Message + "\r\n");
                return 1;
            }
        }
    }
}
