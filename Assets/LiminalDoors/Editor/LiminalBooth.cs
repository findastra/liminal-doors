// Claude, 2026-10-18. The Liminal Doors phone booth as one asset, made with Astra through unity-asset-creator.
// Astra, 2026-10-18: "The phone booth needs to match the world around it. I want there to be sparkles swirling around
// it." Her picks: hot pink with a butter-yellow glow (the floor plan's palette: hot pink / apricot / butter yellow,
// concept plate 01), and gold and pink sparkles.
// Shape: the six booth meshes taken from Astra's Infinite Pole. Everything else here is new: materials, the sparkle
// texture (drawn by this script), the two particle systems and the prefab.
//
// Menu: LiminalDoors/Booth/Apply look       -> sets the booth materials to the chosen look (overwrites hand edits)
//       LiminalDoors/Booth/Build prefab     -> Assets/LiminalDoors/Booth/Phone Booth.prefab + its asset recipe
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityUnicorn.AssetCreator;

public static class LiminalBooth
{
    const string Dir = "Assets/LiminalDoors/Booth/";
    const string PrefabPath = Dir + "Phone Booth.prefab";
    const string RecipePath = Dir + "liminal-doors-phone-booth-asset-recipe.asset";
    const string SparkleTex = Dir + "Textures/booth-sparkle.png";
    const string SparkleMat = Dir + "Materials/Booth sparkles.mat";
    static readonly string[] Parts = { "Booth body", "Booth sign", "Booth glass", "Payphone chrome", "Payphone face", "Payphone lights" };

    // Astra, 2026-10-18 (asset-creator step 5): merge 7 materials to 5. The payphone housing wears the booth's hot pink and
    // the payphone lights share the sign's butter-yellow glow; the prefab uses body, sign (glow), glass, payphone face, sparkles.
    static string MaterialFor(string part) => part == "Payphone chrome" ? "Booth body" : part == "Payphone lights" ? "Booth sign" : part;

    // the look: albedo, metallic, smoothness, HDR emission (black = none)
    static void Look(string name, Color albedo, float metal, float smooth, Color emission)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(Dir + "Materials/" + name + ".mat");
        if (m == null) throw new System.Exception("Missing material " + name);
        bool glass = m.GetFloat("_Mode") >= 2;
        m.color = albedo; m.SetFloat("_Metallic", metal); m.SetFloat("_Glossiness", smooth); m.enableInstancing = true;
        if (emission.maxColorComponent > 0)
        {
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }
        else { m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack; }
        if (glass) m.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
    }

    [MenuItem("LiminalDoors/Booth/Apply look")]
    public static void ApplyLook()
    {
        var butter = new Color(1f, .86f, .46f);
        Look("Booth body", new Color(1f, .16f, .52f), 0f, .55f, new Color(.22f, .02f, .1f));            // hot pink, a little self-lit in the dusk
        Look("Booth sign", new Color(1f, .93f, .62f), 0f, .3f, butter * 2.4f);                           // butter-yellow glow (HDR: the bloom picks it up)
        Look("Booth glass", new Color(1f, .62f, .78f, .26f), 0f, .95f, new Color(.32f, .07f, .17f));     // warm pink-lit glass
        Look("Payphone chrome", new Color(1f, .66f, .8f), .85f, .82f, Color.black);                       // pink chrome
        Look("Payphone face", new Color(.2f, .08f, .15f), 0f, .45f, Color.black);                         // dark plum
        Look("Payphone lights", new Color(1f, .9f, .6f), 0f, .3f, butter * 1.7f);
        AssetDatabase.SaveAssets();
        Debug.Log("[Liminal] booth look applied");
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
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Dir + "Meshes/" + p + ".asset");
                if (mesh.uv2 == null || mesh.uv2.Length == 0) { Unwrapping.GenerateSecondaryUVSet(mesh); EditorUtility.SetDirty(mesh); }   // lightmap UVs
                var go = new GameObject(p); go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Dir + "Materials/" + MaterialFor(p) + ".mat");
                if (p == "Booth glass") r.shadowCastingMode = ShadowCastingMode.Off;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
            }
            var floor = new GameObject("Booth floor"); floor.transform.SetParent(root.transform, false);
            var fc = floor.AddComponent<BoxCollider>(); fc.center = new Vector3(0, .02f, 0); fc.size = new Vector3(1.3f, .04f, 1.3f);

            Swirl(root.transform, sparkle);
            BaseGlow(root.transform, sparkle);

            AssetDatabase.SaveAssets();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { Object.DestroyImmediate(root); }
        EnsureRecipe();
        Debug.Log("[Liminal] booth prefab written: " + PrefabPath);
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
            m = new Material(Shader.Find("Legacy Shaders/Particles/Additive")) { name = "Booth sparkles" };
            AssetDatabase.CreateAsset(m, SparkleMat);
        }
        m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(SparkleTex));
        m.SetColor("_TintColor", new Color(.9f, .84f, .78f, .8f));   // the shader doubles it, so the brightest sparkles reach the bloom
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
        EditorUtility.SetDirty(r); AssetDatabase.SaveAssets();
    }

    static void NewRecipe(AssetRecipe r)
    {
        r.id = "liminal-doors-phone-booth"; r.displayName = "Liminal Doors phone booth"; r.kind = "prop"; r.version = "1.0.0";
        r.description = "The pink phone booth at the start of Liminal Doors: hot pink with a butter-yellow glowing sign and warm pink-lit glass, with gold and pink sparkles swirling up around it and a soft gold glow at its base.";
        r.forPC = true; r.forAndroid = false; r.moves = false;
        r.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        r.creator = "Astra (built with Claude)";
        r.source = "Booth shape from Astra's Infinite Pole (her own world); materials, sparkles and prefab made in Liminal Doors";
        r.license = "own-work"; r.publicOk = true;
        r.changelog = "1.0.0: first version. Hot pink look, glowing sign, swirling gold and pink sparkles, gold base glow.";
    }
}
