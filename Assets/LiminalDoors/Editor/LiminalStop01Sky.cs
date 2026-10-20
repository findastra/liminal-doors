// Claude, 2026-10-18. Stop 01's world, made with Astra through unity-asset-creator: a sunset sky over a sea of pink
// clouds (floor register, scene 1: "alone above a sea of pink clouds"; palette hot pink / apricot / butter yellow).
// Astra: "Build stop 1's sunset sky and sea of pink clouds ... as our own Unity Stable shaders made with the asset
// creator, so the booth sits in its world." Everything here is new and our own: the shaders (Unity Stable/Sunset Sky,
// Cloud Sea, Cloud Puff), the cloud-shape texture and the cumulus meshes, which this script draws and builds.
//
// One prefab, Assets/LiminalDoors/Sky/Stop 01 Sky.prefab, under the stop's root (so it switches with the stop):
//   a sky dome, the cloud sea (2.4 km, parallax billows), distant cumulus, small puffs tucking in the booth's base,
//   and a reflection probe so the booth's paint and glass reflect the sunset.
// Placing it also sets three scene-wide things for the sunset (stop 02's own shaders don't use them): the root "Sun"
// goes low and warm behind the booth, the skybox becomes the sunset (the default reflections), ambient light turns pink.
//
// Menu: LiminalDoors/Sky/Build stop 01 sky prefab   -> the prefab, its meshes, texture, materials and asset recipe
//       LiminalDoors/Stop 01/Add sky and clouds      -> places it in stop 01 (Stop 01/Build does this too)
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityUnicorn.AssetCreator;

public static class LiminalStop01Sky
{
    const string Dir = "Assets/LiminalDoors/Sky/";
    const string PrefabPath = Dir + "Stop 01 Sky.prefab";
    const string RecipePath = Dir + "liminal-doors-stop-01-sky-asset-recipe.asset";
    const string NoisePath = Dir + "Textures/cloud-shapes.png";
    const string InstanceName = "Sunset sky and cloud sea";
    const float SeaHeight = -0.05f;                                   // the highest cloud tops, just under the booth floor
    static readonly Quaternion SunRotation = Quaternion.Euler(14f, 35f, 0f);   // low, behind the booth (door faces +Z)
    static Vector3 ToSun => -(SunRotation * Vector3.forward);

    // ---------------------------------------------------------------- prefab

