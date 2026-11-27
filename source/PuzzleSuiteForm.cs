using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading;
using Timer = System.Windows.Forms.Timer;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace KavrosClueHelper
{
    public sealed class GuidanceOverlay : Form
    {
        private List<GuidanceMark> marks = new List<GuidanceMark>();
        private Rectangle placed;
        private bool placedKnown;
        public bool PlacedNear(Rectangle screen, int tolerance) { return placedKnown && SteadyPolicy.Near(placed, screen, tolerance); }
        public Rectangle PlacedRectangle { get { return placed; } }
        public GuidanceOverlay()
        {
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true;
            BackColor = Color.Fuchsia; TransparencyKey = Color.Fuchsia; DoubleBuffered = true; AutoScaleMode = AutoScaleMode.None;
            // Paint each frame off screen in one pass so the marks never flash.
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams { get { CreateParams p = base.CreateParams; p.ExStyle |= 0x00080000 | 0x00000020 | 0x08000000 | 0x80; return p; } }
        protected override void WndProc(ref Message m) { if (m.Msg == 0x84) { m.Result = new IntPtr(-1); return; } base.WndProc(ref m); }
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); }
        public void UpdateGuidance(Rectangle bounds, List<GuidanceMark> next)
        {
            bool show,hide,move,paint; Rectangle nextPlaced; bool nextKnown;
            GuidanceDrawing.Present(Visible, placedKnown, placed, true, bounds, !GuidanceDrawing.SameMarks(marks, next), out show, out hide, out move, out paint, out nextKnown, out nextPlaced);
            placedKnown=nextKnown; placed=nextPlaced;
            if(paint) marks=next;
            // Show before placing. Creating the handle scales Form.Bounds, so the
            // physical rectangle is applied afterwards and only when it changes.
            if(show) Show();
            if((move||show) && bounds.Width>0 && bounds.Height>0)
            {
                if(!IsHandleCreated) CreateControl();
                Native.PlaceNoZOrder(Handle, bounds);
            }
            else if(hide) Hide();
            if(paint) Invalidate();
        }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Fuchsia);
            GuidanceDrawing.Draw(e.Graphics, marks);
        }
    }

    public sealed partial class PuzzleSuiteForm : Form
    {
        private readonly ComboBox windows = new ComboBox();
        private readonly CheckBox automatic = new KavrosCheckBox(), draw = new KavrosCheckBox(), reverse = new KavrosCheckBox();
        private readonly CheckBox towerCountdown = new KavrosCheckBox { Checked = true };
        private readonly ToolTip sliderTips=new ToolTip();
        private readonly Label detected = new PaintedLabel { Renderer = StatusText.DrawDetection }, summary = new Label(), status = new Label(), connection = new PaintedLabel { Renderer = StatusText.DrawConnection };
        private readonly PictureBox preview = new PreviewBox();
        private readonly SuiteCanvas canvas = new SuiteCanvas();
        private readonly MasterClueView masterView = new MasterClueView();
        private Label instructions;
        private readonly GuidanceOverlay overlay = new GuidanceOverlay();
        private readonly CaptureSearch captureSearch = new CaptureSearch();
        private readonly OverlayPreferences preferences;
        private readonly CheckBox floatingEnabled=new KavrosCheckBox();
        private readonly CheckBox checkUpdates=new KavrosCheckBox();
        private readonly CheckBox blurPreview=new KavrosCheckBox();
        private readonly InterfaceToggle modernInterface=new InterfaceToggle();
        private readonly InterfaceToggle legacyInterface=new InterfaceToggle();
        private readonly ComboBox resolutionChoice=new ComboBox();
        private readonly ComboBox scalingChoice=new ComboBox();
        private bool choosingInterface, refreshingClient;
        private FloatingCluePanel floatingPanel;
        private CompassSession compassSession;
        private NotifyIcon tray;
        internal static bool NoTrayOnLaunch;
        private readonly Timer timer = new Timer();
        private readonly BackgroundWorker analysisWorker = new BackgroundWorker();
        private PuzzleAnalysisEngine analysisEngine;
        private readonly object engineGate=new object();
        private readonly string assetPath;
        private DetectedPuzzle current, lastLocated;
        // Client area of a towers grid that is still opening, read next frame.
        private Rectangle? towerHint;
        // A scan clue stays locked until a different clue is read twice in a row.
        private readonly ChangeConfirmation scanChange=new ChangeConfirmation();
        private readonly ClueRetention retainedClue=new ClueRetention();
        private DateTime scanClosedAt=DateTime.MinValue;
        private volatile string analysisStage;
        private readonly ProgressBar scanProgress=new KavrosProgressBar();
        private string pendingKey;
        private List<GuidanceMark> marks = new List<GuidanceMark>();
        private bool busy, offline;
        private KnotPuzzle knotHistory;
        private List<int> slidingMoves;
        private int[] slidingBoard;
        private int sliderRereadMisses;
        private DateTime sliderStepShown;
        private string slidingTheme;
        private int slidingBatchNumber=1;
        private List<GameWindow> availableWindows = new List<GameWindow>();
        private GameWindow attachedWindow;
        internal static ClientBinding LaunchBinding;
        internal static bool ShowPanelOnLaunch;
        private IntPtr launchHandle;
        private uint launchProcess;
        private IntPtr panelOwner=new IntPtr(-1),overlayOwner=new IntPtr(-1),orderTuck=new IntPtr(-1);
        private bool panelTop,panelTopKnown,overlayTop,overlayTopKnown;
        private DateTime raiseUntil=DateTime.MinValue;
        private bool refreshingWindows, requestedScan;
        private string autoCaption;
        private DateTime nextDiscovery = DateTime.MinValue;
        private string captureDetails = "No capture attempted yet.";
        private string puzzleDetails = "No puzzle detected yet.";
        private CancellationTokenSource analysisCancellation;
        private int sourceGeneration;
        private bool closing;
        private DateTime nextRead = DateTime.MinValue;
        private AnalysisJob activeJob, pendingJob;
        private Bitmap diagnosticCapture, offlineSource;
        private DetectedPuzzle panelPuzzle;
        private string panelText, panelStage;
        private bool panelSaved;

        public bool IsAnalyzing { get { return busy || pendingJob!=null; } }
        private sealed class AnalysisJob
        {
            public Bitmap Image;
            public int Generation;
            public IntPtr Window;
            public Point CaptureOffset;
            public bool Stable, Offline, Reverse;
            public bool WholeRows;
            public bool Cropped;
            public Size ClientSize;
            public DateTime CapturedAt;
            public int Dpi;
            public GuideInk Guide;
            public bool LegacyInterface;
            public int Resolution, Scaling, ResolutionManual, ScalingManual, ClientHeight;
        }

        public PuzzleSuiteForm(bool startWithPanel=true) : this(null, startWithPanel) { }
        internal PuzzleSuiteForm(OverlayPreferences stored, bool startWithPanel)
        {
            preferences = stored ?? new OverlayPreferences();
            if (LaunchBinding != null) { launchHandle = LaunchBinding.Handle; launchProcess = LaunchBinding.ProcessId; }
            KavrosTheme.BlurPreview = preferences.BlurPreview;
            Text = "Kavros Clue Helper · " + AppBranding.DisplayVersion;
            Icon = AppBranding.LoadScroll();
            ClientSize = new Size(516, 1004); FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen; AutoScaleMode = AutoScaleMode.None;
            BackColor = KavrosTheme.Bg; ForeColor = KavrosTheme.Text; Font = KavrosTheme.Ui(9F, false);
            Label title = LabelAt("CLUE HELPER", 90, 27, 400, 30); title.Font = KavrosTheme.Display(16F); title.UseMnemonic = false;
            Label subtitle = LabelAt("Easy / Medium / Hard / Elite / Master / Puzzle helpers", 92, 58, 400, 18); subtitle.ForeColor = KavrosTheme.TextFaint; subtitle.Font = KavrosTheme.Ui(8.5F, false);
            connection.SetBounds(16, 84, 484, 22); connection.Font = KavrosTheme.System(8F, FontStyle.Regular); connection.ForeColor = Color.Gold; connection.Text = "Looking for the Kavros game window..."; Controls.Add(connection);
            windows.SetBounds(28, 134, 355, 28); windows.DropDownStyle = ComboBoxStyle.DropDownList; KavrosTheme.StyleCombo(windows); Controls.Add(windows);
            ButtonAt("Refresh", 393, 134, 95, RefreshWindows);
            ButtonAt("Scan now", 28, 168, 80, RequestScan, ButtonTone.Primary);
            ButtonAt("Open image", 116, 168, 110, OpenScreenshot);
            AllowDrop = true;
            DragEnter += delegate(object sender, DragEventArgs e) { e.Effect = DroppedImage(e.Data) == null ? DragDropEffects.None : DragDropEffects.Copy; };
            DragDrop += delegate(object sender, DragEventArgs e)
            {
                string path = DroppedImage(e.Data);
                if (path != null) OpenImageFile(path);
            };
            sliderTips.SetToolTip(preview, "Drop a PNG, JPEG or BMP here, or press Ctrl+V to paste a screenshot.");
            ButtonAt("Examples", 234, 168, 78, ShowExamples);
            ButtonAt("Save image", 320, 168, 84, SaveCapture);
            ButtonAt("Details", 412, 168, 76, CopyDetails);
            automatic.Text = "Auto detect & follow"; automatic.SetBounds(28, 238, 220, 24); Controls.Add(automatic);
            draw.Text = "Show overlay"; draw.Checked = true; draw.SetBounds(264, 238, 224, 24); Controls.Add(draw);
            CheckBox pin = new KavrosCheckBox { Text = "Pin panel", Left = 28, Top = 262, Width = 220, Height = 24 }; pin.CheckedChanged += delegate { TopMost = pin.Checked; }; Controls.Add(pin);
            automatic.CheckedChanged += delegate
            {
                if(offline){retainedClue.Reset();if(automatic.Checked&&offlineSource!=null){offlineSource.Dispose();offlineSource=null;}}
                offline=false;pendingKey=null;nextRead=DateTime.MinValue;CancelAnalysis();overlay.Hide();
                if(automatic.Checked)
                {
                    captureSearch.Reset();lastLocated=null;current=null;marks.Clear();requestedScan=false;
                    canvas.Puzzle=null;canvas.Invalidate();ShowMasterView(false);
                    status.Text="Automatic reading resumed. Searching the full Kavros window.";
                }
                else status.Text="Automatic reading paused. Use Scan now for one capture.";
            };
            draw.CheckedChanged += delegate { ShowGuidance(); };
            windows.SelectedIndexChanged += delegate
            {
                if(!refreshingWindows)
                {
                    offline=false;CancelAnalysis();captureSearch.Reset();lastLocated=null;current=null;pendingKey=null;marks.Clear();overlay.Hide();
                    canvas.Puzzle=null;canvas.Invalidate();ShowMasterView(false);AttachWindow();nextRead=DateTime.MinValue;
                    requestedScan=SelectedWindow!=IntPtr.Zero;
                }
            };

            detected.SetBounds(16, 304, 484, 34); detected.Font = KavrosTheme.System(12F, FontStyle.Bold); detected.ForeColor = KavrosTheme.Ok; detected.Text = "Waiting for a puzzle"; Controls.Add(detected);
            preview.SetBounds(28, 366, 460, 222); preview.SizeMode = PictureBoxSizeMode.Zoom; preview.BackColor = KavrosTheme.BgDeep; Controls.Add(preview);
            canvas.SetBounds(28, 602, 260, 235); Controls.Add(canvas);
            instructions = new PaintedLabel { Text = InstructionsText(), Left = 300, Top = 602, Width = 188, Height = 46, Renderer = StatusText.DrawLegend }; Controls.Add(instructions);
            masterView.SetBounds(28,366,460,471); masterView.Visible=false; Controls.Add(masterView);
            summary.SetBounds(16, 857, 484, 36); summary.Font = KavrosTheme.Ui(9F, true); summary.ForeColor = KavrosTheme.Ok; summary.Text = "Open a clue or puzzle and keep it visible in Kavros."; Controls.Add(summary);
            status.SetBounds(16, 893, 484, 32); status.ForeColor = KavrosTheme.TextSoft; status.Text = "Ctrl+Shift+L: scan  /  Ctrl+Shift+H: hide overlay"; Controls.Add(status);
            scanProgress.SetBounds(16,927,484,3);scanProgress.Style=ProgressBarStyle.Marquee;scanProgress.MarqueeAnimationSpeed=25;scanProgress.Visible=false;Controls.Add(scanProgress);
            reverse.Text = "Reverse lockbox cycle"; reverse.SetBounds(300, 772, 188, 24); Controls.Add(reverse);
            reverse.CheckedChanged += delegate { if (current != null && current.Kind == PuzzleKind.Lockbox) SolveAndShow(current); };
            towerCountdown.Text = "Tower countdown"; towerCountdown.SetBounds(300, 652, 188, 24); Controls.Add(towerCountdown);
            towerCountdown.CheckedChanged += delegate { instructions.Text = InstructionsText(); if (current != null && current.Kind == PuzzleKind.Towers) SolveAndShow(current); };
            SetupShortestSlider();

            assetPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
            if (!Directory.Exists(assetPath)) throw new Exception("The assets folder is missing. Extract the entire ZIP before running the program.");
            ThreadPool.QueueUserWorkItem(delegate{try{EnsureEngine();}catch(Exception){/* Scan jobs report any asset error on the panel. */}});
            compassSession=new CompassSession(assetPath);
            floatingPanel=new FloatingCluePanel(assetPath,compassSession,preferences);
            floatingPanel.SettingsRequested+=ShowSettings;
            floatingPanel.DebugRequested+=SaveDebug;
            floatingPanel.ScanRequested+=RequestScan;
            floatingPanel.PauseRequested+=delegate{automatic.Checked=!automatic.Checked;};
            floatingPanel.ExitRequested+=ExitHelper;
            floatingPanel.QuickSliderRequested += UseQuickRoute;
            floatingPanel.ClearRequested+=delegate{CancelAnalysis();retainedClue.Clear(current);compassSession.NewClue();ShowMasterView(false);masterView.SetClue(null,assetPath);canvas.Puzzle=null;canvas.Lines=null;canvas.Values=null;canvas.Invalidate();captureSearch.ForceFullWindow();nextRead=DateTime.MinValue;UpdateFloatingPanel();};
            floatingPanel.UserMoved+=delegate{Rectangle game=SelectedWindow==IntPtr.Zero?Rectangle.Empty:Native.ClientBounds(SelectedWindow);preferences.Offset=new Point(floatingPanel.Left-game.Left,floatingPanel.Top-game.Top);preferences.Save();};
            floatingEnabled.Text="Floating clue panel";floatingEnabled.SetBounds(16,939,190,26);floatingEnabled.Checked=preferences.Enabled;Controls.Add(floatingEnabled);
            floatingEnabled.CheckedChanged+=delegate{preferences.Enabled=floatingEnabled.Checked;preferences.Save();UpdateFloatingPanel();};
            ButtonAt("Open panel",232,938,128,OpenPanel);
            ButtonAt("Exit helper",372,938,128,ExitHelper);
            checkUpdates.Text="Check for updates at startup";checkUpdates.SetBounds(16,972,255,26);checkUpdates.Checked=preferences.CheckForUpdates;Controls.Add(checkUpdates);
            sliderTips.SetToolTip(checkUpdates,"When this is on, startup asks GitHub whether a newer release exists.");
            checkUpdates.CheckedChanged+=delegate{preferences.CheckForUpdates=checkUpdates.Checked;preferences.Save();};
            blurPreview.Text="Blur live preview";blurPreview.SetBounds(16,1010,255,26);blurPreview.Checked=preferences.BlurPreview;Controls.Add(blurPreview);
            blurPreview.CheckedChanged+=delegate{preferences.BlurPreview=blurPreview.Checked;KavrosTheme.BlurPreview=blurPreview.Checked;preferences.Save();PreviewStyle.Release(preview);preview.Invalidate();};
            ButtonAt("Check for updates",305,971,195,delegate{AppUpdates.Check(this,true);});
            ButtonAt("What's new",305,1012,195,ToggleWhatsNew);
            SetupClientOptions();
            int contentBottom = 0;
            foreach (Control placed in Controls) if (placed.Bottom > contentBottom) contentBottom = placed.Bottom;
            ClientSize = new Size(ClientSize.Width, contentBottom + 16);
            AppUpdates.Present=ShowToast;
            var trayMenu=new ContextMenuStrip();trayMenu.Items.Add("Open panel",null,delegate{OpenPanel();});trayMenu.Items.Add("Settings",null,delegate{ShowSettings();});trayMenu.Items.Add("Exit helper",null,delegate{ExitHelper();});
            AppBranding.ArrangeSettings(this);
            KavrosTheme.AddMainCards(this);
            closedClientHeight = ClientSize.Height;
            BuildReleaseBanner();
            if(NoTrayOnLaunch)tray=new NotifyIcon();else
            tray=new NotifyIcon{Icon=this.Icon,Text="Kavros Clue Helper",ContextMenuStrip=trayMenu,Visible=true};tray.DoubleClick+=delegate{ShowSettings();};
            Shown+=delegate{raiseUntil=DateTime.UtcNow.AddSeconds(8);if(startWithPanel&&preferences.Enabled){Hide();UpdateFloatingPanel();}if(startWithPanel&&ShowPanelOnLaunch)OpenPanel();};
            FormClosing+=delegate(object sender,FormClosingEventArgs e){if(!closing&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();OpenPanel();}};
            analysisWorker.DoWork += AnalyzeFrame;
            analysisWorker.RunWorkerCompleted += AnalysisCompleted;
            RefreshWindows();
            automatic.Checked = true;
            InitializeTracking();
            timer.Interval = 50; timer.Tick += delegate { TickReader(); }; timer.Start();
            FormClosed += delegate { closing=true; CancelAnalysis(); StopTracking(); if(trackingCancellation!=null && !trackingWorker.IsBusy){trackingCancellation.Dispose();trackingCancellation=null;} timer.Stop(); timer.Dispose(); if(notice!=null)notice.Dispose(); sliderTips.Dispose(); overlay.Dispose();floatingPanel.Dispose();tray.Visible=false;tray.Dispose();trayMenu.Dispose();if (Icon != null) Icon.Dispose(); if (preview.Image != null) preview.Image.Dispose(); if(diagnosticCapture!=null) diagnosticCapture.Dispose(); if(offlineSource!=null) offlineSource.Dispose(); Native.UnregisterHotKey(Handle, 11); Native.UnregisterHotKey(Handle, 12); };
        }
        private NoticeToast notice;
        private int closedClientHeight;
        private int notesShift;
        private bool notesOpen;
        private Panel notesHost;
        private TextBox notesBody;
        internal void ShowToast(string message, bool error, string action, Action click)
        {
            if (notice == null || notice.IsDisposed) notice = new NoticeToast();
            Rectangle game = SelectedWindow != IntPtr.Zero ? Native.ClientBounds(SelectedWindow) : Native.VirtualScreen();
            notice.Present(message, error, action, click, game, NoticeBlocked(), action == "Restart");
        }
        internal void ShowWhatsNew()
        {
            ShowSettings();
            ShowReleaseBanner();
        }
        private void BuildReleaseBanner()
        {
            notesHost = new Panel { Name = "WhatsNewBanner", BackColor = KavrosTheme.BgRaise, Visible = false };
            Label heading = new Label { Name = "WhatsNewHeading", Text = "What's new", ForeColor = KavrosTheme.Gold, BackColor = KavrosTheme.BgRaise, Font = KavrosTheme.Ui(10F, true), TextAlign = ContentAlignment.MiddleLeft };
            OpaqueButton dismiss = new OpaqueButton { Name = "WhatsNewDismiss", Text = "Got it", Tone = ButtonTone.Secondary, FlatStyle = FlatStyle.Flat, BackColor = KavrosTheme.Button, ForeColor = KavrosTheme.Text };
            dismiss.FlatAppearance.BorderColor = KavrosTheme.ButtonLine;
            dismiss.Click += delegate { DismissReleaseNotes(); };
            notesBody = new TextBox { Name = "WhatsNewBody", Multiline = true, ReadOnly = true, WordWrap = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None, HideSelection = true, TabStop = false, Font = KavrosTheme.Ui(8.5F, false), BackColor = KavrosTheme.Bg, ForeColor = KavrosTheme.Text, Text = ReleaseNotes.Text() };
            notesHost.Controls.Add(heading);
            notesHost.Controls.Add(dismiss);
            notesHost.Controls.Add(notesBody);
            Controls.Add(notesHost);
        }
        internal void ShowReleaseBanner()
        {
            notesOpen = true;
            ApplyReleaseLayout();
        }
        internal void DismissReleaseNotes()
        {
            preferences.SeenNotes = AppBranding.DisplayVersion;
            preferences.Save();
            notesOpen = false;
            ApplyReleaseLayout();
            if (floatingPanel != null && !floatingPanel.IsDisposed) floatingPanel.SyncReleaseNotes();
        }
        private void HideReleaseNotes()
        {
            notesOpen = false;
            ApplyReleaseLayout();
        }
        private void ToggleWhatsNew()
        {
            if (notesOpen) HideReleaseNotes();
            else ShowReleaseBanner();
        }
        MainSurface SettingsBody()
        {
            return Controls.OfType<MainSurface>().Single();
        }
        int ClientCardBottom(MainSurface surface)
        {
            foreach (MainSurface.Card card in surface.Cards)
                if (card.Label == "CLIENT") return card.Bounds.Bottom;
            return 0;
        }
        Rectangle ClientCardOnForm()
        {
            MainSurface surface = SettingsBody();
            foreach (MainSurface.Card card in surface.Cards)
                if (card.Label == "CLIENT")
                    return new Rectangle(surface.Left + card.Bounds.X, surface.Top + card.Bounds.Y + surface.AutoScrollPosition.Y, card.Bounds.Width, card.Bounds.Height);
            return Rectangle.Empty;
        }
        void ShiftContentBelow(MainSurface body, int cardBottom, int delta)
        {
            if (delta == 0) return;
            foreach (Control child in body.Controls)
                if (child.Top >= cardBottom) child.Top += delta;
            Size scroll = body.AutoScrollMinSize;
            body.AutoScrollMinSize = new Size(scroll.Width, Math.Max(0, scroll.Height + delta));
        }
        private void ApplyReleaseLayout()
        {
            if (notesHost == null) return;
            MainSurface body = SettingsBody();
            int cardBottom = ClientCardBottom(body);
            if (!notesOpen)
            {
                if (notesShift != 0)
                {
                    ShiftContentBelow(body, cardBottom, -notesShift);
                    notesShift = 0;
                }
                if (ClientSize.Height != closedClientHeight) ClientSize = new Size(ClientSize.Width, closedClientHeight);
                notesHost.Visible = false;
                return;
            }
            if (notesShift == 0 && cardBottom > 0)
            {
                int nextTop = int.MaxValue;
                foreach (Control child in body.Controls)
                    if (child.Top >= cardBottom && child.Top < nextTop) nextTop = child.Top;
                int following = nextTop == int.MaxValue ? WhatsNewLayout.Bottom : nextTop - cardBottom;
                notesShift = WhatsNewLayout.ClientGap + WhatsNewLayout.BannerHeight + following;
                ShiftContentBelow(body, cardBottom, notesShift);
            }
            int contentBottom = 0;
            foreach (Control child in body.Controls)
                if (child.Bottom > contentBottom) contentBottom = child.Bottom;
            Rectangle card = ClientCardOnForm();
            int bannerTop = card.IsEmpty ? closedClientHeight - AppBranding.FooterHeight : card.Bottom + WhatsNewLayout.ClientGap;
            int needed = bannerTop + WhatsNewLayout.BannerHeight + WhatsNewLayout.Bottom + AppBranding.FooterHeight;
            int contentNeeded = body.Top + contentBottom + 16 + AppBranding.FooterHeight;
            if (contentNeeded > needed) needed = contentNeeded;
            int height = Math.Max(closedClientHeight, needed);
            if (ClientSize.Height != height) ClientSize = new Size(ClientSize.Width, height);
            card = ClientCardOnForm();
            if (!card.IsEmpty) bannerTop = card.Bottom + WhatsNewLayout.ClientGap;
            int width = Math.Max(40, ClientSize.Width - WhatsNewLayout.Side * 2);
            notesHost.SetBounds(WhatsNewLayout.Side, bannerTop, width, WhatsNewLayout.BannerHeight);
            Label heading = (Label)notesHost.Controls["WhatsNewHeading"];
            Control dismiss = notesHost.Controls["WhatsNewDismiss"];
            heading.SetBounds(WhatsNewLayout.Pad, 0, Math.Max(40, width - 96), WhatsNewLayout.Header);
            dismiss.SetBounds(width - 80, 4, 72, WhatsNewLayout.Header - 8);
            Rectangle notes = WhatsNewLayout.Body(notesHost.Bounds);
            notesBody.SetBounds(notes.X, notes.Y, notes.Width, notes.Height);
            notesHost.Visible = true;
            notesHost.BringToFront();
            notesBody.SelectionStart = 0;
            notesBody.SelectionLength = 0;
        }
        private void ShowSettings()
        {
            if(WindowState==FormWindowState.Minimized)WindowState=FormWindowState.Normal;
            // The panel is topmost. Drop it, without activating it, so this window can come forward.
            if(floatingPanel!=null&&!floatingPanel.IsDisposed)floatingPanel.PlaceTopMost(false);
            Show();Activate();BringToFront();
        }
        internal void CheckForUpdatesOnStartup(){if(preferences.CheckForUpdates)AppUpdates.Check(this,false);}
        internal void PrepareForUpdate(){closing=true;}
        internal void SetUpdateStatus(string message){status.Text=message;}
        private void OpenPanel(){draw.Checked=true;floatingEnabled.Checked=true;UpdateFloatingPanel();}
        private void ExitHelper(){closing=true;Close();}
        private bool trackedVisible;
        private void UpdateFloatingPanel()
        {
            if(closing||floatingPanel==null||floatingPanel.IsDisposed)return;
            bool exists=SelectedWindow!=IntPtr.Zero&&Native.IsWindow(SelectedWindow);
            // Owner is only the client this helper is reading. Dragging onto another
            // client does not move the owner.
            IntPtr owner=exists?SelectedWindow:IntPtr.Zero;
            if(floatingPanel.IsHandleCreated&&owner!=panelOwner)try{Native.SetOwner(floatingPanel.Handle,owner);panelOwner=owner;}catch(System.DllNotFoundException){}catch(EntryPointNotFoundException){}
            if(overlay.IsHandleCreated&&owner!=overlayOwner)try{Native.SetOwner(overlay.Handle,owner);overlayOwner=owner;}catch(System.DllNotFoundException){}catch(EntryPointNotFoundException){}
            bool minimised=exists&&Native.IsIconic(SelectedWindow);
            bool foreground=exists&&WindowLocator.IsForeground(SelectedWindow);
            IntPtr front=IntPtr.Zero;
            try{front=Native.GetForegroundWindow();}catch(System.DllNotFoundException){}catch(EntryPointNotFoundException){}
            bool settingsFront=front!=IntPtr.Zero&&(front==Handle||(IsHandleCreated&&Native.GetAncestor(front,2)==Handle));
            bool panelFront=floatingPanel.IsHandleCreated&&(front==floatingPanel.Handle||floatingPanel.ContainsFocus);
            bool overlayFront=overlay!=null&&overlay.IsHandleCreated&&front==overlay.Handle;
            Rectangle game=exists?Native.ClientBounds(SelectedWindow):Rectangle.Empty;
            if(exists)floatingPanel.SetAnchor(game);
            bool covered=false;
            if(exists&&!minimised&&(foreground||panelFront)&&game.Width>0&&game.Height>0)
            {
                try{covered=!GameVisibility.Clear(game,delegate(Point point){return WindowLocator.BelongsTo(SelectedWindow,point,OwnSurfaces());});}
                catch(System.DllNotFoundException){}catch(EntryPointNotFoundException){}
            }
            trackedVisible=FloatingPanelLayout.Shown(draw.Checked&&floatingEnabled.Checked,offline,exists,minimised,foreground,panelFront||overlayFront,settingsFront,covered);
            string clientName=ClientLabel();
            floatingPanel.SetClient(clientName);
            ShowBoundClient(clientName);
            if(!trackedVisible)
            {
                if(floatingPanel.Visible)floatingPanel.Hide();
                if(overlay!=null&&overlay.Visible)overlay.Hide();
                panelTopKnown=false;overlayTopKnown=false;return;
            }
            floatingPanel.SetPaused(!automatic.Checked);
            DetectedPuzzle displayed=retainedClue.Display(current);
            bool saved = current == null && displayed != null;
            string message = displayed != null ? summary.Text : exists ? "Open a clue or puzzle in Kavros." : "Waiting for Kavros.";
            string stage = busy && displayed == null ? analysisStage : null;
            if (displayed != panelPuzzle || message != panelText || saved != panelSaved || stage != panelStage)
            {
                if (displayed != null) floatingPanel.SetPuzzle(displayed, message);
                else floatingPanel.SetWaiting(message);
                floatingPanel.SetRetained(saved);
                floatingPanel.SetReading(stage);
                scanProgress.Visible = stage != null;
                panelPuzzle = displayed; panelText = message; panelSaved = saved; panelStage = stage;
            }
            floatingPanel.RefreshCompass(false);
            Rectangle desktop=exists?Screen.FromRectangle(game).WorkingArea:Screen.PrimaryScreen.WorkingArea;
            Rectangle placement=FloatingPanelLayout.Place(game,floatingPanel.Size,preferences.Offset,desktop);
            if(!floatingPanel.IsDragging&&floatingPanel.Bounds!=placement)floatingPanel.Bounds=placement;
            if(!floatingPanel.Visible)floatingPanel.Show();
            KeepSurfacesOnTop(exists,minimised,foreground||panelFront);
        }
        private string ClientLabel()
        {
            return WindowLocator.BoundName(attachedWindow, WindowLocator.Clients(availableWindows));
        }
        // The Auto row, the Game found line, and the panel header all use clientName.
        // Rewriting the combo item only when that text changes keeps the list from flickering.
        private void ShowBoundClient(string clientName)
        {
            if(attachedWindow!=null)
            {
                string line=CaptureLabels.Found(clientName);
                if(connection.Text!=line)
                {
                    connection.ForeColor=Color.FromArgb(87, 220, 181);
                    connection.Text=line;
                }
            }
            if(windows.DroppedDown||windows.Items.Count==0)return;
            string caption=CaptureLabels.Auto(clientName);
            GameWindow entry=windows.Items[0] as GameWindow;
            if(entry==null||!entry.Automatic)return;
            if(entry.ToString()==caption&&autoCaption==caption)return;
            autoCaption=caption;
            entry.Display=caption;
            int index=windows.SelectedIndex;
            refreshingWindows=true;
            try
            {
                windows.Items[0]=entry;
                if(index>=0&&index<windows.Items.Count&&windows.SelectedIndex!=index)windows.SelectedIndex=index;
            }
            finally{refreshingWindows=false;}
        }
        private IntPtr[] OwnSurfaces()
        {
            IntPtr panel=floatingPanel!=null&&floatingPanel.IsHandleCreated?floatingPanel.Handle:IntPtr.Zero;
            IntPtr marks=overlay!=null&&overlay.IsHandleCreated?overlay.Handle:IntPtr.Zero;
            IntPtr toast=notice!=null&&notice.IsHandleCreated?notice.Handle:IntPtr.Zero;
            return new IntPtr[]{panel,marks,toast};
        }
        // Keep the panel and puzzle overlays above the game without taking focus.
        // Topmost is set only while the game or a Kavros helper is in front, and for a
        // few seconds after launch, then they go back under whatever application the
        // player switched to. Settings in front is not treated as a helper, so the
        // panel cannot cover it. Only a change is sent.
        private void KeepSurfacesOnTop(bool exists,bool minimised,bool gameForeground)
        {
            IntPtr owner=exists?SelectedWindow:IntPtr.Zero;
            if(floatingPanel!=null&&floatingPanel.IsHandleCreated&&owner!=panelOwner)try{Native.SetOwner(floatingPanel.Handle,owner);panelOwner=owner;}catch(System.DllNotFoundException){}catch(EntryPointNotFoundException){}
            if(overlay!=null&&overlay.IsHandleCreated&&owner!=overlayOwner)try{Native.SetOwner(overlay.Handle,owner);overlayOwner=owner;}catch(System.DllNotFoundException){}catch(EntryPointNotFoundException){}
            IntPtr front=Native.GetForegroundWindow();
            bool settingsFront=front!=IntPtr.Zero&&(front==Handle||(IsHandleCreated&&Native.GetAncestor(front,2)==Handle));
            bool panelFront=floatingPanel!=null&&floatingPanel.IsHandleCreated&&front==floatingPanel.Handle;
            bool overlayFront=overlay!=null&&overlay.IsHandleCreated&&front==overlay.Handle;
            bool helperFront=!settingsFront&&WindowLocator.IsHelperWindow(front);
            bool want=FloatingPanelLayout.WantTopMost(exists,minimised,gameForeground,helperFront,panelFront||overlayFront,settingsFront,DateTime.UtcNow<raiseUntil);
            // Do not tuck the panel under whichever window is in front. That window
            // may be a different Kavros client.
            IntPtr tuck=IntPtr.Zero;
            if(floatingPanel!=null&&floatingPanel.IsHandleCreated&&floatingPanel.Visible)
                ApplySurfaceOrder(floatingPanel.Handle,want,tuck,ref panelTop,ref panelTopKnown);
            else panelTopKnown=false;
            if(overlay!=null&&overlay.IsHandleCreated&&overlay.Visible)
                ApplySurfaceOrder(overlay.Handle,want,tuck,ref overlayTop,ref overlayTopKnown);
            else overlayTopKnown=false;
        }
        private void ApplySurfaceOrder(IntPtr window,bool want,IntPtr tuck,ref bool top,ref bool known)
        {
            if(known&&want==top&&tuck==orderTuck)return;
            try{Native.ApplyZOrderNoActivate(window,want,tuck);}
            catch(System.DllNotFoundException){return;}
            catch(EntryPointNotFoundException){return;}
            top=want;known=true;orderTuck=tuck;
        }
        private void ShowMasterView(bool show)
        {
            masterView.Visible=show;
            preview.Visible=canvas.Visible=instructions.Visible=reverse.Visible=towerCountdown.Visible=!show;
        }
        private string InstructionsText()
        {
            return "Automatic detection\n\nKeep the full puzzle visible.";
        }
        private Label LabelAt(string text, int x, int y, int w, int h) { Label l = new Label { Text = text, Left = x, Top = y, Width = w, Height = h }; Controls.Add(l); return l; }
        private void SetupClientOptions()
        {
            // The heading is drawn 9px into the Client card. This label starts
            // a full line below that, so the heading is not covered.
            Label interfaceLabel = LabelAt("Interface", 28, 1088, 200, 22);
            interfaceLabel.Name = "InterfaceLabel";
            interfaceLabel.ForeColor = KavrosTheme.TextSoft;
            interfaceLabel.BackColor = KavrosTheme.Panel;
            modernInterface.Name = "ModernInterface";
            modernInterface.Text = "Modern / DarkScape";
            modernInterface.SetBounds(28, 1114, 214, 32);
            modernInterface.Font = KavrosTheme.Ui(9F, true);
            legacyInterface.Name = "LegacyInterface";
            legacyInterface.Text = "Legacy";
            legacyInterface.SetBounds(254, 1114, 214, 32);
            legacyInterface.Font = KavrosTheme.Ui(9F, true);
            Controls.Add(modernInterface);
            Controls.Add(legacyInterface);
            modernInterface.CheckedChanged += delegate { if (modernInterface.Checked) ChooseInterface(false); };
            legacyInterface.CheckedChanged += delegate { if (legacyInterface.Checked) ChooseInterface(true); };
            choosingInterface = true;
            modernInterface.Checked = !preferences.LegacyInterface;
            legacyInterface.Checked = preferences.LegacyInterface;
            choosingInterface = false;
            // Below the Client card, on the same flat background as the other
            // settings rows. The dropdown styling is unchanged.
            Label resolutionLabel = LabelAt("Resolution", 28, 1178, 88, 22);
            resolutionLabel.Name = "ResolutionLabel";
            resolutionLabel.ForeColor = KavrosTheme.TextSoft;
            resolutionLabel.BackColor = KavrosTheme.Bg;
            resolutionChoice.Name = "ResolutionChoice";
            resolutionChoice.DropDownStyle = ComboBoxStyle.DropDownList;
            resolutionChoice.SetBounds(118, 1174, 150, 28);
            resolutionChoice.Items.AddRange(new object[] { "Auto", "1080p", "1440p", "4K" });
            KavrosTheme.StyleCombo(resolutionChoice);
            Controls.Add(resolutionChoice);
            Label scalingLabel = LabelAt("Scaling", 280, 1178, 64, 22);
            scalingLabel.Name = "ScalingLabel";
            scalingLabel.ForeColor = KavrosTheme.TextSoft;
            scalingLabel.BackColor = KavrosTheme.Bg;
            scalingChoice.Name = "ScalingChoice";
            scalingChoice.DropDownStyle = ComboBoxStyle.DropDownList;
            scalingChoice.SetBounds(348, 1174, 140, 28);
            scalingChoice.Items.AddRange(new object[] { "Auto", "100%", "125%", "150%", "200%" });
            KavrosTheme.StyleCombo(scalingChoice);
            Controls.Add(scalingChoice);
            resolutionChoice.SelectedIndex = ResolutionIndex(preferences.Resolution);
            scalingChoice.SelectedIndex = ScalingIndex(preferences.Scaling);
            resolutionChoice.SelectedIndexChanged += delegate { if (!refreshingClient) SaveResolution(); };
            scalingChoice.SelectedIndexChanged += delegate { if (!refreshingClient) SaveScaling(); };
            RefreshClientLabels();
        }
        private void ChooseInterface(bool legacy)
        {
            if (choosingInterface) return;
            choosingInterface = true;
            modernInterface.Checked = !legacy;
            legacyInterface.Checked = legacy;
            choosingInterface = false;
            if (preferences.LegacyInterface == legacy) return;
            preferences.LegacyInterface = legacy;
            preferences.Save();
            if (positionReader != null) positionReader.UseInterface(legacy);
        }
        private static int ResolutionValue(int index)
        {
            if (index == 1) return 1080;
            if (index == 2) return 1440;
            if (index == 3) return 2160;
            return 0;
        }
        private static int ResolutionIndex(int value)
        {
            if (value == 1080) return 1;
            if (value == 1440) return 2;
            if (value == 2160) return 3;
            return 0;
        }
        private static int ScalingValue(int index)
        {
            if (index == 1) return 100;
            if (index == 2) return 125;
            if (index == 3) return 150;
            if (index == 4) return 200;
            return 0;
        }
        private static int ScalingIndex(int value)
        {
            if (value == 100) return 1;
            if (value == 125) return 2;
            if (value == 150) return 3;
            if (value == 200) return 4;
            return 0;
        }
        private void SaveResolution()
        {
            int choice = ResolutionValue(resolutionChoice.SelectedIndex);
            if (choice == 1080 || choice == 1440 || choice == 2160) preferences.ResolutionManual = choice;
            preferences.Resolution = choice;
            preferences.Save();
            RefreshClientLabels();
        }
        private void SaveScaling()
        {
            int choice = ScalingValue(scalingChoice.SelectedIndex);
            if (choice == 100 || choice == 125 || choice == 150 || choice == 200) preferences.ScalingManual = choice;
            preferences.Scaling = choice;
            preferences.Save();
            RefreshClientLabels();
        }
        private void RefreshClientLabels()
        {
            if (resolutionChoice.DroppedDown || scalingChoice.DroppedDown) return;
            int height = 0, dpi = 0;
            if (SelectedWindow != IntPtr.Zero)
            {
                Rectangle bounds = Native.ClientBounds(SelectedWindow);
                if (bounds.Width >= 60 && bounds.Height >= 60) height = bounds.Height;
                try { uint raw = Native.GetDpiForWindow(SelectedWindow); if (raw >= 96) dpi = (int)raw; }
                catch (DllNotFoundException) { }
                catch (EntryPointNotFoundException) { }
            }
            string res = height > 0 ? PuzzleDetector.ResolutionName(PuzzleDetector.ResolutionBucket(height)) : PuzzleDetector.ResolutionName(preferences.ResolutionManual);
            string scale = dpi > 0 ? PuzzleDetector.ScalingPercent(dpi) + "%" : preferences.ScalingManual + "%";
            refreshingClient = true;
            try
            {
                SetAutoItem(resolutionChoice, "Auto (" + res + ")");
                SetAutoItem(scalingChoice, "Auto (" + scale + ")");
            }
            finally { refreshingClient = false; }
        }
        private static void SetAutoItem(ComboBox box, string text)
        {
            if (box.Items.Count == 0 || (string)box.Items[0] == text) return;
            int selected = box.SelectedIndex;
            box.Items[0] = text;
            if (selected >= 0 && box.SelectedIndex != selected) box.SelectedIndex = selected;
        }
        private void ButtonAt(string text, int x, int y, int w, Action action, ButtonTone tone = ButtonTone.Secondary)
        {
            Button b = new OpaqueButton { Text = text, Left = x, Top = y, Width = w, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = KavrosTheme.Button, ForeColor = ForeColor, Tone = tone }; b.FlatAppearance.BorderColor = KavrosTheme.ButtonLine;
            b.Click += delegate { try { action(); } catch (Exception ex) { overlay.Hide(); status.Text = ex.Message; } }; Controls.Add(b);
        }
        private IntPtr SelectedWindow { get { return attachedWindow == null ? IntPtr.Zero : attachedWindow.Handle; } }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            PublishBinding(launchHandle, launchProcess);
            KavrosTheme.ApplyWindowChrome(Handle);
            bool scanKey = Native.RegisterHotKey(Handle, 11, 0x4006, (uint)Keys.L);
            bool drawKey = Native.RegisterHotKey(Handle, 12, 0x4006, (uint)Keys.H);
            if (!scanKey || !drawKey) status.Text = "Ctrl+Shift+" + (!scanKey ? "L" : "H") + " is taken by another program (or another copy of the helper). Use the buttons instead.";
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.NoticeMessage) { try { ShowToast("Already running. Check the tray.", false, null, null); } catch (Exception ex) { NoteFailure(ex); } return; }
            if (m.Msg == Native.ShowPanelMessage) { try { OpenPanel(); } catch (Exception ex) { NoteFailure(ex); } return; }
            if (m.Msg == 0x312) { try { if (m.WParam.ToInt32() == 11) RequestScan(); if (m.WParam.ToInt32() == 12) draw.Checked = !draw.Checked; } catch (Exception ex) { NoteFailure(ex); } }
            base.WndProc(ref m);
        }
        private void RefreshWindows()
        {
            try { RefreshWindowList(); }
            catch (EntryPointNotFoundException) { }
            catch (DllNotFoundException) { }
        }
        private void RefreshWindowList()
        {
            GameWindow previousChoice = windows.SelectedItem as GameWindow;
            bool manual = previousChoice != null && !previousChoice.Automatic;
            IntPtr selected = manual ? previousChoice.Handle : IntPtr.Zero;
            availableWindows = WindowLocator.Enumerate(Process.GetCurrentProcess().Id);
            refreshingWindows = true;
            windows.BeginUpdate();
            try
            {
                windows.Items.Clear();
                windows.Items.Add(new GameWindow { Automatic = true, Title = "Auto - searching..." });
                List<GameWindow> clients = WindowLocator.Clients(availableWindows);
                WindowLocator.NameClients(clients);
                foreach (GameWindow window in clients) windows.Items.Add(window);
                int index = manual ? clients.FindIndex(w => w.Handle == selected) : -1;
                windows.SelectedIndex = index < 0 ? 0 : index + 1;
            }
            finally { windows.EndUpdate(); refreshingWindows = false; }
            AttachWindow();
            nextDiscovery = DateTime.UtcNow.AddSeconds(2.5);
        }
        private void AttachWindow()
        {
            GameWindow choice = windows.SelectedItem as GameWindow;
            IntPtr boundHandle = SelectedWindow != IntPtr.Zero ? SelectedWindow : launchHandle;
            uint boundProcess = attachedWindow != null ? attachedWindow.ProcessId : launchProcess;
            GameWindow next = choice != null && !choice.Automatic && Native.IsWindow(choice.Handle) ? choice : WindowLocator.Choose(availableWindows, boundHandle, boundProcess, Native.GetForegroundWindow(), ClientLaunch.ClaimedHandles(availableWindows));
            IntPtr previous = SelectedWindow;
            attachedWindow = next;
            if (next != null) { launchHandle = next.Handle; launchProcess = next.ProcessId; PublishBinding(next.Handle, next.ProcessId); }
            if (SelectedWindow != previous)
            {
                if(compassSession!=null)compassSession.NewClue();
                retainedClue.Reset();
                CancelAnalysis();
                captureSearch.Reset(); nextRead=DateTime.MinValue;
                lastLocated = null; current = null; pendingKey = null; knotHistory = null; slidingMoves = null; slidingBoard = null; marks.Clear();
                canvas.Puzzle = null; canvas.Values = null; canvas.Invalidate(); overlay.Hide();
                ShowMasterView(false); masterView.SetClue(null,assetPath);
                captureDetails = "No capture from this window yet."; puzzleDetails = "No puzzle detected yet.";
            }
            if (next == null)
            {
                bool launcher = availableWindows.Any(w => WindowLocator.IsLauncher(w) && ((w.Title + " " + w.ProcessName).IndexOf("kavros", StringComparison.OrdinalIgnoreCase) >= 0));
                connection.ForeColor = Color.Gold; connection.Text = launcher ? "Kavros launcher found - waiting for the game client." : "Looking for Kavros - it can be opened after this helper.";
                if (!offline) { detected.Text = "Waiting for Kavros"; status.Text = "The window list refreshes automatically. You can also pick a window manually."; }
            }
            ShowBoundClient(ClientLabel());
        }
        private DateTime nextFailureLog;
        private void NoteFailure(Exception ex)
        {
            if (DateTime.UtcNow >= nextFailureLog) { CrashLog.Write(ex); nextFailureLog = DateTime.UtcNow.AddSeconds(5); }
            if (!closing && !IsDisposed) status.Text = "Could not use that reading. It will try again.";
            nextRead = DateTime.UtcNow.AddMilliseconds(500);
        }
        private void TickReader()
        {
            try
            {
            RefreshClientLabels();
            FollowWindow();
            ReleaseClosedScan();
            ShowGuidance();
            PollShortestSlider();
            TickTracking();
            bool chosenAlive=SelectedWindow!=IntPtr.Zero&&Native.IsWindow(SelectedWindow);
            bool handleLost=attachedWindow!=null&&!chosenAlive;
            if (!windows.DroppedDown && WindowLocator.ShouldEnumerate(chosenAlive, DateTime.UtcNow>=nextDiscovery, handleLost)) RefreshWindows();
            if (busy || DateTime.UtcNow < nextRead)
            {
                // Do not read the worker's bitmap and do not capture the
                // window on this tick. An empty idle look can yield before
                // the slow text search so the next capture sees a new scroll.
                if(busy && current==null && lastLocated==null && automatic.Checked && !offline && IdleFrame.Sheet==0 && activeJob!=null && (DateTime.UtcNow-activeJob.CapturedAt).TotalMilliseconds>800)
                    IdleFrame.Yield=true;
                return;
            }
            if (requestedScan)
            {
                if (SelectedWindow == IntPtr.Zero) { overlay.Hide(); status.Text = "Waiting for Kavros to reopen before the requested scan."; }
                else { Scan(false); }
            }
            else if (automatic.Checked && !offline) Scan(true);
            }
            catch (Exception ex) { NoteFailure(ex); }
            finally
            {
                try
                {
                    if (notice != null && notice.Visible)
                    {
                        Rectangle game = SelectedWindow != IntPtr.Zero ? Native.ClientBounds(SelectedWindow) : Native.VirtualScreen();
                        notice.MoveClear(game, NoticeBlocked());
                    }
                    int interval = IdleTiming.Interval(SelectedWindow != IntPtr.Zero, overlay.Visible, busy);
                    if (timer.Interval != interval) timer.Interval = interval;
                }
                catch (Exception) { }
            }
        }
        private void RequestScan()
        {
            CancelAnalysis(); nextRead=DateTime.MinValue;
            if(analysisEngine!=null)analysisEngine.Reset();
            bool keepRegion=lastLocated!=null&&DetectionPolicy.IsBoardPuzzle(lastLocated.Kind);
            if(!keepRegion){captureSearch.ForceFullWindow();lastLocated=null;}
            current=null;marks.Clear();canvas.Puzzle=null;canvas.Invalidate();ShowMasterView(false);
            knotHistory=null;slidingMoves=null;slidingBoard=null;slidingTheme=null;slidingBatchNumber=1;
            if (offlineSource != null) { offlineSource.Dispose(); offlineSource = null; }
            offline = false; RefreshWindows();

            requestedScan = true; pendingKey = null; overlay.Hide();
            status.Text = "Reading the visible Kavros window...";
            if(!busy) Scan(false);
        }
        private void CopyDetails()
        {
            RefreshWindows();
            string report = WindowLocator.Describe(availableWindows, attachedWindow) + "Automatic reading: " + automatic.Checked + "\r\nScreenshot mode: " + offline + "\r\nCapture: " + captureDetails + "\r\nPuzzle: " + puzzleDetails + "\r\nState: " + status.Text+"\r\nMinimap: "+(positionReader==null?"Not started":positionReader.Diagnostics);
            Clipboard.SetText(report); status.Text = "Troubleshooting details copied. Paste them into our chat.";
        }
        private void Scan(bool stable)
        {
            if (busy) return;
            IntPtr window = SelectedWindow;
            if (window == IntPtr.Zero || !Native.IsWindow(window)) { overlay.Hide(); status.Text = "Waiting for Kavros. The game window will be selected automatically."; return; }
            if (Native.IsIconic(window)) { overlay.Hide(); status.Text = "Game found, but minimized. Restore Kavros to read the puzzle."; return; }
            Rectangle bounds = Native.ClientBounds(window);
            Rectangle visible = CaptureSearch.VisibleClient(bounds, Native.VirtualScreen());
            if (visible.Width < 60 || visible.Height < 60) { overlay.Hide(); status.Text = "Bring the clue into view to capture it."; return; }
            Rectangle visibleScreen = visible; visibleScreen.Offset(bounds.Location);
            IntPtr overlayHandle=overlay.IsHandleCreated ? overlay.Handle : IntPtr.Zero;
            if(!GameVisibility.Any(visibleScreen,p=>WindowLocator.AtPoint(window,p,overlayHandle)))
            { overlay.Hide(); status.Text="Kavros is covered by another window. Keep the puzzle visible."; nextRead=DateTime.UtcNow.AddMilliseconds(300); return; }
            try
            {
                Rectangle fullClient=new Rectangle(Point.Empty,bounds.Size);
                bool followSheet=lastLocated!=null&&(DetectionPolicy.IsBoardPuzzle(lastLocated.Kind)||lastLocated.Kind==PuzzleKind.Compass||lastLocated.Kind==PuzzleKind.Scan);
                Rectangle captureArea=captureSearch.Choose(bounds.Size,followSheet?lastLocated.Bounds:towerHint);
                towerHint=null;
                if(lastLocated!=null&&lastLocated.Kind==PuzzleKind.Sliding&&captureArea!=fullClient)captureArea=Rectangle.Intersect(fullClient,Rectangle.Inflate(captureArea,20,20));
                captureArea = Rectangle.Intersect(captureArea, visible);
                if (captureArea.Width < 60 || captureArea.Height < 60) captureArea = visible;
                Rectangle screenArea=captureArea; screenArea.Offset(bounds.Location);
                Bitmap screenshot=ScreenReader.Capture(screenArea);
                GuideInk guide=GuideInk.ForSlider(screenshot.Width, screenshot.Height, marks, captureArea.Location);
                ScreenReader.Cover(screenshot, screenArea, CaptureInk(bounds.Location, screenArea));
                // Never detect a puzzle in this helper's own screenshot preview
                // when the panel is sitting above the game.
                foreach(Form surface in new Form[]{this,floatingPanel})
                {
                    if(surface==null||!surface.Visible)continue;
                    Rectangle panelOverlap=Rectangle.Intersect(surface.Bounds,screenArea);
                    if(panelOverlap.Width>0 && panelOverlap.Height>0 && GameVisibility.Any(panelOverlap,p=>WindowLocator.AtPoint(surface.Handle,p,IntPtr.Zero)))
                    {
                        Rectangle cover=panelOverlap; cover.Offset(-screenArea.X,-screenArea.Y);
                        using(Graphics graphics=Graphics.FromImage(screenshot)) graphics.FillRectangle(Brushes.Black,cover);
                    }
                }
                bool cropped=captureArea!=visible;
                captureDetails=(cropped?"Puzzle region: ":"Full game window: ")+screenshot.Width+" x "+screenshot.Height+" pixels at "+DateTime.Now.ToString("HH:mm:ss")+"; offset "+captureArea.Location+"; background analysis";
                status.Text=cropped ? "Reading the current puzzle area..." : "Searching the full Kavros window...";
                requestedScan=false;
                int dpi=96;
                try{uint raw=Native.GetDpiForWindow(window);if(raw>=96)dpi=(int)raw;}
                catch(DllNotFoundException){}
                catch(EntryPointNotFoundException){}
                QueueAnalysis(new AnalysisJob { Image=screenshot,Generation=sourceGeneration,Window=window,CaptureOffset=captureArea.Location,Cropped=cropped,ClientSize=bounds.Size,CapturedAt=DateTime.UtcNow,Stable=stable,Offline=false,Reverse=reverse.Checked,WholeRows=true,Dpi=dpi,Guide=guide,LegacyInterface=preferences.LegacyInterface,Resolution=preferences.Resolution,Scaling=preferences.Scaling,ResolutionManual=preferences.ResolutionManual,ScalingManual=preferences.ScalingManual,ClientHeight=bounds.Height });
                ShowGuidance();
            }
            catch (Exception ex) { ClearUnreadableFrame(); status.Text = ex.Message; }
        }
        private void ClearUnreadableFrame()
        {
            current=null;lastLocated=null;marks.Clear();overlay.Hide();captureSearch.ForceFullWindow();
            if(compassSession!=null)compassSession.Observe(null,DateTime.UtcNow);
        }
        private void CancelAnalysis()
        {
            waitingSlider = null;
            quickForThisSlider = false;
            if (analysisEngine != null) analysisEngine.Proofs.Cancel();
            sourceGeneration++;
            analysisStage=null;
            if(compassSession!=null)compassSession.Observe(null,DateTime.UtcNow);
            if(analysisCancellation!=null) analysisCancellation.Cancel();
            if(pendingJob!=null) { pendingJob.Image.Dispose(); pendingJob=null; }
        }
        private void QueueAnalysis(AnalysisJob job)
        {
            if(closing) { job.Image.Dispose(); return; }
            if(analysisWorker.IsBusy)
            {
                if(analysisCancellation!=null) analysisCancellation.Cancel();
                if(pendingJob!=null) pendingJob.Image.Dispose(); pendingJob=job; return;
            }
            busy=true;analysisStage=null; activeJob=job; analysisCancellation=new CancellationTokenSource();
            analysisWorker.RunWorkerAsync(job);
        }
        private void AnalyzeFrame(object sender, DoWorkEventArgs args)
        {
            AnalysisJob job=(AnalysisJob)args.Argument;
            ThreadPriority priority=Thread.CurrentThread.Priority;
            bool retained=false;
            try
            {
                try { Thread.CurrentThread.Priority=ThreadPriority.BelowNormal; } catch { }
                if(analysisEngine==null)analysisStage="Preparing reader...";
                PuzzleAnalysisEngine engine=EnsureEngine();
                engine.Progress=message=>analysisStage=message;
                analysisStage=null;
                engine.UseClient(job.LegacyInterface, job.Resolution, job.Scaling, job.ResolutionManual, job.ScalingManual, job.ClientHeight);
                AnalysisResult result=engine.Analyze(job.Image,job.Generation,job.Reverse,analysisCancellation.Token,!job.Offline,job.WholeRows, job.Offline ? (Point?)null : job.CaptureOffset, job.Dpi, job.Guide);
                if(result.Plan==null || (result.Plan.Puzzle.Kind==PuzzleKind.Towers && result.Plan.Towers==null))
                { result.DiagnosticImage=job.Image; retained=true; }
                args.Result=result;
            }
            catch(OperationCanceledException) { args.Cancel=true; }
            catch(Exception ex)
            {
                CrashLog.Write(ex);
                // Leave Result empty. Completion keeps the last good read.
            }
            finally { if(!retained) job.Image.Dispose(); try { Thread.CurrentThread.Priority=priority; } catch { } }
        }
        private PuzzleAnalysisEngine EnsureEngine(){lock(engineGate){if(analysisEngine==null)analysisEngine=new PuzzleAnalysisEngine(assetPath);return analysisEngine;}}
        private void AnalysisCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            AnalysisJob job=activeJob; activeJob=null; busy=false;analysisStage=null;
            if(analysisCancellation!=null) { analysisCancellation.Dispose(); analysisCancellation=null; }
            AnalysisResult result=!args.Cancelled && args.Error==null ? args.Result as AnalysisResult : null;
            try
            {
                if(closing || IsDisposed || job==null || job.Generation!=sourceGeneration) return;
                if(args.Error!=null)
                {
                    CrashLog.Write(args.Error);
                    nextRead=DateTime.UtcNow.AddMilliseconds(250);
                    if(current==null && lastLocated==null) status.Text="Could not read this frame: "+args.Error.Message;
                    return;
                }
                // A cancelled or failed look must not clear a read that already landed.
                if(result==null) { nextRead=DateTime.UtcNow.AddMilliseconds(current==null && lastLocated==null ? 0 : 250); return; }
                if(diagnosticCapture!=null) { diagnosticCapture.Dispose(); diagnosticCapture=null; }
                diagnosticCapture=result.DiagnosticImage; result.DiagnosticImage=null;
                int wait=result.Plan==null?75:125;
                if(result.Plan!=null&&result.Plan.Puzzle!=null&&result.Plan.Puzzle.Kind==PuzzleKind.Knot)wait=40;
                if(result.Plan!=null&&result.Plan.Puzzle!=null&&result.Plan.Puzzle.Kind==PuzzleKind.Lockbox)wait=LockboxStep.RereadMilliseconds;
                if(result.Plan==null && current==null && lastLocated==null) wait=0;
                nextRead=job.CapturedAt.AddMilliseconds(wait);
                captureDetails += "; last analysis "+result.Milliseconds+" ms"+(result.Unchanged?"; cached":"");
                if(result.SearchWindow&&!job.Offline)
                {
                    captureSearch.ForceFullWindow();requestedScan=true;nextRead=DateTime.MinValue;
                    status.Text="Searching the full Kavros window...";
                    if(current!=null)ShowGuidance();
                    return;
                }
                if(result.Pending&&current!=null&&current.Kind==PuzzleKind.Sliding)
                {
                    sliderRereadMisses++;
                    nextRead=DateTime.UtcNow.AddMilliseconds(75);
                    status.Text=sliderRereadMisses>=3?"Can't read the tiles - move the mouse off the puzzle":"Waiting for the tiles to settle...";
                    ShowGuidance();
                    return;
                }
                if(result.Pending&&current!=null&&DetectionPolicy.IsBoardPuzzle(current.Kind)&&current.Kind!=PuzzleKind.Knot&&current.Kind!=PuzzleKind.Sliding)
                {status.Text="Puzzle covered. Keeping the last reading.";nextRead=DateTime.UtcNow.AddMilliseconds(120);ShowGuidance();return;}
                if(result.Pending&&current!=null&&current.Kind==PuzzleKind.Knot)
                {
                    // A missed frame is not a turn. Keep the counts that are already up.
                    status.Text="Reading the knot again.";nextRead=DateTime.UtcNow.AddMilliseconds(40);ShowGuidance();return;
                }
                if(!job.Offline&&result.Plan==null&&result.TowerGrid.HasValue)
                {
                    // The towers grid is open but its edge clues are still
                    // appearing. Re-read just that area straight away.
                    Rectangle hint=result.TowerGrid.Value;hint.Offset(job.CaptureOffset);
                    towerHint=hint;nextRead=DateTime.MinValue;return;
                }
                if(!job.Offline && job.Cropped && !DetectionPolicy.CanTrack(result.Plan))
                {
                    bool confidentMiss=result.Plan==null;
                    if(confidentMiss&&captureSearch.Complete(true,false))
                    {
                        compassSession.Observe(null,DateTime.UtcNow);
                        lastLocated=null;current=null;pendingKey=null;marks.Clear();canvas.Puzzle=null;canvas.Invalidate();ShowMasterView(false);overlay.Hide();
                        captureSearch.ForceFullWindow();requestedScan=true;nextRead=DateTime.MinValue;
                        detected.Text="Rechecking the full game window";
                        status.Text="The puzzle left its area. Searching the whole window now.";
                        return;
                    }
                    if(lastLocated!=null&&DetectionPolicy.IsBoardPuzzle(lastLocated.Kind))
                    {
                        status.Text="Reading the puzzle area again.";
                        nextRead=DateTime.UtcNow.AddMilliseconds(80);
                        if(current!=null)ShowGuidance();
                        return;
                    }
                }
                if(!job.Offline && result.Plan!=null)
                {
                    Rectangle client=Native.ClientBounds(job.Window);
                    Rectangle area=result.Plan.Puzzle.Bounds;area.Offset(job.CaptureOffset);area.Offset(client.Location);
                    IntPtr visibleOverlay=overlay.IsHandleCreated ? overlay.Handle : IntPtr.Zero;
                    if(client.Size!=job.ClientSize)
                    {
                        compassSession.Observe(null,DateTime.UtcNow);
                        lastLocated=null;current=null;marks.Clear();overlay.Hide();captureSearch.ForceFullWindow();requestedScan=true;nextRead=DateTime.MinValue;
                        detected.Text="Rechecking the resized game window";status.Text="Window size changed. Recalculating the capture area.";return;
                    }
                    if(!GameVisibility.Clear(area,point=>WindowLocator.AtPoint(job.Window,point,visibleOverlay)))
                    {
                        if(current!=null&&DetectionPolicy.IsBoardPuzzle(current.Kind))
                        {status.Text="Puzzle covered. Keeping the last reading.";nextRead=DateTime.UtcNow.AddMilliseconds(120);ShowGuidance();return;}
                        if(lastLocated!=null&&DetectionPolicy.IsBoardPuzzle(lastLocated.Kind))
                        {status.Text="Puzzle covered. Keeping its area.";nextRead=DateTime.UtcNow.AddMilliseconds(120);return;}
                        compassSession.Observe(null,DateTime.UtcNow);
                        lastLocated=null;current=null;marks.Clear();overlay.Hide();captureSearch.ForceFullWindow();
                        detected.Text="Keep the puzzle fully visible";
                        status.Text="The puzzle moved or is covered. Move the helper away from the puzzle.";
                        return;
                    }
                }
                if(result.Preview!=null)
                { Image old=preview.Image; preview.Image=result.Preview; result.Preview=null; if(old!=null) old.Dispose(); }
                if(result.Plan==null)
                {
                    if(current!=null && current.Kind==PuzzleKind.Compass)
                    { status.Text="Compass stays locked."; nextRead=DateTime.UtcNow.AddMilliseconds(120); ShowGuidance(); return; }
                    bool currentWasSlider=current!=null&&current.Kind==PuzzleKind.Sliding;
                    retainedClue.Missing();
                    compassSession.Observe(null,DateTime.UtcNow);
                    lastLocated=null; pendingKey=null; current=null; marks.Clear(); canvas.Puzzle=null; canvas.Invalidate(); overlay.Hide();
                    bool savedMaster=retainedClue.Saved!=null&&retainedClue.Saved.Kind==PuzzleKind.EmoteClue;
                    ShowMasterView(savedMaster);
                    masterView.SetClue(savedMaster?retainedClue.Saved.Master:null,assetPath);
                    puzzleDetails="Capture succeeded; no supported puzzle matched."; detected.Text="Game captured / puzzle not recognised";
                    summary.Text=retainedClue.Saved==null?"Open a clue or puzzle. Text clues use Windows English OCR.":"Last clue saved. Guidance remains visible while you travel.";
                    status.Text=currentWasSlider&&sliderRereadMisses>=3?"Can't read the tiles - move the mouse off the puzzle":(retainedClue.Saved==null?"Looking for a clue. Use Screenshot or Details if one is not recognised.":"New clues replace the saved card automatically. Clear removes it."); return;
                }
                DetectedPuzzle p=result.Plan.Puzzle.OffsetBy(Point.Empty);
                if(!job.Offline&&p.Kind==PuzzleKind.Sliding&&result.Milliseconds>=350)
                {
                    // The player can move tiles while a better route is being
                    // calculated. Re-read before showing any newly found route.
                    Rectangle screen=p.Bounds;screen.Offset(job.CaptureOffset);screen.Offset(Native.ClientBounds(job.Window).Location);
                    Rectangle localGrid=p.Grid;localGrid.Offset(-p.Bounds.Left,-p.Bounds.Top);
                    using(Bitmap freshImage=ScreenReader.Capture(screen))
                    {
                        ScreenReader.Cover(freshImage, screen, CaptureInk(Native.ClientBounds(job.Window).Location, screen));
                        Point guideOrigin=p.Bounds.Location;guideOrigin.Offset(job.CaptureOffset);
                        GuideInk freshInk=GuideInk.ForSlider(freshImage.Width,freshImage.Height,marks,guideOrigin);
                        DetectedPuzzle fresh=analysisEngine.ReadOnlySlider(freshImage,new DetectedPuzzle{Kind=PuzzleKind.Sliding,Bounds=new Rectangle(Point.Empty,freshImage.Size),Grid=localGrid,Scale=p.Scale},freshInk);
                        bool matched=fresh!=null&&fresh.Board!=null&&p.Board!=null&&fresh.Board.SequenceEqual(p.Board);
                        List<int> remaining=null;
                        bool advanced=!matched&&fresh!=null&&fresh.Board!=null&&p.Board!=null&&result.Plan.Sliding!=null&&SlidingRoute.Advance(p.Board,result.Plan.Sliding,fresh.Board,out remaining)&&SlidingRoute.Playable(fresh.Board,remaining);
                        int reread=SliderStep.Reread(matched,advanced,sliderRereadMisses);
                        if(reread==0&&advanced)
                        {
                            sliderRereadMisses=0;
                            int consumed=result.Plan.Sliding.Count-remaining.Count;
                            bool optimal = result.Plan.SlidingOptimal;
                            List<int> spentClicks;
                            bool spentOk=SlidingRoute.TryClickRoute(p.Board,result.Plan.Sliding.Take(consumed).ToList(),out spentClicks);
                            if(result.Plan.SlidingWholeRows&&(!spentOk||SlidingRoute.CompletedClicks(p.Board,result.Plan.Sliding,consumed)!=spentClicks.Count))optimal=false;
                            p.Board=fresh.Board;result.Plan=new PuzzlePlan { Puzzle=p, Sliding=remaining, SlidingWholeRows=result.Plan.SlidingWholeRows, SlidingOptimal=optimal };
                        }
                        else if(reread==1||reread==2)
                        {
                            sliderRereadMisses++;
                            Note("Slide re-read missed ("+sliderRereadMisses+"). Keeping the last board.");
                            nextRead=DateTime.UtcNow.AddMilliseconds(75);
                            status.Text="Reading the slider again...";
                            if(current!=null)ShowGuidance();
                            return;
                        }
                        else sliderRereadMisses=0;
                    }
                }
                Rectangle body=p.Bounds,grid=p.Grid; body.Offset(job.CaptureOffset); grid.Offset(job.CaptureOffset);
                DetectedPuzzle puzzle=new DetectedPuzzle { Kind=p.Kind,Bounds=body,Grid=grid,Scale=p.Scale,Board=p.Board,Towers=p.Towers,Knot=p.Knot,Master=p.Master,Compass=p.Compass,Scan=p.Scan,Theme=p.Theme,Message=p.Message,Confidence=p.Confidence };
                puzzleDetails=puzzle.Kind+"; "+puzzle.Bounds+"; scale "+puzzle.Scale+"; confidence "+puzzle.Confidence.ToString("0.000");
                if(puzzle.Kind==PuzzleKind.Towers && puzzle.Towers!=null && puzzle.Towers.Top!=null && puzzle.Towers.Bottom!=null && puzzle.Towers.Left!=null && puzzle.Towers.Right!=null) puzzleDetails+="; clues "+String.Join(",",puzzle.Towers.Top.Concat(puzzle.Towers.Bottom).Concat(puzzle.Towers.Left).Concat(puzzle.Towers.Right));
                if(puzzle.Kind==PuzzleKind.Compass)puzzleDetails+="; "+puzzle.Compass.Heading+"; arrow pixels "+puzzle.Compass.Pixels+"; error "+(puzzle.Compass.Error*180/Math.PI).ToString("0.000")+" degrees; bearings "+compassSession.Bearings.Count;
                if(job.Stable && pendingKey!=puzzle.Key && DetectionPolicy.NeedsConfirmation(result.Plan) && !SliderStep.SkipConfirm(current,puzzle,slidingMoves) && !LockboxStep.SkipConfirm(current,puzzle,job.Reverse?-1:1)) { pendingKey=puzzle.Key; status.Text="Puzzle found - confirming the next frame..."; nextRead=DateTime.UtcNow.AddMilliseconds(100); return; }
                DetectedPuzzle shownClue=retainedClue.Display(current);
                if(!job.Offline&&shownClue!=null&&shownClue.Kind==PuzzleKind.Scan&&!scanChange.Accept(shownClue.Key,puzzle.Key))
                {nextRead=DateTime.UtcNow.AddMilliseconds(100);return;}
                if(puzzle.Kind==PuzzleKind.Sliding)sliderRereadMisses=0;
                lastLocated=puzzle;
                if(DetectionPolicy.IsBoardPuzzle(puzzle.Kind)||puzzle.Kind==PuzzleKind.Compass||puzzle.Kind==PuzzleKind.Scan)captureSearch.Complete(true,true);
                if(puzzle.Kind==PuzzleKind.Scan)scanClosedAt=DateTime.MinValue;
                retainedClue.Accept(puzzle);
                if(puzzle.Kind==PuzzleKind.Compass){compassSession.Observe(puzzle.Compass,job.Offline?DateTime.UtcNow:job.CapturedAt);compassLeave.Reset();}
                else if((compassSession.HasHistory||compassSession.Current!=null) && !compassLeave.Accept("compass", puzzle.Key))
                { status.Text="Compass stays locked."; nextRead=DateTime.UtcNow.AddMilliseconds(120); ShowGuidance(); return; }
                else if(compassSession.HasHistory||compassSession.Current!=null) compassSession.NewClue();
                pendingKey=puzzle.Key; offline=job.Offline;
                // Same board in the same place: keep what is drawn untouched.
                if(SteadyPolicy.HoldsThroughMisses(puzzle.Kind)&&SteadyPolicy.SameState(current,puzzle,2)) ShowGuidance();
                else if(!result.Unchanged || current==null || current.Bounds!=puzzle.Bounds || current.Grid!=puzzle.Grid || current.Key!=puzzle.Key) { current=puzzle; SolveAndShow(puzzle,result.Plan); }
                else ShowGuidance();
            }
            catch (Exception ex) { NoteFailure(ex); }
            finally
            {
                if(result!=null) result.Dispose();
                if(pendingJob!=null && !closing)
                { AnalysisJob queued=pendingJob; pendingJob=null; if(queued.Generation==sourceGeneration) QueueAnalysis(queued); else queued.Image.Dispose(); }
            }
        }
        private void UpdatePreview(Bitmap source, Rectangle rectangle)
        {
            using (Bitmap crop = Pixels.Crop(source, rectangle, rectangle.Size))
            { Image old = preview.Image; preview.Image = crop == null ? null : (Image)crop.Clone(); if (old != null) old.Dispose(); }
        }
        private Rectangle Cell(DetectedPuzzle p, int index)
        {
            return GuidanceDrawing.Cell(p, index);
        }
        private void SolveAndShow(DetectedPuzzle puzzle, PuzzlePlan prepared = null)
        {
            List<GuidanceMark> previousKnotMarks=puzzle.Kind==PuzzleKind.Knot?marks:null;
            marks = new List<GuidanceMark>(); canvas.Puzzle = puzzle; canvas.Values = null; canvas.Next = -1; canvas.Lines = null;
            ShowMasterView(puzzle.Kind==PuzzleKind.EmoteClue);
            if(puzzle.Kind==PuzzleKind.Scan)
            {
                detected.Text=puzzle.Scan.Region.Tier.ToUpperInvariant()+" SCAN / "+puzzle.Scan.Region.Name.ToUpperInvariant();summary.Text="Use the floating scan map. Auto follow reads your minimap; enable Meerkat when active.";
                canvas.Lines=new[]{puzzle.Scan.Region.Name,puzzle.Scan.Region.Spots.Count+" known spots","Range: "+puzzle.Scan.Region.BaseRange+" paces","Use the floating map."};
                canvas.Invalidate();overlay.Hide();ShowGuidance();return;
            }
            if(puzzle.Kind==PuzzleKind.Compass)
            {
                detected.Text="ELITE COMPASS";summary.Text=compassSession.Summary;
                canvas.Lines=new[]{puzzle.Compass.Heading,"Use the floating panel","to record positions.","Reopened clues check old lines."};
                canvas.Invalidate();overlay.Hide();ShowGuidance();return;
            }
            if(puzzle.Kind==PuzzleKind.EmoteClue)
            {
                knotHistory=null;
                masterView.SetClue(puzzle.Master,assetPath);
                detected.Text=puzzle.Master.Tier.ToUpperInvariant()+" CLUE / "+puzzle.Master.Emote.ToUpperInvariant();
                summary.Text=puzzle.Master.Reference?puzzle.Master.Location:"Equip the listed items, then "+puzzle.Master.Emote.ToLowerInvariant()+" in the marked area.";
                status.Text=offline ? "Screenshot mode. Clue recognised locally." : "Clue recognised. Guidance switches automatically.";
                overlay.Hide();ShowGuidance(); return;
            }
            detected.Text = puzzle.Kind == PuzzleKind.Sliding ? "SLIDING BOX / " + (puzzle.Theme ?? "").Replace('_', ' ') : puzzle.Kind == PuzzleKind.Knot ? "CELTIC KNOT" : puzzle.Kind.ToString().ToUpperInvariant();
            if (puzzle.Kind != PuzzleKind.Knot) knotHistory = null;
            if (puzzle.Kind == PuzzleKind.Lockbox)
            {
                Solution result = prepared==null ? null : prepared.Lockbox;
                if (prepared==null && puzzle.Board != null)
                {
                    try { result = LockboxSolver.Solve(puzzle.Board, reverse.Checked ? -1 : 1); }
                    catch (ArgumentException ex) { CrashLog.Write(ex); result = null; }
                }
                if (result == null) { overlay.Hide(); summary.Text = "No valid lockbox plan. Check the symbols."; canvas.Invalidate(); return; }
                canvas.Values = result.Clicks; canvas.Next = Array.FindIndex(result.Clicks, v => v > 0);
                for (int i = 0; i < 25; i++) if (result.Clicks[i] > 0) marks.Add(new GuidanceMark { Box = Cell(puzzle, i), Text = result.Clicks[i].ToString(), Colour = i == canvas.Next ? Color.Lime : Color.Gold, Corner = true });
                summary.Text = result.Total == 0 ? "All symbols match. Lockbox solved." : result.Total + " clicks. Each number is the click count for that tile.";
            }
            else if (puzzle.Kind == PuzzleKind.Towers)
            {
                if (puzzle.Towers == null || puzzle.Towers.Top == null || puzzle.Towers.Bottom == null || puzzle.Towers.Left == null || puzzle.Towers.Right == null)
                { overlay.Hide(); summary.Text = "Towers found, but the clue numbers were not readable."; canvas.Invalidate(); return; }
                int readable=puzzle.Towers.Top.Concat(puzzle.Towers.Bottom).Concat(puzzle.Towers.Left).Concat(puzzle.Towers.Right).Count(v=>v>0);
                if(readable<20) { overlay.Hide(); summary.Text="Towers found: "+readable+" of 20 clues read. Waiting for a clearer frame."; status.Text="Copy details includes the recognised clue numbers."; canvas.Invalidate(); return; }
                List<int[]> solutions = prepared==null ? null : prepared.Towers;
                if (prepared==null)
                {
                    try { solutions = TowerSolver.Solve(puzzle.Towers, false); }
                    catch (ArgumentException ex) { CrashLog.Write(ex); solutions = null; }
                }
                if (solutions == null || solutions.Count == 0) { overlay.Hide(); summary.Text = "No valid tower solution. Rescan the clue numbers."; canvas.Invalidate(); return; }
                canvas.Values = solutions[0];
                marks = GuidanceDrawing.TowerMarks(puzzle, solutions[0], towerCountdown.Checked);
                bool complete = puzzle.Towers.Cells!=null && puzzle.Towers.Cells.SequenceEqual(solutions[0]);
                summary.Text = complete ? "Tower heights match. Press CHECK in the game." : towerCountdown.Checked ? "Corners show clicks left. Green 0 means that square is complete." : "Set each square to its small corner number, then press CHECK.";
            }
            else if (puzzle.Kind == PuzzleKind.Sliding)
            {
                if (!SlidingSolver.IsSolvable(puzzle.Board)) { overlay.Hide(); summary.Text = "Tile recognition gave an impossible board. Rescan before moving."; canvas.Invalidate(); return; }
                if (AwaitShortestSlider(puzzle, ref prepared)) return;
                if (!FollowSlider(puzzle,prepared==null?null:prepared.Sliding)) { overlay.Hide(); summary.Text = SlidingSolver.IsSolvable(puzzle.Board)?"Reading the slide puzzle...":"Tile recognition gave an impossible board. Rescan before moving."; canvas.Invalidate(); return; }
                if (slidingMoves==null||slidingMoves.Count == 0) summary.Text = "Sliding puzzle solved.";
                else
                {
                    List<int> clicks;
                    if(!SlidingRoute.TryClickRoute(puzzle.Board,slidingMoves,out clicks))
                    {
                        Note("Slider route could not be shown. Waiting for the next read.");
                        slidingMoves=null;slidingBoard=null;
                        overlay.Hide();summary.Text="Reading the slide puzzle again.";status.Text="The slide route did not match the board. Reading it again.";
                        canvas.Invalidate();return;
                    }
                    canvas.Next=clicks[0];
                    marks=GuidanceDrawing.SliderMarks(puzzle,clicks,slidingBatchNumber,1);
                    if(marks.Count>0){marks[0].Colour=KavrosTheme.Gold;marks[0].Pulse=true;int gap=Array.IndexOf(puzzle.Board,24);marks[0].ArrowX=gap%5-clicks[0]%5;marks[0].ArrowY=gap/5-clicks[0]/5;}
                    summary.Text=clicks.Count+" left. "+(prepared!=null&&prepared.SlidingOptimal?"Shortest route.":"Best route so far.");
                }
            }
            else if (puzzle.Kind == PuzzleKind.Knot)
            {
                if(prepared!=null&&prepared.KnotMoving)
                {
                    if(previousKnotMarks!=null)marks=previousKnotMarks.Select(m=>new GuidanceMark{Box=m.Box,Text=m.Text,Colour=Color.Silver,Corner=m.Corner,Compact=m.Compact,Outline=m.Outline}).ToList();
                    canvas.Lines=new[]{"Updating ring counts...","Grey numbers are the","last confirmed counts."};summary.Text="Updating counts - grey numbers show the last confirmed reading.";
                    canvas.Invalidate();ShowGuidance();return;
                }
                if(prepared==null) { puzzle.Knot = KnotImageReader.Merge(puzzle.Knot, knotHistory); knotHistory = puzzle.Knot; }
                KnotSolution solution = prepared==null ? null : prepared.Knot;
                if (prepared==null && puzzle.Knot != null)
                {
                    try { solution = KnotSolver.Solve(puzzle.Knot); }
                    catch (ArgumentException ex) { CrashLog.Write(ex); solution = null; }
                }
                if (solution == null)
                {
                    canvas.Lines = new[] { "Some runes are hidden.", "Rotate any ring once,", "then let it scan again." };
                    summary.Text = "Rotate a ring once to reveal more runes; the reader combines scans.";
                }
                else
                {
                    string[] colourNames = { "Blue", "Red", "Dark blue", "Gold", "Grey" };
                    List<string> lines = new List<string>();
                    for (int i = 0; i < solution.Offsets.Length; i++)
                    {
                        int amount = solution.Offsets[i];
                        int colour = puzzle.Knot.Colours != null && i < puzzle.Knot.Colours.Length ? puzzle.Knot.Colours[i] : -1;
                        string colourName = colour >= 0 && colour < colourNames.Length ? colourNames[colour] : "Ring";
                        lines.Add(colourName + ": " + (amount == 0 ? "done" : Math.Abs(amount) + (amount > 0 ? " clockwise" : " counterclockwise")));
                        if (amount != 0 && puzzle.Knot.Buttons != null && i < puzzle.Knot.Buttons.Length && puzzle.Knot.Buttons[i] != null && puzzle.Knot.Buttons[i].Length > (amount > 0 ? 0 : 1))
                        {
                            Point button = puzzle.Knot.Buttons[i][amount > 0 ? 0 : 1];
                            Rectangle box = new Rectangle(puzzle.Bounds.X + (int)((button.X - 13) * puzzle.Scale), puzzle.Bounds.Y + (int)((button.Y - 13) * puzzle.Scale), (int)(26 * puzzle.Scale), (int)(26 * puzzle.Scale));
                            marks.Add(new GuidanceMark { Box = box, Text = Math.Abs(amount).ToString(), Colour = marks.Count == 0 ? Color.Lime : Color.Gold });
                        }
                    }
                    canvas.Lines = lines.ToArray();
                    summary.Text = solution.Cost == 0 ? "Intersections match. Check the knot in the game." : (solution.Certain ? "Rotate the marked arrows by their shown counts." : "One possible plan remains; rotate and rescan to verify.");
                }
            }
            canvas.Invalidate();
            status.Text = offline ? "Screenshot mode. The game overlay is disabled." : "Detected automatically. Guidance follows the visible puzzle.";
            ShowGuidance();
        }
        private bool FollowSlider(DetectedPuzzle puzzle,List<int> preparedRoute=null)
        {
            int[] shown=slidingTheme==puzzle.Theme?slidingBoard:null;
            List<int> shownRoute=slidingTheme==puzzle.Theme?slidingMoves:null;
            List<int> route;int consumed;
            if(!SliderStep.Follow(shown,shownRoute,puzzle.Board,preparedRoute,out route,out consumed))return false;
            bool moved=shown!=null&&!shown.SequenceEqual(puzzle.Board);
            if(moved)
            {
                slidingBatchNumber+=SlidingRoute.CompletedClicks(shown,shownRoute,consumed);
                if(sliderStepShown!=DateTime.MinValue)Note("click -> next step shown: "+(int)(DateTime.UtcNow-sliderStepShown).TotalMilliseconds+" ms");
                sliderStepShown=DateTime.UtcNow;
            }
            else if(shown==null){slidingBatchNumber=1;sliderStepShown=DateTime.UtcNow;}
            slidingMoves=route;slidingBoard=(int[])puzzle.Board.Clone();slidingTheme=puzzle.Theme;
            return true;
        }
        private Rectangle lastClient;
        private string lastMonitor="";
        private readonly ChangeConfirmation compassLeave=new ChangeConfirmation();
        private readonly List<string> debugLog=new List<string>();
        private void Note(string message)
        {
            debugLog.Add(DateTime.Now.ToString("HH:mm:ss.fff")+" "+message);
            if(debugLog.Count>80) debugLog.RemoveAt(0);
        }
        private void FollowWindow()
        {
            if(SelectedWindow==IntPtr.Zero) return;
            Rectangle client=Native.ClientBounds(SelectedWindow);
            string monitor="";
            try { monitor=Screen.FromRectangle(client).DeviceName; } catch(Exception) { }
            bool resized=lastClient.Width>0 && client.Size!=lastClient.Size;
            bool moved=lastClient.Width>0 && client.Location!=lastClient.Location;
            bool display=lastMonitor.Length>0 && monitor.Length>0 && monitor!=lastMonitor;
            lastClient=client; if(monitor.Length>0) lastMonitor=monitor;
            if(resized||display)
            {
                captureSearch.ForceFullWindow();
                nextRead=DateTime.MinValue;
                Note("window changed "+client+" monitor "+monitor);
            }
            else if(moved) Note("window moved "+client);
        }
        private void PublishBinding(IntPtr handle, uint processId)
        {
            if (!IsHandleCreated) return;
            try
            {
                if (handle != IntPtr.Zero) Native.SetProp(Handle, ClientLaunch.ClientProperty, handle);
                if (processId != 0) Native.SetProp(Handle, ClientLaunch.ProcessProperty, new IntPtr((long)processId));
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }
        private List<Rectangle> CaptureInk(Point clientOrigin, Rectangle screenArea)
        {
            List<Rectangle> ink = OverlayInk(clientOrigin);
            try { ink.AddRange(WindowLocator.ForeignCovers(screenArea)); }
            catch (Exception ex) { CrashLog.Write(ex); }
            return ink;
        }
        private List<Rectangle> OverlayInk(Point clientOrigin)
        {
            var ink=new List<Rectangle>();
            if(overlay.Visible)
            {
                foreach(GuidanceMark mark in marks)
                {
                    if(mark.Sliding)continue;
                    int before=ink.Count;
                    GuidanceDrawing.AddOpaque(mark, ink);
                    for(int i=before;i<ink.Count;i++){Rectangle patch=ink[i];patch.Offset(clientOrigin);ink[i]=patch;}
                }
            }
            if(notice!=null && notice.Visible) ink.Add(notice.Bounds);
            return ink;
        }
        private List<Rectangle> NoticeBlocked()
        {
            var blocked=new List<Rectangle>();
            if(floatingPanel!=null && floatingPanel.Visible) blocked.Add(floatingPanel.Bounds);
            if(overlay.Visible) blocked.Add(overlay.Bounds);
            if(current!=null && SelectedWindow!=IntPtr.Zero)
            {
                Rectangle puzzle=current.Bounds;
                puzzle.Offset(Native.ClientBounds(SelectedWindow).Location);
                blocked.Add(puzzle);
            }
            if(!trackedMinimap.IsEmpty && SelectedWindow!=IntPtr.Zero)
            {
                Rectangle mini=trackedMinimap;
                mini.Offset(Native.ClientBounds(SelectedWindow).Location);
                blocked.Add(mini);
            }
            return blocked;
        }
        private void SaveDebug()
        {
            try
            {
                string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"KavrosClueHelper","debug",DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
                Directory.CreateDirectory(folder);
                Rectangle client=SelectedWindow==IntPtr.Zero?Rectangle.Empty:Native.ClientBounds(SelectedWindow);
                if(client.Width>=60 && client.Height>=60)
                {
                    using(Bitmap shot=ScreenReader.Capture(client)) shot.Save(Path.Combine(folder,"game.png"));
                }
                uint dpi=0;
                try { if(SelectedWindow!=IntPtr.Zero) dpi=Native.GetDpiForWindow(SelectedWindow); } catch(EntryPointNotFoundException) { } catch(DllNotFoundException) { }
                string monitor=lastMonitor;
                var info=new StringBuilder();
                info.AppendLine("Kavros Clue Helper "+AppBranding.DisplayVersion);
                info.AppendLine("Detected: "+(current==null?"none":current.Kind.ToString()));
                if(current!=null)
                {
                    info.AppendLine("Bounds: "+current.Bounds);
                    info.AppendLine("Grid: "+current.Grid);
                    info.AppendLine("Scale: "+current.Scale.ToString(CultureInfo.InvariantCulture));
                    info.AppendLine("Confidence: "+current.Confidence.ToString("0.000"));
                    info.AppendLine("Message: "+current.Message);
                }
                info.AppendLine("Client: "+client);
                info.AppendLine("DPI: "+dpi);
                info.AppendLine("Monitor: "+monitor);
                info.AppendLine("Capture: "+captureDetails);
                info.AppendLine("Puzzle: "+puzzleDetails);
                info.AppendLine("Status: "+status.Text);
                File.WriteAllText(Path.Combine(folder,"detection.txt"), info.ToString());
                File.WriteAllLines(Path.Combine(folder,"log.txt"), debugLog.ToArray());
                try { Clipboard.SetText(folder); } catch(Exception) { }
                try { System.Diagnostics.Process.Start(folder); } catch(Exception) { }
                ShowToast("Debug saved.", false, null, null);
                status.Text="Debug saved.";
            }
            catch(Exception ex) { status.Text="Could not save debug. "+ex.Message; }
        }
        private void ReleaseClosedScan()
        {
            DetectedPuzzle saved=retainedClue.Saved;
            bool scanOpen=current!=null&&current.Kind==PuzzleKind.Scan;
            bool scanSaved=saved!=null&&saved.Kind==PuzzleKind.Scan;
            if(scanOpen||!scanSaved)
            {
                scanClosedAt=DateTime.MinValue;
                return;
            }
            if(scanClosedAt==DateTime.MinValue)scanClosedAt=DateTime.UtcNow;
            else if((DateTime.UtcNow-scanClosedAt).TotalSeconds>=3)
            {
                retainedClue.Clear();
                scanClosedAt=DateTime.MinValue;
                summary.Text="Open a clue or puzzle. Text clues use Windows English OCR.";
                status.Text="Looking for a clue. Use Screenshot or Details if one is not recognised.";
            }
        }
        private void ShowGuidance()
        {
            if (floatingPanel != null) floatingPanel.SetQuickOption(waitingSlider != null && current != null && current.Kind == PuzzleKind.Sliding);
            UpdateFloatingPanel();
            // Marks stay on the client this helper is reading. Another window in
            // front, or covering that client, hides them.
            if (!trackedVisible || offline || !draw.Checked || current == null || marks.Count == 0 || SelectedWindow == IntPtr.Zero || !Native.IsWindow(SelectedWindow) || Native.IsIconic(SelectedWindow))
            { if(overlay.Visible) overlay.Hide(); return; }
            Rectangle client = Native.ClientBounds(SelectedWindow);
            Rectangle screen = current.Bounds; screen.Offset(client.Location);
            // One pixel of window jitter must not call SetWindowPos again.
            if(overlay.PlacedNear(screen, 2)) screen=overlay.PlacedRectangle;
            overlay.UpdateGuidance(screen, marks.Select(mark => mark.Offset(-current.Bounds.X, -current.Bounds.Y)).ToList());
            bool exists=Native.IsWindow(SelectedWindow);
            KeepSurfacesOnTop(exists,Native.IsIconic(SelectedWindow),exists&&WindowLocator.IsForeground(SelectedWindow));
        }
        private static string DroppedImage(IDataObject data)
        {
            string[] paths = data == null ? null : data.GetData(DataFormats.FileDrop) as string[];
            if (paths == null || paths.Length != 1) return null;
            string extension = Path.GetExtension(paths[0]).ToLowerInvariant();
            return new[]{".png", ".bmp", ".jpg", ".jpeg"}.Contains(extension) ? paths[0] : null;
        }
        private void OpenImageFile(string path)
        {
            try
            {
                using (var image = new Bitmap(path)) StartOffline(new Bitmap(image));
            }
            catch (Exception e) { status.Text = "Could not open that image. " + e.Message; }
        }
        protected override bool ProcessCmdKey(ref Message message, Keys keys)
        {
            if (keys == Keys.Escape && notesOpen) { HideReleaseNotes(); return true; }
            if (keys == (Keys.Control | Keys.V))
            {
                try
                {
                    using (Image image = Clipboard.GetImage())
                    {
                        if (image != null) { StartOffline(new Bitmap(image)); return true; }
                    }
                }
                catch (System.Runtime.InteropServices.ExternalException) { status.Text = "Clipboard is busy. Try pasting again."; return true; }
            }
            return base.ProcessCmdKey(ref message, keys);
        }
        private void OpenScreenshot()
        {
            ShowToast("Drop a screenshot here, or press Ctrl+V.", false, null, null);
        }
        private void SaveCapture()
        {
            Image image=diagnosticCapture??preview.Image;
            if(image==null) { status.Text="Capture a puzzle first, then save its image."; return; }
            try
            {
                string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"KavrosClueHelper","captures");
                Directory.CreateDirectory(folder);
                string file=Path.Combine(folder,"Kavros-capture-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".png");
                using(Bitmap snapshot=(Bitmap)image.Clone()) snapshot.Save(file,System.Drawing.Imaging.ImageFormat.Png);
                ShowToast("Capture saved.", false, null, null);
                status.Text="Capture saved to "+file;
            }
            catch(Exception ex) { status.Text="Could not save the capture. "+ex.Message; }
        }
        private void ShowExamples()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            foreach (string kind in new[] { "Lockbox", "Towers", "Sliding", "Knot" }) { string name = kind; menu.Items.Add(name, null, delegate { LoadExample(name); }); }
            ToolStripMenuItem masters=new ToolStripMenuItem("Master emote clues");
            foreach(MasterClue clue in MasterClues.All) { MasterClue selected=clue; masters.DropDownItems.Add(clue.Emote+" / "+clue.Location,null,delegate { StartOffline(new Bitmap(Path.Combine(assetPath,"test_assets","master",selected.Id+".png"))); }); }
            menu.Items.Add(masters);
            menu.Closed += delegate { menu.Dispose(); }; menu.Show(this, new Point(226, 160));
        }
        public void LoadExample(string kind)
        {
            requestedScan = false;
            automatic.Checked = false; overlay.Hide(); offline = true; knotHistory = null;
            int[] board = Enumerable.Range(0, 25).ToArray(); board[24] = 23; board[23] = 24;
            StartOffline(kind == "Towers" ? TestFixtures.TowerModal("rev944") : kind == "Sliding" ? TestFixtures.SliderImage("castle", board, "rev944") : kind == "Knot" ? TestFixtures.KnotModal("rev944") : TestFixtures.LockboxImage("rev944"));
        }
        private void StartOffline(Bitmap image)
        {
            if (offlineSource != null) offlineSource.Dispose();
            offlineSource = new Bitmap(image);
            retainedClue.Reset();
            CancelAnalysis(); requestedScan=false; automatic.Checked=false; offline=true; current=null; pendingKey=null; marks.Clear(); overlay.Hide();
            ShowMasterView(false); masterView.SetClue(null,assetPath);
            detected.Text="Reading image..."; status.Text="The image is being processed in the background.";
            QueueAnalysis(new AnalysisJob { Image=image,Generation=sourceGeneration,Offline=true,Stable=false,Reverse=reverse.Checked,WholeRows=true,LegacyInterface=preferences.LegacyInterface,Resolution=preferences.Resolution,Scaling=preferences.Scaling,ResolutionManual=preferences.ResolutionManual,ScalingManual=preferences.ScalingManual });
        }
    }

    public sealed class SuiteCanvas : Control
    {
        public DetectedPuzzle Puzzle; public int[] Values; public int Next = -1; public string[] Lines;
        public SuiteCanvas() { DoubleBuffered = true; }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(KavrosTheme.Input);
            if (Puzzle == null || (Puzzle.Board == null && Puzzle.Kind != PuzzleKind.Towers)) { TextRenderer.DrawText(e.Graphics, "Open a puzzle in Kavros\nto see its solution here.", Font, ClientRectangle, KavrosTheme.TextSoft, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); return; }
            if (Lines != null)
            {
                using (Font f = new Font("Segoe UI", 10F, FontStyle.Bold))
                    for (int i = 0; i < Lines.Length; i++) TextRenderer.DrawText(e.Graphics, Lines[i], f, new Rectangle(10, 20 + i * 38, Width - 20, 34), Color.FromArgb(110, 227, 190), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                return;
            }
            using (Font f = new Font("Segoe UI", 15F, FontStyle.Bold)) using (Font small = new Font("Segoe UI", 8F, FontStyle.Bold))
                for (int i = 0; i < 25; i++)
                {
                    Rectangle cell = new Rectangle(i % 5 * Width / 5 + 2, i / 5 * Height / 5 + 2, Width / 5 - 4, Height / 5 - 4);
                    using (Brush b = new SolidBrush(i == Next ? Color.FromArgb(22, 69, 61) : Color.FromArgb(30, 41, 56))) e.Graphics.FillRectangle(b, cell);
                    using (Pen pen = new Pen(i == Next ? Color.Lime : Color.FromArgb(60, 78, 100))) e.Graphics.DrawRectangle(pen, cell);
                    string text;
                    if (Puzzle.Kind == PuzzleKind.Towers) text = Values == null ? "?" : Values[i].ToString();
                    else if (Puzzle.Kind == PuzzleKind.Lockbox) text = new[] { "M", "R", "W" }[Puzzle.Board[i]];
                    else text = Puzzle.Board[i] == 24 ? "" : (Puzzle.Board[i] + 1).ToString();
                    Color colour = Puzzle.Kind == PuzzleKind.Towers ? Color.Cyan : Color.FromArgb(225, 233, 243);
                    TextRenderer.DrawText(e.Graphics, text, f, cell, colour, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    if (Puzzle.Kind == PuzzleKind.Lockbox && Values != null && Values[i] > 0)
                        TextRenderer.DrawText(e.Graphics, Values[i].ToString(), small, new Rectangle(cell.Right - 16, cell.Top + 1, 15, 15), Color.Gold, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
        }
    }
}
