# Handoff: Claude, 2026-10-18 (stop 01 hatched in Unity)

status: open (stop 01 walkable, phone wired as a voice link, desktop test passed; fox and doors open)
from: Claude Opus 5.5, 2026-10-18
for: any agent continuing Liminal Doors

## What I did

Astra asked to resume Liminal Doors and see it in Unity, rendering only the first stop.

- Read the newest state first: Codex's floor register, idea inbox, concept plates and delivery receipt
  (`%USERPROFILE%\Documents\Codex\2026-10-15\we-re-going-to-create-a\outputs\`, 2026-10-15/16) and its Door Dormouse
  handoff (hub PR #61, 2026-10-18). Both say the world was "draft design; not built": no Unity world existed.
- Made this Unity project at `Documents\Astra's PetOS\liminal-doors` (name from the hub: pets.json, Door Dormouse `for: liminal-doors`).
  Packages, VRChat SDK 3.10.5 and ProjectSettings were copied from Astra's Infinite Pole; productName is now
  "Liminal Doors" with a new productGUID; build settings list only stop 01.
- Stop 01 (Fox / Pink Booth, floor register scene 1) is built from Infinite Pole's own assets, copied with their GUIDs:
  the "16 - Banner phone booth (Claude)" booth with its fused cloud mound and sparkles, "15 - Pink cloud sea (Claude)",
  the Pink Cloud Sea Sky and the Pinkscape Bloom profile (26 asset files, 13 MB). Infinite Pole was only read; its working tree was clean.
- Added: a gold "Door light drift" at the open door, an empty "Fox hiding spot" behind the booth base, the SDK's
  VRCWorld with the spawn inside the booth facing out of the door (respawn height -60) and an inactive reference camera with bloom.
- Renders (Unity 2022.3.22f1, D3D11, linear): `docs/renders/stop-01-0[1-5]-*.png` (hero, 15 m, bird's-eye,
  behind the booth, spawn view). All 11 renderers have their materials; no `error CS` or shader errors in the batch log or Editor.log.
- Opened the project in the Unity editor on stop 01; queue `status` reported the scene open, not compiling. SDK not signed in (new project).

## Round 2 (same day): walkable mound, phone, desktop test

Done in a side session, driving the open editor only through `Review/agent-queue` (no relaunch).

- **Walkable mound** (`Assets/LiminalDoors/Editor/LiminalStop01Slabs.cs`, menu *LiminalDoors/Stop 01/Mound walk surface (apply / validate / restore scene from backup)*).
  - Infinite Pole's flat 0.06 m box slabs were tried first. The mound is a 15 m dome (about 1.2 m higher in the middle than at the rim), so they broke into rings: 25 boxes covered 17% of the top and left a 1.2 m hole outside the door.
  - Replaced with one thin heightfield MeshCollider, "Cloud mound walk surface" (Default layer; mesh `Assets/LiminalDoors/Meshes/Cloud mound walk surface.asset`). It has 2,444 quads of 0.26 m and covers 162 m².
  - Every corner is at most the lowest visible top of the cells around it minus 2 cm, and at least 0.1 m in from the outline.
  - Validation (`Review/stop-01-slabs/report.md`, all PASS): 0% of 34,216 samples outside the outline or closer than 0.1 m to it, 0% above the 2 cm line. Stand-on 2,444/2,444, no side walls 12/12, bird's-eye 2,552 hits and 0 outside the cloud.
  - Overlays: `docs/renders/stop-01-walk-surface-top.png`, `-side.png`.
  - **Threshold exception, for Astra to judge:** the booth stands in a pocket of cloud. Right outside its walls the cloud is about 10 cm below the floor, then it rises into a 25–30 cm bank. On the booth floor plus a 12 cm wall ring, the surface is kept level with the floor, so it sits up to 0.32 m above the bottom of that pocket. Everywhere else it is under the cloud.
  - Walking straight out of the door: floor 0.04, then 0.05 / 0.07 / 0.10 / … / 0.30 m, with no gap and no up-step over 4 cm.
  - Scene backup taken before any mound collider: `Review/Backups/stop-01-slabs/` (not committed).
- **Phone** (`Assets/LiminalDoors/EditorUdon/Editor/LiminalStop01Phone.cs`, menu *LiminalDoors/Stop 01/Wire phone*).
  - `AstraPhoneBooth` / `AstraPhoneLine` (`.cs`, U# program `.asset` and compiled programs) are copied from Infinite Pole with their GUIDs into `Assets/Astra/Scripts/`.
  - The voice zone has AstraPhoneBooth with booth 0, linked to a new "Phone line" object with hub 0. UdonSharp compiled with no errors.
  - With one booth nobody is on the other end yet. **Astra (2026-10-18): keep the Infinite Pole voice link as the phone for now; the interaction gets designed when more stops have booths.**
- **Desktop test** (`LiminalDesktopTest.cs`; queue verb `desktop-test`; also menu *LiminalDoors/Desktop test*). Build & Test ran without an SDK sign-in.
  - New bundle `scene-standalonewindows64-01 fox - pink booth-f473fbb0-….vrcw` (4.1 MB, 04:09).
  - `output_log_2026-10-18_04-10-01.txt` has that bundle in `--url=`, "Successfully joined room" and "Finished entering world." (04:10:11).
  - No Udon exceptions and no respawns. Walking around in the client was not done (computer use can't drive VRChat).
  - The SDK added signatures to the 40 compiled Udon programs, and that change is committed.
- Renders redone (look unchanged; colliders and Udon are invisible): `docs/renders/stop-01-0[1-5]-*.png`.
- **Rebuild order:** *Build scene* writes a fresh scene, so after it run *Mound walk surface (apply)* and then *Wire phone*.

## Not done / open

1. **Phone:** decided by Astra on 2026-10-18. Keep the Infinite Pole voice link (linked voices between booths) for now. Design the "quiet telephone interaction" once more stops have booths; each new booth joins the same Phone line with the next booth number.
2. **Fox:** no glass fox yet. The idea inbox puts the animals in a later pass (glass-animal tool, Unity Unicorn PR #3 not merged).
3. **Doors:** no random forward doors and no return door yet (core traversal rule in the idea inbox).
4. Not tested: walking in the VRChat client, headset and upload. The Scene view has no bloom, because the only camera with bloom is the inactive VRChat reference camera.

## Tool history (for Unity Unicorn)

| Tool | Completed | Report |
|---|---|---|
| `Tools/infinite-pole-export/extract_prefab.py` (scene subtree to prefab) | 2026-10-18 03:22 -06:00 | dropped 2 Udon MonoBehaviours, 0 external refs |
| `Tools/infinite-pole-export/deps.py` (GUID dependency copy list) | 2026-10-18 03:23 -06:00 | 26 files, 0 missing (built-ins and package scripts only) |
| `LiminalDoors/Stop 01/Build scene` + `Render review shots` (`LiminalStop01.cs`) | 2026-10-18 03:29:15 -06:00 | `Review/stop-01-render/report.md` |
| `LiminalDoors/Stop 01/Mound walk surface (apply)` (`LiminalStop01Slabs.cs`) | 2026-10-18 04:04:42 -06:00 | `Review/stop-01-slabs/apply.md` |
| `LiminalDoors/Stop 01/Mound walk surface (validate)` | 2026-10-18 04:04:54 -06:00 | `Review/stop-01-slabs/report.md` |
| `LiminalDoors/Stop 01/Wire phone` (`LiminalStop01Phone.cs`) | 2026-10-18 04:08:42 -06:00 | `Review/stop-01-phone/report.md` |
| queue `desktop-test` (`LiminalDesktopTest.cs`) | 2026-10-18 04:10:01 -06:00 | `Review/desktop-test-status.txt`; client log `output_log_2026-10-18_04-10-01.txt` |
| `LiminalDoors/Stop 01/Render review shots` | 2026-10-18 04:11:01 -06:00 | `Review/stop-01-render/report.md` |
