// Claude, 2026-10-18. Liminal Doors door system, editor side. Astra, 2026-10-18: "Spawn in the booth, and stepping out
// through the booth door sends you straight to a random door."
// The world is one scene; each stop is a root "Stop NN - <animal> - <place>" in its own slot (3 km apart) with:
//   "Arrival point"          where a forward door lands you (stop 01: inside the booth, where you spawn)
//   "Return point"           where you land when you come back through an arrival door
//   "Forward door trigger"   trigger box: walk in, go somewhere random (stop 01: just outside the booth door)
//   "Arrival door trigger"   trigger box: walk in, go back the way you came (every stop but 01)
// Wire doors puts one LiminalTraveler at the scene root ("World - door travel") and a LiminalDoor on every trigger.
// This file sits outside the LiminalDoors.Editor assembly so it can see the U# scripts and UdonSharpEditor.
//
// Menu: LiminalDoors/World/Make door scripts   -> U# program assets for LiminalTraveler and LiminalDoor, compiled
//       LiminalDoors/World/Wire doors          -> safe to rerun; Review/world-doors/report.md
//       LiminalDoors/World/Build all           -> stop 01 door anchors, stop 02, then the doors, in order
//       LiminalDoors/World/Door trip test (play mode) -> ClientSim walks the trip; Review/world-doors/trip-test.md
// Fresh base (2026-10-18): stop 01's sky is the fresh "Plain Sky", and Build all is stop 01's door anchors, stop 02's Build
// and Wire doors (stop 01 only gets its anchors added, never rebuilt). Only compiled once the VRChat SDK has set its UDON define (a brand-new project compiles once without it).
#if UDON
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UdonSharp;
using UdonSharp.Compiler;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase;

[InitializeOnLoad]
public static class LiminalDoorsTravel
{
    const string ScenePath = "Assets/LiminalDoors/Scenes/Liminal Doors.unity";
    const string TravelName = "World - door travel";
    const string ScriptDir = "Assets/LiminalDoors/Scripts";
    const string StartSky = "Assets/LiminalDoors/Materials/Plain Sky.mat";   // stop 01's sky only if the scene has none (see SkyFor)
    const string Out = "Review/world-doors";
    const float SlotGap = 2600f;   // stops must be farther apart than the camera's far clip (2500 m)

    static LiminalDoorsTravel() { EditorApplication.playModeStateChanged += OnPlayMode; }

    // ---------------------------------------------------------------- U# programs

