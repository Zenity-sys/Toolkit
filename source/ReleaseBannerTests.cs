using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace KavrosClueHelper
{
    public static class ReleaseBannerTests
    {
        public static int Run(string report)
        {
            int count = 0;
            try
            {
                Action<bool, string> check = delegate(bool ok, string msg) { count++; if (!ok) throw new Exception(msg); };
                string notes = ReleaseNotes.Text();
                check(notes.Contains("v0.9.13") && notes.Contains("v0.9.12") && notes.Contains("v0.9.11") && notes.Contains("v0.9.10") && notes.Contains("v0.9.9") && notes.Contains("v0.9.8"), "notes list the current version and the recent ones");
                check(notes.IndexOf("v0.9.13") < notes.IndexOf("v0.9.12") && notes.IndexOf("v0.9.12") < notes.IndexOf("v0.9.11") && notes.IndexOf("v0.9.11") < notes.IndexOf("v0.9.8"), "newer notes come first");
                check(notes.IndexOf("PR #", StringComparison.OrdinalIgnoreCase) < 0 && notes.IndexOf("RuneApps", StringComparison.OrdinalIgnoreCase) < 0 && notes.IndexOf("Jagex", StringComparison.OrdinalIgnoreCase) < 0 && notes.IndexOf("Alt1", StringComparison.OrdinalIgnoreCase) < 0, "notes stay in player-facing language");
                check(ReleaseNotes.IsNewer("", "v0.9.12") && ReleaseNotes.IsNewer(null, "v0.9.12") && ReleaseNotes.IsNewer("v0.9.11", "v0.9.12"), "a missing or older dismissal shows the notes");
                check(!ReleaseNotes.IsNewer("v0.9.12", "v0.9.12") && !ReleaseNotes.IsNewer("0.9.12", "v0.9.12") && !ReleaseNotes.IsNewer("v0.9.13", "v0.9.12"), "the current or a newer dismissal stays hidden");
                check(ReleaseNotes.IsNewer("not-a-version", "v0.9.12"), "an unreadable dismissal shows the notes again");
                string dir = Path.Combine(Path.GetTempPath(), "kavros-notes-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(dir);
                string fresh = Path.Combine(dir, "fresh.txt");
                string panelFile = Path.Combine(dir, "panel.txt");
                string seen = Path.Combine(dir, "seen.txt");
                File.WriteAllText(seen, "seen-notes\t" + AppBranding.DisplayVersion + "\r\n");
                using (PuzzleSuiteForm settings = new PuzzleSuiteForm(new OverlayPreferences(fresh), false))
                {
                    StopTimer(settings);
                    ShowOffscreen(settings);
                    check(settings.AutoScaleMode == AutoScaleMode.None, "the window is laid out at 96 DPI without auto scaling");
                    Panel hidden = (Panel)settings.Controls["WhatsNewBanner"];
                    check(hidden != null && !hidden.Visible, "settings does not open What's new on startup");
                    int closedHeight = settings.ClientSize.Height;
                    settings.ShowReleaseBanner();
                    Application.DoEvents();
                    Panel banner = (Panel)settings.Controls["WhatsNewBanner"];
                    TextBox body = (TextBox)banner.Controls["WhatsNewBody"];
                    Label heading = (Label)banner.Controls["WhatsNewHeading"];
                    Control dismiss = banner.Controls["WhatsNewDismiss"];
                    Rectangle card = ClientCard(settings);
                    Rectangle client = settings.ClientRectangle;
                    Rectangle text = new Rectangle(banner.Left + body.Left, banner.Top + body.Top, body.Width, body.Height);
                    int gap = banner.Top - card.Bottom;
                    check(banner.Visible && heading.Text == "What's new" && heading.ForeColor.ToArgb() == KavrosTheme.Gold.ToArgb(), "the settings banner has a gold What's new header");
                    check(dismiss.Text == "Got it" && dismiss.Right <= banner.Width && heading.Left < dismiss.Left, "Got it sits on the right of the settings header");
                    check(body.ScrollBars == ScrollBars.Vertical && body.Text.Contains("v0.9.13"), "the settings changelog scrolls");
                    check(!card.IsEmpty && gap >= 12 && gap <= 16, "the banner sits " + gap + " px below the CLIENT card");
                    check(!banner.Bounds.IntersectsWith(card), "the banner does not cover the CLIENT card");
                    MainSurface surface = settings.Controls.OfType<MainSurface>().Single();
                    foreach (Control child in surface.Controls)
                    {
                        if (!child.Visible || child.Width < 1 || child.Height < 1) continue;
                        Rectangle placed = new Rectangle(surface.Left + child.Left, surface.Top + child.Top + surface.AutoScrollPosition.Y, child.Width, child.Height);
                        check(!banner.Bounds.IntersectsWith(placed), (child.Name.Length == 0 ? child.Text : child.Name) + " stays clear of What's new");
                    }
                    check(client.Contains(banner.Bounds) && client.Contains(text), "the settings notes sit inside the client");
                    check(client.Height - text.Bottom >= WhatsNewLayout.Bottom, "the settings notes clear the bottom edge");
                    check(body.Bottom <= banner.Height - WhatsNewLayout.Pad && body.Right <= banner.Width - WhatsNewLayout.Pad, "the settings scrollbar stays inside the banner");
                    Label credit = (Label)settings.Controls.Find("BuildCredit", true)[0];
                    check(banner.Bottom <= credit.Top - WhatsNewLayout.Bottom && credit.Bottom <= client.Height, "the settings footer stays below the banner");
                    check(settings.ClientSize.Height > closedHeight, "the settings window grows to hold the banner");
                    Save(settings, "/opt/cursor/artifacts/settings-whats-new-fixed.png");
                    settings.DismissReleaseNotes();
                    check(!banner.Visible && settings.ClientSize.Height == closedHeight, "Got it on settings restores the window");
                }
                string assets = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
                using (FloatingCluePanel panel = new FloatingCluePanel(assets, new CompassSession(assets), new OverlayPreferences(panelFile)))
                using (FloatingCluePanel quiet = new FloatingCluePanel(assets, new CompassSession(assets), new OverlayPreferences(seen)))
                {
                    panel.SetWaiting("Open a clue or puzzle in Kavros.");
                    ShowAt(panel, new Point(40, 40));
                    ShowAt(quiet, new Point(1400, 40));
                    Panel banner = (Panel)panel.Controls["WhatsNewBanner"];
                    TextBox body = (TextBox)banner.Controls["WhatsNewBody"];
                    Label heading = (Label)banner.Controls["WhatsNewHeading"];
                    Control dismiss = banner.Controls["WhatsNewDismiss"];
                    Panel buttons = Footer(panel);
                    check(banner.Visible && heading.Text == "What's new" && heading.ForeColor.ToArgb() == KavrosTheme.Gold.ToArgb(), "the clue panel shows a gold What's new header");
                    check(dismiss.Text == "Got it" && heading.Left < dismiss.Left, "Got it sits on the right of the clue panel header");
                    check(body.ScrollBars == ScrollBars.Vertical && body.Text.Contains("v0.9.8"), "the clue panel changelog scrolls through v0.9.8");
                    CheckPanel(check, panel, banner, body, buttons, "idle");
                    int grown = panel.Height;
                    check(grown == 140 + WhatsNewLayout.PanelExtra, "the idle panel grows by the notes block");
                    check(!quiet.Controls["WhatsNewBanner"].Visible && quiet.Height == 140, "a panel that already saw this version stays at its normal height");
                    SaveScreen(panel, "/opt/cursor/artifacts/floating-whats-new-banner.png");
                    panel.SetPuzzle(new DetectedPuzzle { Kind = PuzzleKind.Towers }, "Corners show clicks left. Green 0 means complete.");
                    buttons = Footer(panel);
                    CheckPanel(check, panel, banner, body, buttons, "puzzle");
                    check(panel.Height == 140 + WhatsNewLayout.PanelExtra, "puzzle text keeps the same notes block");
                    panel.SetWaiting("Open a clue or puzzle in Kavros.");
                    panel.DismissReleaseNotes();
                    check(!banner.Visible && panel.Height == 140, "Got it restores the idle panel");
                    check(buttons.Bottom <= panel.ClientSize.Height && panel.ClientRectangle.Contains(buttons.Bounds), "the button row stays inside after dismiss");
                    SaveScreen(panel, "/opt/cursor/artifacts/floating-whats-new-dismissed.png");
                    OverlayPreferences stored = new OverlayPreferences(panelFile);
                    check(stored.SeenNotes == AppBranding.DisplayVersion, "Got it saves the current version");
                    using (FloatingCluePanel again = new FloatingCluePanel(assets, new CompassSession(assets), new OverlayPreferences(panelFile)))
                        check(!again.ReleaseNotesOpen && again.Height == 140, "the saved version keeps the clue panel notes hidden");
                }
                ScanRegion prif = new ScanVision(assets).Regions.Single(r => r.Id == 368);
                using (FloatingCluePanel scan = new FloatingCluePanel(assets, new CompassSession(assets), new OverlayPreferences(seen)))
                {
                    scan.SetPuzzle(new DetectedPuzzle { Kind = PuzzleKind.Scan, Scan = new ScanReading { Region = prif } }, "");
                    ShowAt(scan, new Point(40, 40));
                    Panel controls = ScanControls(scan);
                    Label note = (Label)controls.Controls["ScanTrackingNote"];
                    Panel buttons = Footer(scan);
                    Control map = ScanMapOf(scan);
                    Rectangle noteBounds = new Rectangle(controls.Left + note.Left, controls.Top + note.Top, note.Width, note.Height);
                    Size measured = TextRenderer.MeasureText(note.Text, note.Font, new Size(Math.Max(1, note.Width - 4), 0), TextFormatFlags.WordBreak);
                    check(note.Visible && note.ForeColor.ToArgb() == KavrosTheme.TextFaint.ToArgb(), "the scan note is dim");
                    check(note.Text.IndexOf("click spots on the map") >= 0 && prif.Route.StartsWith("Fastest: ;;prif"), "the scan note and the Prifddinas route are both shown");
                    check(scan.ClientRectangle.Contains(noteBounds), "the scan note sits inside the panel");
                    check(note.Height >= measured.Height && note.Width >= 360, "the scan note is not truncated (" + note.Height + " vs " + measured.Height + ")");
                    check(map != null && !noteBounds.IntersectsWith(map.Bounds) && !noteBounds.IntersectsWith(buttons.Bounds), "the scan note stays clear of the map and the buttons");
                    check(controls.Bottom <= buttons.Top && buttons.Bottom <= scan.ClientSize.Height, "scan controls and buttons stack inside the panel");
                    SaveScreen(scan, "/opt/cursor/artifacts/floating-scan-note.png");
                }
                using (FloatingCluePanel both = new FloatingCluePanel(assets, new CompassSession(assets), new OverlayPreferences(Path.Combine(dir, "scan-notes.txt"))))
                {
                    both.SetPuzzle(new DetectedPuzzle { Kind = PuzzleKind.Scan, Scan = new ScanReading { Region = prif } }, "");
                    ShowAt(both, new Point(480, 40));
                    Panel controls = ScanControls(both);
                    Label note = (Label)controls.Controls["ScanTrackingNote"];
                    Panel banner = (Panel)both.Controls["WhatsNewBanner"];
                    Panel buttons = Footer(both);
                    Rectangle noteBounds = new Rectangle(controls.Left + note.Left, controls.Top + note.Top, note.Width, note.Height);
                    check(both.ReleaseNotesOpen && banner.Visible && !noteBounds.IntersectsWith(banner.Bounds) && !noteBounds.IntersectsWith(buttons.Bounds), "the scan note stays clear of What's new");
                    check(both.ClientRectangle.Contains(noteBounds) && both.ClientRectangle.Contains(banner.Bounds), "the scan note and What's new both fit");
                }
                File.AppendAllText(report, "WHAT'S NEW PASS: " + count + " assertions.\r\n");
                return 0;
            }
            catch (Exception ex)
            {
                File.AppendAllText(report, "FAILED after " + count + " checks: " + ex + "\r\n");
                return 1;
            }
        }
        static void CheckPanel(Action<bool, string> check, FloatingCluePanel panel, Panel banner, TextBox body, Panel buttons, string mode)
        {
            Rectangle client = panel.ClientRectangle;
            Rectangle text = new Rectangle(banner.Left + body.Left, banner.Top + body.Top, body.Width, body.Height);
            int gap = banner.Top - buttons.Bottom;
            check(gap >= 10 && gap <= 12, mode + " notes sit " + gap + " px below the button row");
            check(!banner.Bounds.IntersectsWith(buttons.Bounds), mode + " notes do not cover the button row");
            check(client.Contains(banner.Bounds) && client.Contains(text), mode + " notes sit inside the clue panel");
            check(client.Height - text.Bottom >= WhatsNewLayout.PanelBottom, mode + " notes clear the bottom edge");
            check(body.Right <= banner.Width - WhatsNewLayout.Pad && body.Bottom <= banner.Height - WhatsNewLayout.Pad, mode + " scrollbar stays inside the banner");
            check(buttons.Bottom < banner.Top, mode + " button row stays above What's new");
        }
        static Rectangle ClientCard(Form form)
        {
            MainSurface surface = form.Controls.OfType<MainSurface>().Single();
            foreach (MainSurface.Card card in surface.Cards)
                if (card.Label == "CLIENT")
                    return new Rectangle(surface.Left + card.Bounds.X, surface.Top + card.Bounds.Y + surface.AutoScrollPosition.Y, card.Bounds.Width, card.Bounds.Height);
            return Rectangle.Empty;
        }
        static Panel ScanControls(Control panel)
        {
            foreach (Panel item in panel.Controls.OfType<Panel>())
                if (item.Controls["ScanTrackingNote"] != null) return item;
            return null;
        }
        static Control ScanMapOf(Control panel)
        {
            foreach (Control child in panel.Controls)
                if (child is ScanMap) return child;
            return null;
        }
        static Panel Footer(Control panel)
        {
            foreach (Panel item in panel.Controls.OfType<Panel>())
                foreach (Button button in item.Controls.OfType<Button>())
                    if (button.Text == "Exit") return item;
            return null;
        }
        static void ShowAt(Form form, Point location)
        {
            form.ShowInTaskbar = false;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = location;
            form.Show();
            form.BringToFront();
            Application.DoEvents();
        }
        static void SaveScreen(Form form, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            form.Refresh();
            Application.DoEvents();
            System.Threading.Thread.Sleep(250);
            Application.DoEvents();
            using (Bitmap bitmap = new Bitmap(form.Width, form.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                    g.CopyFromScreen(form.Location, Point.Empty, bitmap.Size);
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
        static void ShowOffscreen(Form form)
        {
            form.ShowInTaskbar = false;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-4000, -4000);
            form.Show();
            Application.DoEvents();
        }
        static void StopTimer(PuzzleSuiteForm form)
        {
            Timer timer = (Timer)typeof(PuzzleSuiteForm).GetField("timer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
            timer.Stop();
        }
        static void Save(Form form, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            form.Refresh();
            Application.DoEvents();
            Rectangle screen = Screen.PrimaryScreen.Bounds;
            using (Bitmap full = new Bitmap(Math.Max(1, form.Width), Math.Max(1, form.Height)))
            using (Graphics g = Graphics.FromImage(full))
            {
                g.Clear(form.BackColor);
                int covered = 0;
                while (covered < form.Height)
                {
                    form.Location = new Point(40, 40 - covered);
                    form.BringToFront();
                    form.Activate();
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(200);
                    Application.DoEvents();
                    Rectangle bounds = form.Bounds;
                    Rectangle visible = Rectangle.Intersect(bounds, screen);
                    if (visible.Width < 1 || visible.Height < 1) break;
                    using (Bitmap shot = new Bitmap(visible.Width, visible.Height))
                    {
                        using (Graphics cg = Graphics.FromImage(shot))
                            cg.CopyFromScreen(visible.Location, Point.Empty, visible.Size);
                        g.DrawImage(shot, visible.X - bounds.X, visible.Y - bounds.Y);
                    }
                    int next = visible.Bottom - bounds.Top;
                    if (next <= covered) break;
                    covered = next;
                }
                full.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }
}
