// Claude, 2026-10-18. Liminal Doors, stop 02: Hare / Faraway Field (floor register 2026-10-15, scene 2; concept plate 02).
// "Long grass and a distant horizon. A rose door just beyond the rise, with rising motes. Wind moves in broad bands
// across the field. A hare tucked into the grass beside a flat stone." Palette silver green / rose / lavender.
// Everything here is generated (no outside art): the field heightfield and its walkable collider, a worn path, ~120k
// grass blades and wildflowers, the flat stone, two doors and the evening sky. The glass hare is a later pass; its
// hiding spot is marked.
// Fresh base (Astra, 2026-10-18: start fresh, nothing from Infinite Pole but the booth): the sky is
// LiminalDoors/Evening Sky, the doorway light and motes LiminalDoors/Soft Glow, and the bloom is the new Liminal Glow
// profile (Unity's post-processing package; LiminalWorldGlow). All written for this project.
// The field meshes are generated once and then reused, so rebuilding the stop doesn't make a new 27 MB grass asset;
// LiminalDoors/Stop 02/Rebuild field meshes makes them again.
//
// The stop sits in its own slot, 3 km from stop 01, inside the one world scene (LiminalStop01.ScenePath). You arrive
// in front of the pale arrival door (the way back) facing the rise; the rose door beyond it is the forward door.
// Doors are only geometry and named trigger boxes here; LiminalDoorsTravel (LiminalDoors/World/Wire doors) wires them.
//
// Menu: LiminalDoors/Stop 02/Build stop            -> replaces only the Stop 02 root in the world scene
//       LiminalDoors/Stop 02/Rebuild field meshes  -> generates the ground, grass and path again, then builds
//       LiminalDoors/Stop 02/Render review shots   -> Review/stop-02-render/*.png + report.md
// After building a stop, run LiminalDoors/World/Wire doors (Build all does both).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public static class LiminalStop02
{
    public const string StopName = "Stop 02 - Hare - Faraway Field";
    public static readonly Vector3 Origin = new Vector3(3000f, 0f, 0f);
    public const string SkyMaterial = "Assets/LiminalDoors/Materials/Stop 02 Sky.mat";
    const string GenDir = "Assets/LiminalDoors/Stop 02";
    const string GroundMeshPath = GenDir + "/Field ground.asset";
    const string GrassMeshPath = GenDir + "/Field grass.asset";
    const string PathMaskPath = GenDir + "/Field path mask.png";
    const string MatDir = "Assets/LiminalDoors/Materials";
    const string ReviewDir = "Review/stop-02-render";
    const string BuildReport = "Review/stop-02-build/report.md";

    // layout, in stop-local metres: arrival door at the origin, the rise's crest at z 24, the rose door just beyond it
    static readonly Vector2 ArrivalDoorAt = new Vector2(0f, -0.7f);
    static readonly Vector2 ArrivalAt = new Vector2(0f, 1.3f);
    static readonly Vector2 RoseDoorAt = new Vector2(5.5f, 31f);
    static readonly Vector2 StoneAt = new Vector2(-2.4f, 7.5f);
    static readonly Vector2[] PathPts =
    {
        new Vector2(0f, 1.6f), new Vector2(0.8f, 6f), new Vector2(2.6f, 11f), new Vector2(2.2f, 16f),
        new Vector2(3.6f, 21f), new Vector2(5.0f, 26f), new Vector2(5.5f, 30.3f)
    };
    static readonly Vector3 SunDir = new Vector3(0.12f, 0.04f, 1f);   // low, just behind the rose door

    // ---------------------------------------------------------------- terrain

    static float Sq(float v) { return v * v; }
    static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3 - 2 * t); }
    static float Fbm(float x, float z) { return Mathf.PerlinNoise(x + 37.1f, z + 11.7f) * 0.65f + Mathf.PerlinNoise(x * 2.03f + 5.3f, z * 2.03f + 91.2f) * 0.35f; }

    static float RawH(float x, float z)
    {
        float roll = (Fbm(x * 0.022f, z * 0.022f) - 0.5f) * 1.6f * Smooth(0f, 8f, Mathf.Sqrt(x * x + z * z));   // level where you arrive
        float rise = 2.3f * Mathf.Exp(-Sq((z - 24f) / 8f)) * Mathf.Exp(-Sq((x - 4f) / 30f));
        float r = Mathf.Sqrt(x * x + Sq(z - 20f));
        float a = Mathf.Atan2(z - 20f, x);
        float hills = Smooth(110f, 215f, r) * (6f + 11f * Fbm(Mathf.Cos(a) * 2.2f + 3f, Mathf.Sin(a) * 2.2f + 7f));   // the far horizon
        return roll + rise + hills;
    }

    static float h0 = float.NaN;
    public static float H(float x, float z) { if (float.IsNaN(h0)) h0 = RawH(0f, 0f); return RawH(x, z) - h0; }

    static float SegDist(Vector2 p, Vector2 a, Vector2 b) { var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude); return Vector2.Distance(p, a + ab * t); }
    static float PathDist(float x, float z) { var p = new Vector2(x, z); float d = float.MaxValue; for (int i = 0; i + 1 < PathPts.Length; i++) d = Mathf.Min(d, SegDist(p, PathPts[i], PathPts[i + 1])); return d; }
    static float PathWidth(float x, float z) { return 0.42f + 0.14f * Mathf.PerlinNoise(x * 0.4f + 3f, z * 0.4f); }

    static Mesh BuildGround(out int verts)
    {
        const float half = 200f, step = 1.6f;
        int n = Mathf.RoundToInt(half * 2f / step) + 1;
        var v = new Vector3[n * n];
        for (int j = 0; j < n; j++) for (int i = 0; i < n; i++) { float x = -half + i * step, z = -half + j * step; v[i + j * n] = new Vector3(x, H(x, z), z); }
        var t = new int[(n - 1) * (n - 1) * 6]; int k = 0;
        for (int j = 0; j < n - 1; j++) for (int i = 0; i < n - 1; i++)
            {
                int a = i + j * n, b = a + 1, c = a + n, d = c + 1;
                t[k++] = a; t[k++] = c; t[k++] = b; t[k++] = b; t[k++] = c; t[k++] = d;
            }
        var m = new Mesh { name = "Field ground", indexFormat = IndexFormat.UInt32 };
        m.vertices = v; m.triangles = t; m.RecalculateNormals(); m.RecalculateBounds();
        verts = v.Length;
        return m;
    }

    static Texture2D BuildPathMask()
    {
        const int N = 512; const float x0 = -32f, z0 = -8f, size = 64f;
        var tex = new Texture2D(N, N, TextureFormat.RGB24, false, true);
        var px = new Color32[N * N];
        for (int j = 0; j < N; j++) for (int i = 0; i < N; i++)
            {
                float x = x0 + (i + 0.5f) * size / N, z = z0 + (j + 0.5f) * size / N, w = PathWidth(x, z);
                byte b = (byte)Mathf.RoundToInt(255f * (1f - Smooth(w - 0.18f, w + 0.12f, PathDist(x, z))));
                px[i + j * N] = new Color32(b, b, b, 255);
            }
        tex.SetPixels32(px); tex.Apply();
        File.WriteAllBytes(PathMaskPath, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(PathMaskPath, ImportAssetOptions.ForceUpdate);
        var ti = (TextureImporter)AssetImporter.GetAtPath(PathMaskPath);
        ti.sRGBTexture = false; ti.wrapMode = TextureWrapMode.Clamp; ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(PathMaskPath);
    }

    // ---------------------------------------------------------------- grass and wildflowers

    class Tile { public List<Vector3> v = new List<Vector3>(); public List<Color> c = new List<Color>(); public List<Vector2> uv = new List<Vector2>(); }

    static List<Mesh> BuildGrass(out int blades, out int flowers)
    {
        var rnd = new System.Random(20261018);
        Func<float> R = () => (float)rnd.NextDouble();
        var tiles = new Dictionary<Vector2Int, Tile>();
        var center = new Vector2(3f, 15f);
        const float cell = 0.2f, dense = 26f, outer = 72f;
        blades = 0; flowers = 0;
        Color baseA = new Color(0.09f, 0.15f, 0.12f), baseB = new Color(0.14f, 0.21f, 0.16f);
        Color tipA = new Color(0.50f, 0.62f, 0.52f), tipB = new Color(0.67f, 0.75f, 0.65f), rose = new Color(0.80f, 0.62f, 0.67f);
        Color white = new Color(0.95f, 0.94f, 0.97f), lavender = new Color(0.63f, 0.50f, 0.87f);
        for (float gz = center.y - outer; gz < center.y + outer; gz += cell)
            for (float gx = center.x - outer; gx < center.x + outer; gx += cell)
            {
                float x = gx + R() * cell, z = gz + R() * cell;
                float r = Vector2.Distance(new Vector2(x, z), center);
                if (r > outer) continue;
                float perM2 = Mathf.Lerp(22f, 5f, Smooth(dense, dense + 10f, r)) * (1f - Smooth(outer - 12f, outer, r));
                if (R() > perM2 * cell * cell) continue;
                float pd = PathDist(x, z), pw = PathWidth(x, z);
                if (pd < pw + 0.05f) continue;
                if (Vector2.Distance(new Vector2(x, z), RoseDoorAt) < 0.8f || Vector2.Distance(new Vector2(x, z), ArrivalDoorAt) < 0.8f) continue;
                if (Sq((x - StoneAt.x) / 1.55f) + Sq((z - StoneAt.y) / 1.1f) < 1f) continue;

                float hgt = (0.35f + 0.5f * R()) * (0.75f + 0.6f * Fbm(x * 0.08f, z * 0.08f));
                if (pd < pw + 0.6f) hgt *= 0.55f;
                float w = 0.025f + 0.025f * R();
                float kind = R();
                Color tip = Color.Lerp(tipA, tipB, R());
                if (kind < 0.022f) { tip = white; hgt *= 0.7f; flowers++; }
                else if (kind < 0.042f) { tip = lavender; hgt *= 1.1f; w *= 1.5f; flowers++; }
                else if (kind < 0.15f) tip = rose;
                Color bas = Color.Lerp(baseA, baseB, R());

                float ang = R() * Mathf.PI * 2f;
                var side = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * (w * 0.5f);
                float la = R() * Mathf.PI * 2f;
                var lean = new Vector3(Mathf.Cos(la), 0f, Mathf.Sin(la)) * (0.15f * hgt) + new Vector3(0.8f, 0f, 0.6f) * (0.08f * hgt);
                var p = new Vector3(x, H(x, z) - 0.02f, z);

                var key = new Vector2Int(Mathf.FloorToInt(x / 24f), Mathf.FloorToInt(z / 24f));
                if (!tiles.TryGetValue(key, out var t)) tiles[key] = t = new Tile();
                t.v.Add(p - side); t.v.Add(p + side); t.v.Add(p + Vector3.up * hgt + lean);
                t.c.Add(bas.linear); t.c.Add(bas.linear); t.c.Add(tip.linear);
                t.uv.Add(new Vector2(0f, 0f)); t.uv.Add(new Vector2(1f, 0f)); t.uv.Add(new Vector2(0.5f, 1f));
                blades++;
            }
        var meshes = new List<Mesh>();
        foreach (var kv in tiles.OrderBy(k => k.Key.y).ThenBy(k => k.Key.x))
        {
            var t = kv.Value; var m = new Mesh { name = "Field grass " + kv.Key.x + "," + kv.Key.y, indexFormat = t.v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            m.SetVertices(t.v); m.SetColors(t.c); m.SetUVs(0, t.uv);
            m.SetTriangles(Enumerable.Range(0, t.v.Count).ToArray(), 0);
            m.RecalculateBounds();
            var b = m.bounds; b.Expand(new Vector3(0.8f, 0.4f, 0.8f)); m.bounds = b;   // room for the wind bend
            meshes.Add(m);
        }
        return meshes;
    }

    // ---------------------------------------------------------------- build

    [MenuItem("LiminalDoors/Stop 02/Build stop")]
    public static void Build()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != LiminalStop01.ScenePath) scene = EditorSceneManager.OpenScene(LiminalStop01.ScenePath, OpenSceneMode.Single);
        var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == StopName);
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
        Directory.CreateDirectory(GenDir); Directory.CreateDirectory(MatDir);
        var report = new List<string> { "# Stop 02 build", "Built " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " into `" + scene.path + "`", "" };

        // materials
        var sky = Mat(SkyMaterial, "LiminalDoors/Evening Sky");
        sky.SetColor("_Top", new Color(0.38f, 0.30f, 0.68f)); sky.SetColor("_Middle", new Color(0.72f, 0.52f, 0.86f));
        sky.SetColor("_Horizon", new Color(1f, 0.74f, 0.62f)); sky.SetColor("_Ground", new Color(0.60f, 0.48f, 0.66f));
        sky.SetColor("_Streaks", new Color(1f, 0.86f, 0.92f)); sky.SetFloat("_StreakAmount", 0.6f);
        sky.SetColor("_SunColor", new Color(1f, 0.86f, 0.66f)); sky.SetVector("_SunDir", new Vector4(SunDir.x, SunDir.y, SunDir.z, 0));
        bool reuse = File.Exists(GroundMeshPath) && File.Exists(GrassMeshPath) && File.Exists(PathMaskPath);
        var mask = reuse ? AssetDatabase.LoadAssetAtPath<Texture2D>(PathMaskPath) : BuildPathMask();
        var ground = Mat(MatDir + "/Stop 02 Field Ground.mat", "LiminalDoors/Field Ground");
        ground.SetTexture("_PathMask", mask); ground.SetVector("_PathRect", new Vector4(-32f, -8f, 64f, 0f));
        var grass = Mat(MatDir + "/Stop 02 Field Grass.mat", "LiminalDoors/Field Grass");
        foreach (var m in new[] { ground, grass }) { m.SetVector("_SunDir", new Vector4(SunDir.x, SunDir.y, SunDir.z, 0)); }
        var roseFrame = Mat(MatDir + "/Stop 02 Rose Door.mat", "LiminalDoors/Soft Lit");
        roseFrame.SetColor("_Color", new Color(0.93f, 0.40f, 0.58f)); roseFrame.SetColor("_Emission", new Color(0.18f, 0.03f, 0.08f));
        roseFrame.SetVector("_LightDir", new Vector4(-SunDir.x, 0.5f, -SunDir.z, 0));   // lit from the field side, toward you
        var paleFrame = Mat(MatDir + "/Stop 02 Arrival Door.mat", "LiminalDoors/Soft Lit");
        paleFrame.SetColor("_Color", new Color(0.80f, 0.76f, 0.88f)); paleFrame.SetColor("_Emission", new Color(0.05f, 0.04f, 0.06f));
        paleFrame.SetVector("_LightDir", new Vector4(SunDir.x, 0.5f, SunDir.z, 0));
        var stoneMat = Mat(MatDir + "/Stop 02 Flat Stone.mat", "LiminalDoors/Soft Lit");
        // warm grey, lit from above the sun side; a cooler, flatter stone read as a puddle in the first renders
        stoneMat.SetColor("_Color", new Color(0.68f, 0.62f, 0.62f)); stoneMat.SetColor("_Emission", Color.black);
        stoneMat.SetVector("_LightDir", new Vector4(-0.75f, 0.4f, 0.5f, 0)); stoneMat.SetFloat("_Ambient", 0.22f);
        var roseLight = Mat(MatDir + "/Stop 02 Rose Doorway Light.mat", "LiminalDoors/Soft Glow");
        roseLight.SetColor("_Color", new Color(1f, 0.55f, 0.72f) * 1.6f);
        var roseMotes = Mat(MatDir + "/Stop 02 Rose Motes.mat", "LiminalDoors/Soft Glow");
        roseMotes.SetColor("_Color", new Color(1f, 0.62f, 0.78f) * 2.4f);
        var goldLight = Mat(MatDir + "/Stop 02 Way Back Light.mat", "LiminalDoors/Soft Glow");
        goldLight.SetColor("_Color", new Color(1f, 0.80f, 0.45f) * 1.2f);
        var goldMotes = Mat(MatDir + "/Stop 02 Way Back Motes.mat", "LiminalDoors/Soft Glow");
        goldMotes.SetColor("_Color", new Color(1f, 0.82f, 0.5f) * 1.8f);

        // meshes
        Mesh groundMesh; List<Mesh> grassMeshes; int groundVerts, blades, flowers = -1;
        if (reuse)
        {
            groundMesh = AssetDatabase.LoadAssetAtPath<Mesh>(GroundMeshPath); groundVerts = groundMesh.vertexCount;
            grassMeshes = AssetDatabase.LoadAllAssetsAtPath(GrassMeshPath).OfType<Mesh>().OrderBy(m => m.name, StringComparer.Ordinal).ToList();
            blades = grassMeshes.Sum(m => m.vertexCount) / 3;
        }
        else
        {
            groundMesh = BuildGround(out groundVerts);
            Replace(GroundMeshPath, groundMesh);
            grassMeshes = BuildGrass(out blades, out flowers);
            AssetDatabase.DeleteAsset(GrassMeshPath);
            AssetDatabase.CreateAsset(grassMeshes[0], GrassMeshPath);
            foreach (var m in grassMeshes.Skip(1)) AssetDatabase.AddObjectToAsset(m, GrassMeshPath);
            AssetDatabase.SaveAssets();
        }

        // scene objects
        var stop = new GameObject(StopName);
        SceneManager.MoveGameObjectToScene(stop, scene);
        stop.transform.position = Origin;

        var groundGo = new GameObject("Field ground (walkable)");
        groundGo.transform.SetParent(stop.transform, false);
        groundGo.AddComponent<MeshFilter>().sharedMesh = groundMesh;
        groundGo.AddComponent<MeshRenderer>().sharedMaterial = ground;
        groundGo.AddComponent<MeshCollider>().sharedMesh = groundMesh;

        var grassRoot = new GameObject("Long grass and wildflowers");
        grassRoot.transform.SetParent(stop.transform, false);
        foreach (var m in grassMeshes)
        {
            var g = new GameObject(m.name); g.transform.SetParent(grassRoot.transform, false);
            g.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = grass; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
        }

        var stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        UnityEngine.Object.DestroyImmediate(stone.GetComponent<Collider>());
        stone.name = "Flat stone"; stone.transform.SetParent(stop.transform, false);
        stone.transform.localPosition = new Vector3(StoneAt.x, H(StoneAt.x, StoneAt.y) + 0.04f, StoneAt.y);
        stone.transform.localRotation = Quaternion.Euler(9f, 20f, 6f); stone.transform.localScale = new Vector3(1.45f, 0.62f, 1.05f);
        stone.GetComponent<MeshRenderer>().sharedMaterial = stoneMat;

        var hare = new GameObject("Hare hiding spot (glass hare: later pass)");
        hare.transform.SetParent(stop.transform, false);
        hare.transform.localPosition = new Vector3(StoneAt.x + 1.0f, H(StoneAt.x + 1.0f, StoneAt.y + 0.35f) + 0.05f, StoneAt.y + 0.35f);

        // doors: you face the rise when you arrive; the rose door's front looks back down the path at you
        float roseYaw = Mathf.Atan2(RoseDoorAt.x - ArrivalAt.x, RoseDoorAt.y - ArrivalAt.y) * Mathf.Rad2Deg;
        var rose = BuildDoor(stop.transform, "Rose door (forward)", Ground(RoseDoorAt), roseYaw, roseFrame, roseFrame, roseLight, roseMotes, 26, "Forward door trigger");
        var pale = BuildDoor(stop.transform, "Arrival door (the way back)", Ground(ArrivalDoorAt), 180f, paleFrame, paleFrame, goldLight, goldMotes, 10, "Arrival door trigger");

        var arrival = new GameObject("Arrival point"); arrival.transform.SetParent(stop.transform, false);
        arrival.transform.localPosition = Ground(ArrivalAt); arrival.transform.localRotation = Quaternion.Euler(0f, roseYaw, 0f);
        var ret = new GameObject("Return point"); ret.transform.SetParent(stop.transform, false);
        var retAt = rose.transform.localPosition + rose.transform.localRotation * new Vector3(0f, 0f, -1.6f);
        ret.transform.localPosition = new Vector3(retAt.x, H(retAt.x, retAt.z), retAt.z); ret.transform.localRotation = Quaternion.Euler(0f, roseYaw + 180f, 0f);

        LiminalWorldGlow.AddStopVolume(stop.transform);   // the new Liminal Glow profile, only while you're here

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        LiminalWorldGlow.Setup();   // the reference camera that carries the bloom into VRChat

        float crest = Enumerable.Range(0, 60).Select(i => H(4f, 10f + i * 0.5f)).Max();
        report.Add("- Slot: " + Origin + " (3 km from stop 01). Ground: " + groundVerts + " vertices, 400 m square, walkable MeshCollider on the visible ground.");
        report.Add("- Grass: " + blades + " blades in " + grassMeshes.Count + " tiles" + (flowers >= 0 ? " (" + flowers + " white or lavender wildflowers)" : " (reused from the last generation)") + ", none on the path, the doorways or under the stone.");
        report.Add("- Rise: crest " + crest.ToString("0.00") + " m above the arrival spot; rose door base at " + H(RoseDoorAt.x, RoseDoorAt.y).ToString("0.00") + " m, " + Vector2.Distance(ArrivalAt, RoseDoorAt).ToString("0.0") + " m from where you arrive.");
        report.Add("- Arrival point " + arrival.transform.position.ToString("F2") + " facing the rose door; return point " + ret.transform.position.ToString("F2") + " in front of the rose door, facing back down the path.");
        report.Add("- Sky: `" + SkyMaterial + "` (LiminalDoors/Evening Sky); the door system switches it on when you arrive.");
        Directory.CreateDirectory(Path.GetDirectoryName(BuildReport));
        File.WriteAllText(BuildReport, string.Join("\n", report) + "\n");
        Debug.Log("[Liminal] Stop 02 built: " + blades + " grass blades.");
    }

    static Vector3 Ground(Vector2 p) { return new Vector3(p.x, H(p.x, p.y), p.y); }

    static void Replace(string path, Mesh mesh)
    {
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
    }

    static Material Mat(string path, string shaderName)
    {
        var sh = Shader.Find(shaderName);
        if (sh == null) throw new Exception("Shader " + shaderName + " not found (check the console for shader errors).");
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh) { name = Path.GetFileNameWithoutExtension(path) }; AssetDatabase.CreateAsset(m, path); }
        else m.shader = sh;
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    // A free-standing door: two posts, a lintel, a threshold, a leaf swung open toward you, soft light in the doorway,
    // rising motes and a trigger box exactly in the doorway. The front (the side you walk in from) is local -Z.
    static GameObject BuildDoor(Transform parent, string name, Vector3 at, float yaw, Material frame, Material leaf, Material light, Material motes, int moteRate, string trigger)
    {
        var door = new GameObject(name);
        door.transform.SetParent(parent, false);
        door.transform.localPosition = at - new Vector3(0f, 0.03f, 0f);
        door.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Box(door.transform, "Post left", new Vector3(-0.5f, 1.15f, 0f), new Vector3(0.1f, 2.36f, 0.14f), frame);
        Box(door.transform, "Post right", new Vector3(0.5f, 1.15f, 0f), new Vector3(0.1f, 2.36f, 0.14f), frame);
        Box(door.transform, "Lintel", new Vector3(0f, 2.36f, 0f), new Vector3(1.12f, 0.12f, 0.16f), frame);
        Box(door.transform, "Threshold", new Vector3(0f, 0.03f, 0f), new Vector3(1.1f, 0.06f, 0.26f), frame);
        var hinge = new GameObject("Hinge"); hinge.transform.SetParent(door.transform, false);
        hinge.transform.localPosition = new Vector3(-0.45f, 0f, 0f); hinge.transform.localRotation = Quaternion.Euler(0f, 72f, 0f);
        Box(hinge.transform, "Door leaf", new Vector3(0.45f, 1.12f, 0f), new Vector3(0.88f, 2.16f, 0.05f), leaf);
        Box(hinge.transform, "Knob", new Vector3(0.78f, 1.05f, -0.045f), new Vector3(0.05f, 0.05f, 0.05f), frame);

        var glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
        UnityEngine.Object.DestroyImmediate(glow.GetComponent<Collider>());
        glow.name = "Doorway light"; glow.transform.SetParent(door.transform, false);
        glow.transform.localPosition = new Vector3(0f, 1.12f, 0.02f); glow.transform.localScale = new Vector3(1.05f, 2.4f, 1f);
        var gr = glow.GetComponent<MeshRenderer>(); gr.sharedMaterial = light; gr.shadowCastingMode = ShadowCastingMode.Off;

        var m = new GameObject("Rising motes"); m.transform.SetParent(door.transform, false);
        m.transform.localPosition = new Vector3(0f, 0.9f, -0.15f);
        Motes(m, motes, moteRate);

        var t = new GameObject(trigger); t.transform.SetParent(door.transform, false);
        t.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        var bc = t.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(0.84f, 2.05f, 0.36f);
        return door;
    }

    static void Box(Transform parent, string name, Vector3 at, Vector3 size, Material mat)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        UnityEngine.Object.DestroyImmediate(b.GetComponent<Collider>());
        b.name = name; b.transform.SetParent(parent, false);
        b.transform.localPosition = at; b.transform.localScale = size;
        b.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static void Motes(GameObject go, Material mat, int rate)
    {
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true; main.prewarm = true; main.duration = 8f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.05f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, 1f, 1f, 0.7f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        var emission = ps.emission; emission.rateOverTime = rate;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(1.1f, 1.8f, 0.6f);
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f); vel.y = new ParticleSystem.MinMaxCurve(0.12f, 0.3f); vel.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.08f; noise.frequency = 0.4f; noise.scrollSpeed = 0.1f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.7f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat; r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
    }

    [MenuItem("LiminalDoors/Stop 02/Rebuild field meshes")]
    public static void RebuildMeshes()
    {
        foreach (var p in new[] { GroundMeshPath, GrassMeshPath, PathMaskPath }) AssetDatabase.DeleteAsset(p);
        Build();
    }

    // ---------------------------------------------------------------- review renders

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

    [MenuItem("LiminalDoors/Stop 02/Render review shots")]
    public static void RenderShots()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != LiminalStop01.ScenePath) scene = EditorSceneManager.OpenScene(LiminalStop01.ScenePath, OpenSceneMode.Single);
        var stop = scene.GetRootGameObjects().FirstOrDefault(g => g.name == StopName);
        if (stop == null) throw new Exception("No " + StopName + "; run LiminalDoors/Stop 02/Build stop first.");
        var rose = stop.transform.Find("Rose door (forward)");
        var pale = stop.transform.Find("Arrival door (the way back)");
        var arrival = stop.transform.Find("Arrival point");

        // look the way a visitor sees it: only this stop on, with its own sky
        var others = scene.GetRootGameObjects().Where(g => g.name.StartsWith("Stop ") && g != stop && g.activeSelf).ToList();
        var oldSky = RenderSettings.skybox;
        Directory.CreateDirectory(ReviewDir);
        var camGo = new GameObject("_review camera") { hideFlags = HideFlags.DontSave };
        var report = new StringBuilder();
        report.AppendLine("# Liminal Doors stop 02 review renders");
        report.AppendLine("Rendered " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " in Unity " + Application.unityVersion + " (" + SystemInfo.graphicsDeviceType + ", " + PlayerSettings.colorSpace + " colour), other stops hidden, stop 02 sky on.");
        report.AppendLine();
        try
        {
            foreach (var o in others) o.SetActive(false);
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterial);
            foreach (var ps in stop.GetComponentsInChildren<ParticleSystem>()) ps.Simulate(8f, false, true, true);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox; cam.nearClipPlane = 0.05f; cam.farClipPlane = 2500f; cam.allowHDR = true; cam.allowMSAA = false;
            LiminalWorldGlow.Configure(camGo.AddComponent<PostProcessLayer>(), camGo.transform);
            Vector3 eye = arrival.position + Vector3.up * 1.6f, roseC = rose.TransformPoint(0f, 1.2f, 0f), paleC = pale.TransformPoint(0f, 1.2f, 0f);
            Vector3 L(float x, float y, float z) => stop.transform.TransformPoint(x, H(x, z) + y, z);
            var shots = new (string name, Vector3 pos, Vector3 look, float fov)[]
            {
                ("01-arrival-view", eye, roseC + Vector3.up * 0.3f, 55f),
                ("02-rose-door", rose.TransformPoint(0.9f, 1.45f, -3.4f), roseC, 50f),
                ("03-wide-15m", L(-13f, 6f, -2f), L(3f, 1f, 17f), 55f),
                ("04-birds-eye", L(3f, 48f, 8f), L(3f, 0f, 16f), 60f),
                ("05-way-back-door-and-stone", L(1.2f, 1.6f, 11f), paleC - Vector3.up * 0.4f, 58f),
                ("06-over-the-field", L(5.6f, 1.7f, 27f), L(30f, 2f, 140f), 60f),
            };
            foreach (var s in shots)
            {
                camGo.transform.position = s.pos;
                camGo.transform.rotation = Quaternion.LookRotation(s.look - s.pos, Vector3.up);
                cam.fieldOfView = s.fov;
                var file = Path.Combine(ReviewDir, "stop-02-" + s.name + ".png");
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
        Debug.Log("[Liminal] Stop 02 renders written to " + ReviewDir);
    }
}
