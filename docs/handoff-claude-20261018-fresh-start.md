# Handoff: Claude, 2026-10-18 (fresh start)

status: open (booth and stop 01 sky ready for Astra's review; export and publishing next)
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

## Later the same day: tools, shaders, the booth and stop 01's world

Astra asked for the booth to match its world, with sparkles swirling around it, built through a new
**unity-asset-creator** trick and tool. Exports go to a GitHub database, **unity-assets-library**. When the booth still
looked dull she also asked for a **shaders-shop** module. All three are Unity Stable (Unity Unicorn) tools, built here
for now; they get published to findastra/unity-unicorn once they're ready.

- **unity-asset-creator** (`Assets/UnityAssetCreator`, `Tools/unity-asset-creator`):
  - a researched 12-step checklist (sources linked) with automatic checks;
  - an ordered checklist window, plus queue-runnable menus and a confirmations file for agents;
  - previews in the world's light;
  - export: a `.unitypackage` of the prefab and what it uses, the previews, and `manifest.json` with the ledger row,
    the finished checklist and `requires_shaders`. Our own Unity Stable shaders are packed in; third-party shop shaders
    are listed, not packed.
- **shaders-shop** (`Assets/ShadersShop`, `Tools/shaders-shop`):
  - a catalog of 12 researched shaders, refreshed from each source;
  - installs the way VCC does, after showing the exact files and size, and survives reloads;
  - per-world shader records: `docs/shaders-in-world.json`, plus the read-only scans of Infinite Pole and Piranesi
    House in `Tools/shaders-shop/worlds/`;
  - "Report shader errors". With Astra's OK it installed lilToon, ORL, Poiyomi and Mochie.
- **Our own shaders** (`Assets/ShadersShop/UnityStable`): Candy Paint, Pink Glass, Sparkle, Sunset Sky, Cloud Sea and
  Cloud Puff. lilToon and Poiyomi only switch features on through their own inspectors, so scripted materials use ours.
- **Booth** (`LiminalBooth.cs`, Booth/Phone Booth.prefab): hot pink Candy Paint, butter-yellow glow, Pink Glass, and gold
  and pink sparkles swirling up (Astra's picks, 5 materials, 360 particles).
- **Stop 01's world** (`LiminalStop01Sky.cs`, Sky/Stop 01 Sky.prefab, placed by Stop 01/Build):
  - a sunset sky dome over a 2.4 km cloud sea (parallax billows), 14 cumulus, 8 puffs around the booth's base and a
    sunset reflection probe;
  - it sets the scene's sun low and warm behind the booth, the sunset as skybox, and pink ambient light.
- **Checklists:** steps 1-10 are done for both assets (notes in their recipes). Step 11 waits for Astra's review of
  `docs/renders/stop-01-*.png` and the asset previews in `docs/renders/assets/`.
- **Desktop test** with the sky (15:30): this bundle in `--url=`, "Finished entering world." at 15:30:15, no Udon errors.

### Open
1. Astra's review (step 11), then export booth v1.0.0 and sky v1.0.0, create findastra/unity-assets-library (public,
   her choice), and open the export pull request.
2. Publish unity-asset-creator, unity-assets-library and shaders-shop on Unity Unicorn (Unity Stable), and add their
   tool boxes to its pet program page.
3. The asset creator's previews and Stop 01/Render don't show bloom in the editor. The scene has active global glow
   volumes; VRChat's reference camera has the post-processing layer.
4. The cloud sea shows stretched streaks at grazing angles in the middle distance; the glass fox; the phone.
