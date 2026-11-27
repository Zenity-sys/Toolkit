# Kavros Clue Helper

## 0.9.12

**Puzzles**
- What's new stays on the clue panel until you press Got it. The note lists this version and the recent ones, and it comes back only after the next update. Settings can open the same note from its What's new button.
- Lockboxes are found during live play on a 1440p client. The numbers update as soon as you click.

## 0.9.11

**Puzzles**
- DarkScape sliding puzzles are found while the side panel is open, in live play and on a still capture. The route is solved from the tiles, and the frame stays tracked while you solve it.
- A DarkScape Celtic Knot whose parchment is only fully visible along the top edge is found on the first look. Brighter gold tracks and the red ring are read with the existing knot.

**Launch**
- `--show-panel` opens the clue panel at startup, and a second launch with that switch opens the panel of the copy that is already running.
- `--no-tray` starts the helper without a tray icon. The already-running notice no longer mentions the tray when there is no icon.

## 0.9.10

**Puzzles**
- Celtic Knot guidance appears on the first clean look, including when it asks you to turn one ring.
- The click counts update as soon as a ring settles, and a knot that is already open is re-read from that window.

## 0.9.9

**Puzzles**
- Celtic Knot puzzles are found on the angled window. This works on Modern and DarkScape, without the old full-width title bar. Sand behind the window is not treated as the puzzle.

## 0.9.8

**Puzzles**
- Towers puzzles are detected during live play again. Each look checks the Towers board before the other puzzle searches, so a full-size client no longer skips it.

## 0.9.7

**Puzzles**
- Towers puzzles on the DarkScape skin are detected again. The dark brown cells count as the board, and the clue numbers are read.

## 0.9.6

**Scan clues**
- The Meerkat and auto-follow checkboxes on the scan card are visible again. The guide ends above them, and a long route still wraps on the card.
- The buttons along the bottom stay clear of the guide.

## 0.9.5

**Scan clues**
- Elite dungeon scans lock your position on the minimap. Those maps now have white wall lines, the same way Prifddinas already did.
- The pictures on the clue card are unchanged. The right-click line stays hidden while auto-follow is on.

## 0.9.4

**Scan clues**
- A scan that says you are too far away still finds you on the minimap. The card keeps the step-by-step guide, the too-far note, and the position once it locks.
- Coming back into range keeps that position. This is the same for every scan, elite or master.

## 0.9.3

**Scan clues**
- Opening an elite scan no longer freezes the helper or drops the read back to a full-window search.
- The idle tick no longer reads the picture the worker is using, and it no longer captures the whole window.
- Deep Wilderness matches the live title size. A failed text read is ignored instead of crashing, and a failed look is written to crash.log.

## 0.9.2

**Scan clues**
- Every elite scan in the minigames panel is named on the first look, including Deep Wilderness when you are too far away.
- A scan or compass that has not changed is kept without searching the boards again.
- Opening a scroll cancels an idle full-window search instead of waiting for it.

## 0.9.1

**Scan clues**
- A darker scan in the minigames panel is read on the first look. The sheet is taken from the scroll area, and scale 1.0 is tried on that box before the rest of the window.
- Prifddinas and the Heart of Gielinor can be named from their title letters.
- A wide titled clue scroll is still read the same way, ahead of that panel search.

## 0.9

**Compass and scan**
- The first look tries scale 1.0 right after the Auto guess, then searches the parchment before the rest of the window.
- A tall narrow compass or scan is read on that sheet, and the board search is skipped while it is open.
- After a hit, the next capture stays on that sheet.

**Scan tracking**
- The search radius grows with the time since the last fix and how fast you can run.
- A fix that is far from the last spot, seen twice in a row, is treated as a teleport and the marker locks there.
- Two missed frames, or a clear jump of the player dot, start a full search.
- The minimap crop is wider. A result older than 1.5 seconds stays as the seed for the next search.
- On the Heart, the north arrow cuts the angles, and the scan area is searched before the full track map.

**Clues**
- A scan card clears itself 3 seconds after the scan window closes.
- Clue 188, the Fishing Platform crate, is removed. It can be added back if players get it on Kavros.
- On an ultrawide client, only the minimap at the right edge is skipped.
- The sulphur pit master clue shows the TzHaar City map.

## 0.8.1

