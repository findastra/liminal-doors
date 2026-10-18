// Claude, 2026-10-18. Liminal Doors, stop 01: Fox / Pink Booth (floor register 2026-10-15, scene 1).
// "The exact booth from Astra's Infinite Pole, alone above a sea of pink clouds. Enter the booth through a
// fine drift of gold light." The booth, its cloud mound and sparkles, the cloud sea, the sky and the bloom are
// Infinite Pole's own assets, copied with their GUIDs (prefabs extracted from AstrasInfinitePole.unity, read only).
// New here: the gold door drift, the empty fox hiding spot (the glass fox is a later pass) and the VRChat spawn.
// 2026-10-18 (doors): this scene is now the whole world ("Liminal Doors.unity"; it was "01 Fox - Pink Booth.unity").
// Stop 01 is the start: you spawn in the booth, and stepping out through the booth door (the gold light) sends you to a
// random stop (Astra). VRCWorld and the reference camera sit at the scene root so switching stops never hides them.
// Build scene writes a fresh world with stop 01 only; LiminalDoors/World/Build all adds the rest in order.
//
// Menu: LiminalDoors/Stop 01/Build scene  -> (re)writes Assets/LiminalDoors/Scenes/Liminal Doors.unity
//       LiminalDoors/Stop 01/Render review shots -> Review/stop-01-render/*.png + report.md
// Batch (new project only, never on an open editor): -executeMethod LiminalStop01.BatchBuildAndRender
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public static class LiminalStop01
{
    public const string ScenePath = "Assets/LiminalDoors/Scenes/Liminal Doors.unity";
    const string OldScenePath = "Assets/LiminalDoors/Scenes/01 Fox - Pink Booth.unity";
    const string BoothPrefab = "Assets/LiminalDoors/Prefabs/Pink Booth (from Infinite Pole).prefab";
    const string SeaPrefab = "Assets/LiminalDoors/Prefabs/Pink Cloud Sea (from Infinite Pole).prefab";
    const string SkyMaterial = "Assets/Astra/Materials/Backdrop/Pink Cloud Sea Sky.mat";
    const string BloomProfile = "Assets/Astra/Materials/Pinkscape Bloom.asset";
    const string GoldMaterial = "Assets/LiminalDoors/Materials/Door Gold Drift.mat";
    const string ReviewDir = "Review/stop-01-render";

    [MenuItem("LiminalDoors/Stop 01/Build scene")]
    public static void BuildScene()
    {
        var booth = Load<GameObject>(BoothPrefab);
        var sea = Load<GameObject>(SeaPrefab);
        var sky = Load<Material>(SkyMaterial);
        var bloom = Load<PostProcessProfile>(BloomProfile);

        // one-time rename of the stop 01 scene to the world scene (MoveAsset keeps its GUID)
        if (File.Exists(OldScenePath) && !File.Exists(ScenePath))
        {
            if (SceneManager.GetActiveScene().path == OldScenePath) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var err = AssetDatabase.MoveAsset(OldScenePath, ScenePath);
            if (!string.IsNullOrEmpty(err)) throw new Exception("Could not rename the scene: " + err);
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var stop = new GameObject("Stop 01 - Fox - Pink Booth");

        // Both prefabs keep their Infinite Pole world pose, so the booth sits on its mound 1.4 m above the sea
        // exactly as it does there.
        var boothGo = (GameObject)PrefabUtility.InstantiatePrefab(booth, scene);
        boothGo.name = "Pink booth (from Astra's Infinite Pole)";
        boothGo.transform.SetParent(stop.transform, true);
        var seaGo = (GameObject)PrefabUtility.InstantiatePrefab(sea, scene);
        seaGo.name = "Pink cloud sea (from Astra's Infinite Pole)";
        seaGo.transform.SetParent(stop.transform, true);

        // Booth geometry lives under "Banner booth" (scale 1.1). Its door is on local +Z; the payphone hangs on -Z.
        var frame = boothGo.transform.Find("Banner booth");
        if (frame == null) throw new Exception("Booth prefab has no 'Banner booth' child.");

        var drift = new GameObject("Door light drift (gold)");
        drift.transform.SetParent(stop.transform, false);
        drift.transform.SetPositionAndRotation(frame.TransformPoint(0, 1.15f, 0.95f), frame.rotation);
        BuildGoldDrift(drift);

        var fox = new GameObject("Fox hiding spot (glass fox: later pass)");
        fox.transform.SetParent(stop.transform, false);
        fox.transform.SetPositionAndRotation(frame.TransformPoint(0.5f, 0.06f, -0.95f), frame.rotation);

        // Global bloom, the same profile and settings as Infinite Pole's "Volume".
        var volumeGo = new GameObject("Pinkscape bloom (global volume)");
        volumeGo.transform.SetParent(stop.transform, false);
        var volume = volumeGo.AddComponent<PostProcessVolume>();
        volume.sharedProfile = bloom;
        volume.isGlobal = true;
        volume.priority = 10;
        volume.weight = 1;

        // Door anchors (wired by LiminalDoors/World/Wire doors). You arrive and come back inside the booth, where you
        // spawn; the forward door is a trigger just outside the booth's door, inside the gold light, so stepping out
        // sends you on. The spawn is 0.8 m from it, well clear.
        var arrive = new GameObject("Arrival point");
        arrive.transform.SetParent(stop.transform, false);
        arrive.transform.SetPositionAndRotation(frame.TransformPoint(0, 0.05f, 0.05f), frame.rotation);
        var back = new GameObject("Return point");
        back.transform.SetParent(stop.transform, false);
        back.transform.SetPositionAndRotation(frame.TransformPoint(0, 0.05f, 0.05f), frame.rotation);
        var door = new GameObject("Forward door trigger");
        door.transform.SetParent(stop.transform, false);
        door.transform.SetPositionAndRotation(frame.TransformPoint(0, 1.05f, 1.0f), frame.rotation);
        var doorBox = door.AddComponent<BoxCollider>();
        doorBox.isTrigger = true;
        doorBox.size = new Vector3(1.2f, 2.2f, 0.45f);

        // VRChat reference camera (kept inactive, as the SDK expects) carries the post-processing layer.
        var refCam = new GameObject("VRChat reference camera");
        var cam = refCam.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f; cam.farClipPlane = 2500f; cam.allowHDR = true;
        AddPostLayer(refCam);
        refCam.SetActive(false);

        AddVrcWorld(null, frame, cam);

        RenderSettings.skybox = sky;
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.24f, 0.22f, 0.3f);
        RenderSettings.sun = null;

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };   // one world, one scene
        Debug.Log("[Liminal] Stop 01 scene written: " + ScenePath);
    }

    static void BuildGoldDrift(GameObject go)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(GoldMaterial);
        if (mat == null)
        {
            var shader = Shader.Find("Astra/Soft Sparkle");
            if (shader == null) throw new Exception("Shader Astra/Soft Sparkle not found.");
            mat = new Material(shader) { name = "Door Gold Drift", enableInstancing = true };
            mat.SetColor("_Color", new Color(1f, 0.74f, 0.32f, 1f) * 2.4f); // HDR so the bloom picks it up
            Directory.CreateDirectory(Path.GetDirectoryName(GoldMaterial));
            AssetDatabase.CreateAsset(mat, GoldMaterial);
        }

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true; main.prewarm = true;
        main.duration = 10f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.045f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.86f, 0.5f, 1f), new Color(1f, 0.66f, 0.3f, 0.8f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 260;

        var emission = ps.emission; emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(1.25f, 2.1f, 0.7f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
        vel.y = new ParticleSystem.MinMaxCurve(0.01f, 0.06f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.01f); // drifts gently in through the door

        var noise = ps.noise;
        noise.enabled = true; noise.strength = 0.12f; noise.frequency = 0.35f; noise.scrollSpeed = 0.08f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.8f, 0.75f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }

    internal static void AddPostLayer(GameObject camGo)
    {
        var layer = camGo.AddComponent<PostProcessLayer>();
        layer.volumeTrigger = camGo.transform;
        layer.volumeLayer = 1; // Default, as in Infinite Pole
        layer.stopNaNPropagation = true;
        layer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
        // The layer needs its resources asset to render outside play mode.
        var res = AssetDatabase.FindAssets("t:PostProcessResources").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
        if (res != null) layer.Init(AssetDatabase.LoadAssetAtPath<PostProcessResources>(res));
    }

    // Uses the SDK's own VRCWorld prefab when the worlds SDK is present; looked up by name so this file
    // compiles without the SDK.
    static void AddVrcWorld(Transform parent, Transform frame, Camera referenceCamera)
    {
        var path = AssetDatabase.FindAssets("VRCWorld t:Prefab").Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == "VRCWorld");
        if (path == null) { Debug.LogWarning("[Liminal] VRCWorld prefab not found; no spawn added."); return; }
        var world = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        world.transform.SetParent(parent, false);
        // Spawn standing inside the booth on its floor collider, looking out through the door.
        world.transform.SetPositionAndRotation(frame.TransformPoint(0, 0.05f, 0.05f), frame.rotation);
        var desc = world.GetComponents<Component>().FirstOrDefault(c => c != null && c.GetType().Name == "VRCSceneDescriptor");
        if (desc == null) return;
        var so = new SerializedObject(desc);
        var refCam = so.FindProperty("ReferenceCamera");
        if (refCam != null) refCam.objectReferenceValue = referenceCamera.gameObject;
        var respawn = so.FindProperty("RespawnHeightY");
        if (respawn != null) respawn.floatValue = -60f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Opens the stop and points the Scene view at the booth door, the same angle as the hero render.
    [MenuItem("LiminalDoors/Stop 01/Open scene")]
    public static void OpenScene()
    {
        if (SceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var frame = GameObject.Find("Stop 01 - Fox - Pink Booth/Pink booth (from Astra's Infinite Pole)/Banner booth")?.transform;
        if (frame == null) return;
        Selection.activeGameObject = frame.parent.gameObject;
        var sv = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView : EditorWindow.GetWindow<SceneView>();
        Vector3 c = frame.TransformPoint(0, 1.2f, 0);
        sv.LookAt(c, Quaternion.LookRotation(-frame.forward + Vector3.down * 0.05f - frame.right * 0.2f), 3.2f);
        sv.Repaint();
    }

    // ---------- review renders ----------

    struct Shot { public string name; public Vector3 pos; public Vector3 look; public float fov; }

    [MenuItem("LiminalDoors/Stop 01/Render review shots")]
    public static void RenderShots()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var frame = GameObject.Find("Stop 01 - Fox - Pink Booth/Pink booth (from Astra's Infinite Pole)/Banner booth")?.transform;
        if (frame == null) throw new Exception("Scene has no booth; run Build scene first.");

        // Particles don't run outside play mode; step each one 9 s in so the sparkles and gold drift show.
        foreach (var ps in UnityEngine.Object.FindObjectsOfType<ParticleSystem>())
            ps.Simulate(9f, false, true, true);

        Vector3 c = frame.TransformPoint(0, 1.2f, 0), fwd = frame.forward, right = frame.right, up = Vector3.up;
        var shots = new[]
        {
            new Shot { name = "01-hero-door", pos = c + fwd * 5.4f + right * 1.1f - up * 0.35f, look = c + up * 0.15f, fov = 42 },
            new Shot { name = "02-wide-15m", pos = c + fwd * 14f + right * 5f + up * 2.5f, look = c - up * 0.6f, fov = 50 },
            new Shot { name = "03-birds-eye", pos = c + up * 26f + fwd * 7f, look = c - up * 1f, fov = 50 },
            new Shot { name = "04-behind-fox-spot", pos = c - fwd * 4.2f - right * 1.6f + up * 0.1f, look = c - up * 0.5f, fov = 45 },
            new Shot { name = "05-spawn-view", pos = frame.TransformPoint(0, 1.55f, 0.05f), look = frame.TransformPoint(0, 1.3f, 6f), fov = 70 },
        };

        Directory.CreateDirectory(ReviewDir);
        var camGo = new GameObject("_review camera") { hideFlags = HideFlags.DontSave };
        var report = new StringBuilder();
        report.AppendLine("# Liminal Doors stop 01 review renders");
        report.AppendLine("Rendered " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " in Unity " + Application.unityVersion +
                          " (" + SystemInfo.graphicsDeviceType + ", " + PlayerSettings.colorSpace + " colour).");
        report.AppendLine("Scene: `" + ScenePath + "`");
        report.AppendLine();
        try
        {
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 2500f; cam.allowHDR = true; cam.allowMSAA = false;
            AddPostLayer(camGo);
            foreach (var s in shots)
            {
                camGo.transform.position = s.pos;
                camGo.transform.rotation = Quaternion.LookRotation(s.look - s.pos, Vector3.up);
                cam.fieldOfView = s.fov;
                var file = Path.Combine(ReviewDir, "stop-01-" + s.name + ".png");
                RenderTo(cam, 1920, 1080, file);
                report.AppendLine("- `" + file.Replace('\\', '/') + "`: camera " + s.pos.ToString("F2") + " looking at " + s.look.ToString("F2") + ", fov " + s.fov);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(camGo); }

        var renderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
        report.AppendLine();
        report.AppendLine("Renderers in scene: " + renderers.Length + " (" + renderers.Count(r => r.enabled && r.gameObject.activeInHierarchy) + " visible).");
        foreach (var r in renderers.OrderBy(r => r.name))
            report.AppendLine("- " + r.name + ": " + string.Join(", ", r.sharedMaterials.Select(m => m == null ? "MISSING MATERIAL" : m.name + " / " + m.shader.name)) +
                              (r.gameObject.activeInHierarchy ? "" : " (inactive)"));
        File.WriteAllText(Path.Combine(ReviewDir, "report.md"), report.ToString());
        Debug.Log("[Liminal] Stop 01 renders written to " + ReviewDir);
    }

    internal static void RenderTo(Camera cam, int w, int h, string file)
    {
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 1 };
        var prev = RenderTexture.active;
        try
        {
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(file, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        finally
        {
            cam.targetTexture = null;
            RenderTexture.active = prev;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }

    public static void BatchBuildAndRender()
    {
        try
        {
            BuildScene();
            RenderShots();
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    static T Load<T>(string path) where T : UnityEngine.Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null) throw new Exception("Missing asset: " + path);
        return a;
    }
}
