using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace KavrosClueHelper
{
    public static class MapIconRegressionTests
    {
        private static int assertions;
        private static void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new Exception("FAILED: " + message);
        }
        public static int Run(string report)
        {
            try
            {
                string assets = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
                if (!File.Exists(Path.Combine(assets, "compass", "world.png"))) assets = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "assets"));
                if (!File.Exists(Path.Combine(assets, "compass", "world.png"))) assets = "/workspace/assets";
                Check(File.Exists(Path.Combine(assets, "compass", "world.png")), "world map is available for the map tests");
                Rectangle steady = new Rectangle(10, 20, 200, 80);
                bool show, hide, move, paint, known; Rectangle placed;
                GuidanceDrawing.Present(false, false, Rectangle.Empty, true, steady, true, out show, out hide, out move, out paint, out known, out placed);
                Check(show && !hide && move && paint, "the first puzzle frame is drawn once");
                bool show2, hide2, move2, paint2, known2; Rectangle placed2;
                GuidanceDrawing.Present(true, known, placed, true, steady, false, out show2, out hide2, out move2, out paint2, out known2, out placed2);
                Check(!show2 && !hide2 && !move2 && !paint2, "an unchanged puzzle is not hidden, shown, moved, or cleared");
                float[] fourK = PuzzleDetector.SearchOrder(2160);
                Check(fourK.Length >= 12 && Math.Abs(fourK[0] - 2F) < 0.01F, "a 4K frame searches the 2x interface scale first");
                Check(PuzzleDetector.SearchOrder(1080)[0] == 1F, "a 1080p frame searches the native template first");
                Check(PuzzleDetector.ResolutionBucket(1369) == 1440 && PuzzleDetector.ResolutionName(1440) == "1440p", "a 2560x1369 window counts as 1440p");
                Check(Math.Abs(PuzzleDetector.SearchOrder(1369, 96)[0] - 1.25F) < 0.02F, "1440p at 100% searches the 1.25 template first");
                Check(PuzzleDetector.ResolutionBucket(1351) == 1440, "1351px is nearer 1440p than 1080p");
                Check(Math.Abs(PuzzleDetector.SearchOrder(1351, 96)[0] - 1.25F) < 0.02F, "1440p at 100% starts at 1.25");
                Check(Math.Abs(PuzzleDetector.SearchOrder(1351, 120)[0] - 1.75F) < 0.02F, "1440p at 125% searches 1.75 first");
                Check(Math.Abs(PuzzleDetector.SearchOrder(1351, 144)[0] - 2F) < 0.02F, "1440p at 150% searches 2x first");
                Check(Math.Abs(PuzzleDetector.SearchOrder(1351, 192)[0] - 2.5F) < 0.02F, "1440p at 200% searches 2.5 first");
                Check(Math.Abs(PuzzleDetector.SearchOrder(1369, 96, 1080, 100, 1440, 125)[0] - 1F) < 0.02F, "a manual 1080p choice overrides the window height");
                Check(Math.Abs(PuzzleDetector.SearchOrder(0, 0, 0, 0, 1440, 125)[0] - 1.75F) < 0.02F, "auto with no window uses the last manual 1440p and 125%");
                Check(AppBranding.Shown("0.8.0") == "v0.8" && AppBranding.Shown("v0.8.0") == "v0.8" && AppBranding.Shown(new Version(0, 8, 0, 0)) == "v0.8" && AppBranding.Shown("0.8.1") == "v0.8.1" && AppBranding.Shown("v0.8.1") == "v0.8.1" && AppBranding.Shown(new Version(0, 8, 1, 0)) == "v0.8.1" && AppBranding.Shown("0.9.0") == "v0.9" && AppBranding.Shown(new Version(0, 9, 0, 0)) == "v0.9" && AppBranding.Shown("0.9.1") == "v0.9.1" && AppBranding.Shown(new Version(0, 9, 1, 0)) == "v0.9.1" && AppBranding.Shown("0.9.2") == "v0.9.2" && AppBranding.Shown(new Version(0, 9, 2, 0)) == "v0.9.2" && AppBranding.Shown("0.9.3") == "v0.9.3" && AppBranding.Shown(new Version(0, 9, 3, 0)) == "v0.9.3" && AppBranding.Shown("0.9.4") == "v0.9.4" && AppBranding.Shown(new Version(0, 9, 4, 0)) == "v0.9.4" && AppBranding.Shown("0.9.5") == "v0.9.5" && AppBranding.Shown(new Version(0, 9, 5, 0)) == "v0.9.5" && AppBranding.Shown("0.9.6") == "v0.9.6" && AppBranding.Shown(new Version(0, 9, 6, 0)) == "v0.9.6" && AppBranding.Shown("0.9.7") == "v0.9.7" && AppBranding.Shown(new Version(0, 9, 7, 0)) == "v0.9.7" && AppBranding.Shown("0.9.8") == "v0.9.8" && AppBranding.Shown(new Version(0, 9, 8, 0)) == "v0.9.8" && AppBranding.Shown("0.9.9") == "v0.9.9" && AppBranding.Shown(new Version(0, 9, 9, 0)) == "v0.9.9" && AppBranding.Shown("0.9.10") == "v0.9.10" && AppBranding.Shown(new Version(0, 9, 10, 0)) == "v0.9.10" && AppBranding.Shown("0.9.11") == "v0.9.11" && AppBranding.Shown(new Version(0, 9, 11, 0)) == "v0.9.11" && AppBranding.Shown("0.9.12") == "v0.9.12" && AppBranding.Shown(new Version(0, 9, 12, 0)) == "v0.9.12" && AppBranding.Shown("0.9.13") == "v0.9.13" && AppBranding.Shown(new Version(0, 9, 13, 0)) == "v0.9.13" && AppBranding.Shown("0.9.14") == "v0.9.14" && AppBranding.Shown(new Version(0, 9, 14, 0)) == "v0.9.14", "a zero patch shows major.minor and any other patch shows three parts");
                Rectangle scanGuide = FloatingCluePanel.ScanGuideBounds();
                Rectangle scanControls = FloatingCluePanel.ScanControlBounds(88);
                int scanFooter = FloatingCluePanel.ScanFooterTop(88);
                Check(!scanGuide.IntersectsWith(scanControls) && scanGuide.Bottom <= scanControls.Top, "the scan guide ends above the scan controls");
                Check(scanControls.Bottom <= scanFooter, "the scan footer sits below the scan controls");
                string prefsPath = Path.Combine(Path.GetTempPath(), "kavros-overlay-" + DateTime.UtcNow.Ticks.ToString() + ".txt");
                try
                {
                    var prefs = new OverlayPreferences(prefsPath);
                    Check(!prefs.LegacyInterface && !prefs.DarkscapeInterface && prefs.Resolution == 0 && prefs.Scaling == 0 && prefs.ResolutionManual == 1080 && prefs.ScalingManual == 100, "settings default to Modern and Auto");
                    prefs.LegacyInterface = true;
                    prefs.Resolution = 1440;
                    prefs.Scaling = 125;
                    prefs.ResolutionManual = 1440;
                    prefs.ScalingManual = 125;
                    prefs.Save();
                    var loaded = new OverlayPreferences(prefsPath);
                    Check(loaded.LegacyInterface && !loaded.DarkscapeInterface && loaded.Resolution == 1440 && loaded.Scaling == 125 && loaded.ResolutionManual == 1440 && loaded.ScalingManual == 125, "interface, resolution and scaling round-trip in overlay.txt");
                    loaded.LegacyInterface = false;
                    loaded.DarkscapeInterface = true;
                    loaded.Save();
                    var dark = new OverlayPreferences(prefsPath);
                    Check(!dark.LegacyInterface && dark.DarkscapeInterface, "DarkScape is saved on its own");
                    File.WriteAllText(prefsPath, "interface\tlegacy\r\n");
                    Check(new OverlayPreferences(prefsPath).LegacyInterface && !new OverlayPreferences(prefsPath).DarkscapeInterface, "a saved Legacy choice stays Legacy");
                    File.WriteAllText(prefsPath, "interface\tmodern\r\n");
                    OverlayPreferences modern = new OverlayPreferences(prefsPath);
                    Check(!modern.LegacyInterface && !modern.DarkscapeInterface, "a saved Modern choice stays Modern");
                }
                finally { try { if (File.Exists(prefsPath)) File.Delete(prefsPath); } catch (IOException) { } }
                string preferred = LodestoneIcons.Resolve(assets, "Lumbridge lodestone");
                Check(preferred != null && preferred.Replace('\\', '/').Contains("/map-icons/lodestones/lumbridge.png"), "lodestone icons prefer assets/map-icons/lodestones");
                string compass = LodestoneIcons.CompassFile(assets);
                Check(compass.Replace('\\', '/').EndsWith("/map-icons/compass.png"), "compass image path is assets/map-icons/compass.png");
                Check(File.Exists(compass), "compass icon is installed");
                using (Bitmap icon = new Bitmap(compass))
                {
                    Check(icon.GetPixel(0, 0).A == 0 && icon.GetPixel(icon.Width - 1, icon.Height - 1).A == 0, "compass outside the circle is transparent");
                    Check(icon.GetPixel(icon.Width / 2, icon.Height / 2).A == 255, "compass centre is opaque");
                }
                using (Bitmap canvas = new Bitmap(80, 40))
                using (Graphics g = Graphics.FromImage(canvas))
                    LodestoneIcons.DrawCompass(g, SystemFonts.DefaultFont, new Rectangle(0, 0, 80, 40), Path.Combine(Path.GetTempPath(), "kavros-no-compass"));
                foreach (string line in File.ReadAllLines(Path.Combine(assets, "compass", "positions.tsv")))
                {
                    string[] fields = line.Split('\t');
                    if (fields.Length < 3 || fields[0].Length == 0) continue;
                    string file = LodestoneIcons.Resolve(assets, fields[0]);
                    Check(file != null && file.Replace('\\', '/').Contains("/map-icons/lodestones/"), "icon file for " + fields[0]);
                }
                Rectangle box = new Rectangle(8, 43, 444, 139);
                float span = Math.Max(72, Math.Max(20, 28) * 3);
                PointF screen;
                bool lumbridge = LodestoneIcons.InView(box, 3212, 3219, span, 0, 0, 3233, 3221, out screen);
                Check(lumbridge, "Lumbridge lodestone is inside the Lumbridge Castle grounds view");
                bool lunar = LodestoneIcons.InView(box, 3212, 3219, span, 0, 0, 2088, 3911, out screen);
                Check(!lunar, "Lunar Isle lodestone is outside the Lumbridge Castle grounds view");
                MasterClue charm = MasterClues.ById(240);
                Check(charm.Travel.StartsWith("Fastest: Traveller's necklace > The Outpost") && charm.Travel.Contains("Ctrl+T > Monsters > Rune dragons") && charm.Travel.Contains("Ctrl+T > Skilling > Agility > 1/85 - Gnome Course") && charm.Travel.IndexOf("farming", StringComparison.OrdinalIgnoreCase) < 0 && charm.Travel.IndexOf("surge", StringComparison.OrdinalIgnoreCase) < 0 && charm.Travel.IndexOf("bladed dive", StringComparison.OrdinalIgnoreCase) < 0, "charm sprite route");
                string pit = MasterClues.ById(233).Travel;
                Check(pit.StartsWith("Fastest: Ctrl+T > Locations > Fight Caves") && pit.Contains("Final step: Run south") && pit.Contains("Clue: Cheer. Wear Fire cape, Toktz-ket-xil, Spork.") && pit.IndexOf("Alternatively:", StringComparison.Ordinal) < 0 && pit.IndexOf("fairy", StringComparison.OrdinalIgnoreCase) < 0, "sulphur pit route");
                using (MasterClueView view = new MasterClueView())
                {
                    view.SetBounds(0, 0, 460, 480);
                    view.SetClue(charm, assets);
                    Check(view.MapPath.Replace('\\', '/').EndsWith("/compass/world.png"), "master emote map is the world map");
                    Check(view.MapPath.IndexOf("master-maps", StringComparison.OrdinalIgnoreCase) < 0, "master emote map is not the local square");
                    view.SetClue(MasterClues.ById(233), assets);
                    Check(view.MapPath.Replace('\\', '/').EndsWith("/location-maps/233.jpg"), "sulphur pit uses its dungeon map");
                }
                var routes = File.ReadAllLines(Path.Combine(assets, "data", "scan-routes.tsv")).Select(l => l.Split('\t')).Where(f => f.Length == 2).ToDictionary(f => int.Parse(f[0]), f => f[1]);
                Check(routes[358] == "Not available on Kavros.", "Mos Le'Harmless is not available on Kavros");
                Check(routes[371].Contains("Fastest: Beast tab (F3, or click the Adventures tab) > Telos") && routes[371].Contains("Alternatively: Ctrl+T > Locations > Nardah") && routes[371].Contains("Third option: Fairy ring DLQ") && routes[371].Contains("Tip: make a portable fairy ring in ;;ig, or buy an unlimited one for 5 Kavros dollars."), "Heart of Gielinor route");
                Check(routes[368].StartsWith("Fastest: ;;prif") && routes[368].Contains("Alternatively: Prifddinas lodestone."), "Prifddinas scan leads with ;;prif");
                Check(routes[363].Contains("Monsters > Yaks") && routes[363].Contains("rope bridge") && routes[363].IndexOf("boat", StringComparison.OrdinalIgnoreCase) < 0, "Jatizso route uses the yak pen");
                foreach (MasterClue clue in MasterClues.All)
                {
                    Check(clue.Travel.Contains("Fastest:") && !clue.Travel.Contains("[VERIFY]") && !clue.Travel.Contains("Note:"), "master route " + clue.Id);
                    if (clue.Travel.IndexOf("Fairy ring", StringComparison.OrdinalIgnoreCase) >= 0)
                        Check(clue.Travel.Contains("Tip: make a portable fairy ring in ;;ig, or buy an unlimited one for 5 Kavros dollars."), "fairy tooltip on master " + clue.Id);
                }
                foreach (var pair in routes)
                {
                    if (pair.Key == 358) continue;
                    Check(pair.Value.Contains("Fastest:") && !pair.Value.Contains("[VERIFY]") && !pair.Value.Contains("Note:"), "scan route " + pair.Key);
                    if (pair.Value.IndexOf("Fairy ring", StringComparison.OrdinalIgnoreCase) >= 0 || pair.Value.IndexOf("fairy ring", StringComparison.OrdinalIgnoreCase) >= 0)
                        Check(pair.Value.Contains("Tip: make a portable fairy ring in ;;ig, or buy an unlimited one for 5 Kavros dollars."), "fairy tooltip on scan " + pair.Key);
                }
                var vision = new ScanVision(assets);
                ScanRegion mos = vision.Regions.Single(r => r.Id == 358);
                Check(mos.Route == "Not available on Kavros.", "loaded Mos Le'Harmless route");
                Check(vision.Regions.Single(r => r.Id == 368).Route.StartsWith("Fastest: ;;prif"), "loaded Prifddinas route leads with ;;prif");
                Check(vision.MatchEliteText("Scan this area. The orb is in range. 27 paces around Mos Le'Harmless.") == null, "the reader does not offer Mos Le'Harmless");
                ScanRegion falador = vision.MatchEliteText("Scan this area. The orb is in range. 22 paces around Falador.");
                Check(falador != null && falador.Id == 364, "other elite scans are still offered");
                string scroll = "MYSTERIOUS CLUE SCROLL This scroll will work in Prifddinas. Orb scan range: 30 paces.";
                string parchment = "Prifddinas / The orb glows as you scan. You are in range of the coordinate! The coordinate is 26 paces away.";
                ScanRegion prifScroll = vision.MatchEliteText(scroll);
                Check(prifScroll != null && prifScroll.Id == 368 && vision.MatchScrollTranscript(scroll).Id == 368, "mysterious clue scroll text is Prifddinas at 30 paces");
                Check(vision.MatchEliteText(parchment) == null && vision.MatchScrollTranscript(parchment) != null && vision.MatchScrollTranscript(parchment).Id == 368, "side parchment text is Prifddinas");
                ScanRegion prifMeerkat = vision.MatchEliteText("This scroll will work in Prifddinas. Orb scan range: 35 paces.");
                Check(prifMeerkat != null && prifMeerkat.Id == 368 && ScanReading.FromText(prifMeerkat, "Orb scan range: 35 paces.").Meerkat, "Prifddinas meerkats use 35 paces");
                ScanRegion heart = vision.MatchEliteText("This scroll will work in the Heart of Gielinor. Orb scan range: 49 paces.");
                Check(heart != null && heart.Id == 371, "Heart of Gielinor scroll text is 49 paces");
                ScanRegion heartMeerkat = vision.MatchEliteText("Scan the Heart of Gielinor. The orb is in range. 54 paces.");
                Check(heartMeerkat != null && heartMeerkat.Id == 371 && ScanReading.FromText(heartMeerkat, "54 paces").Meerkat, "Heart of Gielinor meerkats use 54 paces");
                Check(vision.MatchScrollTranscript("Heart of Gielinor. The orb glows as you scan. You are in range of the coordinate! The coordinate is 12 paces away.").Id == 371, "Heart of Gielinor side parchment is recognised");
                Check(vision.MatchEliteText("This scroll will work in Prifddinas.") == null && vision.MatchScrollTranscript("This scroll will work in Prifddinas.") == null, "a place name without paces is not a scan");
                Check(vision.MatchEliteText("Orb scan range: 30 paces.") == null && vision.MatchScrollTranscript("The orb glows as you scan. Orb scan range: 30 paces.") == null, "paces without a place name are not a scan");
                ScanRegion menaphos = vision.MatchEliteText("Scan this area. The orb is in range. 30 paces around Menaphos.");
                Check(menaphos != null && menaphos.Id == 354, "Menaphos keeps the 30 pace elite scan");
                foreach (string shot in new[] { "prif-scan-small-window.png", "prif-scan-side-parchment.png" })
                {
                    string shotPath = Path.Combine(assets, "test_assets", "reported", shot);
                    Check(File.Exists(shotPath), "reported scan screenshot " + shot);
                    using (Bitmap image = new Bitmap(shotPath))
                    {
                        DetectedPuzzle found = vision.Find(image, new Pixels(image));
                        if (found != null) Check(found.Scan != null && found.Scan.Region != null && found.Scan.Region.Id == 368, shot + " resolves to Prifddinas");
                        else
                        {
                            string read = vision.LastText ?? "";
                            Check(read.IndexOf("prifddinas", StringComparison.OrdinalIgnoreCase) < 0, shot + " text was read but not matched: " + read);
                        }
                    }
                }
                var clues = new ReferenceClueReader(assets);
                string gnome = clues.Clues.Single(c => c.Card.Id == 122).Card.Travel;
                Check(gnome.Contains("Challenge: What is 57 x 89 + 23?") && gnome.Contains("Answer: 5096"), "gnomeball referee challenge answer");
                Check(clues.MatchText("R AK MI").Card.Id == 335, "R AK MI is Karim");
                Check(clues.MatchText("IMKROM").Card.Id == 340, "IM KROM is Rommik");
                Check(clues.MatchText("Beckon at Varrock Dig Site, near the eastern winch. Bow or curtsy before you talk to me. Have nothing equipped at all when you do.").Card.Id == 7, "eastern winch beckon");
                Check(clues.MatchText("Cheer in the Ogre pen in the Training Camp. Show you are angry before you talk to me. Equip a Green Dragonhide Body, green dragonhide chaps and a steel sq shield.").Card.Id == 256, "ogre pen cheer");
                Check(clues.MatchText("Brush off the sand and dig in the quarry. There is a wheely handy barrow to the east. Don't worry, permission has been granite to dig there.").Card.Id == 217, "granite quarry");
                string khazard = clues.Clues.Single(c => c.Card.Id == 96).Card.Travel;
                Check(khazard.Contains("Lunar spellbook > Khazard Teleport") && khazard.Contains("Alternatively: Yanille lodestone.") && khazard.Contains("Ardougne Monastery") && khazard.Contains("Run east to Port Khazard and search the crate near the cart."), "Port Khazard card");
                string blades = "Four blades I have, yet draw no blood; Still I turn my prey to powder. If you are brave, come search my roof; It is there my blades are louder.";
                Check(clues.MatchText(blades).Card.Id == 173, "four blades is the Lumbridge windmill");
                Check(clues.MatchText("Four blades I have, yet draw no blood, Still I turn my prey to powder. If you are brave, come search my roof, It is there my blades are louder.").Card.Id == 173, "comma wording still matches the windmill");
                string wrapped = "MYSTERIOUS CLUE SCROLL\nFour blades I have, yet draw no\nblood; Still I turn my prey to powder.\nIf you are brave, come search my roof;\nIt is there my blades are louder.";
                Check(clues.MatchText(wrapped).Card.Id == 173, "wrapped windmill lines and the scroll title still match");
                string noisy = "Four biades I have, yet draw no b1ood; Still I turn my prey to powder. If you are brave, come search my roof; It is there my blades are louder.";
                Check(clues.MatchText(noisy).Card.Id == 173, "ocr noise still matches the windmill");
                string mill = clues.Clues.Single(c => c.Card.Id == 173).Card.Travel;
                Check(mill.Contains("Fastest: Combat bracelet > Champions' Guild, then run south-west to the windmill.") && mill.Contains("Alternatively: Draynor Village lodestone, then run east to the windmill.") && mill.Contains("Third option: Quest cape > Champions' Guild, then run south-west to the windmill.") && mill.Contains("Final step: Climb to the top floor and search the crates.") && mill.Contains("Mill Lane Mill, between Lumbridge and Draynor") && mill.IndexOf("Tile ", StringComparison.Ordinal) < 0 && mill.IndexOf("boat", StringComparison.OrdinalIgnoreCase) < 0 && mill.IndexOf("RuneApps", StringComparison.OrdinalIgnoreCase) < 0 && mill.IndexOf("Jagex", StringComparison.OrdinalIgnoreCase) < 0, "windmill card");
                string[] routeChecks = new[] {
                    "293|Amulet of farming > Brimhaven fruit tree patch|Karamja lodestone, run north-east|",
                    "423|Ctrl+T > Skilling > Woodcutting > Overgrown Idol (Karamja)|Amulet of glory > Karamja, walk south down the east coast|",
                    "418|Enlightened amulet > Bandit Camp, run north-west|Bandit Camp teleport scroll (from clues or the GE)|Fairy ring",
                    "32|Combat bracelet > Ranging Guild|Enter the guild and search the large haystack by the southern fence|",
                    "296|Modern spellbook > Watchtower Teleport, run south-east to the guild|3. Yanille Hops Patch, run south-east|Yanille lodestone",
                    "229|Ctrl+T > Slayer > Corrupted creatures, run north|Menaphos lodestone, run north-east|",
                    "458|Amulet of farming > Wilderness herb patch|Carrallangar Teleport (Graveyard of Shadows), run north|Annakarl",
                    "292|right-click the beam and choose bottom floor|Fairy ring DIS (Wizards' Tower)|Traveller's necklace",
                    "235|Ancient spellbook > Dareeyak Teleport, run east|3. Level 27|Wilderness Crater",
                    "169|Ring of respawn > Lumbridge|Talk to Hans as he walks around the castle|",
                    "216|Wilderness herb patch, run north|Ctrl+T > Slayer > Abyssal Lords, run north-east|Annakarl|Wilderness Course",
                    "161|Ring of respawn > Lumbridge|Search the crate in the south-east corner of the cellar|",
                    "156|Clocktower teleport (from clue scrolls only), run north a few steps|Fairy ring BIS (Ardougne Zoo)|Ardougne lodestone|GE",
                    "433|Modern spellbook > Taverley Teleport, run south-east|Taverley lodestone, run south-east|",
                    "39|Attuned crystal teleport seed > Iorwerth|Use the stairs to floor 1 above ground|",
                    "153|Ctrl+T > Skilling > Mining > Necrite (70), run south-east|Fairy ring DLQ|Dominion Tower",
                    "162|Bandit Camp teleport scroll (from clues or the GE), run north-west|Enter the general store and search the crates inside|",
                    "450|Modern spellbook > Trollheim Teleport|Dig immediately south of Mad Eadgar's cave entrance|Burthorpe",
                    "147|Ctrl+T > Skilling > Mining > Coal|Skills necklace > Mining Guild, run north-east|Dungeoneering",
                    "141|Dig site pendant > Exam Centre|Senntisten Teleport (Dig Site temple), run east|",
                    "451|Modern spellbook > Trollheim Teleport|Dig on the south side of the entrance|Burthorpe",
                    "149|right-click the beam and choose bottom floor|Fairy ring DIS (Wizards' Tower), run north|",
                    "148|Ctrl+T > Skilling > Archaeology > Stormguard Citadel|Elder tree (Legends' Guild), run north-west|Fairy ring",
                    "165|Amulet of glory > Draynor Village, run north-east|Ctrl+T > Skilling > Woodcutting > Willow|Falador Herb"
                };
                foreach (string row in routeChecks)
                {
                    string[] part = row.Split('|');
                    string travel = clues.Clues.Single(c => c.Card.Id == Int32.Parse(part[0])).Card.Travel;
                    int ways = 0;
                    bool fairy = false;
                    foreach (string line in travel.Replace("\r", "").Split('\n'))
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("Fastest:") || trimmed.StartsWith("Alternatively:") || trimmed.StartsWith("Third option:")) ways++;
                        if (trimmed.StartsWith("Fastest:"))
                        {
                            Check(trimmed.Contains(part[1]), "updated fastest " + part[0]);
                            Check(trimmed.IndexOf("Hazelmere", StringComparison.OrdinalIgnoreCase) < 0 && trimmed.IndexOf("120 ", StringComparison.Ordinal) < 0, "fastest is not a ring or 120 cape " + part[0]);
                        }
                        if (!trimmed.StartsWith("Tip:") && trimmed.IndexOf("fairy ring", StringComparison.OrdinalIgnoreCase) >= 0) fairy = true;
                    }
                    if (part[2].Length > 0) Check(travel.Contains(part[2]), "updated route detail " + part[0]);
                    Check(ways >= 1 && ways <= 3, "at most three ways on " + part[0]);
                    bool tip = travel.Contains("Tip: make a portable fairy ring in ;;ig, or buy an unlimited one for 5 Kavros dollars.");
                    Check(fairy == tip, "fairy tip only while a fairy ring remains on " + part[0]);
                    Check(travel.IndexOf("Tile ", StringComparison.Ordinal) < 0 && travel.IndexOf("boat", StringComparison.OrdinalIgnoreCase) < 0 && travel.IndexOf("RuneApps", StringComparison.OrdinalIgnoreCase) < 0 && travel.IndexOf("Jagex", StringComparison.OrdinalIgnoreCase) < 0 && travel.IndexOf("Alt1", StringComparison.OrdinalIgnoreCase) < 0, "plain card " + part[0]);
                    for (int banned = 3; banned < part.Length; banned++) if (part[banned].Length > 0) Check(travel.IndexOf(part[banned], StringComparison.OrdinalIgnoreCase) < 0, "removed " + part[banned] + " from " + part[0]);
                }
                string dave = clues.Clues.Single(c => c.Card.Id == 174).Card.Travel;
                Check(dave.Contains("Dave's spellbook > Varrock (Museum)") && dave.IndexOf("bought from the Vote shop", StringComparison.OrdinalIgnoreCase) < 0, "Dave's spellbook note is not written into the card");
                string hamidCard = clues.Clues.Single(c => c.Card.Id == 144).Card.Travel;
                Check(hamidCard.Contains("Ctrl+T or ;;t > Skilling > Firemaking > Menaphos Bonfire") && hamidCard.Contains("Alternatively: Menaphos lodestone") && hamidCard.Contains("7. Menaphos Cactus Patch") && hamidCard.Contains("Run west from the bonfire to Hamid"), "hamid card routes");
                Check(hamidCard.IndexOf("Bandit Camp", StringComparison.OrdinalIgnoreCase) < 0, "hamid card drops the Bandit Camp lodestone");
                string shore = clues.Clues.Single(c => c.Card.Id == 62).Card.Travel;
                Check(shore.Contains("Ctrl+T or ;;t > Skilling > Woodcutting > Willow") && shore.Contains("Alternatively: Ctrl+T > Locations > Wizard's Tower") && shore.Contains("Third option: Amulet of glory > Draynor Village"), "draynor fishing shore routes");
                Check(shore.IndexOf("Draynor Village lodestone", StringComparison.OrdinalIgnoreCase) < 0, "draynor fishing shore drops the lodestone");
                MasterClue rocks = clues.Clues.Single(c => c.Card.Id == 463).Card;
                Check(rocks.Location.IndexOf("Phasmatite rocks", StringComparison.Ordinal) >= 0 && rocks.Travel.IndexOf("Runite", StringComparison.OrdinalIgnoreCase) < 0 && rocks.Travel.IndexOf("phasmatite rocks", StringComparison.OrdinalIgnoreCase) >= 0, "phasmatite card replaces runite rocks");
                MasterClue sigrid = clues.Clues.Single(c => c.Card.Id == 298).Card;
                Check(sigrid.Area.X == 2612 && sigrid.Area.Y == 3871 && sigrid.Area.Width == 6 && sigrid.Area.Height == 9 && sigrid.Floor == 1, "Queen Sigrid map marks the Etceteria throne room");
                string intro = "This anagram reveals who to speak to next: ";
                foreach (string variant in new[] { "ARC O LINE", "ARCOLINE", "ARC OLINE", "ArcO Line" })
                {
                    ReferenceClue bare = clues.MatchText(variant);
                    ReferenceClue withIntro = clues.MatchText(intro + variant);
                    Check(bare != null && bare.Card.Id == 329, "anagram 329 matches " + variant);
                    Check(withIntro != null && withIntro.Card.Id == 329, "anagram 329 matches the intro plus " + variant);
                }
                foreach (int id in new[] { 45, 57, 62 })
                using (Bitmap original = new Bitmap(Path.Combine(assets, "reference-maps", id + ".png")))
                foreach (float scale in new[] { 1F, 1.25F, 1.5F, 2F, 2.5F })
                using (Bitmap sized = Math.Abs(scale - 1F) < 0.01F ? (Bitmap)original.Clone() : Pixels.ScaleNearest(original, scale))
                using (Bitmap scene = new Bitmap(2560, 1369))
                {
                    using (Graphics g = Graphics.FromImage(scene))
                    {
                        g.Clear(Color.FromArgb(30, 40, 50));
                        g.DrawImageUnscaled(sized, Math.Max(0, (scene.Width - sized.Width) / 2), Math.Max(0, (scene.Height - sized.Height) / 2));
                    }
                    DetectedPuzzle map = clues.Detect(scene, new Pixels(scene));
                    Check(map != null && map.Master != null && map.Master.Id == id, "framed map " + id + " at scale " + scale.ToString("0.00"));
                }
                File.AppendAllText(report, "Map icon checks passed: " + assertions + "\n");
                return 0;
            }
            catch (Exception ex)
            {
                File.AppendAllText(report, ex.Message + "\n");
                return 1;
            }
        }
    }
}