**Settings**
- Resolution and Scaling sit on the settings background, in the same flat row style as Check for updates and Blur live preview. The dropdowns themselves are unchanged.
- The CLIENT heading is shown in full, at the same size as the other section headings.
- The Client section sits below What's new, with a clear gap. The Interface label sits flat on that section.

A small update keeps its patch number: 0.8.1, 0.8.2, 0.8.3, 0.8.4. The app shows v0.8.1. When the patch is zero it still shows v0.8.

## 0.8

A full step shows one digit after the decimal: 0.8, 0.9, 1.0, 1.1, and on through 1.9, then 2.0. A small update adds a patch (0.8.1, 0.8.2, 0.8.3, 0.8.4). The app shows the patch only when it is not zero, so this release shows v0.8. The tag is v0.8.0 and the assembly version is 0.8.0.0, which the updater treats as newer than 0.7.28.

**Settings**
- Interface is two toggle boxes, Modern / DarkScape and Legacy. Exactly one is selected, the selected box is gold, and there is no Auto. DarkScape uses the full modern template set.
- Resolution (Auto, 1080p, 1440p, 4K) and scaling (Auto, 100%, 125%, 150%, 200%) default to Auto. Auto reads the game window and shows the result, for example Auto (1440p). A 2560x1369 window counts as 1440p. If the window cannot be read, Auto uses the last manual choice. That scale is searched first, and the other scales stay as fallback.

## 0.7.28

**Puzzles**
- The automatic-detection note now only says to keep the full puzzle visible. The tower countdown switch is unchanged.

**Clues**
- Queen Sigrid's map is the Etceteria castle throne room, floor 1.
- Hamid: bonfire teleport, then west to the waterfront. The other ways are the Menaphos lodestone and the Menaphos cactus patch.
- The coordinate clue north of the Lava Maze names the phasmatite rocks.
- Draynor Village fishing shore: willow teleport, then a short walk. The other ways are the Wizard's Tower and the amulet of glory to Draynor Village.
- A large framed treasure map is still matched. A sandy sheet and the minimap are still ignored.
- A scroll title that the text reader misses is still passed to the map and master readers. The board search stays skipped.
- An anagram still matches when the name is run together or split differently, including after the intro line.

## 0.7.27

**Slider**
- Every frame starts at the scale this client should be using: the Windows DPI scale, otherwise 1.0, otherwise the scale that matched last. One extra scale is tried only while the frame still has time. The modern frame is checked before the legacy one, unless the legacy interface was the last match.
- The shortest-route search runs below the reader, so a full-window frame is not stuck behind it.

**Clues**
- The scroll title bar is still checked on every missed board. Master templates, map matching and text recognition wait a few frames, or until the main scales have been covered. The first look still reads a clue.
- A tracked text clue keeps its card when the screen has not changed, and still searches for a puzzle that opens somewhere else.
- A mysterious scroll has to have the dark title bar and the dark frame. A sandy row is not enough.

## 0.7.26

**Slider**
- A whole-row step advances the route already on screen. The helper does not solve the board again on the UI thread, so the timer and the guide keep moving.
- One route is shared. A shorter proof replaces it only when the next click stays the same. The first step is the fast route; a shorter one is refined in the background.
- The guide mask uses the capture offset, so the ring, arrow and badge are ignored on a cropped read. After missed reads the last board stays up.

**Clues**
- A mysterious clue scroll is found from its title bar first. That frame skips the board search and the master templates and goes straight to the scroll text.
- An unchanged clue already on screen is not searched again.
- A large sand sheet and the minimap are not sent to master matching or text recognition.

## 0.7.25

**Slider**
- The guide is visible in your own screenshots again. It is no longer excluded from capture.
- Capture does not paint over slider tiles. The reader skips the pixels of the pulse ring, the outline, the arrow and the badge, including in the frame-edge check, and scores the remaining tile pixels.
- A whole-row click skips the confirm wait when the new board is the next step of the known route.
- When the short hold expires, the last grid is read again a few pixels either way at the current scale. The board is kept for two more tries before the search falls back to the full window.

**Clues**
- A mysterious clue scroll on sand or sandstone is found from its dark title bar and thin border. The Hamid cryptic (Speak to Hamid, Menaphos beach) is one of these. The card has three ways and a final step, and no tile coordinates.

## 0.7.24

