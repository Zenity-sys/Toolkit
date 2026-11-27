# Kavros Clue Helper v0.9.13

Finding a puzzle or a scan no longer takes focus away from Kavros. The first lockbox on a large window shows up sooner. Scan clues note that live tracking can be finicky, and you can click spots on the map to cross them off. The Prifddinas scan lists ;;prif as the fastest way there.

Settings shows **Made by Zenity  ·  v0.9.13**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.9.12

What's new stays on the clue panel until you press Got it. Lockboxes are found during live play on a 1440p client, and the numbers update as soon as you click.

Settings shows **Made by Zenity  ·  v0.9.12**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.9.11

DarkScape sliding puzzles are found while the side panel is open, and a DarkScape Celtic Knot whose top edge is the only full parchment line is found on the first look. `--show-panel` opens the clue panel at launch, and `--no-tray` starts the helper without a tray icon.

Settings shows **Made by Zenity  ·  v0.9.11**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.9.10

Celtic Knot guidance appears on the first clean look, including when it asks you to turn one ring. The click counts update as soon as a ring settles, and a knot that is already open is re-read from that window.

Settings shows **Made by Zenity  ·  v0.9.10**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.9.9

Celtic Knot puzzles are found on the angled window. This works on Modern and DarkScape, without the old full-width title bar. Sand behind the window is not treated as the puzzle.

Settings shows **Made by Zenity  ·  v0.9.9**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.9.8

Towers puzzles are detected during live play again. Each look checks the Towers board before the other puzzle searches, so a full-size client no longer skips it.

Settings shows **Made by Zenity  ·  v0.9.8**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.9.7

Towers puzzles on the DarkScape skin are detected again. The dark brown cells count as the board, and the clue numbers are read.

Settings shows **Made by Zenity  ·  v0.9.7**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.9.6

The Meerkat and auto-follow checkboxes on the scan card are visible again. The guide ends above them, and a long route still wraps on the card. The buttons along the bottom stay clear.

Settings shows **Made by Zenity  ·  v0.9.6**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.9.5

Elite dungeon scans lock your position on the minimap. The wall lines on those maps are white, the same way Prifddinas already was. The pictures on the clue card are unchanged. The right-click line stays hidden while auto-follow is on.

Settings shows **Made by Zenity  ·  v0.9.5**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.9.4

A scan that says you are too far away still tracks your minimap. The card keeps the steps, the too-far note, and the position once it is found. Coming into range keeps that lock.

Settings shows **Made by Zenity  ·  v0.9.4**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.9.3

Opening an elite scan, including Deep Wilderness, no longer freezes the helper or clears the current read. The title matches the letters on the scroll. A failed text read is ignored, and a failed look is written to crash.log.

Settings shows **Made by Zenity  ·  v0.9.3**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.9.2

Every elite scan in the minigames panel is read on the first look, including Deep Wilderness when the orb says you are too far away. An unchanged scan or compass is kept without another board search. A scroll that opens during an idle search is read straight away.

Settings shows **Made by Zenity  ·  v0.9.2**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.9.1

A scan clue open in the minigames panel is read on the first look, including the darker Prifddinas parchment. Scale 1.0 is tried on that sheet before the rest of the window. Prifddinas and the Heart of Gielinor are named from their titles. Wide clue scrolls are unchanged.

Settings shows **Made by Zenity  ·  v0.9.1**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.9

Compass and scan clues are read on the parchment first, with scale 1.0 right after the Auto guess. A tall narrow sheet skips the board search, and the next capture stays on that sheet.

Scan tracking follows a run or a teleport. Two misses, or a clear jump, search the whole minimap. The Heart uses north to cut the angles and checks the scan area before the full track map. A scan card clears 3 seconds after the scan window closes.

The Fishing Platform crate clue is removed until players get it on Kavros. An ultrawide minimap is ignored from the right edge, so a clue further left is still read. The sulphur pit master clue shows the TzHaar City map.

