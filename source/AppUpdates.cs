using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using Velopack;
using Velopack.Exceptions;
using Velopack.Sources;

namespace KavrosClueHelper
{
    internal static class AppUpdates
    {
        internal const string RepositoryUrl = "https://github.com/Zenity-sys/kavros-clue-helper";
        internal static string RestartedVersion;
        internal static Action<string, bool, string, Action> Present;
        internal static Action<string> OfferPending;
        private static int busy;

        internal static void Start()
        {
                try { VelopackApp.Build().OnRestarted(delegate(SemanticVersion version) { RestartedVersion = version == null ? "" : AppBranding.Shown(version.ToString()); }).Run(); }
            catch (Exception ex) { CrashLog.Write(ex); }
        }

        internal static void Check(Form owner, bool interactive)
        {
            if (Interlocked.CompareExchange(ref busy, 1, 0) != 0)
            {
                if (interactive) Tell(owner, "An update check is already running.", false, null, null);
                return;
            }
            if (interactive) SetStatus(owner, "Checking GitHub for updates...");
            ThreadPool.QueueUserWorkItem(delegate
            {
                try { CheckWorker(owner, interactive); }
                finally { Interlocked.Exchange(ref busy, 0); }
            });
        }

        private static void CheckWorker(Form owner, bool interactive)
        {
            try
            {
                GithubSource source = new GithubSource(RepositoryUrl, null, false, null);
                UpdateManager manager = new UpdateManager(source, null, null);
                if (!manager.IsInstalled)
                {
                    if (interactive) Tell(owner, "This copy cannot update itself. Download Setup.exe from GitHub Releases.", true, null, null);
                    return;
                }
                UpdateInfo info = manager.CheckForUpdates();
                if (info == null || info.TargetFullRelease == null)
                {
                    if (interactive)
                    {
                        string current = manager.CurrentVersion == null ? AppBranding.DisplayVersion : AppBranding.Shown(manager.CurrentVersion.ToString());
                        Tell(owner, "You are on the latest version (" + current + ").", false, null, null);
                    }
                    return;
                }
                string version = AppBranding.Shown(info.TargetFullRelease.Version.ToString());
                Action<string> pending = OfferPending;
                if (pending != null)
                {
                    try { pending(version); }
                    catch (Exception ex) { CrashLog.Write(ex); }
                }
                Tell(owner, "Update ready. Click to restart.", false, "Restart", delegate
                {
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        try
                        {
                            Tell(owner, "Downloading version " + version + "...", false, null, null);
                            manager.DownloadUpdates(info, null);
                            PuzzleSuiteForm suite = owner as PuzzleSuiteForm;
                            if (suite != null && !suite.IsDisposed) suite.PrepareForUpdate();
                            manager.ApplyUpdatesAndRestart(info.TargetFullRelease, null);
                        }
                        catch (Exception ex) { Tell(owner, Describe(ex), true, null, null); }
                    });
                });
            }
            catch (NotInstalledException)
            {
                if (interactive) Tell(owner, "This copy cannot update itself. Download Setup.exe from GitHub Releases.", true, null, null);
            }
            catch (Exception ex)
            {
                if (interactive) Tell(owner, "Could not check GitHub for updates." + Environment.NewLine + Describe(ex), true, null, null);
            }
        }

        private static void Tell(Form owner, string message, bool error, string action, Action click)
        {
            Action show = delegate
            {
                if (Present != null) Present(message, error, action, click);
                else SetStatus(owner, message);
            };
            try
            {
                if (owner != null && !owner.IsDisposed && owner.InvokeRequired) owner.BeginInvoke(show);
                else show();
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        private static string Describe(Exception ex)
        {
            List<string> parts = new List<string>();
            Exception current = ex;
            while (current != null)
            {
                if (!string.IsNullOrEmpty(current.Message)) parts.Add(current.GetType().Name + ": " + current.Message);
                current = current.InnerException;
            }
            return string.Join(Environment.NewLine, parts.ToArray());
        }

        private static void SetStatus(Form owner, string message)
        {
            PuzzleSuiteForm suite = owner as PuzzleSuiteForm;
            if (suite == null || suite.IsDisposed) return;
            Action set = delegate { suite.SetUpdateStatus(message); };
            try
            {
                if (suite.InvokeRequired) suite.BeginInvoke(set);
                else set();
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

    }
}
