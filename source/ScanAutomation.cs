using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
namespace KavrosClueHelper
{
    public sealed partial class PuzzleSuiteForm
    {
        private readonly BackgroundWorker trackingWorker=new BackgroundWorker();
        private MinimapPositionReader positionReader;
        private CancellationTokenSource trackingCancellation;
        private DateTime nextTracking=DateTime.MinValue,nextFullTracking=DateTime.MinValue;
        private Rectangle trackedMinimap;
        private int trackedRegion,trackingGeneration,trackedFloor,trackingMisses;
        private bool trackingAway;
        private IntPtr trackingWindow;
        private Size trackingClient;
        private sealed class TrackingJob
        {
            public Bitmap Image;public ScanRegion Region;public Point Offset;
            public int Generation,Floor;public DateTime CapturedAt;public MinimapMatch Match;
        }
        private void InitializeTracking()
        {
            positionReader=new MinimapPositionReader(assetPath);
            positionReader.UseInterface(preferences.LegacyInterface);
            trackingWorker.DoWork+=delegate(object sender,DoWorkEventArgs e)
            {
                TrackingJob job=(TrackingJob)e.Argument;var priority=Thread.CurrentThread.Priority;
                try{Thread.CurrentThread.Priority=ThreadPriority.BelowNormal;job.Match=positionReader.Find(job.Image,job.Region,job.Offset,trackingCancellation.Token);e.Result=job;}
                catch(OperationCanceledException){e.Cancel=true;}
                finally{job.Image.Dispose();Thread.CurrentThread.Priority=priority;}
            };
            trackingWorker.RunWorkerCompleted+=delegate(object sender,RunWorkerCompletedEventArgs e)
            {
                try
                {
                if(closing||IsDisposed)return;
                if(e.Cancelled||e.Error!=null){trackedMinimap=Rectangle.Empty;if(floatingPanel.TrackingRegion!=null)floatingPanel.ApplyTracking(null,floatingPanel.TrackingRegion.Id,DateTime.UtcNow);return;}
                TrackingJob job=(TrackingJob)e.Result;
                if(job.Generation!=trackingGeneration||job.Generation!=sourceGeneration||SelectedWindow!=trackingWindow)return;
                if(floatingPanel.TrackingRegion==null||floatingPanel.TrackingRegion.Id!=job.Region.Id)return;
                if(floatingPanel.TrackingRegion.Level!=job.Floor)return;
                // A delayed result never revives a stale player box.
                if(job.Match!=null&&(DateTime.UtcNow-job.CapturedAt).TotalMilliseconds>1500)
                {
                    // A slow result is not drawn as the live box. It stays the
                    // seed for the next search, and the crop stays wide enough
                    // to cover a short run.
                    trackedMinimap=Rectangle.Inflate(job.Match.Viewport,30,30);nextFullTracking=DateTime.UtcNow.AddSeconds(6);nextTracking=DateTime.MinValue;
                    positionReader.KeepSeed(job.Match);
                    floatingPanel.PrepareTrackingRecheck();return;
                }
                floatingPanel.ApplyTracking(job.Match,job.Region.Id,job.CapturedAt);
                if(job.Match!=null){trackingMisses=0;trackedMinimap=Rectangle.Inflate(job.Match.Viewport,30,30);}
                else if(++trackingMisses>=2)trackedMinimap=Rectangle.Empty;
                nextTracking=DateTime.UtcNow.AddMilliseconds(100);
                }
                catch(Exception ex){NoteFailure(ex);}
            };
        }
        private void StopTracking(){if(trackingCancellation!=null)trackingCancellation.Cancel();}
        private void TickTracking()
        {
            DateTime now=DateTime.UtcNow;floatingPanel.ExpireTracking(now);
            ScanRegion region=floatingPanel.TrackingRegion;
            if(!automatic.Checked||offline||region==null||SelectedWindow==IntPtr.Zero||!floatingPanel.Visible||!WindowLocator.IsForeground(SelectedWindow))
            {
                if(SelectedWindow!=IntPtr.Zero&&!WindowLocator.IsForeground(SelectedWindow))trackingAway=true;
                if(trackingWorker.IsBusy)StopTracking();
                trackedMinimap=Rectangle.Empty;return;
            }
            if(trackingAway){trackingAway=false;positionReader.ForceWide();trackedMinimap=Rectangle.Empty;}
            if(trackingWorker.IsBusy||now<nextTracking)return;
            if(Native.IsIconic(SelectedWindow))return;
            Rectangle client=Native.ClientBounds(SelectedWindow);
            Rectangle visible = CaptureSearch.VisibleClient(client, Native.VirtualScreen());
            if (visible.Width < 60 || visible.Height < 60) return;
            // Re-reading the clue does not restart tracking. Only a different
            // scan, floor, window or window size starts from scratch.
            bool reset=trackedRegion!=region.Id||trackedFloor!=region.Level||trackingWindow!=SelectedWindow||trackingClient!=client.Size;
            if(reset){positionReader.Reset();floatingPanel.RestartTracking();trackedMinimap=Rectangle.Empty;}
            trackedRegion=region.Id;trackedFloor=region.Level;trackingWindow=SelectedWindow;trackingClient=client.Size;trackingGeneration=sourceGeneration;
            bool full=trackedMinimap.IsEmpty||now>=nextFullTracking;
            Rectangle area=full?visible:Rectangle.Intersect(visible,trackedMinimap);
            if(area.Width<60||area.Height<60){trackedMinimap=Rectangle.Empty;return;}
            Rectangle screen=area;screen.Offset(client.Location);
            try
            {
                Bitmap frame=ScreenReader.Capture(screen);if(full)nextFullTracking=now.AddSeconds(6);
                foreach(System.Windows.Forms.Form surface in new System.Windows.Forms.Form[]{this,floatingPanel,notice})
                {
                    if(surface==null||!surface.Visible||!surface.IsHandleCreated)continue;
                    Rectangle overlap=Rectangle.Intersect(surface.Bounds,screen);
                    if(overlap.Width<1||overlap.Height<1)continue;
                    if(!GameVisibility.Any(overlap,delegate(Point point){return WindowLocator.AtPoint(surface.Handle,point,IntPtr.Zero);}))continue;
                    Rectangle cover=overlap;cover.Offset(-screen.X,-screen.Y);
                    using(Graphics g=Graphics.FromImage(frame))g.FillRectangle(Brushes.Black,cover);
                }
                foreach(Rectangle foreign in WindowLocator.ForeignCovers(screen))
                {
                    Rectangle cover=foreign;cover.Offset(-screen.X,-screen.Y);
                    if(cover.Width>0&&cover.Height>0)using(Graphics g=Graphics.FromImage(frame))g.FillRectangle(Brushes.Black,cover);
                }
                if(trackingCancellation!=null)trackingCancellation.Dispose();trackingCancellation=new CancellationTokenSource();
                nextTracking=now.AddMilliseconds(250);
                trackingWorker.RunWorkerAsync(new TrackingJob{Image=frame,Region=region,Floor=region.Level,Offset=area.Location,Generation=sourceGeneration,CapturedAt=DateTime.UtcNow});
            }
            catch(Exception e){trackedMinimap=Rectangle.Empty;nextTracking=now.AddMilliseconds(500);status.Text="Minimap capture: "+e.Message;}
        }
    }
}