**Slider**
- A click that slides the blank is kept. A swap of two picture tiles is still treated as a misread.
- The last step is held only for about 1.5 seconds after a real read. After that the board is located and solved again, even while the frame is visible. Clue readers are not run on that frame.
- Three missed reads in a row set the status to "Can't read the tiles - move the mouse off the puzzle". There is no popup.
- The guide window is excluded from screen capture on Windows 10 2004 and later, and it stays visible to the player. Where that is unavailable, the capture paint covers the full pulse ring and the gold arrow, and the reader is told which cell has the badge.

## 0.7.23

**Slider**
- While a slider is on screen, a frame in the middle of a move keeps the current step. The clue readers are not run for that frame.
- A re-read that misses the tiles keeps the last board and tries again shortly. The route is dropped only after three missed reads.
- One legal move in the same place is shown without waiting for another frame.
- The legacy close button is looked for a little further from the last position, with the same colour tolerance as the other anchors.

**Windows scaling**
- The game window's DPI is read once per capture. At 100% the search order is unchanged.
- At 125%, 150% and 200% the matching scale is searched first: 1.25, 1.5 and 2. The first frame after a DPI change uses that scale.
- Modern puzzles, the compass, scans and master clues use that order. Scan scale 1.75 is added only when the DPI is nearest to 175%.
- Legacy frame corners, the close button and the slider mark are also tried as nearest-neighbour copies at that scale, so the blocky legacy interface matches at 125%, 150% and 200%.

## 0.7.22

**Clues**
- Talk to the Gnome ball referee includes the challenge "What is 57 x 89 + 23?" and the answer 5096.

**Scans**
- Prifddinas and the Heart of Gielinor are read from the scroll text when the picture match finds nothing. The place name and the paces both have to match. Prifddinas is 30 paces, or 35 with meerkats. The Heart of Gielinor is 49 paces, or 54 with meerkats.
- The same reading accepts the side parchment, including "you are in range of the coordinate" with the distance in paces.

**Capture**
- Companion, AFK Warden, the Slayer helper and another Clue Helper are blanked only while they are in front of the game. A window behind the game is left as it was captured.

## 0.7.21

**Clues**
- Twenty-four cards use the routes checked in-game. Each card still has at most three ways: Fastest, Alternatively, and Third option.
- A teleport that lands on the spot no longer tells you to run from a lodestone that is not on the card.
- The portable fairy ring tip stays only on cards that still list a fairy ring.

## 0.7.20

**Settings**
- Blur live preview sits in the left column, under Check for updates at startup.
- What's new sits on the right, under Check for updates.

**Clues**
- The hard cryptic "Four blades I have, yet draw no blood" is the Lumbridge windmill (Mill Lane Mill, between Lumbridge and Draynor). Search the crates on the top floor.
- Fastest is a combat bracelet to the Champions' Guild, then run south-west. Draynor Village lodestone, then east, is the other way. A quest cape to the Champions' Guild, then south-west, is the third. Wrapped lines and small reading mistakes still match the clue.

## 0.7.19

**Legacy interface**
- Tower clues are read on the older brown stone grid as well as the dark blue one.
- The lockbox, the Celtic knot, and the compass arrow are read on the legacy window.
- The compass also looks at the in-between size a 1440p client uses.
- A scan clue on the legacy side parchment is read, including when you are too far away and nothing scans.
- Minimap tracking can lock onto the round legacy orb from its H button.

**Celtic knot**
- Ring counts show as soon as a still knot is read clearly. A ring that is still turning waits until it stops, so a half-turned frame cannot change the numbers.
- Tiles that have not changed are not read again.

## 0.7.18

**Compass**
- A steady compass settles even when the arrow jitters by a fraction of a degree. One missed frame no longer throws that reading away.
- Record stays available for about two seconds after the last steady heading, and the button only lights up when Record will take it.

**Preview**
- Settings has a Blur live preview checkbox. It starts off, so the capture preview is sharp. Turn it on to dim and soften the preview.

**Scans**
- Scan clues are also recognised when the window uses the older brown stone border.

## 0.7.17

**Clients**
- Auto in the capture list names the client it is reading, such as Auto - Client 1 or Auto - the Companion character name. It says Auto - searching... until a client is found, and the name updates when the binding changes.
- Game found and the panel header use that same name.

