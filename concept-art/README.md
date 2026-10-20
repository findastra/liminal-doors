# Liminal Doors concept art: one image per floor

The 34 floors of the Concept Atlas, cut out of the seven concept plates so each can be edited on its own (Astra, 2026-10-18).
The plates were made with OpenAI Codex's built-in image generation on 2026-10-15 (art direction, not Unity scenes); the originals stay in Codex's outputs and the Atlas site.

- `floors/NN-animal-place-20261015.png`: one floor each, about 500 x 505 px, cut inside the plate's divider lines. Each still shows its floor number in the bottom-left corner, as on the plates.
- **Removed 2026-10-19 (Astra):** floors 34 (Chipmunk / Bubble Chair Cloud Lounge), 35 (Wolf / Midnight Glass Garden) and
  37 (Nautilus / Secret Undersea Galaxy), and plate 7's glass wolf and bubble chair reference panels. The
  originality check (`../docs/originality-check-20261019.md`) found them copied from other creators' VRChat worlds
  (Crystals Absence by Arkîe, Astral Bounds by Vectriel). Floor numbers are kept, so the collection skips 34, 35 and 37.
- **Floor 01 repainted 2026-10-20 (Astra):** "Get rid of the grass in the first floor. I just want it to be clouds
  instead. No green grass, no regular grass, just all clouds." The grass tufts and the wet stone path around the booth
  are painted out with the picture's own cloud sea (`tools/CloudFloor-20261020.cs`); the booth now rises out of the
  clouds. The grassy original is in git history and on plate 1, so `cut-floors.ps1` would bring it back: keep the 20261020 file.
- `floors-contact-sheet-20261019.png`: all 34 at a glance.
- `liminal-doors-redline-sheet-20261019.jpg`: the same layout at full size (3098 x 3374 px) for redlines in Procreate. The Concept Atlas's "Export all 34 for redlines" button saves this file; the + on each card makes a sheet of just the floors you pick. Labels read "NN place animal", as on the page. Send the marked-up sheet back to Claude, and each floor's notes are read by its place on the grid.

To change a floor, replace its file with your edit. A new date in the name (for example `-20261019`) keeps the original next to it.

| # | Animal | Place | File | Cut from |
|---|---|---|---|---|
| 01 | Fox | Pinkscape (you spawn in the pink booth) | `floors/01-fox-pinkscape-20261020.png` | plate 1, left column, row 1; repainted 2026-10-20 |
| 02 | Hare | Faraway Field | `floors/02-hare-faraway-field-20261015.png` | plate 1, right column, row 1 |
| 03 | Axolotl | Pool After Closing | `floors/03-axolotl-pool-after-closing-20261015.png` | plate 1, left column, row 2 |
| 04 | Raccoon | Last Laundrette | `floors/04-raccoon-last-laundrette-20261015.png` | plate 1, right column, row 2 |
| 05 | Gecko | Apricot Underpass | `floors/05-gecko-apricot-underpass-20261015.png` | plate 1, left column, row 3 |
| 06 | Crane | Platform Between Trains | `floors/06-crane-platform-between-trains-20261015.png` | plate 1, right column, row 3 |
| 07 | Moth | Velvet Waiting Room | `floors/07-moth-velvet-waiting-room-20261015.png` | plate 2, left column, row 1 |
| 08 | Frog | Rain That Misses You | `floors/08-frog-rain-that-misses-you-20261015.png` | plate 2, right column, row 1 |
| 09 | Hummingbird | Upside-Down Conservatory | `floors/09-hummingbird-upside-down-conservatory-20261015.png` | plate 2, left column, row 2 |
| 10 | Ibex | Salt Moon | `floors/10-ibex-salt-moon-20261015.png` | plate 2, right column, row 2 |
| 11 | Cat | Motel Without Rooms | `floors/11-cat-motel-without-rooms-20261015.png` | plate 2, left column, row 3 |
| 12 | Manta Ray | Tidal Carpet | `floors/12-manta-ray-tidal-carpet-20261015.png` | plate 2, right column, row 3 |
| 13 | Hedgehog | Vending Machine Clearing | `floors/13-hedgehog-vending-machine-clearing-20261015.png` | plate 3, left column, row 1 |
| 14 | Lynx | Lavender Quarry | `floors/14-lynx-lavender-quarry-20261015.png` | plate 3, right column, row 1 |
| 15 | Seahorse | Empty Aquarium | `floors/15-seahorse-empty-aquarium-20261015.png` | plate 3, left column, row 2 |
| 16 | Heron | Blackwater Footbridge | `floors/16-heron-blackwater-footbridge-20261015.png` | plate 3, right column, row 2 |
| 17 | Dragonfly | Soft Staircase | `floors/17-dragonfly-soft-staircase-20261015.png` | plate 3, left column, row 3 |
| 18 | Snow Leopard | Snow in the Lobby | `floors/18-snow-leopard-snow-in-the-lobby-20261015.png` | plate 3, right column, row 3 |
| 19 | Raven | Pearl Car Park | `floors/19-raven-pearl-car-park-20261015.png` | plate 4, left column, row 1 |
| 20 | Octopus | Room Beneath the Ocean | `floors/20-octopus-room-beneath-the-ocean-20261015.png` | plate 4, right column, row 1 |
| 21 | Deer | Wind Telephone | `floors/21-deer-wind-telephone-20261015.png` | plate 4, left column, row 2 |
| 22 | Firefly | Sleeping Arcade | `floors/22-firefly-sleeping-arcade-20261015.png` | plate 4, right column, row 2 |
| 23 | Kangaroo | Low-Gravity Court | `floors/23-kangaroo-low-gravity-court-20261015.png` | plate 4, left column, row 3 |
| 24 | Scarab | Ribbon Dunes | `floors/24-scarab-ribbon-dunes-20261015.png` | plate 4, right column, row 3 |
| 25 | Praying Mantis | Frosted Office | `floors/25-praying-mantis-frosted-office-20261015.png` | plate 5, left column, row 1 |
| 26 | Flamingo | Red Sun Reservoir | `floors/26-flamingo-red-sun-reservoir-20261015.png` | plate 5, right column, row 1 |
| 27 | Jellyfish | Room of Curtains | `floors/27-jellyfish-room-of-curtains-20261015.png` | plate 5, left column, row 2 |
| 28 | Butterfly | Glass Orchard | `floors/28-butterfly-glass-orchard-20261015.png` | plate 5, right column, row 2 |
| 29 | Red Panda | Midnight Food Court | `floors/29-red-panda-midnight-food-court-20261015.png` | plate 5, left column, row 3 |
| 30 | Swan | Door at Sunrise | `floors/30-swan-door-at-sunrise-20261015.png` | plate 5, right column, row 3 |
| 31 | Otter | Steam Between Stars | `floors/31-otter-steam-between-stars-20261015.png` | plate 6, left column, row 1 |
| 32 | Sandpiper | Sunset Sandbar | `floors/32-sandpiper-sunset-sandbar-20261015.png` | plate 6, right column, row 1 |
| 33 | Dolphin | Aquaglass Concourse | `floors/33-dolphin-aquaglass-concourse-20261015.png` | plate 6, left column, row 2 |
| 36 | Kingfisher | Blue-Sky Terminal | `floors/36-kingfisher-blue-sky-terminal-20261015.png` | plate 6, right column, row 3 |
