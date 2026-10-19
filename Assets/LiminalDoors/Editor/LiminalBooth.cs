// Claude, 2026-10-18. The Liminal Doors phone booth as one asset, made with Astra through unity-asset-creator.
// Astra, 2026-10-18: "The phone booth needs to match the world around it. I want there to be sparkles swirling around
// it." Her picks: hot pink with a butter-yellow glow (the floor plan's palette: hot pink / apricot / butter yellow,
// concept plate 01), and gold and pink sparkles.
//
// Astra, 2026-10-19: "this booth still looks like rudimentary shapes ... I want to use those skills we researched to fix
// the phone and the booth to look real and handcrafted, not computer generated." So the shape is now modelled in Blender
// (Tools/Blender/make_phone_booth.py): a K6 kiosk with bevelled cast-iron parts, narrow-wide-narrow glazing, lit
// TELEPHONE signs, gilded crowns, a dome, a brass door pull, and a real payphone (keypad, card, coin slots, handset,
// coiled cord). Its textures are baked there: ambient occlusion, edge wear, grime, rain streaks and sun fade over CC0
// ambientCG scans (PaintedMetal002, Metal009, Smear002, Scratches001). Our own Unity Stable/Handmade PBR shows them
// (clear coat that mirrors the sunset, world-space scan detail, specular occlusion); the glass is Pink Glass with
// fingerprints. Later that day Astra found the plum board behind the phone weird: the phone now hangs on a panel built
// into the back wall, in the booth's pink.
// The six Infinite Pole meshes in Meshes/ stay only for LiminalStop01's fallback.
//
// Menu: LiminalDoors/Booth/Import Blender model -> Model/*.asset meshes from Review/booth-blender/phone-booth.json,
//                                                  and import settings for Textures/booth-*, payphone-*, glass-*
//       LiminalDoors/Booth/Apply look           -> the booth's materials (overwrites hand edits)
//       LiminalDoors/Booth/Build prefab         -> Assets/LiminalDoors/Booth/Phone Booth.prefab + its asset recipe
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityUnicorn.AssetCreator;
using Object = UnityEngine.Object;

public static class LiminalBooth
{
    const string Dir = "Assets/LiminalDoors/Booth/";
    const string PrefabPath = Dir + "Phone Booth.prefab";
    const string RecipePath = Dir + "liminal-doors-phone-booth-asset-recipe.asset";
    const string SparkleTex = Dir + "Textures/booth-sparkle.png";
    const string SparkleMat = Dir + "Materials/Booth sparkles.mat";
    const string ModelDir = Dir + "Model/";
    const string TexDir = Dir + "Textures/";
    const string ModelJson = "Review/booth-blender/phone-booth.json";   // written by Tools/Blender/make_phone_booth.py

    // The model's parts: mesh, material, parent, position, Y rotation, casts shadows. The door hangs on its hinge (the -X
    // post's inner edge, at the outer face) and stands open 80 degrees, as in make_phone_booth.py (DOOR_HINGE, DOOR_OPEN).
    static readonly (string mesh, string mat, string parent, Vector3 pos, float rotY, bool shadows)[] Parts =
    {
        ("Booth body", "Booth", "", Vector3.zero, 0f, true),
        ("Booth glass", "Booth glass (pink)", "", Vector3.zero, 0f, false),
        ("Booth door", "Booth", "", new Vector3(-.53f, 0f, .625f), -80f, true),
        ("Booth door glass", "Booth glass (pink)", "Booth door", Vector3.zero, 0f, false),
        ("Payphone", "Payphone", "", Vector3.zero, 0f, true),
        ("Payphone handset", "Payphone", "", Vector3.zero, 0f, true),
        ("Payphone cord", "Payphone", "", Vector3.zero, 0f, true),
    };

    [Serializable] class BlenderMesh { public string name, material, parent; public float[] position; public float rotationY; public float[] vertices, normals, tangents, uv; public int[] triangles; }
    [Serializable] class BlenderFile { public string source; public BlenderMesh[] meshes; }

