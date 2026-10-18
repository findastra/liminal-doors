// Claude, 2026-10-18. Liminal Doors, fresh start (Astra, 2026-10-18: "do not start with a copy of Astra's Infinite
// Pole. The only thing you were supposed to take from there was the phone booth as a base, and we are changing it.")
// The project is VRChat Creator Companion's World template. From Infinite Pole there are only the booth's six shape
// meshes (Assets/LiminalDoors/Booth/Meshes); everything else here is new. The booth wears plain materials until Astra
// says how it changes.
//
// Stop 01 (Fox / Pink Booth) is the root "Stop 01 - Fox - Pink Booth" in the one world scene, so later stops can sit
// next to it. Building it again replaces only that root; materials that already exist are kept, so hand edits survive.
//
// Menu: LiminalDoors/Stop 01/Build      -> writes or updates Assets/LiminalDoors/Scenes/Liminal Doors.unity
//       LiminalDoors/Stop 01/Render     -> Review/stop-01-render/*.png + report.md
//       LiminalDoors/Stop 01/Open       -> opens the scene framed on the booth door
// Batch (a project no editor has open only): -executeMethod LiminalStop01.BatchBuildAndRender
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class LiminalStop01
{
    public const string ScenePath = "Assets/LiminalDoors/Scenes/Liminal Doors.unity";
    public const string StopName = "Stop 01 - Fox - Pink Booth";
    const string BoothPrefab = "Assets/LiminalDoors/Booth/Phone Booth.prefab";   // Astra's booth, when it's made
    const string MeshDir = "Assets/LiminalDoors/Booth/Meshes/";
    const string MatDir = "Assets/LiminalDoors/Booth/Materials/";
    const string SkyPath = "Assets/LiminalDoors/Materials/Plain Sky.mat";
    const string ReviewDir = "Review/stop-01-render";

    // the booth's parts: mesh file, material name, colour, metallic, smoothness, see-through
    static readonly (string mesh, string mat, Color color, float metal, float smooth, bool glass)[] Parts =
    {
        ("Booth body", "Booth body", new Color(.86f, .85f, .84f), 0f, .35f, false),
        ("Booth sign", "Booth sign", new Color(.95f, .95f, .95f), 0f, .2f, false),
        ("Booth glass", "Booth glass", new Color(.86f, .92f, .96f, .22f), 0f, .92f, true),
        ("Payphone chrome", "Payphone chrome", new Color(.72f, .72f, .74f), .9f, .78f, false),
        ("Payphone face", "Payphone face", new Color(.16f, .16f, .17f), 0f, .4f, false),
        ("Payphone lights", "Payphone lights", new Color(.8f, .8f, .8f), 0f, .3f, false),
    };

    [MenuItem("LiminalDoors/Stop 01/Build")]
    public static void Build()
    {
        Scene scene;
        if (File.Exists(ScenePath)) scene = SceneManager.GetActiveScene().path == ScenePath ? SceneManager.GetActiveScene() : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupWorld(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == StopName);
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
        var stop = new GameObject(StopName);
        SceneManager.MoveGameObjectToScene(stop, scene);

        // the booth: Astra's finished booth prefab when it exists (the booth session, 2026-10-18), otherwise the six plain
        // meshes. Either way its origin is the floor centre and the door is on local +Z, which the spawn and the door
        // anchors rely on; the instance is always named "Phone booth".
        GameObject booth;
        var boothPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoothPrefab);
        if (boothPrefab != null)
        {
            booth = (GameObject)PrefabUtility.InstantiatePrefab(boothPrefab, scene);
            booth.name = "Phone booth";
            booth.transform.SetParent(stop.transform, false);
        }
        else
        {
            booth = new GameObject("Phone booth");
            booth.transform.SetParent(stop.transform, false);
            foreach (var p in Parts)
            {
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshDir + p.mesh + ".asset");
                if (mesh == null) throw new Exception("Missing booth mesh " + MeshDir + p.mesh + ".asset");
                var go = new GameObject(p.mesh);
                go.transform.SetParent(booth.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = Material(p.mat, p.color, p.metal, p.smooth, p.glass);
                if (p.glass) r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }
        if (booth.transform.Find("Booth floor") == null)   // the prefab may bring its own floor
        {
            var floor = new GameObject("Booth floor");
            floor.transform.SetParent(booth.transform, false);
            var fc = floor.AddComponent<BoxCollider>(); fc.center = new Vector3(0, .02f, 0); fc.size = new Vector3(1.3f, .04f, 1.3f);
        }

        var fox = new GameObject("Fox hiding spot (glass fox: later)");
        fox.transform.SetParent(stop.transform, false);
        fox.transform.localPosition = new Vector3(.5f, .06f, -.95f);

        DoorAnchors(stop.transform, booth.transform);
        LiminalWorldGlow.AddStopVolume(stop.transform);   // so the booth's HDR sign and sparkles bloom while you're here
        bool rewire = scene.GetRootGameObjects().Any(g => g.name == "World - door travel");

        // spawn: standing on the booth floor, looking out through the door
        var world = scene.GetRootGameObjects().Select(g => g.GetComponentsInChildren<Component>(true).FirstOrDefault(c => c != null && c.GetType().Name == "VRCSceneDescriptor")).FirstOrDefault(c => c != null);
        if (world != null) world.transform.SetPositionAndRotation(booth.transform.TransformPoint(0, .05f, .05f), booth.transform.rotation);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[Liminal] Stop 01 built in " + ScenePath);
        // a rebuilt stop 01 loses its door's wiring with the old root; wire the doors again when the world has them
        if (rewire) EditorApplication.ExecuteMenuItem("LiminalDoors/World/Wire doors");
    }

    // Door system anchors (added by the doors session, 2026-10-18; wired by LiminalDoors/World/Wire doors). Astra:
    // "Spawn in the booth, and stepping out through the booth door sends you straight to a random door." You arrive
    // and come back where you spawn, on the booth floor; the forward door is a trigger just outside the booth's door
    // (local +Z), 0.7 m clear of the spawn. Safe to rerun; it only replaces these three objects.
    [MenuItem("LiminalDoors/Stop 01/Add door anchors")]
    public static void AddDoorAnchors()
    {
        var scene = SceneManager.GetActiveScene().path == ScenePath ? SceneManager.GetActiveScene() : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var stop = scene.GetRootGameObjects().FirstOrDefault(g => g.name == StopName);
        var booth = stop != null ? stop.transform.Find("Phone booth") : null;
        if (booth == null) throw new Exception("No " + StopName + "/Phone booth; run LiminalDoors/Stop 01/Build first.");
        DoorAnchors(stop.transform, booth);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // Stop 01's glow: the shared Liminal Glow bloom (LiminalWorldGlow), on only while you're at the booth. Adds it to the
    // existing stop without rebuilding the booth (the booth session asked for it, 2026-10-18).
    [MenuItem("LiminalDoors/Stop 01/Add glow volume")]
    public static void AddGlow()
    {
        var scene = SceneManager.GetActiveScene().path == ScenePath ? SceneManager.GetActiveScene() : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var stop = scene.GetRootGameObjects().FirstOrDefault(g => g.name == StopName);
        if (stop == null) throw new Exception("No " + StopName + "; run LiminalDoors/Stop 01/Build first.");
        LiminalWorldGlow.AddStopVolume(stop.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void DoorAnchors(Transform stop, Transform booth)
    {
        foreach (var n in new[] { "Arrival point", "Return point", "Forward door trigger" })
        {
            var old = stop.Find(n); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        foreach (var n in new[] { "Arrival point", "Return point" })
        {
            var p = new GameObject(n); p.transform.SetParent(stop, false);
            p.transform.SetPositionAndRotation(booth.TransformPoint(0, .05f, .05f), booth.rotation);
        }
        var door = new GameObject("Forward door trigger"); door.transform.SetParent(stop, false);
        door.transform.SetPositionAndRotation(booth.TransformPoint(0, 1.05f, .95f), booth.rotation);
        var box = door.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = new Vector3(1.1f, 2.1f, .45f);
    }

    // One-time scene setup: plain sky, one sun, soft ambient light, the VRChat world descriptor.
    static void SetupWorld(Scene scene)
    {
        var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyPath);
        if (sky == null)
        {
            sky = new Material(Shader.Find("Skybox/Procedural")) { name = "Plain Sky" };
            sky.SetFloat("_SunSize", .035f); sky.SetFloat("_AtmosphereThickness", 1.1f); sky.SetFloat("_Exposure", 1.15f);
            sky.SetColor("_SkyTint", new Color(.5f, .5f, .5f)); sky.SetColor("_GroundColor", new Color(.42f, .4f, .41f));
            Directory.CreateDirectory(Path.GetDirectoryName(SkyPath));
            AssetDatabase.CreateAsset(sky, SkyPath);
        }
        RenderSettings.skybox = sky;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.62f, .64f, .7f);
        RenderSettings.ambientEquatorColor = new Color(.5f, .49f, .5f);
        RenderSettings.ambientGroundColor = new Color(.32f, .31f, .32f);
        RenderSettings.fog = false;

        var sunGo = new GameObject("Sun");
        SceneManager.MoveGameObjectToScene(sunGo, scene);
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional; sun.color = new Color(1f, .96f, .9f); sun.intensity = 1.1f;
        sun.shadows = LightShadows.Soft; sun.lightmapBakeType = LightmapBakeType.Realtime;
        sunGo.transform.rotation = Quaternion.Euler(32f, 210f, 0f);
        RenderSettings.sun = sun;

        var path = AssetDatabase.FindAssets("VRCWorld t:Prefab").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == "VRCWorld");
        if (path == null) { Debug.LogWarning("[Liminal] VRCWorld prefab not found; no spawn added."); return; }
        var world = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), scene);
        var desc = world.GetComponents<Component>().FirstOrDefault(c => c != null && c.GetType().Name == "VRCSceneDescriptor");
        if (desc != null)
        {
            var so = new SerializedObject(desc);
            var respawn = so.FindProperty("RespawnHeightY"); if (respawn != null) respawn.floatValue = -60f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static Material Material(string name, Color color, float metal, float smooth, bool glass)
    {
        var path = MatDir + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;   // keep hand edits
        m = new Material(Shader.Find("Standard")) { name = name, enableInstancing = true };
        m.color = color; m.SetFloat("_Metallic", metal); m.SetFloat("_Glossiness", smooth);
        if (glass)
        {
            m.SetFloat("_Mode", 3);   // Transparent
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.One); m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON"); m.DisableKeyword("_ALPHABLEND_ON"); m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
        }
        Directory.CreateDirectory(MatDir);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    [MenuItem("LiminalDoors/Stop 01/Open")]
    public static void Open()
    {
        if (SceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var booth = GameObject.Find(StopName + "/Phone booth"); if (booth == null) return;
        Selection.activeGameObject = booth;
        var sv = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView : EditorWindow.GetWindow<SceneView>();
        var t = booth.transform;
        sv.LookAt(t.TransformPoint(0, 1.3f, 0), Quaternion.LookRotation(-t.forward + Vector3.down * .08f - t.right * .3f), 3.4f);
        sv.Repaint();
    }

    // ---------- review renders ----------

    [MenuItem("LiminalDoors/Stop 01/Render")]
    public static void Render()
    {
        if (SceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var booth = GameObject.Find(StopName + "/Phone booth")?.transform;
        if (booth == null) throw new Exception("No booth in the scene; run Stop 01/Build first.");
        var shots = new (string name, Vector3 pos, Vector3 look, float fov)[]
        {
            ("01-door", booth.TransformPoint(1.3f, 1.05f, 5.3f), booth.TransformPoint(0, 1.35f, 0), 40),
            ("02-three-quarter", booth.TransformPoint(-3.6f, 2.2f, 4.2f), booth.TransformPoint(0, 1.25f, 0), 42),
            ("03-back", booth.TransformPoint(2.4f, 1.6f, -4.4f), booth.TransformPoint(0, 1.3f, 0), 42),
            ("04-spawn-view", booth.TransformPoint(0, 1.55f, .05f), booth.TransformPoint(0, 1.35f, 6f), 70),
        };
        Directory.CreateDirectory(ReviewDir);
        var report = new StringBuilder();
        report.AppendLine("# Liminal Doors stop 01 (fresh start) review renders");
        report.AppendLine("Rendered " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " in Unity " + Application.unityVersion + " (" + SystemInfo.graphicsDeviceType + ", " + PlayerSettings.colorSpace + " colour).");
        report.AppendLine("Scene: `" + ScenePath + "`");
        report.AppendLine();
        var camGo = new GameObject("_review camera") { hideFlags = HideFlags.DontSave };
        try
        {
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox; cam.nearClipPlane = .05f; cam.farClipPlane = 2000f; cam.allowHDR = true; cam.allowMSAA = true;
            foreach (var s in shots)
            {
                camGo.transform.SetPositionAndRotation(s.pos, Quaternion.LookRotation(s.look - s.pos, Vector3.up));
                cam.fieldOfView = s.fov;
                var file = Path.Combine(ReviewDir, "stop-01-" + s.name + ".png");
                RenderTo(cam, 1920, 1080, file);
                report.AppendLine("- `" + file.Replace('\\', '/') + "`: camera " + s.pos.ToString("F2") + " looking at " + s.look.ToString("F2") + ", fov " + s.fov);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(camGo); }
        var renderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
        report.AppendLine();
        report.AppendLine("Renderers: " + renderers.Length);
        foreach (var r in renderers.OrderBy(r => r.name))
            report.AppendLine("- " + r.name + ": " + string.Join(", ", r.sharedMaterials.Select(m => m == null ? "MISSING MATERIAL" : m.name + " / " + m.shader.name)));
        File.WriteAllText(Path.Combine(ReviewDir, "report.md"), report.ToString());
        Debug.Log("[Liminal] Stop 01 renders written to " + ReviewDir);
    }

    static void RenderTo(Camera cam, int w, int h, string file)
    {
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 8 };
        var prev = RenderTexture.active;
        try
        {
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            File.WriteAllBytes(file, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        finally { cam.targetTexture = null; RenderTexture.active = prev; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
    }

    public static void BatchBuildAndRender()
    {
        try { Build(); Render(); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
