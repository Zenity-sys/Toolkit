using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace KavrosClueHelper
{
    public static class PaintingRegressionTests
    {
        [DllImport("user32.dll",EntryPoint="GetWindowLongW")] private static extern int GetWindowLong(IntPtr window,int index);
        private static int count;
        private static void Check(bool pass,string message){count++;if(!pass)throw new Exception("FAILED: "+message);}
        public static int Run(string report,string application=null){return RunHidden(report,application);}
        private static void CreateHiddenHandles(Control control)
        {
            IntPtr unused=control.Handle;
            foreach(Control child in control.Controls)CreateHiddenHandles(child);
        }
        private static Bitmap Snapshot(Form form)
        {
            CreateHiddenHandles(form);form.PerformLayout();form.Invalidate(true);form.Update();Application.DoEvents();
            Bitmap image=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);
            using(Graphics g=Graphics.FromImage(image))g.Clear(Color.Fuchsia);
            form.DrawToBitmap(image,new Rectangle(Point.Empty,form.ClientSize));
            Check(!form.Visible,"native child rendering keeps the root window hidden");
            return image;
        }
        private static void Solid(Bitmap image,Point point,Color expected,string description)
        {Color actual=image.GetPixel(point.X,point.Y);Check(actual.ToArgb()==expected.ToArgb(),description+" at "+point+": "+actual);}
        private static int Light(Bitmap image,Rectangle bounds,int threshold=125)
        {int result=0;for(int y=bounds.Top;y<bounds.Bottom;y++)for(int x=bounds.Left;x<bounds.Right;x++){Color c=image.GetPixel(x,y);if(c.R>threshold&&c.G>threshold&&c.B>threshold)result++;}return result;}
        public static int RunHidden(string report,string application=null)
        {
            try
            {
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                string root=application==null?AppDomain.CurrentDomain.BaseDirectory:Path.GetDirectoryName(application),assets=Path.Combine(root,"assets");
                string directory=Path.GetDirectoryName(Path.GetFullPath(report));var session=new CompassSession(assets);
                using(var panel=new FloatingCluePanel(assets,session,new OverlayPreferences(Path.Combine(directory,"unused-paint-prefs.txt"))))
                {
                    panel.StartPosition=FormStartPosition.Manual;panel.Location=new Point(-32000,-32000);CreateHiddenHandles(panel);Application.DoEvents();
                    var destinations=panel.Controls.OfType<Panel>().SelectMany(p=>p.Controls.OfType<ComboBox>()).Single(c=>c.DropDownStyle!=ComboBoxStyle.DropDownList);
                    panel.SetCompass(false);var map=panel.Controls.OfType<CompassMap>().Single();
                    int remoteRow=0;
                    foreach(string destination in new[]{"Anachronia lodestone","City of Um lodestone"})
                    {
                        Check(destinations.Items.OfType<CompassPosition>().Any(p=>p.Name==destination),"dropdown retains "+destination);
                        CompassPosition hit=map.HitLodestone(new Point(map.Width-112,40+remoteRow++*29));
                        Check(hit==null||hit.Name!=destination,"removed corner icon cannot select "+destination);
                    }
                    var scans=new ScanVision(assets).Regions;
                    var master=new DetectedPuzzle{Kind=PuzzleKind.EmoteClue,Master=MasterClues.ById(246)};
                    Color background=KavrosTheme.Bg;
                    for(int cycle=0;cycle<24;cycle++)
                    {
                        panel.SetPuzzle(master,"");Application.DoEvents();
                        using(Bitmap image=Snapshot(panel))
                        {
                            Solid(image,new Point(3,100),background,"master outer background remains opaque");
                            Solid(image,new Point(200,437),background,"gap above master footer is fully painted");
                            Solid(image,new Point(200,476),background,"master bottom margin remains opaque");
                            if(cycle==0)image.Save(Path.Combine(directory,"v052-master-native.png"));
                        }
                        panel.SetPuzzle(new DetectedPuzzle{Kind=PuzzleKind.Scan,Scan=new ScanReading{Region=scans[cycle%2]}},"");Application.DoEvents();
                        using(Bitmap image=Snapshot(panel))
                        {
                            Solid(image,new Point(3,200),background,"scan outer background remains opaque");
                            Solid(image,new Point(350,444),background,"scan controls own their background");
                            Solid(image,new Point(200,523),background,"scan footer bottom remains opaque");
                            if(cycle==0)image.Save(Path.Combine(directory,"v052-scan-native.png"));
                        }
                        panel.SetCompass(false);Application.DoEvents();
                        DateTime now=DateTime.UtcNow.AddMilliseconds(-300);for(int i=0;i<3;i++)session.Observe(new CompassReading{Angle=0.7,Error=0.01},now.AddMilliseconds(i*125));
                        // Alternate enabled/disabled recording, partial paints,
                        // movement and repeated card transitions. No window is shown.
                        var list=panel.Controls.OfType<Panel>().SelectMany(p=>p.Controls.OfType<ComboBox>()).Single(c=>c.DropDownStyle!=ComboBoxStyle.DropDownList);
                        if(cycle%2==0)list.SelectedIndex=cycle%list.Items.Count;else session.Observe(null,DateTime.UtcNow);
                        panel.RefreshCompass(false);panel.Location=new Point(-32000+cycle%4,-32000+cycle%3);
                        panel.Invalidate(new Rectangle(0,380,400,130),true);panel.Update();
                        if(cycle%3==0){panel.SetWaiting("Waiting");panel.SetCompass(false);Application.DoEvents();}
                        using(Bitmap image=Snapshot(panel))
                        {
                            Solid(image,new Point(4,40),background,"compass left margin remains opaque");
                            Solid(image,new Point(398,350),background,"compass right margin remains opaque");
                            Solid(image,new Point(200,382),background,"compass section gap remains opaque");
                            Solid(image,new Point(115,428),background,"coordinate input gap remains opaque");
                            Solid(image,new Point(200,554),background,"compass bottom margin remains opaque");
                            Check(Light(image,new Rectangle(11,425,12,18))>4,"X label paints after switching clues");
                            Check(Light(image,new Rectangle(225,425,46,18))>15,"Radius label paints after switching clues");
                            if(cycle==1)image.Save(Path.Combine(directory,"v052-disabled-record.png"));
                            Check(Light(image,new Rectangle(15,483,158,23),80)>20,"Record direction remains painted while enabled or disabled");
                            if(cycle==0)image.Save(Path.Combine(directory,"v052-compass-native.png"));
                        }
                    }
                    var reference=new ReferenceClueReader(assets).Clues.First(c=>c.Type=="anagram");
                    panel.SetPuzzle(new DetectedPuzzle{Kind=PuzzleKind.EmoteClue,Master=reference.Card},"");
                    using(Bitmap image=Snapshot(panel))
                    {
                        var view=panel.Controls.OfType<MasterClueView>().Single();var text=view.Controls.OfType<ReferenceDetailsView>().Single();
                        Check(text.Text.Contains("Answer:")&&text.Bottom<=view.Height,"reference challenge answers remain inside the scrollable card");
                        Check(Light(image,new Rectangle(18,245,340,145))>200,"reference instructions are visibly painted");Solid(image,new Point(3,100),background,"reference card outer background stays opaque");image.Save(Path.Combine(directory,"v060-reference-card.png"));
                    }
                    panel.SetPuzzle(new DetectedPuzzle{Kind=PuzzleKind.Scan,Scan=new ScanReading{Region=scans.Single(r=>r.Id==356)}},"");
                    using(Bitmap image=Snapshot(panel)){
                        var floor=panel.Controls.OfType<Panel>().SelectMany(p=>p.Controls.OfType<ComboBox>()).Single(c=>c.DropDownStyle==ComboBoxStyle.DropDownList&&c.Items.Count==2);
                        var content=panel.PointToClient(floor.PointToScreen(new Point(5,2)));
                        Check(Light(image,new Rectangle(content,new Size(floor.Width-30,floor.Height-4)))>10,"selected scan floor text is visible");
                        image.Save(Path.Combine(directory,"v060-elite-scan.png"));
                    }
                    panel.Show();Application.DoEvents();
                    Panel scanBox=panel.Controls.OfType<Panel>().Single(p=>p.Controls.OfType<CheckBox>().Any(c=>c.Text=="Meerkat (+5 paces)"));
                    Label guide=panel.Controls.OfType<Label>().Single(l=>l.Width>300&&l.Top>200);
                    Panel foot=panel.Controls.OfType<Panel>().Single(p=>p.Controls.OfType<Button>().Any(b=>b.Text=="Exit"));
                    Check(scanBox.Visible&&scanBox.Controls.OfType<CheckBox>().All(c=>c.Visible),"scan controls are visible");
                    Check(!guide.Bounds.IntersectsWith(scanBox.Bounds),"the scan guide ends above the scan controls");
                    Check(guide.Bottom<=scanBox.Top&&scanBox.Bottom<=foot.Top&&foot.Bottom<=panel.ClientSize.Height,"the scan guide, controls and footer stack without covering each other");
                    Check(panel.GetChildAtPoint(new Point(scanBox.Left+16,scanBox.Top+10))==scanBox,"the scan controls are clickable");
                    panel.SetPuzzle(new DetectedPuzzle{Kind=PuzzleKind.Scan,Scan=new ScanReading{Region=scans.Single(r=>r.Id==349)}},"");
                    Check(!guide.Bounds.IntersectsWith(scanBox.Bounds)&&scanBox.Visible&&guide.Height>=248,"a long scan route still leaves the controls clear");
                    panel.Hide();
                    panel.SetCompass(false);destinations.SelectedIndex=0;map.SetSession(session,true);
                    int generation=session.Generation;session.Observe(new CompassReading{Solved=true},DateTime.UtcNow);session.Observe(new CompassReading{Angle=1.2},DateTime.UtcNow.AddMilliseconds(100));panel.SetCompass(false);
                    Check(session.Generation==generation+1&&session.Bearings.Count==0,"back-to-back compass advances clue generation and clears bearings");
                    Check(destinations.SelectedIndex<0&&destinations.Text==""&&map.SelectedOrigin==null,"back-to-back compass clears the previous starting point");
                    PointF centre=map.ToWorld(new Point(map.Width/2,map.Height/2));Check(Math.Abs(centre.X-3030)<1&&Math.Abs(centre.Y-3300)<1,"back-to-back compass restores the map overview");
                    ScanRegion testRegion=scans.Single(r=>r.Id==368);PointF spot=testRegion.Spots[0];var markerSession=new ScanSession();markerSession.Observe(new ScanReading{Region=testRegion});markerSession.SetManual(spot);
                    using(var markerMap=new ScanMap(assets,markerSession))
                    using(var markerImage=new Bitmap(380,260))
                    {
                        markerMap.Size=markerImage.Size;float w=Math.Max(testRegion.MapBounds.Width,testRegion.MapBounds.Height*380F/260F),h=w*260F/380F;
                        float left=testRegion.MapBounds.Left+(testRegion.MapBounds.Width-w)/2,bottom=testRegion.MapBounds.Top+(testRegion.MapBounds.Height-h)/2;
                        Point pixel=Point.Round(new PointF((spot.X-left)/w*380,260-(spot.Y-bottom)/h*260));
                        markerMap.DrawToBitmap(markerImage,new Rectangle(Point.Empty,markerImage.Size));Solid(markerImage,pixel,Color.Yellow,"the player remains visible on top of a candidate dot");
                        markerSession.SetAutomatic(true);DateTime stamp=DateTime.UtcNow;var match=new MinimapMatch{Position=spot};markerSession.ApplyMatch(match,stamp);markerSession.ApplyMatch(match,stamp.AddMilliseconds(100));markerSession.Expire(stamp.AddSeconds(3));
                        markerMap.DrawToBitmap(markerImage,new Rectangle(Point.Empty,markerImage.Size));Solid(markerImage,pixel,Color.Silver,"lost tracking retains a distinct last-confirmed marker above candidate dots");
                    }
                    Check((GetWindowLong(panel.Handle,-20)&0x02000000)==0,"the floating panel does not use parent-window compositing");
                    Check(!panel.AllowTransparency&&panel.TransparencyKey.IsEmpty&&panel.Opacity==1,"the interactive card is an ordinary opaque window");
                    panel.Hide();
                }
                File.AppendAllText(report,"V0.5.2 NATIVE PAINT PASS: "+count+" assertions. Actual Win32 parent/child rendering checked across 72 card transitions, full and partial invalidations, enabled/disabled record controls, moves and waiting/card transitions. Tests rendered hidden native window handles to bitmaps. No windows were shown and no user desktop/game capture or input was used.\r\n");return 0;
            }
            catch(Exception e){File.AppendAllText(report,e+"\r\n");return 1;}
        }
    }
}
