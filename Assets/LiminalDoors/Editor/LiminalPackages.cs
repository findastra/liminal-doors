// Claude, 2026-10-18. Adds Unity's own post-processing package (Post Processing Stack v2) to Liminal Doors, so the
// stops can bloom (Astra, 2026-10-18: "add Unity's own post-processing package ... with a new bloom profile").
// It goes through the Package Manager, so Packages/manifest.json and the lock file change the normal way. This file
// has no post-processing code in it, so it compiles before the package exists.
//
// Menu: LiminalDoors/World/Add post-processing package   -> Review/world-glow/package.md
using System;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

public static class LiminalPackages
{
    // The VRChat SDK already pulls this in (com.vrchat.base asks for 3.2.2; Unity 2022.3 resolves 3.4.0). Listing it
    // directly keeps it there for our own code; 3.4.0 is what was already installed, so nothing is downgraded.
    const string Package = "com.unity.postprocessing@3.4.0";
    const string Report = "Review/world-glow/package.md";
    static AddRequest request;

    [MenuItem("LiminalDoors/World/Add post-processing package")]
    public static void AddPostProcessing()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Report));
        File.WriteAllText(Report, "# Post-processing package\nAdding " + Package + " since " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + "...\n");
        request = Client.Add(Package);
        EditorApplication.update -= Poll; EditorApplication.update += Poll;
    }

    static void Poll()
    {
        if (request == null || !request.IsCompleted) return;
        EditorApplication.update -= Poll;
        File.WriteAllText(Report, "# Post-processing package\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + ": " +
            (request.Status == StatusCode.Success ? "added " + request.Result.packageId + " (" + request.Result.source + ")." : "FAILED: " + request.Error?.message) + "\n");
        request = null;
    }
}
