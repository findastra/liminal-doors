# Liminal Doors

A VRChat world of doors that open onto quiet in-between places: an empty pool after closing, a laundrette with a sky
turning in one washer, a platform between trains. Behind every door hides one little glass animal. Hatching now: the
first door opens on Astra's pink phone booth, alone above a sea of pink clouds. PC VR / desktop.

The world's caretaker pet is **Door Dormouse** (pet program #82, The Threshold). The floor plan, concept book and idea
inbox were drafted by Codex on 2026-10-15/16.

![Stop 01: the booth, still plain](docs/renders/stop-01-01-door.png)

## Fresh start (2026-10-18)

This project starts from VRChat Creator Companion's **World template** (VRChat SDK 3.10.5, Unity 2022.3.22f1). From
Astra's Infinite Pole it takes only the **phone booth's shape** (six meshes in `Assets/LiminalDoors/Booth/Meshes`), and
that booth is being changed: for now it wears plain materials. An earlier version that copied much more of Infinite
Pole is kept on the branch `claude/stop-01-pink-booth-20261018`.

## Stops

| # | Animal | Stop | Status |
|---|---|---|---|
| 01 | Fox | Pink Booth (Pinkscape) | The handmade K6 booth with the spawn inside. The glass fox to come |
| 02 | Hare | Faraway Field | Tall pink frond grass that sways and parts as you walk, a sparkling dusk sky, door portals. The glass hare to come |
| 03 | Axolotl | Pool After Closing | A tiled pool hall: caustics, underwater windows, a violet skylight, the pink double door. The glass axolotl to come |

All stops live in one scene, `Assets/LiminalDoors/Scenes/Liminal Doors.unity`; stop 01 is its root `Stop 01 - Fox - Pink Booth`.
**LiminalDoors > World > Build all** rebuilds stops 02 and 03 and wires every door; renders are in `docs/renders/`.

![Stop 02: the Faraway Field](docs/renders/stop-02-01-arrival-view.jpg)
![Stop 03: Pool After Closing](docs/renders/stop-03-01-arrival-view.jpg)

## Working in it

- The VRChat SDK packages are unpacked into `Packages/` from Creator Companion's cache and aren't committed. Open the
  project in VCC or Unity 2022.3.22f1 to restore them.
- Menu **LiminalDoors > Stop 01**: *Build* rebuilds the stop (existing materials are kept), *Render* writes
  `Review/stop-01-render/`, *Open* frames the booth door. **LiminalDoors > Desktop test** runs the SDK's Build & Test (no upload).
- Agents drive an open editor through `Review/agent-queue/*.cmd` (`status`, `refresh`, `save`, `menu LiminalDoors/...`,
  `desktop-test`) instead of relaunching Unity.
- Uploads, releases and `main` change only with Astra's OK.
