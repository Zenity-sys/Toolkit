# Easy / Medium / Hard / Elite clue catalogue

Compiled 17 November 2026 for Kavros Clue Helper planning. This compilation does not change the released v0.5.5 program.

The linked RS3 guides were checked for tier/category coverage. Their full page downloads are restricted, so the detailed baseline uses the existing, pinned MIT-licensed Clue Trainer source snapshot. This is a reference catalogue, not a claim that every current wiki clue or every Kavros-specific variation is verified. Two newer wiki additions are listed separately rather than silently omitted.

| Tier | Structured baseline definitions | Additional wiki entries | Main categories |
|---|---:|---:|---|
| Easy | 100 | 1 newer Wendlewick search clue | Simple/search/talk, cryptic, emote, map |
| Medium | 121 | 0 identified here | Simple/search/talk, cryptic, emote, anagram, map, coordinates |
| Hard | 122 | 0 identified here | Cryptic, emote, anagram, map, coordinates |
| Elite | 21 | 1 newer Wendlewick scan region | 20 baseline scan regions and one compass pool |

The baseline contains 69 coordinate clues (27 Medium, 42 Hard), 40 anagram clues (24 Medium, 16 Hard) and 50 non-master emote clues (27 Easy, 13 Medium, 10 Hard). Master clues are excluded from this expansion catalogue. Follow-up challenge scrolls, sliding boxes and Celtic knots are secondary puzzles, not extra clue definitions.

Elite has 21 currently listed scan regions: the 20 baseline regions below plus Wendlewick. There are 524 baseline scan candidate tiles and 486 baseline compass candidate tiles in the pinned data, all included in elite-locations.csv. The current wiki's compass frequency numerator is 482; the 486-entry snapshot must be checked for moved/retired locations rather than assumed current. The range, floors and candidate tiles for Wendlewick were not established from the retrieved references. Newer Havenhythe content may not exist on Kavros revision 944.

Coordinates are converted using the standard origin X=2440, Y=3161 and 32 tiles per degree, with minute offsets rounded upward as documented by the Wiki's developer-console conversion. These are reference dig tiles requiring server verification. Plane values are game planes; plane 0 does not always mean the surface world for dungeon coordinates.

## Files

- all-clues.csv and one CSV for each tier: clue text/variants, action/answer, locations, gear, emotes, follow-up questions/conditional answers, references and review status.
- elite-scan-regions.csv: region, base range, Meerkat range, candidate count and planes.
- elite-locations.csv: all 1,010 baseline scan/compass candidate coordinates.
- catalogue.json: structured records for later implementation; image-recognition fingerprints are excluded.
- LICENSE-ClueTrainer.txt: licence for the reference source data.

## Reference sources

- User's coordinate guide: https://runescape.wiki/w/Treasure_Trails/Guide/Coordinates
- User's legacy full guide: https://runescape.fandom.com/wiki/Treasure_Trails/Full_guide
- User's emote guide: https://runescape.wiki/w/Treasure_Trails/Guide/Emotes
- Current anagrams: https://runescape.wiki/w/Treasure_Trails/Guide/Anagrams
- Current scans: https://runescape.wiki/w/Treasure_Trails/Guide/Scans
- Tier guides: https://runescape.wiki/w/Clue_scroll_(easy)/Guide ; https://runescape.wiki/w/Clue_scroll_(medium)/Guide ; https://runescape.wiki/w/Clue_scroll_(hard)/Guide ; https://runescape.wiki/w/Clue_scroll_(elite)/Guide
- Coordinate conversion: https://runescape.wiki/w/Developer_console
- Structured baseline: https://github.com/Leridon/cluetrainer/blob/9fce561ca9ef98c58b67ff98b50b578e153a878f/src/data/clues.ts

The Fandom guide is retained as a legacy cross-check. Modern/legacy names, NPC locations, quest-dependent answers and Kavros differences require review. The Medium legacy anagram Soy Drain -> Ysondria at the Nexus is kept separately in catalogue.json and is not counted as an active baseline entry.

## Elite scan regions

| Region | Base range | With Meerkat | Reference tiles | Game planes |
|---|---:|---:|---:|---|
| Brimhaven Dungeon | 14 | 19 | 42 | 0,2 |
| Dorgesh-Kaan | 16 | 21 | 20 | 0,1 |
| East or West Ardougne | 22 | 27 | 28 | 0 |
| Falador | 22 | 27 | 18 | 0 |
| Fremennik Isles of Jatizso and Neitiznot | 16 | 21 | 32 | 0 |
| Fremennik Slayer Dungeons | 16 | 21 | 19 | 0 |
| Haunted Woods | 11 | 16 | 21 | 0 |
| Isafdar and Lletya | 22 | 27 | 29 | 0 |
| Keldagrim | 11 | 16 | 18 | 0 |
| Kharazi Jungle | 14 | 19 | 26 | 0 |
| Menaphos | 30 | 35 | 21 | 0 |
| Mos Le'Harmless | 27 | 32 | 32 | 0 |
| Piscatoris Hunter Area | 14 | 19 | 26 | 0 |
| Taverley Dungeon | 22 | 27 | 25 | 0 |
| The caves beneath Lumbridge Swamp | 11 | 16 | 12 | 0 |
| The crater in the Wilderness | 11 | 16 | 19 | 0 |
| the deepest levels of the Wilderness | 25 | 30 | 39 | 0 |
| the desert, east of the Elid and north of Nardah | 27 | 32 | 51 | 0 |
| Varrock and the Grand Exchange | 16 | 21 | 24 | 0 |
| Wendlewick | Unverified | Unverified | Unverified | Unverified |
| Zanaris | 16 | 21 | 22 | 0 |

## Authorised implementation order and fixes

The user has now authorised implementation after catalogue compilation: Easy, then Medium, Hard and Elite, followed by the other bug fixes. Routine build/test decisions are authorised within this scope. Compile and test each stage; retain existing master support.

- Add clue recognition, answers/actions, locations/maps, coordinate conversion, equipment and conditional follow-up answers for the catalogued tiers.
- Include Elite scan regions, candidate dig locations, correct game planes, base/Meerkat ranges and minimap auto-follow. Keep unresolved reference gaps explicit.
- Fix scan tracking dropping an already acquired position; investigate brief misses, orientation/zoom changes, periodic full-window checks and state resets. Do not conceal stale positions as current.
- Fix knot numbers flickering/disappearing. Keep guidance visible during routine reads and update from the latest validated ring state.
- Automatically replace the old clue window and guidance when a new clue opens, including transitions between clue types. Clear old clue-specific state without requiring the Clear button. Keep useful current directions visible while travelling.
- Slider solving now works well according to the user. Keep that behaviour; distinguish the next four clicks with green, cyan, orange and purple, clear numbers and stronger outlines instead of three similar yellow previews.
- Publish patch notes in chat with every completed patch, including version, changes/fixes and known issues, alongside the ZIP path.

## Easy clue entries

