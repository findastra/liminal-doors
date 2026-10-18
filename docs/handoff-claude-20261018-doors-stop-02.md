# Handoff: Claude, 2026-10-18 (door system and stop 02, on the first base)

status: superseded base; port to `claude/fresh-start-20261018` pending
from: Claude Opus 5.5, 2026-10-18
for: whoever ports the doors and stop 02 onto the fresh base

## Why this branch is already out of date

Astra asked this session for the door system and stop 02 ("Spawn in the booth, and stepping out through the booth door
sends you straight to a random door. Start building the door system and stop 2."). While it was being built, she told
the other session to **start fresh** (VCC World template, nothing from Infinite Pole but the booth's shape). That newer
instruction overrides the base this branch sits on (`claude/stop-01-pink-booth-20261018`). See
`docs/handoff-claude-20261018-fresh-start.md` on `claude/fresh-start-20261018` and PR findastra/liminal-doors#2.

## What is here, and what can move to the fresh base as it is

Own work, nothing from Infinite Pole (these can be ported as they are):
- **Door system:** `Assets/LiminalDoors/Scripts/LiminalTraveler.cs` and `LiminalDoor.cs` (UdonSharp) and
  `Assets/LiminalDoors/EditorUdon/Editor/LiminalDoorsTravel.cs` (menus *LiminalDoors/World/Make door scripts*,
  *Wire doors*, *Build all*, *Door trip test (play mode)*).
  - One scene holds every stop, each in its own slot 3 km apart.
  - Each stop root has `Arrival point`, `Return point`, `Forward door trigger` and `Arrival door trigger` (stop 01 has no arrival door).
  - Only the current stop is switched on, on each player's own screen. Forward doors are random, never the booth, and avoid recent stops once there are enough.
  - Arrival doors take you back along your own path. A respawn resets you to the booth.
- **Stop 02, Hare / Faraway Field:** `Assets/LiminalDoors/Editor/LiminalStop02.cs`, and the shaders `Field Grass`, `Field Ground`, `Soft Lit` and `LiminalField.cginc`.
  - A generated field with a walkable ground collider and a worn path. 119,215 grass blades, 5,183 of them wildflowers, bending in wind bands.
  - A flat stone, the hare's hiding spot (empty), the rose door just past a 2.35 m rise, and a pale way-back door.

Taken from the first base, so they must be replaced on the fresh base:
- Stop 02's sky uses Infinite Pole's `Astra/Cloud Sea Sky` shader. Its doorway light and motes use `Astra/Soft Sparkle`.
- Stop 02's bloom uses Infinite Pole's `Pinkscape Bloom` profile and the post-processing package, which the VCC template doesn't have.
- Stop 01's door anchors were added to the first base's `LiminalStop01.cs`. The fresh `LiminalStop01.cs` needs the same
  three anchors, its VRCWorld at the scene root, and its own sky material listed in `LiminalDoorsTravel.SkyFor`.
- *Build all* runs the first base's stop 01 steps (mound walk surface, phone); the fresh base has neither.

## Checked here (first base, open editor, driven through the agent queue)

- Every script and shader compiled; both U# programs compiled. That includes `RenderSettings.skybox` and `TeleportTo`, so Udon allows them.
- *Build all*: every step OK. *Wire doors*: 2 stops, 3 doors, all checks PASS (no landing spot inside a door, stops 3 km apart, VRCWorld outside every stop).
- *Door trip test* in ClientSim: 9 of 9 PASS (`docs/door-trip-test-20261018.md`).
  - Booth door to stop 02; the way-back door back into the booth, with the sky switching both ways.
  - The rose door leads back to the booth until stop 03 exists. A respawn puts you in the booth.
- Renders: `docs/renders/stop-02-0[1-6]-*.png`.
- The SDK control panel logs an `IndexOutOfRangeException` when *Build scene* opens an empty scene. It's the SDK's own validation UI, not this code.
- Not tested: walking it in the VRChat client, other players, headset. The generated grass mesh is 27 MB, and each rebuild makes a new asset. The port should update it in place, or ignore it in git and rebuild.
