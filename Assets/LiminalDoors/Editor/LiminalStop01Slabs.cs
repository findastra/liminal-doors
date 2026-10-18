// Claude, 2026-10-18. Liminal Doors stop 01: walk out of the pink booth onto its cloud mound.
// The owner's cloud-collider rule from Astra's Infinite Pole: "thin, and only below the cloud, nothing outside it
// seen from above". The rasteriser is ClaudeCloudRedo.Raster (Infinite Pole, 2026-10-06).
//
// Why a surface and not flat box slabs: the mound is a 15 m dome of fused puffs, about 1.2 m higher in the middle
// than at the rim. A first pass with Infinite Pole's flat 0.06 m slabs (largest rectangles inside 0.15 m height bands)
// broke the dome into rings: 25 boxes covered 17% of the top and left a 1.2 m hole right outside the booth door.
// So the walkable part is one thin heightfield (a MeshCollider, no thickness at all) that follows the mound:
//  - only over cells at least 0.1 m in from the mound's outline seen from above (nothing outside it);
//  - every corner at most the lowest visible top of the cells around it, minus 2 cm (the neighbour rule), so the
//    surface stays under the cloud everywhere the 0.26 m grid can see;
//  - the booth sits in a pocket of cloud: right outside its walls the cloud is ~10 cm under the floor, then a 25-30 cm
//    bank. On the booth ground (the floor plus a 12 cm wall ring, where the mound also has a cut-out for the walls)
//    the surface is the threshold: level with the floor (inside the 4 cm floor box under the booth), and the pocket's
//    low cloud there does not pull the cloud surface down. From the ring it ramps up at most 0.4 m per metre and
//    stays under the cloud. The ring is the one place it can sit above visible cloud; validate reports by how much.
// It lives on its own GameObject "Cloud mound walk surface" (Default layer) under the stop; applying again rebuilds
// it. The mesh is Assets/LiminalDoors/Meshes/Cloud mound walk surface.asset. Booth floor, phone trigger and sea are
// never touched.
//
// Menu: LiminalDoors/Stop 01/Mound walk surface (apply)    -> backs up the scene once, rebuilds, Review/stop-01-slabs/apply.md
//       LiminalDoors/Stop 01/Mound walk surface (validate) -> physics + outline checks, overlay renders, Review/stop-01-slabs/report.md
//       LiminalDoors/Stop 01/Mound walk surface - restore scene from backup
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class LiminalStop01Slabs
{
    // BoothPad: the mound mesh has a cut-out ring where the booth walls stand; within this distance of the booth floor
    // the booth itself covers the ground from above, so that ring is not the cloud's outline.
    const float Margin = .1f, Under = .02f, Ramp = .4f, Tol = .02f, BoothPad = .12f;
    const string StopName = "Stop 01 - Fox - Pink Booth";
    const string BoothName = "Pink booth (from Astra's Infinite Pole)";
    const string SurfaceName = "Cloud mound walk surface";
    const string OldSlabsName = "Cloud mound walk slabs";   // the first, box-slab pass; removed on apply
    const string MeshPath = "Assets/LiminalDoors/Meshes/Cloud mound walk surface.asset";
    const string Out = "Review/stop-01-slabs";
    const string BackupDir = "Review/Backups/stop-01-slabs";
    static string BackupFile => Path.Combine(BackupDir, Path.GetFileName(LiminalStop01.ScenePath) + ".txt");

    // ------------------------------------------------------------------ menu

    [MenuItem("LiminalDoors/Stop 01/Mound walk surface (apply)")]
    public static void Apply()
    {
        var s = Find();
        var r = new List<string> { "# Stop 01 mound walk surface: apply", "Run " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"), "" };

        Directory.CreateDirectory(BackupDir);
        if (!File.Exists(BackupFile)) { File.Copy(LiminalStop01.ScenePath, BackupFile); r.Add("Backup made now (scene before any mound colliders): `" + BackupFile + "`"); }
        else r.Add("Backup kept from the first apply (scene before any mound colliders): `" + BackupFile + "`");
        foreach (var name in new[] { SurfaceName, OldSlabsName })
        {
            var old = s.stop.Find(name);
            if (old != null) { Undo.DestroyObjectImmediate(old.gameObject); r.Add("Removed the previous " + name + "."); }
        }

        var frame = MoundFrame(s.mound); var toFrame = frame.inverse;
        var g = Raster(s.mound.GetComponent<MeshRenderer>(), toFrame);
        var covered = BoothFootprint(s, toFrame, BoothPad);
        float floorTop = s.floor.bounds.max.y - frame.m13;   // in frame height

        // fine cells: standable = solid (cloud, or booth ground) and at least Margin in from the outline.
        // safe = lowest visible top over the cell and its 8 neighbours, minus 2 cm, counting only cloud OUTSIDE the
        // booth ground: the booth sits in a pocket of cloud (about 10 cm under its floor right outside the walls, then a
        // 25-30 cm bank), and inside the floor + wall ring the surface is the threshold, level with the booth floor.
        int W = g.W, H = g.H, m = Mathf.Max(1, Mathf.CeilToInt(Margin / g.cell));
        Func<int, int, bool> boothGround = (x, z) => In(covered, g.X(x), g.Z(z));
        Func<int, int, bool> solid = (x, z) => g.Occ(x, z) || (x >= 0 && z >= 0 && x < W && z < H && boothGround(x, z));
        var stand = new bool[W * H]; var safe = new float[W * H];
        for (int z = 0; z < H; z++) for (int x = 0; x < W; x++)
            {
                int k = x + z * W; safe[k] = float.PositiveInfinity; if (!solid(x, z)) continue;
                bool ok = true;
                for (int dz = -m; dz <= m && ok; dz++) for (int dx = -m; dx <= m; dx++) if (!solid(x + dx, z + dz)) { ok = false; break; }
                stand[k] = ok;
                for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++)
                        if (g.Occ(x + dx, z + dz) && !boothGround(x + dx, z + dz)) safe[k] = Mathf.Min(safe[k], g.top[x + dx + (z + dz) * W] - Under);
            }

        // coarse quads of 2x2 fine cells; a quad is walkable only when all four fine cells are standable
        int CW = W / 2, CH = H / 2; var quad = new bool[CW * CH];
        for (int cz = 0; cz < CH; cz++) for (int cx = 0; cx < CW; cx++)
            {
                bool ok = true;
                for (int dz = 0; dz < 2; dz++) for (int dx = 0; dx < 2; dx++) ok &= stand[cx * 2 + dx + (cz * 2 + dz) * W];
                quad[cx + cz * CW] = ok;
            }
        float cc = g.cell * 2;
        var verts = new List<Vector3>(); var index = new Dictionary<int, int>(); var tris = new List<int>();
        // A corner is at most the safe height of the four fine cells that touch it. The four fine cells of a quad are
        // all neighbours of each other and "safe" already takes the 3x3 minimum, so every corner of a quad is under
        // every fine cell of that quad (minus 2 cm), and so is the flat triangle between them.
        Func<int, int, int> corner = (ix, iz) =>
        {
            int key = ix + iz * (CW + 1); if (index.TryGetValue(key, out int vi)) return vi;
            float h = float.PositiveInfinity;
            for (int dz = -1; dz <= 0; dz++) for (int dx = -1; dx <= 0; dx++)
                {
                    int fx = ix * 2 + dx, fz = iz * 2 + dz; if (fx < 0 || fz < 0 || fx >= W || fz >= H) continue;
                    h = Mathf.Min(h, safe[fx + fz * W]);
                }
            float px = g.x0 + ix * cc, pz = g.z0 + iz * cc;
            float dxB = Mathf.Max(0, Mathf.Max(covered.min.x - px, px - covered.max.x)), dzB = Mathf.Max(0, Mathf.Max(covered.min.z - pz, pz - covered.max.z));
            h = Mathf.Min(h, floorTop - .01f + Ramp * Mathf.Sqrt(dxB * dxB + dzB * dzB));   // ramp down to the booth floor (1 cm under it in the wall ring)
            verts.Add(new Vector3(px, h, pz)); index[key] = verts.Count - 1; return verts.Count - 1;
        };
        int quads = 0;
        for (int cz = 0; cz < CH; cz++) for (int cx = 0; cx < CW; cx++)
            {
                if (!quad[cx + cz * CW]) continue; quads++;
                int a = corner(cx, cz), b = corner(cx + 1, cz), c = corner(cx + 1, cz + 1), d = corner(cx, cz + 1);
                tris.AddRange(new[] { a, d, c, a, c, b });   // facing up
            }

        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (mesh == null) { Directory.CreateDirectory(Path.GetDirectoryName(MeshPath)); mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, MeshPath); }
        mesh.Clear(); mesh.name = SurfaceName; mesh.indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssets();

        var go = new GameObject(SurfaceName); Undo.RegisterCreatedObjectUndo(go, "mound walk surface");
        go.layer = 0;
        go.transform.SetParent(s.stop, false);
        go.transform.SetPositionAndRotation(frame.GetColumn(3), frame.rotation);
        var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = mesh; mc.convex = false;
        Physics.SyncTransforms();

        float cellA = g.cell * g.cell;
        r.Add("");
        r.Add("Mound raster: fine cell " + F(g.cell) + " m, " + g.occ.Count(v => v) + " occupied cells (" + F(g.occ.Count(v => v) * cellA) + " m2); standable (>= " + F(Margin) + " m in from the outline) " + F(stand.Count(v => v) * cellA) + " m2.");
        r.Add("Surface: " + quads + " quads of " + F(cc) + " m (" + F(quads * cc * cc) + " m2), " + verts.Count + " vertices, " + tris.Count / 3 + " triangles; heights " + F(verts.Min(v => v.y) + frame.m13) + " to " + F(verts.Max(v => v.y) + frame.m13) + " m (world y).");
        r.Add("Booth ramp: floor top y " + F(floorTop + frame.m13) + ", surface is 1 cm under it out to the booth's wall ring (" + F(BoothPad) + " m past the floor) and rises at most " + F(Ramp) + " m per metre until it meets the mound.");
        r.Add("Mesh: `" + MeshPath + "`; object: " + StopName + "/" + SurfaceName + " (MeshCollider, Default layer).");
        r.Add("");
        r.AddRange(Door(s, g, toFrame));
        EditorSceneManager.MarkSceneDirty(s.stop.gameObject.scene);
        EditorSceneManager.SaveScene(s.stop.gameObject.scene);
        Write("apply.md", r);
    }

    [MenuItem("LiminalDoors/Stop 01/Mound walk surface (validate)")]
    public static void Validate()
    {
        var s = Find();
        var holder = s.stop.Find(SurfaceName); if (holder == null) throw new Exception("No " + SurfaceName + "; run Mound walk surface (apply) first.");
        var mc = holder.GetComponent<MeshCollider>(); var mesh = mc.sharedMesh;
        Physics.SyncTransforms();
        var r = new List<string> { "# Stop 01 mound walk surface: validation", "Run " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + " in Unity " + Application.unityVersion, "" };

        var frame = MoundFrame(s.mound); var toFrame = frame.inverse;
        var g = Raster(s.mound.GetComponent<MeshRenderer>(), toFrame);
        var covered = BoothFootprint(s, toFrame, BoothPad); var boothOnly = BoothFootprint(s, toFrame, 0f);
        float floorTop = s.floor.bounds.max.y - frame.m13;
        var M = toFrame * holder.localToWorldMatrix;
        var vs = mesh.vertices.Select(v => M.MultiplyPoint3x4(v)).ToArray(); var ts = mesh.triangles;
        Func<int, int, bool> solid = (x, z) => g.Occ(x, z) || (x >= 0 && z >= 0 && x < g.W && z < g.H && In(covered, g.X(x), g.Z(z)));

        // outline and top: sample every triangle at its corners, edge midpoints and centre against the raster.
        // Within BoothPad of the booth floor the booth covers the ground; there the surface only has to stay under the floor top.
        int n = 0, outN = 0, aboveN = 0, marginN = 0, boothN = 0, boothAbove = 0;
        float worstAbove = float.NegativeInfinity, ringOverFloor = float.NegativeInfinity, ringOverCloud = float.NegativeInfinity;
        int m = Mathf.Max(1, Mathf.CeilToInt(Margin / g.cell));
        for (int i = 0; i < ts.Length; i += 3)
        {
            Vector3 a = vs[ts[i]], b = vs[ts[i + 1]], c = vs[ts[i + 2]];
            foreach (var p in new[] { a, b, c, (a + b) / 2, (b + c) / 2, (a + c) / 2, (a + b + c) / 3 })
            {
                // nudge 1 mm toward the triangle centre so shared edges are judged by the cells they belong to
                var q = Vector3.MoveTowards(p, (a + b + c) / 3, .001f);
                n++; int cx = g.CX(q.x), cz = g.CZ(q.z);
                bool underBooth = In(boothOnly, q.x, q.z) || In(covered, g.X(cx), g.Z(cz));   // booth ground by cell centre, as apply decides
                if (underBooth)
                {
                    boothN++; ringOverFloor = Mathf.Max(ringOverFloor, q.y - floorTop);
                    if (q.y > floorTop + Ramp * g.cell * 2) boothAbove++;
                    if (g.Occ(cx, cz) && !In(boothOnly, q.x, q.z)) ringOverCloud = Mathf.Max(ringOverCloud, q.y - g.top[cx + cz * g.W]);
                    continue;
                }
                if (!g.Occ(cx, cz)) { outN++; continue; }
                bool inMargin = true;
                for (int dz = -m; dz <= m && inMargin; dz++) for (int dx = -m; dx <= m; dx++) if (!solid(cx + dx, cz + dz)) { inMargin = false; break; }
                if (!inMargin) marginN++;
                float over = q.y - (g.top[cx + cz * g.W] - Under);
                worstAbove = Mathf.Max(worstAbove, over);
                if (over > Tol) aboveN++;
            }
        }
        r.Add("Collider: one MeshCollider surface (no thickness), " + vs.Length + " vertices, " + ts.Length / 3 + " triangles, Default layer " + Pass(holder.gameObject.layer == 0 && !mc.isTrigger));
        r.Add("Outline: " + Pct(outN, n) + " of " + n + " samples outside the mound seen from above " + Pass(outN == 0) + "; " + Pct(marginN, n) + " closer than " + F(Margin) + " m to the outline " + Pass(marginN == 0));
        r.Add("Under the cloud: " + Pct(aboveN, n) + " of samples more than " + F(Tol) + " m above (visible top - 2 cm); worst " + F(worstAbove) + " m vs the 2 cm line " + Pass(aboveN == 0));
        r.Add("Booth ground (floor + " + F(BoothPad) + " m wall ring): " + boothN + " samples, highest " + F(ringOverFloor) + " m relative to the floor top, " + boothAbove + " more than one ramp cell (" + F(Ramp * g.cell * 2) + " m) above it " + Pass(boothAbove == 0));
        r.Add("Threshold exception: in the wall ring the surface is up to " + F(Mathf.Max(0, ringOverCloud)) + " m above the visible cloud of the pocket the booth sits in (that ring is the only place it is not under the cloud).");

        // physics: stand on it at every quad, nothing horizontal above or below it, nothing outside the outline from above
        int stand = 0, standN = 0; var fails = new List<string>(); var local = mesh.vertices;
        for (int i = 0; i < ts.Length; i += 6)
        {
            var cw = holder.TransformPoint((local[ts[i]] + local[ts[i + 2]]) / 2);   // quad centre (a and c are opposite)
            standN++; RaycastHit h;
            if (Physics.Raycast(cw + Vector3.up * 3f, Vector3.down, out h, 6f, 1, QueryTriggerInteraction.Ignore) && (h.collider == mc || h.collider == s.floor)) stand++;
            else fails.Add("stand " + V(cw) + (h.collider != null ? " first hit " + h.collider.name : " no hit"));
        }
        var bb = mc.bounds; int side = 0, sideN = 0;
        foreach (var y in new[] { bb.max.y + .3f, bb.min.y - .3f })
            foreach (var dir in new[] { holder.right, holder.forward, -holder.right, -holder.forward, (holder.right + holder.forward).normalized, (holder.right - holder.forward).normalized })
            {
                var d = new Vector3(dir.x, 0, dir.z).normalized; float reach = bb.extents.magnitude + 2f;
                var o = new Vector3(bb.center.x, y, bb.center.z) - d * reach; sideN++;
                if (!Physics.RaycastAll(o, d, reach * 2, 1, QueryTriggerInteraction.Ignore).Any(x => x.collider == mc)) side++;
                else fails.Add("side ray hit at " + (y > bb.max.y ? "+0.3" : "-0.3") + " m");
            }
        // grid of rays from above over the mound's whole bounds: anything that lands on the surface must be over the cloud
        int gridHit = 0, gridOut = 0;
        var mb = s.mound.GetComponent<Renderer>().bounds;
        for (float x = mb.min.x - 1; x <= mb.max.x + 1; x += .25f)
            for (float z = mb.min.z - 1; z <= mb.max.z + 1; z += .25f)
            {
                RaycastHit h;
                if (!Physics.Raycast(new Vector3(x, mb.max.y + 3, z), Vector3.down, out h, mb.size.y + 8, 1, QueryTriggerInteraction.Ignore) || h.collider != mc) continue;
                gridHit++; var q = toFrame.MultiplyPoint3x4(h.point);
                if (!g.Occ(g.CX(q.x), g.CZ(q.z)) && !In(covered, q.x, q.z)) { gridOut++; fails.Add("bird's-eye hit outside the cloud at " + V(h.point)); }
            }
        r.Add("Stand-on: " + stand + "/" + standN + " quads, a downward ray lands on the surface (or the booth floor above it) " + Pass(stand == standN));
        r.Add("No side walls: " + side + "/" + sideN + " horizontal rays 0.3 m above and below the surface miss it " + Pass(side == sideN));
        r.Add("Bird's-eye: " + gridHit + " rays on a 0.25 m grid land on the surface, " + gridOut + " of them outside the visible cloud and the booth " + Pass(gridOut == 0));
        r.Add("");
        r.AddRange(Door(s, g, toFrame));
        r.Add("");
        r.Add("Untouched on purpose: booth floor " + V(s.floor.bounds.center) + " (" + V(s.floor.bounds.size) + "), phone voice trigger, cloud sea (no colliders).");
        r.Add("");
        Overlays(s, holder, mesh, r);
        if (fails.Count > 0) { r.Add(""); r.Add("Details (" + fails.Count + "):"); r.AddRange(fails.Take(60).Select(f => "- " + f)); }
        Write("report.md", r);
    }

    [MenuItem("LiminalDoors/Stop 01/Mound walk surface - restore scene from backup")]
    public static void Restore()
    {
        if (!File.Exists(BackupFile)) throw new Exception("No backup at " + BackupFile);
        File.Copy(BackupFile, LiminalStop01.ScenePath, true);
        AssetDatabase.ImportAsset(LiminalStop01.ScenePath, ImportAssetOptions.ForceUpdate);
        EditorSceneManager.OpenScene(LiminalStop01.ScenePath, OpenSceneMode.Single);
        Write("restore.md", new List<string> { "Restored `" + LiminalStop01.ScenePath + "` from `" + BackupFile + "` at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz") + ". Anything added to the scene after that backup (the walk surface, the phone wiring) is gone; re-run their menus." });
    }

    // ------------------------------------------------------------------ scene lookups

    struct Parts { public Transform stop, booth, frame, mound; public BoxCollider floor; }

    static Parts Find()
    {
        if (SceneManager.GetActiveScene().path != LiminalStop01.ScenePath) EditorSceneManager.OpenScene(LiminalStop01.ScenePath, OpenSceneMode.Single);
        var stopGo = GameObject.Find(StopName); if (stopGo == null) throw new Exception("No " + StopName + " in the scene.");
        var booth = stopGo.transform.Find(BoothName); if (booth == null) throw new Exception("No booth under the stop.");
        var p = new Parts { stop = stopGo.transform, booth = booth, frame = booth.Find("Banner booth"), mound = booth.Find("Cloud mound") };
        if (p.frame == null || p.mound == null || p.mound.GetComponent<MeshRenderer>() == null) throw new Exception("Booth prefab is missing Banner booth or Cloud mound.");
        var f = p.frame.Find("Cloud booth floor"); p.floor = f != null ? f.GetComponent<BoxCollider>() : null;
        if (p.floor == null) throw new Exception("Booth prefab is missing its Cloud booth floor collider.");
        return p;
    }

    static bool In(Bounds b, float x, float z) => x > b.min.x && x < b.max.x && z > b.min.z && z < b.max.z;

    // mound position + yaw, metres (no scale)
    static Matrix4x4 MoundFrame(Transform mound) => Matrix4x4.TRS(mound.position, Quaternion.Euler(0, mound.eulerAngles.y, 0), Vector3.one);

    static Bounds BoothFootprint(Parts s, Matrix4x4 toFrame, float pad)
    {
        var b = s.floor; var bs = new Bounds();
        for (int i = 0; i < 4; i++)
        {
            var corner = b.center + Vector3.Scale(b.size * .5f, new Vector3(i % 2 == 0 ? -1 : 1, 0, i < 2 ? -1 : 1));
            var p = toFrame.MultiplyPoint3x4(b.transform.TransformPoint(corner));
            if (i == 0) bs = new Bounds(p, Vector3.zero); else bs.Encapsulate(p);
        }
        bs.Expand(new Vector3(pad * 2, 0, pad * 2));
        return bs;
    }

    // ------------------------------------------------------------------ raster (ClaudeCloudRedo.Raster without the height cap)

    class Grid
    {
        public float x0, z0, cell; public int W, H;
        public bool[] occ; public float[] top;   // top = highest visible surface (-inf = none)
        public bool Occ(int x, int z) { return x >= 0 && z >= 0 && x < W && z < H && occ[x + z * W]; }
        public int CX(float x) { return Mathf.FloorToInt((x - x0) / cell); }
        public int CZ(float z) { return Mathf.FloorToInt((z - z0) / cell); }
        public float X(int x) { return x0 + (x + .5f) * cell; }
        public float Z(int z) { return z0 + (z + .5f) * cell; }
    }

    static Grid Raster(Renderer r, Matrix4x4 toFrame)
    {
        var tris = new List<Vector3>(); var vb = new Bounds(); bool any = false;
        var mesh = r.GetComponent<MeshFilter>().sharedMesh; var M = toFrame * r.localToWorldMatrix;
        var vs = mesh.vertices; var ts = mesh.triangles;
        var p = new Vector3[vs.Length]; for (int i = 0; i < vs.Length; i++) p[i] = M.MultiplyPoint3x4(vs[i]);
        for (int i = 0; i < ts.Length; i += 3)
        {
            Vector3 a = p[ts[i]], b = p[ts[i + 1]], c = p[ts[i + 2]];
            tris.Add(a); tris.Add(b); tris.Add(c);
            if (!any) { vb = new Bounds(a, Vector3.zero); any = true; }
            vb.Encapsulate(a); vb.Encapsulate(b); vb.Encapsulate(c);
        }
        float ext = Mathf.Max(vb.size.x, vb.size.z);
        var g = new Grid { cell = Mathf.Clamp(ext / 120f, .04f, .25f) };
        g.x0 = vb.min.x - g.cell * 2; g.z0 = vb.min.z - g.cell * 2;
        g.W = Mathf.CeilToInt(vb.size.x / g.cell) + 5; g.H = Mathf.CeilToInt(vb.size.z / g.cell) + 5;
        g.occ = new bool[g.W * g.H]; g.top = Enumerable.Repeat(float.NegativeInfinity, g.W * g.H).ToArray();
        for (int i = 0; i < tris.Count; i += 3)
        {
            Vector3 a = tris[i], b = tris[i + 1], c = tris[i + 2];
            float d = (b.x - a.x) * (c.z - a.z) - (c.x - a.x) * (b.z - a.z);
            if (Mathf.Abs(d) < 1e-9f) continue;
            int xa = Mathf.Max(0, g.CX(Mathf.Min(a.x, Mathf.Min(b.x, c.x)))), xb = Mathf.Min(g.W - 1, g.CX(Mathf.Max(a.x, Mathf.Max(b.x, c.x))));
            int za = Mathf.Max(0, g.CZ(Mathf.Min(a.z, Mathf.Min(b.z, c.z)))), zb = Mathf.Min(g.H - 1, g.CZ(Mathf.Max(a.z, Mathf.Max(b.z, c.z))));
            for (int z = za; z <= zb; z++) for (int x = xa; x <= xb; x++)
                {
                    float px = g.X(x), pz = g.Z(z);
                    float w1 = ((b.x - px) * (c.z - pz) - (c.x - px) * (b.z - pz)) / d;
                    float w2 = ((c.x - px) * (a.z - pz) - (a.x - px) * (c.z - pz)) / d;
                    float w3 = 1 - w1 - w2;
                    if (w1 < -1e-4f || w2 < -1e-4f || w3 < -1e-4f) continue;
                    float y = w1 * a.y + w2 * b.y + w3 * c.y;
                    int k = x + z * g.W; g.occ[k] = true; if (y > g.top[k]) g.top[k] = y;
                }
        }
        return g;
    }

    // ------------------------------------------------------------------ checks

    // Walk straight out of the door (local +Z of "Banner booth") and down the mound: what a downward ray stands on
    // every 10 cm, from the middle of the booth floor to 7 m out.
    static List<string> Door(Parts s, Grid g = null, Matrix4x4 toFrame = default)
    {
        Physics.SyncTransforms();
        var r = new List<string> { "Door walk (straight out of the booth door, a downward ray every 0.1 m):" };
        float floorTop = s.floor.bounds.max.y;
        r.Add("- booth floor top: y " + F(floorTop));
        float prev = floorTop, worstUp = 0, worstDown = 0, steepest = 0; string firstMound = null, lastMound = null;
        var line = new List<string>();
        for (float d = 0f; d <= 7.001f; d += .1f)
        {
            var p = s.frame.position + s.frame.forward * d; RaycastHit h;
            if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out h, 8f, 1, QueryTriggerInteraction.Ignore))
            {
                float y = h.point.y; worstUp = Mathf.Max(worstUp, y - prev); worstDown = Mathf.Max(worstDown, prev - y); steepest = Mathf.Max(steepest, Mathf.Abs(y - prev) / .1f);
                prev = y;
                bool mound = h.collider.transform.name == SurfaceName;
                if (mound) { if (firstMound == null) firstMound = F(d) + " m out at y " + F(y); lastMound = F(d) + " m out at y " + F(y); }
                string cloud = "";
                if (g != null) { var q = toFrame.MultiplyPoint3x4(p); int cx = g.CX(q.x), cz = g.CZ(q.z); cloud = g.Occ(cx, cz) ? " (cloud " + F(g.top[cx + cz * g.W] + p.y - q.y) + ")" : " (no cloud)"; }
                line.Add(F(d) + ":" + (mound ? "mound" : h.collider.name == "Cloud booth floor" ? "floor" : h.collider.name) + "@" + F(y) + cloud);
            }
            else line.Add(F(d) + ":none");
        }
        r.Add("- first mound contact: " + (firstMound ?? "none within 7 m") + "; last: " + (lastMound ?? "-") + " (past that is the mound's rim and the drop to the sea)");
        r.Add("- largest 10 cm step up " + F(worstUp) + " m, down " + F(worstDown) + " m (steepest slope " + (Mathf.Atan(steepest) * Mathf.Rad2Deg).ToString("0") + " deg); longest stretch with nothing underfoot before the rim " + F(Gap(line)) + " m");
        r.Add("- samples: " + string.Join("  ", line));
        return r;
    }

    // nothing-underfoot stretches between the booth and the last mound contact
    static float Gap(List<string> line)
    {
        int last = line.FindLastIndex(x => !x.EndsWith(":none")); float run = 0, worst = 0;
        for (int i = 0; i <= last; i++) { if (line[i].EndsWith(":none")) { run += .1f; worst = Mathf.Max(worst, run); } else run = 0; }
        return worst;
    }

    // Orthographic top and side views with the walk surface drawn in magenta through the cloud.
    static void Overlays(Parts s, Transform holder, Mesh surface, List<string> r)
    {
        Directory.CreateDirectory(Out);
        var mat = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_Cull", (int)CullMode.Off); mat.SetInt("_ZWrite", 0); mat.SetInt("_ZTest", (int)CompareFunction.Always);
        mat.color = new Color(1f, 0f, .85f, .5f); mat.renderQueue = 4000;
        var go = new GameObject("_surface overlay") { hideFlags = HideFlags.DontSave };
        var camGo = new GameObject("_surface overlay camera") { hideFlags = HideFlags.DontSave };
        try
        {
            go.transform.SetPositionAndRotation(holder.position, holder.rotation);
            go.AddComponent<MeshFilter>().sharedMesh = surface; go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            var bounds = s.mound.GetComponent<Renderer>().bounds;
            var cam = camGo.AddComponent<Camera>(); cam.orthographic = true; cam.clearFlags = CameraClearFlags.Skybox; cam.nearClipPlane = .05f; cam.farClipPlane = 200f;
            float half = Mathf.Max(bounds.extents.x, bounds.extents.z) + 1f;
            var yaw = Quaternion.Euler(0, s.mound.eulerAngles.y, 0);
            Shot(cam, bounds.center + Vector3.up * 40f, yaw * Quaternion.Euler(90, 0, 0), half, "stop-01-slabs-top.png");
            Shot(cam, bounds.center - (yaw * Vector3.forward) * 40f + Vector3.up * .3f, yaw, half * .55f, "stop-01-slabs-side.png");
            r.Add("Overlays (magenta = walk surface, drawn through the cloud): `" + Out + "/stop-01-slabs-top.png`, `" + Out + "/stop-01-slabs-side.png`");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(camGo); UnityEngine.Object.DestroyImmediate(mat); }
    }

    static void Shot(Camera cam, Vector3 pos, Quaternion rot, float half, string file)
    {
        cam.transform.SetPositionAndRotation(pos, rot); cam.orthographicSize = half;
        int w = 1600, h = 1600;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB); var prev = RenderTexture.active;
        try
        {
            cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(Out, file), tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
        }
        finally { cam.targetTexture = null; RenderTexture.active = prev; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
    }

    static void Write(string file, List<string> lines) { Directory.CreateDirectory(Out); File.WriteAllText(Path.Combine(Out, file), string.Join("\n", lines) + "\n"); }
    static string F(float v) { return v.ToString("0.00"); }
    static string V(Vector3 v) { return F(v.x) + "," + F(v.y) + "," + F(v.z); }
    static string Pct(int a, int n) { return (n == 0 ? 0 : a * 100f / n).ToString("0.0") + "%"; }
    static string Pass(bool ok) { return ok ? "PASS" : "FAIL"; }
}