Settings shows **Made by Zenity  ·  v0.9**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.8.1

The Resolution and Scaling row no longer sits on a dark box. Those labels and dropdowns sit flat on the settings background, like Check for updates and Blur live preview. The dropdowns themselves are unchanged.

The CLIENT heading is shown in full. The Client section, including Interface and the Resolution and Scaling row, sits below What's new with a clear gap. The Interface label sits flat on that section.

Settings shows **Made by Zenity  ·  v0.8.1**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.8

The interface is two toggle boxes: Modern / DarkScape and Legacy. Exactly one is selected, the selected box is gold, and there is no Auto. DarkScape uses the full modern template set.

Resolution and scaling are dropdowns. Auto reads the game window (a 2560x1369 client counts as 1440p) and shows what it found. If the window cannot be read, Auto uses the last manual choice.

Versions show one digit after the decimal, rounding up: 0.8, 0.9, 1.0, 1.1 ... 1.9, 2.0. Never 0.10 or 1.10. This release is v0.8. The tag is v0.8.0 so the updater still sees it as newer than 0.7.28.

Settings shows **Made by Zenity  ·  v0.8**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.27

v0.7.27: Opening a slider shows the guide on the first look at the scale the client is using. A tracked text clue still notices a puzzle that opens somewhere else, and a mysterious scroll still needs its dark title bar and frame.

Settings shows **Made by Zenity · v0.7.27**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.25

v0.7.25: The slider guide stays in your own screenshots. Capture no longer paints over the tiles; the reader ignores the ring, arrow and badge and keeps using the clean pixels. A click that slides a whole row is shown immediately when it matches the route. If a read misses, the last grid is tried again a few pixels either way before the whole window is searched. Mysterious clue scrolls on sand, including the Hamid cryptic, are found from the dark title bar.

Settings shows **Made by Zenity · v0.7.25**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.24

v0.7.24: Clicking a slider tile shows the next step again. The guide is left out of the capture on Windows 10 2004 and later, and the fallback paint covers the pulse ring and the arrow. A missed read keeps the last step for about a second and a half, then reads the board again. After three misses the status asks you to move the mouse off the puzzle.

Settings shows **Made by Zenity · v0.7.24**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.23

v0.7.23: A slider that is still on screen keeps its next step instead of reading every other clue. Windows 125% (and 150% and 200%) searches the matching interface size first. The legacy corners and close button are also matched as blocky pixels at those sizes. The modern interface uses the same 1.25 scale it already uses for 150% and 200%.

Settings shows **Made by Zenity · v0.7.23**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.22

v0.7.22: The gnomeball referee card shows its challenge answer. Prifddinas and Heart of Gielinor scans are recognised from the scroll text when the picture does not match. A helper window sitting behind the game is left on the capture.

Settings shows **Made by Zenity · v0.7.22**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.21

v0.7.21: Twenty-four clue cards use the travel routes checked in-game. A teleport that lands on the spot no longer sends you off from a lodestone that is not on the card, and the fairy ring tip stays only where a fairy ring is still listed.

Settings shows **Made by Zenity · v0.7.21**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.20

v0.7.20: Blur live preview sits under Check for updates at startup, and What's new sits under Check for updates. The hard cryptic about four blades is the Lumbridge windmill: combat bracelet to the Champions' Guild, then south-west, and search the crates on the top floor.

Settings shows **Made by Zenity · v0.7.20**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.19

v0.7.19: Legacy towers, lockbox, Celtic knot, compass, and scan parchment are read alongside the modern interface. The round legacy minimap is found from its H button. The compass also checks the 1440p size. Celtic knot counts show as soon as the knot is still, and tiles that have not changed are not read again.

Settings shows **Made by Zenity · v0.7.19**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.18

