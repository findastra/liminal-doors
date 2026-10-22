// Claude, 2026-10-19. Doorspaces 26-33 and 36, each from its concept floor, made liminal. See LiminalDoorspaces.cs.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class LiminalDoorspaces
{
    // 26 Flamingo / Red Sun Reservoir: "A dry causeway crosses a shallow concrete reservoir. Slow luminous rings travel across the water."
    static void B26(Doorspace d)
    {
        const float L = 400f;
        d.Haze = C(0.95f, 0.62f, 0.55f); d.HazeStart = 30f; d.HazeEnd = 380f; d.Light = new Vector3(0f, 0.4f, -1f);
        d.Sky(C(0.35f, 0.18f, 0.40f), C(0.80f, 0.40f, 0.45f), C(1f, 0.55f, 0.35f), C(0.80f, 0.45f, 0.40f), new Vector3(0f, 0.06f, 1f), C(1f, 0.45f, 0.3f), 0f, 0f, 0f, 0.5f);
        var look = DoorLook.Make("Pink door", C(0.98f, 0.40f, 0.60f), C(1f, 0.5f, 0.75f));
        var concrete = d.Surface("Reservoir concrete", C(0.72f, 0.62f, 0.60f), C(0.68f, 0.58f, 0.57f), C(0.55f, 0.47f, 0.46f), 4f, 0.01f);
        d.Slab("Reservoir floor", -700f, 700f, -0.7f, -0.4f, -200f, L + 900f, concrete, false);
        d.Slab("Shallow water", -700f, 700f, -0.13f, -0.1f, -200f, L + 900f, d.Water("Red water", C(0.6f, 0.30f, 0.35f), H(1f, 0.5f, 0.45f, 1f), 0.45f), false);
        d.Slab("Causeway", -1.6f, 1.6f, -0.7f, 0f, -4f, L + 4f, d.Surface("Causeway", C(0.86f, 0.78f, 0.74f), C(0.82f, 0.74f, 0.71f), C(0.6f, 0.54f, 0.52f), 2f, 0.015f));
        var bollard = d.Lit("Bollard", C(0.80f, 0.74f, 0.70f));
        foreach (var z in Doorspace.Every(0f, L, 10f)) foreach (var s in new[] { -1f, 1f }) d.Cyl("Bollard", new Vector3(s * 1.45f, 0.25f, z), 0.08f, 0.5f, bollard);
        foreach (var s in new[] { -1f, 1f }) d.Box("Causeway edge (collider)", new Vector3(s * 1.7f, 0.6f, L / 2f), new Vector3(0.1f, 1.2f, L + 8f), bollard, true).GetComponent<MeshRenderer>().enabled = false;
        d.Ball("The red sun", new Vector3(0f, 40f, 1700f), Vector3.one * 320f, d.Lit("Red sun", C(1f, 0.3f, 0.2f), H(1f, 0.25f, 0.15f, 1.6f), 1f, false));
        var ring = d.Glow("Luminous ring", H(1f, 0.55f, 0.6f, 0.6f), 0.5f);
        var rnd = new System.Random(26);
        for (int i = 0; i < 40; i++) d.Quad("Luminous ring", new Vector3((float)(rnd.NextDouble() * 120 - 60), -0.08f, (float)(rnd.NextDouble() * L)), Quaternion.Euler(90f, 0f, 0f), Vector2.one * (4f + (float)rnd.NextDouble() * 10f), ring);
        FieldDoors(d, new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, L), look);
        d.Spot(new Vector3(1.3f, 0.05f, L * 0.46f));
        d.Report.Add("- A " + L + " m causeway across a shallow reservoir toward a red sun 320 m wide; faint rings on the water.");
    }

    // 27 Jellyfish / Room of Curtains: "Tall woven curtains divide a vast, sparse room. Quiet fabric movement leaves the route readable."
    static void B27(Doorspace d)
    {
        const float W = 20f, L = 200f, Ht = 14f;
        d.Haze = C(0.92f, 0.84f, 0.84f); d.HazeStart = 14f; d.HazeEnd = 160f; d.Light = new Vector3(0.3f, 0.8f, -0.4f);
        d.Sky(C(0.6f, 0.55f, 0.6f), C(0.75f, 0.7f, 0.72f), C(0.9f, 0.85f, 0.85f), C(0.7f, 0.65f, 0.68f), Vector3.up, Color.black);
        var look = DoorLook.Make("Blue door", C(0.30f, 0.55f, 0.98f), C(0.45f, 0.7f, 1f));
        var wood = d.Surface("Pale floorboards", C(0.84f, 0.74f, 0.64f), C(0.80f, 0.70f, 0.61f), C(0.62f, 0.54f, 0.48f), 0.3f, 0.012f, 0.2f, H(1f, 0.9f, 0.85f, 1f));
        d.Room(W, L, Ht, wood, d.Lit("Pale wall", C(0.90f, 0.86f, 0.84f)), d.Lit("High ceiling", C(0.40f, 0.36f, 0.38f)), DoorLook.WayBack.opening, look.opening);
        var cloth = new[] { d.Lit("Curtain blush", C(0.98f, 0.86f, 0.86f), C(0.06f, 0.04f, 0.04f), 0.6f), d.Lit("Curtain cream", C(0.98f, 0.94f, 0.88f), C(0.05f, 0.05f, 0.04f), 0.6f) };
        int row = 0, pleats = 0;
        foreach (var z in Doorspace.Every(10f, L - 10f, 10f))
        {
            float gap = (row % 2 == 0 ? -1f : 1f) * (6f + 6f * Mathf.Sin(row * 1.7f));   // the way through moves from side to side
            for (float x = -W + 0.2f; x < W - 0.2f; x += 0.42f)
            {
                if (Mathf.Abs(x - gap) < 1.6f) continue;
                int k = Mathf.Abs(Mathf.RoundToInt(x / 0.42f));
                d.Box("Pleat", new Vector3(x, (Ht - 0.6f) / 2f, z + (k % 2 == 0 ? 0.08f : -0.08f)), new Vector3(0.42f, Ht - 0.6f, 0.05f), cloth[(row + k) % 2]);
                pleats++;
            }
            d.Slab("Curtain row " + row + " (collider)", -W, gap - 1.6f, 0f, Ht - 0.6f, z - 0.15f, z + 0.15f, cloth[0]).GetComponent<MeshRenderer>().enabled = false;
            d.Slab("Curtain row " + row + " (collider) r", gap + 1.6f, W, 0f, Ht - 0.6f, z - 0.15f, z + 0.15f, cloth[0]).GetComponent<MeshRenderer>().enabled = false;
            row++;
        }
        RoomDoors(d, L, look);
        d.Motes("Drifting lights", new Vector3(0f, 5f, L / 2f), new Vector3(2f * W - 2f, 9f, L - 6f), H(1f, 0.8f, 0.95f, 1f), 30f, 0.08f, 0.06f, 300, true, 8f);
        d.Spot(new Vector3(-W + 0.6f, 0.05f, 101f));
        d.Report.Add("- A " + L + " m room " + Ht + " m high, " + row + " rows of tall curtains (" + pleats + " pleats); the gap through each moves side to side.");
    }

    // 28 Butterfly / Glass Orchard: "A few metallic trees carry translucent leaves. Nearby leaves brighten when the door wakes."
    // Here: rows and rows of them, 300 m of orchard.
    static void B28(Doorspace d)
    {
        const float L = 300f;
        d.Haze = C(0.86f, 0.80f, 0.92f); d.HazeStart = 25f; d.HazeEnd = 260f; d.Light = new Vector3(0.4f, 0.7f, 0.3f);
        d.Sky(C(0.35f, 0.38f, 0.70f), C(0.62f, 0.58f, 0.85f), C(0.98f, 0.82f, 0.80f), C(0.78f, 0.72f, 0.85f), new Vector3(0.6f, 0.15f, 1f), C(1f, 0.85f, 0.7f), 0.3f, 0.3f, 0f, 0.4f);
        var look = DoorLook.Make("Amber door", C(1f, 0.70f, 0.30f), C(1f, 0.80f, 0.45f));
        d.Slab("Orchard ground", -200f, 200f, -0.3f, 0f, -60f, L + 200f, d.Surface("Lilac grass", C(0.66f, 0.62f, 0.78f), C(0.62f, 0.60f, 0.74f), default, 2f, 0f));
        d.Slab("Path", -0.9f, 0.9f, 0f, 0.01f, -2f, L, d.Surface("Path", C(0.86f, 0.82f, 0.88f), C(0.82f, 0.80f, 0.86f), default, 1f, 0f), false);
        var metal = d.Lit("Silver bark", C(0.82f, 0.84f, 0.90f), C(0.08f, 0.08f, 0.1f), 0.4f);
        var leaves = new[] { d.Glass("Glass leaves mint", C(0.62f, 1f, 0.86f), 0.35f), d.Glass("Glass leaves rose", C(1f, 0.72f, 0.88f), 0.35f) };
        int trees = 0;
        foreach (var z in Doorspace.Every(3f, L - 3f, 6f))
            foreach (var x in new[] { -22f, -16f, -10f, -4f, 4f, 10f, 16f, 22f })
            {
                d.Cyl("Trunk", new Vector3(x, 1.2f, z), 0.12f, 2.4f, metal, true);
                d.Rod("Branch", new Vector3(x, 1.8f, z), new Vector3(x + 0.7f, 2.6f, z + 0.2f), 0.05f, metal);
                d.Ball("Glass crown", new Vector3(x, 3.0f, z), new Vector3(2.4f, 1.9f, 2.4f), leaves[trees % 2]);
                trees++;
            }
        FieldDoors(d, new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, L), look);
        d.Motes("Fairy lights", new Vector3(0f, 2.6f, L / 2f), new Vector3(46f, 3f, L), H(1f, 0.85f, 0.6f, 1.6f), 70f, 0.01f, 0.03f, 700, true);
        d.Spot(new Vector3(-4f, 3.2f, 153f));
        d.Report.Add("- A " + L + " m orchard of " + trees + " silver trees with glass leaves, fairy lights among them.");
    }

    // 29 Red Panda / Midnight Food Court: "Three closed counters and a handful of empty tables. Rippled metal shutters, a refrigerator hum."
    static void B29(Doorspace d)
    {
        const float W = 16f, L = 220f, Ht = 6f;
        d.Haze = C(0.10f, 0.16f, 0.18f); d.HazeStart = 12f; d.HazeEnd = 150f; d.Light = new Vector3(0.2f, 0.9f, -0.2f);
        d.Sky(C(0.05f, 0.06f, 0.08f), C(0.08f, 0.1f, 0.12f), C(0.12f, 0.15f, 0.18f), C(0.06f, 0.08f, 0.1f), Vector3.up, Color.black);
        var look = DoorLook.Make("Green door", C(0.30f, 0.90f, 0.55f), C(0.45f, 1f, 0.7f));
        var floor = d.Surface("Food court terrazzo", C(0.30f, 0.30f, 0.34f), C(0.26f, 0.27f, 0.31f), default, 0.06f, 0f, 0.4f, H(0.5f, 0.8f, 0.8f, 1f));
        d.Room(W, L, Ht, floor, d.Lit("Food court wall", C(0.22f, 0.24f, 0.28f)), d.Lit("Ceiling", C(0.12f, 0.13f, 0.15f)), DoorLook.WayBack.opening, look.opening);
        var shutter = d.Surface("Rippled shutter", C(0.60f, 0.62f, 0.66f), C(0.58f, 0.60f, 0.64f), C(0.35f, 0.37f, 0.40f), 0.08f, 0.22f);
        var counter = d.Lit("Counter", C(0.40f, 0.30f, 0.30f)); var sign = d.Lit("Sign (off)", C(0.18f, 0.16f, 0.2f), C(0.04f, 0.02f, 0.05f));
        var fridge = d.Lit("Fridge glow", C(0.85f, 1f, 1f), H(0.6f, 0.9f, 1f, 1.2f), 1f, false);
        var table = d.Lit("Table", C(0.75f, 0.72f, 0.70f)); var chair = d.Lit("Chair", C(0.85f, 0.45f, 0.35f));
        int stalls = 0;
        foreach (var z in Doorspace.Every(5f, L - 5f, 8f))
            foreach (var s in new[] { -1f, 1f })
            {
                float x = s * (W - 1f);
                d.Box("Counter", new Vector3(x, 0.5f, z), new Vector3(1.8f, 1f, 6.6f), counter, true);
                d.Box("Shutter", new Vector3(x, 2.4f, z), new Vector3(0.2f, 2.8f, 6.4f), shutter);
                d.Box("Sign", new Vector3(x - s * 0.2f, 4.3f, z), new Vector3(0.12f, 0.8f, 5f), sign);
                if (stalls % 5 == 2) d.Box("Fridge", new Vector3(x - s * 0.95f, 0.8f, z + 2f), new Vector3(0.08f, 1.4f, 1.6f), fridge);
                stalls++;
            }
        foreach (var z in Doorspace.Every(6f, L - 6f, 5f))
            foreach (var x in new[] { -8f, -4f, 0f, 4f, 8f })
            {
                if (Mathf.Abs(x) < 0.5f && ((int)z) % 10 < 5) continue;   // a clear way down the middle
                d.Cyl("Table", new Vector3(x, 0.74f, z), 0.45f, 0.04f, table);
                d.Cyl("Table leg", new Vector3(x, 0.37f, z), 0.05f, 0.74f, table, true);
                foreach (var (cx, cz) in new[] { (0.75f, 0f), (-0.75f, 0f), (0f, 0.75f), (0f, -0.75f) }) d.Box("Chair", new Vector3(x + cx, 0.45f, z + cz), new Vector3(0.4f, 0.06f, 0.4f), chair);
            }
        RoomDoors(d, L, look);
        d.Spot(new Vector3(4f, 0.8f, 111f));
        d.Report.Add("- A " + L + " m food court at midnight, " + stalls + " stalls with their shutters down, tables in rows down the middle.");
    }

    // 30 Swan / Door at Sunrise: "A long promenade opens into a quiet sunrise. A calm pause in the random route."
    static void B30(Doorspace d)
    {
        const float L = 500f;
        d.Haze = C(0.98f, 0.82f, 0.78f); d.HazeStart = 40f; d.HazeEnd = 420f; d.Light = new Vector3(0f, 0.4f, -1f);
        d.Sky(C(0.45f, 0.48f, 0.80f), C(0.82f, 0.68f, 0.85f), C(1f, 0.78f, 0.60f), C(0.92f, 0.72f, 0.70f), new Vector3(0f, 0.05f, 1f), C(1f, 0.85f, 0.6f), 0.2f, 0.2f, 0f, 0.5f);
        var look = DoorLook.Make("Rose door", C(0.98f, 0.55f, 0.65f), C(1f, 0.65f, 0.75f));
        d.Slab("Calm water", -900f, 900f, -1.03f, -1f, -200f, L + 1200f, d.Water("Sunrise water", C(0.45f, 0.42f, 0.62f), H(1f, 0.75f, 0.65f, 1f), 0.25f), false);
        d.Slab("Promenade", -2f, 2f, -0.3f, 0f, -4f, L + 4f, d.Surface("Promenade boards", C(0.88f, 0.80f, 0.74f), C(0.84f, 0.76f, 0.71f), C(0.66f, 0.58f, 0.55f), 0.3f, 0.04f, 0.3f, H(1f, 0.85f, 0.75f, 1f)));
        var post = d.Lit("Lamp post", C(0.95f, 0.93f, 0.92f)); var lamp = d.Lit("Lamp", C(1f, 0.9f, 0.75f), H(1f, 0.85f, 0.6f, 1.5f), 1f, false);
        foreach (var z in Doorspace.Every(5f, L - 5f, 10f))
            foreach (var s in new[] { -1f, 1f }) { d.Rod("Lamp post", new Vector3(s * 1.85f, 0f, z), new Vector3(s * 1.85f, 2.8f, z), 0.04f, post); d.Ball("Lamp", new Vector3(s * 1.85f, 2.95f, z), Vector3.one * 0.28f, lamp); }
        foreach (var s in new[] { -1f, 1f }) d.Box("Rail (collider)", new Vector3(s * 2.05f, 0.6f, L / 2f), new Vector3(0.1f, 1.2f, L + 8f), post, true).GetComponent<MeshRenderer>().enabled = false;
        FieldDoors(d, new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, L), look);
        d.Motes("Morning glitter", new Vector3(0f, 0.4f, L / 2f), new Vector3(60f, 1f, L), H(1f, 0.9f, 0.75f, 1.4f), 80f, 0.01f, 0.025f, 600, true);
        d.Spot(new Vector3(-1.6f, 0.05f, L * 0.51f));
        d.Report.Add("- A " + L + " m promenade over calm water straight into the sunrise, a lamp pair every 10 m.");
    }

    // 31 Otter / Steam Between Stars: "A warm bathtub in an open, star-filled bathing pavilion."
    static void B31(Doorspace d)
    {
        const float W = 10f, L = 220f, Ht = 5f;
        d.Haze = C(0.10f, 0.08f, 0.22f); d.HazeStart = 30f; d.HazeEnd = 300f; d.Light = new Vector3(0.3f, 0.8f, 0.2f);
        d.Sky(C(0.03f, 0.02f, 0.10f), C(0.06f, 0.04f, 0.16f), C(0.10f, 0.08f, 0.24f), C(0.05f, 0.04f, 0.14f), Vector3.down, Color.black, 1f, 1f, 0.9f, 0f);
        var look = DoorLook.Make("Gold door", C(0.98f, 0.80f, 0.40f), C(1f, 0.88f, 0.5f));
        var stone = d.Surface("Warm stone", C(0.80f, 0.66f, 0.58f), C(0.76f, 0.62f, 0.55f), C(0.60f, 0.48f, 0.44f), 0.8f, 0.02f, 0.4f, H(0.9f, 0.7f, 1f, 1f));
        d.Slab("Pavilion floor", -W, W, -0.6f, 0f, -4f, L + 4f, stone);
        var column = d.Lit("Column", C(0.92f, 0.86f, 0.82f)); var tub = d.Lit("Bathtub", C(0.98f, 0.97f, 0.96f));
        var water = d.Water("Bath water", C(0.40f, 0.70f, 0.85f), H(1f, 0.8f, 1f, 1f), 0.5f);
        foreach (var z in Doorspace.Every(0f, L, 8f))
            foreach (var s in new[] { -1f, 1f })
            {
                d.Cyl("Column", new Vector3(s * (W - 0.5f), Ht / 2f, z), 0.3f, Ht, column, true);
                d.Box("Roof beam", new Vector3(s * (W - 0.5f), Ht + 0.15f, z + 4f), new Vector3(0.5f, 0.3f, 8.2f), column);
            }
        foreach (var z in Doorspace.Every(0f, L, 8f)) d.Box("Cross beam", new Vector3(0f, Ht + 0.15f, z), new Vector3(2f * W, 0.25f, 0.4f), column);
        foreach (var s in new[] { -1f, 1f }) d.Box("Edge (collider)", new Vector3(s * (W + 0.05f), 0.6f, L / 2f), new Vector3(0.1f, 1.2f, L + 8f), column, true).GetComponent<MeshRenderer>().enabled = false;
        int tubs = 0;
        foreach (var z in Doorspace.Every(10f, L - 10f, 12f))
        {
            d.Box("Bathtub", new Vector3(2.4f, 0.32f, z), new Vector3(0.95f, 0.64f, 1.9f), tub, true);
            d.Quad("Bath water", new Vector3(2.4f, 0.58f, z), Quaternion.Euler(90f, 0f, 0f), new Vector2(0.8f, 1.7f), water);
            d.Motes("Steam " + tubs, new Vector3(2.4f, 0.9f, z), new Vector3(0.8f, 0.3f, 1.6f), H(1f, 1f, 1.1f, 0.25f), 4f, 0.35f, 0.5f, 40, false, 5f, 2.4f);
            tubs++;
        }
        FieldDoors(d, new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, L), look, false);
        d.Spot(new Vector3(2.4f, 0.66f, 106f));
        d.Report.Add("- A " + L + " m bathing pavilion floating in stars, " + tubs + " steaming tubs in a row.");
    }

    // 32 Sandpiper / Sunset Sandbar: "A narrow sandbar takes an uncanny zigzag and S-curve through shallow water. No boardwalk."
    static void B32(Doorspace d)
    {
        const float L = 400f, seg = 4f;
        d.Haze = C(0.98f, 0.74f, 0.62f); d.HazeStart = 40f; d.HazeEnd = 420f; d.Light = new Vector3(0f, 0.4f, -1f);
        d.Sky(C(0.38f, 0.30f, 0.65f), C(0.85f, 0.50f, 0.60f), C(1f, 0.70f, 0.45f), C(0.95f, 0.65f, 0.55f), new Vector3(0.2f, 0.04f, 1f), C(1f, 0.7f, 0.4f), 0.1f, 0.1f, 0f, 0.6f);
        var look = DoorLook.Make("Sparkle door", C(1f, 0.85f, 0.55f), C(1f, 0.9f, 0.7f));
        float Px(float z) => z < 200f ? 10f * (2f * Mathf.Abs(2f * (z / 40f - Mathf.Floor(z / 40f + 0.5f))) - 1f) * Mathf.Clamp01(z / 10f) : 14f * Mathf.Sin((z - 200f) * 0.03f) + Px0(200f);
        float Px0(float z) => 10f * (2f * Mathf.Abs(2f * (z / 40f - Mathf.Floor(z / 40f + 0.5f))) - 1f);
        var sand = d.Surface("Wet sand", C(0.92f, 0.76f, 0.60f), C(0.88f, 0.72f, 0.57f), default, 0.8f, 0f, 0.5f, H(1f, 0.8f, 0.6f, 1f));
        d.Slab("Sand bed", -900f, 900f, -0.8f, -0.5f, -200f, L + 900f, sand);
        d.Slab("Shallow water", -900f, 900f, -0.11f, -0.08f, -200f, L + 900f, d.Water("Sunset shallows", C(0.55f, 0.45f, 0.62f), H(1f, 0.7f, 0.5f, 1f), 0.55f), false);
        int n = 0;
        for (float z = -2f; z < L + 2f; z += seg)
        {
            var a = new Vector3(Px(z), 0f, z); var b = new Vector3(Px(z + seg), 0f, z + seg);
            var t = Doorspace.Group("Sandbar " + n, d.Static); t.localPosition = (a + b) / 2f; t.localRotation = Quaternion.LookRotation(b - a);
            float len = Vector3.Distance(a, b) + 0.6f;
            d.Box("Sandbar", new Vector3(0f, -0.25f, 0f), new Vector3(1.6f, 0.5f, len), sand, false, 0f, t);
            d.Collider("Sandbar " + n, t, new Vector3(0f, -0.25f, 0f), new Vector3(1.6f, 0.5f, len));
            n++;
        }
        var fwdAt = new Vector3(Px(L), 0f, L);
        FieldDoors(d, new Vector3(Px(2f), 0f, 2f), fwdAt, look);
        d.Motes("Champagne sparkles", new Vector3(0f, 0.6f, L / 2f), new Vector3(60f, 1.2f, L), H(1f, 0.9f, 0.6f, 1.6f), 90f, 0.03f, 0.025f, 700, true);
        d.Spot(new Vector3(Px(201f) + 0.5f, 0.05f, 201f));
        d.Report.Add("- A sandbar 1.6 m wide zigzagging then S-curving " + L + " m through shallow water at sunset (" + n + " pieces).");
    }

    // 33 Dolphin / Aquaglass Concourse: "An optimistic Aero hall with glass, sky and a shallow water channel."
    static void B33(Doorspace d)
    {
        const float W = 12f, L = 240f, Ht = 9f, ch = 1.6f;
        d.Haze = C(0.86f, 0.95f, 0.98f); d.HazeStart = 22f; d.HazeEnd = 220f; d.Light = new Vector3(0.3f, 0.9f, 0.2f);
        d.Backing = C(0.90f, 0.95f, 0.97f);   // its doors stand in glass walls
        d.Sky(C(0.25f, 0.55f, 0.95f), C(0.55f, 0.80f, 1f), C(0.92f, 0.97f, 1f), C(0.60f, 0.85f, 0.75f), new Vector3(0.3f, 0.7f, 0.5f), C(1f, 1f, 0.9f), 0f, 0f, 0f, 0.6f);
        var look = DoorLook.Make("Aqua door", C(0.30f, 0.85f, 0.95f), C(0.45f, 0.95f, 1f));
        var white = d.Surface("White gloss floor", C(0.97f, 0.98f, 0.99f), C(0.94f, 0.96f, 0.97f), C(0.82f, 0.86f, 0.88f), 0.8f, 0.01f, 0.7f, H(0.6f, 0.9f, 1f, 1f));
        var glass = d.Glass("Aqua glass", C(0.70f, 0.95f, 1f), 0.1f);
        d.Slab("Floor left", -W, -ch, -0.3f, 0f, 0f, L, white);
        d.Slab("Floor right", ch, W, -0.3f, 0f, 0f, L, white);
        d.Slab("Channel bed", -ch, ch, -0.7f, -0.4f, 0f, L, d.Surface("Channel tiles", C(0.55f, 0.88f, 0.95f), C(0.50f, 0.84f, 0.92f), C(0.35f, 0.62f, 0.70f), 0.2f, 0.05f));
        d.Slab("Channel water", -ch, ch, -0.11f, -0.08f, 0f, L, d.Water("Channel water", C(0.35f, 0.85f, 0.95f), H(0.8f, 1f, 1.1f, 1f), 0.65f), false);
        d.Slab("Lawn outside", -500f, 500f, -0.6f, -0.32f, -300f, L + 500f, d.Surface("Mint lawn", C(0.55f, 0.88f, 0.72f), C(0.52f, 0.84f, 0.70f), default, 2.5f, 0f), false);
        d.WallZ("Glass wall left", -W - Doorspace.WallT, -W, L, Ht, null, glass);
        d.WallZ("Glass wall right", W, W + Doorspace.WallT, L, Ht, null, glass);
        const float doorX = -ch - 3f;   // both doors on the left walkway, clear of the water channel
        d.WallX("Glass wall near", -Doorspace.WallT, 0f, W, Ht, DoorLook.WayBack.opening, glass, doorX);
        d.WallX("Glass wall far", L, L + Doorspace.WallT, W, Ht, look.opening, glass, doorX);
        d.Slab("Glass roof", -W - Doorspace.WallT, W + Doorspace.WallT, Ht, Ht + 0.3f, -Doorspace.WallT, L + Doorspace.WallT, glass, false);
        var frame = d.Lit("White frame", C(0.97f, 0.98f, 0.99f)); var shrub = d.Lit("Round shrub", C(0.45f, 0.85f, 0.62f), C(0.03f, 0.08f, 0.05f)); var pot = d.Lit("White planter", C(0.98f, 0.98f, 0.99f));
        foreach (var z in Doorspace.Every(0f, L, 6f)) { foreach (var s in new[] { -1f, 1f }) d.Box("Frame", new Vector3(s * (W + 0.05f), Ht / 2f, z), new Vector3(0.14f, Ht, 0.14f), frame); d.Box("Roof frame", new Vector3(0f, Ht - 0.06f, z), new Vector3(2f * W, 0.12f, 0.14f), frame); }
        foreach (var z in Doorspace.Every(9f, L - 9f, 12f)) foreach (var s in new[] { -1f, 1f }) { d.Cyl("Planter", new Vector3(s * 6.5f, 0.35f, z), 0.7f, 0.7f, pot, true); d.Ball("Shrub", new Vector3(s * 6.5f, 1.15f, z), Vector3.one * 1.4f, shrub); }
        d.Slab("Channel edge (collider) l", -ch - 0.1f, -ch, 0f, 0.6f, 0f, L, frame).GetComponent<MeshRenderer>().enabled = false;
        d.Slab("Channel edge (collider) r", ch, ch + 0.1f, 0f, 0.6f, 0f, L, frame).GetComponent<MeshRenderer>().enabled = false;
        d.Door("Way back door", new Vector3(doorX, 0f, -Doorspace.WallT / 2f), 180f, DoorLook.WayBack, false, true);
        var fwd = d.Door(look.name + " (forward)", new Vector3(doorX, 0f, L + Doorspace.WallT / 2f), 0f, look, true, true);
        d.Points(new Vector3(doorX, 0f, 2f), 0f, fwd);
        d.Motes("Bubbles", new Vector3(0f, 2.5f, L / 2f), new Vector3(2f * W - 2f, 4f, L - 6f), H(0.8f, 1f, 1.1f, 1.2f), 50f, 0.15f, 0.06f, 500, true);
        d.Spot(new Vector3(0f, -0.35f, 118f));
        d.Report.Add("- A " + L + " m glass concourse, a shallow water channel down the middle, white planters every 12 m.");
    }

    // 36 Kingfisher / Blue-Sky Terminal: "An empty organic 2000s terminal with oval sky windows and no grass."
    static void B36(Doorspace d)
    {
        const float W = 14f, L = 260f, Ht = 8f;
        d.Haze = C(0.84f, 0.90f, 0.96f); d.HazeStart = 22f; d.HazeEnd = 230f; d.Light = new Vector3(0.3f, 0.9f, -0.2f);
        d.Sky(C(0.25f, 0.55f, 0.95f), C(0.50f, 0.75f, 1f), C(0.88f, 0.95f, 1f), C(0.60f, 0.78f, 0.95f), new Vector3(0.4f, 0.6f, 0.6f), C(1f, 1f, 0.92f), 0f, 0f, 0f, 0.6f);
        var look = DoorLook.Make("Aqua door", C(0.35f, 0.88f, 0.95f), C(0.5f, 0.95f, 1f));
        var terrazzo = d.Surface("Pale terrazzo", C(0.93f, 0.91f, 0.87f), C(0.86f, 0.85f, 0.83f), default, 0.05f, 0f, 0.5f, H(1f, 0.9f, 0.8f, 1f));
        var orange = d.Lit("Glossy orange", C(1f, 0.55f, 0.24f), C(0.12f, 0.04f, 0f), 0.6f);
        var holes = Windows(8f, L - 8f, 10f, 3.6f, 2.4f, 5.4f);
        d.Room(W, L, Ht, terrazzo, orange, d.Lit("White ceiling", C(0.98f, 0.97f, 0.95f)), DoorLook.WayBack.opening, look.opening, holes, holes);
        var frame = d.Lit("Window frame", C(0.98f, 0.97f, 0.95f)); var bench = d.Lit("Blue bench", C(0.15f, 0.35f, 0.85f), C(0.02f, 0.04f, 0.1f)); var chrome = d.Lit("Chrome", C(0.85f, 0.88f, 0.92f), C(0.1f, 0.1f, 0.12f), 0.4f);
        foreach (var (z0, z1, y0, y1) in holes)
            foreach (var s in new[] { -1f, 1f })
            {
                float zc = (z0 + z1) / 2f, yc = (y0 + y1) / 2f;
                // round off the window with soft white corners, so it reads as an oval
                foreach (var (dz, dy) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) }) d.Ball("Window corner", new Vector3(s * (W + 0.05f), yc + dy * 1.4f, zc + dz * 1.7f), new Vector3(0.5f, 0.6f, 0.7f), frame);
            }
        foreach (var z in Doorspace.Every(10f, L - 10f, 10f))
        {
            d.Box("Bench", new Vector3(0f, 0.4f, z), new Vector3(1.2f, 0.4f, 4f), bench, true);
            d.Ball("Bench end", new Vector3(0f, 0.4f, z - 2f), new Vector3(1.2f, 0.4f, 1.2f), bench); d.Ball("Bench end", new Vector3(0f, 0.4f, z + 2f), new Vector3(1.2f, 0.4f, 1.2f), bench);
        }
        foreach (var s in new[] { -1f, 1f }) d.Rod("Hand rail", new Vector3(s * (W - 0.3f), 1f, 1f), new Vector3(s * (W - 0.3f), 1f, L - 1f), 0.035f, chrome);
        RoomDoors(d, L, look);
        d.Spot(new Vector3(0f, 0.65f, 130f));
        d.Report.Add("- A " + L + " m terminal of glossy orange walls, " + holes.Count * 2 + " rounded sky windows, blue benches every 10 m.");
    }
}
