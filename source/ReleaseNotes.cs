using System;
using System.Drawing;

namespace KavrosClueHelper
{
    internal static class ReleaseNotes
    {
        internal static bool IsNewer(string seen, string current)
        {
            Version now, last;
            if (!TryParse(current, out now)) return false;
            if (!TryParse(seen, out last)) return true;
            return now > last;
        }
        static bool TryParse(string text, out Version version)
        {
            version = null;
            if (string.IsNullOrEmpty(text)) return false;
            string value = text.Trim();
            if (value.StartsWith("v") || value.StartsWith("V")) value = value.Substring(1);
            string[] parts = value.Split('.');
            if (parts.Length < 2) return false;
            int major, minor, patch = 0;
            if (!int.TryParse(parts[0], out major) || !int.TryParse(parts[1], out minor)) return false;
            if (parts.Length >= 3 && !int.TryParse(parts[2], out patch)) return false;
            version = new Version(major, minor, patch);
            return true;
        }
        internal static string Text()
        {
            return "v0.9.12\r\n"
                + "What's new stays at the bottom of this window until you press Got it. Lockboxes are found during live play on a 1440p client, and the numbers update as soon as you click.\r\n"
                + "\r\n"
                + "v0.9.11\r\n"
                + "DarkScape sliding puzzles are found while the side panel is open. A Celtic Knot whose top edge is the only full line of parchment is found on the first look. The clue panel can open at launch, and the helper can start without a tray icon.\r\n"
                + "\r\n"
                + "v0.9.10\r\n"
                + "Celtic Knot guidance appears on the first clean look, including when it asks you to turn one ring. The click counts update as soon as a ring settles.\r\n"
                + "\r\n"
                + "v0.9.9\r\n"
                + "Celtic Knot puzzles are found on the angled window, on Modern and DarkScape.\r\n"
                + "\r\n"
                + "v0.9.8\r\n"
                + "Towers puzzles are detected during live play. Each look checks the Towers board first, so a full-size client no longer skips it.\r\n";
        }
    }

    internal static class WhatsNewLayout
    {
        internal const int BannerHeight = 168;
        internal const int Side = 12;
        internal const int Bottom = 12;
        internal const int Pad = 8;
        internal const int Header = 32;
        internal static int Extra { get { return BannerHeight + Bottom; } }
        internal static Rectangle Host(Size client)
        {
            int y = client.Height - AppBranding.FooterHeight - Bottom - BannerHeight;
            int width = Math.Max(40, client.Width - Side * 2);
            return new Rectangle(Side, Math.Max(0, y), width, BannerHeight);
        }
        // Inside the banner, under the header, with padding on the sides and the bottom.
        internal static Rectangle Body(Rectangle host)
        {
            return new Rectangle(Pad, Header, Math.Max(20, host.Width - Pad * 2), Math.Max(20, host.Height - Header - Pad));
        }
    }
}
