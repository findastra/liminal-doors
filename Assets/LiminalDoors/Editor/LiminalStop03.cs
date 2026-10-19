// Claude, 2026-10-19. Liminal Doors, stop 03: Axolotl / Pool After Closing (floor register 2026-10-15, scene 3; concept
// plate 1, floor 03). Astra: "do the next 2 levels". "An empty tiled pool hall with generous quiet space. Slow caustics
// and sparse bubbles; a shallow walkable pool edge." The concept picture: warm peach tiles, a long turquoise pool, tall
// windows that look straight into deep water, a skylight of rippling violet water overhead, a ladder, and a hot pink
// double door at the far end.
// Everything is generated here, written new for this project: the hall, the pool and its steps, the tiles
// (LiminalDoors/Pool Tile, with caustic light under and over the water), the water (Pool Water), the underwater windows
// and the skylight (Pool Window), two doors with portals that fill their openings (LiminalDoorKit), bubbles and motes.
// The glass axolotl is a later pass; its hiding spot is marked.
//
// The stop sits in its own slot, 6 km from stop 01. You arrive just inside the pale way-back door, facing down the hall;
// the pink double door at the far end is the forward door. LiminalDoorsTravel (World/Wire doors) wires both.
//
// Menu: LiminalDoors/Stop 03/Build stop           -> replaces only the Stop 03 root in the world scene
//       LiminalDoors/Stop 03/Render review shots  -> Review/stop-03-render/*.png + report.md
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using K = LiminalDoorKit;

public static class LiminalStop03
{
    public const string StopName = "Stop 03 - Axolotl - Pool After Closing";
    public static readonly Vector3 Origin = new Vector3(6000f, 0f, 0f);
    public const string SkyMaterial = "Assets/LiminalDoors/Materials/Stop 03 Sky.mat";
    const string MatDir = "Assets/LiminalDoors/Materials/Stop 03";
    const string ReviewDir = "Review/stop-03-render";
    const string BuildReport = "Review/stop-03-build/report.md";

    // the hall, in stop-local metres: floor at y 0, x across (-7..7), z along (0 = the way-back door, 32 = the pink door)
    const float HallX = 7f, HallZ = 32f, HallY = 6f, WallT = 0.3f;
    const float PoolX0 = -4f, PoolX1 = 4.5f, PoolZ0 = 5f, PoolZ1 = 27f, PoolDepth = 1.4f, WaterY = -0.12f;
    static readonly float[] ColumnZ = { 4.5f, 10.5f, 16.5f, 22.5f, 28.5f };
    static readonly Vector2 WayBackOpening = new Vector2(0.9f, 2.24f);   // like stop 02's doors: sill 0.06, lintel 2.30
    static readonly Vector2 PinkOpening = new Vector2(1.9f, 2.6f);       // the double door, from the floor
    static readonly Vector3 LampDir = new Vector3(0.2f, 0.9f, -0.3f);

