// Claude, 2026-10-18. Liminal Doors, stop 02: Hare / Faraway Field (floor register 2026-10-15, scene 2; concept plate 02).
// "Long grass and a distant horizon. A rose door just beyond the rise, with rising motes. Wind moves in broad bands
// across the field. A hare tucked into the grass beside a flat stone." Palette silver green / rose / lavender.
// Everything here is generated (no outside art): the field heightfield and its walkable collider, a worn path, ~120k
// grass blades and wildflowers, the flat stone, two doors and the evening sky. The glass hare is a later pass; its
// hiding spot is marked.
// Fresh base (Astra, 2026-10-18: start fresh, nothing from Infinite Pole but the booth): the sky is
// LiminalDoors/Evening Sky, the doorway light and motes LiminalDoors/Soft Glow, and the bloom is the new Liminal Glow
// profile (Unity's post-processing package; LiminalWorldGlow). All written for this project.
// The field meshes are generated once and then reused, so rebuilding the stop doesn't make a new 27 MB grass asset;
// LiminalDoors/Stop 02/Rebuild field meshes makes them again.
//
// The stop sits in its own slot, 3 km from stop 01, inside the one world scene (LiminalStop01.ScenePath). You arrive
// in front of the pale arrival door (the way back) facing the rise; the rose door beyond it is the forward door.
// Doors are only geometry and named trigger boxes here; LiminalDoorsTravel (LiminalDoors/World/Wire doors) wires them.
//
//
// 2026-10-19, Astra: "focus on the grass for the first level. make it look like the grass in the Pink Universe world.
// Also get the sparkles in the background from that world." (and before: "need to work on the grass physics and
// texture. make it taller. make it interesting."). Our own version of that look, nothing taken from the world:
// - the grass is now ~45k tufts of tall leafy fronds (two crossed cards each, 0.6-1.5 m) drawn from a frond atlas this
//   script paints, deep rose at the root to pale pink-white at the tip (LiminalDoors/Frond Grass);
// - it sways in travelling gusts, flutters per tuft, and bends away from you as you walk through (the "Grass push
//   (player)" marker gets a LiminalGrassPush from World/Wire doors);
// - the sky has a twinkling sparkle layer, big four-point glints and a faint nebula over the sunset, and sparkles drift
//   in the distance and just over the grass;
// - doorway lights are portals that fill the opening (LiminalDoorKit.Portal).
// The lone tree and the sword of the Pink Universe world (Steven Universe's) are deliberately not here.
// Later the same day, Astra: "they are lacking the liminal (weird) factor ... same with the other paths". So the path is
// now 10x longer, 300 m winding through the grass, with the rose door a small glow at its far end that you can see from
// the moment you arrive, and both doors are glitter paint with sparkling portals and glitter halos.
//
// Menu: LiminalDoors/Stop 02/Build stop            -> replaces only the Stop 02 root in the world scene
//       LiminalDoors/Stop 02/Rebuild field meshes  -> generates the ground, grass and path again, then builds
//       LiminalDoors/Stop 02/Render review shots   -> Review/stop-02-render/*.png + report.md
// After building a stop, run LiminalDoors/World/Wire doors (Build all does both).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public static class LiminalStop02
{
    public const string StopName = "Doorspace 02 · Faraway Field Hare";   // was "Stop 02 - Hare - Faraway Field" (Doorspaces, 2026-10-19)
    public static readonly Vector3 Origin = new Vector3(3000f, 0f, 0f);
    public const string SkyMaterial = "Assets/LiminalDoors/Materials/Stop 02 Sky.mat";
    const string GenDir = "Assets/LiminalDoors/Stop 02";
    const string GroundMeshPath = GenDir + "/Field ground.asset";
    const string GrassMeshPath = GenDir + "/Field fronds.asset";
    const string OldGrassMeshPath = GenDir + "/Field grass.asset";   // the 2026-10-18 single-triangle blades
    const string FrondAtlasPath = GenDir + "/Frond atlas.png";
    const string PathMaskPath = GenDir + "/Field path mask.png";
    const string MatDir = "Assets/LiminalDoors/Materials";
    const string ReviewDir = "Review/stop-02-render";
    const string BuildReport = "Review/stop-02-build/report.md";

    // layout, in stop-local metres: arrival door at the origin; the path winds 300 m through the grass to the rose door,
    // a tiny glow on the horizon when you arrive (Astra, 2026-10-19: the paths should be 10x longer, more liminal)
    const float PathLength = 300f;
    static float PathX(float z) => (5f * Mathf.Sin(z * 0.045f) + 2f * Mathf.Sin(z * 0.13f + 1f) - 2f * Mathf.Sin(1f)) * Smooth(0f, 12f, z);
    static readonly Vector2 ArrivalDoorAt = new Vector2(0f, -0.7f);
    static readonly Vector2 ArrivalAt = new Vector2(0f, 1.3f);
    static readonly Vector2 RoseDoorAt = new Vector2(PathX(PathLength + 0.7f), PathLength + 0.7f);
    static readonly Vector2 StoneAt = new Vector2(-2.4f, 7.5f);
    static readonly Vector2[] PathPts = Enumerable.Range(0, 76).Select(i => { float z = 1.6f + (PathLength - 1.6f) * i / 75f; return new Vector2(PathX(z), z); }).ToArray();
    static readonly Rect PathMaskRect = new Rect(-20f, -10f, 40f, PathLength + 30f);   // x0, z0, width, length
    static readonly Vector3 SunDir = new Vector3(0.55f, 0.05f, 1f);   // low, off to the right of the far door so its glare doesn't hide it
    static readonly Vector2 Opening = new Vector2(0.9f, 2.24f);       // inside the door frame: posts at +-0.45, sill 0.06, lintel 2.30

    // ---------------------------------------------------------------- terrain

    static float Sq(float v) { return v * v; }
    static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3 - 2 * t); }
    static float Fbm(float x, float z) { return Mathf.PerlinNoise(x + 37.1f, z + 11.7f) * 0.65f + Mathf.PerlinNoise(x * 2.03f + 5.3f, z * 2.03f + 91.2f) * 0.35f; }

    // a long, almost level valley: soft rolls underfoot, a low swell before the door, and hills far out on both sides of
    // the path and beyond its ends, so the horizon walks with you the whole way
    static float RawH(float x, float z)
    {
        float roll = (Fbm(x * 0.022f, z * 0.022f) - 0.5f) * 1.4f * Smooth(0f, 8f, Mathf.Sqrt(x * x + z * z));   // level where you arrive
        float rise = 1.6f * Mathf.Exp(-Sq((z - (PathLength - 8f)) / 14f)) * Mathf.Exp(-Sq((x - PathX(z)) / 30f));
        float pd = PathDist(x, z);
        float hills = Smooth(90f, 190f, pd) * (6f + 11f * Fbm(x * 0.011f + 3f, z * 0.011f + 7f));
        return roll + rise + hills;
    }

    static float h0 = float.NaN;
    public static float H(float x, float z) { if (float.IsNaN(h0)) h0 = RawH(0f, 0f); return RawH(x, z) - h0; }

    static float SegDist(Vector2 p, Vector2 a, Vector2 b) { var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude); return Vector2.Distance(p, a + ab * t); }
    static float PathDist(float x, float z) { var p = new Vector2(x, z); float d = float.MaxValue; for (int i = 0; i + 1 < PathPts.Length; i++) d = Mathf.Min(d, SegDist(p, PathPts[i], PathPts[i + 1])); return d; }
    static float PathWidth(float x, float z) { return 0.42f + 0.14f * Mathf.PerlinNoise(x * 0.4f + 3f, z * 0.4f); }

    static Mesh BuildGround(out int verts)
    {
        // 400 m across, and from 110 m behind the arrival door to 130 m past the rose door
        const float half = 200f, step = 1.6f, zBack = -110f, zFar = PathLength + 130f;
        int n = Mathf.RoundToInt(half * 2f / step) + 1, nz = Mathf.RoundToInt((zFar - zBack) / step) + 1;
        var v = new Vector3[n * nz];
        for (int j = 0; j < nz; j++) for (int i = 0; i < n; i++) { float x = -half + i * step, z = zBack + j * step; v[i + j * n] = new Vector3(x, H(x, z), z); }
        var t = new int[(n - 1) * (nz - 1) * 6]; int k = 0;
        for (int j = 0; j < nz - 1; j++) for (int i = 0; i < n - 1; i++)
            {
                int a = i + j * n, b = a + 1, c = a + n, d = c + 1;
                t[k++] = a; t[k++] = c; t[k++] = b; t[k++] = b; t[k++] = c; t[k++] = d;
            }
        var m = new Mesh { name = "Field ground", indexFormat = IndexFormat.UInt32 };
        m.vertices = v; m.triangles = t; m.RecalculateNormals(); m.RecalculateBounds();
        verts = v.Length;
        return m;
    }

    static Texture2D BuildPathMask()
    {
        const int NX = 256, NZ = 2048; var r = PathMaskRect;
        var tex = new Texture2D(NX, NZ, TextureFormat.RGB24, false, true);
        var px = new Color32[NX * NZ];
        for (int j = 0; j < NZ; j++) for (int i = 0; i < NX; i++)
            {
                float x = r.x + (i + 0.5f) * r.width / NX, z = r.y + (j + 0.5f) * r.height / NZ, w = PathWidth(x, z);
                byte b = (byte)Mathf.RoundToInt(255f * (1f - Smooth(w - 0.18f, w + 0.12f, PathDist(x, z))));
                px[i + j * NX] = new Color32(b, b, b, 255);
            }
        tex.SetPixels32(px); tex.Apply();
        File.WriteAllBytes(PathMaskPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(PathMaskPath, ImportAssetOptions.ForceUpdate);
        var ti = (TextureImporter)AssetImporter.GetAtPath(PathMaskPath);
        ti.sRGBTexture = false; ti.wrapMode = TextureWrapMode.Clamp; ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(PathMaskPath);
    }

    // ---------------------------------------------------------------- grass and wildflowers

    // The frond atlas: four leafy pink-grass fronds side by side (256 x 512 px each), painted here from signed
    // distances so the edges are clean at any size. RGB is leaf shading (lighter toward each leaflet's tip, a darker
    // midrib), A is coverage. The shader colours it (root to tip) and cuts it out.
    struct Frond { public float x, angle, height, bend, leaf; public int pairs; public Frond(float x, float a, float h, float b, int p, float l) { this.x = x; angle = a; height = h; bend = b; pairs = p; leaf = l; } }
    static readonly Frond[][] FrondKinds =
    {
        new[] { new Frond(128, 0, 500, 18, 7, 1.0f) },                                                     // one tall frond
        new[] { new Frond(128, 0, 470, -22, 6, 1.15f) },                                                   // one broad frond
        new[] { new Frond(118, -9, 470, -10, 5, 0.85f), new Frond(138, 11, 430, 12, 5, 0.8f) },             // a pair
        new[] { new Frond(128, 0, 360, 8, 5, 1.0f), new Frond(112, -16, 300, -14, 4, 0.8f), new Frond(144, 15, 320, 14, 4, 0.8f) },   // a low bunch
    };

    static Vector2 StemAt(Frond f, float s)
    {
        float a = f.angle * Mathf.Deg2Rad;
        return new Vector2(f.x + Mathf.Sin(a) * f.height * s + f.bend * s * s, 4f + Mathf.Cos(a) * f.height * s);
    }

    static Texture2D BuildFrondAtlas()
    {
        const int W = 256, Hh = 512, N = 4;
        var px = new Color32[W * N * Hh];
        for (int k = 0; k < N; k++)
        {
            // shapes: stem segments (capsules) and leaflets (tapered ellipses), each with a bounding box
            var caps = new List<(Vector2 a, Vector2 b, float ra, float rb)>();
            var leaves = new List<(Vector2 c, Vector2 dir, float len, float wid)>();
            foreach (var f in FrondKinds[k])
            {
                const int seg = 12;
                for (int i = 0; i < seg; i++)
                {
                    float s0 = i / (float)seg, s1 = (i + 1) / (float)seg;
                    caps.Add((StemAt(f, s0), StemAt(f, s1), Mathf.Lerp(3.6f, 1.2f, s0), Mathf.Lerp(3.6f, 1.2f, s1)));
                }
                for (int p = 0; p < f.pairs; p++)
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float s = Mathf.Lerp(0.2f, 0.9f, p / (float)Mathf.Max(1, f.pairs - 1)) + (side > 0 ? 0.035f : 0f);
                        var at = StemAt(f, s); var tan = (StemAt(f, s + 0.01f) - at).normalized;
                        float ang = side * Mathf.Lerp(52f, 34f, s) * Mathf.Deg2Rad;
                        var dir = new Vector2(tan.x * Mathf.Cos(ang) - tan.y * Mathf.Sin(ang), tan.x * Mathf.Sin(ang) + tan.y * Mathf.Cos(ang));   // the stem's tangent turned out to one side
                        float len = (36f + 74f * (1f - s)) * f.leaf, wid = len * 0.24f;
                        leaves.Add((at + dir * (len * 0.5f), dir, len * 0.5f, wid * 0.5f));
                    }
                }
                var top = StemAt(f, 1f); var tt = (top - StemAt(f, 0.97f)).normalized;
                leaves.Add((top + tt * 24f * f.leaf, tt, 28f * f.leaf, 7f * f.leaf));
            }
            for (int y = 0; y < Hh; y++)
                for (int x = 0; x < W; x++)
                {
                    var q = new Vector2(x + 0.5f, y + 0.5f);
                    float cov = 0f, shade = 0f;
                    foreach (var c in caps)
                    {
                        var ab = c.b - c.a; float t = Mathf.Clamp01(Vector2.Dot(q - c.a, ab) / ab.sqrMagnitude);
                        float d = Vector2.Distance(q, c.a + ab * t) - Mathf.Lerp(c.ra, c.rb, t);
                        float a = Mathf.Clamp01(0.5f - d);
                        if (a > cov) { cov = a; shade = 0.62f; }
                    }
                    foreach (var l in leaves)
                    {
                        var r = q - l.c;
                        if (Mathf.Abs(r.x) > l.len + 2f || Mathf.Abs(r.y) > l.len + 2f) continue;
                        float along = Vector2.Dot(r, l.dir), across = l.dir.x * r.y - l.dir.y * r.x;
                        float u = along / l.len;
                        float wEff = l.wid * (u > 0f ? 1f - 0.6f * u * u : 1f - 0.25f * u * u);   // pointed tip, rounder base
                        float e = Mathf.Sqrt(Sq(along / l.len) + Sq(across / Mathf.Max(0.5f, wEff)));
                        float d = (e - 1f) * Mathf.Min(l.len, Mathf.Max(0.5f, wEff));
                        float a = Mathf.Clamp01(0.5f - d);
                        if (a > 0f && a >= cov * 0.98f)
                        {
                            float sh = 0.74f + 0.26f * Mathf.Clamp01(u * 0.5f + 0.5f);
                            if (Mathf.Abs(across) < 1.1f && u > -0.8f) sh *= 0.86f;   // midrib
                            shade = cov > 0f ? Mathf.Lerp(shade, sh, a) : sh; cov = Mathf.Max(cov, a);
                        }
                    }
                    byte s8 = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(shade));
                    px[k * W + x + y * W * N] = new Color32(s8, s8, s8, (byte)Mathf.RoundToInt(255f * cov));
                }
        }
        var tex = new Texture2D(W * N, Hh, TextureFormat.RGBA32, false, true);
        tex.SetPixels32(px); tex.Apply();
        File.WriteAllBytes(FrondAtlasPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(FrondAtlasPath, ImportAssetOptions.ForceUpdate);
        var ti = (TextureImporter)AssetImporter.GetAtPath(FrondAtlasPath);
        ti.sRGBTexture = false; ti.alphaIsTransparency = true; ti.mipmapEnabled = true;
        ti.mipMapsPreserveCoverage = true; ti.alphaTestReferenceValue = 0.42f;
        ti.wrapMode = TextureWrapMode.Clamp; ti.filterMode = FilterMode.Trilinear; ti.anisoLevel = 4;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(FrondAtlasPath);
    }

    class Tile { public List<Vector3> v = new List<Vector3>(), root = new List<Vector3>(); public List<Color> c = new List<Color>(); public List<Vector2> uv = new List<Vector2>(); public List<int> tri = new List<int>(); }

    // Tufts: two crossed cards of one frond each, standing on the ground. Dense where you walk, thinning toward the
    // horizon, none on the path, in the doorways or under the stone; shorter at the path's edge.
    static List<Mesh> BuildFronds(out int tufts)
    {
        var rnd = new System.Random(20261019);
        Func<float> R = () => (float)rnd.NextDouble();
        var tiles = new Dictionary<Vector2Int, Tile>();
        // a band along the whole 300 m path: thick beside it, thinning out to 30 m away; the pink ground carries on beyond
        const float cell = 0.3f, near = 6f, mid = 20f, outer = 30f, inset = 0.004f;
        Color[] tints = { new Color(1f, 1f, 1f), new Color(1f, 0.92f, 0.97f), new Color(0.97f, 0.9f, 1f), new Color(0.99f, 0.85f, 0.92f), new Color(0.88f, 0.84f, 1f) };
        tufts = 0;
        for (float gz = -12f; gz < PathLength + 14f; gz += cell)
            for (float gx = -outer - 8f; gx < outer + 8f; gx += cell)
            {
                float x = gx + R() * cell, z = gz + R() * cell;
                float pd = PathDist(x, z), pw = PathWidth(x, z);
                if (pd > outer) continue;
                float perM2 = pd < near ? 7f : pd < mid ? Mathf.Lerp(6f, 0.8f, (pd - near) / (mid - near)) : Mathf.Lerp(0.8f, 0f, (pd - mid) / (outer - mid));
                if (R() > perM2 * cell * cell) continue;
                if (pd < pw + 0.12f) continue;
                if (Vector2.Distance(new Vector2(x, z), RoseDoorAt) < 0.95f || Vector2.Distance(new Vector2(x, z), ArrivalDoorAt) < 0.95f) continue;
                if (Sq((x - StoneAt.x) / 1.65f) + Sq((z - StoneAt.y) / 1.2f) < 1f) continue;

                float hgt = (0.62f + 0.55f * R()) * (0.8f + 0.5f * Fbm(x * 0.07f, z * 0.07f));
                if (pd < pw + 0.7f) hgt *= 0.6f;
                float wid = hgt * 0.5f * (0.9f + 0.3f * R());
                float pick = R(); int kind = pick < 0.35f ? 0 : pick < 0.65f ? 1 : pick < 0.85f ? 2 : 3;
                float u0 = kind * 0.25f + inset, u1 = (kind + 1) * 0.25f - inset;
                var tint = tints[Mathf.Min(tints.Length - 1, (int)(R() * tints.Length))];
                tint.a = R();   // the tuft's own sway phase
                float yaw = R() * Mathf.PI;
                float la = R() * Mathf.PI * 2f;
                var lean = new Vector3(Mathf.Cos(la), 0f, Mathf.Sin(la)) * (0.12f * hgt) + new Vector3(0.8f, 0f, 0.6f) * (0.06f * hgt);
                var p = new Vector3(x, H(x, z) - 0.03f, z);

                var key = new Vector2Int(Mathf.FloorToInt(x / 24f), Mathf.FloorToInt(z / 24f));
                if (!tiles.TryGetValue(key, out var t)) tiles[key] = t = new Tile();
                for (int card = 0; card < 2; card++)
                {
                    float a = yaw + card * Mathf.PI * 0.5f;
                    var side = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (wid * 0.5f);
                    int b0 = t.v.Count;
                    t.v.Add(p - side); t.v.Add(p + side); t.v.Add(p - side + Vector3.up * hgt + lean); t.v.Add(p + side + Vector3.up * hgt + lean);
                    t.uv.Add(new Vector2(u0, 0f)); t.uv.Add(new Vector2(u1, 0f)); t.uv.Add(new Vector2(u0, 1f)); t.uv.Add(new Vector2(u1, 1f));
                    for (int i = 0; i < 4; i++) { t.c.Add(tint); t.root.Add(p); }
                    t.tri.Add(b0); t.tri.Add(b0 + 2); t.tri.Add(b0 + 1); t.tri.Add(b0 + 1); t.tri.Add(b0 + 2); t.tri.Add(b0 + 3);
                }
                tufts++;
            }
        var meshes = new List<Mesh>();
        foreach (var kv in tiles.OrderBy(k => k.Key.y).ThenBy(k => k.Key.x))
        {
            var t = kv.Value; var m = new Mesh { name = "Field fronds " + kv.Key.x + "," + kv.Key.y, indexFormat = t.v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            m.SetVertices(t.v); m.SetColors(t.c); m.SetUVs(0, t.uv); m.SetUVs(1, t.root);
            m.SetTriangles(t.tri, 0);
            m.RecalculateBounds();
            var b = m.bounds; b.Expand(new Vector3(1.4f, 0.6f, 1.4f)); m.bounds = b;   // room for the wind and the push
            meshes.Add(m);
        }
        return meshes;
    }

    // ---------------------------------------------------------------- build

    [MenuItem("LiminalDoors/Stop 02/Build stop")]
    public static void Build()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != LiminalStop01.ScenePath) scene = EditorSceneManager.OpenScene(LiminalStop01.ScenePath, OpenSceneMode.Single);
        var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == StopName);
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
        Directory.CreateDirectory(GenDir); Directory.CreateDirectory(MatDir);
        var report = new List<string> { "# Stop 02 build", "Built " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " into `" + scene.path + "`", "" };

        // materials
        var sky = Mat(SkyMaterial, "LiminalDoors/Evening Sky");
        // a dusk that turns to a sparkling violet overhead (2026-10-19), still warm at the sunset
        sky.SetColor("_Top", new Color(0.22f, 0.12f, 0.40f)); sky.SetColor("_Middle", new Color(0.60f, 0.34f, 0.78f));
        sky.SetColor("_Horizon", new Color(1f, 0.70f, 0.66f)); sky.SetColor("_Ground", new Color(0.62f, 0.42f, 0.66f));
        sky.SetColor("_Streaks", new Color(1f, 0.84f, 0.94f)); sky.SetFloat("_StreakAmount", 0.35f);
        sky.SetColor("_SunColor", new Color(1f, 0.84f, 0.70f)); sky.SetVector("_SunDir", new Vector4(SunDir.x, SunDir.y, SunDir.z, 0));
        sky.SetColor("_StarColor", new Color(1.8f, 1.45f, 2.0f)); sky.SetFloat("_StarAmount", 1f); sky.SetFloat("_GlintAmount", 0.85f);
        sky.SetColor("_Nebula", new Color(0.66f, 0.30f, 0.74f)); sky.SetFloat("_NebulaAmount", 0.5f);
        bool reuse = File.Exists(GroundMeshPath) && File.Exists(GrassMeshPath) && File.Exists(PathMaskPath) && File.Exists(FrondAtlasPath);
        var mask = reuse ? AssetDatabase.LoadAssetAtPath<Texture2D>(PathMaskPath) : BuildPathMask();
        var atlas = reuse ? AssetDatabase.LoadAssetAtPath<Texture2D>(FrondAtlasPath) : BuildFrondAtlas();
        var haze = new Color(0.92f, 0.62f, 0.84f);
        var ground = Mat(MatDir + "/Stop 02 Field Ground.mat", "LiminalDoors/Field Ground");
        ground.SetTexture("_PathMask", mask); ground.SetVector("_PathRect", new Vector4(PathMaskRect.x, PathMaskRect.y, PathMaskRect.width, PathMaskRect.height));
        ground.SetColor("_Grass", new Color(0.44f, 0.15f, 0.36f)); ground.SetColor("_GrassLight", new Color(0.84f, 0.44f, 0.74f));
        ground.SetColor("_Path", new Color(0.72f, 0.52f, 0.62f));
        var grass = Mat(MatDir + "/Stop 02 Frond Grass.mat", "LiminalDoors/Frond Grass");
        grass.SetTexture("_MainTex", atlas);
        foreach (var m in new[] { ground, grass }) { m.SetVector("_SunDir", new Vector4(SunDir.x, SunDir.y, SunDir.z, 0)); m.SetColor("_FadeColor", haze); }
        // glitter paint on both doors (2026-10-19: "the door more glittery"); the rose door is lit from the field side, toward you
        var roseFrame = LiminalDoorKit.GlitterMat(MatDir + "/Stop 02 Rose Door.mat", new Color(0.93f, 0.40f, 0.58f), new Color(0.32f, 0.05f, 0.14f), new Color(3.2f, 1.9f, 2.7f), new Vector3(-SunDir.x, 0.5f, -SunDir.z), 0.6f);
        var paleFrame = LiminalDoorKit.GlitterMat(MatDir + "/Stop 02 Arrival Door.mat", new Color(0.86f, 0.80f, 0.74f), new Color(0.08f, 0.06f, 0.03f), new Color(3.0f, 2.5f, 1.4f), new Vector3(SunDir.x, 0.5f, SunDir.z), 0.55f);
        var roseGlitter = Mat(MatDir + "/Stop 02 Rose Glitter.mat", "LiminalDoors/Soft Glow");
        roseGlitter.SetColor("_Color", new Color(1.8f, 1.1f, 1.6f) * 2.2f); roseGlitter.SetFloat("_Softness", 2.6f);
        var goldGlitter = Mat(MatDir + "/Stop 02 Way Back Glitter.mat", "LiminalDoors/Soft Glow");
        goldGlitter.SetColor("_Color", new Color(1.8f, 1.4f, 0.8f) * 2.0f); goldGlitter.SetFloat("_Softness", 2.6f);
        var stoneMat = Mat(MatDir + "/Stop 02 Flat Stone.mat", "LiminalDoors/Soft Lit");
        // warm grey, lit from above the sun side; a cooler, flatter stone read as a puddle in the first renders
        stoneMat.SetColor("_Color", new Color(0.82f, 0.58f, 0.74f)); stoneMat.SetColor("_Emission", new Color(0.06f, 0.02f, 0.05f));   // rosy: grey read green among the pink fronds
        stoneMat.SetVector("_LightDir", new Vector4(-0.75f, 0.4f, 0.5f, 0)); stoneMat.SetFloat("_Ambient", 0.22f);
        // doorway portals that fill the opening (0.9 x 2.24 m inside the frame), and the motes rising in front
        var roseLight = LiminalDoorKit.PortalMat(MatDir + "/Stop 02 Rose Portal.mat", new Color(1.5f, 0.62f, 0.95f), new Color(0.85f, 0.25f, 0.62f), new Color(2.4f, 1.3f, 1.9f), Opening);
        var roseMotes = Mat(MatDir + "/Stop 02 Rose Motes.mat", "LiminalDoors/Soft Glow");
        roseMotes.SetColor("_Color", new Color(1f, 0.62f, 0.78f) * 2.4f);
        var goldLight = LiminalDoorKit.PortalMat(MatDir + "/Stop 02 Way Back Portal.mat", new Color(1.5f, 1.05f, 0.55f), new Color(0.9f, 0.5f, 0.32f), new Color(2.3f, 1.8f, 1.1f), Opening);
        roseLight.SetFloat("_Glitter", 1f); roseLight.SetColor("_GlitterColor", new Color(3.2f, 2.4f, 3.0f));
        goldLight.SetFloat("_Glitter", 0.8f); goldLight.SetColor("_GlitterColor", new Color(3.2f, 2.6f, 1.5f));
        var goldMotes = Mat(MatDir + "/Stop 02 Way Back Motes.mat", "LiminalDoors/Soft Glow");
        goldMotes.SetColor("_Color", new Color(1f, 0.82f, 0.5f) * 1.8f);
        var skySparkle = Mat(MatDir + "/Stop 02 Sky Sparkles.mat", "LiminalDoors/Soft Glow");
        skySparkle.SetColor("_Color", new Color(1.7f, 1.3f, 2.0f) * 1.6f); skySparkle.SetFloat("_Softness", 2.4f);
        var fieldSparkle = Mat(MatDir + "/Stop 02 Field Sparkles.mat", "LiminalDoors/Soft Glow");
        fieldSparkle.SetColor("_Color", new Color(1.6f, 1.2f, 1.7f) * 1.4f); fieldSparkle.SetFloat("_Softness", 2.2f);
        foreach (var retired in new[] { MatDir + "/Stop 02 Field Grass.mat", MatDir + "/Stop 02 Rose Doorway Light.mat", MatDir + "/Stop 02 Way Back Light.mat" })
            if (File.Exists(retired)) AssetDatabase.DeleteAsset(retired);   // the 2026-10-18 blades and the oval doorway glows

        // meshes
        Mesh groundMesh; List<Mesh> grassMeshes; int groundVerts, blades;
        bool fresh = !reuse;
        if (reuse)
        {
            groundMesh = AssetDatabase.LoadAssetAtPath<Mesh>(GroundMeshPath); groundVerts = groundMesh.vertexCount;
            grassMeshes = AssetDatabase.LoadAllAssetsAtPath(GrassMeshPath).OfType<Mesh>().OrderBy(m => m.name, StringComparer.Ordinal).ToList();
            blades = grassMeshes.Sum(m => m.vertexCount) / 8;
        }
        else
        {
            groundMesh = BuildGround(out groundVerts);
            Replace(GroundMeshPath, groundMesh);
            grassMeshes = BuildFronds(out blades);
            AssetDatabase.DeleteAsset(OldGrassMeshPath);
            AssetDatabase.DeleteAsset(GrassMeshPath);
            AssetDatabase.CreateAsset(grassMeshes[0], GrassMeshPath);
            foreach (var m in grassMeshes.Skip(1)) AssetDatabase.AddObjectToAsset(m, GrassMeshPath);
            AssetDatabase.SaveAssets();
        }

        // scene objects
        var stop = new GameObject(StopName);
        SceneManager.MoveGameObjectToScene(stop, scene);
        stop.transform.position = Origin;

        var groundGo = new GameObject("Field ground (walkable)");
        groundGo.transform.SetParent(stop.transform, false);
        groundGo.AddComponent<MeshFilter>().sharedMesh = groundMesh;
        groundGo.AddComponent<MeshRenderer>().sharedMaterial = ground;
        groundGo.AddComponent<MeshCollider>().sharedMesh = groundMesh;

        var grassRoot = new GameObject("Tall pink fronds");
        grassRoot.transform.SetParent(stop.transform, false);
        foreach (var m in grassMeshes)
        {
            var g = new GameObject(m.name); g.transform.SetParent(grassRoot.transform, false);
            g.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = grass; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
        }

        var stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        UnityEngine.Object.DestroyImmediate(stone.GetComponent<Collider>());
        stone.name = "Flat stone"; stone.transform.SetParent(stop.transform, false);
        stone.transform.localPosition = new Vector3(StoneAt.x, H(StoneAt.x, StoneAt.y) + 0.04f, StoneAt.y);
        stone.transform.localRotation = Quaternion.Euler(9f, 20f, 6f); stone.transform.localScale = new Vector3(1.45f, 0.62f, 1.05f);
        stone.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;

        var hare = new GameObject("Hare hiding spot (glass hare: later pass)");
        hare.transform.SetParent(stop.transform, false);
        hare.transform.localPosition = new Vector3(StoneAt.x + 1.0f, H(StoneAt.x + 1.0f, StoneAt.y + 0.35f) + 0.05f, StoneAt.y + 0.35f);

        // the grass parts for you: World/Wire doors puts a LiminalGrassPush (U#) on this marker
        var push = new GameObject("Grass push (player)"); push.transform.SetParent(stop.transform, false);

        // sparkles: a slow drift of them far out over the field, against the sky, and a fine glitter just over the grass
        var skyS = new GameObject("Sky sparkles"); skyS.transform.SetParent(stop.transform, false);
        skyS.transform.localPosition = new Vector3(0f, 45f, PathLength * 0.5f);
        var sps = LiminalDoorKit.Motes(skyS, skySparkle, 120f, new Vector3(420f, 70f, PathLength + 180f), 0.05f, 0.9f, 900);
        var sm = sps.main; sm.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6.5f);
        var ssz = sps.sizeOverLifetime; ssz.enabled = true;
        ssz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.15f, 1f), new Keyframe(0.3f, 0.35f), new Keyframe(0.5f, 1f), new Keyframe(0.7f, 0.4f), new Keyframe(1f, 0f)));
        var fieldS = new GameObject("Field sparkles"); fieldS.transform.SetParent(stop.transform, false);
        fieldS.transform.localPosition = new Vector3(0f, H(0f, PathLength * 0.5f) + 1.3f, PathLength * 0.5f);
        var fps = LiminalDoorKit.Motes(fieldS, fieldSparkle, 80f, new Vector3(34f, 1.6f, PathLength + 10f), 0.04f, 0.045f, 800);
        var fsz = fps.sizeOverLifetime; fsz.enabled = true;
        fsz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.25f, 1f), new Keyframe(0.5f, 0.3f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0f)));

        // doors: you face the rise when you arrive; the rose door's front looks back down the path at you
        float roseYaw = Mathf.Atan2(RoseDoorAt.x - ArrivalAt.x, RoseDoorAt.y - ArrivalAt.y) * Mathf.Rad2Deg;
        var rose = BuildDoor(stop.transform, "Rose door (forward)", Ground(RoseDoorAt), roseYaw, roseFrame, roseFrame, roseLight, roseMotes, 26, "Forward door trigger");
        var pale = BuildDoor(stop.transform, "Arrival door (the way back)", Ground(ArrivalDoorAt), 180f, paleFrame, paleFrame, goldLight, goldMotes, 10, "Arrival door trigger");
        LiminalDoorKit.GlitterHalo(rose.transform, roseGlitter, new Vector3(0f, 1.4f, -0.7f), new Vector3(3.0f, 3.0f, 1.6f), 90f, 700);
        // a faint shaft of pink light standing over the far door, so from the arrival door it is a tiny beacon 300 m off
        var beaconMat = Mat(MatDir + "/Stop 02 Rose Beacon.mat", "LiminalDoors/Soft Glow");
        beaconMat.SetColor("_Color", new Color(1.3f, 0.5f, 0.95f) * 0.55f); beaconMat.SetFloat("_Softness", 1.4f);
        LiminalDoorKit.Quad(rose.transform, "Beacon", new Vector3(0f, 16f, 0.4f), Quaternion.identity, new Vector2(5f, 36f), beaconMat);
        LiminalDoorKit.GlitterHalo(pale.transform, goldGlitter, new Vector3(0f, 1.3f, -0.6f), new Vector3(2.2f, 2.6f, 1.2f), 45f, 400);

        var arrival = new GameObject("Arrival point"); arrival.transform.SetParent(stop.transform, false);
        arrival.transform.localPosition = Ground(ArrivalAt); arrival.transform.localRotation = Quaternion.Euler(0f, roseYaw, 0f);
        var ret = new GameObject("Return point"); ret.transform.SetParent(stop.transform, false);
        var retAt = rose.transform.localPosition + rose.transform.localRotation * new Vector3(0f, 0f, -1.6f);
        ret.transform.localPosition = new Vector3(retAt.x, H(retAt.x, retAt.z), retAt.z); ret.transform.localRotation = Quaternion.Euler(0f, roseYaw + 180f, 0f);

        LiminalWorldGlow.AddStopVolume(stop.transform);   // the new Liminal Glow profile, only while you're here

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        LiminalWorldGlow.Setup();   // the reference camera that carries the bloom into VRChat

        float pathLen = 0f; for (int i = 0; i + 1 < PathPts.Length; i++) pathLen += Vector2.Distance(PathPts[i], PathPts[i + 1]);
        float crest = PathPts.Max(p => H(p.x, p.y));
        report.Add("- Slot: " + Origin + " (3 km from stop 01). Ground: " + groundVerts + " vertices, 400 m wide and 540 m long, walkable MeshCollider on the visible ground.");
        report.Add("- Path: " + pathLen.ToString("0") + " m, winding, from the arrival door to the rose door (2026-10-19: 10x longer).");
        report.Add("- Grass: " + blades + " tall pink frond tufts (two crossed cards each, " + grassMeshes.Sum(m => m.vertexCount) + " vertices) in " + grassMeshes.Count + " tiles" + (fresh ? ", generated now" : ", reused from the last generation") + "; none on the path, the doorways or under the stone. Atlas `" + FrondAtlasPath + "`.");
        report.Add("- Sparkles: sky layer on in `" + SkyMaterial + "`; particles: Sky sparkles (max 900), Field sparkles (max 800), a glitter halo at each door. Doors in Glitter Paint, portals with glitter. Grass push marker for LiminalGrassPush.");
        report.Add("- Highest point on the path " + crest.ToString("0.00") + " m above the arrival spot; rose door base at " + H(RoseDoorAt.x, RoseDoorAt.y).ToString("0.00") + " m, " + Vector2.Distance(ArrivalAt, RoseDoorAt).ToString("0.0") + " m from where you arrive.");
        report.Add("- Arrival point " + arrival.transform.position.ToString("F2") + " facing the rose door; return point " + ret.transform.position.ToString("F2") + " in front of the rose door, facing back down the path.");
        report.Add("- Sky: `" + SkyMaterial + "` (LiminalDoors/Evening Sky); the door system switches it on when you arrive.");
        Directory.CreateDirectory(Path.GetDirectoryName(BuildReport));
        File.WriteAllText(BuildReport, string.Join("\n", report) + "\n");
        Debug.Log("[Liminal] Stop 02 built: " + blades + " grass blades.");
    }

    static Vector3 Ground(Vector2 p) { return new Vector3(p.x, H(p.x, p.y), p.y); }

    static void Replace(string path, Mesh mesh)
    {
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
    }

    static Material Mat(string path, string shaderName)
    {
        var sh = Shader.Find(shaderName);
        if (sh == null) throw new Exception("Shader " + shaderName + " not found (check the console for shader errors).");
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh) { name = Path.GetFileNameWithoutExtension(path) }; AssetDatabase.CreateAsset(m, path); }
        else m.shader = sh;
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    // A free-standing door: two posts, a lintel, a threshold, a leaf swung open toward you, soft light in the doorway,
    // rising motes and a trigger box exactly in the doorway. The front (the side you walk in from) is local -Z.
    static GameObject BuildDoor(Transform parent, string name, Vector3 at, float yaw, Material frame, Material leaf, Material light, Material motes, int moteRate, string trigger)
    {
        var door = new GameObject(name);
        door.transform.SetParent(parent, false);
        door.transform.localPosition = at - new Vector3(0f, 0.03f, 0f);
        door.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Box(door.transform, "Post left", new Vector3(-0.5f, 1.15f, 0f), new Vector3(0.1f, 2.36f, 0.14f), frame);
        Box(door.transform, "Post right", new Vector3(0.5f, 1.15f, 0f), new Vector3(0.1f, 2.36f, 0.14f), frame);
        Box(door.transform, "Lintel", new Vector3(0f, 2.36f, 0f), new Vector3(1.12f, 0.12f, 0.16f), frame);
        Box(door.transform, "Threshold", new Vector3(0f, 0.03f, 0f), new Vector3(1.1f, 0.06f, 0.26f), frame);
        var hinge = new GameObject("Hinge"); hinge.transform.SetParent(door.transform, false);
        hinge.transform.localPosition = new Vector3(-0.45f, 0f, 0f); hinge.transform.localRotation = Quaternion.Euler(0f, 72f, 0f);
        Box(hinge.transform, "Door leaf", new Vector3(0.45f, 1.12f, 0f), new Vector3(0.88f, 2.16f, 0.05f), leaf);
        Box(hinge.transform, "Knob", new Vector3(0.78f, 1.05f, -0.045f), new Vector3(0.05f, 0.05f, 0.05f), frame);

        LiminalDoorKit.Portal(door.transform, 0.06f, Opening, light);   // fills the opening between the posts, sill to lintel

        var m = new GameObject("Rising motes"); m.transform.SetParent(door.transform, false);
        m.transform.localPosition = new Vector3(0f, 0.9f, -0.15f);
        Motes(m, motes, moteRate);

        var t = new GameObject(trigger); t.transform.SetParent(door.transform, false);
        t.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        var bc = t.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(0.84f, 2.05f, 0.36f);
        return door;
    }

    static void Box(Transform parent, string name, Vector3 at, Vector3 size, Material mat)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        UnityEngine.Object.DestroyImmediate(b.GetComponent<Collider>());
        b.name = name; b.transform.SetParent(parent, false);
        b.transform.localPosition = at; b.transform.localScale = size;
        b.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static void Motes(GameObject go, Material mat, int rate)
    {
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true; main.prewarm = true; main.duration = 8f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.05f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, 1f, 1f, 0.7f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        var emission = ps.emission; emission.rateOverTime = rate;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(1.1f, 1.8f, 0.6f);
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f); vel.y = new ParticleSystem.MinMaxCurve(0.12f, 0.3f); vel.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.08f; noise.frequency = 0.4f; noise.scrollSpeed = 0.1f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.7f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat; r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
    }

    [MenuItem("LiminalDoors/Stop 02/Rebuild field meshes")]
    public static void RebuildMeshes()
    {
        foreach (var p in new[] { GroundMeshPath, GrassMeshPath, PathMaskPath }) AssetDatabase.DeleteAsset(p);
        Build();
    }

    // ---------------------------------------------------------------- review renders

    static void RenderTo(Camera cam, int w, int h, string file)
    {
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 1 };
        var prev = RenderTexture.active;
        try
        {
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            File.WriteAllBytes(file, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        finally { cam.targetTexture = null; RenderTexture.active = prev; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
    }

    [MenuItem("LiminalDoors/Stop 02/Render review shots")]
    public static void RenderShots()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != LiminalStop01.ScenePath) scene = EditorSceneManager.OpenScene(LiminalStop01.ScenePath, OpenSceneMode.Single);
        var stop = scene.GetRootGameObjects().FirstOrDefault(g => g.name == StopName);
        if (stop == null) throw new Exception("No " + StopName + "; run LiminalDoors/Stop 02/Build stop first.");
        var rose = stop.transform.Find("Rose door (forward)");
        var pale = stop.transform.Find("Arrival door (the way back)");
        var arrival = stop.transform.Find("Arrival point");

        // look the way a visitor sees it: only this stop on, with its own sky
        var others = scene.GetRootGameObjects().Where(g => LiminalDoorspaces.IsDoorspace(g) && g != stop && g.activeSelf).ToList();
        var oldSky = RenderSettings.skybox;
        Directory.CreateDirectory(ReviewDir);
        var camGo = new GameObject("_review camera") { hideFlags = HideFlags.DontSave };
        var report = new StringBuilder();
        report.AppendLine("# Liminal Doors stop 02 review renders");
        report.AppendLine("Rendered " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " in Unity " + Application.unityVersion + " (" + SystemInfo.graphicsDeviceType + ", " + PlayerSettings.colorSpace + " colour), other stops hidden, stop 02 sky on.");
        report.AppendLine();
        try
        {
            foreach (var o in others) o.SetActive(false);
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterial);
            foreach (var ps in stop.GetComponentsInChildren<ParticleSystem>()) ps.Simulate(8f, false, true, true);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox; cam.nearClipPlane = 0.05f; cam.farClipPlane = 2500f; cam.allowHDR = true; cam.allowMSAA = false;
            LiminalWorldGlow.Configure(camGo.AddComponent<PostProcessLayer>(), camGo.transform);
            Vector3 eye = arrival.position + Vector3.up * 1.6f, roseC = rose.TransformPoint(0f, 1.2f, 0f), paleC = pale.TransformPoint(0f, 1.2f, 0f);
            Vector3 L(float x, float y, float z) => stop.transform.TransformPoint(x, H(x, z) + y, z);
            var shots = new (string name, Vector3 pos, Vector3 look, float fov)[]
            {
                ("01-arrival-view", eye, roseC + Vector3.up * 0.3f, 55f),
                ("02-rose-door", rose.TransformPoint(0.9f, 1.45f, -3.4f), roseC, 50f),
                ("03-wide-15m", L(-13f, 6f, -2f), L(3f, 1f, 17f), 55f),
                ("04-high-along-the-path", L(-26f, 55f, -30f), L(0f, 0f, 160f), 62f),
                ("05-way-back-door-and-stone", L(1.2f, 1.6f, 11f), paleC - Vector3.up * 0.4f, 58f),
                ("06-from-the-rose-door-looking-back", rose.TransformPoint(0.7f, 1.7f, -2.2f), eye, 60f),
                ("09-halfway", L(PathX(150f), 1.65f, 150f), L(PathX(185f), 1.2f, 185f), 60f),
                ("07-walking-through", L(-4.4f, 1.75f, 10.2f), L(-3.6f, 0.2f, 13.2f), 62f),   // the grass parts around someone standing at (-3.6, 13.2)
                ("08-sparkle-sky", L(3f, 1.6f, 6f), L(-20f, 40f, 80f), 70f),
            };
            foreach (var s in shots)
            {
                var feet = L(-3.6f, 0f, 13.2f);
                Shader.SetGlobalVector("_UdonLiminalPlayer", s.name.StartsWith("07") ? new Vector4(feet.x, feet.y, feet.z, 1f) : Vector4.zero);
                camGo.transform.position = s.pos;
                camGo.transform.rotation = Quaternion.LookRotation(s.look - s.pos, Vector3.up);
                cam.fieldOfView = s.fov;
                var file = Path.Combine(ReviewDir, "stop-02-" + s.name + ".png");
                RenderTo(cam, 1920, 1080, file);
                report.AppendLine("- `" + file.Replace('\\', '/') + "`: camera " + s.pos.ToString("F1") + " looking at " + s.look.ToString("F1") + ", fov " + s.fov);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(camGo);
            foreach (var o in others) o.SetActive(true);
            RenderSettings.skybox = oldSky;
            Shader.SetGlobalVector("_UdonLiminalPlayer", Vector4.zero);
        }
        var rs = stop.GetComponentsInChildren<Renderer>(true);
        report.AppendLine();
        report.AppendLine("Renderers under the stop: " + rs.Length + "; materials: " + string.Join(", ", rs.SelectMany(r => r.sharedMaterials).Distinct().Select(m => m == null ? "MISSING" : m.name + " / " + m.shader.name)));
        File.WriteAllText(Path.Combine(ReviewDir, "report.md"), report.ToString());
        Debug.Log("[Liminal] Stop 02 renders written to " + ReviewDir);
    }
}
