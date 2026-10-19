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

## 2026-10-19: the booth and payphone, handmade in Blender

Astra: "this booth still looks like rudimentary shapes, it is no where near ready. I want to use those skills we
researched to fix the phone and the booth to look real and handcrafted, not computer generated."

- **No skill to install:** neither the plugin nor the skill catalog has a Blender or 3D-modelling skill. "The skills"
  became prop-artist craft, written into the unity-asset-creator checklist as its new **Craft** section (v1.1.0 of
  `Tools/unity-asset-creator/unity-asset-creator-checklist-20261018.md`, with sources).
- **Downloads (Astra's OK in chat):** four CC0 ambientCG sets, 2K JPG, 81 MB: PaintedMetal002, Metal009, Smear002,
  Scratches001. They stay outside git (scratchpad); only baked textures and `glass-smudge.png` are in the project.
  The .blend files inside the zips were never opened.
- **Model:** `Tools/Blender/make_phone_booth.py` (Blender 4.5.9, headless). It builds a K6 kiosk at the old booth's
  1.3 m footprint (2.76 m tall) with:
  - bevelled corner posts with recessed faces, narrow-wide-narrow glazing in eight rows, fielded kick panels;
  - lit TELEPHONE signs on four sides, gilded crowns in arched pediments, a dome with a rolled eave;
  - the door hinged on the -X post and open 80 degrees, with a brass pull and hinges;
  - inside, a back board and shelf with a payphone: a die-cast housing, a chrome keypad with numbered buttons, an
    instruction card, coin slots, the coin return, a hook switch, a handset with ear and mouth cups, and a coiled cord.
  - 25,907 triangles in total.
- **Bake (Cycles on the RX 9070 XT, about 2 minutes):** AO, an edge mask, and composed albedo, roughness, metallic,
  emission and normal maps. Sparse chips on edges, grime in corners, rain streaks, sun fade, and scans for roughness,
  scratches and fingerprints. Booth atlas 2048, payphone atlas 1024.
- **Unity:**
  - `LiminalDoors/Booth/Import Blender model` reads `Review/booth-blender/phone-booth.json` into `Booth/Model/*.asset`;
  - Apply look sets up Standard PBR (Booth, Payphone) and Pink Glass, which now has optional fingerprints (`_Smudge`,
    off by default);
  - Build prefab adds wall and door colliders as well as the floor;
  - Render close-ups writes `Review/booth-closeups/`.
  - The Candy Paint booth materials were removed. The six Infinite Pole meshes stay only for LiminalStop01's fallback.
- **Checks:** the asset creator passes steps 1-10 (notes refreshed). Desktop test at 01:24: this bundle in `--url=`,
  "Finished entering world." at 01:24:40, no exceptions.
- **Renders:** `docs/renders/stop-01-*.png`, `docs/renders/booth/booth-*.png` (close-ups) and
  `docs/renders/assets/liminal-doors-phone-booth-v1.0.0-*.png`.
- **Trap found:** Git Bash passes `/c/...` paths that contain spaces or apostrophes to native programs unconverted, so
  Blender wrote one run into a stray `C:\c\` folder. Pass `cygpath -w` paths. Other sessions hit the same trap: `C:\c\` also
  holds stray proxy-lake, token-canopy and open-star-test copies that aren't mine, so I left them.

### Open (2026-10-19)
1. Astra's review of the handmade booth (step 11), then the exports in the list above.
2. Old items 2-4 above still stand.

## 2026-10-19 08:10: booth and sky exported, library created

- Astra: "Yes, the booth looks ready. Mark my review on the booth and the sky, export both v1.0.0, create the public
  unity-assets-library repo and open the export pull request."
- Step 11 is marked "by Astra in chat" on both recipes. `Export every ready recipe` wrote
  `Exports/unity-assets-library/<id>/1.0.0/`: the booth is a 30.1 MB package, the sky 277 KB. Both are
  self-contained and need no shaders-shop shaders.
- New public repo **findastra/unity-assets-library** with a README, `library.json` (`unity-unicorn.library/v1`) and
  `.gitattributes` on `main`. The export pull request is findastra/unity-assets-library#1, and Astra merges it.
- New tool `Tools/unity-asset-creator/publish-asset-20261018.py`. It checks each export (checksums, all steps done,
  licence can be public, under 95 MB, no machine paths), copies it into `assets/<id>/<version>/`, and updates
  `library.json` and the README table. Then it commits on `export/...` and opens the PR.
  - `gh` started from Python couldn't see the shell's GitHub login, so the tool prints the `gh pr create` command
    instead. I opened #1 from the shell.
- Open item 1 above is done apart from Astra's merge. Still open: the library's licence (none chosen yet; its
  README asks people to ask first), and publishing unity-asset-creator, unity-assets-library and shaders-shop on Unity
  Unicorn (Unity Stable) with tool boxes on its pet program page.
