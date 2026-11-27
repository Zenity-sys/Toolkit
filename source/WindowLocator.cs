using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

namespace KavrosClueHelper
{
    internal static class CaptureLabels
    {
        public static string Auto(string client)
        {
            if (string.IsNullOrEmpty(client)) return "Auto - searching...";
            return "Auto - " + client;
        }
        public static string Found(string client)
        {
            return "Game found: " + (client ?? "");
        }
    }

    public static class WindowLocator
    {
        private static bool Contains(string text, string value) { return (text ?? "").IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0; }
        public static bool IsLauncher(GameWindow window)
        {
            bool gameTitle = Contains(window.Title, "nxt") || Contains(window.Title, "944") || Contains(window.ClassName, "jag") || Contains(window.ClassName, "glfw");
            return Contains(window.Title, "launcher") || Contains(window.ClassName, "launcher") || (Contains(window.ProcessName, "launcher") && !gameTitle);
        }
        public static int Score(GameWindow window)
        {
            if (window == null || window.Automatic || window.Handle == IntPtr.Zero) return -1000;
            // Companion, AFK Warden, the Slayer helper and this helper sit on a client.
            // Their windows are never the game, even when the title contains Kavros.
            if (IsHelperProcess(window.ProcessName) || Contains(window.Title, "Kavros Clue Helper")) return -1000;
            if (Contains(window.Title, "AFK Warden") || Contains(window.Title, "Slayer Helper")) return -1000;
            if (Contains(window.Title, "Companion") && (Contains(window.Title, "Kavros") || Contains(window.ProcessName, "Kavros"))) return -1000;
            if (Contains(window.ClassName, "CabinetWClass") || Contains(window.ClassName, "WinRarWindow") || Contains(window.ClassName, "Chrome_WidgetWin") || Contains(window.ClassName, "ApplicationFrameWindow")) return -1000;
            if (IsLauncher(window)) return -1000;
            // Title must contain Kavros. A rev number such as 944 or 951 is not required,
            // and another server's rs2client window is not a match.
            bool titleMatch = Contains(window.Title, "kavros");
            if (!titleMatch) return -1000;
            int score = 400;
            if (Contains(window.ProcessName, "kavros")) score += 80;
            if (Contains(window.ClassName, "jag") || Contains(window.ClassName, "glfw") || Contains(window.ClassName, "lwjgl")) score += 70;
            if (window.Client.Width >= 600 && window.Client.Height >= 350) score += 40;
            if (window.Client.Width < 250 || window.Client.Height < 150) score -= 250;
            return score;
        }
        // The same bar auto-selection uses. Helpers, launchers, browsers and other apps stay below it.
        public static bool IsClient(GameWindow window)
        {
            return Score(window) >= 100;
        }
        public static List<GameWindow> Clients(IEnumerable<GameWindow> windows)
        {
            List<GameWindow> clients = new List<GameWindow>();
            if (windows == null) return clients;
            foreach (GameWindow window in windows)
                if (IsClient(window)) clients.Add(window);
            return clients;
        }
        public static void NameClients(IList<GameWindow> clients)
        {
            if (clients == null) return;
            for (int i = 0; i < clients.Count; i++)
            {
                GameWindow window = clients[i];
                if (window == null) continue;
                window.Display = ClientIdentity.Label(window.ProcessId, i, window.Title);
            }
        }
        // The name shown for the client the helper is reading. A saved Companion
        // name wins. Otherwise it is Client N across the real clients only.
        public static string BoundName(GameWindow window, IList<GameWindow> clients)
        {
            if (window == null || window.Automatic) return "";
            if (!string.IsNullOrEmpty(window.Display)) return window.Display;
            int index = 0;
            if (clients != null)
            {
                for (int i = 0; i < clients.Count; i++)
                {
                    GameWindow candidate = clients[i];
                    if (candidate != null && candidate.Handle == window.Handle) { index = i; break; }
                }
            }
            return ClientIdentity.Label(window.ProcessId, index, window.Title);
        }
        public static GameWindow Choose(IEnumerable<GameWindow> windows, IntPtr current, IntPtr foreground)
        {
            return Choose(windows, current, 0, foreground);
        }
        public static GameWindow Choose(IEnumerable<GameWindow> windows, IntPtr current, uint processId, IntPtr foreground)
        {
            return Choose(windows, current, processId, foreground, null);
        }
        // A chosen client stays chosen. Focus and a second Kavros window never replace it.
        // A lost handle is reattached to the same process. Once that process is gone, the
        // foreground client is used. A client AFK Warden is sitting on is not taken.
        public static GameWindow Choose(IEnumerable<GameWindow> windows, IntPtr current, uint processId, IntPtr foreground, ICollection<IntPtr> claimed)
        {
            List<GameWindow> games = windows.Where(IsClient).OrderByDescending(Score).ThenBy(w => w.Title).ToList();
            GameWindow existing = games.Find(w => w.Handle == current);
            if (existing != null) return existing;
            if (processId != 0)
            {
                GameWindow sameProcess = games.Find(w => w.ProcessId == processId);
                if (sameProcess != null) return sameProcess;
                if (current != IntPtr.Zero) return ForegroundClient(games, foreground, claimed);
            }
            if (current != IntPtr.Zero)
            {
                if (games.Count == 1 && !IsClaimed(games[0].Handle, claimed)) return games[0];
                return null;
            }
            List<GameWindow> open = games;
            if (claimed != null && claimed.Count > 0) open = games.Where(w => !IsClaimed(w.Handle, claimed)).ToList();
            if (open.Count == 0) return null;
            GameWindow focused = open.Find(w => w.Handle == foreground);
            if (focused != null && Score(focused) >= Score(open[0]) - 50) return focused;
            return open.FirstOrDefault();
        }
        private static GameWindow ForegroundClient(List<GameWindow> games, IntPtr foreground, ICollection<IntPtr> claimed)
        {
            GameWindow focused = games.Find(w => w.Handle == foreground);
            if (focused == null || IsClaimed(focused.Handle, claimed)) return null;
            return focused;
        }
        private static bool IsClaimed(IntPtr handle, ICollection<IntPtr> claimed)
        {
            return claimed != null && claimed.Contains(handle);
        }
        // Small helper overlays that sit on the capture are masked. A window that
        // covers most of the client is left alone so a transparent shell is not wiped.
        public static List<Rectangle> CoverRects(IEnumerable<GameWindow> windows, Rectangle capture, int selfProcessId)
        {
            return CoverRects(windows, capture, selfProcessId, null);
        }
        // inFront is the same check the helper uses for its own panel: a window is
        // blanked only when it is the window actually drawn over the game.
        public static List<Rectangle> CoverRects(IEnumerable<GameWindow> windows, Rectangle capture, int selfProcessId, Func<GameWindow, Rectangle, bool> inFront)
        {
            List<Rectangle> covers = new List<Rectangle>();
            if (windows == null || capture.Width < 1 || capture.Height < 1) return covers;
            long limit = (long)capture.Width * capture.Height * 7 / 10;
            foreach (GameWindow window in windows)
            {
                if (window == null || window.Handle == IntPtr.Zero || window.ProcessId == (uint)selfProcessId) continue;
                if (!IsHelperProcess(window.ProcessName) || Score(window) >= 100) continue;
                Rectangle hit = Rectangle.Intersect(window.Client, capture);
                if (hit.Width < 1 || hit.Height < 1) continue;
                if ((long)hit.Width * hit.Height > limit) continue;
                if (inFront != null && !inFront(window, hit)) continue;
                covers.Add(hit);
            }
            return covers;
        }
        public static List<Rectangle> ForeignCovers(Rectangle screenArea)
        {
            List<Rectangle> covers = new List<Rectangle>();
            if (screenArea.Width < 1 || screenArea.Height < 1) return covers;
            try
            {
                int self = Process.GetCurrentProcess().Id;
                Dictionary<uint, string> names = new Dictionary<uint, string>();
                List<GameWindow> found = new List<GameWindow>();
                Native.EnumWindows(delegate(IntPtr window, IntPtr parameter)
                {
                    if (!Native.IsWindowVisible(window)) return true;
                    uint pid;
                    Native.GetWindowThreadProcessId(window, out pid);
                    if (pid == 0 || pid == (uint)self) return true;
                    string name;
                    if (!names.TryGetValue(pid, out name))
                    {
                        name = "";
                        try { using (Process process = Process.GetProcessById((int)pid)) name = process.ProcessName ?? ""; }
                        catch { name = ""; }
                        names[pid] = name;
                    }
                    if (!IsHelperProcess(name)) return true;
                    Native.Rect rect;
                    if (!Native.GetWindowRect(window, out rect)) return true;
                    found.Add(new GameWindow { Handle = window, ProcessId = pid, ProcessName = name, Title = "overlay", Client = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom) });
                    return true;
                }, IntPtr.Zero);
                covers = CoverRects(found, screenArea, self, delegate(GameWindow window, Rectangle overlap)
                {
                    return GameVisibility.Any(overlap, delegate(Point point) { return AtPoint(window.Handle, point, IntPtr.Zero); });
                });
            }
            catch (Exception ex) { CrashLog.Write(ex); }
            return covers;
        }
        public static List<GameWindow> Enumerate(int excludedProcessId)
        {
            List<GameWindow> list = new List<GameWindow>();
            Dictionary<uint, string> processNames = new Dictionary<uint, string>();
            Native.EnumWindows(delegate(IntPtr window, IntPtr parameter)
            {
                try { return Inspect(window, excludedProcessId, list, processNames); }
                catch (Exception ex) { CrashLog.Write(ex); return true; }
            }, IntPtr.Zero);
            return list.OrderByDescending(Score).ThenBy(w => w.Title).ToList();
        }
        private static bool Inspect(IntPtr window, int excludedProcessId, List<GameWindow> list, Dictionary<uint, string> processNames)
        {
                if (!Native.IsWindowVisible(window)) return true;
                uint pid; Native.GetWindowThreadProcessId(window, out pid);
                if (pid == excludedProcessId) return true;
                StringBuilder title = new StringBuilder(1024); Native.GetWindowText(window, title, title.Capacity);
                if (title.Length == 0) return true;
                StringBuilder className = new StringBuilder(256); Native.GetClassName(window, className, className.Capacity);
                string processName;
                if (!processNames.TryGetValue(pid, out processName))
                {
                    try { using (Process process = Process.GetProcessById((int)pid)) processName = process.ProcessName; }
                    catch { processName = ""; }
                    processNames[pid] = processName;
                }
                if (Contains(processName, "KavrosClueHelper") || Contains(title.ToString(), "Kavros Clue Helper")) return true;
                list.Add(new GameWindow { Handle = window, Title = title.ToString(), ProcessId = pid, ProcessName = processName, ClassName = className.ToString(), Client = Native.ClientBounds(window) });
                return true;
        }
        public static bool IsForeground(IntPtr game)
        {
            if (game == IntPtr.Zero) return false;
            IntPtr foreground = Native.GetForegroundWindow();
            if (foreground == IntPtr.Zero) return false;
            // Only this window or a child of it. Another Kavros client that shares
            // an owner is a different window and is not this client.
            return foreground == game || Native.GetAncestor(foreground, 2) == game;
        }
        public static bool BelongsTo(IntPtr game, Point point, IntPtr[] own)
        {
            if (game == IntPtr.Zero) return false;
            IntPtr hit = Native.WindowFromPoint(new Native.PointStruct { X = point.X, Y = point.Y });
            if (hit == IntPtr.Zero) return false;
            if (hit == game || Native.GetAncestor(hit, 2) == game) return true;
            if (own == null) return false;
            for (int i = 0; i < own.Length; i++)
            {
                IntPtr window = own[i];
                if (window == IntPtr.Zero) continue;
                if (hit == window || Native.GetAncestor(hit, 2) == window) return true;
            }
            return false;
        }
        // Companion, AFK Warden, the Slayer helper and this helper. Not the launcher
        // and not the game itself (the game is handled by IsForeground).
        public static bool IsHelperProcess(string processName)
        {
            string name = ProcessBaseName(processName);
            return EqualsName(name, "KavrosCompanion")
                || EqualsName(name, "KavrosAfkWarden")
                || EqualsName(name, "KavrosSlayerHelper")
                || EqualsName(name, "KavrosClueHelper");
        }
        public static bool IsWardenProcess(string processName)
        {
            return EqualsName(ProcessBaseName(processName), "KavrosAfkWarden");
        }
        public static bool IsCompanionProcess(string processName)
        {
            return EqualsName(ProcessBaseName(processName), "KavrosCompanion");
        }
        private static string ProcessBaseName(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return "";
            if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return processName.Substring(0, processName.Length - 4);
            return processName;
        }
        private static bool EqualsName(string processName, string expected)
        {
            return string.Equals(processName, expected, StringComparison.OrdinalIgnoreCase);
        }
        public static bool IsHelperWindow(IntPtr window)
        {
            if (window == IntPtr.Zero) return false;
            uint pid;
            Native.GetWindowThreadProcessId(window, out pid);
            if (pid == 0) return false;
            try
            {
                using (Process process = Process.GetProcessById((int)pid))
                    return IsHelperProcess(process.ProcessName);
            }
            catch { return false; }
        }
        public static bool AtPoint(IntPtr window,Point point,IntPtr transparentOverlay)
        {
            IntPtr hit=Native.WindowFromPoint(new Native.PointStruct { X=point.X,Y=point.Y });
            return hit!=IntPtr.Zero && (hit==transparentOverlay || hit==window || Native.GetAncestor(hit,2)==window);
        }
        // A live chosen client is not rediscovered on a timer. Enumeration is only for the first client, or after its handle dies.
        public static bool ShouldEnumerate(bool chosenAlive, bool discoveryDue, bool handleLost)
        {
            if (chosenAlive) return false;
            return discoveryDue || handleLost;
        }
        public static string Describe(IEnumerable<GameWindow> all, GameWindow selected)
        {
            StringBuilder report = new StringBuilder("Kavros Clue Helper " + AppBranding.DisplayVersion + "\r\n");
            report.AppendLine("Windows: " + Environment.OSVersion.VersionString);
            if (selected == null) report.AppendLine("Game target: none");
            else
            {
                report.AppendLine("Game target: " + selected.Title);
                report.AppendLine("Process: " + selected.ProcessName + " / " + selected.ProcessId);
                report.AppendLine("Window class: " + selected.ClassName);
                report.AppendLine("Client rectangle: " + Native.ClientBounds(selected.Handle));
                report.AppendLine("Minimized: " + Native.IsIconic(selected.Handle));
                report.AppendLine("Game foreground: " + IsForeground(selected.Handle));
            }
            foreach (GameWindow window in all.Where(w => Score(w) >= 100 || (IsLauncher(w) && (Contains(w.Title, "kavros") || Contains(w.ProcessName, "kavros")))))
                report.AppendLine("Candidate: " + window.Title + " | " + window.ProcessName + " | " + window.ClassName + " | score " + Score(window) + " | " + window.Client);
            return report.ToString();
        }
    }
}
