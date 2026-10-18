// Claude, 2026-10-18. Liminal Doors desktop test, modelled on Astra's Infinite Pole (AstraReleaseCheck.DesktopTest and
// AstraDesktopTest.Prepare): SDK "Build & Test" with one desktop client, no VR, nothing uploaded.
// Queued from LiminalAgentQueue ("desktop-test"); also on the menu. Progress: Review/desktop-test-status.txt.
// Proof that it really ran is in the client log, not here: the newest
// %USERPROFILE%\AppData\LocalLow\VRChat\VRChat\output_log_*.txt must show this bundle in --url= and "Finished entering world".
// Only compiled once the VRChat SDK has set its UDON define: on a brand-new project the SDK adds it on first load,
// and until then VRC.SDK3.Editor (which has IVRCSdkWorldBuilderApi) isn't compiled either.
#if UDON
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Core;
using VRC.Editor;
using VRC.SDKBase.Editor;
using VRC.SDKBase.Editor.BuildPipeline;
using VRC.SDK3.Editor;

public static class LiminalDesktopTest
{
    const string ScenePath = "Assets/LiminalDoors/Scenes/Liminal Doors.unity";
    const string Status = "Review/desktop-test-status.txt";
    static double deadline;

    [MenuItem("LiminalDoors/Desktop test (Build & Test, no upload)")]
    public static void Run()
    {
        Directory.CreateDirectory("Review");
        if (SceneManager.GetActiveScene().path != ScenePath) { EditorSceneManager.SaveOpenScenes(); EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); }
        var descriptor = UnityEngine.Object.FindObjectOfType<VRC.SDK3.Components.VRCSceneDescriptor>();
        if (descriptor == null) { Write("FAILED: no VRCSceneDescriptor (VRCWorld) in " + ScenePath); return; }
        if (descriptor.GetComponent<PipelineManager>() == null) descriptor.gameObject.AddComponent<PipelineManager>();
        VRCSettings.ForceNoVR = true;
        VRCSettings.NumClients = 1;
        EditorSceneManager.MarkSceneDirty(descriptor.gameObject.scene);
        EditorSceneManager.SaveScene(descriptor.gameObject.scene);
        EditorApplication.ExecuteMenuItem("VRChat SDK/Show Control Panel");
        deadline = EditorApplication.timeSinceStartup + 120;
        EditorApplication.update -= WaitForSdk; EditorApplication.update += WaitForSdk;
        Write("Waiting for the SDK builder (signed in: " + (APIUser.CurrentUser != null ? APIUser.CurrentUser.displayName : "no") + ") since " + DateTime.Now.ToString("HH:mm:ss"));
    }

    static void WaitForSdk()
    {
        if (EditorApplication.timeSinceStartup > deadline)
        {
            EditorApplication.update -= WaitForSdk;
            Write("NOT STARTED: the SDK builder was not ready after 120 s" + (APIUser.CurrentUser == null ? " because the VRChat SDK is not signed in. Sign in once in VRChat SDK > Show Control Panel, then queue desktop-test again." : ". Open VRChat SDK > Show Control Panel and check it, then queue desktop-test again."));
            return;
        }
        if (!VRCSdkControlPanel.TryGetBuilder<IVRCSdkWorldBuilderApi>(out var builder)) return;
        EditorApplication.update -= WaitForSdk;
        BuildAndTest(builder);
    }

    static async void BuildAndTest(IVRCSdkWorldBuilderApi builder)
    {
        try
        {
            Write("Building the world bundle and launching one desktop client since " + DateTime.Now.ToString("HH:mm:ss") + "...");
            await builder.BuildAndTest();
            Write("SDK launched the local Liminal Doors test in desktop mode at " + DateTime.Now.ToString("HH:mm:ss") + ". Confirm in the client log (--url= bundle, \"Finished entering world\", no Udon exceptions).");
        }
        catch (Exception e) { Write("FAILED: " + e.Message); Debug.LogException(e); }
    }

    static void Write(string text) { Directory.CreateDirectory("Review"); File.WriteAllText(Status, text + "\n"); }
}
#endif