    [MenuItem("LiminalDoors/Sky/Build stop 01 sky prefab")]
    public static void BuildPrefab()
    {
        Directory.CreateDirectory(Dir + "Meshes"); Directory.CreateDirectory(Dir + "Materials"); Directory.CreateDirectory(Dir + "Textures");
        var noise = EnsureNoise();
        var sky = Mat("Sunset sky", "Unity Stable/Sunset Sky");
        var sea = Mat("Cloud sea", "Unity Stable/Cloud Sea"); sea.SetTexture("_Noise", noise);
        var puff = Mat("Cloud puff", "Unity Stable/Cloud Puff"); puff.SetTexture("_Noise", noise); puff.SetFloat("_SeaHeight", SeaHeight); puff.SetFloat("_SeaBlend", 2.5f); puff.SetFloat("_FluffScale", .12f);
        var near = Mat("Cloud puff (near)", "Unity Stable/Cloud Puff"); near.SetTexture("_Noise", noise); near.SetFloat("_SeaHeight", SeaHeight); near.SetFloat("_SeaBlend", .25f); near.SetFloat("_FluffScale", 1.6f);
        // the look (first render, 2026-10-18: sun too big, sea read as rippled water, clouds too lavender): small sun,
        // big soft billows with deep relief, gentle fluff, pinker shade on the sides facing away from the low sun
        sky.SetFloat("_SunSize", .0005f); sky.SetFloat("_HaloStrength", .6f);
        sea.SetFloat("_Scale", .018f); sea.SetFloat("_Depth", 4.5f); sea.SetFloat("_Steps", 28f); sea.SetFloat("_HazeStart", 60f);
        puff.SetFloat("_Fluff", .25f); puff.SetFloat("_FluffScale", .035f);
        near.SetFloat("_Fluff", .25f); near.SetFloat("_FluffScale", .5f);
        foreach (var m in new[] { sea, puff, near })
        {
            m.SetColor("_TopColor", new Color(1f, .88f, .78f)); m.SetColor("_SideColor", new Color(1f, .68f, .8f)); m.SetColor("_ShadowColor", new Color(.8f, .53f, .75f));
        }
        foreach (var m in new[] { sky, sea, puff, near }) { m.SetVector("_SunDir", ToSun); EditorUtility.SetDirty(m); }
        var cumulus = new[] { Cumulus("cumulus-a", 11, 7), Cumulus("cumulus-b", 23, 9), Cumulus("cumulus-c", 37, 6) };
        AssetDatabase.SaveAssets();

        var root = new GameObject("Stop 01 Sky");
        try
        {
            Part(root.transform, "Sunset sky dome", Resources.GetBuiltinResource<Mesh>("Sphere.fbx"), sky, Vector3.zero, Vector3.one * 1800f, false);
            Part(root.transform, "Cloud sea", Resources.GetBuiltinResource<Mesh>("Plane.fbx"), sea, new Vector3(0, SeaHeight, 0), new Vector3(240f, 1f, 240f), true);

            // distant cumulus rising out of the sea: (direction in degrees from the door's side, distance m, size m, mesh)
            var far = new (float deg, float dist, float size, int mesh)[]
            {
                (200, 38, 9, 0), (238, 64, 14, 1), (165, 85, 18, 2), (300, 120, 26, 0), (95, 140, 24, 1), (20, 170, 30, 2), (330, 60, 11, 2),
                (60, 55, 10, 0), (130, 260, 48, 0), (250, 300, 60, 2), (10, 420, 70, 1), (180, 520, 90, 0), (290, 480, 80, 1), (75, 600, 95, 2),
            };
            var farRoot = new GameObject("Cumulus"); farRoot.transform.SetParent(root.transform, false);
            for (int k = 0; k < far.Length; k++)
            {
                var c = far[k]; float a = c.deg * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Sin(a) * c.dist, SeaHeight - c.size * .12f, Mathf.Cos(a) * c.dist);
                var go = Part(farRoot.transform, "Cumulus " + (k + 1), cumulus[c.mesh], puff, pos, new Vector3(c.size * 1.3f, c.size * .8f, c.size), true);
                go.transform.localRotation = Quaternion.Euler(0, c.deg * 2.3f, 0);
            }
            // small puffs tucking in the booth's base, kept clear of the door (local +Z, which swings out to the +X side)
            var nearRoot = new GameObject("Booth cloud puffs"); nearRoot.transform.SetParent(root.transform, false);
            // outside the walls (0.73 m from the middle): low tufts about 1 m across
            var puffs = new (float x, float z, float s)[] { (-1.25f, -.3f, 1f), (-1.05f, -1.1f, 1.15f), (.1f, -1.3f, .95f), (1.1f, -1.05f, 1.1f), (1.35f, -.1f, .85f), (-1.3f, .6f, .8f), (-1.75f, -.9f, .9f), (.85f, -1.75f, .8f) };
            for (int k = 0; k < puffs.Length; k++)
                Part(nearRoot.transform, "Puff " + (k + 1), cumulus[k % 3], near, new Vector3(puffs[k].x, SeaHeight - .05f, puffs[k].z), new Vector3(puffs[k].s * .45f, puffs[k].s * .32f, puffs[k].s * .42f), true)
                    .transform.localRotation = Quaternion.Euler(0, k * 47f, 0);
            // 1.1.0, the door side too, so the booth stands in clouds all the way round (Astra, 2026-10-20: "Get rid of the
            // grass in the first floor. I just want it to be clouds instead ... just all clouds"). Lower than the others, about
            // shin high: the doorway is the forward door's trigger, so you are sent on as you step into them.
            var front = new (float x, float z, float s)[] { (-.35f, 1.05f, .9f), (.4f, 1.2f, .8f), (-.95f, .95f, .85f), (1f, .75f, .9f), (.05f, 1.65f, .75f), (-.65f, 1.8f, .7f), (.8f, 1.6f, .7f), (1.6f, .45f, .8f), (-1.65f, .15f, .9f), (1.75f, -.6f, .8f) };
            for (int k = 0; k < front.Length; k++)
                Part(nearRoot.transform, "Puff " + (puffs.Length + k + 1), cumulus[(k + 1) % 3], near, new Vector3(front[k].x, SeaHeight - .1f, front[k].z), new Vector3(front[k].s * .45f, front[k].s * .28f, front[k].s * .42f), true)
                    .transform.localRotation = Quaternion.Euler(0, 20f + k * 61f, 0);

            var probeGo = new GameObject("Sunset reflections"); probeGo.transform.SetParent(root.transform, false); probeGo.transform.localPosition = new Vector3(0, 1.4f, 0);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime; probe.refreshMode = ReflectionProbeRefreshMode.OnAwake; probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.resolution = 128; probe.size = new Vector3(24, 16, 24); probe.blendDistance = 4; probe.importance = 2; probe.hdr = true; probe.farClipPlane = 2000;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { Object.DestroyImmediate(root); }
        EnsureRecipe();
        Debug.Log("[Liminal] stop 01 sky prefab written: " + PrefabPath);
    }

