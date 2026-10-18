# Handoff: Claude, 2026-10-18 (door system and stop 02 on the fresh base)

status: open (door trip passes in ClientSim; VRChat desktop test passes; walking it in the client not done)
from: Claude Opus 5.5, 2026-10-18 (the doors session)
for: any agent continuing Liminal Doors

## What Astra asked

- "Spawn in the booth, and stepping out through the booth door sends you straight to a random door. Start building the door system and stop 2."
- Fresh start (to the booth session): nothing from Infinite Pole but the booth's shape.
- Bloom: "add Unity's own post-processing package to the fresh base ... with a new bloom profile (not Infinite Pole's)."

A first version on the old copy is kept on `claude/doors-and-stop-02-20261018` (b2d94b6). This is its port onto
`claude/fresh-start-20261018`. Here, the parts that came from Infinite Pole (sky and sparkle shaders, the Pinkscape bloom) are replaced by new ones.

## What is here

- **Door system** (UdonSharp): `Assets/LiminalDoors/Scripts/LiminalTraveler.cs` and `LiminalDoor.cs`, wired by
  `Assets/LiminalDoors/EditorUdon/Editor/LiminalDoorsTravel.cs`.
  - The world is one scene, and each stop is a root `Stop NN - ...` in its own slot, 3 km apart.
  - A traveler at the scene root switches only your current stop on (on your screen) and sets that stop's sky.
  - Forward doors pick a random stop: never the booth, never the one you're in, and they avoid your last few once there are enough stops. Arrival doors take you back along your own path. A respawn puts you in the booth.
  - Menus: *LiminalDoors/World/Make door scripts*, *Wire doors*, *Build all*, *Door trip test (play mode)*.
- **Stop 01:** `LiminalStop01.cs` gets the three door anchors (Arrival point, Return point, Forward door trigger just outside the booth door).
  - Build adds them, and *LiminalDoors/Stop 01/Add door anchors* adds them to the existing stop without rebuilding the booth.
  - *Build all* never runs Stop 01/Build, because the booth is being made by hand with Astra.
- **Stop 02, Hare / Faraway Field:** `Assets/LiminalDoors/Editor/LiminalStop02.cs`.
  - A generated 400 m evening field: a walkable ground collider, a worn path, 119,215 grass blades (5,183 of them white or lavender wildflowers) bending in broad wind bands, and a flat stone.
  - The hare's hiding spot is marked (the glass hare comes later). The rose door is just past a 2.35 m rise, 30 m from where you arrive. The pale way-back door has gold light.
  - Shaders, all new: `Field Grass`, `Field Ground`, `Soft Lit`, `Evening Sky` and `Soft Glow`.
  - The field meshes (`Assets/LiminalDoors/Stop 02/`, 33 MB) are generated once and reused. *Stop 02/Rebuild field meshes* makes them again.
- **Bloom:** `com.unity.postprocessing` 3.4.0 is now listed in `Packages/manifest.json`.
  - The VRChat SDK already pulled it in: com.vrchat.base asks for 3.2.2, and Unity resolved it to 3.4.0. So nothing new was installed, and 3.4.0 avoids a downgrade.
  - `LiminalWorldGlow.cs` makes the new profile `Assets/LiminalDoors/Materials/Liminal Glow.asset`: bloom only, threshold 1.05, so only the HDR door light and motes glow.
  - It also adds an off "VRChat reference camera" at the scene root with a post-processing layer, and points VRCWorld at it.
  - Stop 02 carries its own global volume, so the glow is on only while you're there.

## Checked (fresh base, open editor, driven through the agent queue with `c1-` job names)

- Everything compiles with no script or shader errors, and both U# programs compile.
- *Build all*: OK at every step. *Wire doors*: 2 stops, 3 doors, all checks PASS.
- *Door trip test* in ClientSim: 9 of 9 PASS (`docs/door-trip-test-fresh-20261018.md`).
  - The booth door takes you to stop 02's arrival point with the sky switched. The way-back door returns you to the booth.
  - With no stop 03 yet, the rose door leads back to the booth. A respawn puts you in the booth.
- Renders: `docs/renders/stop-02-0[1-6]-*.png`.

## Not done / open

1. ~~The VRChat desktop test can't build yet~~ **Done, 2026-10-18 12:30.**
   - With Astra's OK, *LiminalDoors/World/Set up VRChat layers* (`EditorUdon/Editor/LiminalVrchatLayers.cs`) ran the SDK's *Setup Layers for VRChat* and *Setup Collision Matrix*. Both went from False to True (`Review/world-layers/report.md`).
   - Only layer 3 moved: it was "reserved3" and is now VRChat's "Item" (reserved3 went to 22). Every object in the scene and in `Booth/Phone Booth.prefab` is on Default (0), so nothing needed reassigning.
   - The desktop test (queue `desktop-test`) built `scene-standalonewindows64-liminal doors-193f469f-....vrcw` (8.2 MB).
   - `output_log_2026-10-18_12-30-26.txt` has that bundle in `--url=`, "Successfully joined room" (12:30:33) and "Finished entering world." (12:30:36). It shows 0 exceptions and no Udon errors, so the door traveler's Start ran cleanly.
2. Not tested: walking through the doors in the VRChat client (computer use can't drive VRChat), other players, headset.
3. The README's stop table still lists only stop 01. The booth session owns that file right now.
4. Stop 01 has no visible gold light at its door yet (the booth is being redesigned); the trigger is there.
