using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace KavrosClueHelper
{
    internal static class AppBranding
    {
        internal const int FooterHeight = 28;
        private static readonly Assembly App = typeof(AppBranding).Assembly;

        internal static string DisplayVersion
        {
            get { return Shown(App.GetName().Version); }
        }

        // The assembly and the updater keep three parts (0.8.0, 0.8.1). A zero
        // patch is shown as major.minor (v0.8). Any other patch is shown in full
        // (v0.8.1) so a small update is visible.
        internal static string Shown(Version version)
        {
            if (version == null) return "";
            if (version.Build > 0) return "v" + version.Major + "." + version.Minor + "." + version.Build;
            return "v" + version.Major + "." + version.Minor;
        }

        internal static string Shown(string version)
        {
            if (string.IsNullOrEmpty(version)) return "";
            string text = version.Trim();
            if (text.StartsWith("v") || text.StartsWith("V")) text = text.Substring(1);
            string[] parts = text.Split('.');
            int major, minor, patch;
            if (parts.Length >= 2 && int.TryParse(parts[0], out major) && int.TryParse(parts[1], out minor))
            {
                if (parts.Length >= 3 && int.TryParse(parts[2], out patch) && patch > 0)
                    return "v" + major + "." + minor + "." + patch;
                return "v" + major + "." + minor;
            }
            return "v" + text;
        }

        private static Stream Resource(string name)
        {
            Stream stream = App.GetManifestResourceStream("Kavros.Branding." + name);
            if (stream == null) throw new InvalidDataException("Missing application artwork: " + name);
            return stream;
        }

        internal static Icon LoadScroll()
        {
            using (Stream stream = Resource("clue-scroll.ico"))
            using (var icon = new Icon(stream, 32, 32))
                return (Icon)icon.Clone();
        }

        internal static void ArrangeSettings(Form form)
        {
            var crest = new PictureBox {
                Name = "KavrosCrest", AccessibleName = "Kavros", TabStop = false,
                Bounds = new Rectangle(16, 9, 65, 65), SizeMode = PictureBoxSizeMode.Zoom
            };
            Bitmap crestBitmap;
            using (Stream stream = Resource("kavros-crest.png"))
            using (Image image = Image.FromStream(stream))
                crestBitmap = new Bitmap(image);
            crest.Image = crestBitmap;
            crest.Disposed += delegate { crestBitmap.Dispose(); };

            var footer = new Label {
                Name = "BuildCredit", Text = "Made by Zenity  ·  " + DisplayVersion,
                Dock = DockStyle.Bottom, Height = FooterHeight, TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = KavrosTheme.TextFaint, BackColor = KavrosTheme.Bg
            };

            // The original controls keep their coordinates and stacking order.
            // On a short screen only the settings body scrolls.
            Size originalSize = form.ClientSize;
            var body = new MainSurface {
                Name = "SettingsBody", Dock = DockStyle.Fill, TabStop = false,
                AutoScroll = true, AutoScrollMinSize = new Size(0, originalSize.Height)
            };
            Control[] controls = form.Controls.Cast<Control>().ToArray();
            form.SuspendLayout();
            body.SuspendLayout();
            try
            {
                body.Controls.AddRange(controls);
                body.Controls.Add(crest);
                form.Controls.Add(body);
                form.Controls.Add(footer);
                int frameHeight = form.Height - originalSize.Height;
                int available = Screen.FromControl(form).WorkingArea.Height - frameHeight;
                form.ClientSize = new Size(originalSize.Width, Math.Min(originalSize.Height + FooterHeight, Math.Max(1, available)));
            }
            finally
            {
                body.ResumeLayout(true);
                form.ResumeLayout(true);
            }
        }
    }
}