| ID | Type | Clue / wording variants | Answer or action | Location | Gear / emotes / follow-up |
|---|---|---|---|---|---|
| CT:179 | Cryptic / riddle | A crate found in the tower of a church is your next location.  /  A crate in the tower of a church is your next location. | Search Crate | x=2612..2612, y=3306..3306, plane=1 |  |
| CT:180 | Cryptic / riddle | One of the sailors in Port Sarim is your next destination. | Speak to Captain Tobias. | at Port Sarim — x=3025, y=3215, plane=0; area 4x7 |  |
| CT:182 | Cryptic / riddle | Someone watching Het's Oasis is your next destination. | Speak to Jeed | north of Het's Oasis — x=3371, y=3271, plane=0; area 8x8 |  |
| CT:263 | Emote | Blow a raspberry at the monkey cage in Ardougne Zoo. Equip a studded leather body, bronze platelegs and a mud pie. | Raspberry; then speak to Uri | x=2597, y=3273, plane=0; area 11x11 | Studded leather body; Bronze platelegs; Mud pie; Raspberry |
| CT:266 | Emote | Blow raspberries outside the entrance to Keep Le Faye. Equip a studded leather coif, iron platebody and leather gloves. | Raspberry; then speak to Uri | x=2757, y=3398, plane=0; area 7x8 | Studded leather coif; Iron platebody; Leather gloves; Raspberry |
| CT:280 | Emote | Bow or curtsy at the entrance to Het's Oasis. Equip an iron chainbody, leather chaps and a studded leather coif. | Bow or Curtsy; then speak to Uri | x=3318, y=3225, plane=0; area 9x17 | Iron Chainbody; Leather Chaps; Leather Coif; Bow or Curtsy |
| CT:276 | Emote | Bow or curtsy outside the entrance to the Legends' Guild. Equip iron platelegs, an emerald amulet and an oak shieldbow. | Bow or Curtsy; then speak to Uri | x=2726, y=3346, plane=0; area 6x4 | Iron platelegs; Emerald Amulet; Oak Shieldbow; Bow or Curtsy |
| CT:284 | Emote | Cheer at the Druids' Circle. Equip an air tiara, bronze two-handed sword and a gold amulet. | Cheer; then speak to Uri | x=2917, y=3476, plane=0; area 10x11 | Air Tiara; Bronze 2h Sword; Gold Amulet; Cheer |
| CT:267 | Emote | Cheer for the monks at Port Sarim. Equip a studded leather coif, steel plateskirt and a sapphire necklace. | Cheer; then speak to Uri | x=3044, y=3234, plane=0; area 6x4 | Studded Leather coif; Steel plateskirt; Sapphire necklace; Cheer |
| CT:277 | Emote | Cheer in the centre of the Burthorpe Games Room. Have nothing equipped at all when you do. | Cheer; then speak to Uri | x=2194, y=4946, plane=0; area 28x28 | Nothing; Cheer |
| CT:270 | Emote | Clap in the main exam room of the Exam Centre. Equip a ruby amulet, blue flowers and leather gloves. | Clap; then speak to Uri | x=3351, y=3342, plane=0; area 9x8 | Ruby amulet; Blue flowers; Leather gloves; Clap |
| CT:264 | Emote | Clap on the causeway to the Wizards' Tower. Equip an iron helmet, emerald ring and leather gloves. | Clap; then speak to Uri | x=3101, y=3187, plane=0; area 4x23 | Iron helmet; Emerald ring; Leather gloves; Clap |
| CT:282 | Emote | Clap on the top level of the mill north of East Ardougne. Equip an emerald ring, wood camo top and an unenchanted tiara. | Clap; then speak to Uri | x=2630, y=3381, plane=2; area 6x6 | Emerald Ring; Wood Camo Top; Unenchanted Tiara; Clap |
| CT:281 | Emote | Cry outside the south gates of Fort Forinthry. Equip a hard leather body, leather chaps and a bronze hatchet. | Cry; then speak to Uri | x=3296, y=3520, plane=0; area 16x9 | Hard Leather Body; Leather Chaps; Bronze Hatchet; Cry |
| CT:279 | Emote | Dance a jig by the entrance to the Fishing Guild. Equip an emerald ring, sapphire amulet, and a bronze chainbody. | Jig; then speak to Uri | x=2611, y=3384, plane=0; area 5x3 | Emerald Ring; Sapphire Amulet; Bronze Chainbody; Jig |
| CT:278 | Emote | Dance at the crossroads north of Draynor. Equip an iron chainbody, sapphire ring and a shieldbow. | Dance; then speak to Uri | x=3108, y=3293, plane=0; area 4x4 | Iron Chainbody; Sapphire Ring; Shieldbow; Dance |
| CT:272 | Emote | Dance in the Party Room. Equip a steel full helmet, steel platebody and an iron plateskirt. | Dance; then speak to Uri | x=3036, y=3371, plane=0; area 20x15 | Steel full helmet; Steel platebody; Iron plateskirt; Dance |
| CT:8 | Emote | Dance in the shack in Lumbridge Swamp. Equip a bronze dagger, iron full helm and a gold ring. | Dance; then speak to Uri | x=3202, y=3167, plane=0; area 4x4 | Bronze Dagger; Iron full helm; Gold ring; Dance |
| CT:275 | Emote | Headbang in the mine north of Al-kharid. Equip a Polar Camo Top, Leather Gloves and Leather Boots. | Headbang; then speak to Uri | x=3296, y=3291, plane=0; area 8x26 | Polar Camo Top; Leather Gloves; Leather Boots; Headbang |
| CT:274 | Emote | Jump for joy at the beehives. Equip iron boots, an unholy symbol and a steel hatchet. | Jump for Joy; then speak to Uri | x=2752, y=3437, plane=0; area 15x14 | Iron boots; Unholy smybol; Steel hatchet; Jump for Joy |
| CT:260 | Emote | Laugh at the crossroads south of Sinclair Mansion. Equip a leather cowl, amulet of strength and iron scimitar. | Laugh; then speak to Uri | x=2737, y=3534, plane=0; area 8x8 | Leather cowl; Amulet of strength; Iron scimitar; Laugh |
| CT:261 | Emote | Panic in the limestone mine. Equip bronze platelegs, a steel pickaxe and a steel helmet. | Panic; then speak to Uri | x=3367, y=3497, plane=0; area 10x6 | Bronze platelegs; Steel pickaxe; Steel helm; Panic |
| CT:262 | Emote | Panic on the pier where you catch the Fishing Trawler. Have nothing equipped at all when you do. | Panic; then speak to Uri | x=2676, y=3163, plane=0; area 1x12 | Nothing; Panic |
| CT:269 | Emote | Shrug in the mine near Rimmington. Equip a gold necklace, gold ring and a bronze spear. | Shrug; then speak to Uri | x=2970, y=3230, plane=0; area 20x22 | Gold necklace; Gold ring; Bronze spear; Shrug |
| CT:259 | Emote | Think in the middle of the wheat field by the Lumbridge mill. Equip a sapphire necklace, polar camo legs and an oak shortbow. | Think; then speak to Uri | x=3155, y=3295, plane=0; area 6x9 | Sapphire necklace; Polar camo legs; Oak shortbow; Think |
| CT:265 | Emote | Twirl at the crossroads north of Rimmington. Equip a Sapphire ring, yellow flowers and leather chaps. | Twirl; then speak to Uri | x=2953, y=3239, plane=0; area 3x5 | Sapphire ring; Yellow flowers; Leather chaps; Twirl |
| CT:273 | Emote | Twirl in Draynor Manor by the fountain. Equip an iron platebody, studded chaps and a bronze full helm. | Twirl; then speak to Uri | x=3086, y=3333, plane=0; area 5x5 | Iron platebody; Studden chaps; Bronze full helm; Twirl |
| CT:268 | Emote | Wave on Mudskipper Point. Equip a gold ring, leather chaps and a steel mace. | Wave; then speak to Uri | x=2981, y=3106, plane=0; area 25x25 | Gold ring; Leather chaps; Steel mace; Wave |
| CT:283 | Emote | Yawn in Draynor Marketplace. Equip an iron kiteshield, steel longsword and studded leather chaps. | Yawn; then speak to Uri | x=3074, y=3246, plane=0; area 13x10 | Iron Kiteshield; Steel Longsword; Studded Leather Chaps; Yawn |
| CT:271 | Emote | Yawn in Varrock Palace library. Equip a holy symbol, leather vambraces and an iron warhammer. | Yawn; then speak to Uri | x=3207, y=3490, plane=0; area 8x8 | Holy symbol; Leather vamraces; Iron warhammer; Yawn |
| CT:56 | Map | X marks the spot at the end of a path north of a mine. | Dig two tiles south-east of the dead tree | x=3290, y=3373, plane=0 |  |
| CT:63 | Map | X marks the spot east of a tree near a small island on a river. | Dig three tiles east of the tree | x=3166, y=3360, plane=0 |  |
| CT:50 | Map | X marks the spot in front of a small house surrounded by trees and near a bridge. | Dig in front of the door | x=2612, y=3481, plane=0 |  |
| CT:55 | Map | X marks the spot near a statue. | Dig two tiles south-east of the bench | x=2971, y=3414, plane=0 |  |
| CT:54 | Map | X marks the spot near some rocks and a tree. | Dig west of the tree | x=3043, y=3399, plane=0 |  |
| CT:49 | Map | X marks the spot south of the wizard's tower. | Dig east of the rock | x=3103, y=3133, plane=0 |  |
| CT:99 | Simple / search / talk | Dig near some giant mushrooms behind the Grand Tree. | Dig behind the giant mushrooms | x=2459, y=3505, plane=0 |  |
| CT:84 | Simple / search / talk | Look in the ground floor crates of houses in Falador. | Search Crate | x=3029..3029, y=3355..3355, plane=0 |  |
| CT:105 | Simple / search / talk | Search a barrel east of Challenge Mistress Fara, in Burthorpe. | Search Barrel | x=2912..2912, y=3530..3530, plane=0 |  |
| CT:102 | Simple / search / talk | Search a barrel outside the mill, in Taverley. | Search Barrel | x=2894..2894, y=3418..3418, plane=0 |  |
| CT:100 | Simple / search / talk | Search a barrel outside the Pick and Lute inn, in Taverley. | Search Barrel | x=2886..2886, y=3449..3449, plane=0 |  |
| WIKI:easy-wendlewick-barrel | Simple / search / talk | Search a barrel upstairs in one of Wendlewick's farmhouses. | Search the barrel after climbing the ladder in the south-west building of Marigold Farm. | Marigold Farm, Wendlewick |  |
| CT:71 | Simple / search / talk | Search chests found in the upstairs of shops in Port Sarim. | Search Chest | x=3016..3016, y=3205..3205, plane=1 |  |
| CT:86 | Simple / search / talk | Search for a crate in a building in Hemenster. | Search Crate | x=2636..2636, y=3453..3453, plane=0 |  |
| CT:80 | Simple / search / talk | Search for a crate in Varrock Palace. | Search Crate | x=3224..3224, y=3492..3492, plane=0 |  |
| CT:75 | Simple / search / talk | Search for a crate on the ground floor of a house in Seers' Village. | Search Crate | x=2699..2699, y=3470..3470, plane=0 |  |
| CT:89 | Simple / search / talk | Search in the south-east corner of the Artisan Dwarves' workshop in Falador. | Search Crate | x=3060..3060, y=3334..3334, plane=0 |  |
| CT:95 | Simple / search / talk | Search the boxes in one of the tents in Al Kharid. | Search Boxes | x=3308..3308, y=3206..3206, plane=0 |  |
| CT:110 | Simple / search / talk | Search the boxes in the Goblin house near Lumbridge. | Search Boxes | x=3247..3247, y=3244..3244, plane=0 |  |
| CT:81 | Simple / search / talk | Search the boxes in the house in Al Kharid with racks of silk. | Search Boxes | x=3289..3289, y=3202..3202, plane=0 |  |
| CT:82 | Simple / search / talk | Search the boxes in the house near the south entrance of Varrock. | Search Boxes | x=3203..3203, y=3384..3384, plane=0 |  |
| CT:118 | Simple / search / talk | Search the chest in the Duke of Lumbridge's bedroom. | Search Closed Chest | x=3209..3209, y=3218..3218, plane=1 |  |
| CT:117 | Simple / search / talk | Search the chest in the left-hand tower of Camelot Castle. | Search Closed Chest | x=2748..2748, y=3495..3495, plane=2 |  |
| CT:112 | Simple / search / talk | Search the chests in Al Kharid palace. | Search Ornate chest | x=3301..3301, y=3164..3164, plane=0 |  |
| CT:78 | Simple / search / talk | Search the chests in the Dwarven Mine. | Search Chest | x=3000..3000, y=9798..9798, plane=0 |  |
| CT:126 | Simple / search / talk | Search the crate in the southern tower at the entrance to Lumbridge Castle. | Search Crate | x=3228..3228, y=3212..3212, plane=1 |  |
| CT:96 | Simple / search / talk | Search the crate near a cart in Port Khazard. | Search Crate | x=2660..2660, y=3149..3149, plane=0 |  |
| CT:92 | Simple / search / talk | Search the crates in a house in Yanille that has a piano. | Search Crate | x=2598..2598, y=3105..3105, plane=0 |  |
| CT:74 | Simple / search / talk | Search the crates in Canifis. | Search Crate | x=3509..3509, y=3497..3497, plane=0 |  |
| CT:85 | Simple / search / talk | Search the crates in Draynor Manor. | Search Crate | x=3106..3106, y=3369..3369, plane=2 |  |
| CT:115 | Simple / search / talk | Search the crates in East Ardougne's general store. | Search Crate | x=2615..2615, y=3291..3291, plane=0 |  |
| CT:108 | Simple / search / talk | Search the crates in Horvik's armoury. | Search Crate | x=3228..3228, y=3433..3433, plane=0 |  |
| CT:134 | Simple / search / talk | Search the crates in the Barbarian Village helmet shop. | Search Crate | x=3073..3073, y=3430..3430, plane=0 |  |
| CT:67 | Simple / search / talk | Search the crates in the chicken coop in the farm north of Port Sarim. | Search Crate | x=3018..3018, y=3287..3287, plane=0 |  |
| CT:101 | Simple / search / talk | Search the crates in the house at the north end of the East Ardougne Market. | Search Crate | x=2658..2658, y=3323..3323, plane=0 |  |
| CT:135 | Simple / search / talk | Search the crates in the Port Sarim fishing shop. | Search Crate | x=3012..3012, y=3222..3222, plane=0 |  |
| CT:87 | Simple / search / talk | Search the crates in yard of Citharede Abbey. | Search Crate | x=3415..3415, y=3158..3158, plane=0 |  |
| CT:113 | Simple / search / talk | Search the crates just outside the armour shop in East Ardougne. | Search Crate | x=2654..2654, y=3299..3299, plane=0 |  |
| CT:83 | Simple / search / talk | Search the crates near a cart in Varrock. | Search Crate | x=3226..3226, y=3452..3452, plane=0 |  |
| CT:119 | Simple / search / talk | Search the crates near the Lumbridge Market. | Search Crates | x=3192..3192, y=3257..3257, plane=0 |  |
| CT:133 | Simple / search / talk | Search the crates of Falador's general store. | Search Crate | x=2955..2955, y=3390..3390, plane=0 |  |
| CT:65 | Simple / search / talk | Search the crates outside the bank in the north of east-ardougne | Search Crate | x=2620..2620, y=3336..3336, plane=0 |  |
| CT:69 | Simple / search / talk | Search the drawers above the shops in Varrock. | Search Drawers | x=3206..3206, y=3419..3419, plane=1 |  |
| CT:2 | Simple / search / talk | Search the drawers found upstairs in the Westernmost Inn of East Ardougne. | Search Drawers | x=2575..2575, y=3326..3326, plane=1 |  |
| CT:66 | Simple / search / talk | Search the drawers in a house in Catherby. | Search Drawers | x=2830..2830, y=3448..3448, plane=0 |  |
| CT:79 | Simple / search / talk | Search the drawers in a house in Draynor Village. | Search Drawers | x=3097..3097, y=3277..3277, plane=0 |  |
| CT:130 | Simple / search / talk | Search the drawers in Catherby's Archery Shop. | Search Drawers | x=2828..2828, y=3457..3457, plane=0 |  |
| CT:111 | Simple / search / talk | Search the drawers in Falador's chainmail shop. | Search Drawers | x=2969..2969, y=3311..3311, plane=0 |  |
| CT:73 | Simple / search / talk | Search the drawers in Ivy's house in Taverley. | Search Drawers | x=2914..2914, y=3448..3448, plane=0 |  |
| CT:127 | Simple / search / talk | Search the drawers in one of Gertrude's bedrooms. | Search Drawers | x=3156..3156, y=3406..3406, plane=0 |  |
| CT:93 | Simple / search / talk | Search the drawers in the ground floor of a shop in Yanille. | Search Drawers | x=2570..2570, y=3085..3085, plane=0 |  |
| CT:0 | Simple / search / talk | Search the drawers of Hild and Penda's house in Burthorpe. | Search Drawers | x=2914..2914, y=3521..3521, plane=0 |  |
| CT:72 | Simple / search / talk | Search the drawers on the first floor of a building overlooking Ardougne market. | Search Drawers | x=2655..2655, y=3323..3323, plane=1 |  |
| CT:131 | Simple / search / talk | Search the drawers upstairs in Falador's shield shop. | Search Drawers | x=2971..2971, y=3386..3386, plane=1 |  |
| CT:123 | Simple / search / talk | Search the drawers upstairs in the bank to the East of Varrock. | Search Drawers | x=3250..3250, y=3420..3420, plane=1 |  |
| CT:76 | Simple / search / talk | Search through chests found in the upstairs of houses in eastern Falador. | Search Closed Chest | x=3041..3041, y=3364..3364, plane=1 |  |
| CT:90 | Simple / search / talk | Search through some drawers in the upstairs of a house in Rimmington. | Search Drawers | x=2970..2970, y=3213..3213, plane=1 |  |
| CT:91 | Simple / search / talk | Search upstairs in the houses of Seers' Village for some drawers. | Search Drawers | x=2716..2716, y=3471..3471, plane=1 |  |
| CT:106 | Simple / search / talk | Speak to Arhein in Catherby. | Speak to Arhein | in Catherby — x=2802, y=3425, plane=0; area 3x3 |  |
| CT:121 | Simple / search / talk | Speak to Doric, who lives north of Falador. | Speak to Doric | north of Falador — x=2955, y=3435, plane=0; area 9x9 |  |
| CT:116 | Simple / search / talk | Speak to Ellis in Al Kharid. | Speak to Ellis | in Al Kharid — x=3270, y=3193, plane=0; area 7x6 |  |
| CT:103 | Simple / search / talk | Speak to Gaius in Burthorpe. | Speak to Gaius | in Burthorpe — x=2926, y=3546, plane=0; area 6x7 |  |
| CT:68 | Simple / search / talk | Speak to Hans to solve the clue. | Speak to Hans | at Lumbridge Castle — x=3202, y=3205, plane=0; area 20x28 |  |
| CT:137 | Simple / search / talk | Speak to Ned in Draynor Village. | Speak to Ned | in Draynor — x=3097, y=3255, plane=0; area 6x6 |  |
| CT:136 | Simple / search / talk | Speak to Sir Kay in Camelot Castle. | Speak to Sir Kay | in the courtyard of Camelot Castle — x=2752, y=3493, plane=0; area 13x11 |  |
| CT:125 | Simple / search / talk | Speak to the bartender of the Blue Moon Inn in Varrock. | Speak to Bartender | in the Blue Moon Inn — x=3223, y=3396, plane=0; area 4x7 |  |
| CT:138 | Simple / search / talk | Speak to the Challenge Mistress in Burthorpe. | Speak to Challenge Mistress Fara | in Burthorpe — x=2887, y=3529, plane=0; area 3x3 |  |
| CT:88 | Simple / search / talk | Speak to the staff of Sinclair Mansion. | Speak to Louisa | in Sinclair Mansion — x=2736, y=3574, plane=0; area 2x7 |  |
| CT:107 | Simple / search / talk | Talk to the bartender of the Rusty Anchor in Port Sarim. | Speak to Bartender | in Port Sarim — x=3044, y=3255, plane=0; area 3x4 |  |
| CT:132 | Simple / search / talk | Talk to the Squire in the White Knights' castle in Falador. | Speak to Squire Asrol | in the couryard of the White Knight's castle — x=2970, y=3339, plane=0; area 8x8 |  |
| CT:124 | Simple / search / talk | Talk to Zeke in Al Kharid. | Speak to Zeke | in Al Kharid — x=3284, y=3186, plane=0; area 6x7 |  |

