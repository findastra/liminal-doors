// Claude, 2026-10-19. Doorspaces 15-25, each from its concept floor, made liminal. See LiminalDoorspaces.cs.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class LiminalDoorspaces
{
    // 15 Seahorse / Empty Aquarium: "A quiet aquarium corridor with dark, seemingly vacant tanks. Fish-shaped lights appear, then dissolve."
    static void B15(Doorspace d)
    {
        const float W = 4f, L = 220f, Ht = 4.2f;
        d.Haze = C(0.04f, 0.08f, 0.16f); d.HazeStart = 10f; d.HazeEnd = 140f; d.Light = new Vector3(0.1f, 0.9f, 0.2f);
        d.Sky(C(0.02f, 0.03f, 0.08f), C(0.03f, 0.05f, 0.12f), C(0.05f, 0.08f, 0.16f), C(0.03f, 0.05f, 0.1f), Vector3.up, Color.black);
        var look = DoorLook.Make("Gold door", C(0.95f, 0.75f, 0.30f), C(1f, 0.82f, 0.45f));
        var floor = d.Surface("Dark floor", C(0.08f, 0.10f, 0.16f), C(0.07f, 0.09f, 0.15f), default, 0.8f, 0f, 0.8f, H(0.3f, 0.7f, 1f, 1f));
        var wall = d.Lit("Dark wall", C(0.06f, 0.08f, 0.13f));
        var holes = Windows(6f, L - 6f, 6f, 4.6f, 0.6f, 3.6f);
        d.Room(W, L, Ht, floor, wall, d.Lit("Ceiling", C(0.04f, 0.05f, 0.09f)), DoorLook.WayBack.opening, look.opening, holes, holes);
        var tank = d.Pane("Dark tank", H(0.10f, 0.45f, 0.70f, 1f), H(0.02f, 0.06f, 0.16f, 1f), H(0.3f, 0.8f, 1f, 1f), new Vector2(4.6f, 3f), 0f, 1f);
        foreach (var (z0, z1, y0, y1) in holes)
            foreach (var s in new[] { -1f, 1f })
                d.Quad("Tank", new Vector3(s * (W + 0.12f), (y0 + y1) / 2f, (z0 + z1) / 2f), Quaternion.Euler(0f, s < 0 ? -90f : 90f, 0f), new Vector2(z1 - z0, y1 - y0), tank);
        RoomDoors(d, L, look);
        d.Motes("Fish lights", new Vector3(0f, 2f, L / 2f), new Vector3(2f * W - 0.6f, 2.8f, L - 6f), H(0.4f, 0.9f, 1f, 1.6f), 14f, 0f, 0.08f, 120, true, 5f);
        d.Spot(new Vector3(-W + 0.3f, 0.05f, L * 0.52f));
        d.Report.Add("- A " + L + " m aquarium corridor, " + holes.Count * 2 + " dark tanks, fish-shaped lights that come and go.");
    }

    // 16 Heron / Blackwater Footbridge: "A narrow boardwalk crosses perfectly still dark water."
    static void B16(Doorspace d)
    {
        const float L = 400f;
        d.Haze = C(0.10f, 0.08f, 0.18f); d.HazeStart = 14f; d.HazeEnd = 200f; d.Light = new Vector3(0.3f, 0.8f, 0.4f);
        d.Sky(C(0.03f, 0.03f, 0.10f), C(0.08f, 0.06f, 0.20f), C(0.20f, 0.14f, 0.32f), C(0.06f, 0.05f, 0.12f), new Vector3(0f, -0.2f, 1f), Color.black, 1f, 0.6f, 0.4f, 0.1f);
        var look = DoorLook.Make("Pink door", C(0.98f, 0.38f, 0.66f), C(1f, 0.5f, 0.85f));
        var boards = d.Surface("Boards", C(0.32f, 0.26f, 0.26f), C(0.28f, 0.23f, 0.24f), C(0.16f, 0.13f, 0.14f), 0.25f, 0.06f, 0.4f, H(0.6f, 0.5f, 0.9f, 1f));
        var post = d.Lit("Post", C(0.22f, 0.18f, 0.20f));
        var lantern = d.Lit("Lantern", C(1f, 0.8f, 0.6f), H(1f, 0.65f, 0.4f, 1.8f), 1f, false);
        d.Slab("Black water", -800f, 800f, -0.62f, -0.6f, -300f, L + 500f, d.Water("Black water", C(0.03f, 0.04f, 0.08f), H(0.5f, 0.4f, 0.8f, 1f), 0.1f), false);
        d.Slab("Boardwalk", -0.7f, 0.7f, -0.2f, 0f, -4f, L - 2f, boards);
        d.Slab("Landing", -2.2f, 2.2f, -0.2f, 0f, L - 2f, L + 2.5f, boards);
        foreach (var s in new[] { -1f, 1f }) d.Box("Handrail (collider)", new Vector3(s * 0.8f, 0.6f, L / 2f), new Vector3(0.08f, 1.2f, L + 2f), post, true).GetComponent<MeshRenderer>().enabled = false;
        foreach (var z in Doorspace.Every(0f, L - 3f, 3f))
            foreach (var s in new[] { -1f, 1f }) d.Rod("Post", new Vector3(s * 0.75f, -1.2f, z), new Vector3(s * 0.75f, 0.7f, z), 0.05f, post);
        foreach (var z in Doorspace.Every(15f, L - 5f, 15f)) d.Box("Lantern", new Vector3(0.75f, 0.85f, z), new Vector3(0.16f, 0.22f, 0.16f), lantern);
        FieldDoors(d, new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, L), look);
        d.Motes("Mist sparks", new Vector3(0f, 0.6f, L / 2f), new Vector3(30f, 1.2f, L), H(0.8f, 0.7f, 1f, 0.6f), 30f, 0.01f, 0.03f, 300, true);
        d.Spot(new Vector3(0.75f, 0.75f, L * 0.47f));
        d.Report.Add("- A " + L + " m boardwalk, 1.4 m wide, over still black water; a lantern every 15 m.");
    }

    // 17 Dragonfly / Soft Staircase: "A soft, broad staircase rises into a cloud sky."
    static void B17(Doorspace d)
    {
        const int steps = 600; const float rise = 0.2f, run = 0.5f, W = 3f;
        float top = steps * rise, len = steps * run;
        d.Haze = C(0.98f, 0.80f, 0.78f); d.HazeStart = 40f; d.HazeEnd = 380f; d.Light = new Vector3(0.4f, 0.6f, -0.6f);
        d.Sky(C(0.50f, 0.42f, 0.78f), C(0.85f, 0.62f, 0.80f), C(1f, 0.78f, 0.62f), C(0.95f, 0.75f, 0.80f), new Vector3(-0.3f, 0.25f, 1f), C(1f, 0.8f, 0.6f), 0.2f, 0.2f, 0f, 0.6f);
        var look = DoorLook.Make("Peach door", C(1f, 0.62f, 0.52f), C(1f, 0.72f, 0.62f));
        var soft = d.Surface("Soft steps", C(0.98f, 0.88f, 0.86f), C(0.96f, 0.85f, 0.84f), default, 2f, 0f, 0.3f, H(1f, 0.8f, 0.85f, 1f));
        var edge = d.Lit("Step light", C(1f, 0.85f, 0.7f), H(1f, 0.75f, 0.5f, 1.4f), 1f, false);
        var cloud = d.Lit("Cloud", C(0.98f, 0.86f, 0.90f), C(0.08f, 0.05f, 0.06f), 0.75f);
        d.Slab("Bottom landing", -W - 1f, W + 1f, -0.3f, 0f, -10f, 0f, soft);
        for (int i = 0; i < steps; i++)
        {
            d.Box("Step", new Vector3(0f, (i + 1) * rise - rise / 2f - 0.15f, i * run + run / 2f), new Vector3(2f * W, rise + 0.3f, run), soft);
            if (i % 10 == 0) foreach (var s in new[] { -1f, 1f }) d.Box("Step light", new Vector3(s * (W - 0.15f), (i + 1) * rise + 0.08f, i * run + run / 2f), new Vector3(0.12f, 0.12f, 0.12f), edge);
        }
        float ang = Mathf.Atan2(rise, run) * Mathf.Rad2Deg, slope = Mathf.Sqrt(top * top + len * len);
        var dir = new Vector3(0f, top, len).normalized; var nrm = new Vector3(0f, dir.z, -dir.y);
        // the ramp meets each step at its middle, so your feet stay on the treads
        d.Collider("Ramp under the steps", d.Root, new Vector3(0f, top / 2f + rise / 2f, len / 2f) - nrm * 0.1f, new Vector3(2f * W, 0.2f, slope), Quaternion.Euler(-ang, 0f, 0f));
        foreach (var s in new[] { -1f, 1f }) d.Collider("Side " + s, d.Root, new Vector3(s * (W + 0.1f), top / 2f + 1f, len / 2f), new Vector3(0.2f, 2.2f, slope), Quaternion.Euler(-ang, 0f, 0f));
        d.Slab("Top landing", -W - 1f, W + 1f, top - 0.3f, top, len, len + 10f, soft);
        var rnd = new System.Random(17);
        for (int i = 0; i < 120; i++)
        {
            float z = (float)(rnd.NextDouble() * (len + 200) - 60), x = (rnd.NextDouble() < 0.5 ? -1f : 1f) * (12f + (float)rnd.NextDouble() * 120f), s = 10f + (float)rnd.NextDouble() * 28f;
            d.Ball("Cloud", new Vector3(x, z / len * top - 10f - (float)rnd.NextDouble() * 30f, z), new Vector3(s * 1.5f, s * 0.5f, s), cloud);
        }
        FieldDoors(d, new Vector3(0f, 0f, -3f), new Vector3(0f, top, len + 5f), look);
        d.Spot(new Vector3(-W + 0.4f, 300 * rise + 0.05f, 300 * run + 0.2f));
        d.Report.Add("- " + steps + " soft steps, " + W * 2 + " m wide, rising " + top + " m over " + len + " m into the clouds; one ramp collider under them.");
    }

    // 18 Snow Leopard / Snow in the Lobby: "An empty hotel lobby with a curved reception desk. Very fine indoor snowfall."
    static void B18(Doorspace d)
    {
        const float W = 14f, L = 200f, Ht = 8f;
        d.Haze = C(0.86f, 0.88f, 0.94f); d.HazeStart = 18f; d.HazeEnd = 180f; d.Light = new Vector3(0.3f, 0.9f, -0.2f);
        d.Sky(C(0.6f, 0.65f, 0.75f), C(0.75f, 0.8f, 0.88f), C(0.9f, 0.9f, 0.95f), C(0.8f, 0.82f, 0.88f), Vector3.up, Color.black);
        var look = DoorLook.Make("Gold elevator door", C(0.95f, 0.80f, 0.42f), C(1f, 0.88f, 0.55f), new Vector2(1.6f, 2.6f), true);
        var marble = d.Surface("Marble floor", C(0.93f, 0.91f, 0.90f), C(0.86f, 0.85f, 0.86f), C(0.75f, 0.74f, 0.76f), 1f, 0.01f, 0.6f, H(1f, 0.92f, 0.8f, 1f));
        var wallM = d.Surface("Marble wall", C(0.95f, 0.93f, 0.90f), C(0.90f, 0.88f, 0.86f), C(0.8f, 0.78f, 0.76f), 1.2f, 0.01f);
        d.Room(W, L, Ht, marble, wallM, d.Lit("Ceiling", C(0.95f, 0.94f, 0.92f)), DoorLook.WayBack.opening, look.opening);
        var gold = d.Lit("Gold", C(0.92f, 0.78f, 0.45f), C(0.12f, 0.08f, 0.02f)); var desk = d.Lit("Desk", C(0.96f, 0.95f, 0.95f));
        var chandelier = d.Lit("Chandelier", C(1f, 0.95f, 0.85f), H(1f, 0.9f, 0.7f, 1.6f), 1f, false);
        foreach (var z in Doorspace.Every(12.5f, L - 12.5f, 25f))
        {
            for (int k = -3; k <= 3; k++) { float a = k * 14f; var c = new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 6f, 0.55f, z + 4f - Mathf.Cos(a * Mathf.Deg2Rad) * 6f); d.Box("Reception desk", c, new Vector3(1.5f, 1.1f, 0.7f), desk, true, a); d.Box("Desk top", c + Vector3.up * 0.57f, new Vector3(1.55f, 0.05f, 0.8f), gold, false, a); }
            d.Ball("Chandelier", new Vector3(0f, Ht - 1.4f, z), new Vector3(1.6f, 1.2f, 1.6f), chandelier);
        }
        foreach (var z in Doorspace.Every(6.25f, L - 6.25f, 12.5f)) foreach (var s in new[] { -1f, 1f }) d.Cyl("Column", new Vector3(s * 9f, Ht / 2f, z), 0.5f, Ht, gold, true);
        RoomDoors(d, L, look);
        d.Snow("Snow in the lobby", new Vector3(0f, Ht - 1f, L / 2f), new Vector3(2f * W - 1f, 0.5f, L - 4f), 220f);
        d.Spot(new Vector3(0f, 1.15f, 112.5f + 1.5f));
        d.Report.Add("- A " + L + " m hotel lobby with fine indoor snow; curved reception desks every 25 m, gold columns every 12.5 m.");
    }

    // 19 Raven / Pearl Car Park: "A vacant parking level with numbered pillars and oily puddles. An occasional fluorescent buzz."
    static void B19(Doorspace d)
    {
        const float W = 30f, L = 250f, Ht = 2.8f;
        d.Haze = C(0.26f, 0.30f, 0.30f); d.HazeStart = 10f; d.HazeEnd = 130f; d.Light = new Vector3(0.2f, 0.9f, 0.1f);
        d.Sky(C(0.1f, 0.1f, 0.1f), C(0.15f, 0.15f, 0.15f), C(0.2f, 0.2f, 0.2f), C(0.1f, 0.1f, 0.1f), Vector3.up, Color.black);
        var look = DoorLook.Make("Magenta door", C(0.90f, 0.20f, 0.65f), C(1f, 0.3f, 0.85f));
        var floor = d.Surface("Bay lines", C(0.40f, 0.44f, 0.44f), C(0.38f, 0.42f, 0.42f), C(0.88f, 0.88f, 0.82f), 2.5f, 0.02f, 0.25f, H(0.6f, 0.8f, 0.8f, 1f));
        var conc = d.Surface("Concrete", C(0.50f, 0.52f, 0.52f), C(0.47f, 0.49f, 0.49f), default, 1.5f, 0f);
        d.Room(W, L, Ht, floor, conc, conc, DoorLook.WayBack.opening, look.opening);
        var pillar = d.Lit("Pillar", C(0.58f, 0.60f, 0.60f)); var band = d.Lit("Pearl band", C(0.82f, 0.86f, 0.90f), C(0.05f, 0.06f, 0.07f));
        var tube = d.Lit("Fluorescent tube", C(0.9f, 1f, 0.95f), H(0.85f, 1f, 0.9f, 1.5f), 1f, false);
        var oil = d.Water("Oily puddle", C(0.15f, 0.18f, 0.22f), H(0.8f, 0.5f, 1f, 1f), 0.3f);
        int pillars = 0;
        foreach (var z in Doorspace.Every(8f, L - 6f, 8f))
            foreach (var x in Doorspace.Every(-24f, 24f, 8f))
            {
                d.Box("Pillar", new Vector3(x, Ht / 2f, z), new Vector3(0.6f, Ht, 0.6f), pillar, true);
                d.Box("Number band", new Vector3(x, 1.4f, z), new Vector3(0.64f, 0.36f, 0.64f), band);
                d.Box("Tube", new Vector3(x + 4f, Ht - 0.05f, z + 4f), new Vector3(0.1f, 0.05f, 1.4f), tube);
                pillars++;
            }
        var rnd = new System.Random(19);
        for (int i = 0; i < 60; i++) d.Quad("Puddle", new Vector3((float)(rnd.NextDouble() * 50 - 25), 0.01f, (float)(rnd.NextDouble() * (L - 10) + 5)), Quaternion.Euler(90f, (float)rnd.NextDouble() * 180f, 0f), new Vector2(1f + (float)rnd.NextDouble() * 2f, 0.8f + (float)rnd.NextDouble() * 1.5f), oil);
        RoomDoors(d, L, look);
        d.Spot(new Vector3(16.4f, 0.05f, 120f));
        d.Report.Add("- A " + L + " x " + (2 * W) + " m parking level, " + pillars + " banded pillars, puddles, low fluorescent light.");
    }

    // 20 Octopus / Room Beneath the Ocean: "A dry room beneath a transparent water ceiling. Slow overhead ripples and light shafts."
    static void B20(Doorspace d)
    {
        const float W = 8f, L = 200f, Ht = 5f;
        d.Haze = C(0.20f, 0.42f, 0.55f); d.HazeStart = 14f; d.HazeEnd = 160f; d.Light = new Vector3(0f, 1f, 0.2f);
        d.Sky(C(0.05f, 0.2f, 0.35f), C(0.1f, 0.3f, 0.45f), C(0.2f, 0.45f, 0.55f), C(0.1f, 0.25f, 0.35f), Vector3.up, Color.black);
        var look = DoorLook.Make("Orange door", C(1f, 0.55f, 0.25f), C(1f, 0.65f, 0.35f));
        var sand = d.Surface("Sand tiles", C(0.82f, 0.76f, 0.62f), C(0.78f, 0.72f, 0.60f), C(0.62f, 0.58f, 0.50f), 0.5f, 0.015f, 0f, default, 0.7f, H(0.6f, 1.2f, 1.3f, 1f));
        var wall = d.Surface("Sea-glass tiles", C(0.45f, 0.62f, 0.68f), C(0.42f, 0.58f, 0.64f), C(0.3f, 0.42f, 0.48f), 0.25f, 0.04f, 0f, default, 0.4f, H(0.5f, 1f, 1.1f, 1f));
        d.Room(W, L, Ht, sand, wall, null, DoorLook.WayBack.opening, look.opening);
        var water = d.Pane("Water ceiling", H(0.3f, 0.8f, 1.2f, 1f), H(0.05f, 0.2f, 0.5f, 1f), H(0.6f, 1.2f, 1.4f, 1f), new Vector2(2f * W, 10f), 1f);
        foreach (var z in Doorspace.Every(5f, L - 5f, 10f)) d.Quad("Water overhead", new Vector3(0f, Ht, z), Quaternion.Euler(-90f, 0f, 0f), new Vector2(2f * W + 0.6f, 10f), water);
        var shaft = d.Glow("Light shaft", H(0.5f, 0.9f, 1f, 0.35f), 1.2f);
        var rnd = new System.Random(20);
        foreach (var z in Doorspace.Every(8f, L - 8f, 9f)) d.Quad("Light shaft", new Vector3((float)(rnd.NextDouble() * 10 - 5), Ht / 2f, z), Quaternion.Euler(0f, (float)rnd.NextDouble() * 180f, 12f), new Vector2(2.4f, Ht + 1f), shaft);
        RoomDoors(d, L, look);
        d.Motes("Bubbles", new Vector3(0f, 1.5f, L / 2f), new Vector3(2f * W - 1f, 2.5f, L - 6f), H(0.7f, 1.3f, 1.5f, 1f), 50f, 0.3f, 0.05f, 500);
        d.Spot(new Vector3(-W + 0.4f, 0.05f, L * 0.44f));
        d.Report.Add("- A " + L + " m room under a ceiling of moving water; light shafts every 9 m; caustics on the sand tiles.");
    }

    // 21 Deer / Wind Telephone: "A disconnected telephone rests on a small grassy hill. A soft listening interaction."
    // After the real wind phone in Otsuchi, Japan (Itaru Sasaki's "kaze no denwa"), treated gently: a long climb up a hill
    // of soft grass to a pale phone booth whose line goes nowhere, and the door beside it.
    static void B21(Doorspace d)
    {
        const float L = 260f, hill = 30f;
        d.Haze = C(0.85f, 0.78f, 0.88f); d.HazeStart = 30f; d.HazeEnd = 300f; d.Light = new Vector3(-0.4f, 0.6f, 0.6f);
        d.Sky(C(0.35f, 0.40f, 0.70f), C(0.62f, 0.60f, 0.85f), C(1f, 0.80f, 0.72f), C(0.70f, 0.72f, 0.80f), new Vector3(0.6f, 0.08f, 1f), C(1f, 0.8f, 0.6f), 0.3f, 0.2f, 0f, 0.5f);
        var look = DoorLook.Make("Coral door", C(0.98f, 0.48f, 0.42f), C(1f, 0.6f, 0.5f));
        float Hh(float x, float z) => hill * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(z / L)) * Mathf.Clamp01(1f - Mathf.Abs(x) / 160f) + 1.2f * Mathf.PerlinNoise(x * 0.03f + 5f, z * 0.03f);
        float h0 = Hh(0f, 0f);
        float Gh(float x, float z) => Hh(x, z) - h0;
        float PathX(float z) => 4f * Mathf.Sin(z * 0.03f);
        d.Ground("Hill", -200f, 200f, -60f, L + 120f, 2f, Gh, d.Surface("Hill ground", C(0.56f, 0.70f, 0.64f), C(0.60f, 0.66f, 0.72f), default, 2.2f, 0f));
        d.Tufts("Soft hill grass", -30f, 30f, -6f, L + 6f, (x, z) => { float pd = Mathf.Abs(x - PathX(z)); return pd < 0.8f ? 0f : pd < 6f ? 3.5f : Mathf.Lerp(3.5f, 0f, (pd - 6f) / 14f); }, Gh,
                C(0.22f, 0.36f, 0.40f), C(0.55f, 0.80f, 0.72f), C(0.92f, 0.86f, 1f), 0.4f, 0.9f, 21);
        var booth = d.Lit("Pale booth", C(0.95f, 0.96f, 0.97f)); var glass = d.Glass("Booth glass", C(0.9f, 0.95f, 1f), 0.15f); var phone = d.Lit("Old phone", C(0.15f, 0.15f, 0.18f));
        float bz = L - 6f, by = Gh(PathX(bz) - 3f, bz); var bc = new Vector3(PathX(bz) - 3f, by, bz);
        foreach (var (dx, dz) in new[] { (-0.5f, -0.5f), (0.5f, -0.5f), (-0.5f, 0.5f), (0.5f, 0.5f) }) d.Box("Booth post", bc + new Vector3(dx, 1.15f, dz), new Vector3(0.08f, 2.3f, 0.08f), booth, true);
        d.Box("Booth roof", bc + new Vector3(0f, 2.35f, 0f), new Vector3(1.15f, 0.12f, 1.15f), booth);
        foreach (var (dx, dz, sx, sz) in new[] { (0f, 0.5f, 1f, 0.03f), (-0.5f, 0f, 0.03f, 1f), (0f, -0.5f, 1f, 0.03f) }) d.Box("Booth glass", bc + new Vector3(dx, 1.2f, dz), new Vector3(sx, 2.1f, sz), glass);
        d.Box("Telephone (no line)", bc + new Vector3(-0.42f, 1.3f, 0f), new Vector3(0.12f, 0.28f, 0.2f), phone);
        var fwdAt = new Vector3(PathX(L), Gh(PathX(L), L), L);
        FieldDoors(d, new Vector3(0f, 0f, 2f), fwdAt, look);
        d.Motes("Petals on the wind", new Vector3(0f, hill / 2f + 2f, L / 2f), new Vector3(40f, hill + 6f, L), H(1f, 0.9f, 0.95f, 0.8f), 30f, 0.05f, 0.04f, 300, true);
        d.Spot(bc + new Vector3(0.9f, 0.05f, 0.6f));
        d.Report.Add("- A " + L + " m climb up a " + hill + " m hill of soft grass to a pale phone booth whose line goes nowhere, the door beside it.");
    }

    // 22 Firefly / Sleeping Arcade: "Four silent arcade cabinets in an otherwise empty room. Screens show slow abstract landscapes."
    static void B22(Doorspace d)
    {
        const float W = 10f, L = 180f, Ht = 4f;
        d.Haze = C(0.06f, 0.04f, 0.12f); d.HazeStart = 10f; d.HazeEnd = 130f; d.Light = new Vector3(0.1f, 0.9f, 0.3f);
        d.Sky(C(0.03f, 0.02f, 0.06f), C(0.05f, 0.03f, 0.1f), C(0.08f, 0.05f, 0.14f), C(0.04f, 0.03f, 0.08f), Vector3.up, Color.black);
        var look = DoorLook.Make("Magenta door", C(0.95f, 0.22f, 0.70f), C(1f, 0.35f, 0.9f));
        var carpet = d.Surface("Arcade carpet", C(0.10f, 0.08f, 0.20f), C(0.16f, 0.06f, 0.22f), default, 0.12f, 0f, 0f, default, 0.5f, H(0.6f, 0.3f, 1f, 1f));
        d.Room(W, L, Ht, carpet, d.Lit("Dark violet", C(0.08f, 0.05f, 0.14f)), d.Lit("Black ceiling", C(0.03f, 0.02f, 0.05f)), DoorLook.WayBack.opening, look.opening);
        var body = d.Lit("Cabinet", C(0.10f, 0.08f, 0.16f));
        var screens = new[] {
            d.Pane("Screen dusk", H(1.2f, 0.5f, 1.0f, 1f), H(0.2f, 0.1f, 0.5f, 1f), H(1f, 0.8f, 1.4f, 1f), new Vector2(0.6f, 0.5f), 1f),
            d.Pane("Screen sea", H(0.3f, 0.9f, 1.2f, 1f), H(0.05f, 0.2f, 0.5f, 1f), H(0.7f, 1.2f, 1.4f, 1f), new Vector2(0.6f, 0.5f), 1f),
            d.Pane("Screen ember", H(1.3f, 0.6f, 0.3f, 1f), H(0.4f, 0.1f, 0.2f, 1f), H(1.4f, 1f, 0.6f, 1f), new Vector2(0.6f, 0.5f), 1f) };
        var marquee = d.Lit("Marquee", C(1f, 0.6f, 0.9f), H(1f, 0.4f, 0.8f, 1.4f), 1f, false);
        int n = 0;
        foreach (var z in Doorspace.Every(4f, L - 4f, 1.2f))
            foreach (var s in new[] { -1f, 1f })
            {
                float x = s * 3.5f;
                d.Box("Cabinet", new Vector3(x, 0.9f, z), new Vector3(0.8f, 1.8f, 0.9f), body);
                d.Quad("Screen", new Vector3(x - s * 0.41f, 1.25f, z), Quaternion.Euler(0f, s < 0 ? 90f : -90f, 0f), new Vector2(0.62f, 0.5f), screens[n % 3]);
                d.Box("Marquee", new Vector3(x - s * 0.3f, 1.75f, z), new Vector3(0.24f, 0.14f, 0.8f), marquee);
                n++;
            }
        foreach (var s in new[] { -1f, 1f }) d.Box("Cabinet row (collider)", new Vector3(s * 3.5f, 0.9f, L / 2f), new Vector3(0.8f, 1.8f, L - 6f), body, true).GetComponent<MeshRenderer>().enabled = false;
        RoomDoors(d, L, look);
        d.Motes("Fireflies", new Vector3(0f, 1.6f, L / 2f), new Vector3(2f * W - 1f, 2.6f, L - 4f), H(0.9f, 1f, 0.45f, 1.6f), 40f, 0.03f, 0.04f, 400, true);
        d.Spot(new Vector3(-3.5f, 1.85f, 92f));
        d.Report.Add("- A " + L + " m arcade, " + n + " sleeping cabinets in two long rows, their screens drifting through slow landscapes.");
    }

    // 23 Kangaroo / Low-Gravity Court: "An empty sports court where one ball pauses mid-bounce. An optional low-gravity zone."
    static void B23(Doorspace d)
    {
        const float W = 16f, L = 240f, Ht = 12f;
        d.Haze = C(0.80f, 0.84f, 0.92f); d.HazeStart = 18f; d.HazeEnd = 200f; d.Light = new Vector3(0.2f, 0.9f, -0.3f);
        d.Sky(C(0.6f, 0.7f, 0.85f), C(0.75f, 0.8f, 0.9f), C(0.9f, 0.9f, 0.95f), C(0.8f, 0.8f, 0.85f), Vector3.up, Color.black);
        var look = DoorLook.Make("White door", C(0.96f, 0.96f, 1f), C(0.85f, 0.92f, 1f));
        var wood = d.Surface("Court wood", C(0.82f, 0.64f, 0.44f), C(0.78f, 0.60f, 0.42f), C(0.66f, 0.50f, 0.36f), 0.25f, 0.01f, 0.45f, H(1f, 0.9f, 0.8f, 1f));
        d.Room(W, L, Ht, wood, d.Lit("Pale blue walls", C(0.70f, 0.80f, 0.92f)), d.Lit("Gym ceiling", C(0.86f, 0.88f, 0.92f)), DoorLook.WayBack.opening, look.opening);
        var line = d.Lit("Court line", C(0.98f, 0.98f, 0.98f)); var hoop = d.Lit("Hoop", C(1f, 0.45f, 0.2f)); var board = d.Lit("Backboard", C(0.96f, 0.96f, 0.98f));
        var ball = d.Lit("Ball", C(1f, 0.52f, 0.2f), C(0.15f, 0.05f, 0f)); var lamp = d.Lit("Gym light", C(1f, 1f, 0.95f), H(1f, 1f, 0.9f, 1.5f), 1f, false);
        int courts = 0;
        foreach (var zc in Doorspace.Every(18f, L - 18f, 30f))
        {
            foreach (var s in new[] { -1f, 1f }) { d.Box("Sideline", new Vector3(s * 7f, 0.005f, zc), new Vector3(0.06f, 0.01f, 26f), line, false); d.Box("Baseline", new Vector3(0f, 0.005f, zc + s * 13f), new Vector3(14f, 0.01f, 0.06f), line, false); }
            d.Box("Half line", new Vector3(0f, 0.005f, zc), new Vector3(14f, 0.01f, 0.06f), line, false);
            foreach (var s in new[] { -1f, 1f })
            {
                d.Rod("Hoop pole", new Vector3(0f, 0f, zc + s * 13.8f), new Vector3(0f, 3.4f, zc + s * 13.8f), 0.07f, board);
                d.Box("Backboard", new Vector3(0f, 3.4f, zc + s * 13.3f), new Vector3(1.8f, 1.05f, 0.05f), board);
                d.Disc("Hoop", new Vector3(0f, 3.05f, zc + s * 13f), 0.23f, 0.02f, Vector3.up, hoop);
            }
            d.Ball("Ball (paused mid-bounce)", new Vector3(1.2f, 2.6f, zc - 3f), Vector3.one * 0.24f, ball);
            d.Disc("Ball shadow", new Vector3(1.2f, 0.006f, zc - 3f), 0.16f, 0.002f, Vector3.up, d.Lit("Shadow", C(0.5f, 0.38f, 0.28f)));
            courts++;
        }
        foreach (var z in Doorspace.Every(6f, L - 6f, 12f)) foreach (var x in new[] { -8f, 0f, 8f }) d.Box("Gym light", new Vector3(x, Ht - 0.1f, z), new Vector3(1.2f, 0.1f, 1.2f), lamp);
        d.Disc("Low gravity circle (marked)", new Vector3(0f, 0.012f, L / 2f + 3f), 3f, 0.01f, Vector3.up, d.Glow("Low gravity ring", H(0.7f, 0.85f, 1f, 0.22f), 0.6f));
        RoomDoors(d, L, look);
        d.Spot(new Vector3(-6.6f, 0.05f, 108f));
        d.Report.Add("- A " + L + " m gym hall, " + courts + " courts in a row, at each a ball paused mid-bounce; a marked low-gravity circle.");
    }

    // 24 Scarab / Ribbon Dunes: "Combed sand ridges and a simple winding path. A faint moving ribbon of light follows the route."
    static void B24(Doorspace d)
    {
        const float L = 400f;
        d.Haze = C(0.98f, 0.82f, 0.70f); d.HazeStart = 40f; d.HazeEnd = 420f; d.Light = new Vector3(-0.6f, 0.5f, 0.6f);
        d.Sky(C(0.40f, 0.55f, 0.85f), C(0.70f, 0.75f, 0.92f), C(1f, 0.80f, 0.62f), C(0.95f, 0.80f, 0.68f), new Vector3(-0.5f, 0.12f, 1f), C(1f, 0.8f, 0.55f), 0f, 0f, 0f, 0.4f);
        var look = DoorLook.Make("Blue door", C(0.25f, 0.55f, 0.98f), C(0.4f, 0.7f, 1f));
        float PathX(float z) => 9f * Mathf.Sin(z * 0.024f) + 3f * Mathf.Sin(z * 0.07f + 1f) - 3f * Mathf.Sin(1f);
        float Dune(float x, float z) => 3.2f * Mathf.Sin(x * 0.045f + 1.2f * Mathf.Sin(z * 0.018f)) + 2.2f * Mathf.Sin(z * 0.03f + x * 0.012f) + 0.35f * Mathf.Sin(x * 0.9f + z * 0.25f);
        float pathY(float z) => 0.6f * Mathf.Sin(z * 0.02f);
        float Gh(float x, float z) { float pd = Mathf.Abs(x - PathX(z)); float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((pd - 1.5f) / 14f)); return Mathf.Lerp(pathY(z), Dune(x, z) + 1.5f, k) - pathY(0f); }
        d.Ground("Dunes", -250f, 250f, -60f, L + 60f, 2f, Gh, d.Surface("Combed sand", C(0.94f, 0.76f, 0.58f), C(0.90f, 0.71f, 0.54f), default, 0.9f, 0f, 0.2f, H(1f, 0.85f, 0.7f, 1f)));
        var ribbon = d.Glow("Ribbon of light", H(1f, 0.85f, 0.6f, 0.8f), 0.9f);
        foreach (var z in Doorspace.Every(2f, L - 4f, 4f))
        {
            float x0 = PathX(z), x1 = PathX(z + 4f); var a = new Vector3(x0, Gh(x0, z) + 0.04f, z); var b = new Vector3(x1, Gh(x1, z + 4f) + 0.04f, z + 4f);
            d.Quad("Ribbon", (a + b) / 2f, Quaternion.LookRotation(Vector3.down, b - a), new Vector2(0.35f, Vector3.Distance(a, b) + 0.05f), ribbon);
        }
        var fwdAt = new Vector3(PathX(L), Gh(PathX(L), L), L);
        FieldDoors(d, new Vector3(0f, 0f, 2f), fwdAt, look);
        d.Motes("Sand glitter", new Vector3(0f, 1f, L / 2f), new Vector3(40f, 2f, L), H(1f, 0.9f, 0.7f, 1.1f), 50f, 0.02f, 0.02f, 400, true);
        d.Spot(new Vector3(PathX(205f) + 2.5f, Gh(PathX(205f) + 2.5f, 205f) + 0.05f, 205f));
        d.Report.Add("- " + L + " m of winding path through combed dunes, a faint ribbon of light laid along it.");
    }

    // 25 Praying Mantis / Frosted Office: "Nearly empty office partitions and one abandoned chair. Particles trace the door edge."
    static void B25(Doorspace d)
    {
        const float W = 12f, L = 200f, Ht = 3.2f;
        d.Haze = C(0.80f, 0.84f, 0.90f); d.HazeStart = 14f; d.HazeEnd = 150f; d.Light = new Vector3(0.2f, 0.9f, -0.3f);
        d.Sky(C(0.5f, 0.55f, 0.65f), C(0.65f, 0.7f, 0.8f), C(0.8f, 0.82f, 0.88f), C(0.6f, 0.62f, 0.7f), Vector3.up, Color.black);
        var look = DoorLook.Make("Lilac door", C(0.75f, 0.62f, 0.98f), C(0.85f, 0.72f, 1f));
        var carpet = d.Surface("Office carpet", C(0.40f, 0.45f, 0.54f), C(0.38f, 0.43f, 0.52f), C(0.32f, 0.36f, 0.44f), 0.5f, 0.01f);
        var tiles = d.Surface("Ceiling panels", C(0.92f, 0.93f, 0.94f), C(0.90f, 0.91f, 0.92f), C(0.7f, 0.72f, 0.74f), 0.6f, 0.03f);
        d.Room(W, L, Ht, carpet, d.Lit("Office wall", C(0.86f, 0.87f, 0.90f)), tiles, DoorLook.WayBack.opening, look.opening);
        var frost = d.Glass("Frosted glass", C(0.90f, 0.94f, 0.98f), 0.22f, 0.55f);
        var deskM = d.Lit("Desk", C(0.80f, 0.78f, 0.74f)); var panel = d.Lit("Panel light", C(1f, 1f, 1f), H(0.95f, 1f, 1f, 1.4f), 1f, false); var chairM = d.Lit("Chair", C(0.20f, 0.22f, 0.28f));
        int cubes = 0;
        foreach (var z in Doorspace.Every(6f, L - 6f, 3f))
            foreach (var s in new[] { -1f, 1f })
                for (int c = 0; c < 3; c++)
                {
                    float x = s * (3f + c * 3f);
                    d.Box("Partition", new Vector3(x + s * 1.45f, 0.75f, z), new Vector3(0.05f, 1.5f, 2.9f), frost);
                    d.Box("Partition", new Vector3(x, 0.75f, z + 1.45f), new Vector3(2.9f, 1.5f, 0.05f), frost);
                    d.Box("Desk", new Vector3(x + s * 0.8f, 0.72f, z), new Vector3(1.1f, 0.05f, 2.2f), deskM);
                    cubes++;
                }
        foreach (var z in Doorspace.Every(3f, L - 3f, 3f)) foreach (var x in new[] { -6f, 0f, 6f }) d.Box("Panel light", new Vector3(x, Ht - 0.02f, z), new Vector3(1.2f, 0.03f, 0.6f), panel);
        // the one abandoned chair, pushed out into the aisle halfway down
        d.Cyl("The abandoned chair", new Vector3(-1.4f, 0.24f, L / 2f), 0.28f, 0.06f, chairM);
        d.Box("Chair seat", new Vector3(-1.4f, 0.48f, L / 2f), new Vector3(0.5f, 0.08f, 0.5f), chairM);
        d.Box("Chair back", new Vector3(-1.4f, 0.8f, L / 2f + 0.25f), new Vector3(0.5f, 0.55f, 0.06f), chairM);
        RoomDoors(d, L, look);
        d.Spot(new Vector3(-1.4f, 0.55f, L / 2f + 0.1f));
        d.Report.Add("- A " + L + " m office of frosted cubicles (" + cubes + "), empty but for one chair halfway down.");
    }
}
