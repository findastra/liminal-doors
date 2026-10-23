# Handoff: the glass animals (Claude, 2026-10-23)

status: **IN PROGRESS, waiting on Astra's five answers.** Branch `claude/glass-animals-20261023` (no PR yet). Owner of
these files: this chat, until it says otherwise. Reviewer: unassigned.

Astra, 2026-10-23: apply the Rainbow Maker's cut-glass glitter to Liminal Doors' glass animals and tool; "we will
also need real life scans of animals to do those crystal renders, sort of like the marble carving tool but different,
possibly easier."

## Where things stand

- **No glass animal exists yet.** Every Doorspace's README row still says "the glass <animal> to come"; each has a
  `<Animal> hiding spot (glass <animal>: later pass)` group from the Doorspace kit.
- **The tool is glass-carver**, a Unity Unicorn world tool next to marble-carver: unity-unicorn branch
  `claude/glass-carver-20261023` (`scripts/glass-carver/glass-carver-20261023.py`, guide
  `docs/glass-carver-20261023.md`, catalog `tools/glass-carver-20261023.json`). A real animal scan goes in; it is made
  one solid, re-cut into flat facets (or polished smooth), given the Rainbow Maker's three-colour glass, exported as
  FBX with a source-and-licence record. Pipeline test on a stand-in shape: 9 s, check render shows facets, flashes and
  colour fire. Never an animal built from shapes (marble-carver's rule).
- **Here:** `Assets/LiminalDoors/Shaders/Crystal.shader` (the Rainbow Maker's crystal shader as `LiminalDoors/Crystal`)
  and `Assets/LiminalDoors/Editor/LiminalGlassAnimals.cs` (*LiminalDoors > Glass animals > Place all* puts each
  `GlassAnimals/<animal>-<date>.fbx` at its hiding spot with the crystal material and a one-turn-a-minute Animator;
  *Render checks* writes `Review/glass-animals/`). Batchmode dry run with a stand-in FBX named like a fox: see the
  log `Logs/glass-animals-test-20261023.log`; the stand-in is deleted again, nothing of it is committed.
- **Nothing is downloaded or placed in the world yet.** Five questions decide the defaults. The deck:
  https://claude.ai/artifact/2gGuEkMFTdY3WMCCd3DhpQ (private to Astra).

## The five questions (recommended answer first)

| # | Question | Recommended |
|---|---|---|
| 1 | Where do the real animal scans come from? | Museum and public scans (Smithsonian, Sketchfab CC0 / CC BY, Poly Haven), Astra OKs each download |
| 2 | Cut crystal or smooth glass? | Cut crystal |
| 3 | How big is a glass animal? | A little ornament, about 22 cm |
| 4 | Which animal is cut first? | The fox, Doorspace 01 |
| 5 | Clear glass, or a colour? | Clear, with rainbow fire |

Answers go here and in the glass-carver guide.

## Who acts next

| # | What | Who |
|---|---|---|
| 1 | Answer the five questions (the deck above) | Astra |
| 2 | Find the first scan, carve it, place it, render the check | any agent, after 1 |
| 3 | Review unity-unicorn's glass-carver PR and this branch's PR | reviewer unassigned; merges are Astra's |
