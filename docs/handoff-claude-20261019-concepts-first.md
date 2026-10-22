# Handoff: concepts first, Unity on hold (Claude, 2026-10-19)

status: open. **No Unity edits until Astra says the concepts all look good.**
Exceptions Astra made since: the booth fixes (b94f276), and on 2026-10-19 "do the next 2 levels", which covered stops 02
and 03 and items 3 and 4 below for those stops (see `handoff-claude-20261019-stops-02-03.md`). Items 1 and 2, for stop
01, are still open.

Astra, 2026-10-19: "save the unity changes for later, we are gonna make sure these concepts all look good before
editing the unity world any longer."

## Where the concepts are

- **Collection page.** A Claude artifact, owned by Astra and private, at https://claude.ai/artifact/XvXm6nJ3BQq8XPxukPVKJ2.
  - The chatgpt.site copy lives in Codex's site folder and goes live only when Codex publishes it.
- **The set has 34 floors.** Floors 34, 35 and 37 were removed as copies of other worlds; see
  `docs/originality-check-20261019.md`. Floor numbers are unchanged.
- **Floor 01 is now "Pinkscape" (Pinkscape Fox), not "Pink Booth"** (Astra, 2026-10-19). The booth is where you spawn.
  - Renamed so far: the page, `concept-art/README.md`, the floor file (`floors/01-fox-pinkscape-20261015.png`) and
    the redline sheet.
- **Redlines.** Astra marks up `concept-art/liminal-doors-redline-sheet-20261019.jpg` in Procreate and sends it back.
  Each floor's notes are read by its place on the grid.

## Unity to-do, for when Unity work starts again

1. **Rename stop 01 to Pinkscape.**
   - The scene root `Stop 01 - Fox - Pink Booth` becomes `Stop 01 - Fox - Pinkscape`.
   - Also change `StopName` in `Assets/LiminalDoors/Editor/LiminalStop01.cs`, the comments in `LiminalTraveler.cs`
     and `LiminalDoor.cs`, the door-trip test text, and the stops table in `README.md`.
2. **Spawn inside the booth.** Astra: "I want you to spawn in the booth."
   - Check that the spawn point is inside the rebuilt K6 kiosk (137b26a), facing the booth door.
   - Check that walking out through the booth door still sends you to a random door.
3. **The door portals must fill the whole doorway.** Astra: "the door portals need to fill up the square, in unity you
   got that kind of weird."
   - The glowing portal surface should cover the door opening edge to edge, with no gaps or overhang at the frame.
   - Check every door in stops 01 and 02.
4. **Faraway Field (stop 02) grass.** Astra: "need to work on the grass physics and texture. make it taller. make it
   interesting."
   - Make it taller.
   - Give it a better grass texture.
   - Add real motion: wind gusts in bands, and grass that bends when a player walks through it.

## Not done here

- Nothing in `Assets/` was changed for this handoff.
- The booth session's uncommitted Unity work in this folder is untouched.