    [MenuItem("LiminalDoors/Booth/Import Blender model")]
    public static void ImportModel()
    {
        if (!File.Exists(ModelJson)) throw new Exception(ModelJson + " is missing: run Tools/Blender/make_phone_booth.py first");
        var file = JsonUtility.FromJson<BlenderFile>(File.ReadAllText(ModelJson));
        Directory.CreateDirectory(ModelDir);
        foreach (var b in file.meshes)
        {
            var path = ModelDir + b.name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path); bool isNew = mesh == null;
            if (isNew) mesh = new Mesh();
            mesh.Clear(); mesh.name = b.name;
            int n = b.vertices.Length / 3;
            var v = new Vector3[n]; var nn = new Vector3[n]; var t = new Vector4[n]; var uv = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                v[i] = new Vector3(b.vertices[i * 3], b.vertices[i * 3 + 1], b.vertices[i * 3 + 2]);
                nn[i] = new Vector3(b.normals[i * 3], b.normals[i * 3 + 1], b.normals[i * 3 + 2]);
                t[i] = new Vector4(b.tangents[i * 4], b.tangents[i * 4 + 1], b.tangents[i * 4 + 2], b.tangents[i * 4 + 3]);
                uv[i] = new Vector2(b.uv[i * 2], b.uv[i * 2 + 1]);
            }
            mesh.indexFormat = n > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(v); mesh.SetNormals(nn); mesh.SetTangents(t); mesh.SetUVs(0, uv);
            mesh.SetTriangles(b.triangles, 0); mesh.RecalculateBounds();
            if (!b.name.Contains("glass")) Unwrapping.GenerateSecondaryUVSet(mesh);   // lightmap UVs
            if (isNew) AssetDatabase.CreateAsset(mesh, path); else EditorUtility.SetDirty(mesh);
            Debug.Log("[Liminal] booth mesh " + b.name + ": " + b.triangles.Length / 3 + " triangles");
        }
        AssetDatabase.SaveAssets();
        ConfigureTextures();
    }

    // Import settings for the baked textures: colour maps in sRGB, data maps linear, the normal maps as normal maps.
    static void ConfigureTextures()
    {
        foreach (var path in Directory.GetFiles(TexDir, "*.png").Select(p => p.Replace('\\', '/')))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (!(name.StartsWith("booth-") || name.StartsWith("payphone-") || name.StartsWith("glass-") || name.StartsWith("detail-")) || name == "booth-sparkle") continue;
            var imp = (TextureImporter)AssetImporter.GetAtPath(path); if (imp == null) continue;
            bool color = name.EndsWith("-albedo") || name.EndsWith("-emission");
            imp.textureType = name.EndsWith("-normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.sRGBTexture = color;
            imp.alphaSource = name.EndsWith("-metallic-smoothness") ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            bool tiles = name.StartsWith("glass-") || name.StartsWith("detail-");   // tiling scans
            imp.mipmapEnabled = true; imp.wrapMode = tiles ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            imp.anisoLevel = name.EndsWith("-albedo") || name.EndsWith("-normal") ? 4 : 1;
            imp.maxTextureSize = name.StartsWith("payphone-") || name.EndsWith("-occlusion") || name.EndsWith("-emission") || tiles ? 1024 : 2048;
            imp.textureCompression = TextureImporterCompression.Compressed; imp.crunchedCompression = false;
            imp.SaveAndReimport();
        }
    }

    static Material Mat(string name, string shader)
    {
        var path = Dir + "Materials/" + name + ".mat";
        var sh = Shader.Find(shader); if (sh == null) throw new Exception("Shader '" + shader + "' isn't in this world (shaders-shop)");
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh) { name = name }; AssetDatabase.CreateAsset(m, path); }
        else if (m.shader != sh) m.shader = sh;
        m.enableInstancing = true;
        return m;
    }

    static Texture2D Tex(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(TexDir + name + ".png") ?? throw new Exception("missing texture " + TexDir + name + ".png");

    // Our own Unity Stable/Handmade PBR with the baked maps (metallic R, clear coat G, worn edges B, smoothness A) and a
    // tiling scan for the fine detail. Astra, 2026-10-19: "the colors all look flat, there is no texture. I think we are
    // missing shaders." Unity's Standard had no clear coat and no detail sharper than the atlas.
    static Material Handmade(string name, string prefix, float emission, string detail, float tiling, float detailNormal, float detailRough,
                             float coat, float edgeSheen, float saturation, float contrast)
    {
        var m = Mat(name, "Unity Stable/Handmade PBR");
        m.SetColor("_Color", Color.white); m.SetTexture("_MainTex", Tex(prefix + "-albedo"));
        m.SetTexture("_MetallicGlossMap", Tex(prefix + "-metallic-smoothness")); m.SetFloat("_SmoothnessScale", 1f);
        m.SetTexture("_BumpMap", Tex(prefix + "-normal")); m.SetFloat("_BumpScale", 1f);
        m.SetTexture("_OcclusionMap", Tex(prefix + "-occlusion")); m.SetFloat("_OcclusionStrength", 1f); m.SetFloat("_SpecularOcclusion", .8f);
        m.SetTexture("_DetailNormalMap", Tex("detail-" + detail + "-normal")); m.SetTexture("_DetailRoughMap", Tex("detail-" + detail + "-roughness"));
        m.SetFloat("_DetailTiling", tiling); m.SetFloat("_DetailNormalScale", detailNormal); m.SetFloat("_DetailRoughAmount", detailRough);
        m.SetFloat("_CoatStrength", coat); m.SetFloat("_CoatSmoothness", .93f);
        m.SetFloat("_EdgeSheen", edgeSheen); m.SetFloat("_Saturation", saturation); m.SetFloat("_Contrast", contrast);
        if (emission > 0)
        {
            m.SetTexture("_EmissionMap", Tex(prefix + "-emission")); m.SetColor("_EmissionColor", Color.white * emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }
        else { m.SetTexture("_EmissionMap", null); m.SetColor("_EmissionColor", Color.black); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack; }
        EditorUtility.SetDirty(m);
        return m;
    }

    [MenuItem("LiminalDoors/Booth/Apply look")]
    public static void ApplyLook()
    {
        // enamel booth: glossy clear coat over the pink, orange peel and scratches from the painted-metal scan
        Handmade("Booth", "booth", 2.0f, "paint", 1.6f, .45f, .2f, 1f, .4f, 1.08f, 1.12f);   // the butter-yellow signs glow past the bloom threshold
        // payphone: brushed steel and chrome (no coat), glossy handset and card (their coat comes from the bake)
        Handmade("Payphone", "payphone", 0f, "steel", 3.5f, .35f, .15f, 1f, .3f, 1f, 1.08f);
        var glass = Mat("Booth glass (pink)", "Unity Stable/Pink Glass");
        glass.SetColor("_Tint", new Color(1f, .72f, .84f, .14f)); glass.SetFloat("_Reflection", .9f); glass.SetFloat("_ReflectionBlur", .5f);
        glass.SetColor("_EdgeGlow", new Color(1.2f, .5f, .8f)); glass.SetFloat("_EdgePower", 2.5f);
        glass.SetTexture("_Smudge", Tex("glass-smudge")); glass.SetTextureScale("_Smudge", new Vector2(.9f, .9f)); glass.SetFloat("_SmudgeAmount", .6f);
        EditorUtility.SetDirty(glass);
        // the candy-paint look this replaces, and the lilToon / Mochie test materials before it
        foreach (var old in new[] { "Booth paint", "Booth glow", "Payphone face (candy)", "Booth glass (Mochie)", "Payphone face (lilToon)" })
            AssetDatabase.DeleteAsset(Dir + "Materials/" + old + ".mat");
        AssetDatabase.SaveAssets();
        Debug.Log("[Liminal] booth look applied (Blender bake + Unity Stable/Handmade PBR)");
    }

    [MenuItem("LiminalDoors/Booth/Build prefab")]
    public static void BuildPrefab()
    {
        ApplyLook();
        EnsureSparkleTexture();
        var sparkle = EnsureSparkleMaterial();

        var root = new GameObject("Phone Booth");
        try
        {
            foreach (var p in Parts)
            {
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(ModelDir + p.mesh + ".asset") ?? throw new Exception("missing " + ModelDir + p.mesh + ".asset (Import Blender model first)");
                var go = new GameObject(p.mesh);
                go.transform.SetParent(p.parent == "" ? root.transform : root.transform.Find(p.parent), false);
                go.transform.localPosition = p.pos; go.transform.localRotation = Quaternion.Euler(0f, p.rotY, 0f);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Dir + "Materials/" + p.mat + ".mat");
                if (!p.shadows) r.shadowCastingMode = ShadowCastingMode.Off;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
            }
            // where people stand (the plinth top, 8 cm up), the three glazed walls and the door, so nobody walks through
            var floor = new GameObject("Booth floor"); floor.transform.SetParent(root.transform, false);
            var fc = floor.AddComponent<BoxCollider>(); fc.center = new Vector3(0, .04f, 0); fc.size = new Vector3(1.4f, .08f, 1.4f);
            var walls = new GameObject("Booth walls"); walls.transform.SetParent(root.transform, false);
            foreach (var (c, s) in new[] { (new Vector3(0, 1.14f, -.6f), new Vector3(1.3f, 2.12f, .08f)), (new Vector3(-.6f, 1.14f, 0), new Vector3(.08f, 2.12f, 1.3f)), (new Vector3(.6f, 1.14f, 0), new Vector3(.08f, 2.12f, 1.3f)) })
            { var bc = walls.AddComponent<BoxCollider>(); bc.center = c; bc.size = s; }
            var dc = root.transform.Find("Booth door").gameObject.AddComponent<BoxCollider>(); dc.center = new Vector3(.53f, 1.05f, -.025f); dc.size = new Vector3(1.06f, 1.94f, .05f);

            Swirl(root.transform, sparkle);
            BaseGlow(root.transform, sparkle);

            AssetDatabase.SaveAssets();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { Object.DestroyImmediate(root); }
        EnsureRecipe();
        Debug.Log("[Liminal] booth prefab written: " + PrefabPath);
    }

    // The stop's reflection probe is realtime and renders when the world loads (VRChat). In the editor it stays black
    // after a restart until something renders it, and then every reflection (metal, clear coat) renders black too.
    [MenuItem("LiminalDoors/Booth/Refresh reflections")]
    public static void RefreshReflections()
    {
        int n = 0;
        foreach (var p in Object.FindObjectsOfType<ReflectionProbe>())
            if (p.mode == ReflectionProbeMode.Realtime) { p.RenderProbe(); n++; }
        Debug.Log("[Liminal] rendered " + n + " realtime reflection probes");
    }

    // Close-ups of the booth in the open scene (its own sky and light) for review: the payphone, a crown and sign, the
    // door pull. Positions are in the booth's (or the door's) own space.
    [MenuItem("LiminalDoors/Booth/Render close-ups")]
    public static void RenderCloseUps()
    {
        var booth = GameObject.Find("Phone booth") ?? throw new Exception("no 'Phone booth' in the open scene");
        var t = booth.transform; var door = t.Find("Booth door");
        var shots = new (string name, Transform space, Vector3 pos, Vector3 look, float fov)[]
        {
            ("payphone", t, new Vector3(.16f, 1.55f, .32f), new Vector3(-.01f, 1.37f, -.49f), 42f),
            ("crown-and-sign", t, new Vector3(.38f, 1.95f, 1.85f), new Vector3(0f, 2.28f, .66f), 30f),
            ("door-pull", door, new Vector3(.62f, 1.3f, .62f), new Vector3(1.03f, 1.1f, .036f), 38f),
            ("handset-and-cord", t, new Vector3(.42f, 1.25f, -.08f), new Vector3(.08f, 1.22f, -.48f), 38f),
        };
        const string dir = "Review/booth-closeups/";
        Directory.CreateDirectory(dir);
        var go = new GameObject("close-up camera") { hideFlags = HideFlags.HideAndDontSave };
        var cam = go.AddComponent<Camera>(); cam.nearClipPlane = .02f; cam.farClipPlane = 3000f; cam.allowHDR = true; cam.clearFlags = CameraClearFlags.Skybox;
        var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
        var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        try
        {
            cam.targetTexture = rt;
            foreach (var s in shots)
            {
                go.transform.position = s.space.TransformPoint(s.pos); go.transform.LookAt(s.space.TransformPoint(s.look)); cam.fieldOfView = s.fov;
                cam.Render(); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
                File.WriteAllBytes(dir + "booth-" + s.name + ".png", tex.EncodeToPNG());
            }
        }
        finally { RenderTexture.active = null; cam.targetTexture = null; Object.DestroyImmediate(go); Object.DestroyImmediate(rt); Object.DestroyImmediate(tex); }
        Debug.Log("[Liminal] booth close-ups written to " + dir);
    }

    // Gold and pink sparkles that swirl up around the booth in a slow helix.
    static void Swirl(Transform parent, Material mat)
    {
        var go = new GameObject("Swirling sparkles"); go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0, .15f, 0);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true; main.prewarm = true; main.duration = 8f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5.5f, 8.5f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(.05f, .12f);   // Astra: more and brighter
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .8f, .34f), new Color(1f, .42f, .74f));   // gold .. pink
        main.simulationSpace = ParticleSystemSimulationSpace.Local;   // the swirl stays centred on the booth
        main.maxParticles = 300;
        main.cullingMode = ParticleSystemCullingMode.Automatic;
        var em = ps.emission; em.rateOverTime = 36f;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = 1.15f; shape.radiusThickness = .3f;
        shape.rotation = new Vector3(90f, 0f, 0f);   // circle lies flat around the booth
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
        // every axis of a velocity curve must use the same mode (two constants here), or Unity ignores the whole module
        vel.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f); vel.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f); vel.orbitalY = new ParticleSystem.MinMaxCurve(.6f, .95f);   // round the booth
        vel.radial = new ParticleSystem.MinMaxCurve(-.02f, .04f);
        vel.x = new ParticleSystem.MinMaxCurve(0f, 0f); vel.z = new ParticleSystem.MinMaxCurve(0f, 0f); vel.y = new ParticleSystem.MinMaxCurve(.22f, .42f);   // and up
        var noise = ps.noise; noise.enabled = true; noise.strength = .06f; noise.frequency = .6f; noise.scrollSpeed = .2f; noise.quality = ParticleSystemNoiseQuality.Low;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .15f), new GradientAlphaKey(.85f, .7f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var size = ps.sizeOverLifetime; size.enabled = true;   // a slow twinkle
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, .6f), new Keyframe(.25f, 1.1f), new Keyframe(.5f, .75f), new Keyframe(.75f, 1.05f), new Keyframe(1f, .5f)));
        var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat; r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; r.maxParticleSize = .1f;
    }

    // A low gold glow at the base: soft motes lifting off the floor around the booth.
    static void BaseGlow(Transform parent, Material mat)
    {
        var go = new GameObject("Base glow"); go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0, .04f, 0);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true; main.prewarm = true; main.duration = 5f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(.05f, .12f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, .78f, .3f, .8f), new Color(1f, .9f, .55f, .6f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 60;
        var em = ps.emission; em.rateOverTime = 14f;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(1.7f, .02f, 1.7f);
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(-.03f, .03f); vel.z = new ParticleSystem.MinMaxCurve(-.03f, .03f); vel.y = new ParticleSystem.MinMaxCurve(.04f, .12f);
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(.9f, .3f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat; r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; r.maxParticleSize = .1f;
    }

    // A 64 px sparkle: a soft round glow with a thin four-point star through it. Drawn here, so it's our own.
    static void EnsureSparkleTexture()
    {
        if (File.Exists(SparkleTex)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(SparkleTex));
        const int n = 64; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float u = (x + .5f) / n * 2 - 1, v = (y + .5f) / n * 2 - 1, d = Mathf.Sqrt(u * u + v * v);
                float glow = Mathf.Pow(Mathf.Clamp01(1 - d), 2.2f);
                float star = Mathf.Pow(Mathf.Clamp01(1 - Mathf.Abs(u) * 14), 2) * Mathf.Clamp01(1 - Mathf.Abs(v)) + Mathf.Pow(Mathf.Clamp01(1 - Mathf.Abs(v) * 14), 2) * Mathf.Clamp01(1 - Mathf.Abs(u));
                float a = Mathf.Clamp01(glow * .85f + star * .9f);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        File.WriteAllBytes(SparkleTex, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(SparkleTex);
        var imp = (TextureImporter)AssetImporter.GetAtPath(SparkleTex);
        imp.alphaIsTransparency = true; imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 64;
        imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport();
    }

    static Material EnsureSparkleMaterial()
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(SparkleMat);
        if (m == null)
        {
            m = new Material(Shader.Find("Unity Stable/Sparkle")) { name = "Booth sparkles" };
            AssetDatabase.CreateAsset(m, SparkleMat);
        }
        m.shader = Shader.Find("Unity Stable/Sparkle");
        m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(SparkleTex));
        m.SetFloat("_Brightness", 2.6f);   // HDR: the brightest sparkles pass the bloom threshold
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    static void EnsureRecipe()
    {
        var r = AssetDatabase.LoadAssetAtPath<AssetRecipe>(RecipePath);
        if (r == null) { r = ScriptableObject.CreateInstance<AssetRecipe>(); NewRecipe(r); AssetDatabase.CreateAsset(r, RecipePath); }
        r.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        r.maxMaterials = 5; r.maxParticles = 400;   // Astra, 2026-10-18: merge to 5 materials; more and brighter sparkles (300 + 60 base glow)
        // 2026-10-19, the handmade rebuild: a hero prop for PC worlds, modelled with bevels and real detail (about 26k
        // triangles) and baked to a 2048 booth atlas and a 1024 payphone atlas
        r.maxTriangles = 30000; r.maxTextureSize = 2048;
        r.description = "The pink K6 phone booth at the start of Liminal Doors: a bevelled cast-iron kiosk in hot pink enamel with chipped edges and weathering, lit butter-yellow TELEPHONE signs, gilded crowns, a brass door pull, pink glass with fingerprints, and a payphone inside with a chrome keypad, handset and coiled cord. Gold and pink sparkles swirl up around it.";
        r.source = "Modelled in Blender by Tools/Blender/make_phone_booth.py after Sir Giles Gilbert Scott's K6 kiosk (proportions only); textures baked there from CC0 ambientCG scans; sparkles and prefab made in Liminal Doors";
        r.attribution = "Surface scans: ambientCG.com PaintedMetal002, Metal009, Smear002, Scratches001 (CC0; credit not required)";
        r.changelog = "1.0.0: first version. Handmade K6 booth modelled in Blender (bevels, recessed posts, narrow-wide-narrow glazing, lit TELEPHONE signs, gilded crowns, dome, brass pull); baked ambient occlusion, edge wear, grime, rain streaks and sun fade over CC0 scans; our own Unity Stable/Handmade PBR (clear coat, world-space scan detail); the payphone on a panel built into the back wall, with chrome keypad, instruction card, coin slots, handset and coiled cord; pink glass with fingerprints (Unity Stable Pink Glass); gold and pink sparkles swirling up around it (Unity Stable Sparkle); gold base glow.";
        EditorUtility.SetDirty(r); AssetDatabase.SaveAssets();
    }

    static void NewRecipe(AssetRecipe r)
    {
        r.id = "liminal-doors-phone-booth"; r.displayName = "Liminal Doors phone booth"; r.kind = "prop"; r.version = "1.0.0";
        r.forPC = true; r.forAndroid = false; r.moves = false;
        r.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        r.creator = "Astra (built with Claude)";
        r.license = "own-work"; r.publicOk = true;
    }
}
