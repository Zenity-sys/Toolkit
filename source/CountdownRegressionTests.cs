using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;

namespace KavrosClueHelper
{
    public static class CountdownRegressionTests
    {
        private static int assertions;
        private static void Check(bool value, string message) { assertions++; if (!value) throw new Exception("FAILED: " + message); }
        public static int Run(string report)
        {
            try
            {
                for (int current = 0; current <= 5; current++)
                    for (int target = 1; target <= 5; target++)
                    {
                        int value = current, count = 0;
                        while (value != target && count < 7) { value++; if (value > 5) value = 0; count++; }
                        Check(GuidanceDrawing.TowerClicksRemaining(current, target) == count, "remaining steps for " + current + " to " + target);
                    }
                Check(GuidanceDrawing.TowerClicksRemaining(5, 1) == 2, "a tower goes from 5 through blank to 1");
                string assets = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
                int[] targetCells = TowerSolver.Solve(TestFixtures.TowersExample(), false)[0];
                int index = Array.IndexOf(targetCells, 4);
                List<int[]> savedSolution = null;
                PuzzleAnalysisEngine engine = new PuzzleAnalysisEngine(assets);
                engine.UseLegacy(true);
                int[] visible = { 0, 1, 2, 3, 4, 5, 0, 3, 1, 4 };
                int[] counts = { 4, 3, 2, 1, 0, 5, 4, 1, 3, 0 };
                for (int step = 0; step < visible.Length; step++)
                {
                    int[] cells = (int[])targetCells.Clone(); cells[index] = visible[step];
                    using (Bitmap image = TestFixtures.TowerModal("rev944", false, false, cells))
                    using (AnalysisResult result = engine.Analyze(image, 424, false, CancellationToken.None))
                    {
                        Check(result.Plan != null && result.Plan.Puzzle.Kind == PuzzleKind.Towers, "partly entered tower recognised");
                        Check(result.Plan.Puzzle.Towers.Cells.SequenceEqual(cells), "reads actual visible heights, including blank resets and direct changes");
                        if (savedSolution == null) savedSolution = result.Plan.Towers;
                        else Check(Object.ReferenceEquals(savedSolution, result.Plan.Towers), "changing entered heights reuses the solved answer");
                        List<GuidanceMark> marks = GuidanceDrawing.TowerMarks(result.Plan.Puzzle, result.Plan.Towers[0], true);
                        Check(marks[index].Text == counts[step].ToString(), "corner countdown follows the recognised number");
                        Check(marks[index].Colour == (counts[step] == 0 ? Color.Lime : Color.White), "correct square shows green zero");
                        Check(marks.Where((m, i) => i != index).All(m => m.Text == "0" && m.Colour == Color.Lime), "other completed squares stay at zero");
                        Check(GuidanceDrawing.TowerMarks(result.Plan.Puzzle, result.Plan.Towers[0], false)[index].Text == "4", "target-height mode remains available");
                        using (AnalysisResult unchanged = engine.Analyze(image, 424, false, CancellationToken.None))
                        {
                            Check(unchanged.Unchanged, "an unchanged game number reuses the frame");
                            Check(GuidanceDrawing.TowerMarks(unchanged.Plan.Puzzle, unchanged.Plan.Towers[0], true)[index].Text == counts[step].ToString(), "a missed click does not decrement anything");
                        }
                        using (Graphics g = Graphics.FromImage(image)) GuidanceDrawing.Draw(g, marks);
                        DetectedPuzzle reread = new PuzzleDetector(assets).ReadTowers(image, result.Plan.Puzzle.Grid, 1, true);
                        Check(reread != null && reread.Towers.Cells.SequenceEqual(cells), "white counts and green zeros do not interfere with reading");
                        if (step == 2) image.Save(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report)), "tower-countdown-preview.png"));
                    }
                }
                File.AppendAllText(report, "V0.2.4 COUNTDOWN REGRESSIONS PASS: " + assertions + " assertions.\r\nChecks visible-number countdowns, unchanged frames, direct number selection, resets, overshoots and the blank step between 5 and 1.\r\nExisting solved answers are reused when only entered heights change. No windows were shown or application input sent.\r\n");
                return 0;
            }
            catch (Exception ex) { File.AppendAllText(report, ex + "\r\n"); return 1; }
        }
    }
}
