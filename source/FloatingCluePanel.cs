using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KavrosClueHelper
{
    public sealed class OverlayPreferences
    {
        public bool Enabled=true,Collapsed;
        public bool ShortestSlider = true;
        public bool WholeRowClicks=true;
        public bool CheckForUpdates = true;
        public bool BlurPreview;
        public bool LegacyInterface;
        public bool DarkscapeInterface;
        public int Resolution, Scaling;
        public int ResolutionManual=1080, ScalingManual=100;
        public Point Offset=new Point(12,72);
        public string SeenNotes="";
        public readonly List<CompassPosition> SavedPositions=new List<CompassPosition>();
        private readonly string path;
        public OverlayPreferences(string file=null)
        {
            path=file??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"KavrosClueHelper","overlay.txt");
            try
            {
                if(!File.Exists(path))return;
                foreach(string line in File.ReadAllLines(path))
                {
                    string[] p=line.Split(new[] { '\t' });int x,y;bool b;
                    if(p.Length==2&&p[0]=="enabled"&&Boolean.TryParse(p[1],out b))Enabled=b;
                    if(p.Length==2&&p[0]=="collapsed"&&Boolean.TryParse(p[1],out b))Collapsed=b;
                    if(p.Length==2&&p[0]=="shortest-slider"&&Boolean.TryParse(p[1],out b))ShortestSlider=b;
                    if(p.Length==2&&p[0]=="whole-row-clicks")WholeRowClicks=true;
                    if(p.Length==2&&p[0]=="check-updates"&&Boolean.TryParse(p[1],out b))CheckForUpdates=b;
                    if(p.Length==2&&p[0]=="blur-preview"&&Boolean.TryParse(p[1],out b))BlurPreview=b;
                    if(p.Length==2&&p[0]=="interface")
                    {
                        LegacyInterface=p[1]=="legacy";
                        DarkscapeInterface=p[1]=="darkscape";
                    }
                    if(p.Length==2&&p[0]=="resolution")Resolution=ParseChoice(p[1],1080,1440,2160);
                    if(p.Length==2&&p[0]=="scaling")Scaling=ParseChoice(p[1],100,125,150,200);
                    if(p.Length==2&&p[0]=="resolution-manual")ResolutionManual=ParseChoice(p[1],1080,1440,2160)==0?1080:ParseChoice(p[1],1080,1440,2160);
                    if(p.Length==2&&p[0]=="scaling-manual")ScalingManual=ParseChoice(p[1],100,125,150,200)==0?100:ParseChoice(p[1],100,125,150,200);
                    if(p.Length==2&&p[0]=="seen-notes")SeenNotes=p[1]??"";
                    if(p.Length==3&&p[0]=="offset"&&Int32.TryParse(p[1],out x)&&Int32.TryParse(p[2],out y))Offset=new Point(x,y);
                    if(p.Length==5&&p[0]=="spot"&&Int32.TryParse(p[2],out x)&&Int32.TryParse(p[3],out y))
                    {
                        double radius;
                        if(x>=0&&x<=6400&&y>=0&&y<=12800&&Double.TryParse(p[4],NumberStyles.Float,CultureInfo.InvariantCulture,out radius)&&radius>=0&&radius<=32)
                            SavedPositions.Add(new CompassPosition{Name=LodestoneNames.Clean(Uri.UnescapeDataString(p[1])),X=x,Y=y,Radius=radius});
                    }
                }
            }
            catch(IOException){}catch(UnauthorizedAccessException){}catch(UriFormatException){}
        }
        private static int ParseChoice(string text, params int[] allowed)
        {
            int value;
            if (!int.TryParse(text, out value)) return 0;
            for (int i = 0; i < allowed.Length; i++) if (allowed[i] == value) return value;
            return 0;
        }
        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                var lines=new List<string>{"enabled\t"+Enabled,"collapsed\t"+Collapsed,"offset\t"+Offset.X+"\t"+Offset.Y};
                lines.Add("shortest-slider\t"+ShortestSlider);
                lines.Add("whole-row-clicks\t"+WholeRowClicks);
                lines.Add("check-updates\t"+CheckForUpdates);
                lines.Add("blur-preview\t"+BlurPreview);
                lines.Add("interface\t"+(LegacyInterface?"legacy":(DarkscapeInterface?"darkscape":"modern")));
                lines.Add("resolution\t"+(Resolution==0?"auto":Resolution.ToString()));
                lines.Add("scaling\t"+(Scaling==0?"auto":Scaling.ToString()));
                lines.Add("resolution-manual\t"+ResolutionManual);
                lines.Add("scaling-manual\t"+ScalingManual);
                lines.Add("seen-notes\t"+SeenNotes);
                lines.AddRange(SavedPositions.Select(p=>"spot\t"+Uri.EscapeDataString(p.Name)+"\t"+p.X+"\t"+p.Y+"\t"+p.Radius.ToString(CultureInfo.InvariantCulture)));
                File.WriteAllLines(path,lines);
            }
            catch(IOException){}catch(UnauthorizedAccessException){}
        }
    }
    public static class FloatingPanelLayout
    {
        public static Rectangle Place(Rectangle game,Size panel,Point offset,Rectangle desktop)
        {
            // Stay inside the bound client. The desktop is not a fallback, or the
            // panel can be dragged onto a second Kavros window and left there.
            Rectangle area=Rectangle.Intersect(game,desktop);
            if(area.Width<1||area.Height<1)area=game;
            int x=game.Left+offset.X,y=game.Top+offset.Y;
            int limitX=area.Right-panel.Width,limitY=area.Bottom-panel.Height;
            if(limitX<area.Left)x=area.Left; else x=Math.Max(area.Left,Math.Min(x,limitX));
            if(limitY<area.Top)y=area.Top; else y=Math.Max(area.Top,Math.Min(y,limitY));
            return new Rectangle(x,y,panel.Width,panel.Height);
        }
        public static string Header(string client,string heading)
        {
            if(string.IsNullOrEmpty(client))return heading??"";
            if(string.IsNullOrEmpty(heading))return client;
            return client+"  ·  "+heading;
        }
        public static bool Visible(bool enabled,bool offline,bool gameExists,bool minimised,bool gameForeground,bool panelFocused,bool settingsFocused)
        {
            if(!enabled||offline||!gameExists||minimised||settingsFocused)return false;
            return gameForeground||panelFocused;
        }
        public static bool Shown(bool enabled,bool offline,bool gameExists,bool minimised,bool gameForeground,bool panelFocused,bool settingsFocused,bool covered)
        {
            return Visible(enabled,offline,gameExists,minimised,gameForeground,panelFocused,settingsFocused)&&!covered;
        }
        // Topmost only while this client's own window, or this panel, is in front.
        // Another client, Companion, or any other window drops it. Launching does
        // not float the panel over a client it is not reading.
        public static bool WantTopMost(bool gameExists,bool minimised,bool gameForeground,bool helperForeground,bool panelForeground,bool settingsForeground,bool launchGrace)
        {
            if(minimised||settingsForeground||!gameExists)return false;
            if(panelForeground)return true;
            return gameForeground;
        }
    }
    public sealed class CompassMap : Control
    {
        private readonly string assets;
        private readonly Bitmap map;
        private CompassSession session;
        private PointF centre=new PointF(3030,3310);
        private double span=2200;
        private Point mouseDown;
        private readonly ToolTip locationTip=new ToolTip();
        private string hoverName;
        private List<PointF> paintedCandidates;
        private CompassPosition paintedOrigin;
        private int paintedPositionCount;
        public CompassPosition SelectedOrigin;
        public event Action<CompassPosition> LodestonePicked;
        private Point? drag;
        public event Action<PointF> PositionPicked;
        public CompassMap(string assets)
        {
            DoubleBuffered=true;TabStop=true;SetStyle(ControlStyles.Selectable,true);
            this.assets=assets;map=WorldMap.Get(assets);
            MouseDown+=delegate(object sender,MouseEventArgs e){Focus();if(e.Button==MouseButtons.Left){mouseDown=e.Location;drag=e.Location;Capture=true;}if(e.Button==MouseButtons.Right&&PositionPicked!=null)PositionPicked(ToWorld(e.Location));};
            MouseMove+=delegate(object sender,MouseEventArgs e){if(!drag.HasValue){CompassPosition over=HitLodestone(e.Location);string label=over==null?"":over.Name;if(label!=hoverName){hoverName=label;locationTip.SetToolTip(this,label);}Cursor=over==null?Cursors.Default:Cursors.Hand;return;}if(Math.Abs(e.X-mouseDown.X)+Math.Abs(e.Y-mouseDown.Y)<5)return;centre.X-=(float)((e.X-drag.Value.X)*span/Width);centre.Y+=(float)((e.Y-drag.Value.Y)*span/Width);drag=e.Location;Invalidate();};
            MouseUp+=delegate(object sender,MouseEventArgs e){bool click=drag.HasValue&&Math.Abs(e.X-mouseDown.X)+Math.Abs(e.Y-mouseDown.Y)<5;drag=null;Capture=false;if(!click)return;if(e.Y<26&&e.X<102){Overview();return;}CompassPosition p=HitLodestone(e.Location);if(p!=null&&LodestonePicked!=null)LodestonePicked(p);};
            MouseWheel+=delegate(object sender,MouseEventArgs e){PointF before=ToWorld(e.Location);span=Math.Max(16,Math.Min(3500,span*(e.Delta>0?0.7:1/0.7)));PointF after=ToWorld(e.Location);centre.X+=before.X-after.X;centre.Y+=before.Y-after.Y;Invalidate();};
        }
        public void Overview(){centre=new PointF(3030,3300);span=2200;Invalidate();}
        private static bool HasMapIcon(CompassPosition position)
        {return position.X>=2031&&position.X<=4079&&position.Y>=2543&&position.Y<=4079;}
        private PointF IconPoint(CompassPosition position){return ToScreen(position.Point);}
        public CompassPosition HitLodestone(Point point)
        {
            if(session==null||point.Y<26&&point.X<102)return null;
            return session.Positions.Where(HasMapIcon).OrderByDescending(p=>p.Name.EndsWith(" (Kavros)"))
                .Select(p=>new{Position=p,Screen=IconPoint(p)})
                .Where(p=>Math.Abs(p.Screen.X-point.X)<=12&&Math.Abs(p.Screen.Y-point.Y)<=13)
                .OrderBy(p=>Math.Abs(p.Screen.X-point.X)+Math.Abs(p.Screen.Y-point.Y)).Select(p=>p.Position).FirstOrDefault();
        }
        private void DrawLodestone(Graphics g,PointF p,CompassPosition position)
        {LodestoneIcons.Draw(g,p,position.Name,position==SelectedOrigin,assets);}
        public void SetSession(CompassSession value,bool fit)
        {
            if (!fit && session == value && paintedCandidates == value.Candidates && paintedOrigin == SelectedOrigin && paintedPositionCount == value.Positions.Count) return;
            session = value;
            paintedCandidates = value.Candidates;
            paintedOrigin = SelectedOrigin;
            paintedPositionCount = value.Positions.Count;
            if(fit)
            {
                var points=value.Candidates;
                if(value.Intersection.HasValue&&value.Bearings.Count>=2)points=value.Candidates.Where(p=>CompassMath.Distance(p,value.Intersection.Value)<180).Concat(new[]{value.Intersection.Value}).ToList();
                if(points.Count>0)
                {
                    float left=points.Min(p=>p.X),right=points.Max(p=>p.X),bottom=points.Min(p=>p.Y),top=points.Max(p=>p.Y);
                    centre=new PointF((left+right)/2,(top+bottom)/2);
                    span=Math.Max(60,Math.Max(right-left,(top-bottom)*Math.Max(1,Width)/(double)Math.Max(1,Height))*1.25);
                }
            }
            Invalidate();
        }
        public PointF ToScreen(PointF p){double scale=Width/span;return new PointF((float)(Width/2.0+(p.X-centre.X)*scale),(float)(Height/2.0-(p.Y-centre.Y)*scale));}
        public PointF ToWorld(Point p){return new PointF((float)(centre.X+(p.X-Width/2.0)*span/Width),(float)(centre.Y-(p.Y-Height/2.0)*span/Width));}
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g=e.Graphics;g.Clear(Color.FromArgb(18,30,38));if(Width==0||Height==0)return;
            double scale=Width/span,worldHeight=Height/scale;
            RectangleF view=new RectangleF((float)(centre.X-span/2),(float)(centre.Y-worldHeight/2),(float)span,(float)worldHeight);
            RectangleF world=new RectangleF(2031.5F,2543.5F,2048,1536),overlap=RectangleF.Intersect(view,world);
            if(overlap.Width>0&&overlap.Height>0)
            {
                PointF tl=ToScreen(new PointF(overlap.Left,overlap.Bottom));
                RectangleF dest=new RectangleF(tl.X,tl.Y,(float)(overlap.Width*scale),(float)(overlap.Height*scale));
                RectangleF source=new RectangleF((overlap.Left-world.Left)*map.Width/world.Width,(4079.5F-overlap.Bottom)*map.Height/world.Height,overlap.Width*map.Width/world.Width,overlap.Height*map.Height/world.Height);
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.DrawImage(map,dest,source,GraphicsUnit.Pixel);
            }
            if(session!=null)
            {
                foreach(CompassBearing bearing in session.Bearings)
                {
                    PointF start=ToScreen(bearing.Position.Point);
                    PointF end=ToScreen(new PointF(bearing.Position.X+(float)(Math.Cos(bearing.Angle)*10000),bearing.Position.Y+(float)(Math.Sin(bearing.Angle)*10000)));
                    using(Pen pen=new Pen(Color.Gold,2))g.DrawLine(pen,start,end);
                    g.FillEllipse(Brushes.Black,start.X-5,start.Y-5,10,10);g.FillEllipse(Brushes.Gold,start.X-3,start.Y-3,6,6);
                }
                foreach(PointF spot in session.Candidates)
                {
                    PointF p=ToScreen(spot);if(p.X<0||p.Y<0||p.X>Width||p.Y>Height)continue;
                    bool single=session.Candidates.Count==1&&session.Bearings.Count>=2;int radius=single?6:session.Candidates.Count>100?2:3;
                    g.FillEllipse(Brushes.Black,p.X-radius-1,p.Y-radius-1,radius*2+2,radius*2+2);
                    g.FillEllipse(single?Brushes.Lime:Brushes.Cyan,p.X-radius,p.Y-radius,radius*2,radius*2);
                    if(single){using(Pen pen=new Pen(Color.Lime,1)){g.DrawLine(pen,p.X-13,p.Y,p.X+13,p.Y);g.DrawLine(pen,p.X,p.Y-13,p.X,p.Y+13);}}
                }
                if(session.Intersection.HasValue)
                {
                    PointF p=ToScreen(session.Intersection.Value);
                    using(Pen shadow=new Pen(Color.Black,6))using(Pen ink=new Pen(Color.Magenta,3))
                    {g.DrawLine(shadow,p.X-9,p.Y-9,p.X+9,p.Y+9);g.DrawLine(shadow,p.X-9,p.Y+9,p.X+9,p.Y-9);g.DrawLine(ink,p.X-9,p.Y-9,p.X+9,p.Y+9);g.DrawLine(ink,p.X-9,p.Y+9,p.X+9,p.Y-9);}
                    TextRenderer.DrawText(g,"Estimated dig spot",Font,new Point((int)p.X+12,(int)p.Y-8),Color.Magenta,Color.FromArgb(20,25,30));
                }
            }
            if(session!=null)foreach(CompassPosition position in session.Positions.Where(HasMapIcon))
            {
                PointF p=IconPoint(position);if(p.X<0||p.Y<13||p.X>Width||p.Y>Height-4)continue;
                DrawLodestone(g,p,position);
                if(position==SelectedOrigin)TextRenderer.DrawText(g,position.Name.Replace(" lodestone",""),Font,new Point((int)p.X+13,(int)p.Y-8),Color.Lime,Color.FromArgb(18,30,38));
            }
            g.FillRectangle(Brushes.DarkSlateGray,2,2,99,23);
            TextRenderer.DrawText(g,"Lodestones",Font,new Rectangle(4,3,96,21),Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            LodestoneIcons.DrawCompass(g,Font,new Rectangle(0,0,Width,Height),assets);
        }
        protected override void Dispose(bool disposing){if(disposing){locationTip.Dispose();}base.Dispose(disposing);}
    }
    public sealed partial class FloatingCluePanel : Form
    {
        private readonly Label title=new PaintedLabel(),summary=new Label(),heading=new Label();
        private readonly Button collapse=new OpaqueButton(),pause=new OpaqueButton(),record=new OpaqueButton();
        private readonly Panel compassControls=new OpaquePanel(),footer=new OpaquePanel();
        private readonly MasterClueView master=new MasterClueView();
        private readonly SlidingGuideView sliderGuide;
        private readonly ComboBox positions=new ComboBox();
        private readonly NumericUpDown x=new NumericUpDown(),y=new NumericUpDown(),radius=new NumericUpDown();
        private bool selectingOrigin;
        private bool originSelected;
        private Button undoOrContinue, quickSlider;
        public event Action QuickSliderRequested;
        private Label banner; private Timer bannerTimer; private bool bannerShown, layoutBanner;
        private const int BannerHeight = 28;
        private Panel updateHost;
        private Label updateText;
        private Button updateButton, updateDismiss;
        private bool updateOffered, layoutUpdate;
        private const int UpdateBarHeight = 28;
        public event Action UpdateRequested;
        private Panel notesHost;
        private TextBox notesBody;
        private bool notesOpen, layoutNotes;
        internal bool ReleaseNotesOpen { get { return notesOpen; } }
        public void ShowBanner(string message, bool error)
        {
            if (banner == null)
            {
                // Not docked: the panel grows by the banner's height so the
                // footer buttons stay fully visible underneath nothing.
                banner = new Label { Height = BannerHeight, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0), Visible = false };
                Controls.Add(banner);
                bannerTimer = new Timer { Interval = 5000 };
                bannerTimer.Tick += delegate { bannerTimer.Stop(); bannerShown = false; LayoutMode(); };
                Disposed += delegate { bannerTimer.Dispose(); };
            }
            banner.BackColor = error ? KavrosTheme.Danger : KavrosTheme.Panel;
            banner.ForeColor = error ? KavrosTheme.GoldInk : KavrosTheme.Gold;
            banner.Text = message;
            bannerShown = true;
            LayoutMode();
            bannerTimer.Stop(); bannerTimer.Start();
        }
        public void OfferUpdate(string version)
        {
            if (IsDisposed || string.IsNullOrEmpty(version)) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<string>(OfferUpdate), version); }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                return;
            }
            EnsureUpdateOffer();
            updateText.Text = "Update " + version + " available";
            updateOffered = true;
            LayoutMode();
        }
        public void DismissUpdateOffer()
        {
            if (!updateOffered) return;
            updateOffered = false;
            LayoutMode();
        }
        void EnsureUpdateOffer()
        {
            if (updateHost != null) return;
            updateHost = new Panel { Name = "UpdateOffer", Height = UpdateBarHeight, BackColor = KavrosTheme.Panel, Visible = false };
            updateText = new Label { Name = "UpdateOfferText", AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, ForeColor = KavrosTheme.Gold, BackColor = KavrosTheme.Panel, Font = KavrosTheme.Ui(8.5F, true) };
            updateButton = new OpaqueButton { Name = "UpdateOfferButton", Text = "Update" };
            Style(updateButton);
            updateButton.BackColor = KavrosTheme.Gold;
            updateButton.ForeColor = KavrosTheme.GoldInk;
            updateButton.FlatAppearance.BorderColor = KavrosTheme.GoldEdge;
            updateButton.Click += delegate { if (UpdateRequested != null) UpdateRequested(); };
            updateDismiss = new OpaqueButton { Name = "UpdateOfferDismiss", Text = "\u00d7" };
            Style(updateDismiss);
            updateDismiss.Click += delegate { DismissUpdateOffer(); };
            updateHost.Controls.Add(updateText);
            updateHost.Controls.Add(updateButton);
            updateHost.Controls.Add(updateDismiss);
            Controls.Add(updateHost);
        }
        public void SetQuickOption(bool available)
        {
            if (quickSlider != null) quickSlider.Visible = false;
        }
        private int displayedCompassGeneration=-1;
        private string scanPaintKey;
        private readonly Label originStatus=new Label();
        private readonly CompassSession session;
        private readonly CompassMap map;
        private readonly ScanSession scanSession=new ScanSession();
        private readonly ScanMap scanMap;
        private readonly Panel scanControls=new OpaquePanel();
        private readonly CheckBox meerkat=new KavrosCheckBox(),autoFollow=new KavrosCheckBox();
        private readonly ComboBox scanFloor=new ComboBox();
        private int floorRegion=-1;private bool changingFloor,changingMeerkat;
        private readonly string assets;
        private readonly OverlayPreferences prefs;
        private Point dragStart,windowStart;private bool dragging;private Rectangle anchor;
        private PuzzleKind mode=PuzzleKind.None;
        private PuzzleKind layoutKind=(PuzzleKind)(-1);
        private bool layoutCollapsed;
        private int displayedMasterId=-1;
        public event Action SettingsRequested,ScanRequested,PauseRequested,ExitRequested,UserMoved,ClearRequested,DebugRequested;
        public bool Collapsed {get{return prefs.Collapsed;}}
        internal void Expand()
        {
            if(!prefs.Collapsed)return;
            prefs.Collapsed=false;
            collapse.Text="-";
            prefs.Save();
            LayoutMode();
        }
        public bool IsDragging {get{return dragging;}}
        public void SetAnchor(Rectangle game){anchor=game;}
        public void PlaceTopMost(bool top)
        {
            if(!IsHandleCreated){if(TopMost!=top)TopMost=top;return;}
            Native.SetTopMostNoActivate(Handle,top);
        }
        public string ReadingStage {get;private set;}
        public FloatingCluePanel(string directory,CompassSession compass,OverlayPreferences preferences)
        {
            assets=directory;session=compass;prefs=preferences;
            FormClosing+=delegate(object sender,FormClosingEventArgs e){if(e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}};
            FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;DoubleBuffered=true;AutoScaleMode=AutoScaleMode.None;
            SetStyle(ControlStyles.UserPaint|ControlStyles.Opaque|ControlStyles.ResizeRedraw|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
            ClientSize=new Size(400,140);BackColor=KavrosTheme.Bg;ForeColor=KavrosTheme.Text;Font=KavrosTheme.Ui(9F,false);
            title.SetBounds(0,0,294,32);title.TextAlign=ContentAlignment.MiddleLeft;title.Padding=new Padding(10,0,0,0);title.Font=new Font(Font,FontStyle.Bold);title.BackColor=KavrosTheme.BgRaise;title.AutoEllipsis=true;Controls.Add(title);
            title.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;dragging=true;dragStart=Cursor.Position;windowStart=Location;title.Capture=true;};
            title.MouseMove+=delegate{if(!dragging)return;Point next=new Point(windowStart.X+Cursor.Position.X-dragStart.X,windowStart.Y+Cursor.Position.Y-dragStart.Y);if(anchor.Width>0&&anchor.Height>0)next=FloatingPanelLayout.Place(anchor,Size,new Point(next.X-anchor.Left,next.Y-anchor.Top),Native.VirtualScreen()).Location;Location=next;};
            title.MouseUp+=delegate{if(!dragging)return;dragging=false;title.Capture=false;if(UserMoved!=null)UserMoved();};
            Button settings=ButtonAt(this,"Settings",298,4,65,delegate{if(SettingsRequested!=null)SettingsRequested();});
            settings.SetBounds(298,4,65,24);
            collapse.Text=prefs.Collapsed?"+":"-";collapse.SetBounds(369,4,25,24);Style(collapse);Controls.Add(collapse);collapse.Click+=delegate{prefs.Collapsed=!prefs.Collapsed;collapse.Text=prefs.Collapsed?"+":"-";prefs.Save();LayoutMode();};
            summary.BackColor=BackColor;heading.BackColor=BackColor;originStatus.BackColor=BackColor;
            summary.AutoSize=false;summary.SetBounds(10,38,380,56);summary.ForeColor=KavrosTheme.Ok;Controls.Add(summary);
            heading.SetBounds(10,38,380,23);heading.ForeColor=KavrosTheme.Ice;Controls.Add(heading);
            map=new CompassMap(directory);map.SetBounds(10,66,380,260);map.PositionPicked+=delegate(PointF p){selectingOrigin=true;positions.SelectedIndex=-1;positions.Text="Custom position";x.Value=Clamp((decimal)Math.Round(p.X),x);y.Value=Clamp((decimal)Math.Round(p.Y),y);selectingOrigin=false;originSelected=true;RefreshCompass(false);};map.LodestonePicked+=SelectOrigin;Controls.Add(map);
            master.SetBounds(6,36,388,397);Controls.Add(master);
            sliderGuide=new SlidingGuideView(directory);sliderGuide.SetBounds(10,98,380,182);Controls.Add(sliderGuide);
            quickSlider=ButtonAt(sliderGuide,"Use quick route",174,148,178,delegate{if(QuickSliderRequested!=null)QuickSliderRequested();});
            quickSlider.Visible=false;
            scanMap=new ScanMap(directory,scanSession);scanMap.SetBounds(10,66,380,260);scanMap.PositionChanged+=delegate{autoFollow.Checked=false;RefreshScan();};Controls.Add(scanMap);
            scanControls.SetBounds(10,424,380,88);Controls.Add(scanControls);
            autoFollow.BackColor=BackColor;meerkat.BackColor=BackColor;
            autoFollow.Text="Auto follow minimap";autoFollow.SetBounds(0,0,380,24);autoFollow.Checked=true;autoFollow.CheckedChanged+=delegate{if(scanSession.AutoTracking!=autoFollow.Checked)scanSession.SetAutomatic(autoFollow.Checked);RefreshScan();};scanControls.Controls.Add(autoFollow);
            meerkat.Text="Meerkat (+5 paces)";meerkat.SetBounds(0,25,380,24);meerkat.CheckedChanged+=delegate{if(changingMeerkat)return;scanSession.Meerkat=meerkat.Checked;scanSession.MeerkatLocked=true;RefreshScan();};scanControls.Controls.Add(meerkat);
            meerkat.Width=210;scanFloor.SetBounds(220,25,155,24);scanFloor.DropDownStyle=ComboBoxStyle.DropDownList;scanFloor.BackColor=KavrosTheme.Input;scanFloor.ForeColor=KavrosTheme.Text;scanFloor.FlatStyle=FlatStyle.Flat;scanFloor.Visible=false;scanControls.Controls.Add(scanFloor);scanFloor.BringToFront();
            scanFloor.SelectedIndexChanged+=delegate{if(changingFloor||scanSession.Region==null||scanFloor.SelectedIndex<0)return;scanSession.Region.Level=scanSession.Region.Levels[scanFloor.SelectedIndex];scanSession.SetAutomatic(autoFollow.Checked);RefreshScan();};
            LabelAt(scanControls,"Green box: scan range | Dashed: double range",0,54,380,30);
            Label scanNote=new Label{Name="ScanTrackingNote",AutoSize=false,ForeColor=KavrosTheme.TextFaint,BackColor=KavrosTheme.Bg,Font=KavrosTheme.Ui(8F,false),Text="Live scan tracking is experimental and can be finicky — click spots on the map to cross them off manually."};
            Size noteSize=TextRenderer.MeasureText(scanNote.Text,scanNote.Font,new Size(368,0),TextFormatFlags.WordBreak);
            scanNote.SetBounds(0,90,380,Math.Max(32,noteSize.Height+4));
            scanControls.Controls.Add(scanNote);
            scanControls.Height=scanNote.Bottom+8;
            compassControls.SetBounds(10,391,380,121);Controls.Add(compassControls);
            positions.SetBounds(0,0,304,26);positions.DropDownStyle=ComboBoxStyle.DropDown;
            positions.AutoCompleteMode=AutoCompleteMode.SuggestAppend;positions.AutoCompleteSource=AutoCompleteSource.ListItems;
            positions.Enter+=delegate{positions.SelectAll();};
            positions.KeyDown+=delegate(object sender,KeyEventArgs e)
            {
                if(e.KeyCode!=Keys.Enter||String.IsNullOrWhiteSpace(positions.Text))return;
                string query=positions.Text.Trim();
                var values=positions.Items.OfType<CompassPosition>();
                CompassPosition match=values.FirstOrDefault(v=>String.Equals(v.Name,query,StringComparison.OrdinalIgnoreCase))
                    ??values.FirstOrDefault(v=>v.Name.StartsWith(query,StringComparison.OrdinalIgnoreCase));
                if(match==null)return;
                SelectOrigin(match);positions.DroppedDown=false;positions.Select(positions.Text.Length,0);e.Handled=true;e.SuppressKeyPress=true;
            };
            compassControls.Controls.Add(positions);
            foreach(CompassPosition p in session.Positions)positions.Items.Add(p);
            foreach(CompassPosition p in prefs.SavedPositions){positions.Items.Add(p);session.Positions.Add(p);}
            ButtonAt(compassControls,"Save",310,0,70,SavePosition);
            LabelAt(compassControls,"X",0,34,14,23);x.SetBounds(17,31,81,25);x.Maximum=6400;compassControls.Controls.Add(x);
            LabelAt(compassControls,"Y",107,34,14,23);y.SetBounds(124,31,81,25);y.Maximum=12800;compassControls.Controls.Add(y);
            LabelAt(compassControls,"Radius",214,34,49,23);radius.SetBounds(266,31,52,25);radius.Minimum=0;radius.Maximum=32;radius.Value=1;compassControls.Controls.Add(radius);LabelAt(compassControls,"tiles",323,34,52,23);
            originStatus.Text="Click a lodestone on the map to choose your start.";originStatus.SetBounds(0,61,380,23);originStatus.ForeColor=KavrosTheme.Gold;compassControls.Controls.Add(originStatus);
            foreach(NumericUpDown number in new[]{x,y,radius})number.ValueChanged+=delegate{if(!selectingOrigin){originSelected=true;RefreshCompass(false);}};
            positions.SelectedIndexChanged+=delegate{CompassPosition p=positions.SelectedItem as CompassPosition;if(p!=null&&!selectingOrigin)SelectOrigin(p);};
            positions.TextChanged+=delegate{if(!selectingOrigin&&(positions.SelectedItem==null||!String.Equals(((CompassPosition)positions.SelectedItem).Name,positions.Text,StringComparison.OrdinalIgnoreCase))){originSelected=false;map.SelectedOrigin=null;RefreshCompass(false);}};
            record.Text="Record direction";record.SetBounds(0,90,168,28);Style(record);compassControls.Controls.Add(record);
            record.Click+=delegate{if(originSelected&&session.Record(SelectedPosition(),DateTime.UtcNow)){originSelected=false;map.SelectedOrigin=null;RefreshCompass(true);}};
            undoOrContinue=ButtonAt(compassControls,"Undo",176,90,82,delegate{if(session.HasPrevious)session.ResumePrevious();else session.Undo();RefreshCompass(true);});
            ButtonAt(compassControls,"New clue",266,90,114,delegate{session.NewClue();originSelected=false;map.SelectedOrigin=null;RefreshCompass(true);});
            footer.SetBounds(10,102,380,28);Controls.Add(footer);
            ButtonAt(footer,"Rescan",0,0,62,delegate{if(ScanRequested!=null)ScanRequested();});pause.Text="Pause";pause.SetBounds(68,0,58,28);Style(pause);footer.Controls.Add(pause);pause.Click+=delegate{if(PauseRequested!=null)PauseRequested();};
            ButtonAt(footer,"Save debug",132,0,96,delegate{if(DebugRequested!=null)DebugRequested();});
            ButtonAt(footer,"Clear",234,0,62,delegate{scanSession.Clear();if(ClearRequested!=null)ClearRequested();});
            ButtonAt(footer,"Exit",302,0,78,delegate{if(ExitRequested!=null)ExitRequested();});
            BuildReleaseNotes();
            notesOpen = ReleaseNotes.IsNewer(prefs.SeenNotes, AppBranding.DisplayVersion);
            positions.Text="";
            SetWaiting("Open Kavros and a clue or puzzle.");
            ApplyOverlayPolish();
        }
        void BuildReleaseNotes()
        {
            notesHost = new Panel { Name = "WhatsNewBanner", BackColor = KavrosTheme.BgRaise, Visible = false };
            Label heading = new Label { Name = "WhatsNewHeading", Text = "What's new", ForeColor = KavrosTheme.Gold, BackColor = KavrosTheme.BgRaise, Font = KavrosTheme.Ui(10F, true), TextAlign = ContentAlignment.MiddleLeft };
            OpaqueButton dismiss = new OpaqueButton { Name = "WhatsNewDismiss", Text = "Got it", Tone = ButtonTone.Secondary, FlatStyle = FlatStyle.Flat, BackColor = KavrosTheme.Button, ForeColor = KavrosTheme.Text };
            dismiss.FlatAppearance.BorderColor = KavrosTheme.ButtonLine;
            Style(dismiss);
            dismiss.Click += delegate { DismissReleaseNotes(); };
            notesBody = new TextBox { Name = "WhatsNewBody", Multiline = true, ReadOnly = true, WordWrap = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None, HideSelection = true, TabStop = false, Font = KavrosTheme.Ui(8.5F, false), BackColor = KavrosTheme.Bg, ForeColor = KavrosTheme.Text, Text = ReleaseNotes.Text() };
            notesHost.Controls.Add(heading);
            notesHost.Controls.Add(dismiss);
            notesHost.Controls.Add(notesBody);
            Controls.Add(notesHost);
        }
        void PlaceReleaseNotes(int top, int width)
        {
            notesHost.SetBounds(10, top, width, WhatsNewLayout.BannerHeight);
            Label heading = (Label)notesHost.Controls["WhatsNewHeading"];
            Control dismiss = notesHost.Controls["WhatsNewDismiss"];
            heading.SetBounds(WhatsNewLayout.Pad, 0, Math.Max(40, width - 96), WhatsNewLayout.Header);
            dismiss.SetBounds(width - 80, 4, 72, WhatsNewLayout.Header - 8);
            Rectangle body = WhatsNewLayout.Body(notesHost.Bounds);
            notesBody.SetBounds(body.X, body.Y, body.Width, body.Height);
            if (notesBody.Focused)
            {
                notesBody.SelectionStart = 0;
                notesBody.SelectionLength = 0;
            }
        }
        internal void ShowWithoutFocus()
        {
            if (Visible || IsDisposed) return;
            if (!Native.ShowFormNoActivate(this)) Show();
        }
        internal void DismissReleaseNotes()
        {
            prefs.SeenNotes = AppBranding.DisplayVersion;
            prefs.Save();
            notesOpen = false;
            LayoutMode();
        }
        internal void SyncReleaseNotes()
        {
            bool show = ReleaseNotes.IsNewer(prefs.SeenNotes, AppBranding.DisplayVersion);
            if (show == notesOpen) return;
            notesOpen = show;
            LayoutMode();
        }
        private void SelectOrigin(CompassPosition position)
        {
            selectingOrigin=true;positions.SelectedItem=position;positions.Text=position.Name;x.Value=Clamp((decimal)position.X,x);y.Value=Clamp((decimal)position.Y,y);radius.Value=Clamp((decimal)position.Radius,radius);selectingOrigin=false;originSelected=true;map.SelectedOrigin=position;RefreshCompass(false);
        }
        private static decimal Clamp(decimal value,NumericUpDown control){return Math.Max(control.Minimum,Math.Min(value,control.Maximum));}
        private CompassPosition SelectedPosition(){return new CompassPosition{Name=String.IsNullOrWhiteSpace(positions.Text)?"Custom position":positions.Text,X=(float)x.Value,Y=(float)y.Value,Radius=(double)radius.Value};}
        private void SavePosition()
        {
            CompassPosition p=SelectedPosition();p.Name=p.Name.Replace('\t',' ').Replace('\r',' ').Replace('\n',' ').Trim();if(p.Name.Length>80)p.Name=p.Name.Substring(0,80);
            if(session.Positions.Any(v=>v.Name==p.Name)&&!prefs.SavedPositions.Any(v=>v.Name==p.Name))p.Name+=" (Kavros)";
            CompassPosition old=prefs.SavedPositions.FirstOrDefault(v=>v.Name==p.Name);
            if(old!=null){prefs.SavedPositions.Remove(old);positions.Items.Remove(old);session.Positions.Remove(old);}
            prefs.SavedPositions.Add(p);session.Positions.Add(p);positions.Items.Add(p);positions.SelectedItem=p;prefs.Save();
        }
        private static void Style(Button b){b.FlatStyle=FlatStyle.Flat;b.BackColor=KavrosTheme.Button;b.ForeColor=KavrosTheme.Text;b.FlatAppearance.BorderColor=KavrosTheme.ButtonLine;b.Font=KavrosTheme.Ui(9F,false);}
        private static Button ButtonAt(Control parent,string text,int x,int y,int width,Action click){Button b=new OpaqueButton{Text=text,Left=x,Top=y,Width=width,Height=28};Style(b);parent.Controls.Add(b);b.Click+=delegate{click();};return b;}
        private static void LabelAt(Control parent,string text,int x,int y,int w,int h){parent.Controls.Add(new Label{Text=text,Left=x,Top=y,Width=w,Height=h,BackColor=KavrosTheme.Bg,ForeColor=KavrosTheme.Text});}
        public void SetPaused(bool value){pause.Text=value?"Resume":"Pause";}
        private string cardTitle="MASTER CLUE";
        private bool retained;
        public void SetRetained(bool value){if(retained!=value){retained=value;title.Invalidate();}if(value)ApplyHeading(mode==PuzzleKind.EmoteClue?cardTitle+" / SAVED":mode==PuzzleKind.Scan?scanSession.Region.Tier.ToUpperInvariant()+" SCAN / SAVED":headingText);}
        public void SetReading(string stage)
        {
            // The stage is still tracked for diagnostics and tests, but the
            // panel no longer shows a Searching/Reading strip for it.
            ReadingStage=stage;
        }
        public void SetWaiting(string text)
        {
            if(session.HasHistory){SetCompass(false);return;}
            mode=PuzzleKind.None;ApplyHeading("KAVROS / CLUE HELPER");summary.Text=text;LayoutMode();
        }
        public void SetPuzzle(DetectedPuzzle p,string text)
        {
            if(p.Kind==PuzzleKind.Compass){SetCompass(false);return;}
            if(p.Kind==PuzzleKind.Scan){scanSession.Observe(p.Scan);mode=PuzzleKind.Scan;ApplyHeading(p.Scan.Region.Tier.ToUpperInvariant()+" SCAN");RefreshScan();LayoutMode();return;}
            if(p.Kind==PuzzleKind.EmoteClue)cardTitle=p.Master.Tier.ToUpperInvariant()+" CLUE";
            mode=p.Kind;ApplyHeading(p.Kind==PuzzleKind.EmoteClue?cardTitle:p.Kind.ToString().ToUpperInvariant());summary.Text=text;
            if(p.Kind==PuzzleKind.Sliding)sliderGuide.SetTheme(p.Theme);
            if(p.Kind==PuzzleKind.EmoteClue){master.SetClue(p.Master,assets);if(displayedMasterId!=p.Master.Id){displayedMasterId=p.Master.Id;Invalidate(true);}}LayoutMode();
        }
        public void SetCompass(bool fit){mode=PuzzleKind.Compass;ApplyHeading("ELITE COMPASS");RefreshCompass(fit);LayoutMode();}
        private string clientName="",headingText="KAVROS / CLUE HELPER";
        public void SetClient(string name)
        {
            string next=name??"";
            if(next==clientName)return;
            clientName=next;
            ApplyHeading(headingText);
        }
        private void ApplyHeading(string heading)
        {
            headingText=heading??"";
            string text=FloatingPanelLayout.Header(clientName,headingText);
            if(title.Text!=text)title.Text=text;
        }
        public ScanRegion TrackingRegion {get{return mode==PuzzleKind.Scan&&scanSession.AutoTracking?scanSession.Region:null;}}
        public void RestartTracking(){if(TrackingRegion!=null){scanSession.SetAutomatic(true);RefreshScan();}}
        public void PrepareTrackingRecheck(){scanSession.PrepareRecheck();RefreshScan();}
        public void ApplyTracking(MinimapMatch match,int region,DateTime now)
        {if(TrackingRegion==null||TrackingRegion.Id!=region)return;scanSession.ApplyMatch(match,now);RefreshScan();}
        public void ExpireTracking(DateTime now){if(TrackingRegion==null)return;scanSession.Expire(now);RefreshScan();}
        private static string DaveRoute(string route)
        {
            if(string.IsNullOrEmpty(route)||route.IndexOf("Dave's spellbook",StringComparison.OrdinalIgnoreCase)<0)return route??"";
            var lines=new List<string>();
            foreach(string line in route.Replace("\r\n","\n").Split('\n'))
            {
                lines.Add(line);
                if(line.IndexOf("Dave's spellbook",StringComparison.OrdinalIgnoreCase)>=0&&line.IndexOf("bought from the Vote shop",StringComparison.OrdinalIgnoreCase)<0)
                    lines.Add("Dave's spellbook: bought from the Vote shop");
            }
            return string.Join("\n",lines.ToArray());
        }
        private void RefreshScan()
        {
            if(mode!=PuzzleKind.Scan||scanSession.Region==null)return;
            if(floorRegion!=scanSession.Region.Id)
            {
                changingFloor=true;scanFloor.Items.Clear();foreach(int level in scanSession.Region.Levels)scanFloor.Items.Add("Map level "+level);scanFloor.SelectedIndex=Array.IndexOf(scanSession.Region.Levels,scanSession.Region.Level);scanFloor.Visible=scanFloor.Items.Count>1;floorRegion=scanSession.Region.Id;changingFloor=false;
            }
            if(meerkat.Checked!=scanSession.Meerkat){changingMeerkat=true;meerkat.Checked=scanSession.Meerkat;changingMeerkat=false;}
            heading.Text=scanSession.Region.Name+" | "+scanSession.Range+" paces";
            heading.ForeColor=scanSession.DifferentLevel?KavrosTheme.Gold:KavrosTheme.Ice;
            // The route stays on the card. Too far away only adds a note to the status line.
            string guidance=DaveRoute(scanSession.Region.Route)+"\n"+scanSession.FloorLine();
            if(summary.Text!=guidance)summary.Text=guidance;
            string key=scanSession.Region.AssetKey+":"+scanSession.Range+":"+scanSession.Position+":"+scanSession.LastKnown+":"+scanSession.PositionTracked+":"+scanSession.AutoTracking+":"+scanSession.DifferentLevel;
            if(key!=scanPaintKey){scanPaintKey=key;scanMap.RefreshMap();}
        }
        public void RefreshCompass(bool fit)
        {
            if (mode != PuzzleKind.Compass) return;
            if(displayedCompassGeneration!=session.Generation)
            {
                displayedCompassGeneration=session.Generation;selectingOrigin=true;positions.SelectedIndex=-1;positions.Text="";x.Value=0;y.Value=0;radius.Value=1;selectingOrigin=false;
                originSelected=false;map.SelectedOrigin=null;map.Overview();
            }
            if(undoOrContinue!=null)undoOrContinue.Text=session.HasPrevious?"Continue":"Undo";
            bool stable=session.Stable(DateTime.UtcNow);
            if(mode==PuzzleKind.Compass)
            {
                if(session.Current!=null&&session.Current.Solved)heading.Text=session.Current.Heading;
                else
                {
                    string live=session.DirectionText;
                    heading.Text=live==null?"Waiting for a visible compass...":live+(stable?" | stable":" | waiting for a stable reading");
                }
            }
            if(mode==PuzzleKind.Compass)heading.ForeColor=KavrosTheme.Ice;
            if(mode==PuzzleKind.Compass)summary.Text=session.Summary;
            if(mode==PuzzleKind.Compass&&session.Candidates.Count==1&&session.Bearings.Count>=2&&!(session.Current!=null&&session.Current.Solved))
            {
                PointF target=session.Candidates[0];CompassPosition nearby=session.Positions.OrderBy(p=>CompassMath.Distance(p.Point,target)).FirstOrDefault();
                if(nearby!=null)summary.Text="Matching spot: "+target.X+", "+target.Y+". Verify X before digging.\nClosest listed: "+nearby.Name+" ("+Math.Round(CompassMath.Distance(nearby.Point,target))+" tiles).";
            }
            if(mode==PuzzleKind.Compass)originStatus.Text=originSelected?"Start: "+positions.Text:"Type a lodestone name or click its map icon.";
            record.Enabled=originSelected&&session.CanRecord(SelectedPosition(),DateTime.UtcNow);
            if(mode==PuzzleKind.Compass)map.SetSession(session,fit);
        }
        public static Rectangle ScanGuideBounds(){return new Rectangle(10,330,380,248);}
        public static Rectangle ScanControlBounds(int height){Rectangle guide=ScanGuideBounds();return new Rectangle(guide.Left,guide.Bottom+4,guide.Width,height);}
        public static int ScanFooterTop(int controlsHeight){Rectangle controls=ScanControlBounds(controlsHeight);return controls.Bottom+2;}
        private void LayoutMode()
        {
            bool compass=mode==PuzzleKind.Compass,scan=mode==PuzzleKind.Scan,emote=mode==PuzzleKind.EmoteClue,sliding=mode==PuzzleKind.Sliding,expanded=!prefs.Collapsed;
            Rectangle scanGuide=ScanGuideBounds(),scanControlsBox=ScanControlBounds(scanControls.Height);
            int bottom=compass?518:scan?ScanFooterTop(scanControls.Height):emote?442:sliding?292:102;
            bool showNotes=notesOpen&&expanded;
            int notesExtra=showNotes?WhatsNewLayout.PanelExtra:0;
            int bannerSpace=bannerShown?BannerHeight:0;
            int updateSpace=updateOffered?UpdateBarHeight+1:0;
            Size wanted=new Size(400,(prefs.Collapsed?32:bottom+38)+notesExtra+bannerSpace+updateSpace);
            bool changed=layoutKind!=mode||layoutCollapsed!=prefs.Collapsed||layoutBanner!=bannerShown||layoutNotes!=showNotes||layoutUpdate!=updateOffered||ClientSize!=wanted;
            if(!changed)return;
            SuspendLayout();
            try
            {
                // Resize and arrange the complete card in one layout pass.
                // Otherwise Windows can copy a footer's old pixels while the
                // clue card grows, leaving the half-painted duplicate reported.
                if(ClientSize!=wanted)ClientSize=wanted;
                master.Visible=expanded&&emote;sliderGuide.Visible=expanded&&sliding;map.Visible=compassControls.Visible=expanded&&compass;
                scanMap.Visible=scanControls.Visible=expanded&&scan;heading.Visible=expanded&&(compass||scan);
                summary.Visible=expanded&&!emote;footer.Visible=expanded;
                summary.Top=compass||scan?scanGuide.Top:38;summary.Height=scan?scanGuide.Height:56;
                if(scan){scanControls.SetBounds(scanControlsBox.X,scanControlsBox.Y,scanControlsBox.Width,scanControlsBox.Height);scanControls.BringToFront();}
                footer.Top=bottom;footer.BringToFront();
                if(notesHost!=null)
                {
                    notesHost.Visible=showNotes;
                    if(showNotes){PlaceReleaseNotes(footer.Bottom+WhatsNewLayout.PanelGap,380);notesHost.BringToFront();}
                }
                if(updateHost!=null)
                {
                    updateHost.Visible=updateOffered;
                    if(updateOffered)
                    {
                        int width=wanted.Width-2;
                        updateHost.SetBounds(1,wanted.Height-UpdateBarHeight-1,width,UpdateBarHeight);
                        updateDismiss.SetBounds(width-26,3,22,22);
                        updateButton.SetBounds(width-94,3,64,22);
                        updateText.SetBounds(8,0,Math.Max(40,updateButton.Left-12),UpdateBarHeight);
                        updateHost.BringToFront();
                    }
                }
                if(banner!=null)
                {
                    int bannerTop=wanted.Height-BannerHeight-1-(updateOffered?UpdateBarHeight+1:0);
                    banner.SetBounds(1,bannerTop,wanted.Width-2,BannerHeight);
                    banner.Visible=bannerShown;
                    if(bannerShown)banner.BringToFront();
                }
                layoutKind=mode;layoutCollapsed=prefs.Collapsed;layoutBanner=bannerShown;layoutNotes=showNotes;layoutUpdate=updateOffered;
            }
            finally{ResumeLayout(true);}
            Invalidate(true);
        }
        protected override bool ShowWithoutActivation{get{return true;}}
        internal bool StaysInactive{get{return ShowWithoutActivation;}}
        internal int Activations;
        protected override void OnActivated(EventArgs e){Activations++;base.OnActivated(e);}
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try{Native.SetTopMostNoActivate(Handle,true);}
            catch(DllNotFoundException){}
            catch(EntryPointNotFoundException){}
        }
        // A click still reaches the button or the map. It must not pull Kavros out of the foreground.
        protected override void WndProc(ref Message m)
        {
            if(m.Msg==0x21){base.WndProc(ref m);m.Result=new IntPtr(3);return;}
            base.WndProc(ref m);
        }
        protected override CreateParams CreateParams{get{CreateParams p=base.CreateParams;p.ExStyle|=Native.ExToolWindow|Native.ExNoActivate;return p;}}
        protected override void OnPaintBackground(PaintEventArgs e){using(Brush fill=new SolidBrush(BackColor))e.Graphics.FillRectangle(fill,ClientRectangle);}
        protected override void OnPaint(PaintEventArgs e)
        {
            // Paint every exposed pixel ourselves, including gaps around native
            // child controls. Parent/child compositor buffering left these gaps
            // showing the live game on the reported machine.
            using(Brush fill=new SolidBrush(BackColor))e.Graphics.FillRectangle(fill,ClientRectangle);
            using(Brush bar=new SolidBrush(KavrosTheme.BgRaise))e.Graphics.FillRectangle(bar,0,0,Width,title.Height);
            using(Pen rule=new Pen(KavrosTheme.LineSoft))e.Graphics.DrawLine(rule,0,title.Height-1,Width,title.Height-1);
            base.OnPaint(e);
            using(Pen p=new Pen(KavrosTheme.Frame))e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);
            using(Brush gold=new SolidBrush(KavrosTheme.Gold))e.Graphics.FillRectangle(gold,1,1,Width-2,2);
        }
    }
}
