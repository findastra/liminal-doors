# Handoff: stops 02 and 03 (Claude, World Launch session, 2026-10-19)

status: open. Built, wired and tested in the editor, ClientSim and the VRChat desktop test. Not walked in the client
or a headset, not uploaded. Waiting on Astra's review.

## What Astra asked

"do the next 2 levels. focus on the grass for the first level. make it look like the grass in the Pink Universe world.
Also get the sparkles in the background from that world."

That lifts the Unity hold in `handoff-claude-20261019-concepts-first.md` for stops 02 and 03. Her earlier notes for
stop 02 also apply: "work on the grass physics and texture. make it taller. make it interesting", and "the door portals
need to fill up the square".

**Our own version, not taken from the world.** Pink Universe (wrld_e7f77f11…) recreates the pink realm from *Steven
Universe* (see `originality-check-20261019.md`). Assets can't be pulled out of another creator's world. So the grass and
sparkles here are written new to match its look: dense leafy pink fronds and a sparkly violet sky. Its lone tree and
sword, the Steven Universe parts, are left out on purpose. The only thing looked at was the world's public thumbnail,
from VRChat's public API.

## Stop 02, Hare / Faraway Field (`LiminalStop02.cs`)

- **Grass:** 44,990 tufts of tall pink fronds, 0.6–1.5 m high, 359,920 vertices in 39 tiles.
  - Each tuft is two crossed cards from a frond atlas the script paints (`Stop 02/Frond atlas.png`, four frond shapes).
  - The colour runs from deep rose at the root to pale pink-white at the tips.
  - Shader: `LiminalDoors/Frond Grass`, alpha-to-coverage for MSAA.
- **Grass physics:**
  - Wind gusts travel across the field in bands, and every tuft flutters on its own.
  - Tufts bend away from you as you walk through: `LiminalGrassPush` (U#) sets the global `_UdonLiminalPlayer` each
    frame. VRChat only allows Udon globals whose names start with `_Udon`.
  - World/Wire doors puts the push on the "Grass push (player)" marker.
  - Render `stop-02-07` shows the gap around a standing person.
- **Sparkles:**
  - `Evening Sky` has a new twinkling sparkle layer (three densities), big four-point glints and a faint nebula
    overhead. All of it is off unless set, so other skies don't change.
  - Particles: "Sky sparkles" drift far out (max 600) and "Field sparkles" glitter just above the grass (max 300).
- **Palette:** ground, path and haze are now pink. The stone is mauve, because grey read as green among the fronds.
- **Portals** fill each doorway exactly: 0.9 x 2.24 m between the posts, sill to lintel (`LiminalDoorKit.Portal`,
  shader `LiminalDoors/Portal`). They replace the oval glows.
- **Removed:** the old single-triangle grass (`Stop 02/Field grass.asset`) and three retired materials.

## Stop 03, Axolotl / Pool After Closing (`LiminalStop03.cs`, new)

- **Slot:** (6000, 0, 0). The hall is 14 x 32 x 6 m.
- **Pool:** 8.5 x 22 m and 1.4 m deep, water at -0.12 m. Five steps lead down at the near left corner, so you can walk
  in and back out. The water has no collider.
- **Tiles:** `Pool Tile`, world-space. Caustics move on everything under the water, and reflected caustics show on the
  ceiling. The deck has a wet sheen. Grout lines are anti-aliased so far walls don't shimmer.
- **Water:** `Pool Water`, with ripples, glancing pink reflections and glints.
- **Windows and skylight:** four underwater windows and a 6 x 10 m skylight of violet water (`Pool Window`).
- **Hall details:** ten columns with warm sconces, a chrome ladder, rising bubbles and hall motes.
- **Doors:**
  - The forward door is the pink double door at the far end, open toward you. Its portal fills the 1.9 x 2.6 m opening.
  - The way-back door is the pale door behind the arrival point, with a gold portal.
- The glass axolotl's spot is marked.

## Doors and tests (`LiminalDoorsTravel.cs`)

- **Build all:** now also builds stop 03.
- **Wire doors:** 3 stops and 5 doors, all checks PASS. Forward doors now pick among stops 02 and 03.
- **Trip test** (ClientSim): rewritten for random doors and any number of stops. All 10 checks PASS
  (`Review/world-doors/trip-test.md`). On the recorded run:
  - the booth door went to 03;
  - 03's way-back door returned to the booth;
  - the booth door went to 03 again, and 03's forward door to 02;
  - 02's way-back door returned to 03's return point;
  - a respawn put you back in the booth.
- **Desktop test** (10:33):
  - fresh `scene-standalonewindows64-liminal doors-bb332841-….vrcw`, 20.7 MB;
  - the same bundle in `--url=` of `output_log_2026-10-19_10-33-26.txt`, then "Finished entering world." at 10:33:37;
  - 0 exceptions and no Udon errors.
- Renders: `docs/renders/stop-02-0[1-8]-*.jpg` and `stop-03-0[1-7]-*.jpg`.

## Open

1. Astra's review of both stops.
2. Not tested: walking them in the VRChat client (computer use can't drive VRChat), headset, other players, and the
   grass push with a real avatar.
3. Performance not measured: the alpha-tested grass has a lot of overdraw.
4. The concepts-first list still holds for stop 01: rename it to Pinkscape, and check the spawn sits inside the booth.
5. The glass hare and the glass axolotl.
6. The scene and `Assets/SerializedUdonPrograms` changes from the SDK's earlier test build are included in this
   commit, because the rebuild re-saved the scene over them. `Assets/Astra/` is untracked and not mine; it is untouched.
