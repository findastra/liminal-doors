// Claude, 2026-10-19. Liminal Doors' Doorspaces (Astra's name for each scene): their names in the hierarchy, building the
// ones after stop 03 from the concept floors, renaming the first three, and review renders.
// Astra: "go ahead and take your best crack at the rest of the Liminal Doorspaces (what I am calling each scene) I want to
// see them properly labeled on the hierarchy, make it easy for me to navigate", and "lets do the upside down conservatory
// next use the vine-spawn-tool we created in Unity Unicorn".
// Every Doorspace keeps the liminal rule from the same day ("the pool should be 10x longer and the door more glittery"):
// spaces far too long, the same things repeating into a haze, and a glittering door far away at the end.
//
// Each root is "Doorspace NN · Place Animal" (floor 01 is Pinkscape Fox). The builders below make floors 04-33 and 36 from
// the concept register (34, 35 and 37 were removed as copies). Pieces come from Doorspace (LiminalDoorspaceKit.cs).
//
// Menu: LiminalDoors/Doorspaces/Rename stops 01-03 to Doorspaces
//       LiminalDoors/Doorspaces/Build Doorspaces 04-36       -> Review/doorspaces-build/report.md
//       LiminalDoors/Doorspaces/Render review shots          -> Review/doorspaces-render/
//       LiminalDoors/Doorspaces/Window (jump between them)   -> LiminalDoorspacesWindow
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public static partial class LiminalDoorspaces
{
    public static string RootName(int n, string place, string animal) => "Doorspace " + n.ToString("00") + " · " + place + " " + animal;

    static readonly Regex Num = new Regex(@"^(?:Stop|Doorspace) (\d\d)\b");
    public static int NumberOf(string rootName) { var m = Num.Match(rootName ?? ""); return m.Success ? int.Parse(m.Groups[1].Value) : -1; }
    public static bool IsDoorspace(GameObject g) => g != null && g.transform.parent == null && NumberOf(g.name) > 0;

    public static List<GameObject> All(Scene scene) => scene.GetRootGameObjects().Where(IsDoorspace).OrderBy(g => NumberOf(g.name)).ToList();

    // floors 01-03 are built by their own builders (LiminalStop01/02/03); these are the rest of the concept register
    public static readonly (int n, string place, string animal, Action<Doorspace> build)[] Specs =
    {
        (4, "Last Laundrette", "Raccoon", B04), (5, "Apricot Underpass", "Gecko", B05), (6, "Platform Between Trains", "Crane", B06),
        (7, "Velvet Waiting Room", "Moth", B07), (8, "Rain That Misses You", "Frog", B08), (9, "Upside-Down Conservatory", "Hummingbird", B09),
        (10, "Salt Moon", "Ibex", B10), (11, "Motel Without Rooms", "Cat", B11), (12, "Tidal Carpet", "Manta Ray", B12),
        (13, "Vending Machine Clearing", "Hedgehog", B13), (14, "Lavender Quarry", "Lynx", B14), (15, "Empty Aquarium", "Seahorse", B15),
        (16, "Blackwater Footbridge", "Heron", B16), (17, "Soft Staircase", "Dragonfly", B17), (18, "Snow in the Lobby", "Snow Leopard", B18),
        (19, "Pearl Car Park", "Raven", B19), (20, "Room Beneath the Ocean", "Octopus", B20), (21, "Wind Telephone", "Deer", B21),
        (22, "Sleeping Arcade", "Firefly", B22), (23, "Low-Gravity Court", "Kangaroo", B23), (24, "Ribbon Dunes", "Scarab", B24),
        (25, "Frosted Office", "Praying Mantis", B25), (26, "Red Sun Reservoir", "Flamingo", B26), (27, "Room of Curtains", "Jellyfish", B27),
        (28, "Glass Orchard", "Butterfly", B28), (29, "Midnight Food Court", "Red Panda", B29), (30, "Door at Sunrise", "Swan", B30),
        (31, "Steam Between Stars", "Otter", B31), (32, "Sunset Sandbar", "Sandpiper", B32), (33, "Aquaglass Concourse", "Dolphin", B33),
        (36, "Blue-Sky Terminal", "Kingfisher", B36),
    };

    const string ReportPath = "Review/doorspaces-build/report.md";
    const string RenderDir = "Review/doorspaces-render";

    static Scene World()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != LiminalStop01.ScenePath) scene = EditorSceneManager.OpenScene(LiminalStop01.ScenePath, OpenSceneMode.Single);
        return scene;
    }

    // ---------------------------------------------------------------- renaming the first three

    [MenuItem("LiminalDoors/Doorspaces/Rename stops 01-03 to Doorspaces", false, 1)]
    public static void RenameFirstThree()
    {
        var scene = World();
        var names = new Dictionary<int, string> { { 1, LiminalStop01.StopName }, { 2, LiminalStop02.StopName }, { 3, LiminalStop03.StopName } };
        var done = new List<string>();
        foreach (var g in scene.GetRootGameObjects())
        {
            int n = NumberOf(g.name);
            if (names.TryGetValue(n, out var want) && g.name != want) { done.Add(g.name + " -> " + want); g.name = want; }
        }
        SortHierarchy(scene);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText("Review/doorspaces-build/rename.md", "# Renamed to Doorspaces\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + "\n\n" + (done.Count == 0 ? "Nothing to rename.\n" : string.Join("\n", done.Select(x => "- " + x)) + "\n"));
    }

    // ---------------------------------------------------------------- building

    [MenuItem("LiminalDoors/Doorspaces/Build Doorspaces 04-36", false, 2)]
    public static void BuildAll() => Build(Specs.Select(s => s.n).ToArray());

    public static void Build(params int[] numbers)
    {
        var scene = World();
        var report = new List<string> { "# Doorspaces build", "Run " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " in `" + scene.path + "`", "" };
        int ok = 0;
        foreach (var spec in Specs.Where(s => numbers.Contains(s.n)))
        {
            var d = new Doorspace(spec.n, spec.place, spec.animal);
            try
            {
                d.Begin(scene);
                spec.build(d);
                d.Bake();
                d.Finish();
                ok++;
                report.Add("## " + d.Name); report.Add("Slot " + Doorspace.SlotOf(spec.n) + "."); report.AddRange(d.Report); report.Add("");
            }
            catch (Exception e)
            {
                report.Add("## " + d.Name + " FAILED"); report.Add("    " + e.GetType().Name + ": " + e.Message); report.Add("");
                Debug.LogException(e);
            }
        }
        report.Insert(3, "Built " + ok + " of " + numbers.Length + ".");
        report.Insert(4, "");
        SortHierarchy(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        File.WriteAllText(ReportPath, string.Join("\n", report) + "\n");
        Debug.Log("[Liminal] Doorspaces built: " + ok + " of " + numbers.Length);
    }

    // rebuilding one puts its new root last; keep the Hierarchy in Doorspace order (the other roots stay where they are)
    public static void SortHierarchy(Scene scene)
    {
        var roots = scene.GetRootGameObjects().ToList();
        var slots = roots.Select((g, i) => (g, i)).Where(x => IsDoorspace(x.g)).Select(x => x.i).ToList();
        var ordered = roots.Where(IsDoorspace).OrderBy(g => NumberOf(g.name)).ToList();
        for (int i = 0; i < ordered.Count; i++) ordered[i].transform.SetSiblingIndex(slots[i]);
    }

    [MenuItem("LiminalDoors/Doorspaces/Rebuild the selected Doorspace", false, 4)]
    public static void RebuildSelected()
    {
        var g = Selection.activeGameObject;
        int n = g == null ? 0 : NumberOf(g.transform.root.name);
        if (n == 0) { EditorUtility.DisplayDialog("Rebuild a Doorspace", "Select a Doorspace (or anything inside one) in the Hierarchy first.", "OK"); return; }
        if (!Specs.Any(s => s.n == n)) { EditorUtility.DisplayDialog("Rebuild a Doorspace", "Doorspaces 01-03 have their own builders: LiminalDoors > Stop 0" + n + ".", "OK"); return; }
        Rebuild(n);
        var root = Root(SceneManager.GetActiveScene(), n); if (root != null) LiminalDoorspacesWindow.Go(root, false);
    }

    // for agents driving the editor through Review/agent-queue: rebuild, rewire and re-render the numbers in rebuild.txt
    [MenuItem("LiminalDoors/Doorspaces/Rebuild the ones in rebuild.txt", false, 5)]
    public static void RebuildListed()
    {
        const string list = "Review/doorspaces-build/rebuild.txt";
        var numbers = File.Exists(list) ? Regex.Matches(File.ReadAllText(list), @"\d+").Cast<Match>().Select(m => int.Parse(m.Value)).Where(n => Specs.Any(s => s.n == n)).Distinct().ToArray() : new int[0];
        if (numbers.Length == 0) { Debug.LogWarning("[Liminal] Nothing to rebuild: write Doorspace numbers into " + list); return; }
        Rebuild(numbers);
        Render(numbers);
    }

    static void Rebuild(params int[] numbers)
    {
        Build(numbers);
        EditorApplication.ExecuteMenuItem("LiminalDoors/World/Wire doors");   // the old door triggers went with the old roots
    }

    static GameObject Root(Scene scene, int n) => scene.GetRootGameObjects().FirstOrDefault(g => NumberOf(g.name) == n);

    // ---------------------------------------------------------------- review renders: arrival, halfway, the door

    [MenuItem("LiminalDoors/Doorspaces/Render review shots", false, 3)]
    public static void RenderAll() => Render(Specs.Select(s => s.n).ToArray());

    public static void Render(params int[] numbers)
    {
        var scene = World();
        var all = All(scene);
        var oldSky = RenderSettings.skybox;
        var wasActive = all.ToDictionary(g => g, g => g.activeSelf);
        Directory.CreateDirectory(RenderDir);
        var camGo = new GameObject("_review camera") { hideFlags = HideFlags.DontSave };
        var md = new StringBuilder("# Doorspaces review renders\nRendered " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + ", each with only its own Doorspace and sky on.\n\n");
        try
        {
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox; cam.nearClipPlane = 0.05f; cam.farClipPlane = 2500f; cam.allowHDR = true; cam.allowMSAA = false; cam.fieldOfView = 62f;
            LiminalWorldGlow.Configure(camGo.AddComponent<PostProcessLayer>(), camGo.transform);
            foreach (var root in all.Where(g => numbers.Contains(NumberOf(g.name))))
            {
                int n = NumberOf(root.name);
                foreach (var g in all) g.SetActive(g == root);
                var sky = AssetDatabase.LoadAssetAtPath<Material>(Doorspace.SkyPath(n)); if (sky != null) RenderSettings.skybox = sky;
                foreach (var ps in root.GetComponentsInChildren<ParticleSystem>()) ps.Simulate(6f, false, true, true);
                var a = root.transform.Find("Arrival point"); var r = root.transform.Find("Return point");
                if (a == null || r == null) { md.AppendLine("- " + root.name + ": no arrival or return point"); continue; }
                Vector3 eye = a.position + Vector3.up * 1.6f, door = r.position + Vector3.up * 1.3f - r.forward * 1.8f, mid = Vector3.Lerp(eye, r.position + Vector3.up * 1.6f, 0.5f);
                var hw = root.transform.Find("Halfway point");   // set by Doorspaces that bend, so the shot stays inside them
                var shots = new (string tag, Vector3 pos, Vector3 look)[]
                {
                    ("1-arrival", eye, eye + a.forward * 20f),
                    hw != null ? ("2-halfway", hw.position + Vector3.up * 1.6f, hw.position + Vector3.up * 1.6f + hw.forward * 20f) : ("2-halfway", mid, door),
                    ("3-the-door", r.position + Vector3.up * 1.6f + r.forward * 2.6f + r.right * 0.6f, door),
                };
                foreach (var s in shots)
                {
                    camGo.transform.position = s.pos; camGo.transform.rotation = Quaternion.LookRotation(s.look - s.pos, Vector3.up);
                    var file = Path.Combine(RenderDir, "doorspace-" + n.ToString("00") + "-" + s.tag + ".png");
                    RenderTo(cam, 1280, 720, file);
                    md.AppendLine("- `" + file.Replace('\\', '/') + "`");
                }
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(camGo);
            foreach (var kv in wasActive) if (kv.Key != null) kv.Key.SetActive(kv.Value);
            RenderSettings.skybox = oldSky;
        }
        File.WriteAllText(Path.Combine(RenderDir, "report.md"), md.ToString());
    }

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

    // ---------------------------------------------------------------- shared helpers for the builders

    static Color C(float r, float g, float b) => new Color(r, g, b);
    static Color H(float r, float g, float b, float k) => new Color(r * k, g * k, b * k);   // HDR, for glows the bloom picks up

    // a room's two doors in its end walls, and where you land
    static Transform RoomDoors(Doorspace d, float len, DoorLook look)
    {
        d.Door("Way back door", new Vector3(0f, 0f, -Doorspace.WallT / 2f), 180f, DoorLook.WayBack, false, true);
        var fwd = d.Door(look.name + " (forward)", new Vector3(0f, 0f, len + Doorspace.WallT / 2f), 0f, look, true, true);
        d.Points(new Vector3(0f, 0f, 2f), 0f, fwd);
        return fwd;
    }

    // outdoors: the way-back door just behind where you land, the forward door far off, facing you, with a beacon over it
    static Transform FieldDoors(Doorspace d, Vector3 arrive, Vector3 fwdAt, DoorLook look, bool beacon = true)
    {
        d.Door("Way back door", arrive + new Vector3(0f, 0f, -2f), 180f, DoorLook.WayBack, false, false);
        float yaw = Mathf.Atan2(fwdAt.x - arrive.x, fwdAt.z - arrive.z) * Mathf.Rad2Deg;
        var fwd = d.Door(look.name + " (forward)", fwdAt, yaw, look, true, false);
        if (beacon) d.Beacon(fwd, look.veil);
        d.Points(arrive, yaw, fwd);
        return fwd;
    }

    static List<(float, float, float, float)> Windows(float from, float to, float step, float width, float y0, float y1)
        => Doorspace.Every(from, to, step).Select(z => (z - width / 2f, z + width / 2f, y0, y1)).ToList();

    static float Hash(float x, float z) { float s = Mathf.Sin(x * 127.1f + z * 311.7f) * 43758.5453f; return s - Mathf.Floor(s); }
}
