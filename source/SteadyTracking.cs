using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace KavrosClueHelper
{
    // Shared rule for every live reading: keep the last good result through
    // brief misreads, and only treat the thing as gone after several misses
    // in a row that also span a minimum time (about 0.5 to 1 second).
    public sealed class DetectionHold
    {
        public const int MissFrames = 4;
        public static readonly TimeSpan MissTime = TimeSpan.FromMilliseconds(700);
        private int misses;
        private DateTime firstMiss;
        public int Misses { get { return misses; } }
        public void Seen() { misses = 0; }
        public void Reset() { misses = 0; }
        // Records one missed frame. Returns true while the last good result
        // should stay on screen, false once it counts as closed.
        public bool Miss(DateTime now)
        {
            if (misses == 0) firstMiss = now;
            misses++;
            return !Expired(now);
        }
        public bool Expired(DateTime now)
        {
            return misses >= MissFrames && now - firstMiss >= MissTime;
        }
    }

    // A different reading must be seen twice in a row before it replaces the
    // one on screen. One misread frame can then never swap the guidance.
    public sealed class ChangeConfirmation
    {
        private string candidate;
        public string Candidate { get { return candidate; } }
        public void Reset() { candidate = null; }
        public bool Accept(string shown, string next)
        {
            if (shown == null || next == shown) { candidate = null; return true; }
            if (next == candidate) { candidate = null; return true; }
            candidate = next;
            return false;
        }
    }

    public static class SteadyPolicy
    {
        // Board puzzles whose guidance is held through brief misses: the
        // lockbox and the towers. Slider and knot keep their own rules.
        public static bool HoldsThroughMisses(PuzzleKind kind)
        {
            return kind == PuzzleKind.Lockbox || kind == PuzzleKind.Towers || kind == PuzzleKind.Knot;
        }
        // Same board and nearly the same place: nothing to redraw.
        public static bool SameState(DetectedPuzzle shown, DetectedPuzzle next, int tolerance)
        {
            if (shown == null || next == null || shown.Kind != next.Kind || shown.Key != next.Key) return false;
            return Near(shown.Bounds, next.Bounds, tolerance) && Near(shown.Grid, next.Grid, tolerance);
        }
        public static bool Near(Rectangle a, Rectangle b, int tolerance)
        {
            return Math.Abs(a.X - b.X) <= tolerance && Math.Abs(a.Y - b.Y) <= tolerance
                && Math.Abs(a.Width - b.Width) <= tolerance && Math.Abs(a.Height - b.Height) <= tolerance;
        }
    }

    public static class LockboxStep
    {
        // A locked lockbox is read again on this interval. Short enough that a
        // click shows on the next frame, long enough that a one-frame misread
        // is still held by the normal miss rule.
        public const int RereadMilliseconds = 40;
        public static int AppliedClicks(int[] from, int[] to, int direction)
        {
            if (from == null || to == null || from.Length != 25 || to.Length != 25) return 0;
            if (direction != 1 && direction != -1) return 0;
            for (int i = 0; i < 25; i++) if (from[i] < 0 || from[i] > 2 || to[i] < 0 || to[i] > 2) return 0;
            if (Same(from, to)) return 0;
            for (int i = 0; i < 25; i++)
            {
                int[] once = (int[])from.Clone();
                LockboxSolver.Click(once, i, 1, direction);
                if (Same(once, to)) return 1;
            }
            for (int first = 0; first < 25; first++)
            for (int second = 0; second < 25; second++)
            {
                int[] twice = (int[])from.Clone();
                LockboxSolver.Click(twice, first, 1, direction);
                LockboxSolver.Click(twice, second, 1, direction);
                if (Same(twice, to)) return 2;
            }
            return 0;
        }
        static bool Same(int[] left, int[] right)
        {
            for (int i = 0; i < 25; i++) if (left[i] != right[i]) return false;
            return true;
        }
        // One or two legal clicks from the board on screen. Anything else,
        // including a corrupted read, still waits for a second frame.
        public static bool SkipConfirm(DetectedPuzzle shown, DetectedPuzzle next, int direction)
        {
            if (shown == null || next == null || shown.Kind != PuzzleKind.Lockbox || next.Kind != PuzzleKind.Lockbox) return false;
            if (!SteadyPolicy.Near(shown.Bounds, next.Bounds, 8) || !SteadyPolicy.Near(shown.Grid, next.Grid, 8)) return false;
            int clicks = AppliedClicks(shown.Board, next.Board, direction);
            return clicks == 1 || clicks == 2;
        }
    }

    public static class SliderStep
    {
        public static bool OneLegalMove(int[] from, int[] to)
        {
            if (from == null || to == null || from.Length != 25 || to.Length != 25) return false;
            int blank = Array.IndexOf(from, 24);
            if (blank < 0) return false;
            foreach (int next in SlidingSolver.Neighbours(blank))
            {
                if (to[next] != 24 || to[blank] != from[next]) continue;
                bool same = true;
                for (int i = 0; i < 25; i++)
                {
                    if (i == blank || i == next) continue;
                    if (from[i] != to[i]) { same = false; break; }
                }
                if (same) return true;
            }
            return false;
        }
        // The board did not move on screen and the tiles are one click on.
        // That frame can be shown without waiting for a second read.
        public static bool SkipConfirm(DetectedPuzzle shown, DetectedPuzzle next)
        {
            return SkipConfirm(shown, next, null);
        }
        public static bool SkipConfirm(DetectedPuzzle shown, DetectedPuzzle next, IList<int> route)
        {
            if (shown == null || next == null || shown.Kind != PuzzleKind.Sliding || next.Kind != PuzzleKind.Sliding) return false;
            if (!SteadyPolicy.Near(shown.Bounds, next.Bounds, 8) || !SteadyPolicy.Near(shown.Grid, next.Grid, 8)) return false;
            if (OneLegalMove(shown.Board, next.Board)) return true;
            List<int> remaining;
            return SlidingRoute.Advance(shown.Board, route, next.Board, out remaining);
        }
        // 0 = the fresh read matches or continues the route.
        // 1 = keep the last board and try again.
        // 2 = three bad reads, drop the route.
        public static int Reread(bool matched, bool advanced, int priorMisses)
        {
            if (matched || advanced) return 0;
            if (priorMisses >= 2) return 2;
            return 1;
        }
        // The form displays the route the engine already owns. A whole-row
        // step matches any later board on that route. This never solves.
        public static bool Follow(int[] shownBoard, IList<int> shownRoute, int[] nextBoard, IList<int> preparedRoute, out List<int> route, out int consumed)
        {
            route = null;
            consumed = 0;
            if (nextBoard == null || !SlidingSolver.IsValid(nextBoard)) return false;
            if (shownBoard != null && shownRoute != null && SlidingSolver.IsValid(shownBoard))
            {
                if (shownBoard.SequenceEqual(nextBoard))
                {
                    route = SameStep(shownBoard, shownRoute, preparedRoute);
                    return true;
                }
                List<int> remaining;
                if (SlidingRoute.Advance(shownBoard, shownRoute, nextBoard, out remaining))
                {
                    consumed = shownRoute.Count - remaining.Count;
                    route = remaining;
                    return true;
                }
            }
            if (preparedRoute != null && SlidingRoute.Playable(nextBoard, preparedRoute))
            {
                route = new List<int>(preparedRoute);
                return true;
            }
            return false;
        }
        public static bool SameNextClick(int[] board, IList<int> shown, IList<int> prepared)
        {
            List<int> left, right;
            if (board == null || shown == null || prepared == null) return false;
            if (!SlidingRoute.TryClickRoute(board, shown, out left) || !SlidingRoute.TryClickRoute(board, prepared, out right)) return false;
            return left.Count > 0 && right.Count > 0 && left[0] == right[0];
        }
        static List<int> SameStep(int[] board, IList<int> shown, IList<int> prepared)
        {
            if (prepared != null && prepared.Count < shown.Count && SlidingRoute.Playable(board, prepared) && SameNextClick(board, shown, prepared))
                return new List<int>(prepared);
            return new List<int>(shown);
        }
    }

    // Estimates the player's map position from recent movement while the
    // minimap cannot be matched. Positions are map tiles; time in UTC.
    public sealed class MotionEstimate
    {
        // RS3 running is about 3.3 tiles per second; anything faster is a
        // teleport or a misread, not movement to extrapolate.
        public const double MaxSpeed = 4.0;
        public static readonly TimeSpan MaxExtrapolation = TimeSpan.FromSeconds(1.2);
        private PointF? last;
        private DateTime lastTime;
        private double vx, vy;
        public PointF Velocity { get { return new PointF((float)vx, (float)vy); } }
        public void Reset() { last = null; vx = vy = 0; }
        public void Observe(PointF position, DateTime now)
        {
            if (last.HasValue)
            {
                double dt = (now - lastTime).TotalSeconds;
                if (dt > 0.02 && dt < 3)
                {
                    double nx = (position.X - last.Value.X) / dt, ny = (position.Y - last.Value.Y) / dt;
                    double speed = Math.Sqrt(nx * nx + ny * ny);
                    if (speed > MaxSpeed) { nx *= MaxSpeed / speed; ny *= MaxSpeed / speed; }
                    // Smooth so one jittery match cannot fling the estimate.
                    vx = vx * 0.5 + nx * 0.5; vy = vy * 0.5 + ny * 0.5;
                }
                else if (dt >= 3) { vx = vy = 0; }
            }
            last = position; lastTime = now;
        }
        public PointF? Estimate(DateTime now)
        {
            if (!last.HasValue) return null;
            double dt = (now - lastTime).TotalSeconds;
            if (dt <= 0) return last;
            dt = Math.Min(dt, MaxExtrapolation.TotalSeconds);
            return new PointF((float)(last.Value.X + vx * dt), (float)(last.Value.Y + vy * dt));
        }
    }
}
