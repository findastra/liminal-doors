// Claude, 2026-10-19. The Doorspace kit: what every Liminal Doors Doorspace (Astra's name for each scene) is built from.
// Astra: "go ahead and take your best crack at the rest of the Liminal Doorspaces ... I want to see them properly labeled
// on the hierarchy, make it easy for me to navigate", and before that, "they are lacking the liminal (weird) factor".
//
// A Doorspace is one root "Doorspace NN · Place Animal" in the world scene, in its own 3 km slot (a 6-wide grid), with:
//   Arrival point / Return point / a way-back door ("Arrival door trigger") / a forward door ("Forward door trigger"),
//   all glitter paint with portals that fill the opening and glitter halos (LiminalDoorKit), its own sky, haze, effects
//   and a marked hiding spot for its glass animal. LiminalDoorsTravel (World/Wire doors) wires them like stops 01-03.
// Everything still is built from simple pieces under a temporary "Static" group, then Bake() combines them into one mesh
// per material (saved under Assets/LiminalDoors/Doorspaces/Doorspace NN/) and keeps only box colliders, so the world scene
// stays small and each Doorspace is a handful of draw calls.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using K = LiminalDoorKit;

public struct DoorLook
{
    public string name;
    public Color paint, glow, glitter, veil, deep, rim, halo;
    public Vector2 opening;
    public bool twin;

    static Color A(Color c) { c.a = 1f; return c; }

    // a door painted `paint` whose portal glows `veil`; opening is inside the frame (width, height)
    public static DoorLook Make(string name, Color paint, Color veil, Vector2? opening = null, bool twin = false)
    {
        return new DoorLook
        {
            name = name, paint = paint, opening = opening ?? new Vector2(0.9f, 2.24f), twin = twin,
            glow = A(paint * 0.3f), glitter = A(Color.Lerp(paint, Color.white, 0.55f) * 3f),
            veil = A(veil * 1.5f), deep = A(veil * 0.7f), rim = A(Color.Lerp(veil, Color.white, 0.5f) * 2.3f), halo = A(Color.Lerp(veil, Color.white, 0.4f) * 3.2f),
        };
    }

    public static DoorLook WayBack => Make("Way back door", new Color(0.92f, 0.84f, 0.70f), new Color(1f, 0.72f, 0.38f));
}

public sealed class Doorspace
{
    public readonly int N;
    public readonly string Place, Animal;
    public Transform Root, Static, Fx;
    public readonly string MatDir, GenDir;
    public Color Haze = new Color(0.9f, 0.72f, 0.8f);
    public float HazeStart = 20f, HazeEnd = 220f;
    public Vector3 Light = new Vector3(0.3f, 0.8f, -0.4f);
    public Color Backing = new Color(0.1f, 0.05f, 0.1f);   // the panel behind a door set in a wall; pale in glass rooms so it does not show as a dark slab
    public readonly List<string> Report = new List<string>();
    readonly List<(Vector3 c, Vector3 s)> walk = new List<(Vector3, Vector3)>();   // root-local, axis-aligned box colliders
    Transform turned;                                                              // colliders that aren't axis-aligned

    public Doorspace(int n, string place, string animal)
    {
        N = n; Place = place; Animal = animal;
        MatDir = "Assets/LiminalDoors/Materials/Doorspace " + n.ToString("00");
        GenDir = "Assets/LiminalDoors/Doorspaces/Doorspace " + n.ToString("00");
    }

    public string Name => LiminalDoorspaces.RootName(N, Place, Animal);
    public static Vector3 SlotOf(int n) { int i = n - 1; return new Vector3((i % 6) * 3000f, 0f, (i / 6) * 3000f); }
    public static string SkyPath(int n) => "Assets/LiminalDoors/Materials/Doorspace " + n.ToString("00") + "/Doorspace " + n.ToString("00") + " Sky.mat";