## Medium clue entries

| ID | Type | Clue / wording variants | Answer or action | Location | Gear / emotes / follow-up |
|---|---|---|---|---|---|
| CT:323 | Anagram | This anagram reveals who to speak to next: Aha Jar | Speak to Jaraah | at the northern end of Al Kharid — x=3290, y=3215, plane=0; area 8x5 |  |
| CT:329 | Anagram | This anagram reveals who to speak to next: Arc O Line | Speak to Caroline | in Witchhaven — x=2713, y=3290, plane=0; area 6x16  /  in her house in Witchhaven — x=2708, y=3280, plane=1; area 5x6 | How many fishermen are there on the Fishing Platform? Answer: 11 (Before 'Kennith's Concerns'); 0 (After 'Kennith's Concerns') |
| CT:328 | Anagram | This anagram reveals who to speak to next: Are Col | Speak to Oracle | on the peak of Ice Mountain — x=3008, y=3498, plane=0; area 12x6 | If x is 15 and y is 3, what is 3x + y?  Answer: 48 |
| CT:326 | Anagram | This anagram reveals who to speak to next: Bail Trims | Speak to Brimstail | in his cave at Tree Gnome Stronghold — x=2405, y=9815, plane=0; area 9x8 |  |
| CT:332 | Anagram | This anagram reveals who to speak to next: Dt Run B | Speak to Brundt the Chieftain | in Relekka Longhall — x=2657, y=3666, plane=0; area 4x5 | How many people are waiting for the next bard to perform? Answer: 4 |
| CT:14 | Anagram | This anagram reveals who to speak to next: Eek Zero Op | Speak to Zookeeper | in Ardougne Zoo — x=2607, y=3268, plane=0; area 14x14 | How many animals are in the Ardougne Zoo? Answer: 40 (Before Eagles' Peak and Hunt for Red Raktuber); 41 (After either Eagles' Peak or Hunt for Red Raktuber); 42 (After both Eagles' Peak and Hunt for Red Raktuber) |
| CT:341 | Anagram | This anagram reveals who to speak to next: El Ow | Speak to Lowe | in Varrock's Archery Store — x=3230, y=3420, plane=0; area 7x3 |  |
| CT:338 | Anagram | This anagram reveals who to speak to next: Err Cure It | Speak to Recruiter | in West Ardougne — x=2539, y=3302, plane=0; area 6x7 | How many houses have a cross on the door? Answer: 20 |
| CT:330 | Anagram | This anagram reveals who to speak to next: Goblin Kern | Speak to King Bolren | in Tree Gnome Village — x=2540, y=3167, plane=0; area 3x5 |  |
| CT:336 | Anagram | This anagram reveals who to speak to next: Got A Boy | Speak to Gabooty | in Tai Bwo Wannai Village — x=2786, y=3063, plane=0; area 16x8 | How many buildings are in the village? Answer: 11 |
| CT:337 | Anagram | This anagram reveals who to speak to next: Halt Us | Speak to Luthas | at Musa Point — x=2935, y=3152, plane=0; area 7x5 |  |
| CT:471 | Anagram | This anagram reveals who to speak to next: HE ANY FURTHER | Speak to Father Urhney | in the Lumbridge swamp — x=3203, y=3147, plane=0; area 8x5 | How many ore rocks are there in the nearby copper and tin mine? Answer: 8 |
| CT:322 | Anagram | This anagram reveals who to speak to next: Icy Fe | Speak to Fycie | in Rantz's cave — x=2646, y=9390, plane=0; area 8x9 |  |
| CT:340 | Anagram | This anagram reveals who to speak to next: Im Krom | Speak to Rommik | in Rimmington — x=2945, y=3193, plane=0; area 6x7 | How many rocks in the Rimmington mine cannot be used to make bronze? Answer: 7 |
| CT:339 | Anagram | This anagram reveals who to speak to next: Iz A Ammo Load For Mrs Yakkers | Speak to Moldark, Emissary of Zamorak | in Edgeville — x=3104, y=3508, plane=0; area 2x3 | How many bottles are there on the stall to the east of Mr Ex? Answer: 7 |
| CT:343 | Anagram | This anagram reveals who to speak to next: Lark In Dog | Speak to King Roald | in Varrock Castle — x=3217, y=3471, plane=0; area 8x8 | How many bookcases are there in the Varrock Palace library? Answer: 24 |
| CT:334 | Anagram | This anagram reveals who to speak to next: Me if | Speak to Femi | at gates of Tree Gnome Stronghold — x=2457, y=3380, plane=0; area 5x3 |  |
| CT:318 | Anagram | This anagram reveals who to speak to next: Nod med | Speak to Edmond | north of Ardougne castle — x=2564, y=3330, plane=0; area 7x7 | How many pigeon cages are there around the back of Jerico's house? Answer: 3 |
| CT:327 | Anagram | This anagram reveals who to speak to next: Ok Co | Speak to Cook | in Lumbridge Castle's kitchen — x=3208, y=3215, plane=0; area 3x1 | How many cannons does Lumbridge Castle have? Answer: 7 |
| CT:321 | Anagram | This anagram reveals who to speak to next: Or A Vile | Speak to Valerio | outside of the abbey — x=3399, y=3148, plane=0; area 3x3 | How many windows look out into the Citharede Abbey courtyard? Answer: 17 |
| CT:342 | Anagram | This anagram reveals who to speak to next: Peaty Pert | Speak to Party Pete | in the Party Room — x=3048, y=3372, plane=0; area 7x7 |  |
| CT:335 | Anagram | This anagram reveals who to speak to next: R Ak Mi | Speak to Karim | in Al Kharid — x=3269, y=3182, plane=0; area 3x3 | I have 16 kebabs, I eat one myself and share the rest equally between 3 friends. How many do they have each? Answer: 5 |
| CT:24 | Anagram | This anagram reveals who to speak to next: Stab Ob  /  This anagram reveals who to speak to next: Boast B | Speak to Sabbot | in his cave near Burthorpe — x=2267, y=4752, plane=0; area 5x9 |  |
| CT:320 | Anagram | This anagram reveals who to speak to next: Winston Lane | Speak to Nails Newton | near Taverley Lodestone — x=2880, y=3443, plane=0; area 3x3 | How many tables are there in the Pick and Lute Inn? Answer: 10 |
| CT:402 | Coordinates | 00 degrees 05 minutes south,01 degrees 13 minutes east | Dig at x=2479, y=3158, plane=0 | x=2479, y=3158, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:403 | Coordinates | 00 degrees 13 minutes south,14 degrees 00 minutes east | Dig at x=2888, y=3154, plane=0 | x=2888, y=3154, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:404 | Coordinates | 00 degrees 18 minutes south,09 degrees 28 minutes east | Dig at x=2743, y=3151, plane=0 | x=2743, y=3151, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:405 | Coordinates | 00 degrees 20 minutes south,23 degrees 15 minutes east | Dig at x=3184, y=3150, plane=0 | x=3184, y=3150, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:406 | Coordinates | 00 degrees 31 minutes south,17 degrees 43 minutes east | Dig at x=3007, y=3144, plane=0 | x=3007, y=3144, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:407 | Coordinates | 00 degrees 50 minutes north,24 degrees 16 minutes east | Dig at x=3217, y=3188, plane=0 | x=3217, y=3188, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:408 | Coordinates | 01 degrees 18 minutes south,14 degrees 15 minutes east | Dig at x=2896, y=3119, plane=0 | x=2896, y=3119, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:410 | Coordinates | 01 degrees 26 minutes north,08 degrees 01 minutes east | Dig at x=2697, y=3207, plane=0 | x=2697, y=3207, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:411 | Coordinates | 01 degrees 35 minutes south,07 degrees 28 minutes east | Dig at x=2679, y=3110, plane=0 | x=2679, y=3110, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:413 | Coordinates | 02 degrees 50 minutes north,06 degrees 20 minutes east | Dig at x=2643, y=3252, plane=0 | x=2643, y=3252, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:414 | Coordinates | 02 degrees 50 minutes north,21 degrees 46 minutes east | Dig at x=3137, y=3252, plane=0 | x=3137, y=3252, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:416 | Coordinates | 03 degrees 35 minutes south,13 degrees 35 minutes east | Dig at x=2875, y=3046, plane=0 | x=2875, y=3046, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:419 | Coordinates | 04 degrees 00 minutes south,12 degrees 46 minutes east | Dig at x=2849, y=3033, plane=0 | x=2849, y=3033, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:422 | Coordinates | 04 degrees 13 minutes north,12 degrees 45 minutes east | Dig at x=2848, y=3296, plane=0 | x=2848, y=3296, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:425 | Coordinates | 05 degrees 20 minutes south,04 degrees 28 minutes east | Dig at x=2583, y=2990, plane=0 | x=2583, y=2990, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:427 | Coordinates | 05 degrees 43 minutes north,23 degrees 05 minutes east | Dig at x=3179, y=3344, plane=0 | x=3179, y=3344, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:431 | Coordinates | 06 degrees 31 minutes north,01 degrees 46 minutes west | Dig at x=2383, y=3370, plane=0 | x=2383, y=3370, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:432 | Coordinates | 07 degrees 05 minutes north,30 degrees 56 minutes east | Dig at x=3430, y=3388, plane=0 | x=3430, y=3388, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:434 | Coordinates | 07 degrees 33 minutes north,15 degrees 00 minutes east | Dig at x=2920, y=3403, plane=0 | x=2920, y=3403, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:440 | Coordinates | 08 degrees 33 minutes north,01 degrees 39 minutes west | Dig at x=2387, y=3435, plane=0 | x=2387, y=3435, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:442 | Coordinates | 09 degrees 33 minutes north,02 degrees 15 minutes east | Dig at x=2512, y=3467, plane=0 | x=2512, y=3467, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:443 | Coordinates | 09 degrees 48 minutes north,17 degrees 39 minutes east | Dig at x=3005, y=3475, plane=0 | x=3005, y=3475, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:444 | Coordinates | 11 degrees 03 minutes north,31 degrees 20 minutes east | Dig at x=3443, y=3515, plane=0 | x=3443, y=3515, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:445 | Coordinates | 11 degrees 05 minutes north,00 degrees 45 minutes west | Dig at x=2416, y=3516, plane=0 | x=2416, y=3516, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:446 | Coordinates | 11 degrees 41 minutes north,14 degrees 58 minutes east | Dig at x=2919, y=3535, plane=0 | x=2919, y=3535, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:449 | Coordinates | 14 degrees 54 minutes north,09 degrees 13 minutes east | Dig at x=2735, y=3638, plane=0 | x=2735, y=3638, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:462 | Coordinates | 22 degrees 30 minutes north,03 degrees 01 minutes east | Dig at x=2537, y=3881, plane=0 | x=2537, y=3881, plane=0 (standard coordinate conversion; verify in Kavros) |  |
| CT:191 | Cryptic / riddle | A town with a different sort of night-life is your destination. Search for some crates in one of the houses. | Search Crate | x=3498..3498, y=3507..3507, plane=0 |  |
| CT:189 | Cryptic / riddle | After a hard day of spraying back the vegetation, you mite want to pop to the nearby forge and search the crates. | Search Crate | x=2399..2399, y=4471..4471, plane=0 |  |
| CT:197 | Cryptic / riddle | After trawling for bars, go to the nearest place to smith them and dig by the door. | Dig in front of the door | x=2656, y=3160, plane=0 |  |
| CT:196 | Cryptic / riddle | Being this far north has meant that these crates have escaped being battled over. | Search Crates | x=2519..2519, y=3259..3259, plane=0 |  |
| CT:217 | Cryptic / riddle | Brush off the sand and dig in the quarry. There is a wheely handy barrow to the east. Don't worry, it's coal to dig there - in fact, it's all oclay. | Dig west of the cart | x=3176, y=2916, plane=0 |  |
| CT:207 | Cryptic / riddle | By the town of the dead, walk south down a rickety bridge, then dig near the spotted fungus. | Dig next to the mushroom | x=3644, y=3494, plane=0 |  |
| CT:199 | Cryptic / riddle | Dig here if you are not feeling too well after travelling through the desert. Ali heartily recommends it. | Dig next to the well | x=3358, y=2971, plane=0 |  |
| CT:204 | Cryptic / riddle | Don't skip here, it's too muddy. You'll feel like a star if you dig here, though. | Dig on top of the starfish | x=3000, y=3110, plane=0 |  |
| CT:212 | Cryptic / riddle | Find a crate close to the monks that like to paaarty! | Search Crate | x=2614..2614, y=3204..3204, plane=0 |  |
| CT:190 | Cryptic / riddle | For any aspiring mage, I'm sure searching this bookcase will be a rewarding experience. | Search Bookcase | x=3366..3366, y=3319..3319, plane=1 |  |
| CT:194 | Cryptic / riddle | Go to the village being attacked by trolls and search the drawers in one of the houses. | Search Drawers | x=2928..2928, y=3552..3552, plane=0 |  |
| CT:211 | Cryptic / riddle | Go to this building to be illuminated, and check the drawers while you are there. | Search Drawers | x=2512..2512, y=3641..3641, plane=1 |  |
| CT:192 | Cryptic / riddle | Hay! Stop for a bit and admire the scenery, just like the tourism promoter says. | Search Hay bales | x=2524..2524, y=3438..3438, plane=0 |  |
| CT:195 | Cryptic / riddle | In a town where everyone has perfect vision, seek some locked drawers in a house that sits opposite a workshop. | Search Drawers | x=2709..2709, y=3478..3478, plane=0 |  |
| CT:187 | Cryptic / riddle | In a town where the guards are armed with maces, search the upstairs rooms of the public house. | Search Drawers | x=2575..2575, y=3326..3326, plane=1 |  |
| CT:220 | Cryptic / riddle | In a town where thieves steal from stalls, search for some drawers in the upstairs of a house near the bank. | Search Drawers | x=2610..2610, y=3324..3324, plane=1 |  |
| CT:213 | Cryptic / riddle | In a town where wizards are known to gather, search upstairs in a large house to the north. | Search Closed chest | x=2593..2593, y=3108..3108, plane=1 |  |
| CT:185 | Cryptic / riddle | In a village made of bamboo, look for some crates under one of the houses. | Search Crate | x=2800..2800, y=3074..3074, plane=0 |  |
| CT:193 | Cryptic / riddle | North of the best monkey restaurant on Karamja, look for the centre of the triangle of boats and search there. | Search Crate | x=2905..2905, y=3189..3189, plane=0 |  |
| CT:218 | Cryptic / riddle | Observe: In the crate just North of the stairs leading down, you will find the answer. | Search Crate | x=2438..2438, y=3165..3165, plane=0 |  |
| CT:184 | Cryptic / riddle | Probably filled with wizards' socks. | Search Cupboard | x=3113..3113, y=3153..3153, plane=2 |  |
| CT:203 | Cryptic / riddle | Search the upstairs drawers of a house in a village where pirates are known to have a good time. | Search Drawers | x=2809..2809, y=3165..3165, plane=1 |  |
| CT:198 | Cryptic / riddle | Sophind yourself some treasure by searching these boxes. | Search Crate | x=3280..3280, y=2787..2787, plane=0 |  |
| CT:209 | Cryptic / riddle | The dead, red dragon watches over this chest. He must really dig the view. | Search Closed chest | x=3354..3354, y=3349..3349, plane=0 |  |
| CT:215 | Cryptic / riddle | The gnomes' nearby cart must have collapsed under the weight of all the treasure in these boxes! | Search Gnome crates | x=2397..2397, y=3355..3355, plane=0 |  |
| CT:186 | Cryptic / riddle | The owner of this crate has a hunch that he put more than fish inside. | Search Crate | x=2770..2770, y=3172..3172, plane=0 |  |
| CT:202 | Cryptic / riddle | The rock cakes to the south are definitely more edible than the two rocks I buried the treasure between. | Dig between the rocks | x=2513, y=3041, plane=0 |  |
| CT:183 | Cryptic / riddle | The socks in these drawers are holier than thine, according to the tonsured owners. | Search Drawers | x=3056..3056, y=3497..3497, plane=0 |  |
| CT:206 | Cryptic / riddle | The treasure is buried in a small building full of bones. Here's a Hint: It's not near a graveyard. | Dig in the western shed | x=3356, y=3507, plane=0 |  |
| CT:210 | Cryptic / riddle | This crate clearly marks the end of the line for coal. | Search Crate | x=2691..2691, y=3508..3508, plane=0 |  |
| CT:201 | Cryptic / riddle | This crate holds a better reward than a broken arrow. | Search Crate | x=2671..2671, y=3437..3437, plane=0 |  |
| CT:205 | Cryptic / riddle | This crate is mine, all mine, even if it is in the middle of the desert. | Search Crate | x=3289..3289, y=3022..3022, plane=0 |  |
| CT:208 | Cryptic / riddle | This cupboard has treasure, pirate pots and corsair cutlery! | Search Cupboard | x=2811..2811, y=3160..3160, plane=0 |  |
| CT:214 | Cryptic / riddle | This temple is rather sluggish. The chest just inside the entrance, however, is filled with goodies. | Search Chest | x=2698..2698, y=9684..9684, plane=0 |  |
| CT:188 | Cryptic / riddle | Try not to step on any aquatic nasties while searching this crate. | Removed. Not on Kavros. Add it back if players get the Fishing Platform crate. |  |  |
| CT:222 | Cryptic / riddle | While a sea view is nice, it seems this church has not seen visitors in a while. Dig outside the rim of the round window for a reward. | Dig west of the church near Port Sarim | x=2992, y=3178, plane=0 |  |
| CT:200 | Cryptic / riddle | You'll need to have Doug Deep into the distant past to get to these sacks | Search Sacks | x=3155..3155, y=5727..5727, plane=0 |  |
| CT:181 | Cryptic / riddle | You'll need to look for a city with a central fountain. Look for a locked chest in the city's chapel. | Search Closed Chest | x=3256..3256, y=3487..3487, plane=0 |  |
| CT:7 | Emote | Beckon at the Digsite, near the eastern winch. Bow or curtsy before you talk to me. Equip a pointed red and black snelm, snakeskin boots, and an iron pickaxe. | Beckon then Bow or Curtsy; then speak to Uri | x=3375, y=3440, plane=0; area 7x7 | Pointed Red and Black Snelm; Snakeskin Boots; Iron Pickaxe; Beckon then Bow or Curtsy |
| CT:255 | Emote | Beckon in Tai Bwo Wannai. Clap before you talk to me. Equip green dragonhide chaps, a ring of duelling and a mithril helmet. | Beckon then Clap; then speak to Uri | x=2778, y=3057, plane=0; area 33x17 | Green dragonhide chaps; Ring of duelling; Mithril helm; Beckon then Clap |
| CT:253 | Emote | Cheer in the Barbarian Agility Arena. Headbang before you talk to me. Equip a steel platebody, maple shortbow and bronze boots. | Cheer then Headbang; then speak to Uri | x=2528, y=3542, plane=0; area 28x18 | Steel Platebody; Maple Shortbow; Bronze Boots; Cheer then Headbang |
| CT:256 | Emote | Cheer in the Ogre Pen in the Training Camp. Show you are angry before you talk to me. Equip a green dragonhide body and chaps, and a steel squareshield. | Cheer then Angry; then speak to Uri | x=2523, y=3373, plane=0; area 11x5 | Green dragonhide body; Green dragonhide chaps; Steel square shield; Cheer then Angry |
| CT:257 | Emote | Cry in the Catherby archery shop. Bow or curtsy before you talk to me. Equip a round red and black snelm, a hard leather body and an unblessed silver sickle. | Cry then Bow or Curtsy; then speak to Uri | x=2824, y=3454, plane=0; area 6x4 | Round red and black snelm; Hard leather body; Unblessed silver sickle; Cry then Bow or Curtsy |
| CT:254 | Emote | Cry on the platform of the south-west tree in the Gnome Agility Arena. Indicate 'no' before you talk to me. Equip a steel kiteshield, ring of forging and green dragonhide chaps. | Cry then No; then speak to Uri | x=2472, y=3418, plane=2; area 6x4 | Green Dragonhide Chaps; Steel Kiteshield; Ring of Forging; Cry then No |
| CT:248 | Emote | Dance a jig under Shantay's Awning. Bow or curtsy before you talk to me. Equip a pointed blue snail helmet and an air staff. | Jig then Bow or Curtsy; then speak to Uri | x=3303, y=3121, plane=0; area 4x4 | Pointed Blue Snelm; Air Staff; Jig then Bow or Curtsy |
| CT:252 | Emote | Dance in the centre of Canifis. Bow or curtsy before you talk to me. Equip a spiny helmet, mithril platelegs and an iron two-handed sword. | Dance then Bow or Curtsy; then speak to Uri | x=3485, y=3480, plane=0; area 19x17 | Spine Helm; Mithril Platelegs; Iron two-handed sword; Dance then Bow or Curtsy |
| CT:251 | Emote | Jump for joy in Yanille bank. Dance a jig before you talk to me. Equip an iron crossbow, adamant helmet and snakeskin chaps. | Jump for Joy then Jig; then speak to Uri | x=2609, y=3088, plane=0; area 5x10 | Iron Crossbow; Adamant Med Helmet; Snakeskin Chaps; Jump for Joy then Jig |
| CT:247 | Emote | Panic by the mausoleum in Morytania. Wave before you speak to me. Equip a mithril plateskirt, a maple shieldbow and no boots. | Panic then Wave; then speak to Uri | x=3490, y=3565, plane=0; area 25x17 | Mithril Plateskirt; Maple Shieldbow; No boots; Panic then Wave |
| CT:249 | Emote | Think under the lens in the Observatory. Twirl before you talk to me. Equip a mithril chainbody, green dragonhide chaps and a ruby amulet. | Think then Twirl; then speak to Uri | x=2439, y=3164, plane=1; area 3x3 | Mithril Chainbody; Green Dragonhide Chaps; Ruby Amulet; Think then Twirl |
| CT:250 | Emote | Twirl on the bridge by Barbarian Village. Salute before you talk to me. Equip an iron hatchet, steel kiteshield and a mithril full helmet. | Twirl then Salute; then speak to Uri | x=3102, y=3420, plane=0; area 7x2 | Iron Hatchet; Steel Kiteshield; Mithril Full Helm; Twirl then Salute |
| CT:258 | Emote | Yawn in the Castle Wars lobby. Shrug before you talk to me. Equip a ruby amulet, mithril scimitar and an iron square shield. | Yawn then Shrug; then speak to Uri | x=2436, y=3080, plane=0; area 13x20 | Ruby amulet; Mithril scimitar; Iron square shield; Yawn then Shrug |
| CT:45 | Map | A crate in the woods directly next to a building. | Search Crate | x=2658..2658, y=3488..3488, plane=0 |  |
| CT:44 | Map | A crate near a bridge over a river. | Search Crate | x=2565..2565, y=3248..3248, plane=0 |  |
| CT:62 | Map | X marks the spot at the end of a path starting from a rock and near a fishing spot. | Dig between the southern most willow tree and the small obelisk | x=3092, y=3225, plane=0 |  |
| CT:48 | Map | X marks the spot between lava lakes. | Dig behind the chaos altar at Ourania | x=2454, y=3230, plane=0 |  |
| CT:52 | Map | X marks the spot between some trees at the end of a path. | Dig between the signpost and the evergreen | x=2536, y=3865, plane=0 |  |
| CT:58 | Map | X marks the spot near a rock on a peninsula. | Dig at the tip of the Hobgoblin Peninsula, west of the Crafting Guild. | x=2906, y=3296, plane=0 |  |
| CT:61 | Map | X marks the spot near some weird looking grass | Dig four tiles west of the mushroom | x=3434, y=3265, plane=0 |  |
| CT:51 | Map | X marks the spot next to a building at the coast. | Dig west of the Chemist's House | x=2923, y=3210, plane=0 |  |
| CT:57 | Map | X marks the spot on a narrow landstrip near a road and a lot of trees. | Dig next to the bush | x=2667, y=3562, plane=0 |  |
| CT:60 | Map | X marks the spot on a small landmass surrounded by water. | Dig at the end of the branching path next to the crystal tree | x=2579, y=3597, plane=0 |  |
| CT:53 | Map | X marks the spot on a surprisingly detailled map next to a statue on a road. | Dig south of the giant head | x=2648, y=3231, plane=0 |  |
| CT:122 | Simple / search / talk | Speak to a referee. | Speak to Gnome ball referee | at the gate of the Gnomeball Field — x=2384, y=3487, plane=0; area 3x3 | What is 57 x 89 + 23? Answer: 5096 |
| CT:97 | Simple / search / talk | Speak to Brimstail. | Speak to Brimstail | in Brimstail's cave — x=2405, y=9815, plane=0; area 9x8 | What is 19 to the power of 3? Answer: 6859 |
| CT:94 | Simple / search / talk | Speak to Donovan, the family handyman. | Speak to Donovan the Family Handyman | at Sinclair Mansion — x=2740, y=3577, plane=1; area 5x6 |  |
| CT:114 | Simple / search / talk | Speak to Hajedy. | Speak to Hajedy | in Brimhaven — x=2779, y=3210, plane=0; area 4x4 |  |
| CT:128 | Simple / search / talk | Speak to Hazelmere. | Speak to Hazelmere | east of Yanille — x=2675, y=3085, plane=1; area 5x5 | What is 19 to the power of 3? Answer: 6859 |
| CT:120 | Simple / search / talk | Speak to Kangai Mau. | Speak to Kangai Mau | in Brimhaven — x=2790, y=3181, plane=0; area 6x5 |  |
| CT:98 | Simple / search / talk | Speak to Roavar. | Speak to Roavar | in Canifis' bar — x=3489, y=3471, plane=0; area 7x5 |  |
| CT:109 | Simple / search / talk | Speak to Ulizius. | Speak to Ulizius | near the Swamp Gate — x=3440, y=3458, plane=0; area 7x7 |  |

## Hard clue entries

| ID | Type | Clue / wording variants | Answer or action | Location | Gear / emotes / follow-up |
|---|---|---|---|---|---|
| CT:299 | Anagram | This anagram reveals who to speak to next: A Zen She | Speak to Zenesha | in her house south of Ardougne Market — x=2657, y=3291, plane=0; area 5x3 | slider |
| CT:288 | Anagram | This anagram reveals who to speak to next: An Paint Tonic | Speak to Captain Ninto | in the Dwarven Tunnel under the White Wolf Mountain — x=2868, y=9876, plane=0; area 3x2 | slider |
| CT:289 | Anagram | This anagram reveals who to speak to next: Arr! So I am a crust, and? | Speak to Ramara du Croissant | at the Piscatoris Fishing Colony — x=2338, y=3673, plane=0; area 9x6 | slider |
| CT:297 | Anagram | This anagram reveals who to speak to next: By Look | Speak to Bolkoy | in Tree Gnome Village — x=2525, y=3160, plane=1; area 7x5 | How many flowers are there in the clearing below this platform? Answer: 13 |
| CT:290 | Anagram | This anagram reveals who to speak to next: C On Game Hoc | Speak to Gnome Coach | north-east of the gnomeball field — x=2405, y=3497, plane=0; area 3x3 | How many gnomes on the gnome ball field have red patches on their uniforms? Answer: 6 |
| CT:300 | Anagram | This anagram reveals who to speak to next: Gulag Run | Speak to Uglug Nar | south of Castle Wars — x=2437, y=3047, plane=0; area 14x11 | slider |
| CT:291 | Anagram | This anagram reveals who to speak to next: He Do Pose. It Is Cultrrl, Mk? | Speak to Riki the sculptor's model | in eastern Keldagrim — x=2903, y=10206, plane=0; area 3x3 | slider |
| CT:287 | Anagram | This anagram reveals who to speak to next: I Eat Its Chart Hints Do U | Speak to Shiratti the Custodian | in Nardah — x=3420, y=2926, plane=0; area 11x5 | slider |
| CT:302 | Anagram | This anagram reveals who to speak to next: I Faffy Run | Speak to Fairy Nuff | north of the bank in Zanaris — x=2384, y=4466, plane=0; area 6x6 | slider |
| CT:301 | Anagram | This anagram reveals who to speak to next: Land Doomd | Speak to Odd Old Man | at the limestone mine — x=3358, y=3502, plane=0; area 7x9 | slider |
| CT:20 | Anagram | This anagram reveals who to speak to next: Me Am The Calc  /  This anagram reveals who to speak to next: Ace Match Elm  /  This anagram reveals who to speak to next: The Cal Came | Speak to Cam the Camel | north of Al Kharid — x=3284, y=3234, plane=0; area 11x8 | slider |
| CT:293 | Anagram | This anagram reveals who to speak to next: O Birdz A Zany En Pc | Speak to Cap'n Izzy No-Beard | at the Agility Arena — x=2806, y=3190, plane=0; area 4x3 | How many banana trees are there in the plantation? Answer: 33 |
| CT:296 | Anagram | This anagram reveals who to speak to next: Or Zinc Fumes Ward | Speak to Wizard Frumscone | in the basement of Wizard's Guild — x=2587, y=9488, plane=0; area 3x2 | slider |
| CT:292 | Anagram | This anagram reveals who to speak to next: Profs Lose Wrong Pie | Speak to Professor Onglewip | on the ground floor of Wizard's Tower — x=3103, y=3146, plane=0; area 12x20 | slider |
| CT:295 | Anagram | This anagram reveals who to speak to next: Red Art Tans | Speak to Trader Stan | in Port Sarim — x=3032, y=3190, plane=0; area 3x2 | slider |
| CT:298 | Anagram | This anagram reveals who to speak to next: Sequin Dirge | Speak to Queen Sigrid | in Miscellania's Throneroom — x=2499, y=3861, plane=1; area 4x4  /  in Etceteria Castle — x=2612, y=3871, plane=1; area 6x9 | slider |
| CT:401 | Coordinates | 00 degrees 00 minutes north,07 degrees 13 minutes west | Dig at x=2209, y=3161, plane=0 | x=2209, y=3161, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:409 | Coordinates | 01 degrees 24 minutes north,08 degrees 05 minutes west | Dig at x=2181, y=3206, plane=0 | x=2181, y=3206, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:412 | Coordinates | 02 degrees 33 minutes north,28 degrees 45 minutes east | Dig at x=3360, y=3243, plane=0 | x=3360, y=3243, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:415 | Coordinates | 03 degrees 03 minutes south,05 degrees 03 minutes east | Dig at x=2602, y=3063, plane=0 | x=2602, y=3063, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:417 | Coordinates | 03 degrees 39 minutes south,13 degrees 58 minutes east | Dig at x=2887, y=3044, plane=0 | x=2887, y=3044, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:418 | Coordinates | 03 degrees 45 minutes south,22 degrees 45 minutes east | Dig at x=3168, y=3041, plane=0 | x=3168, y=3041, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:420 | Coordinates | 04 degrees 03 minutes south,03 degrees 11 minutes east | Dig at x=2542, y=3031, plane=0 | x=2542, y=3031, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:421 | Coordinates | 04 degrees 05 minutes south,04 degrees 24 minutes east | Dig at x=2581, y=3030, plane=0 | x=2581, y=3030, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:423 | Coordinates | 04 degrees 16 minutes south,16 degrees 16 minutes east | Dig at x=2961, y=3024, plane=0 | x=2961, y=3024, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:424 | Coordinates | 04 degrees 41 minutes north,03 degrees 09 minutes west | Dig at x=2339, y=3311, plane=0 | x=2339, y=3311, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:426 | Coordinates | 05 degrees 37 minutes north,31 degrees 15 minutes east | Dig at x=3440, y=3341, plane=0 | x=3440, y=3341, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:428 | Coordinates | 05 degrees 50 minutes south,10 degrees 05 minutes east | Dig at x=2763, y=2974, plane=0 | x=2763, y=2974, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:429 | Coordinates | 06 degrees 00 minutes south,21 degrees 48 minutes east | Dig at x=3138, y=2969, plane=0 | x=3138, y=2969, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:430 | Coordinates | 06 degrees 11 minutes south,15 degrees 07 minutes east | Dig at x=2924, y=2963, plane=0 | x=2924, y=2963, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:433 | Coordinates | 07 degrees 22 minutes north,14 degrees 15 minutes east | Dig at x=2896, y=3397, plane=0 | x=2896, y=3397, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:435 | Coordinates | 07 degrees 41 minutes north,06 degrees 00 minutes east | Dig at x=2632, y=3407, plane=0 | x=2632, y=3407, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:436 | Coordinates | 07 degrees 43 minutes south,12 degrees 26 minutes east | Dig at x=2838, y=2914, plane=0 | x=2838, y=2914, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:437 | Coordinates | 08 degrees 03 minutes north,31 degrees 16 minutes east | Dig at x=3441, y=3419, plane=0 | x=3441, y=3419, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:438 | Coordinates | 08 degrees 05 minutes south,15 degrees 56 minutes east | Dig at x=2950, y=2902, plane=0 | x=2950, y=2902, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:439 | Coordinates | 08 degrees 26 minutes south,10 degrees 28 minutes east | Dig at x=2775, y=2891, plane=0 | x=2775, y=2891, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:441 | Coordinates | 09 degrees 22 minutes north,02 degrees 24 minutes west | Dig at x=2363, y=3461, plane=0 | x=2363, y=3461, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:447 | Coordinates | 12 degrees 48 minutes north,20 degrees 20 minutes east | Dig at x=3091, y=3571, plane=0 | x=3091, y=3571, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:448 | Coordinates | 13 degrees 46 minutes north,21 degrees 01 minutes east | Dig at x=3113, y=3602, plane=0 | x=3113, y=3602, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:450 | Coordinates | 15 degrees 48 minutes north,13 degrees 52 minutes east | Dig at x=2884, y=3667, plane=0 | x=2884, y=3667, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:451 | Coordinates | 16 degrees 20 minutes north,12 degrees 45 minutes east | Dig at x=2848, y=3684, plane=0 | x=2848, y=3684, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:452 | Coordinates | 16 degrees 30 minutes north,16 degrees 28 minutes east | Dig at x=2967, y=3689, plane=0 | x=2967, y=3689, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:453 | Coordinates | 16 degrees 35 minutes north,27 degrees 01 minutes east | Dig at x=3305, y=3692, plane=0 | x=3305, y=3692, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:454 | Coordinates | 17 degrees 50 minutes north,08 degrees 30 minutes east | Dig at x=2712, y=3732, plane=0 | x=2712, y=3732, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:455 | Coordinates | 18 degrees 03 minutes north,25 degrees 16 minutes east | Dig at x=3249, y=3739, plane=0 | x=3249, y=3739, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:456 | Coordinates | 18 degrees 22 minutes north,16 degrees 33 minutes east | Dig at x=2970, y=3749, plane=0 | x=2970, y=3749, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:457 | Coordinates | 19 degrees 43 minutes north,25 degrees 07 minutes east | Dig at x=3244, y=3792, plane=0 | x=3244, y=3792, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:458 | Coordinates | 20 degrees 05 minutes north,21 degrees 52 minutes east | Dig at x=3140, y=3804, plane=0 | x=3140, y=3804, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:459 | Coordinates | 20 degrees 07 minutes north,18 degrees 33 minutes east | Dig at x=3034, y=3805, plane=0 | x=3034, y=3805, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:460 | Coordinates | 20 degrees 33 minutes north,15 degrees 48 minutes east | Dig at x=2946, y=3819, plane=0 | x=2946, y=3819, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:461 | Coordinates | 21 degrees 24 minutes north,17 degrees 54 minutes east | Dig at x=3013, y=3846, plane=0 | x=3013, y=3846, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:463 | Coordinates | 22 degrees 35 minutes north,19 degrees 18 minutes east | Dig at x=3058, y=3884, plane=0 | x=3058, y=3884, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:464 | Coordinates | 22 degrees 45 minutes north,26 degrees 33 minutes east | Dig at x=3290, y=3889, plane=0 | x=3290, y=3889, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:465 | Coordinates | 24 degrees 26 minutes north,26 degrees 24 minutes east | Dig at x=3285, y=3943, plane=0 | x=3285, y=3943, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:466 | Coordinates | 24 degrees 56 minutes north,22 degrees 28 minutes east | Dig at x=3159, y=3959, plane=0 | x=3159, y=3959, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:467 | Coordinates | 24 degrees 58 minutes north,18 degrees 43 minutes east | Dig at x=3039, y=3960, plane=0 | x=3039, y=3960, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:468 | Coordinates | 25 degrees 03 minutes north,17 degrees 05 minutes east | Dig at x=2987, y=3963, plane=0 | x=2987, y=3963, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:469 | Coordinates | 25 degrees 03 minutes north,23 degrees 24 minutes east | Dig at x=3189, y=3963, plane=0 | x=3189, y=3963, plane=0 (standard coordinate conversion; verify in Kavros) | wizard |
| CT:146 | Cryptic / riddle | 'Small Shoe.' Often found with rod on mushroom. | Speak to Gnome trainer | at the Gnome Agility Course — x=2467, y=3426, plane=0; area 11x15 | slider |
| CT:159 | Cryptic / riddle | 'Throat mage seeks companionship. Seek answers inside my furniture if interested.' | Search Drawers | x=2666..2666, y=3238..3238, plane=1 |  |
| CT:216 | Cryptic / riddle | 46 is my number. My body is the colour of burnt orange and crawls among those with eight. Three mouths I have, yet I cannot eat. My blinking blue eye hides my grave. | Dig on top of the sapphire spawn | x=3169, y=3887, plane=0 |  |
| CT:142 | Cryptic / riddle | A great view: watch the rapidly drying hides get splashed. Check the box you are sitting on. | Search Boxes | x=2523..2523, y=3493..3493, plane=1 |  |
| CT:175 | Cryptic / riddle | A strange little man who sells armour only to those who've proven themselves to be unafraid of dragons. | Speak to Oziach | in Edgeville — x=3066, y=3514, plane=0; area 5x5 | slider |
| CT:165 | Cryptic / riddle | Aggie I see, Lonely and southern I feel, I am neither inside nor outside the house, yet no house would be complete without me. The treasure lies beneath me! | Dig in front of the window | x=3083, y=3257, plane=0 |  |
| CT:223 | Cryptic / riddle | And so on, and so on, and so on. Walking from the land of many unimportant things leads to a choice of paths. | Dig at the crossoads on Etceteria | x=2591, y=3879, plane=0 |  |
| CT:145 | Cryptic / riddle | As you desert this town, keep an eye out for a set of spines that could ruin nearby rugs: dig carefully around the greenery. | Dig next to the cacti west of Nardaah | x=3396, y=2918, plane=0 |  |
| CT:150 | Cryptic / riddle | Citric cellar. | Speak to Heckel Funch | in the south-east corner on the first floor of the Grand Tree — x=2487, y=3487, plane=1; area 7x3 | slider |
| CT:176 | Cryptic / riddle | Come to the evil ledge, Yew know yew want to. Try not to get stung.  /  Come to the Try not to get stung. | Dig next to the yew tree | x=3088, y=3468, plane=0 |  |
| CT:28 | Cryptic / riddle | Covered in shadows, the centre of the circle is where you will find the answer. | Dig at the centre of Mort'ton | x=3488, y=3287, plane=0 |  |
| CT:173 | Cryptic / riddle | Four blades I have, yet draw no blood, Still I turn my prey to powder. If you are brave, come search my roof, It is there my blades are louder. | Search Crate | x=3166..3166, y=3309..3309, plane=2 |  |
| CT:30 | Cryptic / riddle | Generally speaking, his nose was very bent | Speak to General Bentnoze | in Goblin Village — x=2954, y=3512, plane=0; area 7x4 | slider |
| CT:153 | Cryptic / riddle | His head might be hollow, but the crates nearby are filled with surprises. | Search Crate | x=3478..3478, y=3091..3091, plane=0 |  |
| CT:166 | Cryptic / riddle | I am a token of the greatest love. I have no beginning or end. My eye is red, I can fit like a glove. Go to the place where it's money they lend, And dig by the gate to be my friend. | Dig in front of the gate | x=3191, y=9825, plane=0 |  |
| CT:139 | Cryptic / riddle | I am head of the abbey with a cold breeze from the west.  /  'A bag belt only?' he asked his balding brothers | Speak to Abbot Langley | at the Edgeville Monastery — x=3055, y=3482, plane=0; area 5x6 |  |
| CT:221 | Cryptic / riddle | I lie lonely and forgotten in mid wilderness, Where the dead rise from their beds. Feel free to quarrel and wind me up, and dig while you shoot their heads. | Dig on top of the crossbow in the Graveyard of Shadows | x=3235, y=3673, plane=0 |  |
| CT:151 | Cryptic / riddle | I'm sure they will let ya buy some things here, as long as you are in good 'ealth. | Search Crate | x=2340..2340, y=3187..3187, plane=0 |  |
| CT:144 | Cryptic / riddle | Identify the back of this over-acting brother. (He's a long way from home.) | Speak to Hamid | on the beach in Menaphos Worker District — x=3131, y=2800, plane=0; area 5x5 | slider |
| CT:219 | Cryptic / riddle | If you didn't want to be here and in danger, you should lever things well enough alone. | Dig in front of the lever in deep wilderness | x=3154, y=3923, plane=0 |  |
| CT:32 | Cryptic / riddle | If you look closely enough, it seems that the archers have lost more than their needles. | Search Haystack | x=2671..2671, y=3416..3416, plane=0 |  |
| CT:147 | Cryptic / riddle | It seems to have reached the end of the line, and it's still empty. | Search Mine Cart | x=3041..3041, y=9820..9820, plane=0 |  |
| CT:152 | Cryptic / riddle | Mine was the strangest birth under the sun. I left the crimson sack. Yet life had not begun. Entered the world and yet was seen by none. | Dig on top of the Red spider's eggs spawn | x=2832, y=9586, plane=0 |  |
| CT:160 | Cryptic / riddle | Must be full of railings | Search Boxes | x=2576..2576, y=3464..3464, plane=0 |  |
| CT:154 | Cryptic / riddle | My giant guardians below the market streets would be fans of rock and roll, if only they could grab hold of it. Dig near my purple smoke! | Dig next to the cauldron | x=3161, y=9905, plane=0 |  |
| CT:170 | Cryptic / riddle | My home is grey, and made of stone, A castle with a search for a meal. Hidden in some drawers I am, across from a wooden wheel. | Search Drawers | x=3213..3213, y=3216..3216, plane=1 |  |
| CT:174 | Cryptic / riddle | My name is like a tree, yet it is spelt with a 'g'. Come see the fur which is right near me. | Speak to Wilough | near Varrock's fountain — x=3219, y=3434, plane=0; area 5x5 | slider |
| CT:140 | Cryptic / riddle | My name's a tirade, fishing is my trade, by the docks is where my fortune is made.  /  If a man carried my burden, he would break his back. I am not rich, but leave silver in my track. Speak to the keeper of my trail. | Speak to Gerrant | in the fishing shop in Port Sarim — x=3011, y=3223, plane=0; area 7x7 |  |
| CT:167 | Cryptic / riddle | Navigating to this crate will be a trial. | Search Crate | x=2648..2648, y=3662..3662, plane=0 |  |
| CT:141 | Cryptic / riddle | Often sought out by scholars of histories past, find me where words of wisdom speak volumes. | Speak to Examiner | in the Exam Centre — x=3351, y=3342, plane=0; area 9x8 | slider |
| CT:149 | Cryptic / riddle | Probably filled with books on magic. | Search Bookcase | x=3094..3095, y=3153..3152, plane=0 |  |
| CT:148 | Cryptic / riddle | Read 'How to Breed Scorpions' By O.W. Thathurt. | Search Bookcase | x=2702..2702, y=3409..3409, plane=1 |  |
| CT:169 | Cryptic / riddle | Snah? I feel all confused, like one of those cakes. | Speak to Hans | at Lumbridge Castle — x=3202, y=3205, plane=0; area 20x28 |  |
| CT:171 | Cryptic / riddle | Surprising? I bet he is...  /  I bet he is... | Speak to Sir Prysin | in Varrock Castle — x=3202, y=3470, plane=0; area 5x5 |  |
| CT:156 | Cryptic / riddle | The beasts to my east snap claws and tails. The rest to my west can slide and eat fish. The force to my north will jump and they'll wail, Come dig by my fire and make a wish. | Dig next to the torch in Ardougne Zoo | x=2598, y=3267, plane=0 |  |
| CT:162 | Cryptic / riddle | The cheapest water for miles around, but they react badly to religious icons. | Search Crate | x=3178..3178, y=2987..2987, plane=0 |  |
| CT:39 | Cryptic / riddle | There is no 'worthier' lord. | Speak to Lord Iorwerth | in Prifddinas — x=2185, y=3283, plane=1; area 3x3  /  at Iorwerth Camp — x=2198, y=3247, plane=0; area 12x12 | slider |
| CT:168 | Cryptic / riddle | This aviator is at the peak of his profession. | Speak to Captain Bleemadge | at the top of White Wolf Mountain — x=2849, y=3492, plane=1; area 3x3 | slider |
| CT:224 | Cryptic / riddle | This village has a problem with cartloads of the undead. Try checking the bookcase to find the answer. | Search Bookcase | x=2833..2833, y=2992..2992, plane=0 |  |
| CT:164 | Cryptic / riddle | Try not to let yourself be dazzled when you search these drawers. | Search Drawers | x=2561..2561, y=3323..3323, plane=0 |  |
| CT:178 | Cryptic / riddle | When no weapons are at hand, then is the time to reflect. In Saradomin's name, redemption draws closer. | Search Drawers | x=2818..2818, y=3351..3351, plane=0 |  |
| CT:158 | Cryptic / riddle | When you get tired of fighting, go deep, deep down until you need an antidote. | Search Crate | x=2576..2576, y=9583..9583, plane=0 |  |
| CT:155 | Cryptic / riddle | You can cook food on me, but don't cook any foodles - That would just be wrong. | Dig by the fire next to Captain Klemfoodle | x=2969, y=2975, plane=0 |  |
| CT:157 | Cryptic / riddle | You don't need to go hopping mad - or take steps - to get to this treasure: just be totally shellfish. | Dig on top of the shell | x=2519, y=3594, plane=0 |  |
| CT:163 | Cryptic / riddle | You have all of the elements available to solve this clue. Fortunately you do not have to go as far as to stand in a draft. | Search Crate | x=2723..2723, y=9891..9891, plane=0 |  |
| CT:161 | Cryptic / riddle | You will need to under-cook to solve this one. | Search Crate | x=3219..3219, y=9617..9617, plane=0 |  |
| CT:143 | Cryptic / riddle | You will need to wash the old ash off of your spade when you dig here, but the only water nearby is stagnant. | Dig next to the puddle on top of the mountain on Braindeath Island | x=2134, y=5162, plane=0 |  |
| CT:231 | Emote | Blow a kiss between the tables in Shilo Village bank. Beware of double agents! Equip a splitbark helm, mud pie and rune platebody. | Blow kiss; then speak to Uri | x=2851, y=2952, plane=0; area 3x3 | Splitbark helmet; Mud pie; Rune platebofy; Blow kiss; Double agent |
| CT:230 | Emote | Blow a raspberry in the Fishing Guild bank. Beware of double agents! Equip an elemental shield, blue dragonhide chaps and a rune warhammer. | Raspberry; then speak to Uri | x=2585, y=3420, plane=0; area 3x4 | Elemental shield; Blue dragonhide chaps; Rune warhammer; Raspberry; Double agent |
| CT:232 | Emote | Bow or curtsy at the top of the lighthouse. Beware of double agents! Equip a blue dragonhide body, blue dragonhide vambraces and no jewellery. | Bow or Curtsy; then speak to Uri | x=2504, y=3636, plane=2; area 10x10 | Blue dragonhide body; Blue dragon vambraces; No jewellry; Bow or Curtsy; Double agent |
| CT:229 | Emote | Dance at the cat-doored pyramid in Sophanem. Beware of double agents! Equip a ring of life, an amulet of glory and an adamant two-handed sword. | Dance; then speak to Uri | x=3292, y=2781, plane=0; area 6x4 | Ring of life; Uncharged amulet of glory; Adamant 2h sword; Dance; Double agent |
| CT:226 | Emote | Laugh in the Jokul's tent in the Mountain Camp. Beware of double agents! Equip a rune full helmet, blue dragonhide chaps and a fire battlestaff. | Laugh; then speak to Uri | x=2790, y=3667, plane=0; area 8x9 | Rune full helm; Blue dragonhide chaps; Fire battlestaff; Laugh; Double agent |
| CT:227 | Emote | Panic by the pilot on White Wolf Mountain. Beware of double agents! Equip mithril platelegs, a ring of life, and a rune hatchet. | Panic; then speak to Uri | x=2843, y=3489, plane=1; area 11x10 | Mithril platelegs; Ring of life; Rune hatchet; Panic; Double agent |
| CT:225 | Emote | Panic in the heart of the Haunted Woods. Beware of double agents! Have no items equipped when you do. | Panic; then speak to Uri | x=3611, y=3483, plane=0; area 11x11 | Nothing; Panic; Double agent |
| CT:228 | Emote | Salute in the banana plantation. Beware of double agents! Equip a diamond ring, amulet of power and nothing on your chest and legs. | Salute; then speak to Uri | x=2908, y=3154, plane=0; area 27x21 | Diamond ring; Amulet of power; Nothing on your chest and legs; Salute; Double agent |
| CT:243 | Emote | Shrug in the Zamorak temple, found in the eastern Wilderness. Beware of double agents! Equip bronze platelegs, an iron platebody and blue dragonhide vambraces. | Shrug; then speak to Uri | x=3234, y=3603, plane=0; area 12x12 | Bronze platelegs; Iron platebody; Blue dragonhide vambraces; Shrug; Double agent |
| CT:235 | Emote | Yawn near the Wilderness Bandit camp obelisk. Beware of double agents! Equip an iron square shield, blue dragon vambraces and an iron pickaxe. | Yawn; then speak to Uri | x=3031, y=3728, plane=0; area 9x9 | Iron square shield; Blue dragonhide vambraces; Iron pickaxe; Yawn; Double agent |
| CT:40 | Map | A crate at a group of three buildings. | Search Crate | x=2457..2457, y=3182..3182, plane=0 |  |
| CT:46 | Map | A crate in the courtyard of a castle. | Search Crate | x=3026..3026, y=3629..3629, plane=0 |  |
| CT:47 | Map | A crate outside of some fort's walls. | Search Crate | x=3312..3312, y=3528..3528, plane=0 |  |
| CT:59 | Map | X marks the spot between three small volcanoes. | Dig two tiles west of the large geyser | x=3021, y=3913, plane=0 |  |
| CT:42 | Map | X marks the spot between two tables. | Dig two tiles south of the northern bench | x=2722, y=3338, plane=0 |  |
| CT:43 | Map | X marks the spot in one of multiple column-like buildings. | Dig in the north-western corner of the building | x=2488, y=3308, plane=0 |  |
| CT:41 | Map | X marks the spot next to a tree behind a building in the corner of a city. | Dig inside, next to the south-eastern wall of the anvil building, or outside next to the tree | x=2616, y=3077, plane=0 |  |

## Elite clue entries

| ID | Type | Clue / wording variants | Answer or action | Location | Gear / emotes / follow-up |
|---|---|---|---|---|---|
| CT:399 | Compass | The compass shows where you need to go. | Record bearings from known positions; triangulate and verify the arrived state | 486 reference candidate tiles; see elite-locations.csv | wizard  /  slider  /  celticknot |
| CT:356 | Scan | This scroll will work in Brimhaven Dungeon. Orb scan range: 14 paces. | Scan Brimhaven Dungeon; dig at the detected candidate | 42 reference candidate tiles; see elite-locations.csv |  |
| CT:350 | Scan | This scroll will work in Isafdar and Lletya. Orb scan range: 22 paces. | Scan Isafdar and Lletya; dig at the detected candidate | 29 reference candidate tiles; see elite-locations.csv |  |
| CT:354 | Scan | This scroll will work in Menaphos. Orb scan range: 30 paces | Scan Menaphos; dig at the detected candidate | 21 reference candidate tiles; see elite-locations.csv |  |
| CT:357 | Scan | This scroll will work in Taverley Dungeon. Orb scan range: 22 paces. | Scan Taverley Dungeon; dig at the detected candidate | 25 reference candidate tiles; see elite-locations.csv |  |
| CT:365 | Scan | This scroll will work in the cave goblin city of Dorgesh-Kaan. Orb scan range: 16 paces. | Scan Dorgesh-Kaan; dig at the detected candidate | 20 reference candidate tiles; see elite-locations.csv |  |
| CT:361 | Scan | This scroll will work in the city of Zanaris. Orb scan range: 16 paces. | Scan Zanaris; dig at the detected candidate | 22 reference candidate tiles; see elite-locations.csv |  |
| CT:367 | Scan | This scroll will work in the crater of the Wilderness volcano. Orb scan range: 11 paces. | Scan The crater in the Wilderness; dig at the detected candidate | 19 reference candidate tiles; see elite-locations.csv |  |
| CT:362 | Scan | This scroll will work in the dark and damp caves below Lumbridge Swamp. Orb scan range: 11 paces. | Scan The caves beneath Lumbridge Swamp; dig at the detected candidate | 12 reference candidate tiles; see elite-locations.csv |  |
| CT:36 | Scan | This scroll will work in the deepest levels of the Wilderness. Orb scan range: 25 paces. | Scan the deepest levels of the Wilderness; dig at the detected candidate | 39 reference candidate tiles; see elite-locations.csv |  |
| CT:349 | Scan | This scroll will work in the desert, east of the Elid and north of Nardah. Orb scan range: 27 paces. | Scan the desert, east of the Elid and north of Nardah; dig at the detected candidate | 51 reference candidate tiles; see elite-locations.csv |  |
| CT:366 | Scan | This scroll will work in the Fremennik Slayer Dungeons. Orb scan range: 16 paces. | Scan Fremennik Slayer Dungeons; dig at the detected candidate | 19 reference candidate tiles; see elite-locations.csv |  |
| CT:360 | Scan | This scroll will work in the Kharazi Jungle. Orb scan range: 14 paces. | Scan Kharazi Jungle; dig at the detected candidate | 26 reference candidate tiles; see elite-locations.csv |  |
| CT:355 | Scan | This scroll will work in the Piscatoris Hunter Area. Orb scan range: 14 paces. | Scan Piscatoris Hunter Area; dig at the detected candidate | 26 reference candidate tiles; see elite-locations.csv |  |
| CT:358 | Scan | This scroll will work on the faraway island of Mos Le'Harmless. Orb scan range: 27 paces. | Scan Mos Le'Harmless; dig at the detected candidate | 32 reference candidate tiles; see elite-locations.csv |  |
| CT:363 | Scan | This scroll will work on the Fremennik Isles of Jatizso and Neitiznot. Orb scan range: 16 paces. | Scan Fremennik Isles of Jatizso and Neitiznot; dig at the detected candidate | 32 reference candidate tiles; see elite-locations.csv |  |
| CT:353 | Scan | This scroll will work within the dwarven city of Keldagrim. Orb scan range: 11 paces. | Scan Keldagrim; dig at the detected candidate | 18 reference candidate tiles; see elite-locations.csv |  |
| CT:359 | Scan | This scroll will work within the Haunted Woods. Orb scan range: 11 paces. | Scan Haunted Woods; dig at the detected candidate | 21 reference candidate tiles; see elite-locations.csv |  |
| CT:352 | Scan | This scroll will work within the walls of East or West Ardougne. Orb scan range: 22 paces. | Scan East or West Ardougne; dig at the detected candidate | 28 reference candidate tiles; see elite-locations.csv |  |
| CT:364 | Scan | This scroll will work within the walls of Falador. Orb scan range: 22 paces. | Scan Falador; dig at the detected candidate | 18 reference candidate tiles; see elite-locations.csv |  |
| CT:351 | Scan | This scroll will work within the walls of Varrock and the Grand Exchange Orb scan range: 16 paces | Scan Varrock and the Grand Exchange; dig at the detected candidate | 24 reference candidate tiles; see elite-locations.csv |  |
| WIKI:elite-wendlewick-scan | Scan | Wendlewick | Scan Wendlewick; detailed reference still needed | Wendlewick; detailed coordinates pending |  |

