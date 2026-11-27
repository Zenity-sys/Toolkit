using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KavrosClueHelper
{
    // Presentation only: colour tokens, fonts and small owner-drawn helpers for
    // the Kavros navy/gold look. Nothing in here reads the game or changes what
    // a control does; it only changes how it is painted.
    internal enum ButtonTone { Secondary, Primary, Ghost }
    internal enum PillTone { Ok, Warn, Bad, Example }

    internal static partial class KavrosTheme
    {
        // Colour tokens (match the UI spec / styles.css).
        public static readonly Color Bg = Color.FromArgb(10, 18, 25);          // #0a1219
        public static readonly Color BgDeep = Color.FromArgb(7, 13, 19);       // #070d13
        public static readonly Color BgRaise = Color.FromArgb(15, 26, 35);     // #0f1a23
        public static readonly Color Panel = Color.FromArgb(19, 33, 44);       // #13212c
        public static readonly Color Hover = Color.FromArgb(24, 42, 55);       // #182a37
        public static readonly Color Input = Color.FromArgb(12, 22, 30);       // #0c161e
        public static readonly Color Line = Color.FromArgb(42, 70, 96);        // #2a4660
        public static readonly Color LineSoft = Color.FromArgb(42, 59, 72);    // rgba(160,196,218,.16) on panel
        public static readonly Color Text = Color.FromArgb(227, 238, 244);     // #e3eef4
        public static readonly Color TextSoft = Color.FromArgb(163, 184, 198); // #a3b8c6
        public static readonly Color TextFaint = Color.FromArgb(143, 167, 182);// #8fa7b6
        public static readonly Color Gold = Color.FromArgb(232, 195, 106);     // #e8c36a
        public static readonly Color GoldEdge = Color.FromArgb(201, 166, 78);  // #c9a64e
        public static readonly Color GoldInk = Color.FromArgb(26, 20, 8);      // #1a1408
        public static readonly Color Ice = Color.FromArgb(140, 207, 238);      // #8ccfee
        public static readonly Color Ok = Color.FromArgb(135, 220, 181);       // #87dcb5
        public static readonly Color Warn = Color.FromArgb(236, 212, 154);     // #ecd49a
        public static readonly Color Danger = Color.FromArgb(242, 180, 180);   // #f2b4b4
        public static readonly Color Button = Color.FromArgb(26, 42, 56);      // #1a2a38
        public static readonly Color ButtonLine = Color.FromArgb(61, 90, 114); // #3d5a72
        public static readonly Color TitleBar = Color.FromArgb(13, 22, 32);    // #0d1620
        public static readonly Color Frame = Color.FromArgb(36, 56, 74);       // #24384a

        // The player turns this on from Settings. Off leaves the preview sharp.
        public static bool BlurPreview;

        private const string DisplayResource = "CinzelKavros-Regular.ttf";
        private static FontFamily display;
        private static bool fontsLoaded;
        private static readonly List<IntPtr> gdiFonts = new List<IntPtr>();
        private static readonly Dictionary<string, Font> cache = new Dictionary<string, Font>();

        [DllImport("gdi32.dll")]
        private static extern IntPtr AddFontMemResourceEx(IntPtr data, uint length, IntPtr reserved, ref uint fonts);
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        // Cinzel (SIL OFL 1.1) is embedded as a static 700-weight instance.
        // GDI (TextRenderer) needs AddFontMemResourceEx as well as the GDI+
        // collection, otherwise labels silently fall back to another font.
        private static readonly System.Collections.Generic.List<PrivateFontCollection> fontCollections = new System.Collections.Generic.List<PrivateFontCollection>();
        private static FontFamily uiFamily, semiboldFamily;
        private static FontFamily LoadFont(string resource)
        {
            try
            {
                using (Stream stream = typeof(KavrosTheme).Assembly.GetManifestResourceStream("Kavros.Branding." + resource))
                {
                    if (stream == null) return null;
                    byte[] bytes = new byte[stream.Length];
                    int read = 0; while (read < bytes.Length) { int n = stream.Read(bytes, read, bytes.Length - read); if (n <= 0) break; read += n; }
                    IntPtr memory = Marshal.AllocCoTaskMem(bytes.Length);
                    Marshal.Copy(bytes, 0, memory, bytes.Length);
                    PrivateFontCollection collection = new PrivateFontCollection();
                    collection.AddMemoryFont(memory, bytes.Length); fontCollections.Add(collection);
                    uint count = 0;
                    try { IntPtr handle = AddFontMemResourceEx(memory, (uint)bytes.Length, IntPtr.Zero, ref count); if (handle != IntPtr.Zero) gdiFonts.Add(handle); }
                    catch (DllNotFoundException) { }
                    catch (EntryPointNotFoundException) { }
                    // GDI+ owns the font only while this process-lifetime memory exists.
                    return collection.Families.Length > 0 ? collection.Families[0] : null;
                }
            }
            catch (Exception) { return null; }
        }

        private static void EnsureFonts()
        {
            if (fontsLoaded) return;
            fontsLoaded = true;
            display = LoadFont(DisplayResource);
            uiFamily = LoadFont("OutfitKavros-Regular.ttf");
            semiboldFamily = LoadFont("OutfitKavros-Semibold.ttf");
        }

        private static FontFamily DisplayFamily { get { EnsureFonts(); return display; } }

        static Font Make(FontFamily family, float size, FontStyle style, GraphicsUnit unit, bool display)
        {
            if (family != null)
            {
                Font embedded = TryFont(delegate { return new Font(family, size, style, unit); });
                if (embedded != null) return embedded;
            }
            string[] names = display ? new[] { "Georgia", "DejaVu Serif", "Liberation Serif" } : new[] { "Segoe UI", "DejaVu Sans", "Liberation Sans" };
            foreach (string name in names)
            {
                string chosen = name;
                Font match = TryFont(delegate { return new Font(chosen, size, style, unit); });
                if (match != null) return match;
            }
            return new Font(display ? FontFamily.GenericSerif : FontFamily.GenericSansSerif, size, style, unit);
        }
        static Font TryFont(Func<Font> create)
        {
            try
            {
                Font font = create();
                float height = font.Height;
                if (height > 0) return font;
            }
            catch (Exception) { }
            return null;
        }
        public static Font Display(float size)
        {
            string key = "d" + size;
            Font font;
            if (cache.TryGetValue(key, out font)) return font;
            font = Make(DisplayFamily, size, FontStyle.Regular, GraphicsUnit.Point, true);
            cache[key] = font;
            return font;
        }

        // Pixel-sized variant for drawings that are laid out in pixels.
        public static Font DisplayPixels(float pixels)
        {
            string key = "p" + pixels;
            Font font;
            if (cache.TryGetValue(key, out font)) return font;
            font = Make(DisplayFamily, pixels, FontStyle.Regular, GraphicsUnit.Pixel, true);
            cache[key] = font;
            return font;
        }

        public static Font Ui(float size, bool semibold)
        {
            string key = (semibold ? "s" : "u") + size;
            Font font;
            if (cache.TryGetValue(key, out font)) return font;
            EnsureFonts();
            FontFamily family = semibold ? semiboldFamily : uiFamily;
            font = Make(family, size, FontStyle.Regular, GraphicsUnit.Point, false);
            cache[key] = font;
            return font;
        }
        public static Font System(float size, FontStyle style)
        {
            return Make(null, size, style, GraphicsUnit.Point, false);
        }

        // Dark native caption. Windows 11 also takes the navy caption colour and
        // a frame colour; older builds just use the dark-mode caption. The real
        // title bar is kept so snapping, DPI and accessibility stay native.
        public static void ApplyWindowChrome(IntPtr handle)
        {
            try
            {
                int on = 1;
                if (DwmSetWindowAttribute(handle, 20, ref on, 4) != 0) DwmSetWindowAttribute(handle, 19, ref on, 4);
                int caption = ColorRef(TitleBar), border = ColorRef(Frame), text = ColorRef(Text);
                DwmSetWindowAttribute(handle, 35, ref caption, 4);
                DwmSetWindowAttribute(handle, 34, ref border, 4);
                DwmSetWindowAttribute(handle, 36, ref text, 4);
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }
        private static int ColorRef(Color c) { return c.R | (c.G << 8) | (c.B << 16); }

        public static GraphicsPath Rounded(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = Math.Max(0.1F, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRounded(Graphics g, Rectangle r, float radius, Color fill, Color border)
        {
            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Rounded(new RectangleF(r.X + 0.5F, r.Y + 0.5F, r.Width - 1, r.Height - 1), radius))
            {
                if (fill.A > 0) using (Brush b = new SolidBrush(fill)) g.FillPath(b, path);
                if (border.A > 0) using (Pen p = new Pen(border)) g.DrawPath(p, path);
            }
            g.SmoothingMode = old;
        }

        public static Color Blend(Color top, Color under, double alpha)
        {
            return Color.FromArgb((int)(top.R * alpha + under.R * (1 - alpha)), (int)(top.G * alpha + under.G * (1 - alpha)), (int)(top.B * alpha + under.B * (1 - alpha)));
        }

        public static Color PillColor(PillTone tone)
        {
            return tone == PillTone.Ok ? Ok : tone == PillTone.Warn ? Warn : tone == PillTone.Bad ? Danger : Gold;
        }

        public static int MeasurePill(Graphics g, string text)
        {
            Size size = TextRenderer.MeasureText(g, text.ToUpperInvariant(), Ui(7.5F, true), new Size(1000, 40), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            return size.Width + 30;
        }

        // Rounded status pill with a dot, as in the mockups. Returns its width.
        public static int DrawPill(Graphics g, int x, int centreY, string text, PillTone tone, Color surface)
        {
            Color ink = PillColor(tone);
            bool dot = tone != PillTone.Example;
            int width = MeasurePill(g, text) - (dot ? 0 : 10), height = 20;
            Rectangle r = new Rectangle(x, centreY - height / 2, width, height);
            Color fill = Blend(ink, surface, tone == PillTone.Example ? 0.08 : tone == PillTone.Bad ? 0.10 : 0.14);
            Color edge = Blend(ink, surface, tone == PillTone.Example ? 0.35 : 0.45);
            FillRounded(g, r, 10, fill, edge);
            int textLeft = r.Left + 9;
            if (dot)
            {
                SmoothingMode old = g.SmoothingMode; g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Brush b = new SolidBrush(ink)) g.FillEllipse(b, r.Left + 9, centreY - 3, 6, 6);
                g.SmoothingMode = old;
                textLeft = r.Left + 20;
            }
            TextRenderer.DrawText(g, text.ToUpperInvariant(), Ui(7.5F, true), new Rectangle(textLeft, r.Top, r.Right - textLeft - 6, r.Height), ink, fill, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            return width;
        }

        // Gold card heading such as CAPTURE or SOLUTION.
        public static void DrawCardLabel(Graphics g, string text, int x, int y, Color surface)
        {
            DrawTracked(g, text, Display(7.5F), x, y, Gold, surface, 1.8F, 0);
        }

        // TextRenderer has no letter spacing, so small capitals are drawn one
        // glyph at a time with extra tracking, like the mockup's headings.
        public static int DrawTracked(Graphics g, string text, Font font, int x, int y, Color colour, Color back, float tracking, int maxRight)
        {
            const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
            int space = Math.Max(2, (int)Math.Round(font.GetHeight(g) / 4));
            float left = x;
            for (int i = 0; i < text.Length; i++)
            {
                string glyph = text[i].ToString();
                int width = text[i] == ' ' ? space : TextRenderer.MeasureText(g, glyph, font, new Size(500, 100), flags).Width;
                if (maxRight > 0 && left + width > maxRight)
                {
                    TextRenderer.DrawText(g, "\u2026", font, new Rectangle((int)left, y, Math.Max(1, maxRight - (int)left), font.Height + 4), colour, back, flags);
                    return maxRight;
                }
                if (text[i] != ' ') TextRenderer.DrawText(g, glyph, font, new Rectangle((int)left, y, width + 4, font.Height + 4), colour, back, flags);
                left += width + tracking;
            }
            return (int)left;
        }

        public static void DrawButton(Graphics g, Rectangle client, string text, Font font, ButtonTone tone, bool enabled, bool hover, bool pressed, Color surface)
        {
            using (Brush s = new SolidBrush(surface)) g.FillRectangle(s, client);
            Color fill, edge, ink;
            if (tone == ButtonTone.Primary) { fill = Gold; edge = GoldEdge; ink = GoldInk; if (hover) fill = Color.FromArgb(240, 207, 128); if (pressed) fill = Color.FromArgb(213, 178, 107); }
            else if (tone == ButtonTone.Ghost) { fill = surface; edge = ButtonLine; ink = TextSoft; if (hover) fill = Hover; if (pressed) fill = BgDeep; }
            else { fill = Button; edge = ButtonLine; ink = Text; if (hover) fill = Color.FromArgb(34, 54, 71); if (pressed) fill = Color.FromArgb(20, 34, 46); }
            if (!enabled) { fill = Blend(fill, surface, 0.45); edge = Blend(edge, surface, 0.6); ink = Blend(TextSoft, surface, 0.6); }
            FillRounded(g, new Rectangle(client.X, client.Y, client.Width, client.Height), 3, fill, edge);
            Font face = tone == ButtonTone.Primary ? Ui(font.SizeInPoints, true) : font;
            TextRenderer.DrawText(g, text, face, client, ink, fill, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }

        public static Color SurfaceOf(Control control)
        {
            for (Control c = control.Parent; c != null; c = c.Parent)
            {
                CardSurface card = c as CardSurface;
                if (card != null) return card.SurfaceAt(control);
                if (c.BackColor.A == 255) return c.BackColor;
            }
            return Bg;
        }
    }

    // A surface that paints rounded cards behind its child controls.
    internal interface CardSurface { Color SurfaceAt(Control child); }

    // Flat checkbox with a gold tick. Behaviour is the stock CheckBox.
    public class KavrosCheckBox : CheckBox
    {
        private bool hover;
        public KavrosCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            ForeColor = KavrosTheme.Text;
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color surface = BackColor;
            using (Brush s = new SolidBrush(surface)) g.FillRectangle(s, ClientRectangle);
            int box = 15, top = (Height - box) / 2;
            Rectangle r = new Rectangle(1, top, box, box);
            Color fill = Checked ? KavrosTheme.Gold : KavrosTheme.Input;
            Color edge = Checked ? KavrosTheme.GoldEdge : hover ? KavrosTheme.Gold : KavrosTheme.ButtonLine;
            if (!Enabled) { fill = KavrosTheme.Blend(fill, surface, 0.4); edge = KavrosTheme.Blend(edge, surface, 0.5); }
            KavrosTheme.FillRounded(g, r, 2.5F, fill, edge);
            if (Checked)
            {
                SmoothingMode old = g.SmoothingMode; g.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen tick = new Pen(KavrosTheme.GoldInk, 2F) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                    g.DrawLines(tick, new[] { new PointF(r.Left + 3.6F, r.Top + 7.8F), new PointF(r.Left + 6.4F, r.Top + 10.6F), new PointF(r.Left + 11.6F, r.Top + 4.6F) });
                g.SmoothingMode = old;
            }
            Color ink = Enabled ? ForeColor : KavrosTheme.Blend(KavrosTheme.TextSoft, surface, 0.55);
            Rectangle text = new Rectangle(r.Right + 8, 0, Math.Max(1, Width - r.Right - 8), Height);
            TextRenderer.DrawText(g, Text, Font, text, ink, surface, (UseMnemonic ? (ShowKeyboardCues ? TextFormatFlags.Default : TextFormatFlags.HidePrefix) : TextFormatFlags.NoPrefix) | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, new Rectangle(text.Left - 2, 2, Math.Min(text.Width, TextRenderer.MeasureText(Text, Font).Width + 2), Height - 4));
        }
    }

    // Thin gold progress bar. The stock bar ignores colours under visual styles.
    public class KavrosProgressBar : ProgressBar
    {
        private readonly Timer animation = new Timer { Interval = 30 };
        private int phase = 380;
        public Color Track = Color.FromArgb(28, 44, 56);
        public KavrosProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            animation.Tick += delegate { phase = (phase + 6) % 1000; Invalidate(); };
        }
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            animation.Enabled = Visible && Style == ProgressBarStyle.Marquee;
        }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (Brush t = new SolidBrush(Track)) g.FillRectangle(t, ClientRectangle);
            int w = Width;
            using (Brush b = new SolidBrush(KavrosTheme.Gold))
            {
                if (Style == ProgressBarStyle.Marquee)
                {
                    int segment = Math.Max(24, w * 38 / 100);
                    int x = (int)((phase / 1000.0) * (w + segment)) - segment;
                    g.FillRectangle(b, Math.Max(0, x), 0, Math.Min(w, x + segment) - Math.Max(0, x), Height);
                }
                else if (Maximum > Minimum)
                    g.FillRectangle(b, 0, 0, (int)((long)w * (Value - Minimum) / (Maximum - Minimum)), Height);
            }
        }
        protected override void Dispose(bool disposing) { if (disposing) animation.Dispose(); base.Dispose(disposing); }
    }

    // Label whose whole surface is painted by a renderer. Text, ForeColor and
    // every other Label property still work; only the drawing is replaced.
    public class PaintedLabel : Label
    {
        public Action<Graphics, PaintedLabel> Renderer;
        public PaintedLabel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnForeColorChanged(EventArgs e) { base.OnForeColorChanged(e); Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Renderer == null) { base.OnPaint(e); return; }
            Renderer(e.Graphics, this);
        }
    }

    internal static class StatusText
    {
        // Connection line: "Game found: <client>" (green), launcher or searching
        // (amber), or nothing found yet (red). The first part becomes a pill.
        public static void DrawConnection(Graphics g, PaintedLabel label)
        {
            Color surface = KavrosTheme.SurfaceOf(label);
            using (Brush s = new SolidBrush(surface)) g.FillRectangle(s, label.ClientRectangle);
            string text = label.Text ?? "";
            string pill, rest; PillTone tone;
            if (text.StartsWith("Game found: ", StringComparison.Ordinal)) { pill = "Game found"; rest = text.Substring(12); tone = PillTone.Ok; }
            else if (text.IndexOf("launcher found", StringComparison.OrdinalIgnoreCase) >= 0) { pill = "Launcher"; rest = text; tone = PillTone.Warn; }
            else if (text.StartsWith("Looking for Kavros -", StringComparison.Ordinal)) { pill = "Not found"; rest = text; tone = PillTone.Bad; }
            else { pill = "Searching"; rest = text; tone = PillTone.Warn; }
            int cy = label.Height / 2;
            int w = KavrosTheme.DrawPill(g, 0, cy, pill, tone, surface);
            TextRenderer.DrawText(g, rest, KavrosTheme.Ui(8.5F, false), new Rectangle(w + 9, 0, Math.Max(1, label.Width - w - 9), label.Height), KavrosTheme.TextSoft, surface, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        private static readonly string[] WaitingStarts = { "Waiting", "Rechecking", "Keep the puzzle", "Reading", "Looking", "Searching" };

        // The detection line becomes a status strip: amber accent while
        // waiting or not recognised, green once something was read.
        public static void DrawDetection(Graphics g, PaintedLabel label)
        {
            Color page = KavrosTheme.SurfaceOf(label);
            using (Brush s = new SolidBrush(page)) g.FillRectangle(s, label.ClientRectangle);
            string text = label.Text ?? "";
            var pills = new List<KeyValuePair<string, PillTone>>();
            string rest = "";
            bool found;
            if (text == "Game captured / puzzle not recognised")
            { pills.Add(new KeyValuePair<string, PillTone>("Game captured", PillTone.Ok)); pills.Add(new KeyValuePair<string, PillTone>("Puzzle not recognised", PillTone.Warn)); found = false; }
            else if (WaitingStarts.Any(w => text.StartsWith(w, StringComparison.OrdinalIgnoreCase)))
            { pills.Add(new KeyValuePair<string, PillTone>(text.StartsWith("Reading", StringComparison.OrdinalIgnoreCase) ? "Reading" : text.StartsWith("Waiting", StringComparison.OrdinalIgnoreCase) ? "Waiting" : "Searching", PillTone.Warn)); rest = text; found = false; }
            else
            { pills.Add(new KeyValuePair<string, PillTone>("Found", PillTone.Ok)); rest = text; found = true; }
            Color accent = found ? KavrosTheme.Ok : KavrosTheme.Gold;
            Color fill = KavrosTheme.Blend(accent, KavrosTheme.Bg, 0.09);
            Rectangle strip = new Rectangle(0, 0, label.Width, label.Height);
            KavrosTheme.FillRounded(g, strip, 4, fill, KavrosTheme.Blend(accent, KavrosTheme.Bg, 0.40));
            using (Brush a = new SolidBrush(accent)) g.FillRectangle(a, 1, 2, 3, label.Height - 4);
            int x = 12, cy = label.Height / 2;
            foreach (var p in pills) x += KavrosTheme.DrawPill(g, x, cy, p.Key, p.Value, fill) + 6;
            if (rest.Length > 0)
                TextRenderer.DrawText(g, rest, KavrosTheme.Ui(9.5F, true), new Rectangle(x + 4, 0, Math.Max(1, label.Width - x - 10), label.Height), found ? KavrosTheme.Ok : KavrosTheme.Warn, fill, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        // "Automatic detection" legend with colour swatches. Same text as before.
        public static void DrawLegend(Graphics g, PaintedLabel label)
        {
            Color surface = KavrosTheme.SurfaceOf(label);
            using (Brush s = new SolidBrush(surface)) g.FillRectangle(s, label.ClientRectangle);
            string[] lines = (label.Text ?? "").Split('\n').Where(l => l.Trim().Length > 0).ToArray();
            int y = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (i == 0) { TextRenderer.DrawText(g, line, KavrosTheme.Ui(9F, true), new Rectangle(0, y, label.Width, 18), KavrosTheme.Text, surface, TextFormatFlags.NoPadding); y += 18; continue; }
                if (i == 1) { TextRenderer.DrawText(g, line, KavrosTheme.Ui(8.5F, false), new Rectangle(0, y, label.Width, 18), KavrosTheme.TextSoft, surface, TextFormatFlags.NoPadding); y += 24; continue; }
                Color swatch = line.StartsWith("Green 0") ? Color.FromArgb(46, 125, 90) : line.StartsWith("Green") ? Color.FromArgb(62, 207, 142) : line.StartsWith("Gold") ? KavrosTheme.Gold : Color.FromArgb(200, 224, 236);
                using (Brush b = new SolidBrush(swatch)) g.FillRectangle(b, 0, y + 4, 9, 9);
                TextRenderer.DrawText(g, line, KavrosTheme.Ui(8.5F, false), new Rectangle(16, y, label.Width - 16, 18), KavrosTheme.TextSoft, surface, TextFormatFlags.NoPadding);
                y += 19;
            }
        }
    }

    // The settings body: paints the header text, the gold rule and the
    // rounded cards behind the existing controls. Controls keep their parents'
    // stacking order; this panel only owns the background pixels.
    internal sealed class MainSurface : Panel, CardSurface
    {
        public sealed class Card { public Rectangle Bounds; public string Label; }
        public readonly List<Card> Cards = new List<Card>();
        public MainSurface()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = KavrosTheme.Bg;
        }
        public Color SurfaceAt(Control child)
        {
            Rectangle b = child.Bounds; b.Offset(-AutoScrollPosition.X, -AutoScrollPosition.Y);
            return Cards.Any(c => c.Bounds.Contains(b)) ? KavrosTheme.Panel : KavrosTheme.Bg;
        }
        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            base.ScaleControl(factor, specified);
            for (int i = 0; i < Cards.Count; i++)
            {
                Rectangle b = Cards[i].Bounds;
                Cards[i].Bounds = new Rectangle(
                    (int)Math.Round(b.X * factor.Width),
                    (int)Math.Round(b.Y * factor.Height),
                    (int)Math.Round(b.Width * factor.Width),
                    (int)Math.Round(b.Height * factor.Height));
            }
            int lowest = 0;
            foreach (Control child in Controls) if (child.Bottom > lowest) lowest = child.Bottom;
            int needed = lowest + 16;
            if (needed > AutoScrollMinSize.Height) AutoScrollMinSize = new Size(AutoScrollMinSize.Width, needed);
        }
        protected override void OnScroll(ScrollEventArgs se) { base.OnScroll(se); Invalidate(); }
        protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); Invalidate(); }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(KavrosTheme.Bg);
            g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);
            using (Brush gold = new SolidBrush(KavrosTheme.Gold)) g.FillRectangle(gold, 0, 0, Math.Max(Width, DisplayRectangle.Width), 2);

            foreach (Card card in Cards)
            {
                KavrosTheme.FillRounded(g, card.Bounds, 5, KavrosTheme.Panel, KavrosTheme.LineSoft);
                if (!String.IsNullOrEmpty(card.Label))
                {
                    g.ResetTransform();
                    KavrosTheme.DrawCardLabel(g, card.Label, card.Bounds.Left + 12 + AutoScrollPosition.X, card.Bounds.Top + 9 + AutoScrollPosition.Y, KavrosTheme.Panel);
                    g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);
                }
            }
            g.ResetTransform();
            KavrosTheme.DrawTracked(g, "KAVROS", KavrosTheme.Display(8.5F), 92 + AutoScrollPosition.X, 13 + AutoScrollPosition.Y, KavrosTheme.Gold, KavrosTheme.Bg, 2.6F, 0);
        }
    }

    internal static class PreviewStyle
    {
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<PictureBox, Holder> blurred = new System.Runtime.CompilerServices.ConditionalWeakTable<PictureBox, Holder>();
        private sealed class Holder { public Image Source; public Bitmap Blur; public Size Shown; }
        public static void Release(PictureBox box)
        {
            Holder h;
            if (blurred.TryGetValue(box, out h) && h.Blur != null) { h.Blur.Dispose(); h.Blur = null; }
            blurred.Remove(box);
        }

        // Repaints the capture preview dimmed and softened with a LIVE CAPTURE
        // tag. The captured bitmap itself is untouched (Save image is sharp).
        public static void Paint(object sender, PaintEventArgs e)
        {
            PictureBox box = (PictureBox)sender;
            Graphics g = e.Graphics;
            Rectangle client = box.ClientRectangle;
            using (Brush deep = new SolidBrush(KavrosTheme.BgDeep)) g.FillRectangle(deep, client);
            Image image = box.Image;
            if (image != null)
            {
                Rectangle fit = Fit(image.Size, client);
                if (KavrosTheme.BlurPreview)
                {
                    Holder h = blurred.GetOrCreateValue(box);
                    if (!ReferenceEquals(h.Source, image) || h.Blur == null || h.Shown != fit.Size)
                    {
                        if (h.Blur != null) h.Blur.Dispose();
                        h.Blur = null; h.Source = image; h.Shown = fit.Size;
                        try { h.Blur = Soften(image, fit.Size); } catch (Exception) { h.Blur = null; }
                    }
                    if (h.Blur != null)
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                        g.PixelOffsetMode = PixelOffsetMode.Half;
                        g.DrawImage(h.Blur, fit);
                    }
                    else DrawSharp(g, image, fit);
                    using (Brush dim = new SolidBrush(Color.FromArgb(110, KavrosTheme.BgDeep))) g.FillRectangle(dim, fit);
                }
                else DrawSharp(g, image, fit);
            }
            else
                TextRenderer.DrawText(g, "No capture yet", KavrosTheme.Ui(8.5F, false), client, KavrosTheme.TextFaint, KavrosTheme.BgDeep, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            PreviewBox preview = box as PreviewBox;
            string tag = preview != null && preview.IsOffline != null && preview.IsOffline() ? "IMAGE PREVIEW" : "LIVE CAPTURE";
            Size size = TextRenderer.MeasureText(g, tag, KavrosTheme.Ui(7F, true), new Size(400, 40), TextFormatFlags.NoPadding);
            Rectangle chip = new Rectangle(8, client.Height - 8 - 18, size.Width + 14, 18);
            KavrosTheme.FillRounded(g, chip, 3, Color.FromArgb(7, 13, 19), KavrosTheme.LineSoft);
            TextRenderer.DrawText(g, tag, KavrosTheme.Ui(7F, true), chip, KavrosTheme.TextSoft, Color.FromArgb(7, 13, 19), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            using (Pen edge = new Pen(KavrosTheme.LineSoft)) g.DrawRectangle(edge, 0, 0, client.Width - 1, client.Height - 1);
        }

        internal static void DrawSharp(Graphics g, Image image, Rectangle fit)
        {
            bool pixels = image.Width > 0 && image.Height > 0 && Math.Min(fit.Width / (double)image.Width, fit.Height / (double)image.Height) >= 2;
            g.InterpolationMode = pixels ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = pixels ? PixelOffsetMode.Half : PixelOffsetMode.HighQuality;
            g.DrawImage(image, fit);
        }

        private static Rectangle Fit(Size image, Rectangle box)
        {
            if (image.Width <= 0 || image.Height <= 0) return box;
            double scale = Math.Min(box.Width / (double)image.Width, box.Height / (double)image.Height);
            int w = (int)(image.Width * scale), h = (int)(image.Height * scale);
            return new Rectangle(box.Left + (box.Width - w) / 2, box.Top + (box.Height - h) / 2, w, h);
        }

        // Cheap blur: scale down hard, then let bilinear scaling spread it.
        private static Bitmap Soften(Image image, Size shown)
        {
            int w = Math.Max(1, shown.Width / 12), h = Math.Max(1, shown.Height / 12);
            var small = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(small))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.DrawImage(image, new Rectangle(0, 0, w, h));
            }
            return small;
        }
    }
}

namespace KavrosClueHelper
{
    // PictureBox-derived so the original image ownership and drag/drop paths
    // remain intact. Only its surface is repainted; it never changes Image.
    internal sealed class PreviewBox : PictureBox
    {
        public Func<bool> IsOffline;
        public PreviewBox() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); }
        protected override void OnPaint(PaintEventArgs pe) { PreviewStyle.Paint(this, pe); }
        protected override void Dispose(bool disposing)
        {
            if (disposing) PreviewStyle.Release(this);
            base.Dispose(disposing);
        }
    }
}
