KAVROS CLUE HELPER v0.9.15
=========================

Close the previous helper with Exit. Extract the WHOLE ZIP into a new folder,
then run KavrosClueHelper.exe beside its assets folder.

Requires Windows 10/11 and .NET Framework 4.8. The new text clue reader uses
Windows English OCR. If English OCR is missing, add the English language OCR
component in Windows Settings and restart the helper.

NEW IN v0.6.6
- Redesign, clue fixes, slider overhaul, and an update toast instead of popups.
- v0.6.5: fix update check (TLS 1.2), ship exe.config.

NEW IN v0.6.4
- Travel directions and zoomable offline maps for location clues. The wheel
  zooms the map, left-drag pans it, and right-click resets a clue card's map.
- Elite scan scroll recognition, 22 scan-region travel guides, and improved
  Heart of Gielinor minimap matching. See PATCH-NOTES.md.

NEW IN v0.6.0
- Global slider route search with fewer moves; shortest routes are labelled only
  when proven. Your supplied screenshot: 235 to 109 normal tile moves, or
  67 clicks in the separate whole-row mode. Whole-row mode is off until Kavros
  support is confirmed; enable it in Settings only if a far tile slides a line.
- 345 Easy/Medium/Hard clue cards, with local text recognition, NPC/equipment
  details, coordinates and conditional challenge answers.
- All 24 baseline map clue drawings; 20 Elite scan regions with 524 spots.
- Recognised new clues replace old cards automatically; travel directions stay.
- Minimap recovery after brief misses, camera turns and zoom changes.
- Persistent knot counts; grey means updating from the last confirmed counts.
- Four slider preview colours: green next, then cyan, orange and purple.

See PATCH-NOTES.md for the complete changes and remaining reference gaps.
CLUE-CATALOG.md lists the source catalogue. verification/ contains test reports.
The source catalogue is a baseline, not a claim that every variant is identical
on Kavros. Wendlewick scan candidate data remains unverified and is not mapped.

PREVIOUS RELEASE NOTES
======================
NEW IN v0.5.5
- Home Teleport H anchors the minimap; the north needle resolves rotation.
  The supplied actual Prifddinas minimap now matches, including resized views.
  The scan box follows on the tracker beside its known dig-spot dots.
- Fresh cropped readings confirm a first location before showing the box.
- Slider markings stay visible during captures and short tile animations.
  Each confirmed move replenishes up to four preview steps; green is next.
  The same route survives switching between full and cropped captures.
- Knot compatibility/minimum-cost search avoids enumerating millions of
  possible plans when runes become uncertain after fast rotations.

NEW IN v0.5.4
- Native Edgeville Monastery master-clue text recognition fixed.
- Native Tuska slider detection, finished-picture preview and clear numbered
  click batches. Green is next, gold follows. Step numbers keep their order as the preview advances.
  Fast successive moves can reuse and advance the existing solution route.
- Knot guidance waits for settled runes and retains rune memory through brief
  missing/animation frames and capture-area changes.
- Adaptive scan minimap framing, zoom/rotation matching and reacquisition.
  Reference tests cover rectangular/circular views from 96 to 640 pixels wide.
- Higher-resolution offline compass AND scan maps, with twice the width and
  height of the previous display images. Marker coordinates are preserved.
- All coordinate clues, anagrams and remaining emotes stay on the backlog.

NEW IN v0.5.3
- Type a lodestone prefix: ed suggests Edgeville. Press Enter or click a
  suggestion to select it and fill its coordinates. Matching ignores case.
- Saved Kavros positions are searchable too.
- Unmatched text disables recording from the previous selection.

NEW IN v0.5.2
- Fully opaque panel backgrounds and independently painted control sections.
- Removed parent-window compositing used by the previous clipping fix.
- Disabled Record direction remains readable on the dark background.
- Anachronia and City of Um are dropdown-only; no map-corner icons.

NEW IN v0.5.1
- Click a lodestone picture on the compass map, then Record direction.
  No repeated Standing at these coordinates checkbox. Hover for the name;
  Lodestones returns to the overview. The map does not teleport the player.
- Full 28-entry baseline catalog, corrected labels and saved-name repairs.
  Existing reference coordinates and saved Kavros overrides are preserved.
- Auto follow minimap for the two scan maps, with background matching,
  two-frame confirmation and automatic removal of lost/stale positions.
- Correct scan headings and a full panel repaint on clue/card resizing.
See PATCH-NOTES.md for validation coverage and live tracking limits.

CHANGES
- Reading / solving indicator on the floating panel and in Settings.
- Faster capture scheduling: checks no longer wait an extra 150-200 ms
  after analysis finishes. Reader assets are prepared in the background.
- Master text recognition tolerates changes in text size and rendering,
  while requiring a complete matching paragraph. Includes the newly
  reported Lumbridge Fishing Supplies screenshot.
- Master maps, emotes and equipment remain visible when you close the
  scroll. New recognised clues/puzzles replace them. Clear removes the card.
- Elite compass recognition tested against your real Edgeville arrow
  screenshot and the arrived cross/spade screenshot.
- Lodestone reference positions, bearing lines, an estimated intersection
  marked with a magenta X, and standard elite dig candidates.
- Prifddinas and Heart of Gielinor scan maps: 18 and 26 reference spots,
  square range boxes, double-range outlines, a different-level message
  and a Meerkat (+5 paces) option.
- Existing Towers, Lockbox, Celtic knot and sliding-box solvers retained.
  The broader all-clue database expansion is still on hold.

