using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace KavrosClueHelper
{
    public static class FocusSurfaceTests
    {
        public static int Run(string report)
        {
            int count = 0;
            try
            {
                Action<bool, string> check = delegate(bool ok, string msg) { count++; if (!ok) throw new Exception(msg); };
                check((Native.TopMostFlags & Native.SwpNoActivate) != 0, "every topmost change passes SWP_NOACTIVATE");
                check((Native.PlaceFlags & Native.SwpNoActivate) != 0, "moving a surface passes SWP_NOACTIVATE");
                string assets = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
                string prefs = Path.Combine(Path.GetTempPath(), "kavros-focus-" + Guid.NewGuid().ToString("N") + ".txt");
                using (GuidanceOverlay overlay = new GuidanceOverlay())
                using (FloatingCluePanel panel = new FloatingCluePanel(assets, new CompassSession(assets), new OverlayPreferences(prefs)))
                {
                    check(overlay.StaysInactive && panel.StaysInactive, "helper surfaces opt out of activation when shown");
                    int overlayStyle = ExtendedStyle(overlay);
                    int panelStyle = ExtendedStyle(panel);
                    check((overlayStyle & Native.ExNoActivate) != 0, "the puzzle overlay is WS_EX_NOACTIVATE");
                    check((panelStyle & Native.ExNoActivate) != 0 && (panelStyle & Native.ExToolWindow) != 0, "the clue panel is WS_EX_NOACTIVATE");
                    bool overlayNative = Native.ShowFormNoActivate(overlay);
                    panel.ShowWithoutFocus();
                    check(Native.LastShowCommand == Native.SwShowNoActivate, "showing a surface requests SW_SHOWNOACTIVATE");
                    if (overlayNative)
                    {
                        check(overlay.Activations == 0, "the native show does not activate the overlay");
                        check(panel.Activations == 0, "the native show does not activate the clue panel");
                    }
                    int shown = panel.Activations;
                    panel.SetPuzzle(new DetectedPuzzle { Kind = PuzzleKind.Lockbox, Board = new[] { 0 }, Bounds = new Rectangle(10, 10, 40, 40), Confidence = 0.95 }, "Click the marked tiles.");
                    overlay.UpdateGuidance(new Rectangle(80, 80, 120, 90), new List<GuidanceMark>());
                    var scans = new ScanVision(assets).Regions;
                    panel.SetPuzzle(new DetectedPuzzle { Kind = PuzzleKind.Scan, Scan = new ScanReading { Region = scans[0] } }, "");
                    check(panel.Activations == shown && overlay.Activations == (overlayNative ? 0 : overlay.Activations), "a lockbox or scan update does not request activation");
                    bool exited = false;
                    panel.ExitRequested += delegate { exited = true; };
                    Button exit = null;
                    foreach (Control child in panel.Controls)
                    {
                        Panel row = child as Panel;
                        if (row == null) continue;
                        foreach (Control item in row.Controls)
                        {
                            Button button = item as Button;
                            if (button != null && button.Text == "Exit") exit = button;
                        }
                    }
                    check(exit != null, "the Exit button is on the panel");
                    exit.PerformClick();
                    check(exited, "Exit still clicks when the panel does not take focus");
                }
                File.AppendAllText(report, "FOCUS PASS: " + count + " assertions.\r\n");
                return 0;
            }
            catch (Exception ex)
            {
                File.AppendAllText(report, "FAILED after " + count + " checks: " + ex + "\r\n");
                return 1;
            }
        }
        static int ExtendedStyle(Control control)
        {
            PropertyInfo property = typeof(Control).GetProperty("CreateParams", BindingFlags.Instance | BindingFlags.NonPublic);
            CreateParams parameters = (CreateParams)property.GetValue(control, null);
            return parameters.ExStyle;
        }
    }
}