v0.7.18: A steady compass settles through small jitter, and one missed frame no longer throws the reading away. Record stays available for about two seconds and only lights up when it will take the heading. Settings has Blur live preview, off by default. Scan clues are also recognised with the older brown stone border.

Settings shows **Made by Zenity · v0.7.18**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.17

v0.7.17: Auto in the capture list names the client it is reading, such as Auto - Client 1 or the Companion character name. It says Auto - searching... until a client is found. Game found and the panel header use that same name.

Settings shows **Made by Zenity · v0.7.17**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.16

v0.7.16: The floating panel and the puzzle marks stay on the Kavros client the helper is reading. They follow that window, and they hide when it is minimised or when another window is in front. The panel header shows the character name from Companion, or Client 1, Client 2, and so on.

Settings shows **Made by Zenity · v0.7.16**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.15

v0.7.15: The client list shows only your Kavros clients. Client 1, Client 2, and Client 3 match the clients you have open. A name saved by Companion is still shown.

Settings shows **Made by Zenity · v0.7.15**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.14

v0.7.14: The client list shows the character name Companion saved for that client. A client with no saved name is listed as Client 1, Client 2, and so on. Thanks to PvML-Admin.

Settings shows **Made by Zenity · v0.7.14**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.13

v0.7.13: The helper attaches to the Kavros client it was opened from and stays on that client. Each client can run its own helper. Companion, AFK Warden and the Slayer helper are not treated as the game, and the helper does not take the client AFK Warden is using. A small overlay from those apps is covered before the screen is read.

Settings shows **Made by Zenity · v0.7.13**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.12

v0.7.12: Nothing pops up over the game. Notices are small, sit off the clue card and the puzzle, and clicks pass through them. An update waits on Restart. A bad frame is logged and skipped, and the helper keeps running.

Settings shows **Made by Zenity · v0.7.12**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.11

v0.7.11: A slide route that does not match the board is solved again instead of closing the helper. A puzzle stays tracked in the area where it was found. Clue cards are shorter, and a fairy ring is a small tip at the bottom of the card.

Settings shows **Made by Zenity · v0.7.11**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.10

v0.7.10: Rescan reads the puzzle that is on screen again. A slide route is solved from the current tiles, including when a move does not match the old route. The hard cryptic about four blades is the Lumbridge windmill, top floor. A Dave's spellbook step notes that it is bought from the Vote shop.

Settings shows **Made by Zenity · v0.7.10**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.9

v0.7.9: The screen no longer flashes black. The helper copies the screen instead of asking the game window to repaint. The overlay still shows in screenshots, and its own numbers are covered before a puzzle is read.

Settings shows **Made by Zenity · v0.7.9**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.8

v0.7.8: Puzzle numbers sit on the tiles again at 1440p and 4K, and they do not blink. Towers go 5, blank, then 1. Slider clicks move the whole row. The overlay shows up in screenshots. Save debug on the panel stores a picture and the detection log. A compass clue stays locked, and the overlay follows the game window.

Settings shows **Made by Zenity · v0.7.8**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.7

v0.7.7: The panel stays above the game and the other Kavros helpers, not above Discord or a browser. Lockbox and tower numbers no longer blink. Five medium clues that were missed now have cards. Port Khazard uses the Lunar Khazard Teleport. Thanks to PvML-Admin.

Settings shows **Made by Zenity · v0.7.7**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.6

v0.7.6: Clue cards use Fastest, Alternatively, Third option, Final step, and Clue. The lockbox no longer flickers. Scan tracking holds your position through missed frames. The tower puzzle locks on faster. The yellow search bar is removed. Thanks to PvML-Admin.

Settings shows **Made by Zenity · v0.7.6**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.5

v0.7.5: Master emote cards use the full-width map. Lodestone icons are drawn on every map, and the round compass replaces the letter N. Elite and master routes are filled in. Jatizso and Neitiznot use Ctrl+T > Monsters > Yaks. Mos Le'Harmless is not available on Kavros.

