// Claude, 2026-10-18. Bloom for Liminal Doors (Astra, 2026-10-18: Unity's own post-processing package "with a new
// bloom profile (not Infinite Pole's)").
// - Profile `Assets/LiminalDoors/Materials/Liminal Glow.asset`: bloom only, so just the HDR glows (door light, motes)
//   bloom and ordinary colours stay as they are. Made once; hand edits are kept.
// - "VRChat reference camera" at the scene root, switched off (VRChat copies its settings onto each player's camera),
//   with a post-processing layer reading volumes on the Default layer; the VRCWorld descriptor points at it.
// - Each stop that wants glow carries its own global volume with this profile under its root, so it is only on while
//   you are at that stop (the door system switches stops).
//
// Menu: LiminalDoors/World/Glow (bloom)   -> Review/world-glow/report.md   (Stop 02's Build calls it too)
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public static class LiminalWorldGlow
{
    public const string ProfilePath = "Assets/LiminalDoors/Materials/Liminal Glow.asset";
    const string CameraName = "VRChat reference camera";
    const string Report = "Review/world-glow/report.md";

    public static PostProcessProfile Profile()
    {
        var p = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(ProfilePath);
        if (p != null) return p;
        p = ScriptableObject.CreateInstance<PostProcessProfile>();
        Directory.CreateDirectory(Path.GetDirectoryName(ProfilePath));
        AssetDatabase.CreateAsset(p, ProfilePath);
        var bloom = p.AddSettings<Bloom>();
        bloom.enabled.Override(true);
        bloom.intensity.Override(1.2f);
        bloom.threshold.Override(1.05f);   // only colours brighter than white bloom
        bloom.softKnee.Override(0.6f);
        bloom.diffusion.Override(5.5f);
        bloom.fastMode.Override(false);
        foreach (var s in p.settings) { s.name = s.GetType().Name; AssetDatabase.AddObjectToAsset(s, p); }
        EditorUtility.SetDirty(p);
        AssetDatabase.SaveAssets();
        return p;
    }

    [MenuItem("LiminalDoors/World/Glow (bloom)")]
    public static void Setup()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != LiminalStop01.ScenePath) scene = EditorSceneManager.OpenScene(LiminalStop01.ScenePath, OpenSceneMode.Single);
        var profile = Profile();
        var camGo = scene.GetRootGameObjects().FirstOrDefault(g => g.name == CameraName);
        if (camGo == null) { camGo = new GameObject(CameraName); SceneManager.MoveGameObjectToScene(camGo, scene); }
        var cam = camGo.GetComponent<Camera>(); if (cam == null) cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f; cam.farClipPlane = 2500f; cam.allowHDR = true;
        var layer = camGo.GetComponent<PostProcessLayer>(); if (layer == null) layer = camGo.AddComponent<PostProcessLayer>();
        Configure(layer, camGo.transform);
        camGo.SetActive(false);

        string linked = "no VRCWorld in the scene";
        var desc = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true)).FirstOrDefault(c => c != null && c.GetType().Name == "VRCSceneDescriptor");
        if (desc != null)
        {
            var so = new SerializedObject(desc);
            var rc = so.FindProperty("ReferenceCamera");
            if (rc != null) { rc.objectReferenceValue = camGo; so.ApplyModifiedPropertiesWithoutUndo(); linked = "VRCWorld's reference camera is set"; }
            else linked = "VRCWorld has no ReferenceCamera field";
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory(Path.GetDirectoryName(Report));
        File.WriteAllText(Report, "# World glow\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + "\n\n- Profile `" + ProfilePath + "`: " +
            string.Join(", ", profile.settings.Select(s => s.GetType().Name + (s is Bloom b ? " (intensity " + b.intensity.value + ", threshold " + b.threshold.value + ")" : ""))) +
            "\n- `" + CameraName + "` at the scene root (off), post-processing layer on the Default layer; " + linked + ".\n");
    }

    // Shared with the review renders: a camera layer that reads the scene's volumes.
    public static void Configure(PostProcessLayer layer, Transform trigger)
    {
        layer.volumeTrigger = trigger;
        layer.volumeLayer = 1;   // Default
        layer.stopNaNPropagation = true;
        layer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
        var res = AssetDatabase.FindAssets("t:PostProcessResources").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
        if (res != null) layer.Init(AssetDatabase.LoadAssetAtPath<PostProcessResources>(res));
    }

    // A global bloom volume for one stop, under its root.
    public static void AddStopVolume(Transform stop)
    {
        var go = new GameObject("Glow (global volume)");
        go.transform.SetParent(stop, false);
        var v = go.AddComponent<PostProcessVolume>();
        v.sharedProfile = Profile(); v.isGlobal = true; v.priority = 10; v.weight = 1;
    }
}