## 0.7.16

**Clients**
- The floating panel and the puzzle marks stay on the Kavros client the helper is reading. They follow that window, and they hide when it is minimised or when another window is in front. They come back when that client is in front again.
- The panel header shows the character name from Companion, or Client 1, Client 2, and so on.

## 0.7.15

**Clients**
- The client list shows only your Kavros clients. Client 1, Client 2, and Client 3 match the clients you have open. A name saved by Companion is still shown.

## 0.7.14

**Clients**
- The client list shows the character name Companion saved for that client. A client with no saved name is listed as Client 1, Client 2, and so on.
- Thanks to PvML-Admin.

## 0.7.13

**Clients**
- Opening the helper from a client's Companion panel attaches to that client and stays there. Another Kavros window in front does not take it.
- Each client can have its own helper. Opening it again on the same client shows the small notice on that helper.
- Companion, AFK Warden and the Slayer helper are not treated as the game. The helper does not attach to the client AFK Warden is using.
- A small overlay from those apps is covered before the screen is read. If Companion passes the window or the process, that client is used.

## 0.7.12

**Notices**
- Nothing pops up over the game. A helper that is already running, an update check, and a saved capture show a small notice for a few seconds.
- The notice sits in a free corner of the game window, off the clue card, the puzzle, and the numbers on the tiles. Clicks pass through it. When an update is ready, Restart stays until you click it.
- The helper covers its own notice before it reads the screen, so the notice is not treated as part of the puzzle.

**Stability**
- A bad frame or a broken line in the clue data is written to a log and skipped. The helper keeps running.
- With the game closed and nothing on screen, the helper checks less often.

## 0.7.11

**Puzzles**
- A bad slide route no longer closes the helper. The route is checked against the tiles and their parity, and a route that does not fit is thrown away, logged, and solved again from the board on screen.
- Once a slide, lockbox, tower or knot is found, the helper keeps reading that area through moves and rescans. It leaves the area only after several reads in a row show the puzzle is gone.

**Clues**
- Cards use one short line per step: Fastest, Alternatively, Third option, Final step, then the clue. A fairy ring is a small dim tip at the bottom of the card.
- The charm sprite card uses Traveller's necklace to the Outpost, then Rune dragons, then the Gnome course.
- The Heart of Gielinor scan starts at Telos from the Beast tab. The TzHaar sulphur pit starts at the Fight Caves entrance.

## 0.7.10

**Puzzles**
- Rescan throws away the cached slide route and reads the tiles that are on screen. If a move does not land on the expected board, the helper solves that board instead of continuing the old route.
- Rescan also clears a cached lockbox, tower or celtic knot.

**Clues**
- The hard cryptic "Four blades I have, yet draw no blood" leads to the Lumbridge windmill. Search the crates on the top floor.
- A step that uses Dave's spellbook notes that it is bought from the Vote shop.

## 0.7.9

**Capture**
- Reading the game no longer uses PrintWindow. That call was blanking the whole screen for a moment on the DirectX client. Tracking, puzzle reads and Save debug copy the screen again.
- The overlay still shows in screenshots and recordings. Before a puzzle is read, the helper paints over its own numbers and box edges so it does not treat them as the puzzle.

## 0.7.8

**Overlays**
- Lockbox, tower, slider, knot and light-box numbers sit on the puzzle tiles at 1080p, 1440p and 4K. The overlay is placed in the screen's real pixels, and it is not moved again until the puzzle or the game window actually moves. The numbers do not blink.
- A tower cell goes 5, then blank, then 1. The "5 wraps straight to 1" option is gone.
- Slider clicks are whole-row clicks. That checkbox is gone.
- The celtic knot holds its marks through a small jitter. A compass clue stays locked when a frame is missed.
- Moving or resizing the game window, including onto another monitor, moves the overlay with it and reads the puzzle again at the new size.

**Capture**
- The overlay is no longer hidden from screenshots or recordings. The helper reads the game window itself, and paints over its own marks, so those numbers are not treated as the puzzle.

**Panel**
- Save debug writes a screenshot, the detection details and a short log to a timestamped folder under Local AppData, shows that path for a few seconds, and opens the folder.

## 0.7.7

Thanks to PvML-Admin for the panel change.

