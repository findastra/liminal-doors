// Claude, 2026-10-18. Liminal Doors stop 01: the booth's phone, wired the way Astra's Infinite Pole does it
// (ClaudeRound3g / AstraClaudePolish; scripts copied from there with their GUIDs: Assets/Astra/Scripts/AstraPhone*).
// AstraPhoneBooth on the booth's "Phone booth voice zone" (booth 0) tells one AstraPhoneLine (hub 0) who steps in and
// out; while you stand in a booth you hear the people standing in the booths linked to it, at full volume.
// Stop 01 has one booth, so nobody is on the other end yet: the line works once a second booth joins it.
// This file sits outside the LiminalDoors.Editor assembly so it can see Assembly-CSharp (the U# scripts) and UdonSharpEditor.
//
// Menu: LiminalDoors/Stop 01/Wire phone  -> safe to rerun; Review/stop-01-phone/report.md
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LiminalStop01Phone
{
    const string ScenePath = "Assets/LiminalDoors/Scenes/01 Fox - Pink Booth.unity";
    const string StopName = "Stop 01 - Fox - Pink Booth";
    const string ZonePath = "Pink booth (from Astra's Infinite Pole)/Banner booth/Phone booth voice zone";
    const string LineName = "Phone line";
    const string Out = "Review/stop-01-phone";

    [MenuItem("LiminalDoors/Stop 01/Wire phone")]
    public static void Wire()
    {
        if (SceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var stopGo = GameObject.Find(StopName); if (stopGo == null) throw new Exception("No " + StopName + " in the scene.");
        var stop = stopGo.transform;
        var zone = stop.Find(ZonePath); if (zone == null) throw new Exception("No " + ZonePath + " under the stop.");
        var r = new List<string> { "# Stop 01 phone", "Run " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"), "" };

        // rerun: take the old wiring out first (proxy and its backing UdonBehaviour)
        int removed = 0;
        foreach (var old in zone.GetComponents<AstraPhoneBooth>())
        {
            var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(old);
            UnityEngine.Object.DestroyImmediate(old); if (backing != null) UnityEngine.Object.DestroyImmediate(backing); removed++;
        }
        var oldLine = stop.Find(LineName); if (oldLine != null) { UnityEngine.Object.DestroyImmediate(oldLine.gameObject); removed++; }
        if (removed > 0) r.Add("Removed the previous wiring (" + removed + " parts).");

        var lineGo = new GameObject(LineName); lineGo.transform.SetParent(stop, false);
        var line = lineGo.AddUdonSharpComponent<AstraPhoneLine>();
        line.hub = 0;   // this booth is the hub: every later booth talks to it
        UdonSharpEditorUtility.CopyProxyToUdon(line);

        var pb = zone.gameObject.AddUdonSharpComponent<AstraPhoneBooth>();
        pb.line = line; pb.booth = 0;
        UdonSharpEditorUtility.CopyProxyToUdon(pb);

        EditorSceneManager.MarkSceneDirty(stopGo.scene);
        EditorSceneManager.SaveScene(stopGo.scene);

        var trig = zone.GetComponent<BoxCollider>();
        var lineUdon = UdonSharpEditorUtility.GetBackingUdonBehaviour(line);
        var boothUdon = UdonSharpEditorUtility.GetBackingUdonBehaviour(pb);
        r.Add("Phone line: " + StopName + "/" + LineName + ", AstraPhoneLine hub " + line.hub + ", voices near/far " + line.linkedNear + "/" + line.linkedFar + " m; backing UdonBehaviour " + (lineUdon != null ? "present, program " + Name(lineUdon.programSource) : "MISSING"));
        r.Add("Booth 0: " + ZonePath + ", AstraPhoneBooth booth " + pb.booth + ", line " + (pb.line == line ? "linked" : "NOT linked") + "; backing UdonBehaviour " + (boothUdon != null ? "present, program " + Name(boothUdon.programSource) : "MISSING"));
        r.Add("Voice trigger: " + (trig == null ? "MISSING" : (trig.isTrigger ? "trigger" : "NOT a trigger") + " box " + trig.size + " at " + trig.center + ", layer " + LayerMask.LayerToName(zone.gameObject.layer)) + ".");
        r.Add("");
        r.Add("With one booth nobody is on the other end yet: stepping in changes nothing until a second booth (on another stop) joins this line with booth 1, 2, ...");
        Directory.CreateDirectory(Out);
        File.WriteAllText(Path.Combine(Out, "report.md"), string.Join("\n", r) + "\n");
    }

    static string Name(UnityEngine.Object o) => o == null ? "none" : o.name;
}
