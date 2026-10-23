// The glass animals: one little cut-crystal animal hides behind every door. Claude, 2026-10-23.
// glass-carver (Unity Unicorn, scripts/glass-carver/) turns a real animal scan into <animal>-<date>.fbx; those land in
// Assets/LiminalDoors/GlassAnimals/. This places each one at its Doorspace's hiding spot (the "<Animal> hiding spot"
// group LiminalDoorspaceKit made), gives it the LiminalDoors/Crystal material, and sets it turning slowly on an
// Animator (one turn a minute) so its facets catch the Doorspace's sun as you watch.
//   LiminalDoors > Glass animals > Place all        every animal that has an FBX; the others are listed as missing
//   LiminalDoors > Glass animals > Render checks    one picture per placed animal into Review/glass-animals/
// Both run from the agent queue (menu LiminalDoors/Glass animals/...) and from batchmode:
//   -executeMethod LiminalGlassAnimals.PlaceAllBatch
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LiminalGlassAnimals
{
    const string Dir = "Assets/LiminalDoors/GlassAnimals";
    const string MatPath = Dir + "/Materials/crystal-20261023.mat";
    const string ClipPath = Dir + "/Materials/slow-turn-20261023.anim";
    const string ControllerPath = Dir + "/Materials/slow-turn-20261023.controller";
    const string ScenePath = "Assets/LiminalDoors/Scenes/Liminal Doors.unity";

    [MenuItem("LiminalDoors/Glass animals/Place all")]
    public static void PlaceAll()
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
        var mat = CrystalMaterial();
        var controller = SlowTurn();
        var placed = 0;
        var missing = new System.Collections.Generic.List<string>();
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects().OrderBy(r => r.name))
        {
            if (!root.name.StartsWith("Doorspace ")) continue;
            var spot = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Contains("hiding spot"));
            if (spot == null) continue;
            var animal = spot.name.Substring(0, spot.name.IndexOf(" hiding spot"));
            var fbx = LatestFbx(animal);
            if (fbx == null) { missing.Add(animal); continue; }
            var old = spot.Find("Glass " + animal);
            if (old) Object.DestroyImmediate(old.gameObject);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            go.name = "Glass " + animal;
            go.transform.SetParent(spot, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0, 180, 0);   // faces the way you arrive
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.sharedMaterials = r.sharedMaterials.Select(_ => mat).ToArray();
            var anim = go.GetComponent<Animator>();
            if (anim == null) anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.cullingMode = AnimatorCullingMode.CullCompletely;
            placed++;
        }
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("LiminalGlassAnimals: placed " + placed + "; no FBX yet for: " + (missing.Count == 0 ? "none" : string.Join(", ", missing)));
    }

    public static void PlaceAllBatch()
    {
        PlaceAll();
        RenderChecks();
        AssetDatabase.SaveAssets();
    }

    static GameObject LatestFbx(string animal)
    {
        var slug = animal.ToLowerInvariant().Replace(" ", "-");
        if (!Directory.Exists(Dir)) return null;
        var path = Directory.GetFiles(Dir, slug + "-*.fbx").OrderBy(p => p).LastOrDefault();
        return path == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace('\\', '/'));
    }

    static Material CrystalMaterial()
    {
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/LiminalDoors", "GlassAnimals");
        if (!AssetDatabase.IsValidFolder(Dir + "/Materials")) AssetDatabase.CreateFolder(Dir, "Materials");
        var m = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (m == null) { m = new Material(Shader.Find("LiminalDoors/Crystal")); AssetDatabase.CreateAsset(m, MatPath); }
        m.shader = Shader.Find("LiminalDoors/Crystal");
        m.SetColor("_Tint", new Color(0.96f, 0.98f, 1f));
        m.SetFloat("_IOR", 1.55f); m.SetFloat("_Dispersion", 0.12f); m.SetFloat("_BodyAlpha", 0.4f);
        m.SetFloat("_Cut", 2.5f); m.SetFloat("_Sparkle", 10f); m.SetFloat("_SparkleSharpness", 40f);
        EditorUtility.SetDirty(m);
        return m;
    }

    // one slow turn a minute, looping, as a clip asset: Animators are allowed in VRChat worlds, scripts are not
    static AnimatorController SlowTurn()
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null)
        {
            clip = new AnimationClip { frameRate = 30 };
            var curve = AnimationCurve.Linear(0, 0, 60, 360);
            clip.SetCurve("", typeof(Transform), "localEulerAnglesRaw.y", curve);
            var s = AnimationUtility.GetAnimationClipSettings(clip); s.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, s);
            AssetDatabase.CreateAsset(clip, ClipPath);
        }
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPathWithClip(ControllerPath, clip);
        return ctrl;
    }

    [MenuItem("LiminalDoors/Glass animals/Render checks")]
    public static void RenderChecks()
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        Directory.CreateDirectory("Review/glass-animals");
        var camGo = new GameObject("glass check cam");
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 40;
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (!root.name.StartsWith("Doorspace ")) continue;
            var spot = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Contains("hiding spot"));
            if (spot == null) continue;
            var animal = spot.Find("Glass " + spot.name.Substring(0, spot.name.IndexOf(" hiding spot")));
            if (animal == null) continue;
            var b = animal.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; });
            cam.transform.position = b.center + new Vector3(0.6f, 0.35f, -1.2f) * Mathf.Max(0.3f, b.size.magnitude * 1.4f);
            cam.transform.LookAt(b.center);
            var rt = new RenderTexture(1280, 720, 24) { antiAliasing = 4 };
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            var nn = root.name.Substring(10, 2);
            File.WriteAllBytes("Review/glass-animals/doorspace-" + nn + "-" + animal.name.Substring(6).ToLowerInvariant().Replace(" ", "-") + "-20261023.png", tex.EncodeToPNG());
            RenderTexture.active = null; cam.targetTexture = null; Object.DestroyImmediate(rt);
        }
        Object.DestroyImmediate(camGo);
    }
}