    static GameObject Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Vector3 scale, bool isStatic)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; r.lightProbeUsage = LightProbeUsage.Off;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);   // never an occluder: the dome would hide everything
        return go;
    }

    static Material Mat(string name, string shader)
    {
        var path = Dir + "Materials/" + name + ".mat";
        var sh = Shader.Find(shader); if (sh == null) throw new System.Exception("Shader '" + shader + "' isn't in this world (shaders-shop)");
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh) { name = name }; AssetDatabase.CreateAsset(m, path); }
        else if (m.shader != sh) m.shader = sh;
        m.enableInstancing = true;
        return m;
    }

    // A rounded cumulus: an icosphere pushed out to the farthest of several overlapping lobes, flattened underneath.
    static Mesh Cumulus(string name, int seed, int lobes)
    {
        var path = Dir + "Meshes/" + name + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        bool isNew = mesh == null; if (isNew) mesh = new Mesh { name = name };
        Icosphere(3, out var verts, out var tris);
        var rnd = new System.Random(seed);
        float R() => (float)rnd.NextDouble();
        var centres = new List<Vector3> { new Vector3(0, .15f, 0) }; var radii = new List<float> { .72f };
        for (int k = 0; k < lobes; k++)                                  // big lobes for the shape
        {
            float a = R() * Mathf.PI * 2, up = .15f + R() * .55f;
            centres.Add(new Vector3(Mathf.Cos(a) * (.35f + R() * .35f), up, Mathf.Sin(a) * (.35f + R() * .35f)));
            radii.Add(.36f + R() * .3f);
        }
        for (int k = 0; k < lobes * 2; k++)                              // small lobes on top: the cauliflower bumps
        {
            var d = new Vector3(R() * 2 - 1, .35f + R() * .65f, R() * 2 - 1).normalized;
            centres.Add(d * (.62f + R() * .2f)); radii.Add(.16f + R() * .16f);
        }
        for (int i = 0; i < verts.Count; i++)
        {
            var d = verts[i]; float best = .25f;
            for (int k = 0; k < centres.Count; k++)
            {
                float b = Vector3.Dot(d, centres[k]), disc = b * b - centres[k].sqrMagnitude + radii[k] * radii[k];
                if (disc >= 0) best = Mathf.Max(best, b + Mathf.Sqrt(disc));
            }
            var p = d * best;
            if (p.y < 0) p.y *= .18f;                                    // a flat-ish base that sinks into the sea
            verts[i] = p;
        }
        mesh.Clear(); mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        if (isNew) AssetDatabase.CreateAsset(mesh, path); else EditorUtility.SetDirty(mesh);
        return mesh;
    }

    static void Icosphere(int subdivisions, out List<Vector3> verts, out List<int> tris)
    {
        float t = (1 + Mathf.Sqrt(5)) / 2;
        verts = new List<Vector3> { new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0), new Vector3(0, -1, t), new Vector3(0, 1, t),
                                    new Vector3(0, -1, -t), new Vector3(0, 1, -t), new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1) };
        for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;
        tris = new List<int> { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8, 3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
        var cache = new Dictionary<long, int>();
        var vs = verts;
        int Mid(int a, int b)
        {
            long key = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
            if (cache.TryGetValue(key, out int m)) return m;
            vs.Add(((vs[a] + vs[b]) * .5f).normalized); cache[key] = vs.Count - 1; return vs.Count - 1;
        }
        for (int s = 0; s < subdivisions; s++)
        {
            var next = new List<int>();
            for (int i = 0; i < tris.Count; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2], ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            tris = next;
        }
    }

    // 256 px tileable cloud shapes, drawn here: R = rounded billow domes (two sizes), G = fine detail, B = a large swell.
    static Texture2D EnsureNoise()
    {
        if (!File.Exists(NoisePath))
        {
            const int n = 256; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false, true);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                {
                    float u = (float)x / n, v = (float)y / n;
                    float domes = Domes(u, v, 5, 3) * .62f + Domes(u, v, 11, 7) * .38f;
                    float detail = Domes(u, v, 23, 13) * .6f + Value(u, v, 31, 5) * .4f;
                    float swell = Value(u, v, 3, 17) * .65f + Value(u, v, 6, 29) * .35f;
                    tex.SetPixel(x, y, new Color(domes, detail, swell, 1));
                }
            File.WriteAllBytes(NoisePath, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(NoisePath);
            var imp = (TextureImporter)AssetImporter.GetAtPath(NoisePath);
            imp.sRGBTexture = false; imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Repeat; imp.maxTextureSize = 256;
            imp.textureCompression = TextureImporterCompression.Compressed; imp.alphaSource = TextureImporterAlphaSource.None; imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(NoisePath);
    }

    // Tileable rounded domes: 1 at a cell's feature point, falling to 0 as a soft hemisphere (Worley F1 on a wrapped grid).
    static float Domes(float u, float v, int cells, int seed)
    {
        float px = u * cells, py = v * cells; int cx = Mathf.FloorToInt(px), cy = Mathf.FloorToInt(py); float best = 9;
        for (int oy = -1; oy <= 1; oy++) for (int ox = -1; ox <= 1; ox++)
            {
                int gx = cx + ox, gy = cy + oy;
                int wx = ((gx % cells) + cells) % cells, wy = ((gy % cells) + cells) % cells;
                float fx = gx + H(wx, wy, seed), fy = gy + H(wx, wy, seed + 101);
                best = Mathf.Min(best, (px - fx) * (px - fx) + (py - fy) * (py - fy));
            }
        float d = Mathf.Sqrt(best) / .78f;
        return Mathf.Sqrt(Mathf.Clamp01(1 - d * d));
    }

    static float Value(float u, float v, int cells, int seed)
    {
        float px = u * cells, py = v * cells; int x0 = Mathf.FloorToInt(px), y0 = Mathf.FloorToInt(py);
        float fx = px - x0, fy = py - y0; fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        int W(int a) => ((a % cells) + cells) % cells;
        float a00 = H(W(x0), W(y0), seed), a10 = H(W(x0 + 1), W(y0), seed), a01 = H(W(x0), W(y0 + 1), seed), a11 = H(W(x0 + 1), W(y0 + 1), seed);
        return Mathf.Lerp(Mathf.Lerp(a00, a10, fx), Mathf.Lerp(a01, a11, fx), fy);
    }

    static float H(int x, int y, int seed)
    {
        unchecked { uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647); h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16; return (h & 0xFFFFFF) / 16777216f; }
    }

    static void EnsureRecipe()
    {
        var r = AssetDatabase.LoadAssetAtPath<AssetRecipe>(RecipePath);
        if (r == null)
        {
            r = ScriptableObject.CreateInstance<AssetRecipe>();
            r.id = "liminal-doors-stop-01-sky"; r.displayName = "Liminal Doors stop 1 sunset sky and cloud sea"; r.kind = "sky"; r.version = "1.0.0";
            r.description = "Stop 1's world: a sunset sky (lavender, pink, apricot, butter-yellow horizon, a low sun) over a sea of rolling pink clouds with cumulus rising out of it, small puffs around the booth and a reflection probe for the sunset.";
            r.forPC = true; r.forAndroid = false; r.moves = false;
            r.creator = "Astra (built with Claude)"; r.source = "Made in Liminal Doors: Unity Stable shaders, generated meshes and cloud texture"; r.license = "own-work"; r.publicOk = true;
            r.maxTriangles = 30000; r.maxMaterials = 4; r.maxTextureSize = 1024; r.maxParticles = 0;
            r.changelog = "1.0.0: first version. Sunset Sky dome, Cloud Sea (parallax billows, soft self-shadow, sun glow, haze), 14 cumulus, 8 booth puffs, sunset reflection probe.";
            AssetDatabase.CreateAsset(r, RecipePath);
        }
        if (r.version == "1.0.0")   // 2026-10-20: ten more puffs on the door side; their triangles need a higher cap
        {
            r.version = "1.1.0"; r.maxTriangles = 45000;
            r.description = "Stop 1's world: a sunset sky (lavender, pink, apricot, butter-yellow horizon, a low sun) over a sea of rolling pink clouds with cumulus rising out of it, puffs all the way round the booth (door side too) and a reflection probe for the sunset.";
            r.changelog = "1.1.0 (2026-10-20): 10 more puffs, on the door side, so the booth stands in clouds all the way round (Astra: \"just all clouds\"); triangle cap 30000 -> 45000.\n" + r.changelog;
        }
        r.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        EditorUtility.SetDirty(r); AssetDatabase.SaveAssets();
    }

    // ---------------------------------------------------------------- scene

    [MenuItem("LiminalDoors/Stop 01/Add sky and clouds")]
    public static void AddToScene()
    {
        if (SceneManager.GetActiveScene().path != LiminalStop01.ScenePath) EditorSceneManager.OpenScene(LiminalStop01.ScenePath, OpenSceneMode.Single);
        var stop = GameObject.Find(LiminalStop01.StopName); if (stop == null) throw new System.Exception("No " + LiminalStop01.StopName + " in the scene.");
        Place(stop.transform);
        EditorSceneManager.MarkSceneDirty(stop.scene);
        EditorSceneManager.SaveScene(stop.scene);
    }

    public static void Place(Transform stop)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { BuildPrefab(); prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath); }
        var old = stop.Find(InstanceName); if (old != null) Object.DestroyImmediate(old.gameObject);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stop.gameObject.scene);
        inst.name = InstanceName; inst.transform.SetParent(stop, false);

        // scene-wide, for the sunset (stop 02's shaders carry their own sun and sky, so they don't use these)
        var sun = stop.gameObject.scene.GetRootGameObjects().Select(g => g.GetComponent<Light>()).FirstOrDefault(l => l != null && l.type == LightType.Directional);
        if (sun != null)
        {
            sun.transform.rotation = SunRotation; sun.color = new Color(1f, .74f, .58f); sun.intensity = 1.15f; sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
        }
        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(Dir + "Materials/Sunset sky.mat");
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.78f, .55f, .76f);
        RenderSettings.ambientEquatorColor = new Color(.95f, .66f, .66f);
        RenderSettings.ambientGroundColor = new Color(.62f, .43f, .56f);
        DynamicGI.UpdateEnvironment();
        foreach (var p in inst.GetComponentsInChildren<ReflectionProbe>()) p.RenderProbe();
    }
}
