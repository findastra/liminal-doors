# Handoff: booth fixes parked until Unity work resumes (Claude, 2026-10-19)

status: **done 2026-10-19 02:40.** Astra asked to finish it despite the hold: "Finish the booth fixes now even though
Unity is on hold: compile the Handmade PBR shader, re-import the booth, render it for me and run the desktop test."
The parked branch `claude/booth-panel-and-shader-20261019` is now in `claude/fresh-start-20261018`. The rest of
Unity (rename, spawn, portals, grass) stays on hold per `docs/handoff-claude-20261019-concepts-first.md`.

Astra, 2026-10-19, on the handmade booth (`137b26a`): "the thing the phone is sitting on inside looks weird. not
sure what it is supposed to be. the colors all look flat, there is no texture. I think we are missing shaders."

While I worked on it she told another session: "save the unity changes for later, we are gonna make sure these
concepts all look good before editing the unity world any longer" (`docs/handoff-claude-20261019-concepts-first.md`).
That newer instruction wins, so this work stops here and waits on its own branch.

## What is on the branch

- **The phone's backboard:** the plum board and shelf are gone. The wide middle column of the back wall is now solid
  and painted like the booth, with a raised field inside that the payphone is fixed to and a fielded panel outside.
  The back glazing is split around it (`Tools/Blender/make_phone_booth.py`, `glazed_panel(phone_panel=True)`).
- **Our own shader, `Unity Stable/Handmade PBR`** (`Assets/ShadersShop/UnityStable/HandmadePBR.shader`), which
  answers "flat, no texture, missing shaders". The booth was on Unity's stock Standard shader. The new one adds:
  - a clear coat: a second sharp reflection over the colour, so the enamel mirrors the sunset;
  - world-space triplanar detail from the CC0 scans: orange peel and scratches on the paint, brushing on the steel,
    sharper than the 2048 atlas;
  - specular occlusion from the baked AO, an edge sheen on worn edges, and per-material saturation and contrast.
- **The bake:** stronger wear (more chips, darker grime in corners, stronger streaks and colour variation). The
  metallic-smoothness map now carries metallic (R), the clear-coat mask (G), worn edges (B) and smoothness (A).
- **`LiminalBooth.cs`:** Apply look uses the new shader. New tiling scans `Booth/Textures/detail-{paint,steel}-*.png`
  (CC0 ambientCG PaintedMetal002 and Metal009, 1024 px).

## Not done (do these when Unity work starts again)

1. In Unity, compile the shader: `Unity Unicorn/Shaders Shop/Report shader errors` should list no errors for
   `Unity Stable/Handmade PBR`. It has never been compiled.
2. Re-run the bake on this branch (`Review/booth-blender` is not in git; command in the script header, with
   `cygpath -w` paths). Then run `LiminalDoors/Booth/Import Blender model` and `Build prefab`, so the meshes match the
   new textures. The committed `Booth/Model/*.asset` meshes are still the 137b26a ones, and the new textures are laid
   out for the new meshes.
3. Render (`Stop 01/Render`, `Booth/Render close-ups`), compare with `docs/renders/booth/`, show Astra, run the
   desktop test, then open a pull request into `claude/fresh-start-20261018`.
4. Lighting still to judge: the sun sits low behind the booth, so its front is lit only by flat pink ambient light.
   Post colour grading (contrast, tonemapping) would belong to the stop's glow volume (`LiminalWorldGlow.cs`).

## Done (02:40)

- Shader: "178 shaders checked, 0 with errors" (Unity Stable/Handmade PBR included).
- Re-imported (25,889 triangles), prefab rebuilt, asset creator steps 1-10 pass (notes refreshed).
- **Trap:** after a Unity restart, the stop's realtime reflection probe is black in the editor until rendered, so metal
  and clear coat render black. `LiminalDoors/Booth/Refresh reflections` renders it; run it before `Stop 01/Render` and
  `Booth/Render close-ups`. VRChat renders it on load.
- Renders: `docs/renders/stop-01-*.png`, `docs/renders/booth/booth-*.png`.
- Desktop test 02:36: this bundle in `--url=`, "Finished entering world." at 02:36:15, no exceptions.
- Shaders shop: the catalog's Unity Stable entry lists Handmade PBR; `docs/shaders-in-world.json` rescanned.
- Still open: Astra's review (step 11), then the exports. The lighting (sun behind the booth, flat pink ambient) and
  colour grading stay as they are; they're scene-wide, and the scene is on hold.
