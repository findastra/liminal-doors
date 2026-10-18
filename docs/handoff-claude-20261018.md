# Handoff: Claude, 2026-10-18 (stop 01 hatched in Unity)

status: open (stop 01 rendered; local repo only, no GitHub repo yet)
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

## Not done / open

1. **No GitHub repo.** This is a local git repo on `claude/stop-01-pink-booth-20261018`. Creating `findastra/liminal-doors` needs Astra's OK.
2. **Walking:** only the booth floor (1.3 m box) is solid. The mound needs thin slabs inside its outline (Infinite Pole skill rule) before anyone walks out of the booth.
3. **Phone:** the booth's voice zone keeps its trigger box, but its two Udon scripts (AstraPhoneBooth / phone line) were left out. The "quiet telephone interaction" is still to do.
4. **Fox:** no glass fox yet. The idea inbox puts the animals in a later pass (glass-animal tool, Unity Unicorn PR #3 not merged).
5. **Doors:** no random forward doors and no return door yet (core traversal rule in the idea inbox).
6. Not tested: VRChat desktop test, headset and upload. The Scene view has no bloom, because the only camera with bloom is the inactive VRChat reference camera.

## Tool history (for Unity Unicorn)

| Tool | Completed | Report |
|---|---|---|
| `Tools/infinite-pole-export/extract_prefab.py` (scene subtree to prefab) | 2026-10-18 03:22 -06:00 | dropped 2 Udon MonoBehaviours, 0 external refs |
| `Tools/infinite-pole-export/deps.py` (GUID dependency copy list) | 2026-10-18 03:23 -06:00 | 26 files, 0 missing (built-ins and package scripts only) |
| `LiminalDoors/Stop 01/Build scene` + `Render review shots` (`LiminalStop01.cs`) | 2026-10-18 03:29:15 -06:00 | `Review/stop-01-render/report.md` |