    public void Begin(UnityEngine.SceneManagement.Scene scene)
    {
        foreach (var g in scene.GetRootGameObjects().Where(g => LiminalDoorspaces.NumberOf(g.name) == N).ToList()) UnityEngine.Object.DestroyImmediate(g);
        Directory.CreateDirectory(MatDir); Directory.CreateDirectory(GenDir);
        var root = new GameObject(Name);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        Root = root.transform; Root.position = SlotOf(N);
        Static = Group("Static (baked when built)", Root);
        Fx = Group("Effects", Root);
    }

    public static Transform Group(string name, Transform parent) { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }

    // ---------------------------------------------------------------- materials (made once, kept under MatDir)

    Material M(string name, string shader, bool hazy = true)
    {
        var m = K.Mat(MatDir + "/" + name + ".mat", shader);
        if (hazy && m.HasProperty("_HazeEnd")) { m.SetColor("_HazeColor", Haze); m.SetFloat("_HazeStart", HazeStart); m.SetFloat("_HazeEnd", HazeEnd); }
        return m;
    }

    public Material Surface(string name, Color a, Color b, Color grout, float tile, float groutW = 0.06f, float sheen = 0f, Color sheenColor = default, float caustic = 0f, Color causticColor = default, Color warm = default)
    {
        var m = M(name, "LiminalDoors/Pool Tile");
        m.SetColor("_Color", a); m.SetColor("_Color2", b); m.SetColor("_Grout", groutW > 0f ? grout : Color.Lerp(a, b, 0.5f));
        m.SetFloat("_TileSize", tile); m.SetFloat("_GroutWidth", groutW); m.SetVector("_LightDir", Light);
        m.SetColor("_Warm", warm); m.SetFloat("_WaterLevel", -1000f);
        m.SetFloat("_Sheen", sheen); if (sheen > 0f) m.SetColor("_SheenColor", sheenColor);
        m.SetFloat("_CausticAbove", caustic); if (caustic > 0f) m.SetColor("_CausticColor", causticColor);
        return m;
    }

    public Material Lit(string name, Color c, Color glow = default, float ambient = 0.55f, bool hazy = true)
    {
        var m = M(name, "LiminalDoors/Soft Lit", hazy);
        m.SetColor("_Color", c); m.SetColor("_Emission", glow); m.SetVector("_LightDir", Light); m.SetFloat("_Ambient", ambient);
        return m;
    }

    public Material Glow(string name, Color hdr, float softness = 1.6f)
    {
        var m = M(name, "LiminalDoors/Soft Glow", false);
        m.SetColor("_Color", hdr); m.SetFloat("_Softness", softness);
        return m;
    }

    public Material Water(string name, Color shallow, Color reflect, float clarity = 0.6f)
    {
        var m = M(name, "LiminalDoors/Pool Water");
        m.SetColor("_Shallow", shallow); m.SetColor("_Reflect", reflect); m.SetFloat("_Clarity", clarity); m.SetVector("_LightDir", Light);
        return m;
    }

    public Material Pane(string name, Color top, Color bottom, Color light, Vector2 size, float mode = 0f, float bubbles = 1f)
    {
        var m = M(name, "LiminalDoors/Pool Window");
        m.SetColor("_Top", top); m.SetColor("_Bottom", bottom); m.SetColor("_Light", light);
        m.SetVector("_Size", new Vector4(size.x, size.y, 0, 0)); m.SetFloat("_Mode", mode); m.SetFloat("_Bubbles", bubbles);
        return m;
    }

    public Material Glass(string name, Color tint, float opacity = 0.12f, float frost = 0f)
    {
        var m = M(name, "LiminalDoors/Glass");
        m.SetColor("_Tint", tint); m.SetFloat("_Opacity", opacity); m.SetFloat("_Frost", frost);
        return m;
    }

    public Material Glitter(string name, Color paint, Color glow, Color glitter, float density = 0.55f) => K.GlitterMat(MatDir + "/" + name + ".mat", paint, glow, glitter, Light, density);

