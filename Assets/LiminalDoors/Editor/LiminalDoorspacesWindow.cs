// Claude, 2026-10-19. A window for jumping between Liminal Doors' Doorspaces in the editor.
// Astra: "Show me how to navigate between scenes on Liminal Doors, I can only see the first scene", and "I want to see them
// properly labeled on the hierarchy, make it easy for me to navigate".
// Every Doorspace is a root in the one world scene, 3 km from the next, so the Scene view only ever shows the one you are
// near. This window lists them all; Go flies the Scene view to that Doorspace's arrival point, puts its sky up and (with
// Solo on) hides the others in the Scene view only; Door goes to its forward door instead; Show all brings everything back.
// Menu: LiminalDoors/Doorspaces/Window (Ctrl+Shift+D), Next (Ctrl+Shift+]) and Previous (Ctrl+Shift+[).
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LiminalDoorspacesWindow : EditorWindow
{
    Vector2 scroll;
    static bool solo = true;
    static int current = 1;

    [MenuItem("LiminalDoors/Doorspaces/Window (jump between them) %#d", false, 0)]
    public static void Open() { var w = GetWindow<LiminalDoorspacesWindow>("Doorspaces"); w.minSize = new Vector2(340, 320); }

    [MenuItem("LiminalDoors/Doorspaces/Next Doorspace %#]", false, 20)] static void Next() => Step(1);
    [MenuItem("LiminalDoors/Doorspaces/Previous Doorspace %#[", false, 21)] static void Previous() => Step(-1);
    [MenuItem("LiminalDoors/Doorspaces/Show all in the Scene view", false, 22)] static void ShowAllMenu() => ShowAll();

    void OnGUI()
    {
        var all = LiminalDoorspaces.All(SceneManager.GetActiveScene());
        EditorGUILayout.LabelField("Liminal Doors · " + all.Count + " Doorspaces", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Each Doorspace is its own root in the Hierarchy, 3 km from the next, so the Scene view shows only the one near you.\n" +
                                "Go: fly to where you arrive and show its sky. Door: fly to its forward door.\nCtrl+Shift+] and Ctrl+Shift+[ step to the next and previous.", MessageType.None);
        EditorGUILayout.BeginHorizontal();
        solo = EditorGUILayout.ToggleLeft("Solo the one I go to (Scene view only)", solo);
        if (GUILayout.Button("Show all", GUILayout.Width(80))) ShowAll();
        if (GUILayout.Button(new GUIContent("Rebuild", "Rebuild the selected Doorspace from its builder and rewire the doors"), GUILayout.Width(70))) LiminalDoorspaces.RebuildSelected();
        EditorGUILayout.EndHorizontal();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var g in all)
        {
            int n = LiminalDoorspaces.NumberOf(g.name);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(g.name.Replace("Doorspace ", ""), n == current ? EditorStyles.boldLabel : EditorStyles.label);
            if (GUILayout.Button("Go", GUILayout.Width(44))) Go(g, false);
            if (GUILayout.Button("Door", GUILayout.Width(50))) Go(g, true);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        if (all.Count == 0) EditorGUILayout.HelpBox("Open Assets/LiminalDoors/Scenes/Liminal Doors.unity first.", MessageType.Info);
    }

    static void Step(int dir)
    {
        var all = LiminalDoorspaces.All(SceneManager.GetActiveScene());
        if (all.Count == 0) return;
        int i = all.FindIndex(g => LiminalDoorspaces.NumberOf(g.name) == current);
        Go(all[((i < 0 ? 0 : i + dir) % all.Count + all.Count) % all.Count], false);
    }

    public static void Go(GameObject root, bool toDoor)
    {
        current = LiminalDoorspaces.NumberOf(root.name);
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        if (solo) { SceneVisibilityManager.instance.ExitIsolation(); SceneVisibilityManager.instance.Isolate(root, true); }
        var sky = SkyOf(root, current);
        if (sky != null && RenderSettings.skybox != sky) RenderSettings.skybox = sky;
        var at = root.transform.Find(toDoor ? "Return point" : "Arrival point");
        var sv = SceneView.lastActiveSceneView;
        if (sv == null && SceneView.sceneViews.Count > 0) sv = (SceneView)SceneView.sceneViews[0];
        if (sv != null && at != null)
        {
            var rot = toDoor ? Quaternion.LookRotation(-at.forward) : at.rotation;   // at the door, look at it
            sv.LookAt(at.position + Vector3.up * 1.6f + rot * Vector3.forward * 4f, rot, 4f, false, true);
            sv.Repaint();
        }
        foreach (var w in Resources.FindObjectsOfTypeAll<LiminalDoorspacesWindow>()) w.Repaint();
    }

    static void ShowAll()
    {
        SceneVisibilityManager.instance.ExitIsolation();
        SceneVisibilityManager.instance.ShowAll();
        var home = SkyOf(null, 1); if (home != null) RenderSettings.skybox = home;
    }

    // the sky each Doorspace's door switches on: stop 01's is the one the door travel already holds, the rest by name
    static Material SkyOf(GameObject root, int n)
    {
        if (n == 1)
        {
            var travel = GameObject.Find("World - door travel");
            var t = travel == null ? null : travel.GetComponents<MonoBehaviour>().FirstOrDefault(c => c != null && c.GetType().Name == "LiminalTraveler");
            var skies = t == null ? null : t.GetType().GetField("skies")?.GetValue(t) as Material[];
            return skies != null && skies.Length > 0 ? skies[0] : null;
        }
        return AssetDatabase.LoadAssetAtPath<Material>("Assets/LiminalDoors/Materials/Stop " + n.ToString("00") + " Sky.mat")
            ?? AssetDatabase.LoadAssetAtPath<Material>(Doorspace.SkyPath(n));
    }
}