**Panel**
- The clue panel and the puzzle overlays stay above the game, and above Companion, AFK Warden, the Slayer helper, and this helper. They drop back when another application, such as Discord or a browser, is in front. Settings still stays in front of the panel.

**Puzzles**
- The numbered boxes on a lockbox and the solution numbers on a towers puzzle no longer blink. The overlay is drawn once and redrawn only when the puzzle actually changes.

**Clues**
- Medium scrolls that were not recognised now open a card: the Varrock Dig Site eastern winch, Karim in Al Kharid (R Ak Mi, answer 5), Rommik in Rimmington (IM KROM, answer 7), the desert quarry wheelbarrow, and the ogre pen in the Combat Training Camp.
- Search the crate near a cart in Port Khazard: Lunar spellbook Khazard Teleport, then the Yanille lodestone, then Ctrl+T > Locations > Ardougne Monastery and run south-east.

## 0.7.6

Thanks to PvML-Admin for this one.

**Clue cards**
- Every card, easy through master, is laid out on its own lines: Fastest, Alternatively, Third option, Final step, and Clue.

**Tracking**
- The lockbox no longer flickers off on a single misread frame. The last solution stays until the puzzle is really gone.
- Scan tracking holds your position through missed frames, then picks you up again without a manual step.
- The tower puzzle locks on faster while the grid is opening.

**Panel**
- The yellow search bar is removed.

## 0.7.5

**Maps**
- Master emote cards use the same full-width location map as hard cards. The map is centred on the emote area and keeps the green area box.
- A lodestone that falls inside a clue map, a scan map, or the compass map is drawn with that lodestone's own icon. The icons load from assets/map-icons/lodestones. The older badges are used only when a file there is missing.
- Every clue, scan, and compass map draws the round compass in the top-right corner. The letter N is used only if assets/map-icons/compass.png is missing.

**Routes**
- Elite and master cards now have a Fastest route, and an alternative when one is confirmed, in the same style as the other clues. A fairy ring that is the fastest step says a portable fairy ring is required, and the ;;ig note stays on every fairy ring step.
- Mos Le'Harmless is not on Kavros. That scan says it is not available, and the reader will not offer it, the same way as Wendlewick.
- Jatizso and Neitiznot: Ctrl+T > Monsters > Yaks, which lands at the yak pen on Neitiznot. Jatizso is across the rope bridge to the east.

## 0.7.4

**Settings and input**
- Settings opens in front of the floating panel and can be used. The panel is no longer pushed to the front on a timer, so it does not take the mouse away from the game. That was the lag and the camera that would not turn. The panel still stays above other windows, without activating, while Settings is closed.

## 0.7.3

**Client**
- The helper stays on the Kavros window you chose. Another client getting focus, or sitting in front, does not take over reading or tracking. If that window closes, it is found again only by the same process.

**Panel**
- The floating panel stays on screen and above other windows when you click another application or the game loses focus. Exit, Pause and the minimise button behave as before. Minimising the chosen game still hides the panel.

## 0.7.2

**Treasure map**
- The Wilderness Agility Course stump map is recognised from the in-game treasure map scroll. The dig is a few steps south of the Wilderness Course gate, by the stumps. The level 44 wilderness obelisk, then north-east, is the other way.

## 0.7.1

**Fairy rings**
- Codes and walks were checked against the fairy ring list. A wrong direction was corrected where the ring's tile and the clue did not agree, including the Wizards' Tower ring, Tyras Camp, the Gu'Tanoth cave, and the walk from Miscellania to Etceteria.
- ALP is the Feldip Hills ring near Gu'Tanoth, so it is no longer used for the Lighthouse or the Barbarian Outpost shore. Those cards use the Games necklace, the Barbarian Course, and Relekka.
- When a fairy ring is the Fastest step, that step says "(portable fairy ring required to be the fastest)". The note that a portable ring can be made in ;;ig, and an unlimited one costs 5 Kavros dollars, stays on every ring step.
- 26 clues gained a fairy ring as Alternatively or Or. Hazelmere is fairy ring CLS, with the Yanille lodestone as the other way onto the island.

## 0.7.0