    public Material Sky(Color top, Color middle, Color horizon, Color ground, Vector3 sunDir, Color sun, float stars = 0f, float glints = 0f, float nebula = 0f, float streaks = 0.4f)
    {
        var m = K.Mat(SkyPath(N), "LiminalDoors/Evening Sky");
        m.SetColor("_Top", top); m.SetColor("_Middle", middle); m.SetColor("_Horizon", horizon); m.SetColor("_Ground", ground);
        m.SetColor("_Streaks", Color.Lerp(horizon, Color.white, 0.5f)); m.SetFloat("_StreakAmount", streaks);
        m.SetVector("_SunDir", new Vector4(sunDir.x, sunDir.y, sunDir.z, 0)); m.SetColor("_SunColor", sun);
        m.SetColor("_StarColor", new Color(1.7f, 1.4f, 2.0f)); m.SetFloat("_StarAmount", stars); m.SetFloat("_GlintAmount", glints);
        m.SetColor("_Nebula", Color.Lerp(top, new Color(0.7f, 0.3f, 0.75f), 0.5f)); m.SetFloat("_NebulaAmount", nebula);
        return m;
    }

    // ---------------------------------------------------------------- still pieces (all stop-local)

    public GameObject Box(string name, Vector3 c, Vector3 size, Material m, bool solid = false, float yaw = 0f, Transform parent = null)
    {
        var b = K.Box(parent ?? Static, name, c, size, m, false);
        if (yaw != 0f) b.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        if (solid) Solid(b.transform, size);
        return b;
    }

    // a collider for a piece: axis-aligned pieces straight under Static share one object, the rest get their own
    void Solid(Transform piece, Vector3 size)
    {
        if (piece.parent == Static && piece.localRotation == Quaternion.identity) { walk.Add((piece.localPosition, size)); return; }
        if (turned == null) turned = Group("Colliders (turned)", Root);
        var c = new GameObject(piece.name + " (collider)"); c.transform.SetParent(turned, false);
        c.transform.position = piece.position; c.transform.rotation = piece.rotation;
        c.AddComponent<BoxCollider>().size = size;
    }

    // a collider only, in some turned frame (e.g. one ring of a curved tunnel)
    public void Collider(string name, Transform frame, Vector3 localCenter, Vector3 size, Quaternion? localRot = null)
    {
        if (turned == null) turned = Group("Colliders (turned)", Root);
        var c = new GameObject(name); c.transform.SetParent(turned, false);
        c.transform.position = frame.TransformPoint(localCenter); c.transform.rotation = frame.rotation * (localRot ?? Quaternion.identity);
        c.AddComponent<BoxCollider>().size = size;
    }

