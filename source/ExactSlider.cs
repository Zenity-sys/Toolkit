using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace KavrosClueHelper
{
    // All search state belongs to one board and one click rule.
    public static class ExactSlider
    {
        [ThreadStatic] internal static Action<int> BoundChanged;

        public static List<int> Solve(int[] board, IList<int> seed, bool lines, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!SlidingSolver.IsSolvable(board)) throw new ArgumentException("Unsolvable slider");
            var previous = SearchCancellation.Token;
            try
            {
                SearchCancellation.Token = token;
                var incumbent = seed == null ? SlidingSolver.Solve(board) : new List<int>(seed);
                if (!SlidingOptimizer.Replay(board, incumbent).SequenceEqual(Enumerable.Range(0, 25)))
                    throw new ArgumentException("The starting route does not solve this board.");
                return lines ? SlidingClickOptimizer.Prove(board, incumbent) : SlidingOptimizer.Prove(board, incumbent);
            }
            finally { SearchCancellation.Token = previous; }
        }
    }

    // The reader and the UI may query this at the same time. A completed worker
    // can publish only to its own request. Polling never waits on the search.
    public sealed class SliderProofSession
    {
        private sealed class Job
        {
            public int[] Board;
            public bool Lines;
            public string Theme;
            public CancellationTokenSource Stop = new CancellationTokenSource();
            public List<int> Route;
            public string Error;
            public bool Finished;
            public int Lower, Upper;
        }

        private readonly object gate = new object();
        private Job active;

        private void Release()
        {
            if (active != null && !active.Finished) active.Stop.Cancel();
            active = null;
        }

        public void Cancel() { lock (gate) Release(); }

        public void Start(int[] board, string theme, bool lines, IList<int> seed)
        {
            lock (gate)
            {
                if (active != null && active.Theme == theme && active.Lines == lines && active.Board.SequenceEqual(board)) return;
                Release();
                var job = new Job { Board = (int[])board.Clone(), Theme = theme, Lines = lines };
                var upper = seed == null ? null : new List<int>(seed);
                if (upper != null)
                {
                    List<int> clicks;
                    // A route that is not a straight click still has a length. Counting it must not throw on the UI thread.
                    job.Upper = lines && SlidingRoute.TryClickRoute(board, upper, out clicks) ? clicks.Count : upper.Count;
                }
                active = job;
                var search = new Thread(delegate()
                {
                    List<int> route = null;
                    string error = null;
                    ExactSlider.BoundChanged = delegate(int bound) { lock (gate) job.Lower = bound; };
                    try
                    {
                        // The reader runs below normal. This search stays lower
                        // so a full-window frame is not stuck behind it.
                        try { Thread.CurrentThread.Priority = ThreadPriority.Lowest; } catch (Exception) { }
                        route = ExactSlider.Solve(job.Board, upper, lines, job.Stop.Token);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { error = ex.Message; }
                    finally
                    {
                        ExactSlider.BoundChanged = null;
                        lock (gate)
                        {
                            job.Finished = true;
                            job.Route = route;
                            job.Error = error;
                            job.Stop.Dispose();
                        }
                    }
                });
                search.IsBackground = true;
                try { search.Priority = ThreadPriority.Lowest; } catch (Exception) { }
                search.Start();
            }
        }

        public string Progress()
        {
            lock (gate)
            {
                if (active == null || active.Upper == 0) return "Finding the shortest route...";
                string unit = active.Lines ? " clicks" : " moves";
                return "Finding the shortest route. " + (active.Lower > 0 ? "At least " + active.Lower + unit + ". " : "") +
                    "Quick route available in " + active.Upper + unit + ".";
            }
        }

        public bool Read(int[] board, string theme, bool lines, out List<int> route, out string error)
        {
            route = null;
            error = null;
            lock (gate)
            {
                if (active == null || active.Theme != theme || active.Lines != lines || !active.Finished) return false;
                if (active.Board.SequenceEqual(board))
                {
                    error = active.Error;
                    if (active.Route != null) route = new List<int>(active.Route);
                    return route != null;
                }
                if (active.Route == null || !SlidingRoute.Advance(active.Board, active.Route, board, out route)) return false;
                if (!lines) return true;
                int used = active.Route.Count - route.Count;
                if (used == 0 || used == active.Route.Count) return true;
                int before = used == 1 ? Array.IndexOf(active.Board, 24) : active.Route[used - 2];
                if (active.Route[used - 1] - before != active.Route[used] - active.Route[used - 1]) return true;
                // A frame partway through a line slide is a different click problem.
                route = null;
                return false;
            }
        }
    }

    public sealed partial class PuzzleSuiteForm
    {
        private readonly System.Windows.Forms.CheckBox shortestSlider = new KavrosCheckBox();
        private PuzzlePlan waitingSlider;
        private bool quickForThisSlider;

        private void UseQuickRoute()
        {
            if (waitingSlider == null || current == null || current.Kind != PuzzleKind.Sliding) return;
            quickForThisSlider = true;
            slidingBoard = null;
            analysisEngine.Proofs.Cancel();
            SolveAndShow(current, waitingSlider);
        }

        private void SetupShortestSlider()
        {
            shortestSlider.Text = "Wait for shortest slider route";
            shortestSlider.SetBounds(264, 262, 224, 24);
            shortestSlider.Checked = false;
            shortestSlider.Visible = false;
            Controls.Add(shortestSlider);
            sliderTips.SetToolTip(shortestSlider, "Prove the minimum number of clicks. Untick to use the current quick route.");
            shortestSlider.CheckedChanged += delegate
            {
                quickForThisSlider = false;
                preferences.ShortestSlider = shortestSlider.Checked;
                preferences.Save();
                if (analysisEngine == null || current == null || current.Kind != PuzzleKind.Sliding) return;
                var plan = waitingSlider ?? new PuzzlePlan { Puzzle = current, Sliding = slidingMoves, SlidingWholeRows = true };
                if (!shortestSlider.Checked) analysisEngine.Proofs.Cancel();
                SolveAndShow(current, plan);
            };
        }

        private void PollShortestSlider()
        {
            if (analysisEngine == null) return;
            if (current == null || current.Kind != PuzzleKind.Sliding)
            {
                waitingSlider = null;
                quickForThisSlider = false;
                analysisEngine.Proofs.Cancel();
                return;
            }
            if (waitingSlider == null || slidingMoves == null || slidingBatchNumber != 1) return;
            List<int> route;
            string error;
            if (!analysisEngine.Proofs.Read(current.Board, current.Theme, true, out route, out error) || route == null) return;
            if (route.Count >= slidingMoves.Count || !SliderStep.SameNextClick(current.Board, slidingMoves, route)) return;
            waitingSlider = null;
            SolveAndShow(current, new PuzzlePlan { Puzzle = current, Sliding = route, SlidingOptimal = true, SlidingWholeRows = true });
        }

        private bool AwaitShortestSlider(DetectedPuzzle puzzle, ref PuzzlePlan plan)
        {
            waitingSlider = null;
            if (quickForThisSlider && slidingBoard != null && !slidingBoard.SequenceEqual(puzzle.Board))
            {
                List<int> remaining;
                quickForThisSlider = slidingTheme == puzzle.Theme && SlidingRoute.Advance(slidingBoard, slidingMoves, puzzle.Board, out remaining);
            }
            bool lines = true;
            List<int> route;
            string error;
            if (analysisEngine.Proofs.Read(puzzle.Board, puzzle.Theme, lines, out route, out error) && route != null && (plan == null || plan.Sliding == null || SliderStep.SameNextClick(puzzle.Board, plan.Sliding, route)))
            {
                plan = new PuzzlePlan { Puzzle = puzzle, Sliding = route, SlidingOptimal = true, SlidingWholeRows = lines };
                return false;
            }
            if (plan != null && plan.Sliding != null && !plan.SlidingOptimal)
            {
                waitingSlider = plan;
                analysisEngine.Proofs.Start(puzzle.Board, puzzle.Theme, lines, plan.Sliding);
            }
            return false;
        }
    }
}
