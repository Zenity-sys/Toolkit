using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace KavrosClueHelper
{
    public static class WindowRegressionTests
    {
        private static int assertions;
        private static void Check(bool condition, string message) { assertions++; if (!condition) throw new Exception("FAILED: " + message); }
        private static bool BorderedScan(Color border)
        {
            return BorderedScan(border, null);
        }
        private static bool BorderedScan(Color border, bool? legacy)
        {
            using (Bitmap image = new Bitmap(320, 360))
            {
                using (Graphics g = Graphics.FromImage(image))
                using (Brush frame = new SolidBrush(border))
                using (Brush paper = new SolidBrush(Color.FromArgb(210, 168, 110)))
                {
                    g.Clear(Color.FromArgb(24, 24, 26));
                    g.FillRectangle(frame, 30, 20, 180, 240);
                    g.FillRectangle(paper, 50, 40, 140, 200);
                }
                return ScrollAreas.Find(new Pixels(image), true, legacy).Count > 0;
            }
        }
        private static GameWindow Window(int handle, string title, string process, string className)
        { return new GameWindow { Handle = new IntPtr(handle), Title = title, ProcessName = process, ClassName = className, Client = new Rectangle(0, 0, 1920, 1080) }; }
        public static int Run(string report)
        {
            try
            {
                GameWindow browser = Window(1, "Browser", "chrome", "Chrome_WidgetWin_1");
                GameWindow launcher = Window(2, "com.kavros.launcher-siw", "KavrosLauncher", "SunAwtFrame");
                GameWindow game = Window(3, "Kavros 944 | NXT LIVE", "rs2client", "JagWindow");
                GameWindow oldHelper = Window(4, "Kavros Clue Helper | v0.2", "KavrosClueHelper", "WindowsForms10.Window");
                Check(WindowLocator.Choose(new[] { browser, launcher, oldHelper }, IntPtr.Zero, browser.Handle) == null, "no arbitrary window or launcher selected before game opens");
                Check(WindowLocator.Choose(new[] { browser, launcher, game, oldHelper }, IntPtr.Zero, browser.Handle) == game, "client opened after helper is selected");
                GameWindow replacement = Window(5, "KAVROS 944 | nxt live", "rs2client", "JagWindow");
                Check(WindowLocator.Choose(new[] { browser, launcher, replacement }, game.Handle, browser.Handle) == replacement, "restarted game reconnects by new handle");
                Check(WindowLocator.Choose(new[] { browser, launcher, game }, game.Handle, browser.Handle) == game, "foreground browser does not replace connected game");
                Check(WindowLocator.Choose(new[] { game, replacement }, game.Handle, replacement.Handle) == game, "foreground client does not replace the chosen client");
                Check(WindowLocator.Choose(new[] { game, replacement }, game.Handle, browser.Handle) == game, "existing valid selection remains stable without focus change");
                game.ProcessId = 44; replacement.ProcessId = 55;
                GameWindow restarted = Window(15, "Kavros 944 | NXT LIVE", "rs2client", "JagWindow"); restarted.ProcessId = 44;
                Check(WindowLocator.Choose(new[] { restarted, replacement }, game.Handle, 44, replacement.Handle) == restarted, "lost handle reattaches only to the same process");
                Check(WindowLocator.Choose(new[] { replacement }, game.Handle, 44, replacement.Handle) == replacement, "a closed client falls back to the foreground client");
                Check(WindowLocator.Choose(new[] { replacement }, game.Handle, 44, browser.Handle) == null, "a closed client does not adopt a client that is not in front");
                GameWindow third = Window(16, "Kavros 951 | NXT LIVE", "rs2client", "JagWindow"); third.ProcessId = 66;
                Check(WindowLocator.Choose(new[] { replacement, third }, game.Handle, 44, third.Handle) == third, "a closed client falls back to the foreground client, not the first");
                HashSet<IntPtr> wardenClient = new HashSet<IntPtr>();
                wardenClient.Add(third.Handle);
                Check(WindowLocator.Choose(new[] { replacement, third }, game.Handle, 44, third.Handle, wardenClient) == null, "a closed client does not fall back onto the client AFK Warden is using");
                Check(WindowLocator.Choose(new[] { game, replacement }, IntPtr.Zero, replacement.Handle) == replacement, "first selection may use the focused client");
                Check(WindowLocator.Score(replacement) >= 100, "case-insensitive client title recognition");
                GameWindow titledLauncher = Window(6, "Kavros", "KavrosLauncher", "SunAwtFrame");
                Check(WindowLocator.Score(titledLauncher) < 100, "process-only launcher hint excludes launcher");
                GameWindow sharedLauncherProcess = Window(7, "Kavros 944 | NXT LIVE", "KavrosLauncher", "JagWindow");
                Check(WindowLocator.Score(sharedLauncherProcess) >= 100, "real client may share launcher's process name");
                GameWindow processOnly = Window(8, "Game", "KavrosClient", "GLFW30");
                Check(WindowLocator.Score(processOnly) < 100, "a title without Kavros is not a client");
                GameWindow tiny = Window(9, "Kavros", "java", "Splash"); tiny.Client = new Rectangle(0, 0, 120, 80);
                Check(WindowLocator.Choose(new[] { game, tiny }, IntPtr.Zero, browser.Handle) == game, "game preferred over small splash window");
                GameWindow official = Window(10, "RuneScape", "rs2client", "JagWindow");
                Check(WindowLocator.Choose(new[] { game, official }, game.Handle, official.Handle) == game, "lower-confidence NXT process does not steal Kavros selection");
                Check(WindowLocator.Choose(new GameWindow[0], game.Handle, IntPtr.Zero) == null, "closed game cleanly disconnects");
                GameWindow discord = Window(11, "Kavros - Discord", "Discord", "Chrome_WidgetWin_1");
                GameWindow archive = Window(12, "KavrosClueHelper-v0.2.zip", "WinRAR", "WinRarWindow");
                GameWindow folder = Window(13, "KavrosClueHelper-v0.2", "explorer", "CabinetWClass");
                Check(WindowLocator.Choose(new[] { discord, archive, folder, launcher }, IntPtr.Zero, discord.Handle) == null, "Kavros mentions in Discord, archives and folders are not game windows");
                GameWindow notepad = Window(50, "Kavros.txt - Notepad++", "notepad++", "Notepad++"); notepad.ProcessId = 77;
                GameWindow firefox = Window(52, "Kavros - Mozilla Firefox", "firefox", "MozillaWindowClass");
                GameWindow edge = Window(53, "Kavros wiki - Microsoft Edge", "msedge", "Chrome_WidgetWin_1");
                GameWindow terminal = Window(55, "Kavros - Windows Terminal", "WindowsTerminal", "CASCADIA_HOSTING_WINDOW_CLASS");
                GameWindow editor = Window(57, "Kavros.cs - Visual Studio Code", "Code", "Chrome_WidgetWin_1");
                Check(WindowLocator.Score(notepad) < 100 && !WindowLocator.IsClient(notepad), "Notepad++ is not a Kavros client");
                Check(WindowLocator.Score(firefox) < 100 && WindowLocator.Score(edge) < 100 && WindowLocator.Score(terminal) < 100 && WindowLocator.Score(editor) < 100, "browsers, terminals and editors are not Kavros clients");
                GameWindow clientOne = Window(60, "Kavros 944 | NXT LIVE", "rs2client", "JagWindow"); clientOne.ProcessId = 100;
                GameWindow clientTwo = Window(61, "Kavros 951 | NXT LIVE", "rs2client", "JagWindow"); clientTwo.ProcessId = 200;
                GameWindow custom = Window(62, "Kavros", "KavrosClient", "GLFW30"); custom.ProcessId = 300;
                Check(WindowLocator.IsClient(clientOne) && WindowLocator.IsClient(clientTwo) && WindowLocator.IsClient(custom), "NXT clients and a Kavros client process stay clients");
                GameWindow autoFocused = WindowLocator.Choose(new[] { notepad, firefox, terminal, clientOne, clientTwo }, IntPtr.Zero, notepad.Handle);
                Check(autoFocused == clientOne || autoFocused == clientTwo, "auto capture keeps a real client while Notepad++ is in front");
                Check(WindowLocator.Choose(new[] { notepad, clientOne, clientTwo }, clientOne.Handle, 100, notepad.Handle) == clientOne, "focusing Notepad++ does not drop Client 1");
                Check(WindowLocator.Choose(new[] { notepad, clientOne }, notepad.Handle, 77, clientOne.Handle) == clientOne, "a Notepad++ latch releases when the client is in front");
                List<GameWindow> onlyClients = WindowLocator.Clients(new[] { notepad, firefox, edge, terminal, editor, folder, clientOne, clientTwo, custom });
                Check(onlyClients.Count == 3 && onlyClients.Contains(clientOne) && onlyClients.Contains(clientTwo) && onlyClients.Contains(custom), "the picker lists Client windows and skips Notepad++ and browsers");
                Check((Native.TopMostFlags & Native.SwpNoActivate) != 0, "raising the panel does not activate it");
                Check((Native.TopMostFlags & Native.SwpNoOwnerZOrder) != 0, "raising the panel does not move the game window");
                Check(FloatingPanelLayout.WantTopMost(true, false, true, false, false, false, false), "the panel is topmost while its own client is in front");
                Check(!FloatingPanelLayout.WantTopMost(true, false, false, false, false, false, false), "the panel drops back when another application is in front");
                Check(!FloatingPanelLayout.WantTopMost(true, false, false, true, false, false, false), "another client or helper in front does not keep the panel up");
                Check(FloatingPanelLayout.WantTopMost(true, false, false, false, true, false, false), "the panel stays up while it is being used");
                Check(!FloatingPanelLayout.WantTopMost(true, false, true, true, true, true, true), "Settings in front is not covered by a topmost panel");
                Check(!FloatingPanelLayout.WantTopMost(false, false, false, false, false, false, true), "the panel stays hidden until its own client is in front");
                Check(!FloatingPanelLayout.WantTopMost(true, true, true, true, true, false, true), "a minimised client never gets a topmost panel");
                Check(WindowLocator.IsHelperProcess("KavrosCompanion") && WindowLocator.IsHelperProcess("KavrosAfkWarden.exe") && WindowLocator.IsHelperProcess("kavrosslayerhelper") && WindowLocator.IsHelperProcess("KavrosClueHelper"), "Companion, AFK Warden, the Slayer helper and this helper count as friendly");
                Check(!WindowLocator.IsHelperProcess("Discord") && !WindowLocator.IsHelperProcess("chrome") && !WindowLocator.IsHelperProcess("KavrosLauncher") && !WindowLocator.IsHelperProcess("rs2client") && !WindowLocator.IsHelperProcess("kavrosclient"), "the launcher, the game and unrelated applications are not helpers");
                Check(!WindowLocator.IsHelperWindow(IntPtr.Zero), "a missing window is not a helper");
                Check(WindowLocator.ShouldEnumerate(true, true, false) == false, "a live chosen client is not enumerated on the timer");
                Check(WindowLocator.ShouldEnumerate(false, false, true), "a dead handle is rediscovered immediately");
                Check(WindowLocator.ShouldEnumerate(false, true, false), "the first client is sought on the slow timer");
                Check(WindowLocator.ShouldEnumerate(false, false, false) == false, "an empty search does not enumerate every tick");
                Check(FloatingPanelLayout.Visible(true, false, true, false, true, false, false), "the panel is visible while its own client is in front");
                Check(!FloatingPanelLayout.Visible(true, false, true, false, false, false, false), "another window in front hides the panel");
                Check(FloatingPanelLayout.Visible(true, false, true, false, false, true, false), "using the panel keeps it visible");
                Check(!FloatingPanelLayout.Shown(true, false, true, false, true, false, false, true), "a window covering the tracked client hides the panel");
                Check(!FloatingPanelLayout.Visible(true, false, true, true, true, false, false), "minimising the chosen client hides the panel");
                Check(!FloatingPanelLayout.Visible(true, true, true, false, true, false, false), "screenshot mode does not show the live panel");
                Rectangle bound = new Rectangle(0, 0, 1000, 800);
                Rectangle other = new Rectangle(1000, 0, 1000, 800);
                Rectangle stayed = FloatingPanelLayout.Place(bound, new Size(400, 300), new Point(1200, 40), new Rectangle(0, 0, 2000, 800));
                Check(stayed.Left >= bound.Left && stayed.Right <= bound.Right && stayed.Top >= bound.Top && stayed.Bottom <= bound.Bottom && !stayed.IntersectsWith(other), "dragging toward another client keeps the panel on the bound client");
                Check(FloatingPanelLayout.Place(new Rectangle(-1920, 0, 1920, 1080), new Size(400, 556), new Point(12, 72), new Rectangle(-1920, 0, 3840, 1080)).Location == new Point(-1908, 72), "the panel follows its own client on a negative monitor");
                Check(FloatingPanelLayout.Header("Zezima", "MASTER CLUE") == "Zezima  ·  MASTER CLUE" && FloatingPanelLayout.Header("Client 2", "ELITE COMPASS") == "Client 2  ·  ELITE COMPASS" && FloatingPanelLayout.Header("", "KAVROS / CLUE HELPER") == "KAVROS / CLUE HELPER", "the panel header names the client it is reading");
                GameWindow clientA = Window(30, "Kavros 944 | NXT LIVE", "rs2client", "JagWindow"); clientA.ProcessId = 100;
                GameWindow clientB = Window(31, "Kavros 944 | NXT LIVE", "rs2client", "JagWindow"); clientB.ProcessId = 200;
                GameWindow clientC = Window(32, "Kavros 951 | NXT LIVE", "rs2client", "JagWindow"); clientC.ProcessId = 300;
                GameWindow[] clients = new GameWindow[] { clientA, clientB, clientC, browser };
                LaunchOverlay panelB = new LaunchOverlay { Handle = new IntPtr(40), ProcessId = 7, ProcessName = "KavrosCompanion", Owner = clientB.Handle };
                LaunchOverlay panelC = new LaunchOverlay { Handle = new IntPtr(42), ProcessId = 7, ProcessName = "KavrosCompanion", Owner = clientC.Handle };
                LaunchOverlay wardenOnA = new LaunchOverlay { Handle = new IntPtr(41), ProcessId = 8, ProcessName = "KavrosAfkWarden", Owner = clientA.Handle, Below = clientA.Handle };
                LaunchOverlay[] overlays = new LaunchOverlay[] { panelC, panelB, wardenOnA };
                Check(WindowLocator.Choose(new[] { clientA, clientB, clientC }, clientB.Handle, 200, clientC.Handle) == clientB, "the bound client stays bound while its window is open");
                HashSet<IntPtr> taken = new HashSet<IntPtr>();
                taken.Add(clientA.Handle);
                Check(WindowLocator.Choose(new[] { clientA, clientB }, IntPtr.Zero, 0, clientA.Handle, taken) == clientB, "the first client is not the one AFK Warden is using");
                ClientBinding byHwnd = ClientLaunch.Resolve(new[] { "--hwnd", "32" }, clientA.Handle, clients, new uint[0], overlays);
                Check(byHwnd.Handle == clientC.Handle && byHwnd.ProcessId == 300, "a hwnd argument selects that client");
                Check(ClientLaunch.ShowPanelRequested(new[] { "--hwnd", "32", "--show-panel" }) && ClientLaunch.ShowPanelRequested(new[] { "--SHOW-PANEL" }), "the show-panel switch is recognised anywhere in the arguments");
                Check(!ClientLaunch.ShowPanelRequested(new string[0]) && !ClientLaunch.ShowPanelRequested(null) && !ClientLaunch.ShowPanelRequested(new[] { "--hwnd", "32", "--pid=200", "--show-panels" }), "without the switch the helper starts as it always did");
                Check(ClientLaunch.Resolve(new[] { "--show-panel", "--hwnd", "32" }, clientA.Handle, clients, new uint[0], overlays).Handle == clientC.Handle, "the show-panel switch does not change which client is bound");
                Check(Native.ShowPanelMessage != Native.NoticeMessage, "a second launch with the switch posts its own message, not the already-running notice");
                ClientBinding byHex = ClientLaunch.Resolve(new[] { "--hwnd", "0x1F" }, clientA.Handle, clients, new uint[0], overlays);
                Check(byHex.Handle == clientB.Handle, "a hex hwnd argument selects that client");
                Check(ClientLaunch.NoTrayRequested(new[] { "--hwnd", "32", "--no-tray" }) && ClientLaunch.NoTrayRequested(new[] { "--NO-TRAY" }), "the no-tray switch is recognised anywhere in the arguments");
                Check(!ClientLaunch.NoTrayRequested(new string[0]) && !ClientLaunch.NoTrayRequested(null) && !ClientLaunch.NoTrayRequested(new[] { "--hwnd", "32", "--pid=200", "--no-trays", "--notray" }), "without the switch the helper keeps its tray icon");
                Check(ClientLaunch.Resolve(new[] { "--no-tray", "--hwnd", "32" }, clientA.Handle, clients, new uint[0], overlays).Handle == clientC.Handle, "the no-tray switch does not change which client is bound");
                ClientBinding byPid = ClientLaunch.Resolve(new[] { "--pid=200" }, clientA.Handle, clients, new uint[0], overlays);
                Check(byPid.Handle == clientB.Handle && byPid.ProcessId == 200, "a pid argument selects that client's process");
                ClientBinding byOverlay = ClientLaunch.Resolve(new[] { "--hwnd", "40" }, IntPtr.Zero, clients, new uint[0], overlays);
                Check(byOverlay.Handle == clientB.Handle, "a companion hwnd binds to the client that owns the overlay");
                LaunchOverlay sitting = new LaunchOverlay { Handle = new IntPtr(43), ProcessId = 7, ProcessName = "KavrosCompanion", Below = clientB.Handle };
                ClientBinding byBelow = ClientLaunch.Resolve(new string[0], sitting.Handle, clients, new uint[0], new[] { sitting, wardenOnA });
                Check(byBelow.Handle == clientB.Handle, "the client under the companion overlay is used");
                ClientBinding bare = ClientLaunch.Resolve(new[] { "--hwnd", "40" }, IntPtr.Zero, clients, new uint[0], new[] { new LaunchOverlay { Handle = new IntPtr(40), ProcessId = 7, ProcessName = "KavrosCompanion" } });
                Check(bare.Handle == IntPtr.Zero, "the companion overlay itself is never the game window");
                ClientBinding wardenHwnd = ClientLaunch.Resolve(new[] { "--hwnd", "41" }, IntPtr.Zero, clients, new uint[0], overlays);
                Check(wardenHwnd.Handle == IntPtr.Zero, "an AFK Warden window is not the game window");
                ClientBinding byParent = ClientLaunch.Resolve(new string[0], panelB.Handle, clients, new uint[] { 7 }, overlays);
                Check(byParent.Handle == clientB.Handle, "the foreground companion panel selects the client it sits on");
                ClientBinding frontWins = ClientLaunch.Resolve(new string[0], clientC.Handle, clients, new uint[] { 7 }, new[] { panelB });
                Check(frontWins.Handle == clientC.Handle, "the foreground client wins over a companion overlay on another client");
                ClientBinding skip = ClientLaunch.Resolve(new string[0], clientA.Handle, clients, new uint[0], new[] { panelB, wardenOnA });
                Check(skip.Handle == clientB.Handle, "a companion overlay is used instead of the client AFK Warden is using");
                ClientBinding front = ClientLaunch.Resolve(new string[0], clientC.Handle, clients, new uint[0], new LaunchOverlay[0]);
                Check(front.Handle == clientC.Handle && front.ProcessId == 300, "the foreground client is used when Companion passes nothing");
                ClientBinding ambiguous = ClientLaunch.Resolve(new string[0], browser.Handle, clients, new uint[0], overlays);
                Check(ambiguous.Handle == IntPtr.Zero, "two companion overlays do not pick an arbitrary client");
                ClientBinding namedTaken = ClientLaunch.Resolve(new[] { "--hwnd", "30" }, clientB.Handle, clients, new uint[0], overlays);
                Check(namedTaken.Handle == clientA.Handle, "an explicit hwnd names its client");
                GameWindow companionWindow = Window(20, "Kavros Companion", "KavrosCompanion", "WindowsForms10.Window");
                GameWindow wardenWindow = Window(21, "Kavros AFK Warden", "KavrosAfkWarden", "WindowsForms10.Window");
                GameWindow slayerWindow = Window(22, "Kavros Slayer Helper", "KavrosSlayerHelper", "WindowsForms10.Window");
                Check(WindowLocator.Score(companionWindow) < 100, "Companion is not a game window");
                Check(WindowLocator.Score(wardenWindow) < 100, "AFK Warden is not a game window");
                Check(WindowLocator.Score(slayerWindow) < 100, "the Slayer helper is not a game window");
                Check(WindowLocator.Choose(new[] { clientB, companionWindow, wardenWindow }, IntPtr.Zero, companionWindow.Handle) == clientB, "a helper overlay in front does not become the client");
                GameWindow chip = Window(23, "AFK", "KavrosAfkWarden", "WindowsForms10.Window");
                chip.Client = new Rectangle(700, 0, 90, 36);
                GameWindow shell = Window(24, "Companion", "KavrosCompanion", "WindowsForms10.Window");
                shell.Client = new Rectangle(0, 0, 800, 600);
                clientB.Client = new Rectangle(0, 0, 800, 600);
                List<Rectangle> covers = WindowLocator.CoverRects(new[] { clientB, shell, chip }, new Rectangle(0, 0, 800, 600), 1);
                Check(covers.Count == 1 && covers[0].Width == 90, "a small helper overlay is masked and the game window is not");
                GameWindow behind = Window(25, "Companion", "KavrosCompanion", "WindowsForms10.Window");
                behind.Client = new Rectangle(100, 80, 140, 90);
                GameWindow ahead = Window(26, "Slayer Helper", "KavrosSlayerHelper", "WindowsForms10.Window");
                ahead.Client = new Rectangle(300, 40, 80, 40);
                List<Rectangle> frontOnly = WindowLocator.CoverRects(new[] { behind, ahead, clientB }, new Rectangle(0, 0, 800, 600), 1, delegate(GameWindow window, Rectangle overlap) { return window.Handle == ahead.Handle; });
                Check(frontOnly.Count == 1 && frontOnly[0].X == 300 && frontOnly[0].Width == 80, "a helper window behind the game is not blanked");
                clientB.Display = "Zezima";
                Check(WindowLocator.Choose(new[] { clientA, clientB, clientC }, clientB.Handle, 200, clientC.Handle) == clientB, "a character name does not change the bound client");
                Check(clientB.ToString() == "Zezima" && clientA.ToString() == clientA.Title, "the picker shows a character name and otherwise the window title");
                Dictionary<int, string> names = ClientIdentity.ParseShare("[{\"pid\":44,\"name\":\"Zezima\"},{\"pid\":55,\"name\":\"Draken\"},{\"pid\":1}]");
                Check(names.Count == 2 && names[44] == "Zezima" && names[55] == "Draken", "client names are read from Companion's list");
                Check(ClientIdentity.ParseShare("not json").Count == 0 && ClientIdentity.ParseShare("").Count == 0, "a broken client-name list is ignored");
                string nameFile = Path.Combine(Path.GetTempPath(), "kavros-client-names-" + DateTime.UtcNow.Ticks.ToString() + ".json");
                try
                {
                    File.WriteAllText(nameFile, "{\"pid\":44,\"name\":\"Zezima\"}");
                    DateTime stamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    File.SetLastWriteTimeUtc(nameFile, stamp);
                    ClientIdentity.ResetShareForTests(nameFile);
                    Check(ClientIdentity.Label(44, 0, "Kavros 944 | NXT LIVE") == "Zezima", "a saved Companion name is shown for that process");
                    Check(ClientIdentity.Label(0, 2, "Kavros") == "Client 3", "a client without a process id is Client N");
                    Check(ClientIdentity.Label(99, 4, "Kavros") == "Client 5", "a client with no saved name is Client N");
                    File.WriteAllText(nameFile, "{\"pid\":44,\"name\":\"Other\"}");
                    File.SetLastWriteTimeUtc(nameFile, stamp);
                    Check(ClientIdentity.Label(44, 0, "Kavros") == "Zezima", "the client-name list is reused until Companion writes it again");
                    File.SetLastWriteTimeUtc(nameFile, stamp.AddMinutes(1));
                    Check(ClientIdentity.Label(44, 0, "Kavros") == "Other", "a new client-name list replaces the saved names");
                }
                finally
                {
                    ClientIdentity.ResetShareForTests(null);
                    try { if (File.Exists(nameFile)) File.Delete(nameFile); } catch (Exception) { }
                }
                GameWindow[] mix = new GameWindow[] { browser, discord, archive, folder, launcher, oldHelper, companionWindow, wardenWindow, slayerWindow, clientA, clientB, clientC };
                List<GameWindow> shown = WindowLocator.Clients(mix);
                Check(shown.Count == 3 && shown[0] == clientA && shown[1] == clientB && shown[2] == clientC, "the picker lists only the Kavros clients");
                string pickerFile = Path.Combine(Path.GetTempPath(), "kavros-picker-names-" + DateTime.UtcNow.Ticks.ToString() + ".json");
                try
                {
                    File.WriteAllText(pickerFile, "{\"pid\":200,\"name\":\"Zezima\"}");
                    ClientIdentity.ResetShareForTests(pickerFile);
                    WindowLocator.NameClients(shown);
                    Check(shown[0].Display == "Client 1" && shown[1].Display == "Zezima" && shown[2].Display == "Client 3", "Client N counts only real clients and keeps a Companion name");
                    string boundName = WindowLocator.BoundName(shown[1], shown);
                    Check(boundName == "Zezima" && CaptureLabels.Auto(boundName) == "Auto - Zezima" && CaptureLabels.Found(boundName) == "Game found: Zezima" && FloatingPanelLayout.Header(boundName, "MASTER CLUE") == "Zezima  ·  MASTER CLUE", "auto, game found and the panel header name the bound client");
                    GameWindow unnamed = Window(33, "Kavros 944 | NXT LIVE", "rs2client", "JagWindow");
                    Check(WindowLocator.BoundName(unnamed, new GameWindow[] { unnamed }) == "Client 1" && CaptureLabels.Auto("Client 1") == "Auto - Client 1" && CaptureLabels.Found("Client 1") == "Game found: Client 1", "a client without a Companion name is Client N in auto and game found");
                    Check(CaptureLabels.Auto("") == "Auto - searching..." && CaptureLabels.Auto(null) == "Auto - searching..." && WindowLocator.BoundName(null, shown) == "", "auto says it is searching until a client is bound");
                }
                finally
                {
                    ClientIdentity.ResetShareForTests(null);
                    try { if (File.Exists(pickerFile)) File.Delete(pickerFile); } catch (Exception) { }
                }
                Check(BorderedScan(Color.FromArgb(28, 48, 86)) && BorderedScan(Color.FromArgb(96, 68, 38)) && !BorderedScan(Color.FromArgb(180, 30, 30)), "a scan accepts a modern blue border and a legacy brown border");
                Check(BorderedScan(Color.FromArgb(28, 48, 86), false) && !BorderedScan(Color.FromArgb(96, 68, 38), false), "modern mode keeps the blue scan border only");
                Check(BorderedScan(Color.FromArgb(96, 68, 38), true) && !BorderedScan(Color.FromArgb(28, 48, 86), true), "legacy mode keeps the brown scan border only");
                File.AppendAllText(report, "\r\nV0.2.1 WINDOW REGRESSIONS PASS: " + assertions + " assertions.\r\nCovers late launch, restart, launcher exclusion, renamed client, multiple windows and stable selection.\r\n");
                return 0;
            }
            catch (Exception ex) { File.AppendAllText(report, ex + "\r\n"); return 1; }
        }
    }
}