    public GameObject Slab(string name, float x0, float x1, float y0, float y1, float z0, float z1, Material m, bool solid = true)
        => Box(name, new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, y1 - y0, z1 - z0), m, solid);

    public GameObject Quad(string name, Vector3 c, Quaternion rot, Vector2 size, Material m, Transform parent = null) => K.Quad(parent ?? Static, name, c, rot, size, m);

    public GameObject Prim(PrimitiveType type, string name, Vector3 c, Vector3 scale, Material m, Quaternion? rot = null, Transform parent = null)
    {
        var g = GameObject.CreatePrimitive(type);
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        g.name = name; g.transform.SetParent(parent ?? Static, false);
        g.transform.localPosition = c; g.transform.localScale = scale; g.transform.localRotation = rot ?? Quaternion.identity;
        var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        return g;
    }

    // balls use a lighter sphere than Unity's (16 x 10: 187 vertices instead of 515), so crowns, mounds and lamps cost a third
    public GameObject Ball(string name, Vector3 c, Vector3 scale, Material m, Transform parent = null)
    {
        var g = Prim(PrimitiveType.Sphere, name, c, scale, m, null, parent);
        g.GetComponent<MeshFilter>().sharedMesh = LowSphere();
        return g;
    }

    const string LowSpherePath = "Assets/LiminalDoors/Doorspaces/Kit low sphere.asset";
    static Mesh lowSphere;
    static Mesh LowSphere()
    {
        if (lowSphere != null) return lowSphere;
        lowSphere = AssetDatabase.LoadAssetAtPath<Mesh>(LowSpherePath);
        if (lowSphere != null) return lowSphere;
        const int seg = 16, rings = 10;
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int j = 0; j <= rings; j++)
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f, b = j / (float)rings * Mathf.PI;
                var d = new Vector3(Mathf.Sin(b) * Mathf.Cos(a), Mathf.Cos(b), Mathf.Sin(b) * Mathf.Sin(a));
                v.Add(d * 0.5f); n.Add(d); uv.Add(new Vector2(i / (float)seg, 1f - j / (float)rings));
            }
        for (int j = 0; j < rings; j++)
            for (int i = 0; i < seg; i++)
            {
                int a = j * (seg + 1) + i, b = a + seg + 1;
                if (j > 0) { t.Add(a); t.Add(a + 1); t.Add(b); }
                if (j < rings - 1) { t.Add(a + 1); t.Add(b + 1); t.Add(b); }
            }
        lowSphere = new Mesh { name = "Kit low sphere" };
        lowSphere.SetVertices(v); lowSphere.SetNormals(n); lowSphere.SetUVs(0, uv); lowSphere.SetTriangles(t, 0); lowSphere.RecalculateBounds();
        Directory.CreateDirectory(Path.GetDirectoryName(LowSpherePath));
        AssetDatabase.CreateAsset(lowSphere, LowSpherePath);
        return lowSphere;
    }

    // an upright cylinder; solid ones get a box collider round them
    public GameObject Cyl(string name, Vector3 c, float radius, float height, Material m, bool solid = false, Transform parent = null)
    {
        var g = Prim(PrimitiveType.Cylinder, name, c, new Vector3(radius * 2f, height * 0.5f, radius * 2f), m, null, parent);
        if (solid) Solid(g.transform, new Vector3(radius * 2f, height, radius * 2f));
        return g;
    }

    public GameObject Rod(string name, Vector3 a, Vector3 b, float radius, Material m, Transform parent = null)
        => Prim(PrimitiveType.Cylinder, name, (a + b) * 0.5f, new Vector3(radius * 2f, Vector3.Distance(a, b) * 0.5f, radius * 2f), m, Quaternion.FromToRotation(Vector3.up, (b - a).normalized), parent);

    // a flat disc facing `normal`
    public GameObject Disc(string name, Vector3 c, float radius, float thick, Vector3 normal, Material m, Transform parent = null)
        => Prim(PrimitiveType.Cylinder, name, c, new Vector3(radius * 2f, thick * 0.5f, radius * 2f), m, Quaternion.FromToRotation(Vector3.up, normal), parent);

    // walkable ground from a height function, a mesh of its own (kept out of the bake so its collider is exact)
    public GameObject Ground(string name, float x0, float x1, float z0, float z1, float step, Func<float, float, float> h, Material m)
    {
        int nx = Mathf.RoundToInt((x1 - x0) / step) + 1, nz = Mathf.RoundToInt((z1 - z0) / step) + 1;
        var v = new Vector3[nx * nz];
        for (int j = 0; j < nz; j++) for (int i = 0; i < nx; i++) { float x = x0 + i * step, z = z0 + j * step; v[i + j * nx] = new Vector3(x, h(x, z), z); }
        var t = new int[(nx - 1) * (nz - 1) * 6]; int k = 0;
        for (int j = 0; j < nz - 1; j++) for (int i = 0; i < nx - 1; i++)
            { int a = i + j * nx, b = a + 1, c = a + nx, d = c + 1; t[k++] = a; t[k++] = c; t[k++] = b; t[k++] = b; t[k++] = c; t[k++] = d; }
        var mesh = new Mesh { name = Name + " " + name, indexFormat = v.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.vertices = v; mesh.triangles = t; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var path = GenDir + "/Doorspace " + N.ToString("00") + " " + name + ".asset";
        AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(mesh, path);
        var g = new GameObject(name + " (walkable)"); g.transform.SetParent(Root, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        g.AddComponent<MeshCollider>().sharedMesh = mesh;
        Report.Add("- " + name + ": " + v.Length + " vertices, walkable mesh collider.");
        return g;
    }

    // tufts of the Faraway Field's frond grass (its atlas and shader), here in this Doorspace's own colours; `density` is
    // tufts per square metre where `where` says yes. Saved as its own mesh, kept out of the bake (it sways and parts).
    public GameObject Tufts(string name, float x0, float x1, float z0, float z1, Func<float, float, float> density, Func<float, float, float> h,
                            Color root, Color mid, Color tip, float hMin = 0.5f, float hMax = 1.1f, int seed = 1)
    {
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/LiminalDoors/Stop 02/Frond atlas.png");
        var m = M(name, "LiminalDoors/Frond Grass", false);
        m.SetTexture("_MainTex", atlas); m.SetColor("_Root", root); m.SetColor("_Mid", mid); m.SetColor("_Tip", tip);
        m.SetColor("_FadeColor", Haze); m.SetFloat("_FadeStart", HazeStart); m.SetFloat("_FadeEnd", HazeEnd);
        var rnd = new System.Random(seed); Func<float> R = () => (float)rnd.NextDouble();
        var v = new List<Vector3>(); var c = new List<Color>(); var uv = new List<Vector2>(); var rt = new List<Vector3>(); var tri = new List<int>();
        const float cell = 0.3f;
        for (float gz = z0; gz < z1; gz += cell)
            for (float gx = x0; gx < x1; gx += cell)
            {
                float x = gx + R() * cell, z = gz + R() * cell;
                if (R() > density(x, z) * cell * cell) continue;
                float hg = Mathf.Lerp(hMin, hMax, R()), wd = hg * 0.5f * (0.9f + 0.3f * R());
                int kind = (int)(R() * 3.99f); float u0 = kind * 0.25f + 0.004f, u1 = (kind + 1) * 0.25f - 0.004f;
                var tint = Color.Lerp(Color.white, new Color(0.92f, 0.9f, 1f), R()); tint.a = R();
                var p = new Vector3(x, h(x, z) - 0.03f, z); float yaw = R() * Mathf.PI;
                for (int card = 0; card < 2; card++)
                {
                    float a = yaw + card * Mathf.PI * 0.5f; var side = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (wd * 0.5f);
                    int b0 = v.Count;
                    v.Add(p - side); v.Add(p + side); v.Add(p - side + Vector3.up * hg); v.Add(p + side + Vector3.up * hg);
                    uv.Add(new Vector2(u0, 0f)); uv.Add(new Vector2(u1, 0f)); uv.Add(new Vector2(u0, 1f)); uv.Add(new Vector2(u1, 1f));
                    for (int i = 0; i < 4; i++) { c.Add(tint); rt.Add(p); }
                    tri.Add(b0); tri.Add(b0 + 2); tri.Add(b0 + 1); tri.Add(b0 + 1); tri.Add(b0 + 2); tri.Add(b0 + 3);
                }
            }
        var mesh = new Mesh { name = Name + " " + name, indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.SetVertices(v); mesh.SetColors(c); mesh.SetUVs(0, uv); mesh.SetUVs(1, rt); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
        var bb = mesh.bounds; bb.Expand(new Vector3(1.4f, 0.6f, 1.4f)); mesh.bounds = bb;
        var path = GenDir + "/Doorspace " + N.ToString("00") + " " + name + ".asset";
        AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(mesh, path);
        var g = new GameObject(name); g.transform.SetParent(Root, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        Group("Grass push (player)", Root);   // World/Wire doors adds a LiminalGrassPush here, so this grass parts for you too
        Report.Add("- " + name + ": " + (v.Count / 8) + " frond tufts.");
        return g;
    }

    // ---------------------------------------------------------------- rooms

    public const float WallT = 0.3f;

    // a long room along +z: floor (y 0) from z 0 to len, walls at x = +-halfW, the near wall (z 0) and far wall (z len)
    // each with a centred door opening (width, height); holes in the side walls are (z0, z1, y0, y1)
    public void Room(float halfW, float len, float height, Material floor, Material wall, Material ceiling, Vector2 nearDoor, Vector2 farDoor,
                     List<(float z0, float z1, float y0, float y1)> leftHoles = null, List<(float z0, float z1, float y0, float y1)> rightHoles = null)
    {
        Slab("Floor", -halfW, halfW, -0.3f, 0f, 0f, len, floor);
        if (ceiling != null) Slab("Ceiling", -halfW - WallT, halfW + WallT, height, height + 0.3f, -WallT, len + WallT, ceiling, false);
        WallZ("Wall left", -halfW - WallT, -halfW, len, height, leftHoles, wall);
        WallZ("Wall right", halfW, halfW + WallT, len, height, rightHoles, wall);
        WallX("Wall near", -WallT, 0f, halfW, height, nearDoor, wall);
        WallX("Wall far", len, len + WallT, halfW, height, farDoor, wall);
    }

    public void WallZ(string name, float x0, float x1, float len, float height, List<(float z0, float z1, float y0, float y1)> holes, Material m)
    {
        float z = -WallT; int n = 0;
        foreach (var h in (holes ?? new List<(float, float, float, float)>()).OrderBy(h => h.Item1))
        {
            if (h.Item1 > z) Slab(name + " " + n++, x0, x1, 0f, height, z, h.Item1, m);
            if (h.Item3 > 0f) Slab(name + " below " + n++, x0, x1, 0f, h.Item3, h.Item1, h.Item2, m);
            if (h.Item4 < height) Slab(name + " above " + n++, x0, x1, h.Item4, height, h.Item1, h.Item2, m);
            z = h.Item2;
        }
        Slab(name + " " + n, x0, x1, 0f, height, z, len + WallT, m);
    }

    public void WallX(string name, float z0, float z1, float halfW, float height, Vector2 door, Material m, float doorX = 0f)
    {
        float w = door.x * 0.5f;
        Slab(name + " left", -halfW - WallT, doorX - w, 0f, height, z0, z1, m);
        Slab(name + " right", doorX + w, halfW + WallT, 0f, height, z0, z1, m);
        if (door.y < height) Slab(name + " above door", doorX - w, doorX + w, door.y, height, z0, z1, m);
    }

    public static IEnumerable<float> Every(float from, float to, float step) { for (float x = from; x <= to + 0.001f; x += step) yield return x; }

    // ---------------------------------------------------------------- doors and where you land

    public Transform Door(string name, Vector3 at, float yaw, DoorLook look, bool forward, bool inWall)
    {
        var door = Group(name, Root); door.localPosition = at; door.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var paint = Glitter(look.name + " paint", look.paint, look.glow, look.glitter, 0.6f);
        var portal = K.PortalMat(MatDir + "/" + look.name + " portal.mat", look.veil, look.deep, look.rim, look.opening);
        portal.SetFloat("_Glitter", 1f); portal.SetColor("_GlitterColor", look.halo);
        float w = look.opening.x, h = look.opening.y, sill = inWall ? 0f : 0.06f, depth = inWall ? WallT + 0.06f : 0.16f;
        K.Box(door, "Post left", new Vector3(-w / 2f - 0.06f, (sill + h + 0.12f) / 2f, 0f), new Vector3(0.12f, sill + h + 0.12f, depth), paint);
        K.Box(door, "Post right", new Vector3(w / 2f + 0.06f, (sill + h + 0.12f) / 2f, 0f), new Vector3(0.12f, sill + h + 0.12f, depth), paint);
        K.Box(door, "Lintel", new Vector3(0f, sill + h + 0.06f, 0f), new Vector3(w + 0.24f, 0.12f, depth), paint);
        if (!inWall) K.Box(door, "Threshold", new Vector3(0f, 0.03f, 0f), new Vector3(w + 0.2f, 0.06f, 0.26f), paint);
        // leaves swung open toward you (the front is local -Z)
        if (look.twin)
            foreach (var s in new[] { -1f, 1f })
            {
                var hinge = Group("Hinge " + (s < 0 ? "left" : "right"), door);
                hinge.localPosition = new Vector3(s * w / 2f, sill, -depth / 2f); hinge.localRotation = Quaternion.Euler(0f, s * -68f, 0f);
                K.Box(hinge, "Leaf", new Vector3(-s * w / 4f, h / 2f, -0.025f), new Vector3(w / 2f - 0.02f, h - 0.04f, 0.05f), paint);
            }
        else
        {
            var hinge = Group("Hinge", door);
            hinge.localPosition = new Vector3(-w / 2f, sill, inWall ? -depth / 2f : 0f); hinge.localRotation = Quaternion.Euler(0f, 72f, 0f);
            K.Box(hinge, "Leaf", new Vector3(w / 2f, h / 2f, -0.025f), new Vector3(w - 0.02f, h - 0.04f, 0.05f), paint);
        }
        var p = K.Portal(door, sill, look.opening, portal);
        if (inWall)
        {
            p.transform.localPosition += new Vector3(0f, 0f, -depth / 2f + 0.02f);
            K.Box(door, "Backing", new Vector3(0f, (h + 0.4f) / 2f, depth / 2f + 0.3f), new Vector3(w + 0.6f, h + 0.5f, 0.1f), Lit("Doorway backing", Backing, Color.black, 0.8f, false));
        }
        var motes = Group("Rising motes", door); motes.localPosition = new Vector3(0f, 1.0f, -0.45f);
        K.Motes(motes.gameObject, Glow(look.name + " motes", look.halo * 0.7f), forward ? 24f : 10f, new Vector3(w + 0.2f, 1.9f, 0.5f));
        K.GlitterHalo(door, Glow(look.name + " glitter", look.halo, 2.6f), new Vector3(0f, 1.4f, -0.8f), new Vector3(w + 2f, 3f, 1.5f), forward ? 80f : 45f, forward ? 600 : 360);
        float th = Mathf.Min(h, 2.2f);
        K.Trigger(door, forward ? "Forward door trigger" : "Arrival door trigger", new Vector3(0f, sill + th / 2f, inWall ? -depth / 2f : 0f), new Vector3(Mathf.Max(0.8f, w - 0.06f), th - 0.15f, 0.36f));
        return door;
    }

    // where the review renders stand for their halfway shot, when a straight line from arrival to door would leave the place
    public void Halfway(Vector3 at, float yaw) { var h = Group("Halfway point", Root); h.localPosition = at; h.localRotation = Quaternion.Euler(0f, yaw, 0f); }

    // where a forward door lands you, and where its way-back door brings you back (in front of the forward door)
    public void Points(Vector3 arrival, float yaw, Transform forwardDoor, float frontOffset = 1.8f)
    {
        var a = Group("Arrival point", Root); a.localPosition = arrival; a.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var r = Group("Return point", Root);
        var front = forwardDoor.localRotation * Vector3.back;
        r.localPosition = forwardDoor.localPosition + front * frontOffset; r.localRotation = Quaternion.LookRotation(front);
    }

    // a faint shaft of light standing over a far door, so it reads as a beacon from where you arrive
    public void Beacon(Transform door, Color hdr, float height = 36f)
        => K.Quad(door, "Beacon", new Vector3(0f, height / 2f - 1f, 0.4f), Quaternion.identity, new Vector2(5f, height), Glow("Beacon", hdr * 0.55f, 1.4f));

    public void Spot(Vector3 at)
    {
        var s = Group(Animal + " hiding spot (glass " + Animal.ToLowerInvariant() + ": later pass)", Root);
        s.localPosition = at;
    }

    // ---------------------------------------------------------------- effects

    static readonly AnimationCurve Twinkle = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.15f, 1f), new Keyframe(0.3f, 0.3f), new Keyframe(0.5f, 1f), new Keyframe(0.7f, 0.35f), new Keyframe(1f, 0f));

    public ParticleSystem Motes(string name, Vector3 c, Vector3 box, Color hdr, float rate, float rise, float size, int max, bool twinkle = false, float life = 6f, float softness = 2f)
    {
        var go = Group(name, Fx); go.localPosition = c;
        var ps = K.Motes(go.gameObject, Glow(name, hdr, softness), rate, box, rise, size, max);
        var main = ps.main; main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
        if (twinkle) { var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, Twinkle); }
        return ps;
    }

    public ParticleSystem Rain(string name, Vector3 c, Vector3 box, float rate, Color hdr)
    {
        var ps = Motes(name, c, box, hdr, rate, -7f, 0.018f, Mathf.CeilToInt(rate * 1.4f) + 50, false, 1.2f, 0.8f);
        var noise = ps.noise; noise.enabled = false;
        var r = ps.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.05f; r.lengthScale = 1f;
        return ps;
    }

    public ParticleSystem Snow(string name, Vector3 c, Vector3 box, float rate) => Motes(name, c, box, new Color(1.2f, 1.2f, 1.3f), rate, -0.45f, 0.035f, Mathf.CeilToInt(rate * 12f) + 50, false, 10f, 1.4f);

    public void Finish()
    {
        LiminalWorldGlow.AddStopVolume(Root);
    }

    // ---------------------------------------------------------------- bake the still pieces

    public void Bake()
    {
        var byMat = new Dictionary<Material, List<CombineInstance>>();
        var toRoot = Root.worldToLocalMatrix;
        int pieces = 0;
        foreach (var r in Static.GetComponentsInChildren<MeshRenderer>())
        {
            var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null || r.sharedMaterial == null || !r.enabled) continue;   // hidden ones are colliders only
            if (!byMat.TryGetValue(r.sharedMaterial, out var list)) byMat[r.sharedMaterial] = list = new List<CombineInstance>();
            list.Add(new CombineInstance { mesh = mf.sharedMesh, transform = toRoot * r.transform.localToWorldMatrix });
            pieces++;
        }
        var path = GenDir + "/Doorspace " + N.ToString("00") + " still parts.asset";
        AssetDatabase.DeleteAsset(path);
        var baked = Group("Still parts (baked)", Root);
        bool first = true; int meshes = 0, verts = 0;
        foreach (var kv in byMat.OrderBy(k => k.Key.name, StringComparer.Ordinal))
        {
            var chunk = new List<CombineInstance>(); int cv = 0, part = 0;
            void Flush()
            {
                if (chunk.Count == 0) return;
                var mesh = new Mesh { name = Name + " · " + kv.Key.name + (part > 0 ? " " + part : ""), indexFormat = cv > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.CombineMeshes(chunk.ToArray(), true, true);
                mesh.RecalculateBounds();
                if (first) { AssetDatabase.CreateAsset(mesh, path); first = false; } else AssetDatabase.AddObjectToAsset(mesh, path);
                var g = new GameObject(kv.Key.name + (part > 0 ? " " + part : "")); g.transform.SetParent(baked, false);
                g.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = kv.Key; mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
                meshes++; verts += cv; part++; chunk.Clear(); cv = 0;
            }
            foreach (var ci in kv.Value)
            {
                if (cv + ci.mesh.vertexCount > 60000) Flush();
                chunk.Add(ci); cv += ci.mesh.vertexCount;
            }
            Flush();
        }
        if (walk.Count > 0)
        {
            var col = new GameObject("Walkable and walls (colliders)"); col.transform.SetParent(Root, false);
            foreach (var (c, s) in walk) { var bc = col.AddComponent<BoxCollider>(); bc.center = c; bc.size = s; }
        }
        UnityEngine.Object.DestroyImmediate(Static.gameObject);
        AssetDatabase.SaveAssets();
        Report.Insert(0, "- Still parts: " + pieces + " pieces baked into " + meshes + " meshes (" + verts + " vertices, `" + path + "`); " + walk.Count + " box colliders" + (turned != null ? " and " + turned.childCount + " turned ones" : "") + ".");
    }
}