**Routes**
- 155 clues that had no usable teleport now have a Fastest route, an alternative, and a final step. 33 cards that already had a teleport were given a shorter walk. Hazelmere's ring and the 120 capes are never the first option.
- Every fairy ring step includes this note: a portable fairy ring can be made in ;;ig, and an unlimited one can be bought for 5 Kavros dollars.
- Queen Sigrid is in Etceteria castle. The banana plantation salute, Swensen, the Karamja Volcano dungeon, and the Pirates' Hideout (north-east from the Wilderness Course) use the newer routes.
- Wendlewick and the Witchaven fishing platform are not on Kavros. Those cards say they are not available, and the reader will not offer them.
- Entrana is the hops-patch teleport only. That is the only way onto the island here. The crystal-tree island west of Rellekka keeps the Barbarian Course route only, because the crossing is still unconfirmed. Spider island east of Yanille is walkable from Hazelmere's island over the log bridges and from the Yanille coast, so both routes stay.

**Scans**
- A dig spot you have already stood in range of turns grey. Clear, or a new scan, puts the colours back.
- After you tab away and come back, the helper searches the whole minimap again. It does not ask you to right-click if the minimap is still on screen.

**Treasure map**
- The map by the Wilderness Agility Course stumps uses the Wilderness Course teleport, then a short walk. The picture of that scroll was not in the files for this release, so the image match is the existing map template.

## 0.6.10

Thanks to PvML-Admin for this one.

**Clue panel**
- The last clue solve stays on the panel after you close the scroll, so you can still read the text, the map, and the directions while you walk.
- The badge says **Scroll closed** while that saved solve is showing. It does not say Found.
- Opening the same scroll again switches the badge back to **Clue found**. Nothing else is reset.
- Clear removes the saved solve. A new clue step replaces it.
- Puzzles are unchanged. A slider, lockbox, towers, knot, or compass still goes away when that interface closes, because the guidance only applies while the board is open.

## 0.6.9

**Scans**
- Once the minimap lock is on, a missed frame no longer drops you. The marker stays on the last good tile, the next search looks ahead of your movement, and a bad jump is ignored.
- A saved scan or clue no longer comes back as Found after you close the scroll. Found only appears while that scroll, scan, or puzzle is actually on screen.

**Puzzles**
- The slider count follows the moves you make. A frame in the middle of a slide is ignored, so the highlight and the "left" number stay put until the tile lands, then the count goes down.
- Right-clicking a tower or lockbox still keeps the last reading while the menu covers the board.

**Panel**
- Searching is plain text with moving dots, not a badge. Found stays a badge.
- Settings is the same height as the minimise button.
- Exit matches Pause and Clear. Rescan stays gold.

**Clues**
- Dig Site Exam Centre: get the key from a male barbarian in Barbarian Village, then the Archaeology Guild teleport and walk to the chest under the dragon head. A Digsite pendant or the Varrock lodestone is the other way.
- Luthas is an amulet of glory to Karamja, then a short walk south-east. The Karamja lodestone is the backup. No boat.

## 0.6.8

**Puzzles**
- Puzzle boards are looked for before a clue scroll is read. A slider, lockbox, towers or knot on screen takes over even if a clue was already open, and the capture is no longer cropped down to that old clue.
- Knot puzzles are also checked at the smaller interface sizes.
- Right-clicking a tower or lockbox no longer makes the guidance vanish. The last reading stays while the menu covers the board, and it drops only after the board itself is gone.

**Maps**
- Location maps and the compass zoom out across the whole surface map. Scan maps show the world around the region when you zoom out.
- The hint bars are gone from the compass, the scan map and clue maps. Tile numbers are no longer shown on the clue card. The pin on the map is unchanged.
- Every surface lodestone has its own icon, on the compass and on scan maps. Click a lodestone on either map to set your position there.

**Scans**
- If the scroll says you are too far away, or the minimap cannot be locked, the panel tells you to right-click your location on the map. It no longer sits on "Looking for your minimap".
- A position is only published once the minimap compass anchor agrees, so a guess is not drawn as your tile.
- Meerkat range is turned on when the scroll text or the pace count includes the extra five paces. The checkbox still overrides it.
- Zanaris clues and the Zanaris scan use Ctrl+T or ;;t, then Locations, then Zanaris.

