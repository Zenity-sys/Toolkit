using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace KavrosClueHelper
{
    public sealed class Reading
    {
        public int[] Board = new int[25];
        public bool[] Certain = new bool[25];
        public int Uncertain;
    }

    public static class ScreenReader
    {
        // Always a plain screen copy. PrintWindow with PW_RENDERFULLCONTENT
        // forces the DirectX client to repaint and blanks the whole screen.
        public static Bitmap Capture(Rectangle bounds)
        {
            if (bounds.Width < 60 || bounds.Height < 60)
                throw new ArgumentException("Select the whole 5 x 5 grid, from its outer top-left to outer bottom-right corner.");
            Bitmap image = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
            try
            {
                using (Graphics graphics = Graphics.FromImage(image))
                    graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size, CopyPixelOperation.SourceCopy);
                return image;
            }
            catch { image.Dispose(); throw; }
        }
        public static void Cover(Bitmap image, Rectangle captureScreen, IList<Rectangle> screenRects)
        {
            if (image == null || screenRects == null) return;
            using (Graphics graphics = Graphics.FromImage(image))
            foreach (Rectangle screen in screenRects)
            {
                Rectangle local = screen; local.Offset(-captureScreen.X, -captureScreen.Y);
                local = Rectangle.Intersect(local, new Rectangle(Point.Empty, image.Size));
                if (local.Width < 1 || local.Height < 1) continue;
                int sampleX = local.Left > 1 ? local.Left - 2 : Math.Min(image.Width - 1, local.Right + 1);
                int sampleY = Math.Max(0, Math.Min(image.Height - 1, local.Top + local.Height / 2));
                using (Brush fill = new SolidBrush(image.GetPixel(sampleX, sampleY))) graphics.FillRectangle(fill, local);
            }
        }
        // Legacy 944 symbols have distinctive colours: blue hats, gold bows,
        // and pale swords. Ignore cell edges and vote on the icon's interior.
        // Unrecognised/ambiguous cells stay unknown rather than becoming swords.
        public static Reading Read(Bitmap source)
        {
            if (source.Width < 60 || source.Height < 60) throw new ArgumentException("Grid image is too small.");
            Reading result = new Reading();
            Pixels pixels = new Pixels(source);
            for (int row = 0; row < 5; row++)
                for (int column = 0; column < 5; column++)
                {
                    int left = (int)Math.Round((column + 0.18) * source.Width / 5.0);
                    int right = (int)Math.Round((column + 0.82) * source.Width / 5.0);
                    int top = (int)Math.Round((row + 0.18) * source.Height / 5.0);
                    int bottom = (int)Math.Round((row + 0.82) * source.Height / 5.0);
                    int blue = 0, gold = 0, pale = 0, samples = 0;
                    for (int y = top; y < bottom; y++)
                        for (int x = left; x < right; x++)
                        {
                            Color p = pixels.Get(x, y);
                            samples++;
                            if (p.B > 78 && p.B > p.R * 1.30 && p.B > p.G * 1.14 && p.B - p.R > 28) blue++;
                            if (p.R > 112 && p.G > 62 && p.R > p.G * 1.12 && p.G > p.B * 1.18 && p.R - p.B > 38) gold++;
                            int high = Math.Max(p.R, Math.Max(p.G, p.B));
                            int low = Math.Min(p.R, Math.Min(p.G, p.B));
                            if (low > 112 && high - low < 62) pale++;
                        }
                    int index = row * 5 + column;
                    int threshold = Math.Max(3, (int)(samples * 0.025));
                    if (blue >= threshold && blue > gold * 0.55)
                    { result.Board[index] = 2; result.Certain[index] = true; }
                    else if (gold >= threshold && gold > blue * 1.5)
                    { result.Board[index] = 1; result.Certain[index] = true; }
                    else if (pale >= threshold && blue < threshold && gold < threshold)
                    { result.Board[index] = 0; result.Certain[index] = true; }
                    else { result.Board[index] = -1; result.Uncertain++; }
                }
            return result;
        }
    }

    internal static class Native
    {
        [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] public struct PointStruct { public int X, Y; }
        public delegate bool EnumProc(IntPtr window, IntPtr param);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc callback, IntPtr parameter);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr window, uint flags);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr window, System.Text.StringBuilder name, int capacity);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr window, System.Text.StringBuilder text, int length);
        [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr window);
        [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr window, out Rect rect);
        [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr window, ref PointStruct point);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(PointStruct point);
        [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr window, uint command);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out Rect rect);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern bool SetProp(IntPtr window, string name, IntPtr data);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr GetProp(IntPtr window, string name);
        public const uint GwHwndNext = 2, GwOwner = 4, GaRoot = 2;
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr window, int id);
        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr window, int message, IntPtr wparam, IntPtr lparam);
        public const int NoticeMessage = 0x8014;
        public const int ShowPanelMessage = 0x8015;
        [DllImport("dwmapi.dll")] public static extern int DwmFlush();

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")] static extern int SetWindowLong32(IntPtr window, int index, int value);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")] static extern IntPtr SetWindowLongPtr64(IntPtr window, int index, IntPtr value);
        public const int SwpNoSize=0x0001,SwpNoMove=0x0002,SwpNoZOrder=0x0004,SwpNoActivate=0x0010,SwpNoOwnerZOrder=0x0200;
        public const int SwShowNoActivate=4,ExNoActivate=0x08000000,ExToolWindow=0x80;
        public static readonly IntPtr HwndTopMost=new IntPtr(-1),HwndNoTopMost=new IntPtr(-2);
        // Reasserting topmost must not activate the panel. Activation every tick steals the game's mouse.
        public static int TopMostFlags { get { return SwpNoSize|SwpNoMove|SwpNoActivate|SwpNoOwnerZOrder; } }
        public static int PlaceFlags { get { return SwpNoZOrder|SwpNoActivate|SwpNoOwnerZOrder; } }
        internal static int LastShowCommand;
        [DllImport("user32.dll", EntryPoint="GetWindowLong")] static extern int GetWindowLong32(IntPtr window, int index);
        [DllImport("user32.dll", EntryPoint="GetWindowLongPtr")] static extern IntPtr GetWindowLongPtr64(IntPtr window, int index);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, int flags);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr window, int command);
        // Show a helper surface without making it the foreground window.
        // SW_SHOWNOACTIVATE is the only show command this path uses.
        public static bool ShowFormNoActivate(System.Windows.Forms.Form form)
        {
            if (form == null || form.IsDisposed) return false;
            LastShowCommand = SwShowNoActivate;
            if (!form.IsHandleCreated) form.CreateControl();
            try { return ShowWindow(form.Handle, SwShowNoActivate); }
            catch (DllNotFoundException) { return false; }
            catch (EntryPointNotFoundException) { return false; }
        }
        public static void SetOwner(IntPtr child, IntPtr owner)
        {
            if (child == IntPtr.Zero) return;
            if (IntPtr.Size == 8) SetWindowLongPtr64(child, -8, owner);
            else SetWindowLong32(child, -8, owner.ToInt32());
        }
        // Move in physical pixels without touching z-order. WinForms Bounds is
        // scaled again when the handle is created, which shrinks the overlay
        // toward the top-left on a 1440p or 4K display.
        public static void PlaceNoZOrder(IntPtr window, Rectangle screen)
        {
            if (window == IntPtr.Zero || screen.Width < 1 || screen.Height < 1) return;
            SetWindowPos(window, IntPtr.Zero, screen.X, screen.Y, screen.Width, screen.Height, PlaceFlags);
        }
        public static void SetTopMostNoActivate(IntPtr window, bool top)
        {
            if (window == IntPtr.Zero) return;
            int style = IntPtr.Size == 8 ? GetWindowLongPtr64(window, -20).ToInt32() : GetWindowLong32(window, -20);
            bool already = (style & 0x00000008) != 0;
            if (already == top) return;
            SetWindowPos(window, top ? HwndTopMost : HwndNoTopMost, 0, 0, 0, 0, TopMostFlags);
        }
        // Drop or raise topmost without activating. After dropping, HWND_NOTOPMOST
        // lands above every normal window, so tuckBehind (usually the foreground
        // application) is used to slide the window just under that app.
        public static void ApplyZOrderNoActivate(IntPtr window, bool topMost, IntPtr tuckBehind)
        {
            if (window == IntPtr.Zero) return;
            SetTopMostNoActivate(window, topMost);
            if (!topMost && tuckBehind != IntPtr.Zero && tuckBehind != window)
                SetWindowPos(window, tuckBehind, 0, 0, 0, 0, TopMostFlags);
        }
        [DllImport("user32.dll")] static extern int GetSystemMetrics(int index);
        // Same pixel space as ClientToScreen. WinForms VirtualScreen can disagree
        // with that space when monitors use different DPI.
        public static Rectangle VirtualScreen()
        {
            try
            {
                int width = GetSystemMetrics(78), height = GetSystemMetrics(79);
                if (width > 0 && height > 0) return new Rectangle(GetSystemMetrics(76), GetSystemMetrics(77), width, height);
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
            return System.Windows.Forms.SystemInformation.VirtualScreen;
        }
        public static Rectangle ClientBounds(IntPtr window)
        {
            Rect r;
            PointStruct p = new PointStruct();
            if (!GetClientRect(window, out r) || !ClientToScreen(window, ref p)) return Rectangle.Empty;
            return new Rectangle(p.X, p.Y, r.Right - r.Left, r.Bottom - r.Top);
        }
    }
}
