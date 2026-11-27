// Adapted from Clue Trainer's MIT-licensed KnotReader and CelticKnots.
// Copyright (c) 2024 Lukas Gail. See THIRD_PARTY.txt for the full notice.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace KavrosClueHelper
{
    public sealed class KnotImageReader
    {
        public string LastError = "";
        private sealed class Tile
        {
            public Point Position;
            public int Id, Colour;
            public bool Intersection, Matches;
            public bool[] Neighbours;
            public double RuneScore, TrackScore, MatchMargin;
        }
        private sealed class Remembered
        {
            public ulong Hash;
            public int Id, Colour;
            public bool Empty, Intersection, Matches;
            public bool[] Neighbours;
            public double RuneScore, TrackScore, MatchMargin;
        }
        private sealed class Lane { public int Colour; public List<Tile> Tiles = new List<Tile>(); }
        private sealed class Layout { public long[] Hash; public Point[][] Buttons; }
        private readonly List<double[]> runes = new List<double[]>();
        private readonly List<Layout> layouts = new List<Layout>();
        private Remembered[,] remembered;
        private Point rememberedOrigin;
        public int CacheHits, CacheMisses;
        private static readonly Point[] Directions = { new Point(-1, -1), new Point(1, -1), new Point(1, 1), new Point(-1, 1) };
        private static readonly Point[] Corners = { new Point(0, 0), new Point(23, 0), new Point(24, 24), new Point(0, 24) };
        private static readonly Point[] TrackSamples = { new Point(11, -3), new Point(12, -3), new Point(-5, 12), new Point(-5, 13), new Point(29, 12), new Point(29, 13), new Point(11, 28), new Point(12, 28) };
        private static readonly Point[] MatchSamples = { new Point(12, -1), new Point(-1, 12), new Point(24, 12), new Point(12, 24) };
        private static readonly Color[] Backgrounds = Colours(new[] { 159, 145, 86, 177, 161, 95, 144, 131, 79 });
        private static readonly Color[] Greens = Colours(new[] { 99, 178, 74, 86, 155, 64, 108, 195, 81, 130, 235, 98 });
        private static readonly Color[] Reds = Colours(new[] { 60, 0, 0, 86, 0, 0, 92, 0, 0, 106, 0, 0 });
        private static readonly Color[][] Tracks = {
            Colours(new[] { 46,46,77,45,45,75,28,28,47,35,25,58,50,50,85 }),
            Colours(new[] { 137,60,43,67,29,21,97,43,30,62,27,17 }),
            Colours(new[] { 12,12,18,11,11,16,18,18,27 }),
            Colours(new[] { 43,30,0,159,112,0,205,146,0,104,74,0,137,97,0,247,180,19 }),
            Colours(new[] { 95,89,76,123,116,98,30,28,24 })
        };
        private static Color[] Colours(int[] values) { return Enumerable.Range(0, values.Length / 3).Select(i => Color.FromArgb(values[3 * i], values[3 * i + 1], values[3 * i + 2])).ToArray(); }
        private static bool Background(Color colour) { return Backgrounds.Max(c => Pixels.ColourSimilarity(c, colour)) > 0.9; }
        public KnotImageReader(string directory)
        {
            using (Bitmap atlas = new Bitmap(Path.Combine(directory, "alt1anchors", "knot_runes", "atlas.png")))
            {
                Pixels pixels = new Pixels(atlas);
                for (int x = 0; x + 12 <= atlas.Width; x += 12) runes.Add(pixels.Fingerprint(new Rectangle(x, 0, 12, 12), 4, 4, false));
            }
            foreach (string line in File.ReadAllLines(Path.Combine(directory, "knot-buttons.txt")))
            {
                string[] parts = line.Split(new[] { '|' });
                layouts.Add(new Layout { Hash = parts[0].Split(new[] { ',' }).Select(Int64.Parse).ToArray(), Buttons = parts[1].Split(new[] { ';' }).Select(b => { int[] v = b.Split(new[] { ',' }).Select(Int32.Parse).ToArray(); return new[] { new Point(v[0], v[1]), new Point(v[2], v[3]) }; }).ToArray() });
            }
        }
        private static Bitmap RemoveGuidance(Bitmap body)
        {
            var pixels=new Pixels(body);Bitmap clean=null;
            foreach(Color colour in new[]{Color.Lime,Color.Gold,Color.Silver})
            for(int y=0;y<body.Height-28;y++)for(int x=0;x<body.Width-28;x++)
            {
                if(pixels.Error(x+6,y,colour)>35)continue;
                foreach(int size in new[]{26,25,27})
                {
                    int hits=0;foreach(int along in new[]{4,12,22})
                    {if(pixels.Error(x+along,y,colour)<40)hits++;if(pixels.Error(x+along,y+size,colour)<40)hits++;if(pixels.Error(x,y+along,colour)<40)hits++;if(pixels.Error(x+size,y+along,colour)<40)hits++;}
                    if(hits<11)continue;
                    int r=0,g=0,b=0,n=0;for(int yy=y-6;yy<=y+size+6;yy++)for(int xx=x-6;xx<=x+size+6;xx++)
                    {if(xx>=x-2&&xx<=x+size+2&&yy>=y-2&&yy<=y+size+2)continue;Color c=pixels.Get(xx,yy);if(Background(c)){r+=c.R;g+=c.G;b+=c.B;n++;}}
                    if(clean==null)clean=(Bitmap)body.Clone();using(Graphics drawing=Graphics.FromImage(clean))using(Brush fill=new SolidBrush(n>0?Color.FromArgb(r/n,g/n,b/n):Backgrounds[0]))drawing.FillRectangle(fill,x-2,y-2,size+5,size+5);
                    x+=size;break;
                }
            }
            return clean;
        }
        public KnotPuzzle Read(Bitmap body)
        {
            LastError = "find origin";
            if (body.Width < 350 || body.Height < 180) return null;
            using(Bitmap clean=RemoveGuidance(body))
            using (Bitmap relevant = Pixels.Crop(clean??body, new Rectangle(70, 13, body.Width - 140, body.Height - 26), new Size(body.Width - 140, body.Height - 26)))
            {
                Pixels pixels = new Pixels(relevant);
                Point? intercepted = null; bool above = true;
                for (int y = 5; y < Math.Min(200, pixels.Height - 25); y++)
                {
                    bool current = Background(pixels.Get(107, y));
                    if (!current && above && !Background(pixels.Get(107, y + 24))) { intercepted = new Point(107, y); break; }
                    above = current;
                }
                if (!intercepted.HasValue) return null;
                Point tip = intercepted.Value;
                while (tip.Y > 0 && tip.X > 0 && tip.X < pixels.Width - 2)
                {
                    if (!Background(pixels.Get(tip.X - 1, tip.Y - 1))) tip = new Point(tip.X - 1, tip.Y - 1);
                    else if (!Background(pixels.Get(tip.X + 1, tip.Y - 1))) tip = new Point(tip.X + 1, tip.Y - 1);
                    else break;
                }
                if (!Background(pixels.Get(tip.X - 1, tip.Y))) tip.X--;
                Point tile = new Point(tip.X - 11, tip.Y + 9);
                int column = (int)Math.Floor(tile.X / 24.0), row = (int)Math.Floor(tile.Y / 24.0);
                Point origin = new Point(tile.X - column * 24, tile.Y - row * 24);
                bool odd = (column + row) % 2 != 0;
                int width = (pixels.Width - origin.X) / 24, height = (pixels.Height - origin.Y) / 24;
                LastError = "grid origin " + origin + " size " + width + "x" + height;
                if (width < 5 || width > 22 || height < 4 || height > 18) return null;
                Tile[,] grid = new Tile[width, height]; int valid = 0;
                double worstRune = 1, worstTrack = 1, worstMargin = 4;
                if (remembered == null || rememberedOrigin != origin || remembered.GetLength(0) != width || remembered.GetLength(1) != height)
                { remembered = new Remembered[width, height]; rememberedOrigin = origin; }
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                {
                    if (((x + y) % 2 != 0) != odd) continue;
                    Point position = new Point(origin.X + x * 24, origin.Y + y * 24);
                    ulong hash = HashTile(pixels, position);
                    Remembered prior = remembered[x, y];
                    Tile value;
                    if (prior != null && prior.Hash == hash)
                    {
                        CacheHits++;
                        value = prior.Empty ? null : new Tile { Id = prior.Id, Colour = prior.Colour, Intersection = prior.Intersection, Matches = prior.Matches, Neighbours = prior.Neighbours, RuneScore = prior.RuneScore, TrackScore = prior.TrackScore, MatchMargin = prior.MatchMargin };
                    }
                    else
                    {
                        CacheMisses++;
                        value = ReadTile(pixels, position);
                        remembered[x, y] = new Remembered { Hash = hash, Empty = value == null, Id = value == null ? 0 : value.Id, Colour = value == null ? 0 : value.Colour, Intersection = value != null && value.Intersection, Matches = value != null && value.Matches, Neighbours = value == null ? null : value.Neighbours, RuneScore = value == null ? 0 : value.RuneScore, TrackScore = value == null ? 0 : value.TrackScore, MatchMargin = value == null ? 0 : value.MatchMargin };
                    }
                    if (value != null)
                    {
                        value.Position = new Point(x, y); grid[x, y] = value; valid++;
                        worstRune = Math.Min(worstRune, value.RuneScore); worstTrack = Math.Min(worstTrack, value.TrackScore);
                        if (value.Intersection) worstMargin = Math.Min(worstMargin, value.MatchMargin);
                    }
                }
                if (valid < 20) { LastError += "; valid tiles " + valid; return null; }
                bool rs3 = false;
                for (int y = 0; y < height && !rs3; y++) for (int x = 0; x < width; x++) if (grid[x, y] != null && grid[x, y].Intersection && RedRing(pixels, new Point(origin.X + x * 24, origin.Y + y * 24))) rs3 = true;
                if (rs3) for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                {
                    Tile cell = grid[x, y];
                    if (cell == null || !cell.Intersection) continue;
                    cell.Matches = !RedRing(pixels, new Point(origin.X + x * 24, origin.Y + y * 24));
                }
                // A red ring replaces the old green pip. The pip samples then
                // land in the empty middle and the margin is not a real doubt.
                if (rs3) worstMargin = Math.Max(worstMargin, 1);
                List<Lane> lanes = new List<Lane>();
                while (true)
                {
                    Tile start = null;
                    for (int y = 0; y < height && start == null; y++) for (int x = 0; x < width; x++)
                    { Tile t = grid[x, y]; if (t != null && !t.Intersection && !lanes.Any(l => l.Colour == t.Colour)) { start = t; break; } }
                    if (start == null) break;
                    Lane lane = new Lane { Colour = start.Colour }; int direction = 2; Tile current = start;
                    while (true)
                    {
                        lane.Tiles.Add(current);
                        LastError = "lane " + lane.Colour + "; step " + lane.Tiles.Count + " at " + current.Position + " dir " + direction;
                        Point next = new Point(current.Position.X + Directions[direction].X, current.Position.Y + Directions[direction].Y);
                        if (next.X < 0 || next.Y < 0 || next.X >= width || next.Y >= height) return null;
                        current = grid[next.X, next.Y];
                        if (current == null || lane.Tiles.Count > 40) return null;
                        if (current == start) break;
                        if (lane.Tiles.Contains(current)) return null;
                        if (!current.Intersection && !current.Neighbours[direction])
                        {
                            int nextDirection = Enumerable.Range(0, 4).Where(i => i != (direction + 2) % 4 && current.Neighbours[i]).DefaultIfEmpty(-1).First();
                            if (nextDirection < 0) return null; direction = nextDirection;
                        }
                    }
                    if (lane.Tiles.Count < 10 || lanes.Count >= 5) return null;
                    lanes.Add(lane);
                }
                if (lanes.Count < 3) { LastError = "too few lanes: " + lanes.Count; return null; }
                List<KnotLock> locks = new List<KnotLock>();
                for (int a = 0; a < lanes.Count; a++) for (int i = 0; i < lanes[a].Tiles.Count; i++)
                {
                    Tile t = lanes[a].Tiles[i];
                    if (!t.Intersection || t.Colour == lanes[a].Colour) continue;
                    int b = lanes.FindIndex(l => l.Colour == t.Colour);
                    if (b < 0) return null;
                    int j = lanes[b].Tiles.IndexOf(t); if (j < 0) return null;
                    locks.Add(new KnotLock { A = a, IndexA = i, B = b, IndexB = j });
                }
                if (locks.Count < 3) { LastError = "too few locks: " + locks.Count; return null; }
                // A still capture from the game scores a rune just under 0.93.
                // 0.92 still rejects a blurred or half-read ring.
                KnotPuzzle puzzle = new KnotPuzzle { Lanes = lanes.Select(l => l.Tiles.Select(t => new RuneValue(t.Id, t.Intersection && !t.Matches && t.Colour != l.Colour)).ToArray()).ToArray(), Locks = locks.ToArray(), Colours = lanes.Select(l => l.Colour).ToArray(), MinRune = worstRune, MinTrack = worstTrack, MinMargin = worstMargin, Reliable = worstRune >= 0.92 && worstTrack >= 0.88 && worstMargin >= 0.45 };
                long[] shape = puzzle.Lanes.Select(l => (long)l.Length).Concat(puzzle.Locks.Select(l => l.A < l.B ? 10000L * (l.A * 100 + l.IndexA) + l.B * 100 + l.IndexB : 10000L * (l.B * 100 + l.IndexB) + l.A * 100 + l.IndexA).OrderBy(h => h)).ToArray();
                Layout layout = layouts.Find(l => l.Hash.SequenceEqual(shape));
                if (layout == null) { LastError = "unknown shape " + String.Join(",", shape); return null; }
                puzzle.Buttons = layout.Buttons.Select(pair => pair.Select(p => new Point(70 + origin.X + p.X * 24 + 12, 13 + origin.Y + p.Y * 24 + 12)).ToArray()).ToArray();
                return puzzle;
            }
        }
        private Tile ReadTile(Pixels image, Point p)
        {
            bool[] background = Corners.Select(c => Background(image.Get(p.X + c.X, p.Y + c.Y))).ToArray();
            int neighbours = background.Count(v => v); if (neighbours > 2) return null;
            double[] fingerprint = image.Fingerprint(new Rectangle(p.X + 7, p.Y + 7, 12, 12), 4, 4, false);
            double score = 0; int id = -1;
            for (int i = 0; i < runes.Count; i++) { double s = Pixels.Similarity(fingerprint, runes[i], false); if (s > score) { score = s; id = i; } }
            if (score < 0.89) return null;
            int colour = -1; double certainty = 0;
            for (int i = 0; i < Tracks.Length; i++)
            {
                double s = TrackSamples.Average(t => Tracks[i].Max(c => Pixels.ColourSimilarity(c, image.Get(p.X + t.X, p.Y + t.Y))));
                if (s > certainty) { certainty = s; colour = i; }
            }
            if (certainty < 0.8) return null;
            bool intersection = neighbours < 2;
            double green = MatchSamples.Sum(s => Greens.Max(c => Pixels.ColourSimilarity(c, image.Get(p.X + s.X, p.Y + s.Y))));
            double red = MatchSamples.Sum(s => Reds.Max(c => Pixels.ColourSimilarity(c, image.Get(p.X + s.X, p.Y + s.Y))));
            // The current client draws a red ring around a crossing that does
            // not match. Older knots use a small green or red pip instead.
            bool matches = green > red;
            return new Tile { Id = id, Colour = colour, Intersection = intersection, Matches = matches, Neighbours = background.Select(b => !b).ToArray(), RuneScore = score, TrackScore = certainty, MatchMargin = Math.Abs(green - red) };
        }
        private static bool RedRing(Pixels image, Point p)
        {
            int red = 0;
            for (int dy = -12; dy <= 12; dy++) for (int dx = -12; dx <= 12; dx++)
            {
                int d2 = dx * dx + dy * dy;
                if (d2 < 81 || d2 > 144) continue;
                Color c = image.Get(p.X + 13 + dx, p.Y + 13 + dy);
                if (c.R > 90 && c.G < 40 && c.B < 40 && c.R > c.G + 60) red++;
            }
            return red >= 10;
        }
        private static ulong HashTile(Pixels image, Point p)
        {
            unchecked
            {
                ulong hash = 14695981039346656037UL;
                for (int y = -6; y <= 30; y++) for (int x = -6; x <= 30; x++)
                {
                    Color colour = image.Get(p.X + x, p.Y + y);
                    hash ^= colour.R; hash *= 1099511628211UL;
                    hash ^= colour.G; hash *= 1099511628211UL;
                    hash ^= colour.B; hash *= 1099511628211UL;
                }
                return hash;
            }
        }

        public static KnotPuzzle Merge(KnotPuzzle current, KnotPuzzle previous)
        {
            if (previous == null || current.Lanes.Length != previous.Lanes.Length || current.Locks.Length != previous.Locks.Length || !current.Lanes.Select(l => l.Length).SequenceEqual(previous.Lanes.Select(l => l.Length)) || !current.Colours.SequenceEqual(previous.Colours)
                || !current.Locks.Select(l => l.A + ":" + l.IndexA + ":" + l.B + ":" + l.IndexB).SequenceEqual(previous.Locks.Select(l => l.A + ":" + l.IndexA + ":" + l.B + ":" + l.IndexB))) return current;
            RuneValue[][] merged = new RuneValue[current.Lanes.Length][];
            for (int lane = 0; lane < current.Lanes.Length; lane++)
            {
                RuneValue[] a = current.Lanes[lane], b = previous.Lanes[lane];
                List<RuneValue[]> choices = new List<RuneValue[]>();
                for (int offset = 0; offset < a.Length; offset++)
                {
                    RuneValue[] candidate = new RuneValue[a.Length]; bool compatible = true;
                    for (int i = 0; i < a.Length; i++)
                    {
                        RuneValue x = a[i], y = b[(i + offset) % b.Length];
                        if (!KnotSolver.Equal(x, y)) { compatible = false; break; }
                        candidate[i] = x.IsNot && !y.IsNot ? y : x;
                    }
                    if (compatible) choices.Add(candidate);
                }
                merged[lane] = choices.Count == 1 ? choices[0] : a;
            }
            current.Lanes = merged; return current;
        }
    }
}
