using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace KavrosClueHelper
{
    public static class Program
    {
        private static System.Threading.Mutex singleInstance;
        [STAThread]
        public static int Main(string[] args)
        {
            CrashLog.Install();
            NetSecurity.EnableModernTls();
            try
            {
                try { AppUpdates.Start(); }
                catch (Exception startup) { CrashLog.Write(startup); }
                if(args.Length>1&&args[0]=="--catalogue-test")return ReferenceRegressionTests.Run(args[1]);
                if(args.Length>1&&args[0]=="--slider-optimiser-test")return SlidingOptimizerTests.Run(args[1]);
                if(args.Length>1&&args[0]=="--slider-click-test")return SlidingClickOptimizerTests.Run(args[1]);
                if(args.Length>1&&args[0]=="--expansion-test")return ExpansionRegressionTests.Run(args[1]);
                if(args.Length>1&&args[0]=="--slider-guidance-test")return SliderGuidanceRegressionTests.Run(args[1]);
                if(args.Length>1&&args[0]=="--follow-test")return FollowRegressionTests.Run(args[1]);
                if(args.Length>1&&args[0]=="--window-test")return WindowRegressionTests.Run(args[1]);
                if(args.Length>1&&args[0]=="--legacy-test")return LegacyInterfaceTests.Run(args[1]);
                if(args.Length>1&&args[0]=="--darkscape-test")return DarkscapeTowersTests.Run(args[1]);
                if(args.Length>1&&args[0]=="--tower-poll-test")return TowerPollingTests.Run(args[1]);
                if(args.Length>1&&args[0]=="--knot-test")return KnotSpeedTests.Run(args[1]);
                if(args.Length>1&&args[0]=="--knot-capture-test")return KnotCaptureTests.Run(args[1]);
                if(args.Length>1&&args[0]=="--steady-test")return SteadyTrackingTests.Run(args[1]);
                if(args.Length>1&&args[0]=="--reliability-test")return ReliabilityRegressionTests.Run(args[1]);
                if(args.Length>1&&args[0]=="--paint-test")return PaintingRegressionTests.Run(args[1]);
                if(args.Length>0 && args[0]=="--map-test") return MapIconRegressionTests.Run(args.Length>1 ? args[1] : "map-test-results.txt");
                if(args.Length>0 && args[0]=="--countdown-test") return CountdownRegressionTests.Run(args.Length>1 ? args[1] : "countdown-test-results.txt");
                if(args.Length>0 && args[0]=="--capture-test") return CaptureRegressionTests.Run(args.Length>1 ? args[1] : "capture-test-results.txt");
                try { Native.SetProcessDpiAwarenessContext(new IntPtr(-4)); }
                catch (EntryPointNotFoundException)
                {
                    try { Native.SetProcessDPIAware(); }
                    catch (EntryPointNotFoundException) { }
                    catch (DllNotFoundException) { }
                }
                catch (DllNotFoundException) { }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                if(args.Length>1&&args[0]=="--automation-test")return AutomationRegressionTests.Run(args[1]);
                if(args.Length>1&&args[0]=="--update-test")return UpdateRegressionTests.Run(args[1]);
                if (args.Length > 1 && args[0] == "--window-diagnostics")
                {
                    List<GameWindow> found = WindowLocator.Enumerate(System.Diagnostics.Process.GetCurrentProcess().Id);
                    File.WriteAllText(args[1], WindowLocator.Describe(found, WindowLocator.Choose(found, IntPtr.Zero, Native.GetForegroundWindow())));
                    return 0;
                }
                if(args.Length>0 && args[0]=="--master-test") return MasterRegressionTests.Run(args.Length>1 ? args[1] : "master-test-results.txt");
                if(args.Length>0 && args[0]=="--discovery-test") return DiscoveryRegressionTests.Run(args.Length>1 ? args[1] : "discovery-test-results.txt");
                if(args.Length>0 && args[0]=="--compass-test") return CompassRegressionTests.Run(args.Length>1 ? args[1] : "compass-test-results.txt");
                if(args.Length>0 && args[0]=="--robustness-test") return RobustnessRegressionTests.Run(args.Length>1 ? args[1] : "robustness-test-results.txt");
                if (args.Length > 0 && args[0] == "--self-test")
                {
                    string report = args.Length > 1 ? args[1] : "test-results.txt";
                    int legacy = Tests.Run(report);
                    if (legacy != 0) return legacy;
                    int suite = SuiteTests.Run(report);
                    if(suite!=0) return suite;
                    int windows=WindowRegressionTests.Run(report);
                    if(windows!=0) return windows;
                    int maps=MapIconRegressionTests.Run(report);
                    if(maps!=0) return maps;
                    int analysis=AnalysisRegressionTests.Run(report);
                    if(analysis!=0) return analysis;
                    int towerPoll=TowerPollingTests.Run(report);
                    if(towerPoll!=0) return towerPoll;
                    int knotCapture=KnotCaptureTests.Run(report);
                    if(knotCapture!=0) return knotCapture;
                    int overlay=OverlayRegressionTests.Run(report);
                    if(overlay!=0) return overlay;
                    int countdown=CountdownRegressionTests.Run(report);
                    if(countdown!=0) return countdown;
                    int master=MasterRegressionTests.Run(report);
                    if(master!=0) return master;
                    int capture=CaptureRegressionTests.Run(report);
                    if(capture!=0)return capture;
                    int robust=RobustnessRegressionTests.Run(report);
                    if(robust!=0)return robust;
                    int discovery=DiscoveryRegressionTests.Run(report);
                    if(discovery!=0)return discovery;
                    int darkscape=DarkscapeTowersTests.Run(report);
                    return darkscape==0?CompassRegressionTests.Run(report):darkscape;
                }
                if (args.Length > 1 && args[0] == "--render-ui")
                {
                    using (PuzzleSuiteForm form = new PuzzleSuiteForm(false))
                    {
                        form.ShowInTaskbar = false;
                        form.StartPosition = FormStartPosition.Manual;
                        form.Location = new Point(-30000, -30000);
                        form.Show();
                        form.LoadExample(args.Length > 2 ? args[2] : "Towers");
                        System.Diagnostics.Stopwatch renderWait=System.Diagnostics.Stopwatch.StartNew();
                        while(form.IsAnalyzing && renderWait.ElapsedMilliseconds<15000) { Application.DoEvents(); System.Threading.Thread.Sleep(10); }
                        Application.DoEvents();
                        using (Bitmap bitmap = new Bitmap(form.Width, form.Height))
                        { form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height)); bitmap.Save(args[1]); }
                    }
                    return 0;
                }
                IntPtr launchForeground = IntPtr.Zero;
                try { launchForeground = Native.GetForegroundWindow(); }
                catch (DllNotFoundException) { }
                catch (EntryPointNotFoundException) { }
                ClientBinding binding = ClientLaunch.Capture(args, launchForeground);
                bool firstInstance;
                singleInstance = new System.Threading.Mutex(true, InstanceName(binding), out firstInstance);
                NoticeToast.TrayHidden = PuzzleSuiteForm.NoTrayOnLaunch = ClientLaunch.NoTrayRequested(args);
                if (!firstInstance)
                {
                    IntPtr running = FindRunningHelper(binding);
                    bool posted = running != IntPtr.Zero && Native.PostMessage(running, ClientLaunch.ShowPanelRequested(args) ? Native.ShowPanelMessage : Native.NoticeMessage, IntPtr.Zero, IntPtr.Zero);
                    if (!posted) NoticeToast.ShowAndWait("Already running. Check the tray.", Native.VirtualScreen());
                    return 0;
                }
                PuzzleSuiteForm.LaunchBinding = binding;
                PuzzleSuiteForm.ShowPanelOnLaunch = ClientLaunch.ShowPanelRequested(args);
                PuzzleSuiteForm helper = new PuzzleSuiteForm();
                helper.Shown += delegate
                {
                    if (!String.IsNullOrEmpty(AppUpdates.RestartedVersion)) helper.ShowToast("Updated to " + AppUpdates.RestartedVersion + ". See what's new.", false, "What's new", delegate { helper.ShowWhatsNew(); });
                    helper.CheckForUpdatesOnStartup();
                };
                Application.Run(helper);
                System.GC.KeepAlive(singleInstance);
                return 0;
            }
            catch (Exception exception)
            {
                CrashLog.Write(exception);
                if (args.Length > 0) File.WriteAllText("error.txt", exception.ToString());
                return 1;
            }
        }

        private static string InstanceName(ClientBinding binding)
        {
            if (binding != null && binding.Handle != IntPtr.Zero) return @"Local\KavrosClueHelper." + binding.Handle.ToInt64();
            if (binding != null && binding.ProcessId != 0) return @"Local\KavrosClueHelper.pid." + binding.ProcessId;
            return @"Local\KavrosClueHelper";
        }
        // The notice goes to the helper already bound to this client, not to a helper on another client.
        private static IntPtr FindRunningHelper(ClientBinding binding)
        {
            IntPtr found = IntPtr.Zero;
            IntPtr wanted = binding == null ? IntPtr.Zero : binding.Handle;
            uint wantedProcess = binding == null ? 0 : binding.ProcessId;
            try
            {
                Native.EnumWindows(delegate(IntPtr window, IntPtr parameter)
                {
                    StringBuilder text = new StringBuilder(256);
                    if (Native.GetWindowText(window, text, text.Capacity) <= 0 || !text.ToString().StartsWith("Kavros Clue Helper")) return true;
                    if (wanted != IntPtr.Zero)
                    {
                        if (Native.GetProp(window, ClientLaunch.ClientProperty) != wanted) return true;
                    }
                    else if (wantedProcess != 0)
                    {
                        if (Native.GetProp(window, ClientLaunch.ProcessProperty).ToInt64() != wantedProcess) return true;
                    }
                    found = window;
                    return false;
                }, IntPtr.Zero);
            }
            catch (Exception ex) { CrashLog.Write(ex); }
            return found;
        }
    }

    public sealed class GameWindow
    {
        public IntPtr Handle;
        public string Title;
        public string ProcessName = "";
        public string ClassName = "";
        public uint ProcessId;
        public Rectangle Client;
        public bool Automatic;
        public string Display;
        public override string ToString() { return string.IsNullOrEmpty(Display) ? Title : Display; }
    }

    public sealed class HelperForm : Form
    {
        private readonly ComboBox windows = new ComboBox();
        private readonly CheckBox live = new CheckBox();
        private readonly CheckBox showOverlay = new CheckBox();
        private readonly CheckBox reverse = new CheckBox();
        private readonly Label status = new Label();
        private readonly Label answer = new Label();
        private readonly Label regionText = new Label();
        private readonly Label hotkeys = new Label();
        private readonly BoardView boardView = new BoardView();
        private readonly PictureBox preview = new PictureBox();
        private readonly OverlayForm overlay = new OverlayForm();
        private readonly Timer timer = new Timer();
        private Rectangle relativeRegion;
        private Size calibratedClientSize;
        private int[] current;
        private int[] candidate;
        private int[] accepted;
        private Solution solution;
        private bool busy;
        private bool manual;
        private bool changedSinceCapture;

        public HelperForm()
        {
            Text = "Kavros Clue Helper | Lockbox v0.1";
            ClientSize = new Size(440, 756);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.FromArgb(18, 23, 32);
            ForeColor = Color.FromArgb(224, 232, 242);
            Font = new Font("Segoe UI", 9F);

            Label title = LabelAt("KAVROS / CLUE HELPER", 20, 17, 400, 25);
            title.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            Label subtitle = LabelAt("Legacy 944  /  Lockbox puzzles", 20, 46, 400, 20);
            subtitle.ForeColor = Color.FromArgb(151, 172, 194);
            LabelAt("1  Choose your game window", 20, 81, 380, 20);
            windows.SetBounds(20, 107, 310, 28);
            windows.DropDownStyle = ComboBoxStyle.DropDownList;
            Controls.Add(windows);
            ButtonAt("Refresh", 338, 106, 82, delegate { RefreshWindows(); });
            windows.SelectedIndexChanged += delegate { ResetCalibration(); };

            LabelAt("2  Select the outer edges of the 5 x 5 grid", 20, 150, 400, 20);
            ButtonAt("Select grid", 20, 177, 126, delegate { SelectRegion(); });
            ButtonAt("Read & solve", 154, 177, 126, delegate { ReadOnce(); });
            ButtonAt("Load example", 288, 177, 132, delegate { LoadExample(); });
            regionText.SetBounds(20, 215, 400, 21);
            regionText.ForeColor = Color.FromArgb(151, 172, 194);
            regionText.Text = "No grid selected yet.";
            Controls.Add(regionText);

            live.Text = "Track live"; live.SetBounds(20, 244, 120, 24);
            live.CheckedChanged += delegate
            {
                candidate = null; accepted = null;
                if (live.Checked && relativeRegion.IsEmpty)
                { live.Checked = false; status.Text = "Select the live game grid before enabling tracking."; return; }
                if (!live.Checked) { overlay.Hide(); status.Text = "Live tracking paused. Read & solve to capture once."; }
            };
            Controls.Add(live);
            showOverlay.Text = "Show overlay"; showOverlay.SetBounds(160, 244, 140, 24); showOverlay.Checked = true;
            showOverlay.CheckedChanged += delegate { UpdateOverlay(); };
            Controls.Add(showOverlay);
            CheckBox pin = new CheckBox { Text = "Pin panel", Left = 320, Top = 244, Width = 100, Height = 24 };
            pin.CheckedChanged += delegate { TopMost = pin.Checked; };
            Controls.Add(pin);

            LabelAt("3  Check the symbols, then follow the numbers", 20, 283, 400, 21);
            boardView.SetBounds(20, 313, 250, 250);
            boardView.Edited += delegate
            {
                live.Checked = false;
                manual = true;
                changedSinceCapture = true;
                current = (int[])boardView.Board.Clone();
                SolveCurrent();
            };
            Controls.Add(boardView);
            Label legend = LabelAt("M  Melee / sword\nR   Ranged / bow\nW  Magic / hat\n\nClick a preview tile\nto fix its symbol.\n\n1 / 2 = clicks needed\nGreen = next tile", 287, 314, 140, 192);
            legend.ForeColor = Color.FromArgb(167, 186, 207);
            preview.SetBounds(288, 510, 128, 64);
            preview.SizeMode = PictureBoxSizeMode.Zoom;
            preview.BackColor = Color.FromArgb(26, 34, 46);
            Controls.Add(preview);

            answer.SetBounds(20, 585, 400, 43);
            answer.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            answer.ForeColor = Color.FromArgb(87, 220, 181);
            answer.Text = "Open a lockbox in Kavros to begin.";
            Controls.Add(answer);
            status.SetBounds(20, 634, 400, 46);
            status.ForeColor = Color.FromArgb(167, 186, 207);
            status.Text = "Screen reading and visual guidance. You perform the moves.";
            Controls.Add(status);
            reverse.Text = "Reverse symbol cycle (only if the server needs it)";
            reverse.SetBounds(20, 685, 400, 24);
            reverse.CheckedChanged += delegate { accepted = null; SolveCurrent(); };
            Controls.Add(reverse);
            hotkeys.SetBounds(20, 721, 400, 22);
            hotkeys.ForeColor = Color.FromArgb(125, 148, 173);
            hotkeys.Text = "Ctrl+Shift+L: read  /  Ctrl+Shift+H: hide overlay";
            Controls.Add(hotkeys);

            timer.Interval = 700;
            timer.Tick += delegate { TickCapture(); };
            timer.Start();
            RefreshWindows();
            FormClosed += delegate
            {
                timer.Stop(); timer.Dispose(); overlay.Dispose();
                if (preview.Image != null) preview.Image.Dispose();
                Native.UnregisterHotKey(Handle, 1); Native.UnregisterHotKey(Handle, 2);
            };
        }

        protected override void OnHandleCreated(EventArgs args)
        {
            base.OnHandleCreated(args);
            bool read = Native.RegisterHotKey(Handle, 1, 0x4006, (uint)Keys.L);
            bool hide = Native.RegisterHotKey(Handle, 2, 0x4006, (uint)Keys.H);
            if ((!read || !hide) && hotkeys != null)
                hotkeys.Text = "A hotkey is in use; use the panel buttons instead.";
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x0312)
            {
                if (message.WParam.ToInt32() == 1) ReadOnce();
                if (message.WParam.ToInt32() == 2) showOverlay.Checked = !showOverlay.Checked;
            }
            base.WndProc(ref message);
        }

        private Label LabelAt(string text, int x, int y, int width, int height)
        {
            Label label = new Label { Text = text, Left = x, Top = y, Width = width, Height = height };
            Controls.Add(label); return label;
        }

        private Button ButtonAt(string text, int x, int y, int width, Action action)
        {
            Button button = new Button { Text = text, Left = x, Top = y, Width = width, Height = 31, FlatStyle = FlatStyle.Flat };
            button.BackColor = Color.FromArgb(35, 48, 64);
            button.ForeColor = ForeColor;
            button.FlatAppearance.BorderColor = Color.FromArgb(62, 81, 104);
            button.Click += delegate { try { action(); } catch (Exception ex) { overlay.Hide(); status.Text = ex.Message; } };
            Controls.Add(button); return button;
        }

        private void RefreshWindows()
        {
            IntPtr previous = SelectedWindow;
            List<GameWindow> found = WindowLocator.Clients(WindowLocator.Enumerate(System.Diagnostics.Process.GetCurrentProcess().Id));
            windows.Items.Clear();
            WindowLocator.NameClients(found);
            foreach (GameWindow window in found) windows.Items.Add(window);
            int selected = found.FindIndex(w => w.Handle == previous);
            if (selected >= 0) windows.SelectedIndex = selected;
            else if (previous == IntPtr.Zero && found.Count == 1) windows.SelectedIndex = 0;
            else windows.SelectedIndex = -1;
        }

        private IntPtr SelectedWindow { get { GameWindow window = windows.SelectedItem as GameWindow; return window == null ? IntPtr.Zero : window.Handle; } }
        private int Direction { get { return reverse.Checked ? -1 : 1; } }

        private void ResetCalibration()
        {
            relativeRegion = Rectangle.Empty;
            live.Checked = false;
            candidate = null; accepted = null;
            overlay.Hide(); solution = null;
            if (regionText != null) regionText.Text = "No grid selected yet.";
        }

        private void SelectRegion()
        {
            IntPtr game = SelectedWindow;
            if (game == IntPtr.Zero || !Native.IsWindow(game)) throw new Exception("Choose the Kavros window first. Use Refresh if it is missing.");
            if (Native.IsIconic(game)) throw new Exception("Restore the Kavros window first.");
            live.Checked = false; overlay.Hide();
            Native.SetForegroundWindow(game);
            Rectangle client = Native.ClientBounds(game);
            if (client.Width < 100 || client.Height < 100) throw new Exception("The game window is too small.");
            GridSelector selector = new GridSelector(client);
            selector.FormClosed += delegate
            {
                try
                {
                    if (selector.DialogResult != DialogResult.OK) return;
                    Rectangle region = selector.Selection;
                    if (region.Width < 60 || region.Height < 60 || Math.Abs(region.Width - region.Height) > Math.Max(region.Width, region.Height) * 0.12)
                    { status.Text = "Select a square around all 25 tiles, without the title or instructions. Try again."; return; }
                    relativeRegion = region;
                    calibratedClientSize = client.Size;
                    manual = false; candidate = null; accepted = null;
                    regionText.Text = String.Format("Grid selected: {0} x {1} px. Reselect if the puzzle moves.", relativeRegion.Width, relativeRegion.Height);
                    ReadOnce();
                }
                catch (Exception ex) { status.Text = ex.Message; }
                finally { selector.Dispose(); }
            };
            selector.Show();
        }

        private Rectangle ScreenRegion()
        {
            IntPtr game = SelectedWindow;
            if (relativeRegion.IsEmpty) throw new Exception("Click Select grid first.");
            if (game == IntPtr.Zero || !Native.IsWindow(game)) throw new Exception("Kavros has closed. Use Refresh and select its new window.");
            if (Native.IsIconic(game)) throw new Exception("Restore the game window to read the puzzle.");
            Rectangle client = Native.ClientBounds(game);
            if (client.Size != calibratedClientSize) throw new Exception("The game window was resized. Select the grid again.");
            Rectangle result = relativeRegion;
            result.Offset(client.Location);
            if (!SystemInformation.VirtualScreen.Contains(result)) throw new Exception("Move the whole puzzle onto the screen.");
            return result;
        }

        private Reading CaptureReading()
        {
            Rectangle region = ScreenRegion();
            if (Visible && Bounds.IntersectsWith(region) && (TopMost || Native.GetForegroundWindow() == Handle))
                throw new Exception("Move the helper panel away from the puzzle before capturing.");
            // Hide our overlay before capture, so its numbers never become input.
            overlay.Hide();
            try { Native.DwmFlush(); } catch (DllNotFoundException) { }
            using (Bitmap image = ScreenReader.Capture(region))
            {
                Image old = preview.Image;
                preview.Image = (Image)image.Clone();
                if (old != null) old.Dispose();
                return ScreenReader.Read(image);
            }
        }

        private bool CanReadForeground()
        {
            IntPtr foreground = Native.GetForegroundWindow();
            return foreground == SelectedWindow || foreground == Handle;
        }

        private void ReadOnce()
        {
            if (busy) return;
            if (!CanReadForeground()) { overlay.Hide(); status.Text = "Bring Kavros or this panel to the foreground first."; return; }
            busy = true;
            try
            {
                Reading reading = CaptureReading();
                live.Checked = false;
                manual = false; changedSinceCapture = false; accepted = null; candidate = null;
                current = reading.Board;
                boardView.Board = (int[])current.Clone();
                if (reading.Uncertain > 0)
                {
                    solution = null; boardView.Clicks = null; boardView.Invalidate();
                    answer.Text = "Check the ? tiles in the preview.";
                    status.Text = String.Format("{0} unclear tile(s). Fix them by clicking the preview, or reselect the grid.", reading.Uncertain);
                    return;
                }
                SolveCurrent();
            }
            catch (Exception ex) { overlay.Hide(); status.Text = ex.Message; }
            finally { busy = false; }
        }

        private void TickCapture()
        {
            if (busy) return;
            if (relativeRegion.IsEmpty || SelectedWindow == IntPtr.Zero || Native.GetForegroundWindow() != SelectedWindow || Native.IsIconic(SelectedWindow))
            { overlay.Hide(); return; }
            if (!live.Checked) { UpdateOverlay(); return; }
            busy = true;
            try
            {
                Reading reading = CaptureReading();
                if (reading.Uncertain > 0)
                {
                    candidate = null; accepted = null; solution = null;
                    current = reading.Board;
                    boardView.Board = (int[])current.Clone();
                    boardView.Clicks = null;
                    boardView.Invalidate();
                    answer.Text = "Waiting for a clear lockbox...";
                    status.Text = "Keep the grid visible. Reselect it if the puzzle has moved.";
                    return;
                }
                // Wait for two matching samples to avoid solving animation frames.
                if (candidate == null || !candidate.SequenceEqual(reading.Board))
                { candidate = reading.Board; return; }
                if (accepted == null || !accepted.SequenceEqual(reading.Board))
                {
                    accepted = (int[])reading.Board.Clone();
                    current = reading.Board;
                    manual = false; changedSinceCapture = false;
                    boardView.Board = (int[])current.Clone();
                    SolveCurrent();
                }
                else UpdateOverlay();
            }
            catch (Exception ex)
            { overlay.Hide(); live.Checked = false; status.Text = ex.Message; }
            finally { busy = false; }
        }

        private void SolveCurrent()
        {
            overlay.Hide();
            if (current == null || current.Any(x => x < 0))
            { solution = null; boardView.Clicks = null; boardView.Invalidate(); answer.Text = "Fill every ? tile before solving."; return; }
            solution = LockboxSolver.Solve(current, Direction);
            boardView.Clicks = solution == null ? null : solution.Clicks;
            boardView.Invalidate();
            if (solution == null)
            { answer.Text = "This board has no solution."; status.Text = "Check the symbols or try reversing the cycle."; return; }
            if (solution.Total == 0)
            { answer.Text = "Solved - all symbols match."; status.Text = manual ? "Example / manual board." : "Open the next lockbox to continue."; return; }
            int next = Array.FindIndex(solution.Clicks, x => x > 0);
            string symbol = new[] { "melee", "ranged", "magic" }[solution.Target];
            answer.Text = String.Format("{0} clicks to match {1}\nNext: row {2}, column {3} - click {4}x", solution.Total, symbol, next / 5 + 1, next % 5 + 1, solution.Clicks[next]);
            status.Text = manual
                ? (changedSinceCapture ? "Manual correction. Overlay shows this plan; read again after making moves." : "Example board from your screenshot. Select the live grid to play.")
                : (live.Checked ? "Following the game. Numbers refresh after the board settles." : "Numbers show total clicks per tile. Enable Track live to follow your moves.");
            UpdateOverlay();
        }

        private void UpdateOverlay()
        {
            if (!showOverlay.Checked || relativeRegion.IsEmpty || solution == null || solution.Total == 0 || Native.GetForegroundWindow() != SelectedWindow)
            { overlay.Hide(); return; }
            try { overlay.Display(ScreenRegion(), solution.Clicks); }
            catch { overlay.Hide(); }
        }

        public void LoadExample()
        {
            live.Checked = false; overlay.Hide();
            // A demo is never associated with an existing screen calibration.
            relativeRegion = Rectangle.Empty;
            regionText.Text = "Example loaded. Select the game grid for an overlay.";
            current = (int[])LockboxSolver.Example.Clone();
            boardView.Board = (int[])current.Clone();
            candidate = null; accepted = null; manual = true; changedSinceCapture = false;
            SolveCurrent();
        }
    }

    public sealed class BoardView : Control
    {
        public int[] Board = Enumerable.Repeat(-1, 25).ToArray();
        public int[] Clicks;
        public event Action Edited;
        public BoardView() { DoubleBuffered = true; }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.X < 0 || e.Y < 0 || e.X >= Width || e.Y >= Height) return;
            int index = Math.Min(4, e.Y * 5 / Height) * 5 + Math.Min(4, e.X * 5 / Width);
            Board[index] = (Board[index] + 1) % 3;
            if (Edited != null) Edited();
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int next = Clicks == null ? -1 : Array.FindIndex(Clicks, x => x > 0);
            using (Font symbolFont = new Font("Segoe UI", 16F, FontStyle.Bold))
            using (Font countFont = new Font("Segoe UI", 8F, FontStyle.Bold))
                for (int i = 0; i < 25; i++)
                {
                    Rectangle cell = new Rectangle(i % 5 * Width / 5 + 2, i / 5 * Height / 5 + 2, Width / 5 - 4, Height / 5 - 4);
                    using (Brush bg = new SolidBrush(i == next ? Color.FromArgb(22, 69, 61) : Color.FromArgb(30, 41, 56))) g.FillRectangle(bg, cell);
                    using (Pen border = new Pen(i == next ? Color.FromArgb(87, 220, 181) : Color.FromArgb(54, 70, 91))) g.DrawRectangle(border, cell);
                    int v = Board[i];
                    string text = v < 0 ? "?" : new[] { "M", "R", "W" }[v];
                    Color colour = v == 0 ? Color.FromArgb(225, 230, 235) : v == 1 ? Color.FromArgb(237, 181, 88) : v == 2 ? Color.FromArgb(113, 166, 255) : Color.FromArgb(230, 121, 119);
                    TextRenderer.DrawText(g, text, symbolFont, cell, colour, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    if (Clicks != null && Clicks[i] > 0)
                    {
                        Rectangle badge = new Rectangle(cell.Right - 16, cell.Top + 2, 14, 14);
                        using (Brush bg = new SolidBrush(Color.FromArgb(8, 16, 24))) g.FillEllipse(bg, badge);
                        TextRenderer.DrawText(g, Clicks[i].ToString(), countFont, badge, Color.FromArgb(87, 220, 181), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    }
                }
        }
    }

    public sealed class OverlayForm : Form
    {
        private int[] clicks;
        public OverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.Fuchsia;
            TransparencyKey = Color.Fuchsia;
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.None;
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get { CreateParams value = base.CreateParams; value.ExStyle |= 0x00080000 | 0x00000020 | 0x08000000 | 0x80; return value; }
        }
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x0084) { message.Result = new IntPtr(-1); return; }
            base.WndProc(ref message);
        }
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); }
        public void Display(Rectangle bounds, int[] values)
        {
            Bounds = bounds;
            clicks = (int[])values.Clone();
            if (!Visible) Show();
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (clicks == null) return;
            int next = Array.FindIndex(clicks, x => x > 0);
            using (Font font = new Font("Segoe UI", Math.Max(8, Math.Min(12, Width / 18F)), FontStyle.Bold))
                for (int i = 0; i < 25; i++)
                {
                    if (clicks[i] == 0) continue;
                    int x = i % 5 * Width / 5, y = i / 5 * Height / 5;
                    Rectangle cell = new Rectangle(x + 1, y + 1, Width / 5 - 3, Height / 5 - 3);
                    Color colour = i == next ? Color.Lime : Color.Gold;
                    using (Pen pen = new Pen(colour, i == next ? 2 : 1)) e.Graphics.DrawRectangle(pen, cell);
                    Rectangle label = new Rectangle(x + 2, y + 2, Math.Max(11, Width / 12), Math.Max(13, Height / 12));
                    e.Graphics.FillRectangle(Brushes.Black, label);
                    TextRenderer.DrawText(e.Graphics, clicks[i].ToString(), font, label, colour, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
        }
    }

    public sealed class GridSelector : Form
    {
        private readonly Bitmap screenshot;
        private Point start;
        private bool dragging;
        public Rectangle Selection;
        public GridSelector(Rectangle bounds)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
            TopMost = true;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.None;
            Cursor = Cursors.Cross;
            screenshot = ScreenReader.Capture(bounds);
            KeyPreview = true;
            KeyDown += delegate(object sender, KeyEventArgs args) { if (args.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); } };
        }
        protected override void OnMouseDown(MouseEventArgs e)
        { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { start = e.Location; dragging = true; Capture = true; } }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!dragging) return;
            Point end = new Point(Math.Max(0, Math.Min(ClientSize.Width - 1, e.X)), Math.Max(0, Math.Min(ClientSize.Height - 1, e.Y)));
            Selection = Rectangle.FromLTRB(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Max(start.X, end.X), Math.Max(start.Y, end.Y));
            Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!dragging || e.Button != MouseButtons.Left) return;
            dragging = false; Capture = false;
            if (Selection.Width < 10 || Selection.Height < 10) return;
            DialogResult = DialogResult.OK; Close();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.DrawImageUnscaled(screenshot, Point.Empty);
            using (Brush shade = new SolidBrush(Color.FromArgb(100, 0, 0, 0))) e.Graphics.FillRectangle(shade, ClientRectangle);
            if (!Selection.IsEmpty)
            {
                e.Graphics.DrawImage(screenshot, Selection, Selection, GraphicsUnit.Pixel);
                using (Pen pen = new Pen(Color.Lime, 1))
                {
                    e.Graphics.DrawRectangle(pen, Selection);
                    for (int i = 1; i < 5; i++)
                    {
                        int x = Selection.Left + Selection.Width * i / 5, y = Selection.Top + Selection.Height * i / 5;
                        e.Graphics.DrawLine(pen, x, Selection.Top, x, Selection.Bottom);
                        e.Graphics.DrawLine(pen, Selection.Left, y, Selection.Right, y);
                    }
                }
            }
            Rectangle banner = new Rectangle(10, 10, Math.Min(680, Width - 20), 40);
            e.Graphics.FillRectangle(Brushes.Black, banner);
            TextRenderer.DrawText(e.Graphics, "Drag around ALL 25 tiles only. Line up the 5 x 5 guide. Esc cancels.", Font, banner, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        protected override void Dispose(bool disposing)
        { if (disposing && screenshot != null) screenshot.Dispose(); base.Dispose(disposing); }
    }
}