    [MenuItem("LiminalDoors/Stop 03/Build stop")]
    public static void Build()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != LiminalStop01.ScenePath) scene = EditorSceneManager.OpenScene(LiminalStop01.ScenePath, OpenSceneMode.Single);
        var oldRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == StopName);
        if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);
        Directory.CreateDirectory(MatDir);
        var report = new List<string> { "# Stop 03 build", "Built " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " into `" + scene.path + "`", "" };

        // ---- materials
        var sky = K.Mat(SkyMaterial, "LiminalDoors/Evening Sky");   // seen only past the doors; a late violet evening
        sky.SetColor("_Top", new Color(0.16f, 0.08f, 0.26f)); sky.SetColor("_Middle", new Color(0.36f, 0.18f, 0.44f));
        sky.SetColor("_Horizon", new Color(0.70f, 0.40f, 0.52f)); sky.SetColor("_Ground", new Color(0.30f, 0.16f, 0.30f));
        sky.SetFloat("_StreakAmount", 0.2f); sky.SetColor("_SunColor", new Color(0.4f, 0.25f, 0.3f));
        sky.SetFloat("_StarAmount", 0.6f); sky.SetFloat("_GlintAmount", 0.3f); sky.SetFloat("_NebulaAmount", 0f);

        Material Tile(string name, Color a, Color b, Color grout, float size)
        {
            var m = K.Mat(MatDir + "/" + name + ".mat", "LiminalDoors/Pool Tile");
            m.SetColor("_Color", a); m.SetColor("_Color2", b); m.SetColor("_Grout", grout); m.SetFloat("_TileSize", size);
            m.SetVector("_LightDir", LampDir); m.SetFloat("_WaterLevel", WaterY);
            m.SetFloat("_CausticAbove", 0f); m.SetFloat("_Sheen", 0f);
            return m;
        }
        var wall = Tile("Wall tiles", new Color(0.96f, 0.72f, 0.58f), new Color(0.92f, 0.65f, 0.53f), new Color(0.70f, 0.52f, 0.46f), 0.2f);
        wall.SetColor("_Warm", new Color(0.22f, 0.10f, 0.05f));
        var column = Tile("Column tiles", new Color(0.93f, 0.66f, 0.54f), new Color(0.89f, 0.60f, 0.50f), new Color(0.66f, 0.47f, 0.42f), 0.2f);
        column.SetColor("_Warm", new Color(0.24f, 0.11f, 0.05f));
        var deck = Tile("Deck tiles", new Color(0.97f, 0.80f, 0.72f), new Color(0.93f, 0.73f, 0.67f), new Color(0.74f, 0.56f, 0.52f), 0.3f);
        deck.SetColor("_Warm", Color.black); deck.SetFloat("_Sheen", 0.55f); deck.SetColor("_SheenColor", new Color(1.1f, 0.55f, 0.85f));
        deck.SetFloat("_CausticAbove", 0.12f); deck.SetColor("_CausticColor", new Color(1.2f, 0.9f, 1.1f));
        var pool = Tile("Pool tiles", new Color(0.82f, 0.95f, 0.97f), new Color(0.72f, 0.90f, 0.94f), new Color(0.46f, 0.62f, 0.66f), 0.15f);
        pool.SetColor("_Warm", Color.black); pool.SetColor("_Underwater", new Color(0.25f, 0.85f, 0.92f)); pool.SetColor("_CausticColor", new Color(1.3f, 1.75f, 1.75f));
        var coping = Tile("Coping tiles", new Color(0.99f, 0.93f, 0.87f), new Color(0.96f, 0.89f, 0.83f), new Color(0.78f, 0.66f, 0.60f), 0.3f);
        coping.SetColor("_Warm", Color.black); coping.SetFloat("_Sheen", 0.35f); coping.SetColor("_SheenColor", new Color(1.0f, 0.6f, 0.85f));
        var ceiling = Tile("Ceiling tiles", new Color(0.82f, 0.58f, 0.50f), new Color(0.78f, 0.54f, 0.47f), new Color(0.60f, 0.42f, 0.38f), 0.3f);
        ceiling.SetColor("_Warm", Color.black); ceiling.SetFloat("_CausticAbove", 0.7f); ceiling.SetColor("_CausticColor", new Color(1.0f, 0.62f, 1.15f));
        ceiling.SetVector("_LightDir", new Vector3(0f, -1f, 0f));
        var water = K.Mat(MatDir + "/Pool water.mat", "LiminalDoors/Pool Water");
        water.SetVector("_LightDir", LampDir);
        var window = K.Mat(MatDir + "/Underwater window.mat", "LiminalDoors/Pool Window");
        window.SetFloat("_Mode", 0f); window.SetVector("_Size", new Vector4(4.4f, 3.4f, 0, 0));
        var skylight = K.Mat(MatDir + "/Skylight water.mat", "LiminalDoors/Pool Window");
        skylight.SetFloat("_Mode", 1f); skylight.SetColor("_Top", new Color(1.3f, 0.55f, 1.4f)); skylight.SetColor("_Bottom", new Color(0.45f, 0.18f, 0.65f));
        skylight.SetColor("_Light", new Color(1.6f, 1.1f, 1.9f)); skylight.SetVector("_Size", new Vector4(6f, 10f, 0, 0));
        Material Lit(string name, Color c, Color glow, float ambient = 0.55f)
        {
            var m = K.Mat(MatDir + "/" + name + ".mat", "LiminalDoors/Soft Lit");
            m.SetColor("_Color", c); m.SetColor("_Emission", glow); m.SetVector("_LightDir", LampDir); m.SetFloat("_Ambient", ambient);
            return m;
        }
        var frame = Lit("Window frames", new Color(0.93f, 0.87f, 0.83f), new Color(0.05f, 0.04f, 0.04f));
        var chrome = Lit("Ladder chrome", new Color(0.86f, 0.89f, 0.94f), new Color(0.14f, 0.13f, 0.16f), 0.4f);
        var sconce = Lit("Sconce", new Color(1f, 0.86f, 0.62f), new Color(1.7f, 1.05f, 0.5f));
        var pinkFrame = Lit("Pink door frame", new Color(0.98f, 0.33f, 0.55f), new Color(0.55f, 0.06f, 0.20f));
        var pinkLeaf = Lit("Pink door leaves", new Color(0.95f, 0.38f, 0.58f), new Color(0.14f, 0.01f, 0.05f));
        var pinkPane = Lit("Pink door panes", new Color(0.55f, 0.20f, 0.38f), new Color(0.35f, 0.10f, 0.25f));
        var paleFrame = Lit("Way back door frame", new Color(0.86f, 0.80f, 0.90f), new Color(0.05f, 0.04f, 0.06f));
        var backing = Lit("Doorway backing", new Color(0.12f, 0.05f, 0.12f), Color.black, 0.8f);
        var pinkPortal = K.PortalMat(MatDir + "/Pink door portal.mat", new Color(1.6f, 0.55f, 1.0f), new Color(0.9f, 0.22f, 0.62f), new Color(2.6f, 1.2f, 1.9f), PinkOpening);
        var goldPortal = K.PortalMat(MatDir + "/Way back portal.mat", new Color(1.5f, 1.05f, 0.55f), new Color(0.9f, 0.5f, 0.32f), new Color(2.3f, 1.8f, 1.1f), WayBackOpening);
        var pinkMotes = K.Mat(MatDir + "/Pink door motes.mat", "LiminalDoors/Soft Glow"); pinkMotes.SetColor("_Color", new Color(1f, 0.6f, 0.8f) * 2.4f);
        var goldMotes = K.Mat(MatDir + "/Way back motes.mat", "LiminalDoors/Soft Glow"); goldMotes.SetColor("_Color", new Color(1f, 0.82f, 0.5f) * 1.8f);
        var bubbleMat = K.Mat(MatDir + "/Bubbles.mat", "LiminalDoors/Soft Glow"); bubbleMat.SetColor("_Color", new Color(0.7f, 1.4f, 1.5f)); bubbleMat.SetFloat("_Softness", 0.8f);
        var dustMat = K.Mat(MatDir + "/Hall motes.mat", "LiminalDoors/Soft Glow"); dustMat.SetColor("_Color", new Color(1.2f, 0.9f, 0.8f)); dustMat.SetFloat("_Softness", 2f);

        // ---- the hall
        var stop = new GameObject(StopName);
        SceneManager.MoveGameObjectToScene(stop, scene);
        stop.transform.position = Origin;
        var hall = Child(stop.transform, "Hall");
        var walk = Child(stop.transform, "Walkable floor");

        // deck around the pool (walkable boxes, tops at y 0); the basin's rim walls are separate so nothing overlaps
        float rimT = 0.3f;
        Slab(walk, "Deck near", -HallX, HallX, -0.3f, 0f, 0f, PoolZ0 - rimT, deck);
        Slab(walk, "Deck far", -HallX, HallX, -0.3f, 0f, PoolZ1 + rimT, HallZ, deck);
        Slab(walk, "Deck left", -HallX, PoolX0 - rimT, -0.3f, 0f, PoolZ0 - rimT, PoolZ1 + rimT, deck);
        Slab(walk, "Deck right", PoolX1 + rimT, HallX, -0.3f, 0f, PoolZ0 - rimT, PoolZ1 + rimT, deck);
        // the basin: floor, four rim walls (pool tiles inside) with coping on top, steps down at the near left corner
        float floorY = -PoolDepth;
        Slab(walk, "Pool floor", PoolX0, PoolX1, floorY - 0.3f, floorY, PoolZ0, PoolZ1, pool);
        Slab(walk, "Pool wall left", PoolX0 - rimT, PoolX0, floorY, 0f, PoolZ0 - rimT, PoolZ1 + rimT, pool);
        Slab(walk, "Pool wall right", PoolX1, PoolX1 + rimT, floorY, 0f, PoolZ0 - rimT, PoolZ1 + rimT, pool);
        Slab(walk, "Pool wall near", PoolX0, PoolX1, floorY, 0f, PoolZ0 - rimT, PoolZ0, pool);
        Slab(walk, "Pool wall far", PoolX0, PoolX1, floorY, 0f, PoolZ1, PoolZ1 + rimT, pool);
        var cop = Child(hall, "Coping");
        Slab(cop, "Coping left", PoolX0 - rimT, PoolX0, 0f, 0.05f, PoolZ0 - rimT, PoolZ1 + rimT, coping, false);
        Slab(cop, "Coping right", PoolX1, PoolX1 + rimT, 0f, 0.05f, PoolZ0 - rimT, PoolZ1 + rimT, coping, false);
        Slab(cop, "Coping near", PoolX0, PoolX1, 0f, 0.05f, PoolZ0 - rimT, PoolZ0, coping, false);
        Slab(cop, "Coping far", PoolX0, PoolX1, 0f, 0.05f, PoolZ1, PoolZ1 + rimT, coping, false);
        const int steps = 6; const float stepD = 0.45f;
        for (int k = 1; k < steps; k++)
            Slab(walk, "Pool step " + k, PoolX0, PoolX0 + 2.4f, floorY, -PoolDepth * k / steps, PoolZ0 + stepD * (k - 1), PoolZ0 + stepD * k, pool);

        // walls: tiles inside, openings for the windows (left), the way-back door (near) and the pink door (far)
        var walls = Child(hall, "Walls");
        var windowsZ = new List<(float z0, float z1)>();
        for (int i = 0; i + 1 < ColumnZ.Length; i++) { float c = (ColumnZ[i] + ColumnZ[i + 1]) * 0.5f; windowsZ.Add((c - 2.2f, c + 2.2f)); }
        const float winY0 = 0.9f, winY1 = 4.3f;
        WallAlongZ(walls, "Wall left", -HallX - WallT, -HallX, windowsZ.Select(w => (w.z0, w.z1, winY0, winY1)).ToList(), wall);
        WallAlongZ(walls, "Wall right", HallX, HallX + WallT, new List<(float, float, float, float)>(), wall);
        WallAlongX(walls, "Wall near", -WallT, 0f, new List<(float, float, float, float)> { (-WayBackOpening.x * 0.5f, WayBackOpening.x * 0.5f, 0f, 0.06f + WayBackOpening.y) }, wall);
        WallAlongX(walls, "Wall far", HallZ, HallZ + WallT, new List<(float, float, float, float)> { (-PinkOpening.x * 0.5f, PinkOpening.x * 0.5f, 0f, PinkOpening.y) }, wall);

        // ceiling with the skylight well over the far half of the pool
        const float skyX0 = -3f, skyX1 = 3f, skyZ0 = 14f, skyZ1 = 24f, wellH = 1f;
        var ceil = Child(hall, "Ceiling");
        Slab(ceil, "Ceiling near", -HallX - WallT, HallX + WallT, HallY, HallY + 0.3f, -WallT, skyZ0, ceiling, false);
        Slab(ceil, "Ceiling far", -HallX - WallT, HallX + WallT, HallY, HallY + 0.3f, skyZ1, HallZ + WallT, ceiling, false);
        Slab(ceil, "Ceiling left", -HallX - WallT, skyX0, HallY, HallY + 0.3f, skyZ0, skyZ1, ceiling, false);
        Slab(ceil, "Ceiling right", skyX1, HallX + WallT, HallY, HallY + 0.3f, skyZ0, skyZ1, ceiling, false);
        Slab(ceil, "Skylight well left", skyX0 - 0.2f, skyX0, HallY, HallY + wellH, skyZ0, skyZ1, ceiling, false);
        Slab(ceil, "Skylight well right", skyX1, skyX1 + 0.2f, HallY, HallY + wellH, skyZ0, skyZ1, ceiling, false);
        Slab(ceil, "Skylight well near", skyX0, skyX1, HallY, HallY + wellH, skyZ0 - 0.2f, skyZ0, ceiling, false);
        Slab(ceil, "Skylight well far", skyX0, skyX1, HallY, HallY + wellH, skyZ1, skyZ1 + 0.2f, ceiling, false);
        K.Quad(ceil, "Skylight (water overhead)", new Vector3((skyX0 + skyX1) * 0.5f, HallY + wellH - 0.02f, (skyZ0 + skyZ1) * 0.5f), Quaternion.Euler(-90f, 0f, 0f), new Vector2(skyX1 - skyX0, skyZ1 - skyZ0), skylight);

        // columns with warm sconces
        var cols = Child(hall, "Columns");
        foreach (var z in ColumnZ)
            foreach (var side in new[] { -1f, 1f })
            {
                float x = side * (HallX - 0.3f);
                K.Box(cols, "Column " + (side < 0 ? "L" : "R") + " " + z.ToString("0.0"), new Vector3(x, HallY * 0.5f, z), new Vector3(0.6f, HallY, 0.6f), column, true);
                K.Box(cols, "Sconce " + (side < 0 ? "L" : "R") + " " + z.ToString("0.0"), new Vector3(x - side * 0.36f, 2.7f, z), new Vector3(0.12f, 0.36f, 0.26f), sconce);
            }

        // the underwater windows, framed
        var wins = Child(hall, "Underwater windows");
        foreach (var (z0, z1) in windowsZ)
        {
            float zc = (z0 + z1) * 0.5f, w = z1 - z0, hgt = winY1 - winY0, yc = (winY0 + winY1) * 0.5f, x = -HallX - 0.05f;
            K.Quad(wins, "Window " + zc.ToString("0.0"), new Vector3(x, yc, zc), Quaternion.Euler(0f, -90f, 0f), new Vector2(w, hgt), window);
            K.Box(wins, "Frame top " + zc.ToString("0.0"), new Vector3(x + 0.04f, winY1, zc), new Vector3(0.14f, 0.1f, w + 0.1f), frame);
            K.Box(wins, "Frame sill " + zc.ToString("0.0"), new Vector3(x + 0.06f, winY0, zc), new Vector3(0.2f, 0.1f, w + 0.1f), frame);
            K.Box(wins, "Mullion " + zc.ToString("0.0"), new Vector3(x + 0.04f, yc, zc), new Vector3(0.12f, hgt, 0.08f), frame);
        }

        // the ladder: two chrome rails curving over the coping on the left, three rungs under the water
        var ladder = Child(hall, "Ladder");
        const float ladZ = 12.2f;
        foreach (var dz in new[] { -0.28f, 0.28f })
        {
            Rod(ladder, "Rail down " + dz, new Vector3(PoolX0 + 0.22f, -1.15f, ladZ + dz), new Vector3(PoolX0 + 0.22f, 0.55f, ladZ + dz), 0.025f, chrome);
            Rod(ladder, "Rail bend " + dz, new Vector3(PoolX0 + 0.22f, 0.55f, ladZ + dz), new Vector3(PoolX0 - 0.1f, 0.85f, ladZ + dz), 0.025f, chrome);
            Rod(ladder, "Rail over " + dz, new Vector3(PoolX0 - 0.1f, 0.85f, ladZ + dz), new Vector3(PoolX0 - 0.55f, 0.6f, ladZ + dz), 0.025f, chrome);
            Rod(ladder, "Rail foot " + dz, new Vector3(PoolX0 - 0.55f, 0.6f, ladZ + dz), new Vector3(PoolX0 - 0.6f, 0.04f, ladZ + dz), 0.025f, chrome);
        }
        for (int r = 0; r < 3; r++) Rod(ladder, "Rung " + r, new Vector3(PoolX0 + 0.16f, -0.95f + r * 0.35f, ladZ - 0.28f), new Vector3(PoolX0 + 0.16f, -0.95f + r * 0.35f, ladZ + 0.28f), 0.02f, chrome);

        // the water
        K.Quad(hall, "Pool water", new Vector3((PoolX0 + PoolX1) * 0.5f, WaterY, (PoolZ0 + PoolZ1) * 0.5f), Quaternion.Euler(90f, 0f, 0f), new Vector2(PoolX1 - PoolX0, PoolZ1 - PoolZ0), water);

        // ---- doors
        // the pink double door, open toward you, its portal filling the whole opening; dark backing behind it
        var pink = Child(stop.transform, "Pink double door (forward)");
        pink.localPosition = new Vector3(0f, 0f, HallZ); pink.localRotation = Quaternion.identity;   // front (local -Z) faces down the hall, toward you
        float hw = PinkOpening.x * 0.5f, top = PinkOpening.y;
        K.Box(pink, "Post left", new Vector3(-hw - 0.06f, top * 0.5f, 0f), new Vector3(0.12f, top + 0.12f, WallT + 0.06f), pinkFrame);
        K.Box(pink, "Post right", new Vector3(hw + 0.06f, top * 0.5f, 0f), new Vector3(0.12f, top + 0.12f, WallT + 0.06f), pinkFrame);
        K.Box(pink, "Lintel", new Vector3(0f, top + 0.06f, 0f), new Vector3(PinkOpening.x + 0.24f, 0.12f, WallT + 0.06f), pinkFrame);
        foreach (var s in new[] { -1f, 1f })
        {
            var hinge = new GameObject("Hinge " + (s < 0 ? "left" : "right")).transform; hinge.SetParent(pink, false);
            hinge.localPosition = new Vector3(s * hw, 0f, -WallT * 0.5f); hinge.localRotation = Quaternion.Euler(0f, s * -68f, 0f);
            K.Box(hinge, "Leaf", new Vector3(-s * hw * 0.5f, top * 0.5f - 0.02f, -0.025f), new Vector3(hw - 0.02f, top - 0.06f, 0.05f), pinkLeaf);
            K.Box(hinge, "Pane", new Vector3(-s * hw * 0.5f, top * 0.66f, -0.055f), new Vector3(0.16f, 0.62f, 0.02f), pinkPane);
            K.Box(hinge, "Push bar", new Vector3(-s * (hw - 0.14f), 1.05f, -0.07f), new Vector3(0.05f, 0.05f, 0.05f), chrome);
        }
        K.Box(pink, "Backing", new Vector3(0f, top * 0.5f, WallT + 0.35f), new Vector3(PinkOpening.x + 0.6f, top + 0.4f, 0.1f), backing);
        var pp = K.Portal(pink, 0f, PinkOpening, pinkPortal); pp.transform.localPosition += new Vector3(0f, 0f, -WallT * 0.5f + 0.01f);
        var pm = new GameObject("Rising motes"); pm.transform.SetParent(pink, false); pm.transform.localPosition = new Vector3(0f, 1.0f, -0.45f);
        K.Motes(pm, pinkMotes, 26f, new Vector3(1.9f, 2.0f, 0.6f));
        K.Trigger(pink, "Forward door trigger", new Vector3(0f, 1.15f, -WallT * 0.5f), new Vector3(1.7f, 2.2f, 0.36f));

        // the pale way-back door behind you as you arrive
        var back = Child(stop.transform, "Way back door (the way back)");
        back.localPosition = new Vector3(0f, 0f, -WallT * 0.5f);   // front (local -Z) faces out; you walk into it from the hall side
        float bw = WayBackOpening.x * 0.5f, btop = 0.06f + WayBackOpening.y;
        K.Box(back, "Post left", new Vector3(-bw - 0.05f, btop * 0.5f, 0f), new Vector3(0.1f, btop + 0.1f, WallT + 0.06f), paleFrame);
        K.Box(back, "Post right", new Vector3(bw + 0.05f, btop * 0.5f, 0f), new Vector3(0.1f, btop + 0.1f, WallT + 0.06f), paleFrame);
        K.Box(back, "Lintel", new Vector3(0f, btop + 0.05f, 0f), new Vector3(WayBackOpening.x + 0.2f, 0.1f, WallT + 0.06f), paleFrame);
        K.Box(back, "Threshold", new Vector3(0f, 0.03f, 0f), new Vector3(WayBackOpening.x, 0.06f, WallT + 0.06f), paleFrame);
        K.Box(back, "Backing", new Vector3(0f, btop * 0.5f, -WallT - 0.3f), new Vector3(WayBackOpening.x + 0.5f, btop + 0.4f, 0.1f), backing);
        var bp = K.Portal(back, 0.06f, WayBackOpening, goldPortal); bp.transform.localPosition += new Vector3(0f, 0f, WallT * 0.5f - 0.01f);
        var bm = new GameObject("Rising motes"); bm.transform.SetParent(back, false); bm.transform.localPosition = new Vector3(0f, 0.9f, 0.45f);
        K.Motes(bm, goldMotes, 10f, new Vector3(1.0f, 1.8f, 0.5f));
        K.Trigger(back, "Arrival door trigger", new Vector3(0f, 1.05f, 0f), new Vector3(0.84f, 2.05f, 0.36f));

        // ---- where you land, the axolotl's spot, bubbles and motes
        var arrival = new GameObject("Arrival point"); arrival.transform.SetParent(stop.transform, false);
        arrival.transform.localPosition = new Vector3(0f, 0f, 1.8f); arrival.transform.localRotation = Quaternion.identity;
        var ret = new GameObject("Return point"); ret.transform.SetParent(stop.transform, false);
        ret.transform.localPosition = new Vector3(0f, 0f, HallZ - 1.8f); ret.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var axo = new GameObject("Axolotl hiding spot (glass axolotl: later pass)"); axo.transform.SetParent(stop.transform, false);
        axo.transform.localPosition = new Vector3(PoolX1 - 0.5f, floorY + 0.05f, PoolZ1 - 0.6f);

        var bub = new GameObject("Bubbles"); bub.transform.SetParent(stop.transform, false);
        bub.transform.localPosition = new Vector3((PoolX0 + PoolX1) * 0.5f, floorY + 0.6f, (PoolZ0 + PoolZ1) * 0.5f);
        var bps = K.Motes(bub, bubbleMat, 7f, new Vector3(PoolX1 - PoolX0 - 0.6f, 1.0f, PoolZ1 - PoolZ0 - 0.6f), 0.3f, 0.07f, 120);
        var bmain = bps.main; bmain.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
        var dust = new GameObject("Hall motes"); dust.transform.SetParent(stop.transform, false);
        dust.transform.localPosition = new Vector3(0f, 2.5f, HallZ * 0.5f);
        K.Motes(dust, dustMat, 10f, new Vector3(12f, 4f, 28f), 0.03f, 0.03f, 150);

        LiminalWorldGlow.AddStopVolume(stop.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        int boxes = walk.GetComponentsInChildren<BoxCollider>().Length + walls.GetComponentsInChildren<BoxCollider>().Length + cols.GetComponentsInChildren<BoxCollider>().Length;
        report.Add("- Slot: " + Origin + " (6 km from stop 01). Hall " + (HallX * 2) + " x " + HallZ + " m, " + HallY + " m high; pool " + (PoolX1 - PoolX0) + " x " + (PoolZ1 - PoolZ0) + " m, " + PoolDepth + " m deep, water at " + WaterY + " m, " + (steps - 1) + " steps down at the near left corner.");
        report.Add("- Walkable and solid: " + boxes + " box colliders (deck, pool floor, steps, rim, walls, columns). The water has no collider: you can walk on the pool floor.");
        report.Add("- " + windowsZ.Count + " underwater windows on the left wall, a " + (skyX1 - skyX0) + " x " + (skyZ1 - skyZ0) + " m skylight of rippling violet water, " + (ColumnZ.Length * 2) + " columns with sconces, a ladder.");
        report.Add("- Doors: the pink double door (forward, portal " + PinkOpening.x + " x " + PinkOpening.y + " m) at the far end; the pale way-back door (portal " + WayBackOpening.x + " x " + WayBackOpening.y + " m) behind the arrival point.");
        report.Add("- Arrival point " + arrival.transform.position.ToString("F2") + " facing the pink door; return point " + ret.transform.position.ToString("F2") + " in front of it, facing back.");
        report.Add("- Sky: `" + SkyMaterial + "`, seen only past the doorways.");
        Directory.CreateDirectory(Path.GetDirectoryName(BuildReport));
        File.WriteAllText(BuildReport, string.Join("\n", report) + "\n");
        Debug.Log("[Liminal] Stop 03 built.");
    }

    // ---------------------------------------------------------------- building blocks

    static Transform Child(Transform parent, string name) { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }

    // an axis-aligned box from (x0, y0, z0) to (x1, y1, z1), stop-local
    static GameObject Slab(Transform parent, string name, float x0, float x1, float y0, float y1, float z0, float z1, Material mat, bool solid = true)
    {
        return K.Box(parent, name, new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, y1 - y0, z1 - z0), mat, solid);
    }

    // a wall running along z between x0 and x1, from the floor to the ceiling, with openings (z0, z1, y0, y1)
    static void WallAlongZ(Transform parent, string name, float x0, float x1, List<(float z0, float z1, float y0, float y1)> holes, Material mat)
    {
        var w = Child(parent, name);
        float z = -WallT; int n = 0;
        foreach (var h in holes.OrderBy(h => h.z0))
        {
            if (h.z0 > z) Slab(w, "Panel " + n++, x0, x1, 0f, HallY, z, h.z0, mat);
            if (h.y0 > 0f) Slab(w, "Below " + n++, x0, x1, 0f, h.y0, h.z0, h.z1, mat);
            if (h.y1 < HallY) Slab(w, "Above " + n++, x0, x1, h.y1, HallY, h.z0, h.z1, mat);
            z = h.z1;
        }
        Slab(w, "Panel " + n, x0, x1, 0f, HallY, z, HallZ + WallT, mat);
    }

    // a wall running along x between z0 and z1, with openings (x0, x1, y0, y1)
    static void WallAlongX(Transform parent, string name, float z0, float z1, List<(float x0, float x1, float y0, float y1)> holes, Material mat)
    {
        var w = Child(parent, name);
        float x = -HallX; int n = 0;
        foreach (var h in holes.OrderBy(h => h.x0))
        {
            if (h.x0 > x) Slab(w, "Panel " + n++, x, h.x0, 0f, HallY, z0, z1, mat);
            if (h.y0 > 0f) Slab(w, "Below " + n++, h.x0, h.x1, 0f, h.y0, z0, z1, mat);
            if (h.y1 < HallY) Slab(w, "Above " + n++, h.x0, h.x1, h.y1, HallY, z0, z1, mat);
            x = h.x1;
        }
        Slab(w, "Panel " + n, x, HallX, 0f, HallY, z0, z1, mat);
    }

    // a round rod from a to b (stop-local)
    static void Rod(Transform parent, string name, Vector3 a, Vector3 b, float radius, Material mat)
    {
        var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        UnityEngine.Object.DestroyImmediate(c.GetComponent<Collider>());
        c.name = name; c.transform.SetParent(parent, false);
        c.transform.localPosition = (a + b) * 0.5f;
        c.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
        c.transform.localScale = new Vector3(radius * 2f, Vector3.Distance(a, b) * 0.5f, radius * 2f);
        var r = c.GetComponent<MeshRenderer>(); r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
    }

    // ---------------------------------------------------------------- review renders

    [MenuItem("LiminalDoors/Stop 03/Render review shots")]
    public static void RenderShots()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != LiminalStop01.ScenePath) scene = EditorSceneManager.OpenScene(LiminalStop01.ScenePath, OpenSceneMode.Single);
        var stop = scene.GetRootGameObjects().FirstOrDefault(g => g.name == StopName);
        if (stop == null) throw new Exception("No " + StopName + "; run LiminalDoors/Stop 03/Build stop first.");
        var others = scene.GetRootGameObjects().Where(g => g.name.StartsWith("Stop ") && g != stop && g.activeSelf).ToList();
        var oldSky = RenderSettings.skybox;
        Directory.CreateDirectory(ReviewDir);
        var camGo = new GameObject("_review camera") { hideFlags = HideFlags.DontSave };
        var report = new StringBuilder();
        report.AppendLine("# Liminal Doors stop 03 review renders");
        report.AppendLine("Rendered " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " in Unity " + Application.unityVersion + ", other stops hidden, stop 03 sky on.");
        report.AppendLine();
        try
        {
            foreach (var o in others) o.SetActive(false);
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterial);
            foreach (var ps in stop.GetComponentsInChildren<ParticleSystem>()) ps.Simulate(8f, false, true, true);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox; cam.nearClipPlane = 0.05f; cam.farClipPlane = 2500f; cam.allowHDR = true; cam.allowMSAA = false;
            LiminalWorldGlow.Configure(camGo.AddComponent<PostProcessLayer>(), camGo.transform);
            Vector3 L(float x, float y, float z) => stop.transform.TransformPoint(x, y, z);
            var shots = new (string name, Vector3 pos, Vector3 look, float fov)[]
            {
                ("01-arrival-view", L(0f, 1.6f, 1.8f), L(0.3f, 1.2f, 32f), 62f),
                ("02-pool-from-the-steps", L(-5.2f, 1.7f, 3.4f), L(1.5f, -0.8f, 20f), 64f),
                ("03-windows", L(3.2f, 1.7f, 9f), L(-7f, 2.4f, 16f), 66f),
                ("04-skylight", L(-1.2f, 1.6f, 13f), L(0f, 7f, 19.5f), 70f),
                ("05-pink-door", L(1.2f, 1.6f, 27.6f), L(0f, 1.3f, 32f), 58f),
                ("06-wide-from-the-corner", L(6f, 4.6f, 1.2f), L(-1f, -0.8f, 22f), 72f),
                ("07-way-back-door", L(0.4f, 1.6f, 6.5f), L(0f, 1.2f, 0f), 58f),
            };
            foreach (var s in shots)
            {
                camGo.transform.position = s.pos;
                camGo.transform.rotation = Quaternion.LookRotation(s.look - s.pos, Vector3.up);
                cam.fieldOfView = s.fov;
                var file = Path.Combine(ReviewDir, "stop-03-" + s.name + ".png");
                RenderTo(cam, 1920, 1080, file);
                report.AppendLine("- `" + file.Replace('\\', '/') + "`: camera " + s.pos.ToString("F1") + " looking at " + s.look.ToString("F1") + ", fov " + s.fov);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(camGo);
            foreach (var o in others) o.SetActive(true);
            RenderSettings.skybox = oldSky;
        }
        var rs = stop.GetComponentsInChildren<Renderer>(true);
        report.AppendLine();
        report.AppendLine("Renderers under the stop: " + rs.Length + "; materials: " + string.Join(", ", rs.SelectMany(r => r.sharedMaterials).Distinct().Select(m => m == null ? "MISSING" : m.name + " / " + m.shader.name)));
        File.WriteAllText(Path.Combine(ReviewDir, "report.md"), report.ToString());
        Debug.Log("[Liminal] Stop 03 renders written to " + ReviewDir);
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
}