    [MenuItem("LiminalDoors/World/Make door scripts")]
    public static void MakePrograms()
    {
        var r = new List<string> { "# Door scripts", "Run " + Now(), "" };
        foreach (var name in new[] { "LiminalTraveler", "LiminalDoor" })
        {
            string cs = ScriptDir + "/" + name + ".cs", asset = ScriptDir + "/" + name + ".asset";
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(cs);
            if (script == null) throw new Exception("Missing " + cs);
            var prog = AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(asset);
            if (prog == null)
            {
                prog = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
                prog.sourceCsScript = script;
                AssetDatabase.CreateAsset(prog, asset);
                r.Add("- created `" + asset + "`");
            }
            else r.Add("- `" + asset + "` already there");
        }
        AssetDatabase.SaveAssets();
        UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions { IsEditorBuild = true });
        foreach (var name in new[] { "LiminalTraveler", "LiminalDoor" })
        {
            var prog = AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(ScriptDir + "/" + name + ".asset");
            r.Add("- " + name + ": " + (prog != null && prog.SerializedProgramAsset != null ? "compiled program `" + AssetDatabase.GetAssetPath(prog.SerializedProgramAsset) + "`" : "NOT COMPILED (see the console)"));
        }
        Write("scripts.md", r);
    }

    // ---------------------------------------------------------------- wiring

    [MenuItem("LiminalDoors/World/Wire doors")]
    public static void Wire()
    {
        var scene = OpenWorld();
        var r = new List<string> { "# Door wiring", "Run " + Now() + " in `" + scene.path + "`", "" };
        var stops = Stops(scene);
        if (stops.Count == 0 || !stops[0].name.StartsWith("Stop 01")) throw new Exception("The world has no Stop 01 root.");

        // rerun: take the old wiring out (proxies and their backing UdonBehaviours)
        int removed = 0;
        foreach (var d in Resources.FindObjectsOfTypeAll<LiminalDoor>().Where(d => d != null && d.gameObject.scene == scene).ToList()) { Remove(d); removed++; }
        foreach (var t in Resources.FindObjectsOfTypeAll<LiminalTraveler>().Where(t => t != null && t.gameObject.scene == scene).ToList())
        {
            var go = t.gameObject; Remove(t); if (go != null && go.name == TravelName) UnityEngine.Object.DestroyImmediate(go); removed++;
        }
        if (removed > 0) r.Add("Removed the previous wiring (" + removed + " parts).");

        var travelGo = new GameObject(TravelName);
        SceneManager.MoveGameObjectToScene(travelGo, scene);
        var traveler = travelGo.AddUdonSharpComponent<LiminalTraveler>();
        traveler.stops = stops.ToArray();
        traveler.arrivals = stops.Select(s => Need(s, "Arrival point")).ToArray();
        traveler.returns = stops.Select(s => Need(s, "Return point")).ToArray();
        traveler.skies = stops.Select(SkyFor).ToArray();
        traveler.avoidRecent = 3;
        UdonSharpEditorUtility.CopyProxyToUdon(traveler);

        var problems = new List<string>();
        var triggers = new List<BoxCollider>();
        for (int i = 0; i < stops.Count; i++)
        {
            var s = stops[i];
            var forward = s.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Forward door trigger").ToList();
            var back = s.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Arrival door trigger").ToList();
            if (forward.Count != 1) problems.Add(s.name + ": " + forward.Count + " forward door triggers (want 1)");
            if (i > 0 && back.Count != 1) problems.Add(s.name + ": " + back.Count + " arrival door triggers (want 1)");
            foreach (var t in forward.Concat(back))
            {
                var box = t.GetComponent<BoxCollider>();
                if (box == null || !box.isTrigger) { problems.Add(Path(t) + " has no trigger box"); continue; }
                if (t.gameObject.layer != 0) problems.Add(Path(t) + " is not on the Default layer");
                var door = t.gameObject.AddUdonSharpComponent<LiminalDoor>();
                door.traveler = traveler; door.stop = i; door.back = t.name == "Arrival door trigger";
                UdonSharpEditorUtility.CopyProxyToUdon(door);
                triggers.Add(box);
                r.Add("- " + s.name + ": " + (door.back ? "arrival door (the way back)" : "forward door (random)") + " `" + Path(t) + "`, box " + V(Vector3.Scale(box.size, t.lossyScale)) + " m at " + V(t.TransformPoint(box.center)));
            }
            r.Add("  - arrive at " + V(traveler.arrivals[i].position) + ", come back to " + V(traveler.returns[i].position) + ", sky " + (traveler.skies[i] != null ? traveler.skies[i].name : "unchanged"));
        }

        // checks: nobody lands inside a door (a player is ~0.3 m wide and 1.8 m tall), stops far apart, spawn outside every stop
        Physics.SyncTransforms();
        var desc = UnityEngine.Object.FindObjectsOfType<VRC.SDK3.Components.VRCSceneDescriptor>(true).FirstOrDefault();
        var points = new List<(string, Vector3)>();
        for (int i = 0; i < stops.Count; i++) { points.Add((stops[i].name + " arrival", traveler.arrivals[i].position)); points.Add((stops[i].name + " return", traveler.returns[i].position)); }
        if (desc != null) points.Add(("VRChat spawn", desc.transform.position));
        foreach (var (label, p) in points)
            foreach (var b in triggers)
            {
                var bb = b.bounds; bb.Expand(new Vector3(0.6f, 0f, 0.6f));
                if (bb.Contains(p + Vector3.up * 0.1f) || bb.Contains(p + Vector3.up * 0.9f)) problems.Add(label + " is inside " + Path(b.transform));
            }
        for (int i = 0; i < stops.Count; i++)
            for (int j = i + 1; j < stops.Count; j++)
            {
                float d = Vector3.Distance(traveler.arrivals[i].position, traveler.arrivals[j].position);
                if (d < SlotGap) problems.Add(stops[i].name + " and " + stops[j].name + " are only " + d.ToString("0") + " m apart");
            }
        if (desc == null) problems.Add("no VRCWorld (scene descriptor)");
        else if (stops.Any(s => desc.transform.IsChildOf(s.transform))) problems.Add("VRCWorld sits inside a stop, so switching that stop off would hide it");
        var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(traveler);
        if (backing == null || backing.programSource == null) problems.Add("the traveler has no compiled Udon program");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        r.Insert(3, "Traveler `" + TravelName + "` at the scene root: " + stops.Count + " stops, " + triggers.Count + " doors. Checks: " + (problems.Count == 0 ? "all PASS" : problems.Count + " PROBLEM(S)") + ".");
        r.Insert(4, "");
        if (problems.Count > 0) { r.Add(""); r.Add("## Problems"); r.AddRange(problems.Select(p => "- " + p)); }
        r.Add("");
        r.Add("Stop 01 is the start and is never picked at random. With only stop 02 besides it, the rose door has nowhere new to go, so it leads back to the booth (and the way back starts over) until stop 03 exists.");
        Write("report.md", r);
        if (problems.Count > 0) Debug.LogWarning("[Liminal] Door wiring: " + problems.Count + " problem(s), see " + Out + "/report.md");
    }

    [MenuItem("LiminalDoors/World/Build all")]
    public static void BuildAll()
    {
        var r = new List<string> { "# Build all", "Run " + Now(), "" };
        try
        {
            Step(r, "LiminalDoors/Stop 01/Add door anchors", () => Find(Root("Stop 01"), "Forward door trigger") != null);   // never Stop 01/Build: it would replace the booth being made
            Step(r, "LiminalDoors/Stop 02/Build stop", () => Root("Stop 02") != null);
            Wire();
            r.Add("OK   LiminalDoors/World/Wire doors (see report.md)");
            r.Add(""); r.Add("Done. Scene: `" + SceneManager.GetActiveScene().path + "`");
        }
        catch (Exception e) { r.Add(""); r.Add("STOPPED: " + e.Message); Debug.LogException(e); }
        Write("build-all.md", r);
    }

    static void Step(List<string> r, string menu, Func<bool> ok)
    {
        bool ran = EditorApplication.ExecuteMenuItem(menu);
        bool good = ran && ok();
        r.Add((good ? "OK   " : "FAIL ") + menu);
        if (!good) throw new Exception("stopped at " + menu + (ran ? " (it ran, but its result is missing; see its own Review report and the console)" : " (no such menu item)"));
    }

    // ---------------------------------------------------------------- play-mode trip test (ClientSim)

    const string TestKey = "LiminalDoors.TripTest";
    static int step; static double next, deadline;
    static readonly List<string> log = new List<string>();

    [MenuItem("LiminalDoors/World/Door trip test (play mode)")]
    public static void TripTest()
    {
        OpenWorld();
        EditorSceneManager.SaveOpenScenes();
        var cs = VRC.SDK3.ClientSim.ClientSimSettings.Instance;
        if (cs != null && !cs.enableClientSim) { Write("trip-test.md", new List<string> { "# Door trip test", Now(), "", "NOT RUN: ClientSim is switched off (VRChat SDK > ClientSim settings)." }); return; }
        SessionState.SetBool(TestKey, true);
        Write("trip-test.md", new List<string> { "# Door trip test", "Started " + Now() + "; entering play mode with ClientSim..." });
        EditorApplication.isPlaying = true;
    }

    static void OnPlayMode(PlayModeStateChange s)
    {
        if (s != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(TestKey, false)) return;
        SessionState.EraseBool(TestKey);
        step = 0; log.Clear(); next = EditorApplication.timeSinceStartup + 3; deadline = EditorApplication.timeSinceStartup + 90;
        log.Add("# Door trip test (ClientSim, play mode)");
        log.Add("Run " + Now() + ". Each step moves the simulated local player into a door's trigger box and checks where the traveler put them.");
        log.Add("");
        EditorApplication.update += Tick;
    }

    static int passed, failed;
    static Material startSky;   // the scene's sky as play mode starts, which is stop 01's

    static void Tick()
    {
        if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
        double now = EditorApplication.timeSinceStartup;
        if (now > deadline) { Finish("TIMED OUT at step " + step + "."); return; }
        if (now < next) return;
        var me = Networking.LocalPlayer;
        if (!Utilities.IsValid(me)) { next = now + 0.5; return; }
        var scene = SceneManager.GetActiveScene();
        var stops = Stops(scene);
        if (stops.Count < 2) { Finish("Fewer than two stops in the world."); return; }
        Transform Trig(GameObject s, string n) => s.GetComponentsInChildren<Transform>(true).First(t => t.name == n);
        Transform Pt(GameObject s, string n) => s.transform.Find(n);
        string At() => "on " + string.Join(", ", stops.Where(x => x.activeSelf).Select(x => x.name.Substring(0, 7))) + " at " + V(me.GetPosition());
        bool Near(Vector3 a, Vector3 b) => Vector3.Distance(a, b) < 1.0f;
        void Check(string what, bool ok) { if (ok) passed++; else failed++; log.Add((ok ? "PASS " : "FAIL ") + what + " (" + At() + ")"); }
        void Enter(Transform trigger) { var box = trigger.GetComponent<BoxCollider>(); me.TeleportTo(trigger.TransformPoint(box.center) - Vector3.up * 0.9f, trigger.rotation); }
        var s1 = stops[0]; var s2 = stops[1];
        switch (step)
        {
            case 0:
                passed = failed = 0; startSky = RenderSettings.skybox;
                Check("starts in the pink booth with only stop 01 switched on", s1.activeSelf && stops.Skip(1).All(x => !x.activeSelf) && Near(me.GetPosition(), Pt(s1, "Arrival point").position));
                Enter(Trig(s1, "Forward door trigger")); break;
            case 1:
                Check("stepping out of the booth door lands you at stop 02's arrival point, stop 01 off", s2.activeSelf && !s1.activeSelf && Near(me.GetPosition(), Pt(s2, "Arrival point").position));
                Check("the sky is stop 02's", RenderSettings.skybox != null && RenderSettings.skybox == SkyFor(s2));
                Enter(Trig(s2, "Arrival door trigger")); break;
            case 2:
                Check("stop 02's arrival door takes you back into the booth", s1.activeSelf && !s2.activeSelf && Near(me.GetPosition(), Pt(s1, "Return point").position));
                Check("the sky is stop 01's again", RenderSettings.skybox != null && RenderSettings.skybox == startSky);
                Enter(Trig(s1, "Forward door trigger")); break;
            case 3:
                Check("out through the booth door again: stop 02", s2.activeSelf && Near(me.GetPosition(), Pt(s2, "Arrival point").position));
                Enter(Trig(s2, "Forward door trigger")); break;
            case 4:
                Check(stops.Count == 2 ? "the rose door, with nowhere new yet, leads back to the booth" : "the rose door leads on to another stop",
                      stops.Count == 2 ? s1.activeSelf && Near(me.GetPosition(), Pt(s1, "Return point").position) : !s2.activeSelf);
                Enter(Trig(s1, "Forward door trigger")); break;
            case 5:
                Check("out to stop 02 once more", s2.activeSelf);
                me.Respawn(); break;
            case 6:
                Check("a respawn (falling off) puts you back in the booth with stop 01 on", s1.activeSelf && !s2.activeSelf);
                Finish(null); return;
        }
        step++; next = now + 1.6;   // the traveler ignores doors for 0.75 s after a trip
    }

    static void Finish(string error)
    {
        EditorApplication.update -= Tick;
        log.Insert(2, "Result: " + (error ?? (failed == 0 ? "all " + passed + " checks PASS" : failed + " FAIL, " + passed + " pass")) + ".");
        if (error != null) log.Add(error);
        log.Add("");
        log.Add("Not covered: walking with real VRChat physics and avatars, other players, headsets.");
        Write("trip-test.md", log);
        EditorApplication.isPlaying = false;
    }

    // ---------------------------------------------------------------- helpers

    static Scene OpenWorld()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return scene;
    }

    static List<GameObject> Stops(Scene scene) =>
        scene.GetRootGameObjects().Where(g => Regex.IsMatch(g.name, @"^Stop \d\d - ")).OrderBy(g => g.name, StringComparer.Ordinal).ToList();

    static GameObject Root(string prefix) => SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name.StartsWith(prefix + " - "));

    static Transform Find(GameObject root, string name) => root == null ? null : root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

    static Transform Need(GameObject stop, string child)
    {
        var t = stop.transform.Find(child);
        if (t == null) throw new Exception(stop.name + " has no '" + child + "'.");
        return t;
    }

    static Material SkyFor(GameObject stop)
    {
        if (stop.name.StartsWith("Stop 01"))
        {
            // stop 01 is the start, so its sky is the scene's own (the booth session's sunset sky, 2026-10-18), read when
            // the doors are wired; Stop 01's Build rewires after setting it. Another stop's sky there means a render or a
            // test left it switched, so fall back.
            var s = RenderSettings.skybox;
            if (s != null && (!s.name.StartsWith("Stop ") || s.name.StartsWith("Stop 01"))) return s;
            return AssetDatabase.LoadAssetAtPath<Material>(StartSky);
        }
        return AssetDatabase.LoadAssetAtPath<Material>("Assets/LiminalDoors/Materials/Stop " + stop.name.Substring(5, 2) + " Sky.mat");
    }

    static void Remove(UdonSharpBehaviour b)
    {
        var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(b);
        UnityEngine.Object.DestroyImmediate(b);
        if (backing != null) UnityEngine.Object.DestroyImmediate(backing);
    }

    static string Path(Transform t) { var s = t.name; for (var p = t.parent; p != null; p = p.parent) s = p.name + "/" + s; return s; }
    static string V(Vector3 v) => "(" + v.x.ToString("0.00") + ", " + v.y.ToString("0.00") + ", " + v.z.ToString("0.00") + ")";
    static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz");
    static void Write(string file, List<string> lines) { Directory.CreateDirectory(Out); File.WriteAllText(System.IO.Path.Combine(Out, file), string.Join("\n", lines) + "\n"); }
}
#endif
