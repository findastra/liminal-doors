// Claude, 2026-10-18. The VRChat SDK's one-time project setup for building: "Setup Layers for VRChat" and "Setup
// Collision Matrix" (the two buttons in VRChat SDK > Show Control Panel > Builder), run without their confirm dialogs
// so the agent queue can do it. Astra said yes on 2026-10-18 for the fresh Liminal Doors project. They change
// ProjectSettings/TagManager.asset (VRChat's layers; custom layers move down) and DynamicsManager.asset (collisions).
// The SDK's UpdateLayers class is looked up by name, so this file compiles whichever SDK assembly holds it.
//
// Menu: LiminalDoors/World/Set up VRChat layers   -> Review/world-layers/report.md
#if UDON
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class LiminalVrchatLayers
{
    const string Report = "Review/world-layers/report.md";

    [MenuItem("LiminalDoors/World/Set up VRChat layers")]
    public static void Setup()
    {
        var t = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } })
            .FirstOrDefault(x => x.Name == "UpdateLayers" && x.GetMethod("SetupEditorLayers", BindingFlags.Public | BindingFlags.Static) != null);
        Directory.CreateDirectory(Path.GetDirectoryName(Report));
        if (t == null) { File.WriteAllText(Report, "FAILED: the SDK's UpdateLayers class was not found.\n"); return; }
        bool Call(string m) => (bool)t.GetMethod(m, BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
        bool layersBefore = Call("AreLayersSetup"), matrixBefore = Call("IsCollisionLayerMatrixSetup");
        var usedBefore = Enumerable.Range(0, 32).Select(i => i + " " + LayerMask.LayerToName(i)).Where(s => s.Length > 3).ToList();
        if (!layersBefore) t.GetMethod("SetupEditorLayers", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
        if (!Call("IsCollisionLayerMatrixSetup")) t.GetMethod("SetupCollisionLayerMatrix", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
        AssetDatabase.SaveAssets();
        bool layersAfter = Call("AreLayersSetup"), matrixAfter = Call("IsCollisionLayerMatrixSetup");
        File.WriteAllText(Report, "# VRChat layers and collision matrix\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " (`" + t.FullName + "`)\n\n" +
            "- Layers set up: " + layersBefore + " -> " + layersAfter + "\n- Collision matrix set up: " + matrixBefore + " -> " + matrixAfter + "\n" +
            "- Layers before: " + string.Join(", ", usedBefore) + "\n- Layers after: " +
            string.Join(", ", Enumerable.Range(0, 32).Select(i => i + " " + LayerMask.LayerToName(i)).Where(s => s.Length > 3)) + "\n");
    }
}
#endif
