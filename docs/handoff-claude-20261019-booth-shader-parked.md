# Handoff: booth fixes parked until Unity work resumes (Claude, 2026-10-19)

status: parked on branch `claude/booth-panel-and-shader-20261019`, not merged and not imported in Unity.

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
