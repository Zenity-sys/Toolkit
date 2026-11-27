using System;
using System.Drawing;
using System.IO;
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
                check(notes.Contains("v0.9.12") && notes.Contains("v0.9.11") && notes.Contains("v0.9.10") && notes.Contains("v0.9.9") && notes.Contains("v0.9.8"), "notes list the current version and the recent ones");
                check(notes.IndexOf("v0.9.12") < notes.IndexOf("v0.9.11") && notes.IndexOf("v0.9.11") < notes.IndexOf("v0.9.8"), "newer notes come first");
                check(notes.IndexOf("PR #", StringComparison.OrdinalIgnoreCase) < 0 && notes.IndexOf("RuneApps", StringComparison.OrdinalIgnoreCase) < 0 && notes.IndexOf("Jagex", StringComparison.OrdinalIgnoreCase) < 0 && notes.IndexOf("Alt1", StringComparison.OrdinalIgnoreCase) < 0, "notes stay in player-facing language");
                check(ReleaseNotes.IsNewer("", "v0.9.12") && ReleaseNotes.IsNewer(null, "v0.9.12") && ReleaseNotes.IsNewer("v0.9.11", "v0.9.12"), "a missing or older dismissal shows the notes");
                check(!ReleaseNotes.IsNewer("v0.9.12", "v0.9.12") && !ReleaseNotes.IsNewer("0.9.12", "v0.9.12") && !ReleaseNotes.IsNewer("v0.9.13", "v0.9.12"), "the current or a newer dismissal stays hidden");
                check(ReleaseNotes.IsNewer("not-a-version", "v0.9.12"), "an unreadable dismissal shows the notes again");
                string dir = Path.Combine(Path.GetTempPath(), "kavros-notes-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(dir);
                string fresh = Path.Combine(dir, "fresh.txt");
                string seen = Path.Combine(dir, "seen.txt");
                File.WriteAllText(seen, "seen-notes\t" + AppBranding.DisplayVersion + "\r\n");
                using (PuzzleSuiteForm open = new PuzzleSuiteForm(new OverlayPreferences(fresh), false))
                using (PuzzleSuiteForm closed = new PuzzleSuiteForm(new OverlayPreferences(seen), false))
                {
                    StopTimer(open);
                    StopTimer(closed);
                    ShowOffscreen(open);
                    ShowOffscreen(closed);
                    check(open.AutoScaleMode == AutoScaleMode.None, "the window is laid out at 96 DPI without auto scaling");
                    Panel banner = (Panel)open.Controls["WhatsNewBanner"];
                    TextBox body = (TextBox)banner.Controls["WhatsNewBody"];
                    Label heading = (Label)banner.Controls["WhatsNewHeading"];
                    Control dismiss = banner.Controls["WhatsNewDismiss"];
                    check(banner.Visible && heading.Text == "What's new" && heading.ForeColor.ToArgb() == KavrosTheme.Gold.ToArgb(), "the gold What's new header is showing");
                    check(dismiss.Text == "Got it" && dismiss.Right <= banner.Width && heading.Left < dismiss.Left, "Got it sits on the right of the header");
                    check(body.ScrollBars == ScrollBars.Vertical && body.ReadOnly && body.Text.Contains("v0.9.12"), "the changelog scrolls and lists this version");
                    Rectangle client = open.ClientRectangle;
                    Rectangle text = new Rectangle(banner.Left + body.Left, banner.Top + body.Top, body.Width, body.Height);
                    int below = client.Height - text.Bottom;
                    check(client.Contains(banner.Bounds), "the banner sits inside the client (" + banner.Bounds + " / " + client + ")");
                    check(client.Contains(text), "the scrollable notes sit inside the client (" + text + " / " + client + ")");
                    check(below >= WhatsNewLayout.Bottom, "the notes have " + below + " px under them");
                    check(banner.Left >= WhatsNewLayout.Side && banner.Right <= client.Width - WhatsNewLayout.Side, "the banner keeps its side margins");
                    check(body.Left >= WhatsNewLayout.Pad && body.Right <= banner.Width - WhatsNewLayout.Pad && body.Bottom <= banner.Height - WhatsNewLayout.Pad, "the notes stay inside the banner with padding");
                    check(text.Right <= client.Width - WhatsNewLayout.Side && text.Bottom <= client.Height - WhatsNewLayout.Bottom, "nothing in the notes crosses the window edge");
                    Label footer = (Label)open.Controls.Find("BuildCredit", true)[0];
                    check(banner.Bottom <= footer.Top - WhatsNewLayout.Bottom && footer.Bottom <= client.Height, "the footer stays below the banner and inside the window");
                    int grown = open.ClientSize.Height;
                    check(grown == closed.ClientSize.Height + WhatsNewLayout.Extra, "the window grows by the banner and its bottom margin");
                    Save(open, "/opt/cursor/artifacts/whats-new-banner.png");
                    open.DismissReleaseNotes();
                    check(!banner.Visible && open.ClientSize.Height == grown - WhatsNewLayout.Extra, "Got it restores the window");
                    foreach (Control child in open.Controls)
                        if (child.Visible) check(child.Bottom <= open.ClientSize.Height && child.Right <= open.ClientSize.Width, child.Name + " stays inside after dismiss (" + child.Bounds + ")");
                    Save(open, "/opt/cursor/artifacts/whats-new-dismissed.png");
                    OverlayPreferences stored = new OverlayPreferences(fresh);
                    check(stored.SeenNotes == AppBranding.DisplayVersion, "Got it saves the current version");
                    using (PuzzleSuiteForm again = new PuzzleSuiteForm(new OverlayPreferences(fresh), false))
                    {
                        StopTimer(again);
                        ShowOffscreen(again);
                        check(!again.Controls["WhatsNewBanner"].Visible && again.ClientSize.Height == open.ClientSize.Height, "the saved version keeps the banner hidden");
                    }
                    check(!closed.Controls["WhatsNewBanner"].Visible, "a window that already saw this version stays closed");
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
