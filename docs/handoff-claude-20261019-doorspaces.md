# Handoff: all 34 Doorspaces built, labelled and easy to jump between (Claude, 2026-10-19)

status: **done 2026-10-19 22:35.** Branch `claude/fresh-start-20261018` (PR #2, not merged).

Astra, 2026-10-19:
- "lets do the upside down conservatory next use the vine-spawn-tool we created in Unity Unicorn"
- "Show me how to navigate between scenes on Liminal Doors, I can only see the first scene."
- "go ahead and take your best crack at the rest of the Liminal Doorspaces (what I am calling each scene) I want to see
  them properly labeled on the hierarchy, make it easy for me to navigate"

This lifts the Unity hold in `docs/handoff-claude-20261019-concepts-first.md` for this work: Astra asked for it after
that note.

## Where everything is

- **One scene**, `Assets/LiminalDoors/Scenes/Liminal Doors.unity`. Each place behind a door is a **Doorspace**, a root in
  the Hierarchy named `Doorspace NN · Place Animal` (for example `Doorspace 09 · Upside-Down Conservatory Hummingbird`),
  kept in number order. Stops 01-03 were renamed to match (`Doorspace 01 · Pinkscape Fox`, `02 · Faraway Field Hare`,
  `03 · Pool After Closing Axolotl`).
- They sit 3 km apart (`Doorspace.SlotOf(n)`: six across, then the next row), so the Scene view only shows the one near
  you. That is why only the first one showed before.
- **Getting around:**
  - *LiminalDoors > Doorspaces > Window* (**Ctrl+Shift+D**) lists every Doorspace.
  - *Go* flies the Scene view to where you arrive and puts that Doorspace's sky up; *Door* flies to its forward door.
  - *Solo* hides the others in the Scene view only; *Show all* brings them back; *Rebuild* rebuilds the selected one.
  - **Ctrl+Shift+]** and **Ctrl+Shift+[** step to the next and previous one.
- 34 Doorspaces: 01-33 and 36. Floors 34, 35 and 37 were taken out after the originality check.

## What is new

- **The Doorspace kit** (`Editor/LiminalDoorspaceKit.cs`):
  - Pieces: rooms with door holes, walls, slabs, balls, rods, ground meshes, frond grass, rain, snow, motes, beacons,
    skies, and doors (glitter frames, a portal filling the opening, motes and a glitter halo).
  - Still parts are baked into one mesh per material per Doorspace (`Assets/LiminalDoors/Doorspaces/Doorspace NN/`).
    Colliders are merged onto one object, so the scene keeps few objects.
  - Balls use a light 16 x 10 sphere (187 vertices instead of Unity's 515). That took the generated meshes from 188 MB
    to 118 MB.
  - `Doorspace.Backing` sets the panel behind a door set in a wall. It is pale in the two glass rooms (09 and 33), where
    a dark one showed through the glass.
- **31 builders**, one per Doorspace, each from its concept in the register (`Editor/LiminalDoorspaces.cs`, `A`, `B`,
  `C`). The per-Doorspace notes are in the build report (the commit message lists them too).
- **The Upside-Down Conservatory (09)**, made with Unity Unicorn's vine tool:
  - The look follows the concept: a 180 m glass conservatory, 129 white planters hanging from the roof beams with
    their plants growing down (mint, lilac and rose crowns), and curtains of vines from the roof.
  - The tool is `vine-spawn-with-trumpet-flowers` (unity-unicorn PR #18, merged; this copy in
    `Assets/UnityUnicorn/Editor` is byte-identical to its `main`). `GrowVines` calls it by reflection, so the
    Doorspaces still build without it.
  - **Glass lines:** the tool only lets a hanging vine dangle a little in the air, so each vine trails down a strip of
    clear glass, 72 of them.
  - **Grown small, then scaled up:** it grows true-to-life vines (1 cm stems, 5-10 cm leaves), which read as wisps
    across a 16 m room. So they grow on a copy of each line at 1/1.7 size, and the result is scaled up 1.7x about the
    line's top.
  - Flowers come in five colours: #ff6fae #e040fb #9d6bff #fff4fb #ffb38a. The vines total 254k triangles; the meshes
    are in `Assets/UnityUnicorn/Generated/Vines/`.
- **The Glass shader** (`Shaders/Glass.shader`): clear or frosted glass with a fresnel edge and the Doorspace's haze.
- **Door travel:**
  - `LiminalDoorsTravel` reads stops by number from either name.
  - Stop 01 keeps its recorded Sunset sky.
  - *Build all* now also rebuilds Doorspaces 04-36 before it wires the doors.
- **Menus:**
  - *Rebuild the selected Doorspace* and *Rebuild the ones in rebuild.txt*. The second is for agents:
    `Review/doorspaces-build/rebuild.txt` holds the numbers; it rebuilds them, rewires and re-renders.
  - *Render review shots*: arrival, halfway and the door for each. A Doorspace that bends can set a
    `Halfway point` (the Underpass does).

## Checks (2026-10-19)

- **Door wiring:** 34 stops, 67 doors, all checks PASS (`Review/world-doors/report.md`).
- **Trip test (ClientSim, play mode):** 10 of 10 PASS. It went booth to Doorspace 05 and back, then booth to 21, to 28
  and back to 21, then a respawn to the booth.
- **Desktop test:**
  - The bundle `scene-...-5d3c76e7-...vrcw` is 33.0 MB (it was 22.1 MB with three Doorspaces), built 22:30:33.
  - The client log has the same bundle in `--url=` and "Finished entering world" at 22:31:15, with 0 exceptions.
  - Editor.log printed the usual "Build Finished, Result: Failure" at the bundle step.
- **Renders:** `docs/renders/doorspaces-20261019/sheet-NN.jpg` (four Doorspaces per sheet: arrival, halfway, the door)
  and `docs/renders/doorspace-09-*.jpg`.

## Not verified / open

- Not checked in a headset, with real VRChat walking physics, or with other players. Colliders were only checked by the
  trip test's door-to-door moves and by eye in the renders.
- **Effects are only marks, not behaviour:** the low-gravity circles (09, 23) are marks with no low gravity; the rain
  (08) and snow (18) are particles.
- **First-pass looks:** some Doorspaces are plain and need their own pass with Astra (13's trees, 14's quarry, 17's
  stairs, 36's windows). The glass animals are still to come everywhere.
- The car park (19) and the terminal (36) have pillars or benches on the centre line, so their halfway renders stand
  behind one.
- **Scene size:** the scene is 26 MB, mostly its 175 particle systems (about 114 KB of settings each). Making the door
  motes and glitter halos prefab instances would shrink it. Only the Doorspace you are in is switched on at runtime
  (the traveler turns the others off), so they cost nothing in play.
- Unity had been closed when this work resumed. It was reopened once with `-logFile %TEMP%\liminal-doors-editor.log`,
  because Poppy's editor was writing the shared `Editor.log`. Agents should read that file for this editor's
  compile errors.
- A stray `Review/agent-queue/d-refresh1.cmd` ran when the editor reopened (it was only a refresh).

## Tools used (for Unity Unicorn's tool history)

All of these were run through the agent queue on 2026-10-19. Each has a completed report in `Review/` (ignored by git):

| Tool | Last run | Report |
|---|---|---|
| `doorspace-builders`, `doorspace-kit` | 22:27:06 | `Review/doorspaces-build/report.md` |
| `vine-spawn-with-trumpet-flowers` (unity-unicorn `main`) | 22:27, inside the build | the Doorspace 09 section of the build report |
| `door-wiring` | 22:27 (rewire) | `Review/world-doors/report.md` |
| Trip test | 22:28:47 | `Review/world-doors/trip-test.md` |
| `desktop-test` | 22:29:16 | `Review/desktop-test-status.txt` |
| Doorspace renders | 22:27 | `Review/doorspaces-render/` |

The tools ledger `docs/tools-by-floor-20261019.json` lists the tools behind each Doorspace.