Settings shows **Made by Zenity · v0.7.5**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.4

v0.7.4: Settings opens in front of the floating panel again. The lag and frozen camera are fixed. The panel was taking focus from the game on a timer, and it no longer does.

Settings shows **Made by Zenity · v0.7.4**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.3

v0.7.3: the helper stays on the Kavros window you chose. Another client in front does not take over. The floating panel stays visible when you click another application.

Settings shows **Made by Zenity · v0.7.3**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.2

v0.7.2: the Wilderness Agility Course stump treasure map is recognised. The dig is a few steps south of the course gate, by the stumps. The level 44 wilderness obelisk, then north-east, is the other way.

Settings shows **Made by Zenity · v0.7.2**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.1

v0.7.1: fairy ring codes and walks were checked against the wiki list. ALP is no longer used for the Lighthouse or the Barbarian Outpost shore. A Fastest fairy ring step says "(portable fairy ring required to be the fastest)". 26 clues gained a fairy ring option.

Settings shows **Made by Zenity · v0.7.1**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.7.0

v0.7.0: new routes for clues that had no teleport, faster routes for 33 existing cards, scan dots you have already checked, and a treasure-map route by the Wilderness Agility Course.

Settings shows **Made by Zenity · v0.7.0**. The full notes are in CHANGELOG.md.

# Kavros Clue Helper v0.6.5

v0.6.5: fix update check (TLS 1.2), ship exe.config.

Settings shows **Made by Zenity · v0.6.5**. Install this build by downloading Setup.exe. Copies of 0.6.4 cannot download it themselves, because their update check fails before TLS 1.2 is offered.

# Kavros Clue Helper v0.6.4

18 November 2026 · Portable Windows release

## Finding the destination

- Coordinate, treasure map, search and NPC cards give a named destination and travel directions. The “under-cook” clue takes you from the Lumbridge lodestone into the castle kitchen, down the trapdoor and to the crate in the cellar.
- Added offline detail maps for all 345 catalogue entries with a known position, including interiors and upper floors. Use the wheel to zoom, drag to pan and right-click to reset a location card. The gold pin stays on the destination as you move the map.
- Fixed recognition of the Falador crossroads statue drawing. The card gives the dig offset of one tile south and three east. Added the reported 05°35′ south, 26°30′ east clue with directions to the desert west of Pollnivneach.
- Removed the repeated “Reference location” line. The helper keeps the current directions visible during background reads and replaces them when it recognises the next clue.

## Elite and Master scans

- The reader recognises the reported Elite scrolls for Brimhaven Dungeon, Wilderness volcano, Varrock and the Grand Exchange, Piscatoris Hunter Area, East or West Ardougne, Falador, Fremennik Slayer Dungeons, the desert east of the Elid and north of Nardah, Zanaris and Taverley Dungeon.
- All 20 supported Elite regions and both Master regions show travel directions alongside their possible dig spots, even before the helper finds the player. The scan map supports wheel zoom, drag to pan and right-click to set your own position.
- Heart of Gielinor tracking uses the shape of the platforms around the player. A grey marker keeps your last confirmed position visible when the tracker needs a new match.

## Interface and verification

- Fixed the clipped bottom puzzle option in Settings. Checked the layout at 100%, 125% and 150% scale.
- Replayed the reported Elite scrolls with both floor messages, the Falador and Galahad treasure maps, the Lumbridge crate and the desert coordinate clue. Checked all 24 catalogue treasure maps at three sizes.
- The Heart tracker matched the supplied game capture and four repeat crops within one tile. Continuous walking in the live Heart instance still needs an in-game test. The Wendlewick farmhouse has written directions. Its exact tile and the Wendlewick Elite scan spots remain unverified.

## Running the update

Exit the old helper, extract the entire ZIP into a new folder and open **KavrosClueHelper.exe**. Settings shows **Made by Zenity · v0.6.4**.
