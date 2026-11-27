using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace KavrosClueHelper
{
    public static class LegacyInterfaceTests
    {
        public static int Run(string report)
        {
            int count = 0;
            try
            {
                Action<bool, string> check = (ok, msg) => { count++; if (!ok) throw new Exception(msg); };
                string assets = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
                string shots = Path.Combine(assets, "test_assets", "legacy");
                var detector = new PuzzleDetector(assets);
                detector.UseLegacy(true);
                using (Bitmap towers = new Bitmap(Path.Combine(shots, "towers-legacy.png")))
                {
                    DetectedPuzzle found = detector.Detect(towers, true, false);
                    int[] top = found == null || found.Towers == null ? null : found.Towers.Top;
                    check(found != null && found.Kind == PuzzleKind.Towers && top != null, "legacy towers grid is read");
                    check(top != null && top.SequenceEqual(new[] { 2, 1, 2, 3, 3 }), "legacy tower top clues " + (top == null ? "" : String.Join(",", top)));
                    check(found.Towers.Right.SequenceEqual(new[] { 2, 2, 2, 3, 1 }), "legacy tower right clues");
                    check(found.Towers.Bottom.SequenceEqual(new[] { 2, 3, 2, 3, 1 }), "legacy tower bottom clues");
                    check(found.Towers.Left.SequenceEqual(new[] { 2, 3, 3, 1, 4 }), "legacy tower left clues");
                    check(found.TowerSolutions != null && found.TowerSolutions.Count > 0, "legacy towers have a solution");
                }
                using (Bitmap knot = new Bitmap(Path.Combine(shots, "celtic-knot-legacy.png")))
                {
                    DetectedPuzzle found = detector.Detect(knot, true, false);
                    check(found != null && found.Kind == PuzzleKind.Knot && found.Knot != null && found.Knot.Lanes.Length >= 3, "legacy knot is read");
                    KnotSolution solved = found == null || found.Knot == null ? null : KnotSolver.Solve(found.Knot);
                    check(solved != null, "legacy knot solves");
                }
                using (Bitmap box = new Bitmap(Path.Combine(shots, "lockbox-legacy.png")))
                {
                    DetectedPuzzle found = detector.Detect(box, true, false);
                    check(found != null && found.Kind == PuzzleKind.Lockbox && found.Board != null && found.Board.Length == 25, "legacy lockbox is read " + (found == null ? "none" : found.Kind.ToString()));
                    Solution solved = found == null || found.Board == null ? null : LockboxSolver.Solve(found.Board, 1);
                    check(solved != null, "legacy lockbox solves");
                }
                using (Bitmap scan = new Bitmap(Path.Combine(shots, "scan-legacy.png")))
                {
                    DetectedPuzzle found = detector.Detect(scan, true, false);
                    check(found != null && found.Kind == PuzzleKind.Scan && found.Scan != null && found.Scan.Region != null && found.Scan.Region.Name == "Taverley Dungeon", "legacy scan names Taverley Dungeon");
                    check(found != null && found.Scan != null && found.Scan.Outside, "legacy scan sees that nothing is in range");
                }
                using (Bitmap compass = new Bitmap(Path.Combine(shots, "compass-parchment.png")))
                {
                    DetectedPuzzle found = new CompassVision(assets).Find(compass, new Pixels(compass));
                    double heading = found == null || found.Compass == null ? -1 : CompassMath.Bearing(found.Compass.Angle);
                    check(found != null && found.Kind == PuzzleKind.Compass && !found.Compass.Solved && heading > 200 && heading < 260, "legacy compass arrow " + heading.ToString("0.0"));
                }
                using (Bitmap orb = new Bitmap(Path.Combine(shots, "minimap-orb.png")))
                {
                    var anchor = new MinimapAnchor(assets);
                    anchor.InterfaceLegacy = true;
                    var marks = anchor.Marks(orb);
                    MinimapAnchor.Mark legacy = marks.FirstOrDefault(m => m.Legacy);
                    check(marks.Any(m => m.Legacy) && legacy.Score >= 0.85, "legacy H button is the minimap anchor " + String.Join(";", marks.Select(m => (m.Legacy ? "H" : "home") + " " + m.Score.ToString("0.00")).ToArray()));
                    MinimapViewport view = MinimapAnchor.LegacyViewport(orb.Width, orb.Height, legacy.Box);
                    check(view != null && MinimapAnchor.ContainsLegacy(view.Bounds, legacy.Box), "legacy orb is placed from the H button");
                    check(view.Bounds.Contains(new Point(142, 90)), "legacy orb covers the map centre");
                }
                using (Bitmap home = new Bitmap(Path.Combine(assets, "minimap", "home.png")))
                using (Bitmap padded = new Bitmap(80, 80))
                {
                    using (Graphics g = Graphics.FromImage(padded)) { g.Clear(Color.FromArgb(20, 30, 24)); g.DrawImageUnscaled(home, 20, 30); }
                    var anchor = new MinimapAnchor(assets);
                    var marks = anchor.Marks(padded);
                    check(marks.Any(m => !m.Legacy && m.Score >= 0.90), "modern home button still anchors on its own");
                    check(!marks.Any(m => m.Legacy), "the grey H template does not claim the modern home button");
                }
                File.WriteAllText(report, "LEGACY INTERFACE PASS: " + count + " assertions.\r\n");
                return 0;
            }
            catch (Exception ex)
            {
                File.AppendAllText(report, "FAIL after " + count + ": " + ex + "\r\n");
                return 1;
            }
        }
    }
}
