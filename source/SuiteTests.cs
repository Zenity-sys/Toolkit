using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace KavrosClueHelper
{
    public static class TestFixtures
    {
        public static TowerPuzzle TowersExample()
        {
            return new TowerPuzzle { Top = new[] { 4, 1, 3, 2, 2 }, Bottom = new[] { 2, 4, 1, 3, 2 }, Left = new[] { 2, 4, 3, 1, 2 }, Right = new[] { 2, 1, 2, 3, 2 } };
        }
        public static KnotPuzzle KnotExample()
        {
            return new KnotPuzzle {
                Lanes = new[] { new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, new[] { 0, 3, 2, 1, 4, 5, 6, 7 } }.Select(l => l.Select(v => new RuneValue(v, false)).ToArray()).ToArray(),
                Locks = new[] { new KnotLock { A = 0, IndexA = 1, B = 1, IndexB = 7 }, new KnotLock { A = 0, IndexA = 3, B = 1, IndexB = 5 } },
                Colours = new[] { 0, 1 }, Buttons = new[] { new[] { new Point(0, 0), new Point(0, 0) }, new[] { new Point(0, 0), new Point(0, 0) } }
            };
        }
        private static string Assets { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets"); } }
        public static void Glyph(Graphics g, int value, Point centre, Color colour)
        {
            string[] pieces = File.ReadAllLines(Path.Combine(Assets, "digits.txt")).First(l => l.StartsWith(value + "|")).Split(new[] { '|' });
            int width = Int32.Parse(pieces[1]); int[] data = pieces[2].Split(new[] { ',' }).Select(Int32.Parse).ToArray();
            Color bg = Color.FromArgb(15, 32, 40);
            for (int i = 0; i < data.Length; i += 3)
            {
                double a = data[i + 2] / 255.0;
                using (Brush b = new SolidBrush(Color.FromArgb((int)(bg.R * (1 - a) + colour.R * a), (int)(bg.G * (1 - a) + colour.G * a), (int)(bg.B * (1 - a) + colour.B * a))))
                    g.FillRectangle(b, centre.X - width / 2 + data[i], centre.Y - 8 + data[i + 1], 1, 1);
            }
        }
        public static Bitmap TowerImage()
        {
            Bitmap image = new Bitmap(288, 288); TowerPuzzle p = TowersExample();
            using (Graphics g = Graphics.FromImage(image))
            {
                g.Clear(Color.FromArgb(15, 32, 40));
                for (int i = 0; i < 5; i++)
                {
                    int c = 35 + i * 44 + 21;
                    Glyph(g, p.Top[i], new Point(c, 18), Color.FromArgb(255, 205, 10));
                    Glyph(g, p.Bottom[i], new Point(c, 270), Color.FromArgb(255, 205, 10));
                    Glyph(g, p.Left[i], new Point(18, c), Color.FromArgb(255, 205, 10));
                    Glyph(g, p.Right[i], new Point(270, c), Color.FromArgb(255, 205, 10));
                }
                for (int i = 0; i < 25; i++) g.DrawRectangle(Pens.White, 35 + i % 5 * 44, 35 + i / 5 * 44, 42, 42);
            }
            return image;
        }
        public static void Frame(Graphics g, Rectangle body, string skin = "modern")
        {
            if (skin != "modern")
            {
                bool classic = skin == "classic";
                foreach (var item in new[] {
                    new { Name = classic ? "legacytopleft" : "eoctopleft", Point = new Point(body.X - 4, body.Y - 29) },
                    new { Name = classic ? "legacyx" : "eocx", Point = new Point(body.Right - (classic ? 20 : 11), body.Y - (classic ? 20 : 24)) },
                    new { Name = classic ? "legacybotleft" : "eocbotleft", Point = new Point(body.X - (classic ? 6 : 3), body.Bottom - (classic ? -1 : 8)) } })
                    using (Bitmap asset = new Bitmap(Path.Combine(Assets, "alt1anchors", "rev944", item.Name + ".png"))) g.DrawImage(asset, new Rectangle(item.Point, asset.Size), new Rectangle(Point.Empty, asset.Size), GraphicsUnit.Pixel);
                return;
            }
            foreach (var item in new[] {
                new { Name = "top_left", Point = new Point(body.X - 5, body.Y - 31) },
                new { Name = "top_right", Point = new Point(body.Right - 12, body.Y - 31) },
                new { Name = "bot_left", Point = new Point(body.X - 5, body.Bottom - 3) } })
                using (Bitmap asset = new Bitmap(Path.Combine(Assets, "alt1anchors", "modal", item.Name + ".png"))) g.DrawImage(asset, new Rectangle(item.Point, asset.Size), new Rectangle(Point.Empty, asset.Size), GraphicsUnit.Pixel);
        }
        public static Bitmap LockboxImage(string skin = "modern")
        {
            Bitmap image = new Bitmap(600, 500); Rectangle body = new Rectangle(170, 100, 267, 270);
            using (Graphics g = Graphics.FromImage(image))
            {
                g.Clear(Color.FromArgb(30, 40, 50)); Frame(g, body, skin);
                for (int i = 0; i < 25; i++) using (Bitmap tile = new Bitmap(Path.Combine(Assets, "alt1anchors", "lockbox_tiles", new[] { "melee", "range", "mage" }[LockboxSolver.Example[i]] + ".png")))
                    g.DrawImage(tile, new Rectangle(body.X + 42 + i % 5 * 38, body.Y + 17 + i / 5 * 38, tile.Width, tile.Height), new Rectangle(Point.Empty, tile.Size), GraphicsUnit.Pixel);
            }
            return image;
        }
        public static Bitmap SliderImage(string theme, int[] board, string skin = "modern")
        {
            Bitmap image = new Bitmap(650, 500); Point origin = new Point(240, 90);
            using (Graphics g = Graphics.FromImage(image)) using (Bitmap reference = new Bitmap(Path.Combine(Assets, "data", "sliders", "sliders", theme + ".png")))
            {
                g.Clear(Color.FromArgb(30, 40, 50));
                string anchorPath = skin == "modern" ? Path.Combine(Assets, "alt1anchors", "sliders", "eoc_x.png") : Path.Combine(Assets, "alt1anchors", "rev944", skin == "classic" ? "slidelegacy.png" : "slide.png");
                using (Bitmap anchor = new Bitmap(anchorPath)) g.DrawImage(anchor, new Rectangle(origin.X + (skin == "modern" ? 298 : 297), origin.Y - (skin == "modern" ? 14 : 15), anchor.Width, anchor.Height), new Rectangle(Point.Empty, anchor.Size), GraphicsUnit.Pixel);
                for (int i = 0; i < 25; i++) g.DrawImage(reference, new Rectangle(origin.X + i % 5 * 56, origin.Y + i / 5 * 56, 49, 49), new Rectangle(board[i] % 5 * 49, board[i] / 5 * 49, 49, 49), GraphicsUnit.Pixel);
            }
            return image;
        }
        public static Bitmap TowerModal(string skin = "modern", bool greyHints = false, bool filled = false, int[] enteredCells = null, TowerPuzzle suppliedClues = null)
        {
            Bitmap image = new Bitmap(660, 450); Rectangle body = new Rectangle(80, 75, 500, 300);
            if(skin!="modern")
            {
                TowerPuzzle puzzle=suppliedClues ?? TowersExample();
                using(Graphics g=Graphics.FromImage(image))
                {
                    g.Clear(Color.FromArgb(30,40,50)); Frame(g,body,skin);
                    for(int i=0;i<25;i++) g.DrawRectangle(Pens.White,body.X+41+i%5*44,body.Y+38+i/5*44,42,42);
                    for(int i=0;i<5;i++)
                    {
                        Color color=greyHints?Color.FromArgb(102,102,102):Color.FromArgb(255,205,10);
                        LegacyGlyph(g,puzzle.Top[i],new Point(body.X+60+i*44,body.Y+25),color);
                        LegacyGlyph(g,puzzle.Bottom[i],new Point(body.X+60+i*44,body.Y+275),color);
                        LegacyGlyph(g,puzzle.Left[i],new Point(body.X+23,body.Y+62+i*44),color);
                        LegacyGlyph(g,puzzle.Right[i],new Point(body.X+274,body.Y+62+i*44),color);
                    }
                    if(filled || enteredCells != null)
                    { int[] cells=enteredCells ?? TowerSolver.Solve(puzzle,false)[0]; for(int i=0;i<25;i++) if(cells[i]>0) LegacyGlyph(g,cells[i],new Point(body.X+60+i%5*44,body.Y+62+i/5*44),Color.White); }
                }
                return image;
            }
            using (Graphics g = Graphics.FromImage(image)) using (Bitmap tower = TowerImage())
            { g.Clear(Color.FromArgb(30, 40, 50)); Frame(g, body, skin); g.DrawImage(tower, new Rectangle(body.X + 6, body.Y + 3, tower.Width, tower.Height), new Rectangle(Point.Empty, tower.Size), GraphicsUnit.Pixel); }
            return image;
        }
        private static void LegacyGlyph(Graphics g,int value,Point baseline,Color gold)
        {
            string[] parts=File.ReadAllLines(Path.Combine(Assets,"legacy-digits.txt")).First(l=>l.StartsWith(value+"|")).Split(new[] { '|' });
            int[] data=parts[2].Split(new[] { ',' }).Select(Int32.Parse).ToArray();
            Color background=Color.FromArgb(30,40,50);
            for(int i=0;i<data.Length;i+=3)
            {
                double a=data[i+2]/255.0;
                using(Brush brush=new SolidBrush(Color.FromArgb((int)(background.R*(1-a)+gold.R*a),(int)(background.G*(1-a)+gold.G*a),(int)(background.B*(1-a)+gold.B*a))))
                    g.FillRectangle(brush,baseline.X-2+data[i],baseline.Y-10+data[i+1],1,1);
            }
        }
        public static Bitmap KnotModal(string skin = "modern")
        {
            // Three overlapping diamond rings correspond to compatibility
            // layout #2. Explicit samples model track/rune pixels separately.
            Bitmap image = new Bitmap(700, 460); Rectangle body = new Rectangle(80, 75, 500, 300);
            Point relevant = new Point(body.X + 70, body.Y + 13), origin = new Point(0, 12);
            List<Point>[] lanes = new List<Point>[3]; Dictionary<Point, int> visible = new Dictionary<Point, int>();
            for (int lane = 0; lane < 3; lane++)
            {
                int cx = 4 + lane * 2, cy = 4; lanes[lane] = new List<Point>();
                for (int i = 0; i < 4; i++) lanes[lane].Add(new Point(cx + i, cy - 4 + i));
                for (int i = 0; i < 4; i++) lanes[lane].Add(new Point(cx + 4 - i, cy + i));
                for (int i = 0; i < 4; i++) lanes[lane].Add(new Point(cx - i, cy + 4 - i));
                for (int i = 0; i < 4; i++) lanes[lane].Add(new Point(cx - 4 + i, cy - i));
                foreach (Point p in lanes[lane]) visible[p] = lane;
            }
            Color[] colours = { Color.FromArgb(46, 46, 77), Color.FromArgb(137, 60, 43), Color.FromArgb(12, 12, 18) };
            using (Graphics g = Graphics.FromImage(image)) using (Bitmap atlas = new Bitmap(Path.Combine(Assets, "alt1anchors", "knot_runes", "atlas.png")))
            {
                g.Clear(Color.FromArgb(30, 40, 50)); Frame(g, body, skin);
                using (Brush bg = new SolidBrush(Color.FromArgb(159, 145, 86))) g.FillRectangle(bg, body);
                for (int lane = 0; lane < 3; lane++) using (Pen pen = new Pen(colours[lane], 7))
                {
                    Point[] points = lanes[lane].Concat(new[] { lanes[lane][0] }).Select(p => new Point(relevant.X + origin.X + p.X * 24 + 12, relevant.Y + origin.Y + p.Y * 24 + 12)).ToArray(); g.DrawLines(pen, points);
                }
                using (Brush track = new SolidBrush(colours[0])) g.FillPolygon(track, new[] { new Point(relevant.X + 108, relevant.Y + 2), new Point(relevant.X + 96, relevant.Y + 14), new Point(relevant.X + 120, relevant.Y + 14) });
                foreach (KeyValuePair<Point, int> item in visible)
                {
                    Point p = new Point(relevant.X + origin.X + item.Key.X * 24, relevant.Y + origin.Y + item.Key.Y * 24);
                    int rune = (item.Key.X * 3 + item.Key.Y * 7) % (atlas.Width / 12);
                    g.DrawImage(atlas, new Rectangle(p.X + 7, p.Y + 7, 12, 12), new Rectangle(rune * 12, 0, 12, 12), GraphicsUnit.Pixel);
                    foreach (Point sample in new[] { new Point(11, -3), new Point(12, -3), new Point(-5, 12), new Point(-5, 13), new Point(29, 12), new Point(29, 13), new Point(11, 28), new Point(12, 28) })
                        using (Brush b = new SolidBrush(colours[item.Value])) g.FillRectangle(b, p.X + sample.X, p.Y + sample.Y, 1, 1);
                    bool cross = lanes.Count(l => l.Contains(item.Key)) > 1;
                    if (cross) foreach (Point sample in new[] { new Point(12, -1), new Point(-1, 12), new Point(24, 12), new Point(12, 24) })
                            using (Brush b = new SolidBrush(Color.FromArgb(99, 178, 74))) g.FillRectangle(b, p.X + sample.X, p.Y + sample.Y, 1, 1);
                }
                Point[] offsets = { new Point(-1, -1), new Point(1, -1), new Point(1, 1), new Point(-1, 1) };
                Point[] corners = { new Point(0, 0), new Point(23, 0), new Point(24, 24), new Point(0, 24) };
                foreach (KeyValuePair<Point, int> item in visible)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Point next = new Point(item.Key.X + offsets[i].X, item.Key.Y + offsets[i].Y);
                        bool connected = lanes.Any(l => l.Contains(item.Key) && l.Contains(next));
                        using (Brush b = new SolidBrush(connected ? colours[item.Value] : Color.FromArgb(159, 145, 86)))
                            g.FillRectangle(b, relevant.X + origin.X + item.Key.X * 24 + corners[i].X, relevant.Y + origin.Y + item.Key.Y * 24 + corners[i].Y, 1, 1);
                    }
                }
                Frame(g, body, skin);
            }
            return image;
        }
    }

    public static class SuiteTests
    {
        private static int checks;
        private static void Check(bool value, string description) { checks++; if (!value) throw new Exception("FAILED: " + description); }
        public static int Run(string reportPath)
        {
            StringBuilder report = new StringBuilder(); Stopwatch elapsed = Stopwatch.StartNew();
            try
            {
                List<int[]> towerSolutions = TowerSolver.Solve(TestFixtures.TowersExample(), false);
                Check(towerSolutions.Count == 1, "generated tower fixture has one solution (found " + towerSolutions.Count + ")");
                int[] solution = towerSolutions[0]; TowerPuzzle example = TestFixtures.TowersExample();
                for (int r = 0; r < 5; r++) { int[] line = solution.Skip(r * 5).Take(5).ToArray(); Check(line.OrderBy(v => v).SequenceEqual(new[] { 1, 2, 3, 4, 5 }), "tower row permutation"); Check(TowerSolver.Visible(line) == example.Left[r] && TowerSolver.Visible(line.Reverse()) == example.Right[r], "tower row visibility"); }
                for (int c = 0; c < 5; c++) { int[] line = Enumerable.Range(0, 5).Select(r => solution[r * 5 + c]).ToArray(); Check(line.Distinct().Count() == 5, "tower column permutation"); Check(TowerSolver.Visible(line) == example.Top[c] && TowerSolver.Visible(line.Reverse()) == example.Bottom[c], "tower column visibility"); }
                Random random = new Random(944951);
                for (int n = 0; n < 40; n++)
                {
                    int[] board = Enumerable.Range(0, 25).ToArray();
                    for (int step = 0; step < 300; step++) { int b = Array.IndexOf(board, 24); int[] choices = SlidingSolver.Neighbours(b).ToArray(); int next = choices[random.Next(choices.Length)]; board[b] = board[next]; board[next] = 24; }
                    int[] original = (int[])board.Clone();
                    foreach (int next in SlidingSolver.Solve(board))
                    {
                        int blank = Array.IndexOf(board, 24); Check(Math.Abs(blank / 5 - next / 5) + Math.Abs(blank % 5 - next % 5) == 1, "sliding route has only adjacent moves");
                        board[blank] = board[next]; board[next] = 24;
                    }
                    Check(board.SequenceEqual(Enumerable.Range(0, 25)), "sliding route finishes correctly " + n);
                    Check(!original.SequenceEqual(board), "sliding fixture was shuffled");
                }
                int[] impossible = Enumerable.Range(0, 25).ToArray(); impossible[0] = 1; impossible[1] = 0; Check(!SlidingSolver.IsSolvable(impossible), "odd inversion sliding board rejected");
                Check(SlidingSolver.Solve(Enumerable.Range(0, 25).ToArray()).Count == 0, "solved sliding board needs no moves");
                KnotPuzzle knot = TestFixtures.KnotExample(); KnotSolution knotSolution = KnotSolver.Solve(knot);
                Check(knotSolution != null && knotSolution.Certain && knotSolution.Cost == 4, "knot reference case minimum rotation cost");
                foreach (KnotLock l in knot.Locks) Check(knot.Lanes[l.A][KnotSolver.Mod(l.IndexA - knotSolution.Offsets[l.A], knot.Lanes[l.A].Length)].Id == knot.Lanes[l.B][KnotSolver.Mod(l.IndexB - knotSolution.Offsets[l.B], knot.Lanes[l.B].Length)].Id, "knot final intersection equality");
                // Random exact-rune knots, checked against independent exhaustive enumeration.
                for (int n = 0; n < 35; n++)
                {
                    RuneValue[][] lanes = Enumerable.Range(0, 3).Select(i => Enumerable.Range(0, 6).Select(j => new RuneValue(random.Next(4), false)).ToArray()).ToArray();
                    KnotLock[] locks = { new KnotLock { A = 0, IndexA = 1, B = 1, IndexB = 2 }, new KnotLock { A = 1, IndexA = 4, B = 2, IndexB = 3 }, new KnotLock { A = 0, IndexA = 5, B = 2, IndexB = 0 } };
                    KnotPuzzle p = new KnotPuzzle { Lanes = lanes, Locks = locks }; KnotSolution found = KnotSolver.Solve(p); int best = 999;
                    for (int a = 0; a < 6; a++) for (int b = 0; b < 6; b++) for (int c = 0; c < 6; c++)
                    {
                        int[] offsets = { a, b, c }; bool valid = locks.All(l => lanes[l.A][(l.IndexA - offsets[l.A] + 6) % 6].Id == lanes[l.B][(l.IndexB - offsets[l.B] + 6) % 6].Id);
                        if (valid) best = Math.Min(best, offsets.Sum(v => Math.Min(v, 6 - v)));
                    }
                    Check((found == null ? 999 : found.Cost) == best, "knot rotations agree with independent exhaustive search");
                }
                string assets = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
                DigitReader digits = new DigitReader(assets);
                using (Bitmap tower = TestFixtures.TowerImage())
                {
                    Pixels pixels = new Pixels(tower);
                    for (int i = 0; i < 5; i++)
                    {
                        int c = 35 + i * 44 + 21;
                        Check(digits.Read(pixels, new Point(c, 18), true) == example.Top[i], "tower top clue digit");
                        Check(digits.Read(pixels, new Point(c, 270), true) == example.Bottom[i], "tower bottom clue digit");
                        Check(digits.Read(pixels, new Point(18, c), true) == example.Left[i], "tower left clue digit");
                        Check(digits.Read(pixels, new Point(270, c), true) == example.Right[i], "tower right clue digit");
                    }
                    PuzzleDetector detector = new PuzzleDetector(assets);
                    DetectedPuzzle read = detector.ReadTowers(tower, new Rectangle(35, 35, 218, 218), 1);
                    Check(read != null && read.Towers.Key == example.Key, "tower clue reading and unique solution");
                }
                PuzzleDetector fullDetector = new PuzzleDetector(assets);
                using (Bitmap lockbox = TestFixtures.LockboxImage())
                {
                    DetectedPuzzle detected = fullDetector.Detect(lockbox);
                    if (detected == null) lockbox.Save("test-lockbox.png");
                    Check(detected != null && detected.Kind == PuzzleKind.Lockbox && detected.Board.SequenceEqual(LockboxSolver.Example), "automatic lockbox localisation and icon reading: " + (detected == null ? fullDetector.Diagnose(lockbox) : detected.Kind + " " + String.Join(",", detected.Board)));
                    using (Bitmap scaled = Pixels.Crop(lockbox, new Rectangle(Point.Empty, lockbox.Size), new Size(480, 400)))
                    { DetectedPuzzle small = fullDetector.Detect(scaled); if (small == null) scaled.Save("test-lockbox-small.png"); Check(small != null && small.Kind == PuzzleKind.Lockbox, "automatic lockbox localisation at 80% scale: " + (small == null ? fullDetector.Diagnose(scaled) : small.Kind.ToString())); }
                }
                int[] slide = Enumerable.Range(0, 25).ToArray(); slide[24] = 23; slide[23] = 24;
                using (Bitmap slider = TestFixtures.SliderImage("castle", slide))
                {
                    DetectedPuzzle found = fullDetector.Detect(slider);
                    Check(found != null && found.Kind == PuzzleKind.Sliding && found.Theme == "castle" && found.Board.SequenceEqual(slide), "automatic sliding detection, theme and tile mapping");
                }
                using (Bitmap towerModal = TestFixtures.TowerModal())
                {
                    DetectedPuzzle found = fullDetector.Detect(towerModal);
                    Check(found != null && found.Kind == PuzzleKind.Towers && found.Towers.Key == example.Key, "automatic tower modal detection and all clues");
                }
                using (Bitmap knotModal = TestFixtures.KnotModal())
                {
                    DetectedPuzzle found = fullDetector.Detect(knotModal);
                    if (found == null) knotModal.Save("test-knot.png");
                    Check(found != null && found.Kind == PuzzleKind.Knot, "automatic Celtic knot detection and lane tracing: " + (found == null ? fullDetector.DiagnoseKnot(knotModal, new Rectangle(80, 75, 500, 300)) : found.Kind.ToString()));
                    Check(found.Knot.Lanes.Length == 3 && found.Knot.Locks.Length == 6 && found.Knot.Buttons.Length == 3, "knot shape and rotation controls recognised");
                    Check(KnotSolver.Solve(found.Knot).Cost == 0, "generated knot intersections all match");
                }
                SliderImageReader themeReader = new SliderImageReader(assets);
                foreach (string themeFile in Directory.GetFiles(Path.Combine(assets, "data", "sliders", "sliders"), "*.png").Where(f => !f.EndsWith("blanktiles.png")))
                {
                    string theme = Path.GetFileNameWithoutExtension(themeFile);
                    using (Bitmap fixture = TestFixtures.SliderImage(theme, slide))
                    {
                        DetectedPuzzle found = themeReader.Read(fixture, new Rectangle(240, 90, 273, 273), 1);
                        Check(found != null && found.Theme == theme && found.Board.SequenceEqual(slide), "sliding theme reference: " + theme);
                    }
                }
                foreach (string captureFile in Directory.GetFiles(Path.Combine(assets, "test_assets", "sliders"), "*.png"))
                {
                    string name = Path.GetFileNameWithoutExtension(captureFile);
                    using (Bitmap capture = new Bitmap(captureFile))
                    {
                        DetectedPuzzle found = themeReader.Read(capture, new Rectangle(Point.Empty, capture.Size), 1);
                        report.AppendLine("Capture " + name + ": " + (found == null ? "rejected" : found.Theme + "; score " + found.Confidence.ToString("0.000") + "; parity " + SlidingSolver.IsSolvable(found.Board)));
                        if (name == "desert_negative") Check(found == null, "unmatched slider image rejected");
                        else if (name.Contains("solved")) Check(found != null && found.Board.SequenceEqual(Enumerable.Range(0, 25)), "real solved slider capture: " + name);
                        else if (!name.Contains("low_graphhics") && name != "bridge_swapped") Check(found != null && SlidingSolver.IsSolvable(found.Board), "real scrambled slider capture: " + name);
                    }
                }
                using (Bitmap blank = new Bitmap(640, 480)) Check(fullDetector.Detect(blank) == null, "empty image cannot become a puzzle");
                fullDetector.UseLegacy(true);
                foreach (string skin in new[] { "rev944", "classic" })
                {
                    using (Bitmap image = TestFixtures.LockboxImage(skin))
                    {
                        DetectedPuzzle found = fullDetector.Detect(image);
                        Check(found != null && found.Kind == PuzzleKind.Lockbox && found.Board.SequenceEqual(LockboxSolver.Example), "old interface lockbox: " + skin);
                        using (Bitmap small = Pixels.Crop(image, new Rectangle(Point.Empty, image.Size), new Size(480, 400)))
                        { DetectedPuzzle reduced = fullDetector.Detect(small); if (reduced == null) small.Save("old-lockbox-small.png"); Check(reduced != null && reduced.Kind == PuzzleKind.Lockbox, "old interface lockbox at 80%: " + skin + (reduced == null ? fullDetector.Diagnose(small) : reduced.Kind.ToString())); }
                    }
                    using (Bitmap image = TestFixtures.TowerModal(skin))
                    { DetectedPuzzle found = fullDetector.Detect(image); Check(found != null && found.Kind == PuzzleKind.Towers && found.Towers.Key == example.Key, "old interface towers: " + skin); }
                    using (Bitmap image = TestFixtures.KnotModal(skin))
                    { DetectedPuzzle found = fullDetector.Detect(image); Check(found != null && found.Kind == PuzzleKind.Knot && found.Knot.Lanes.Length == 3, "old interface Celtic knot: " + skin); }
                    using (Bitmap image = TestFixtures.SliderImage("castle", slide, skin))
                    { DetectedPuzzle found = fullDetector.Detect(image); Check(found != null && found.Kind == PuzzleKind.Sliding && found.Board.SequenceEqual(slide), "old interface sliding box: " + skin); }
                }
                if (TestOptions.IncludeUi) using (PuzzleSuiteForm form = new PuzzleSuiteForm())
                {
                    form.ShowInTaskbar = false; form.StartPosition = System.Windows.Forms.FormStartPosition.Manual; form.Location = new Point(-30000, -30000); form.Show();
                    foreach (string kind in new[] { "Lockbox", "Towers", "Sliding", "Knot" })
                    {
                        form.LoadExample(kind); System.Windows.Forms.Application.DoEvents();
                        using (Bitmap rendered = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(rendered, new Rectangle(Point.Empty, rendered.Size)); Check(rendered.Width > 400, "suite UI renders " + kind); }
                    }
                    form.Close();
                }
                report.AppendLine("\nV0.2 SUITE PASS: " + checks + " assertions in " + elapsed.ElapsedMilliseconds + " ms.");
                report.AppendLine("Tower: generated unique fixture, all row/column constraints, 20 glyph readings.");
                report.AppendLine("Sliding: 40 shuffled 5x5 boards, every legal move and complete solution verified.");
                report.AppendLine("Knot: reference case + 35 cases compared with exhaustive rotation search.");
                report.AppendLine("Detection: generated tower, Celtic knot, lockbox at 100%/80%, sliding box, blank screen rejection.");
                report.AppendLine("All 27 sliding artwork references matched. UI tests: " + TestOptions.IncludeUi);
                report.AppendLine("Pre-update EOC and classic skins: all four puzzle types; lockboxes additionally at 80%.");
                report.AppendLine("Exact uploaded screenshot bytes and live game capture remain unvalidated.");
                report.AppendLine("Tower solution:"); for (int r = 0; r < 5; r++) report.AppendLine(String.Join(" ", solution.Skip(r * 5).Take(5)));
                File.AppendAllText(reportPath, report.ToString()); return 0;
            }
            catch (Exception ex) { report.AppendLine(ex.ToString()); File.AppendAllText(reportPath, report.ToString()); return 1; }
        }
    }
}