FLOATING PANEL
The helper starts with its small floating panel. Settings opens the large
window; closing Settings returns to the panel. Exit closes the program.
Drag the header to move it; - / + collapses or expands it. The panel
follows the client and hides when the game is minimised or unrelated apps
are active. Puzzle markings allow game clicks through them.
Rescan searches the full game window. Pause/Resume controls reading.
Clear dismisses saved guidance. If the same clue is still open, close and
reopen it to show that dismissed card again.
Ctrl+Shift+L scans. Ctrl+Shift+H hides/shows guidance.
Keep the clue/puzzle visible and place the helper away from it.

MASTER CLUES
The eight requested master emotes have offline maps, instructions and gear.
After recognition, close the scroll and travel with the card still visible.
A missing frame does not erase the card or lock detection to its old position.

ELITE COMPASS: TWO LODESTONES
1. Open the compass. Use New clue at the start of each new compass clue.
2. Teleport to a lodestone and stay where you land. Select that lodestone
   on the helper map (or type its first letters and select the suggestion).
3. Once the arrow is stable, press Record direction. A gold line is drawn
   on the map automatically; you do not need to draw it by hand.
4. Teleport to a different lodestone and repeat. The crossing is marked
   with a magenta X. Cyan dots are matching standard locations; a single
   matching location is marked green.
5. If the rays are too parallel or inconsistent, use a third lodestone.
   Travel to the estimated location and verify the game's actual compass
   shows its cross/spade before digging.
Closing the compass keeps recorded lines. Another supported clue/puzzle
clears the compass session. Undo removes the last line.
Wheel zooms; left-drag pans; right-click selects a custom position.
Click Lodestones to return to the overview. Anachronia and City of Um are available in the dropdown only. Choosing an icon sets a reference, not a teleport.
Baseline lodestone coordinates come from the standard pre-update map data.
Kavros custom arrival tiles can be edited and saved separately: enter a
name, X/Y and Radius, then Save. The screenshots verify recognition states;
they do not independently verify every Kavros teleport coordinate or the
final triangulation accuracy on the server.

SCAN MAPS
Includes 20 Elite regions plus the Prifddinas and Heart of Gielinor Master
regions. Choose the displayed floor on a multi-level scan.
The map appears when the scan scroll is recognised and stays available
after you close it.
Auto follow minimap is enabled by default. Keep the in-game minimap visible
and away from the helper. Two consistent terrain matches place the yellow
marker, and it follows movement. Green is within scan range; orange is
within double range. A grey marker shows your last confirmed position when
tracking is uncertain. Wheel to zoom and left-drag to pan.
Right-clicking the map switches to manual positioning. Re-enable Auto
follow minimap to resume tracking. Initial localization can take a few seconds; subsequent acquired movement
reads are typically faster. The supplied actual Prifddinas minimap now matches, including resized/moved
versions. Movement tests transform that captured terrain. Heart tracking also
matched a Kavros capture and four repeat crops. Live walking remains unverified.
The familiar itself is not automatically detected. Enable Meerkat when active: the box increases by five paces. The bonus is
applied once to the base range, not repeatedly to an already boosted range.
A recognised different-level message is shown above the map.
Reference spots/floors still need confirmation where Kavros differs from RS3.

VALIDATION AND PERFORMANCE
Built on Windows with the native .NET Framework compiler, with explicit
Framework references to avoid the earlier String.Split startup error.
test-results.txt contains the headless regression and real screenshot reads.
The new screenshot analysis was measured separately from native capture,
timer scheduling and painting. The goal is roughly half a second from
opening a supported clue to guidance; this is not a guaranteed response
time for every clue, solver, machine or game state. One-time reader
initialisation is prepared at startup.
Previous v0.5.0 sample analysis (before scan tracking): native Lumbridge 18 ms (29-40 ms in full scenes),
real compass arrow 107 ms, arrived state 85 ms, Prifddinas scan 181 ms,
full-scene Towers 96 ms, Knot 343 ms, Sliding 334 ms and Lockbox 249 ms.
Opening Lockbox during the idle test took 271 ms; the slowest measured
idle/live search stage was 794 ms. Initial asset preparation took 1,082 ms.
Tests show no app windows, capture no desktop and send no game input.
Live focus, dragging, minimisation and click-through still require normal
in-game validation. The helper processes pixels locally, reads no game
process memory, and sends no game mouse/keyboard input. Maps and clue
references are bundled offline. The only runtime network request is to
GitHub, for update checks and update downloads, and only when Check for
updates at startup is enabled or you press Check for updates.

UPDATES
Settings has "Check for updates at startup" (on by default) and a Check
for updates button. Both contact only this project's GitHub Releases.
Turn the startup check off and the helper makes no network requests. A
folder copy that was not installed with Setup.exe, and is not the portable
release package, cannot apply an update by itself.

REBUILD / TEST
Run source/build.ps1 on Windows.
KavrosClueHelper.exe --follow-test results.txt checks the actual minimap,
north orientation, fresh confirmations and knot search cases.
KavrosClueHelper.exe --slider-guidance-test results.txt checks visible
badges, advancing steps, capture reframing and animation/closure handling.
KavrosClueHelper.exe --reliability-test results.txt checks the v0.5.4 native
Edgeville/Tuska captures, slider routes, knot state and minimap-size cases.
KavrosClueHelper.exe --self-test results.txt runs the existing headless suites.
KavrosClueHelper.exe --update-test results.txt checks the new real captures,
retained guidance, intersections, scan ranges and offline map rendering.
KavrosClueHelper.exe --paint-test results.txt checks native hidden-window
rendering, opaque backgrounds and readable enabled/disabled buttons.
KavrosClueHelper.exe --automation-test results.txt checks map clicks,
minimap position/movement matching, position loss and clue-panel layouts.
See THIRD_PARTY.txt for source and game/map asset attribution.

