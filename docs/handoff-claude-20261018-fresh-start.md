# Handoff: Claude, 2026-10-18 (fresh start)

status: open (fresh base built and rendered; waiting for Astra's booth changes; not yet swapped into the PC folder)
from: Claude Opus 5.5, 2026-10-18
for: any agent continuing Liminal Doors

## Why

Astra, 2026-10-18: "start fresh, do not start with a copy of Astra's Infinite Pole. The only thing you were supposed
to take from there was the phone booth as a base, and we are changing it." This overrides the first version (the
branch `claude/stop-01-pink-booth-20261018`). That version copied Infinite Pole's packages, project settings, cloud
mound, cloud sea, sky, bloom and phone scripts.

## What I did

- Branch `claude/fresh-start-20261018`, cut from `main`. The old files are removed in the same commit, so main changes
  only when Astra merges the pull request.
- Project skeleton from **VRChat Creator Companion's World template** (`%LOCALAPPDATA%\VRChatCreatorCompanion\VRCTemplates\World`,
  template 1.5.0): `Packages/manifest.json`, `ProjectSettings/` (productName "Liminal Doors", a new productGUID), `.gitignore`.
- VRChat SDK 3.10.5 (`com.vrchat.base`, `com.vrchat.worlds`) unpacked from VCC's own package cache into `Packages/`.
  It isn't committed (the template's `Packages/.gitignore`). `vpm-manifest.json` is locked to 3.10.5.
- From Infinite Pole: only the booth's six meshes (body, sign, glass, payphone chrome, face, lights). They are
  renamed under `Assets/LiminalDoors/Booth/Meshes/` and keep their original GUIDs. No materials, shaders, scripts or
  settings came with them; the old hidden payphone mesh was left behind.
- Stop 01 (`Assets/LiminalDoors/Editor/LiminalStop01.cs`): one world scene `Assets/LiminalDoors/Scenes/Liminal Doors.unity`.
  - Root `Stop 01 - Fox - Pink Booth`, holding the booth with plain Standard materials (`Assets/LiminalDoors/Booth/Materials/`), a 1.3 m booth floor collider and an empty fox hiding spot.
  - One sun, a plain procedural sky and soft ambient light.
  - The SDK's VRCWorld, with the spawn on the booth floor facing out of the door (respawn height -60).
- Tools kept from the earlier session because they are agent tooling, not world content:
  - `LiminalAgentQueue.cs`, so agents drive the open editor through `Review/agent-queue`;
  - `LiminalDesktopTest.cs`, the SDK Build & Test.

## Not done / open

1. **Booth changes:** Astra will describe them. The plain materials are placeholders, and Build keeps existing materials.
2. **Swap into the PC folder:** the fresh project was built in a short temp worktree (`%LOCALAPPDATA%\Temp\claude\ld-fresh`),
   because the SDK's deep paths don't fit in the session scratchpad with Windows long paths off.
   - `Documents\Astra's PetOS\liminal-doors` is still the old copy. The parent session is mid-work there on doors and stop 02 (`claude/doors-and-stop-02-20261018`), and I asked it to commit first.
   - Astra approved closing the Liminal Doors editor and switching that folder to this branch.
3. **Not in the fresh base yet:** clouds and sky for stop 01, the glass fox, doors, the phone. The Infinite Pole voice-link scripts were dropped with the rest; the phone gets designed when more stops have booths.
4. Not tested yet on the fresh base: desktop test, headset and upload.