**Clues**
- The new Mysterious Clue Scroll window is read ahead of other parchment in the scene. "Yawn in the rogues' general store" matches the Wilderness Bandit Camp clue.
- Hobgoblin Peninsula now routes through the Crafting Guild menu and no longer mentions snape grass. Falador lodestone remains the alternative.
- Terrorbird enclosure is a walk west from the Gnome Course. The farming-patch teleport stays as the alternative.
- The West Ardougne Recruiter is a few steps east of Dave's spellbook Ardougne teleport. The Ardougne lodestone stays as the other way there.
- Twirl on the Barbarian Village bridge is a short walk east from the Barbarian Village teleport. Edgeville lodestone, then south, stays as the alternative.
- A faster teleport does not replace the lodestone (or other) backup. Both stay on the card.
- Luthas is reached with an amulet of glory to Karamja, then a short walk south-east into the banana plantation. The Karamja lodestone, then east, is the backup. Charter ships and the Port Sarim crossing are not used.
- The Karamja shipyard, Musa Point docks, Entrana, and the Miscellania clues no longer send you onto a boat. Braindeath Island and the Witchaven fishing platform have no verified island teleport, so those cards say so instead of sending you to Pirate Pete or Holgart.
- Wendlewick still has no verified tile, so its marker was left unset.

**Install**
- The Windows installer shows a navy and gold Kavros splash while it unpacks.

## 0.6.7

A small hotfix for two things that slipped through in 0.6.6.

**Floating panel banner**
- The "Updated to ..." banner on the floating clue panel no longer covers the Rescan / Pause buttons. The panel now grows a little to make room for it and shrinks back afterwards.
- The banner now disappears on its own after 5 seconds instead of staying forever.

**Settings window**
- What's new now has a **Close** button in its header. You can also press **Esc**, or click the What's new button again, to close it.
- The bottom banner in Settings also clears itself after 5 seconds. The only exception is "Update now", which stays until you click it so you don't miss an update.
- These patch notes are more detailed.

## 0.6.6

**New look**
- Settings and the floating panel have a navy and gold restyle (thanks to PvML-Admin for the redesign).

**Routes and teleports**
- Every route uses the same layout: the **Fastest** way first, then an **Alternatively** option when a lodestone is still reasonably close, then the final step to the clue.
- Closer Kavros teleports are now suggested where they beat the lodestone, for example **Ctrl+T or ;;t** menu teleports (Artisan's Workshop, Gnome Agility Course, Barbarian Course, Sawmill, Taverley), Ring of wealth to Miscellania, Amulet of farming patches, and fairy rings.
- Clue fixes:
  - Port Khazard: no more Karamja boat route (Kavros has no boat NPCs). It now uses the Yanille lodestone, Khazard Teleport, fairy ring DJP or the Ardougne farming patch.
  - Louisa: the marker now points to the side room where she actually stands on Kavros.
  - Sir Kay, Miscellania, Party Pete, the Gnomeball referee, the Odd Old Man, the Barbarian Course emote and the Ardougne Monastery crate all have corrected or faster routes.
  - Doric, the Dwarven Mine chests, Wizards' Tower and several Falador and Taverley clues have new faster routes.

**Clue reading**
- Text clues are read from the inner parchment only, so the scroll border and the game background no longer confuse the reader.
- Stray junk words from the border are ignored, and a clue that fails to read is retried instead of being remembered as unknown.
- This fixes clues that didn't get recognised before, such as "The beasts to my east...", the "ME AM THE CALC" anagram and similar short clues.

**Updates and patch notes**
- No more popups. Update checks show a small banner at the bottom of the window, with an **Update now** button when a new version is out.
- After updating, a banner tells you what version you're on, and **What's new** in Settings shows these notes.

**Kavros only**
- The helper only attaches to a game window whose title contains "Kavros". It keeps working through revision updates, but won't run on other servers.

**Floating panel**
- The floating panel is tied to the Kavros window. It stays on top of the game while you play, and hides when you tab out to another app.

**Slider puzzles**
- The solver always goes straight for the best route it can find. There's no more "Use quick route" button.
- Whole-row moves are used, since Kavros lets you slide a full row or column in one click.
- Only the next click is highlighted, with a pulsing outline, an arrow and a "N left" counter, so it's much easier to keep track of where to click.
- Identical-looking tiles (like the two near-matching tree pieces) are tracked properly, so they no longer get swapped by mistake.

## 0.6.5

- Fixed the update check failing to connect (it now uses TLS 1.2).
- The app's .exe.config file now ships with the installer, which the update check needs.
