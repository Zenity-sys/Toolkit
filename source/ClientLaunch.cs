using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace KavrosClueHelper
{
    // The client this helper was opened from. A zero handle means it was not known yet.
    public sealed class ClientBinding
    {
        public readonly IntPtr Handle;
        public readonly uint ProcessId;
        public ClientBinding(IntPtr handle, uint processId)
        {
            Handle = handle;
            ProcessId = processId;
        }
        public static readonly ClientBinding Empty = new ClientBinding(IntPtr.Zero, 0);
    }

    // A Companion or AFK Warden window. Owner, Root and Below are the client it
    // belongs to, when one of those can be seen. The overlay itself is never a client.
    public sealed class LaunchOverlay
    {
        public IntPtr Handle;
        public uint ProcessId;
        public string ProcessName = "";
        public IntPtr Owner;
        public IntPtr Root;
        public IntPtr Below;
    }

    public static class ClientLaunch
    {
        public const string ClientProperty = "KavrosBoundClient";
        public const string ProcessProperty = "KavrosBoundProcess";

        // hwnd, then pid, then the foreground window, then the launching process,
        // then the single Companion overlay. A Companion or AFK Warden window is
        // never returned. AFK Warden's client is skipped unless an argument names it.
        public static ClientBinding Resolve(string[] args, IntPtr foreground, IList<GameWindow> windows, IList<uint> parentPids, IList<LaunchOverlay> overlays)
        {
            List<GameWindow> games = new List<GameWindow>();
            if (windows != null)
                foreach (GameWindow window in windows)
                    if (WindowLocator.Score(window) >= 100) games.Add(window);
            HashSet<IntPtr> claimed = Claimed(overlays, games);
            IntPtr requestedHandle = IntPtr.Zero;
            uint requestedProcess = 0;
            string handleText, processText;
            if (TryArg(args, "--hwnd", out handleText)) TryHandle(handleText, out requestedHandle);
            if (TryArg(args, "--pid", out processText))
            {
                uint parsed;
                if (uint.TryParse(processText, out parsed) && parsed != 0) requestedProcess = parsed;
            }
            if (requestedHandle != IntPtr.Zero)
            {
                ClientBinding named = FromHint(requestedHandle, games, overlays, claimed, true);
                if (named != null) return named;
            }
            if (requestedProcess != 0)
            {
                ClientBinding named = FromProcess(requestedProcess, foreground, games, overlays, claimed, true);
                if (named != null) return named;
            }
            if (foreground != IntPtr.Zero)
            {
                ClientBinding fromFront = FromHint(foreground, games, overlays, claimed, false);
                if (fromFront != null) return fromFront;
            }
            if (parentPids != null)
            {
                foreach (uint parent in parentPids)
                {
                    if (parent == 0) continue;
                    ClientBinding fromParent = FromProcess(parent, foreground, games, overlays, claimed, false);
                    if (fromParent != null) return fromParent;
                }
            }
            ClientBinding only = OnlyCompanion(overlays, games, claimed);
            return only == null ? ClientBinding.Empty : only;
        }

        // Companion passes --show-panel to open the clue panel without a remote click.
        public static bool ShowPanelRequested(string[] args)
        {
            if (args == null) return false;
            foreach (string arg in args)
                if (string.Equals(arg, "--show-panel", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static ClientBinding Capture(string[] args, IntPtr foreground)
        {
            try
            {
                int self = Process.GetCurrentProcess().Id;
                List<GameWindow> windows = WindowLocator.Enumerate(self);
                return Resolve(args, foreground, windows, ParentProcessIds(), CollectOverlays(self));
            }
            catch (Exception ex)
            {
                CrashLog.Write(ex);
                return ClientBinding.Empty;
            }
        }

        public static HashSet<IntPtr> ClaimedHandles(IList<GameWindow> windows)
        {
            try
            {
                List<GameWindow> games = new List<GameWindow>();
                if (windows != null)
                    foreach (GameWindow window in windows)
                        if (WindowLocator.Score(window) >= 100) games.Add(window);
                return Claimed(CollectOverlays(Process.GetCurrentProcess().Id), games);
            }
            catch (Exception ex)
            {
                CrashLog.Write(ex);
                return new HashSet<IntPtr>();
            }
        }

        private static ClientBinding FromHint(IntPtr hwnd, List<GameWindow> games, IList<LaunchOverlay> overlays, HashSet<IntPtr> claimed, bool explicitRequest)
        {
            GameWindow game = Find(games, hwnd);
            if (game != null)
            {
                if (!explicitRequest && claimed.Contains(game.Handle)) return null;
                return new ClientBinding(game.Handle, game.ProcessId);
            }
            LaunchOverlay overlay = FindOverlay(overlays, hwnd);
            if (overlay == null || WindowLocator.IsWardenProcess(overlay.ProcessName)) return null;
            return FromHost(HostOf(overlay, games), games, claimed, explicitRequest);
        }

        private static ClientBinding FromProcess(uint pid, IntPtr foreground, List<GameWindow> games, IList<LaunchOverlay> overlays, HashSet<IntPtr> claimed, bool explicitRequest)
        {
            foreach (GameWindow game in games)
            {
                if (game.ProcessId != pid) continue;
                if (!explicitRequest && claimed.Contains(game.Handle)) continue;
                return new ClientBinding(game.Handle, game.ProcessId);
            }
            List<ClientBinding> hosts = new List<ClientBinding>();
            if (overlays != null)
            {
                foreach (LaunchOverlay overlay in overlays)
                {
                    if (overlay == null || overlay.ProcessId != pid || WindowLocator.IsWardenProcess(overlay.ProcessName)) continue;
                    ClientBinding host = FromHost(HostOf(overlay, games), games, claimed, explicitRequest);
                    if (host == null) continue;
                    if (foreground == overlay.Handle || foreground == host.Handle) return host;
                    hosts.Add(host);
                }
            }
            if (hosts.Count == 1) return hosts[0];
            return null;
        }

        private static ClientBinding OnlyCompanion(IList<LaunchOverlay> overlays, List<GameWindow> games, HashSet<IntPtr> claimed)
        {
            ClientBinding found = null;
            if (overlays == null) return null;
            foreach (LaunchOverlay overlay in overlays)
            {
                if (overlay == null || !WindowLocator.IsCompanionProcess(overlay.ProcessName)) continue;
                ClientBinding host = FromHost(HostOf(overlay, games), games, claimed, false);
                if (host == null) continue;
                if (found != null && found.Handle != host.Handle) return null;
                found = host;
            }
            return found;
        }

        private static ClientBinding FromHost(IntPtr host, List<GameWindow> games, HashSet<IntPtr> claimed, bool explicitRequest)
        {
            if (host == IntPtr.Zero) return null;
            if (!explicitRequest && claimed.Contains(host)) return null;
            GameWindow game = Find(games, host);
            if (game == null) return null;
            return new ClientBinding(game.Handle, game.ProcessId);
        }

        private static IntPtr HostOf(LaunchOverlay overlay, List<GameWindow> games)
        {
            if (overlay == null) return IntPtr.Zero;
            IntPtr[] hints = new IntPtr[] { overlay.Owner, overlay.Root, overlay.Below };
            for (int i = 0; i < hints.Length; i++)
            {
                IntPtr hint = hints[i];
                if (hint == IntPtr.Zero || hint == overlay.Handle) continue;
                if (Find(games, hint) != null) return hint;
            }
            return IntPtr.Zero;
        }

        private static HashSet<IntPtr> Claimed(IList<LaunchOverlay> overlays, List<GameWindow> games)
        {
            HashSet<IntPtr> claimed = new HashSet<IntPtr>();
            if (overlays == null) return claimed;
            foreach (LaunchOverlay overlay in overlays)
            {
                if (overlay == null || !WindowLocator.IsWardenProcess(overlay.ProcessName)) continue;
                IntPtr host = HostOf(overlay, games);
                if (host != IntPtr.Zero) claimed.Add(host);
            }
            return claimed;
        }

        private static GameWindow Find(List<GameWindow> games, IntPtr handle)
        {
            if (handle == IntPtr.Zero) return null;
            for (int i = 0; i < games.Count; i++)
                if (games[i].Handle == handle) return games[i];
            return null;
        }

        private static LaunchOverlay FindOverlay(IList<LaunchOverlay> overlays, IntPtr handle)
        {
            if (overlays == null || handle == IntPtr.Zero) return null;
            foreach (LaunchOverlay overlay in overlays)
                if (overlay != null && overlay.Handle == handle) return overlay;
            return null;
        }

        private static bool TryArg(string[] args, string name, out string value)
        {
            value = null;
            if (args == null) return false;
            string prefix = name + "=";
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i] ?? "";
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    value = arg.Substring(prefix.Length).Trim();
                    return value.Length > 0;
                }
                if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    string next = args[i + 1] ?? "";
                    if (next.Length == 0 || next[0] == '-') return false;
                    value = next.Trim();
                    return value.Length > 0;
                }
            }
            return false;
        }

        private static bool TryHandle(string text, out IntPtr handle)
        {
            handle = IntPtr.Zero;
            if (string.IsNullOrEmpty(text)) return false;
            text = text.Trim();
            long number;
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (!long.TryParse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out number)) return false;
            }
            else if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                return false;
            if (number == 0) return false;
            handle = new IntPtr(number);
            return true;
        }

        private static List<uint> ParentProcessIds()
        {
            List<uint> parents = new List<uint>();
            IntPtr snapshot = IntPtr.Zero;
            try
            {
                snapshot = CreateToolhelp32Snapshot(2, 0);
                if (snapshot == IntPtr.Zero || snapshot == new IntPtr(-1)) return parents;
                Dictionary<uint, uint> map = new Dictionary<uint, uint>();
                ProcessEntry entry = new ProcessEntry();
                entry.dwSize = (uint)Marshal.SizeOf(typeof(ProcessEntry));
                if (!Process32First(snapshot, ref entry)) return parents;
                do { map[entry.ProcessId] = entry.ParentProcessId; }
                while (Process32Next(snapshot, ref entry));
                uint pid = (uint)Process.GetCurrentProcess().Id;
                for (int depth = 0; depth < 4; depth++)
                {
                    uint parent;
                    if (!map.TryGetValue(pid, out parent) || parent == 0 || parent == pid) break;
                    parents.Add(parent);
                    pid = parent;
                }
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
            finally
            {
                if (snapshot != IntPtr.Zero && snapshot != new IntPtr(-1)) CloseHandle(snapshot);
            }
            return parents;
        }

        private static List<LaunchOverlay> CollectOverlays(int selfProcessId)
        {
            List<LaunchOverlay> overlays = new List<LaunchOverlay>();
            Dictionary<uint, string> names = new Dictionary<uint, string>();
            try
            {
                Native.EnumWindows(delegate(IntPtr window, IntPtr parameter)
                {
                    try { NoteOverlay(window, selfProcessId, overlays, names); }
                    catch (Exception ex) { CrashLog.Write(ex); }
                    return true;
                }, IntPtr.Zero);
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
            return overlays;
        }

        private static void NoteOverlay(IntPtr window, int selfProcessId, List<LaunchOverlay> overlays, Dictionary<uint, string> names)
        {
            if (!Native.IsWindowVisible(window) || Native.IsIconic(window)) return;
            uint pid;
            Native.GetWindowThreadProcessId(window, out pid);
            if (pid == 0 || pid == (uint)selfProcessId) return;
            string name;
            if (!names.TryGetValue(pid, out name))
            {
                name = "";
                try { using (Process process = Process.GetProcessById((int)pid)) name = process.ProcessName ?? ""; }
                catch { name = ""; }
                names[pid] = name;
            }
            if (!WindowLocator.IsHelperProcess(name)) return;
            LaunchOverlay overlay = new LaunchOverlay();
            overlay.Handle = window;
            overlay.ProcessId = pid;
            overlay.ProcessName = name;
            IntPtr owner = Native.GetWindow(window, Native.GwOwner);
            if (owner != window) overlay.Owner = owner;
            IntPtr root = Native.GetAncestor(window, Native.GaRoot);
            if (root != window) overlay.Root = root;
            overlay.Below = WindowUnder(window, names);
            overlays.Add(overlay);
        }

        private static IntPtr WindowUnder(IntPtr overlay, Dictionary<uint, string> names)
        {
            Native.Rect bounds;
            if (!Native.GetWindowRect(overlay, out bounds)) return IntPtr.Zero;
            int x = bounds.Left + Math.Max(0, (bounds.Right - bounds.Left) / 2);
            int y = bounds.Top + Math.Max(0, (bounds.Bottom - bounds.Top) / 2);
            IntPtr cursor = overlay;
            for (int step = 0; step < 24 && cursor != IntPtr.Zero; step++)
            {
                cursor = Native.GetWindow(cursor, Native.GwHwndNext);
                if (cursor == IntPtr.Zero || cursor == overlay) break;
                if (!Native.IsWindowVisible(cursor) || Native.IsIconic(cursor)) continue;
                Native.Rect other;
                if (!Native.GetWindowRect(cursor, out other)) continue;
                if (x < other.Left || x >= other.Right || y < other.Top || y >= other.Bottom) continue;
                uint pid;
                Native.GetWindowThreadProcessId(cursor, out pid);
                string name;
                if (!names.TryGetValue(pid, out name))
                {
                    name = "";
                    try { using (Process process = Process.GetProcessById((int)pid)) name = process.ProcessName ?? ""; }
                    catch { name = ""; }
                    names[pid] = name;
                }
                if (WindowLocator.IsHelperProcess(name)) continue;
                return cursor;
            }
            return IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct ProcessEntry
        {
            public uint dwSize;
            public uint cntUsage;
            public uint ProcessId;
            public IntPtr DefaultHeapId;
            public uint ModuleId;
            public uint ThreadCount;
            public uint ParentProcessId;
            public int Priority;
            public uint Flags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string ExeFile;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "Process32FirstW")]
        private static extern bool Process32First(IntPtr snapshot, ref ProcessEntry entry);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "Process32NextW")]
        private static extern bool Process32Next(IntPtr snapshot, ref ProcessEntry entry);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);
    }
}
