// Claude, 2026-10-19. Doorspaces 04-14, each from its concept floor (register 2026-10-15), made liminal: far too long,
// repeating into a haze, a glittering door at the end. See LiminalDoorspaces.cs.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static partial class LiminalDoorspaces
{
    // 04 Raccoon / Last Laundrette: "A few enamel washers in a small, vacant laundrette. One washer contains a slowly turning sky."
    // Here: 180 m of laundrette, washers the whole way down the left wall, windows onto a night of clouds on the right.
    static void B04(Doorspace d)
    {
        const float W = 5f, L = 180f, Ht = 3.4f;
        d.Haze = C(0.84f, 0.90f, 0.88f); d.HazeStart = 14f; d.HazeEnd = 150f; d.Light = new Vector3(0.2f, 0.9f, -0.3f);
        d.Sky(C(0.08f, 0.06f, 0.22f), C(0.22f, 0.16f, 0.42f), C(0.52f, 0.40f, 0.68f), C(0.78f, 0.66f, 0.86f), new Vector3(-0.6f, 0.25f, 0.3f), C(0.9f, 0.9f, 1f), 1f, 0.7f, 0.3f, 0.2f);
        var look = DoorLook.Make("Pink door", C(0.98f, 0.36f, 0.58f), C(1f, 0.45f, 0.75f));
        var floor = d.Surface("Terrazzo", C(0.93f, 0.88f, 0.84f), C(0.84f, 0.78f, 0.80f), default, 0.045f, 0f, 0.45f, C(1f, 0.8f, 0.9f));
        var wall = d.Surface("Mint tiles", C(0.62f, 0.84f, 0.76f), C(0.57f, 0.79f, 0.71f), C(0.44f, 0.60f, 0.55f), 0.15f);
        d.Room(W, L, Ht, floor, wall, d.Lit("Ceiling", C(0.93f, 0.93f, 0.88f)), DoorLook.WayBack.opening, look.opening, null, Windows(6f, L - 6f, 9f, 5f, 1.0f, 2.9f));
        var body = d.Lit("Washer enamel", C(0.95f, 0.94f, 0.90f));
        var glass = d.Lit("Washer glass", C(0.16f, 0.18f, 0.24f), C(0.04f, 0.05f, 0.08f));
        var chrome = d.Lit("Chrome", C(0.80f, 0.83f, 0.88f), C(0.1f, 0.1f, 0.12f), 0.4f);
        var tube = d.Lit("Strip light", C(1f, 1f, 1f), H(1f, 1f, 0.95f, 1.6f), 1f, false);
        var basket = d.Lit("Basket", C(0.86f, 0.78f, 0.64f));
        int washers = 0;
        foreach (var z in Doorspace.Every(3f, L - 5f, 0.95f))
        {
            d.Box("Washer", new Vector3(-W + 0.45f, 0.45f, z), new Vector3(0.82f, 0.9f, 0.8f), body, true);
            d.Disc("Washer ring", new Vector3(-W + 0.87f, 0.48f, z), 0.3f, 0.03f, Vector3.right, chrome);
            if (Mathf.Abs(z - (L - 13.3f)) > 0.5f) d.Disc("Washer door", new Vector3(-W + 0.885f, 0.48f, z), 0.25f, 0.03f, Vector3.right, glass);
            washers++;
        }
        foreach (var z in Doorspace.Every(4f, L - 4f, 4f)) d.Box("Strip light", new Vector3(0f, Ht - 0.04f, z), new Vector3(0.22f, 0.05f, 2.4f), tube);
        foreach (var z in Doorspace.Every(10f, L - 10f, 13f)) d.Box("Basket", new Vector3(-W + 1.5f, 0.18f, z), new Vector3(0.5f, 0.36f, 0.38f), basket);
        // one washer, far down, has a slowly turning sky inside
        float zs = Doorspace.Every(3f, L - 5f, 0.95f).OrderBy(z => Mathf.Abs(z - (L - 13.3f))).First();
        d.Disc("The washer with a sky in it", new Vector3(-W + 0.885f, 0.48f, zs), 0.25f, 0.035f, Vector3.right,
               d.Pane("Turning sky", H(1.2f, 0.6f, 1.4f, 1f), H(0.3f, 0.2f, 0.7f, 1f), H(1.5f, 1.2f, 1.9f, 1f), new Vector2(0.5f, 0.5f), 1f));
        RoomDoors(d, L, look);
        d.Spot(new Vector3(-W + 0.45f, 0.95f, L * 0.62f));
        d.Motes("Lint motes", new Vector3(0f, 1.6f, L / 2f), new Vector3(2f * W - 1f, 2.8f, L - 6f), H(1f, 1f, 1f, 0.7f), 40f, 0.03f, 0.025f, 400);
        d.Report.Add("- " + washers + " washers down the left wall, windows onto a night of clouds on the right, " + L + " m long.");
    }

    // 05 Gecko / Apricot Underpass: "A plain concrete passage bends out of sight. Thin light strips and damp, textured concrete."
    // Here: a 340 m tunnel that keeps bending right, so you never see far, with a cyan door at the end of the bend.
    static void B05(Doorspace d)
    {
        const float R = 140f, Len = 340f, W = 2.4f, Ht = 3.2f, seg = 2f;
        d.Haze = C(0.52f, 0.58f, 0.64f); d.HazeStart = 10f; d.HazeEnd = 110f; d.Light = new Vector3(0.4f, 0.7f, 0.3f);
        d.Sky(C(0.05f, 0.06f, 0.10f), C(0.10f, 0.12f, 0.18f), C(0.30f, 0.30f, 0.34f), C(0.20f, 0.20f, 0.24f), Vector3.up, Color.black, 0f, 0f, 0f, 0f);
        var look = DoorLook.Make("Cyan door", C(0.20f, 0.85f, 0.86f), C(0.35f, 1f, 1f));
        var floor = d.Surface("Wet concrete", C(0.46f, 0.40f, 0.40f), C(0.42f, 0.37f, 0.38f), default, 1.2f, 0f, 0.9f, H(0.9f, 0.6f, 0.7f, 1f));
        var wall = d.Surface("Apricot concrete", C(0.92f, 0.62f, 0.46f), C(0.88f, 0.58f, 0.43f), C(0.70f, 0.45f, 0.36f), 1.5f, 0.012f, 0f, default, 0f, default, C(0.12f, 0.05f, 0.02f));
        var ceil = d.Surface("Concrete ceiling", C(0.62f, 0.52f, 0.50f), C(0.58f, 0.49f, 0.47f), default, 1.5f, 0f);
        var strip = d.Lit("Cyan light strip", C(0.6f, 1f, 1f), H(0.3f, 1f, 1f, 1.8f), 1f, false);
        var lamp = d.Lit("Floor lamp", C(1f, 0.8f, 0.6f), H(1f, 0.6f, 0.3f, 1.6f), 1f, false);
        Vector3 P(float s) { float th = s / R; return new Vector3(R - R * Mathf.Cos(th), 0f, R * Mathf.Sin(th)); }
        float Yaw(float s) => s / R * Mathf.Rad2Deg;
        // one big floor collider under the whole bend (the walls keep you in)
        d.Slab("Floor (collider)", -W - 4f, 2f * R + W + 4f, -0.3f, 0f, -W - 4f, R + W + 4f, d.Lit("Hidden", Color.black), true).GetComponent<MeshRenderer>().enabled = false;
        int n = Mathf.CeilToInt(Len / seg);
        for (int i = 0; i < n; i++)
        {
            float s = i * seg + seg / 2f, l = seg + 0.05f;
            var t = Doorspace.Group("Tunnel " + i.ToString("000"), d.Static);
            t.localPosition = P(s); t.localRotation = Quaternion.Euler(0f, Yaw(s), 0f);
            d.Box("Floor", new Vector3(0f, -0.15f, 0f), new Vector3(2f * W + 0.6f, 0.3f, l), floor, false, 0f, t);
            d.Box("Wall L", new Vector3(-W - 0.15f, Ht / 2f, 0f), new Vector3(0.3f, Ht, l), wall, false, 0f, t);
            d.Box("Wall R", new Vector3(W + 0.15f, Ht / 2f, 0f), new Vector3(0.3f, Ht, l), wall, false, 0f, t);
            d.Box("Ceiling", new Vector3(0f, Ht + 0.15f, 0f), new Vector3(2f * W + 0.6f, 0.3f, l), ceil, false, 0f, t);
            d.Box("Strip", new Vector3(W - 0.02f, Ht - 0.35f, 0f), new Vector3(0.05f, 0.07f, l), strip, false, 0f, t);
            if (i % 3 == 0)
            {
                d.Box("Lamp", new Vector3(-W + 0.03f, 0.3f, 0f), new Vector3(0.06f, 0.12f, 0.3f), lamp, false, 0f, t);
                d.Collider("Wall L " + i, t, new Vector3(-W - 0.15f, Ht / 2f, seg), new Vector3(0.3f, Ht, seg * 3f + 0.2f));
                d.Collider("Wall R " + i, t, new Vector3(W + 0.15f, Ht / 2f, seg), new Vector3(0.3f, Ht, seg * 3f + 0.2f));
            }
        }
        // end caps behind the two doors
        var c0 = Doorspace.Group("Cap near", d.Static); c0.localPosition = P(-0.3f); c0.localRotation = Quaternion.Euler(0f, Yaw(0f), 0f);
        d.Box("Cap", Vector3.up * Ht / 2f, new Vector3(2f * W + 0.6f, Ht + 0.3f, 0.3f), wall, false, 0f, c0); d.Collider("Cap near", c0, Vector3.up * Ht / 2f, new Vector3(2f * W, Ht, 0.3f));
        var c1 = Doorspace.Group("Cap far", d.Static); c1.localPosition = P(Len + 0.3f); c1.localRotation = Quaternion.Euler(0f, Yaw(Len), 0f);
        d.Box("Cap", Vector3.up * Ht / 2f, new Vector3(2f * W + 0.6f, Ht + 0.3f, 0.3f), wall, false, 0f, c1); d.Collider("Cap far", c1, Vector3.up * Ht / 2f, new Vector3(2f * W, Ht, 0.3f));
        d.Door("Way back door", P(0.6f), Yaw(0.6f) + 180f, DoorLook.WayBack, false, false);
        var fwd = d.Door(look.name + " (forward)", P(Len - 1f), Yaw(Len - 1f), look, true, false);
        d.Points(P(2.6f), Yaw(2.6f), fwd);
        d.Halfway(P(Len / 2f), Yaw(Len / 2f));
        d.Spot(P(Len * 0.55f) + new Vector3(0f, 0.05f, 0f));
        d.Motes("Damp drift", P(Len * 0.5f) + Vector3.up * 1.6f, new Vector3(R * 1.2f, 2.6f, R * 1.2f), H(0.7f, 1f, 1f, 0.6f), 30f, 0.02f, 0.02f, 300);
        d.Report.Add("- A " + Len + " m tunnel bending right on a " + R + " m radius; " + n + " rings with a cyan light strip, a lamp every third ring.");
    }

    // 06 Crane / Platform Between Trains: "An empty platform suspended above clouds. A distant arrival tone, then silence."
    // Here: 500 m of night platform over a cloud sea, the same canopy and bench repeating, a lit cabin door at the end.
    static void B06(Doorspace d)
    {
        const float L = 500f;
        d.Haze = C(0.16f, 0.18f, 0.32f); d.HazeStart = 20f; d.HazeEnd = 260f; d.Light = new Vector3(-0.3f, 0.8f, 0.4f);
        d.Sky(C(0.03f, 0.04f, 0.12f), C(0.08f, 0.10f, 0.25f), C(0.25f, 0.25f, 0.45f), C(0.32f, 0.30f, 0.52f), new Vector3(0.5f, 0.35f, 0.8f), C(0.5f, 0.55f, 0.8f), 1f, 0.8f, 0.35f, 0.15f);
        var look = DoorLook.Make("Lit cabin door", C(0.95f, 0.55f, 0.45f), C(1f, 0.72f, 0.5f));
        var deck = d.Surface("Platform", C(0.42f, 0.42f, 0.48f), C(0.38f, 0.38f, 0.44f), C(0.28f, 0.28f, 0.33f), 0.6f, 0.02f, 0.6f, H(0.8f, 0.7f, 0.9f, 1f));
        var tactile = d.Lit("Tactile strip", C(0.95f, 0.78f, 0.25f), C(0.25f, 0.18f, 0.02f));
        var steel = d.Lit("Canopy steel", C(0.30f, 0.32f, 0.40f));
        var light = d.Lit("Canopy light", C(1f, 0.95f, 0.85f), H(1f, 0.9f, 0.75f, 1.7f), 1f, false);
        var bench = d.Lit("Bench", C(0.40f, 0.42f, 0.50f));
        var cloud = d.Lit("Cloud", C(0.40f, 0.38f, 0.62f), C(0.05f, 0.04f, 0.08f), 0.7f);
        d.Slab("Platform", -3.5f, 3.5f, -0.6f, 0f, -4f, L + 6f, deck);
        d.Slab("Tactile strip", 3.1f, 3.4f, 0f, 0.01f, -4f, L + 6f, tactile, false);
        d.Box("Edge rail (invisible)", new Vector3(3.55f, 1f, L / 2f), new Vector3(0.1f, 2f, L + 10f), d.Lit("Hidden", Color.black), true).GetComponent<MeshRenderer>().enabled = false;
        d.Box("Edge rail (invisible) L", new Vector3(-3.55f, 1f, L / 2f), new Vector3(0.1f, 2f, L + 10f), d.Lit("Hidden", Color.black), true).GetComponent<MeshRenderer>().enabled = false;
        foreach (var z in Doorspace.Every(0f, L, 10f))
        {
            d.Box("Canopy post", new Vector3(-2.4f, 1.6f, z), new Vector3(0.18f, 3.2f, 0.18f), steel, true);
            d.Box("Canopy roof", new Vector3(-0.8f, 3.25f, z), new Vector3(5.6f, 0.12f, 9.4f), steel);
            d.Box("Canopy light", new Vector3(-0.2f, 3.17f, z), new Vector3(0.18f, 0.04f, 6f), light);
            if (((int)(z / 10f)) % 2 == 0) d.Box("Bench", new Vector3(-1.4f, 0.42f, z + 4f), new Vector3(0.5f, 0.1f, 1.8f), bench, true);
        }
        // the cloud sea below and around
        var rnd = new System.Random(6);
        for (int i = 0; i < 90; i++)
        {
            float x = (float)(rnd.NextDouble() * 360 - 180), z = (float)(rnd.NextDouble() * (L + 300) - 150), s = 12f + (float)rnd.NextDouble() * 30f;
            if (Mathf.Abs(x) < 6f) x += Mathf.Sign(x) * 10f;
            d.Ball("Cloud", new Vector3(x, -10f - (float)rnd.NextDouble() * 8f, z), new Vector3(s * 1.6f, s * 0.35f, s), cloud);
        }
        d.Slab("Cloud floor", -800f, 800f, -16f, -15.5f, -400f, L + 600f, cloud, false);
        d.Ball("Moon", new Vector3(260f, 160f, 900f), Vector3.one * 70f, d.Lit("Moon", C(0.95f, 0.95f, 1f), H(0.6f, 0.6f, 0.7f, 1f), 1f, false));
        FieldDoors(d, new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, L), look);
        d.Spot(new Vector3(-1.4f, 0.55f, L * 0.4f + 4f));
        d.Motes("Paper notes", new Vector3(0f, 1.5f, L - 30f), new Vector3(6f, 2.5f, 50f), H(1f, 0.95f, 0.85f, 0.9f), 6f, 0.05f, 0.08f, 80, false, 8f, 1.2f);
        d.Report.Add("- A " + L + " m platform over a cloud sea; canopy, light and bench repeat every 10 m.");
    }

    // 07 Moth / Velvet Waiting Room: "Low seats and an oversized expanse of soft carpet. Fabric pile catches the light."
    // Here: a 160 m waiting room, low ceiling, rows of round velvet seats, a yellow door glowing at the far end.
    static void B07(Doorspace d)
    {
        const float W = 9f, L = 160f, Ht = 3.2f;
        d.Haze = C(0.30f, 0.08f, 0.13f); d.HazeStart = 12f; d.HazeEnd = 130f; d.Light = new Vector3(0.3f, 0.8f, -0.2f);
        d.Sky(C(0.1f, 0.03f, 0.05f), C(0.15f, 0.05f, 0.08f), C(0.3f, 0.1f, 0.14f), C(0.2f, 0.06f, 0.1f), Vector3.up, Color.black, 0f, 0f, 0f, 0f);
        var look = DoorLook.Make("Yellow door", C(1f, 0.86f, 0.25f), C(1f, 0.92f, 0.4f));
        var carpet = d.Surface("Velvet carpet", C(0.46f, 0.09f, 0.16f), C(0.40f, 0.07f, 0.14f), default, 0.5f, 0f, 0.25f, H(0.9f, 0.3f, 0.45f, 1f));
        var velvet = d.Lit("Velvet wall", C(0.36f, 0.06f, 0.12f), C(0.03f, 0f, 0.01f), 0.45f);
        d.Room(W, L, Ht, carpet, velvet, d.Lit("Ceiling", C(0.20f, 0.05f, 0.08f)), DoorLook.WayBack.opening, look.opening);
        var seat = d.Lit("Seat velvet", C(0.62f, 0.12f, 0.22f), C(0.05f, 0f, 0.02f), 0.5f);
        var brass = d.Lit("Brass", C(0.85f, 0.65f, 0.35f), C(0.1f, 0.06f, 0.02f));
        var lampGlow = d.Lit("Lamp shade", C(1f, 0.8f, 0.55f), H(1f, 0.7f, 0.4f, 1.5f), 1f, false);
        foreach (var z in Doorspace.Every(8f, L - 8f, 6f))
            foreach (var x in new[] { -5.5f, 0f, 5.5f })
            {
                d.Cyl("Seat", new Vector3(x, 0.21f, z), 0.7f, 0.42f, seat, true);
                d.Ball("Seat top", new Vector3(x, 0.42f, z), new Vector3(1.4f, 0.2f, 1.4f), seat);
            }
        foreach (var z in Doorspace.Every(6f, L - 6f, 12f))
            foreach (var x in new[] { -W + 0.6f, W - 0.6f })
            {
                d.Rod("Lamp stand", new Vector3(x, 0f, z), new Vector3(x, 1.4f, z), 0.025f, brass);
                d.Cyl("Lamp shade", new Vector3(x, 1.5f, z), 0.22f, 0.3f, lampGlow);
            }
        RoomDoors(d, L, look);
        d.Spot(new Vector3(5.5f, 0.6f, L * 0.7f));
        d.Motes("Moth dust", new Vector3(0f, 1.5f, L / 2f), new Vector3(2f * W - 1f, 2.6f, L - 4f), H(1f, 0.9f, 0.8f, 0.6f), 30f, 0.02f, 0.03f, 300);
        d.Report.Add("- " + L + " m of velvet waiting room; round seats in rows of three every 6 m, brass lamps every 12 m.");
    }

    // 08 Frog / Rain That Misses You: "Rain falls into an open stone courtyard. A small dry route crosses wet reflective paving."
    // Here: 320 m of colonnaded courtyard, rain everywhere but a dry path down the middle, a magenta door in a stone block.
    static void B08(Doorspace d)
    {
        const float L = 320f, half = 22f;
        d.Haze = C(0.15f, 0.19f, 0.25f); d.HazeStart = 12f; d.HazeEnd = 170f; d.Light = new Vector3(0.2f, 0.9f, 0.3f);
        d.Sky(C(0.05f, 0.08f, 0.12f), C(0.10f, 0.14f, 0.20f), C(0.20f, 0.25f, 0.32f), C(0.15f, 0.18f, 0.22f), Vector3.up, Color.black, 0f, 0f, 0f, 0.5f);
        var look = DoorLook.Make("Magenta door", C(0.92f, 0.22f, 0.62f), C(1f, 0.35f, 0.85f));
        var wet = d.Surface("Wet paving", C(0.21f, 0.23f, 0.28f), C(0.18f, 0.20f, 0.25f), C(0.10f, 0.11f, 0.14f), 0.9f, 0.04f, 1f, H(0.55f, 0.7f, 1f, 1f));
        var dry = d.Surface("Dry path", C(0.36f, 0.36f, 0.40f), C(0.33f, 0.33f, 0.37f), C(0.22f, 0.22f, 0.26f), 0.9f, 0.04f);
        var stone = d.Lit("Stone", C(0.32f, 0.35f, 0.40f), default, 0.5f);
        var lantern = d.Lit("Lantern", C(1f, 0.8f, 0.6f), H(1f, 0.7f, 0.45f, 1.6f), 1f, false);
        var leaves = d.Lit("Dark leaves", C(0.08f, 0.16f, 0.16f), default, 0.6f);
        d.Slab("Courtyard", -half, half, -0.3f, 0f, -4f, L + 6f, wet);
        d.Slab("Dry path", -1.2f, 1.2f, 0f, 0.012f, -4f, L + 2f, dry, false);
        foreach (var z in Doorspace.Every(0f, L, 6f))
            foreach (var s in new[] { -1f, 1f })
            {
                d.Cyl("Column", new Vector3(s * 16f, 2.5f, z), 0.45f, 5f, stone, true);
                d.Box("Lintel", new Vector3(s * 16f, 5.15f, z + 3f), new Vector3(1f, 0.3f, 6.2f), stone);
            }
        foreach (var s in new[] { -1f, 1f }) d.Slab("Courtyard wall " + s, s < 0 ? -half - 0.4f : half, s < 0 ? -half : half + 0.4f, 0f, 6f, -4f, L + 6f, stone);
        foreach (var z in Doorspace.Every(12f, L - 12f, 12f))
            foreach (var s in new[] { -1f, 1f })
            {
                d.Cyl("Tree", new Vector3(s * 19f, 1.6f, z), 0.22f, 3.2f, stone);
                d.Ball("Tree crown", new Vector3(s * 19f, 4.2f, z), new Vector3(3.6f, 3f, 3.6f), leaves);
            }
        foreach (var z in Doorspace.Every(15f, L - 5f, 15f))
            foreach (var s in new[] { -1f, 1f })
            {
                d.Box("Lantern post", new Vector3(s * 1.8f, 0.45f, z), new Vector3(0.12f, 0.9f, 0.12f), stone);
                d.Box("Lantern", new Vector3(s * 1.8f, 1.0f, z), new Vector3(0.22f, 0.25f, 0.22f), lantern);
            }
        d.Box("Stone block", new Vector3(0f, 1.8f, L + 0.7f), new Vector3(3.2f, 3.6f, 1.0f), stone, true);
        FieldDoors(d, new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, L), look);
        d.Rain("Rain left", new Vector3(-8.5f, 7f, L / 2f), new Vector3(13.4f, 2f, L + 8f), 1300f, H(0.6f, 0.72f, 0.95f, 0.5f));
        d.Rain("Rain right", new Vector3(8.5f, 7f, L / 2f), new Vector3(13.4f, 2f, L + 8f), 1300f, H(0.6f, 0.72f, 0.95f, 0.5f));
        d.Spot(new Vector3(-1.9f, 0.05f, L * 0.45f));
        d.Report.Add("- A " + L + " m colonnaded courtyard; rain falls on both sides of the 2.4 m dry path and nowhere on it.");
    }

    // 09 Hummingbird / Upside-Down Conservatory: "Planters and vines hang overhead in an empty glass room. Optional gentle low
    // gravity in one marked area." Here: a 180 m glass hall where the garden grows from the ceiling, its vines and trumpet
    // flowers grown by Unity Unicorn's vine-spawn-with-trumpet-flowers (Astra: "use the vine-spawn-tool we created in Unity Unicorn").
    static void B09(Doorspace d)
    {
        const float W = 8f, L = 180f, Ht = 7f;
        d.Haze = C(0.88f, 0.94f, 0.95f); d.HazeStart = 22f; d.HazeEnd = 200f; d.Light = new Vector3(0.3f, 0.9f, 0.2f);
        d.Backing = C(0.90f, 0.93f, 0.95f);   // the far wall is glass: a pale panel behind the lilac door, not a dark slab
        d.Sky(C(0.45f, 0.68f, 0.88f), C(0.70f, 0.85f, 0.95f), C(0.96f, 0.95f, 0.92f), C(0.62f, 0.78f, 0.70f), new Vector3(0.4f, 0.6f, 0.6f), C(1f, 0.95f, 0.85f), 0f, 0f, 0f, 0.5f);
        var look = DoorLook.Make("Lilac door", C(0.78f, 0.62f, 0.98f), C(0.85f, 0.70f, 1f));
        var floor = d.Surface("Pale stone", C(0.88f, 0.88f, 0.86f), C(0.84f, 0.85f, 0.84f), C(0.70f, 0.72f, 0.72f), 0.6f, 0.02f, 0.3f, H(0.8f, 0.9f, 1f, 1f));
        var glass = d.Glass("Conservatory glass", C(0.80f, 0.97f, 0.95f), 0.08f);
        d.Room(W, L, Ht, floor, glass, glass, DoorLook.WayBack.opening, look.opening);
        var frame = d.Lit("White frame", C(0.95f, 0.96f, 0.96f));
        var pot = d.Lit("White planter", C(0.93f, 0.94f, 0.93f));
        var trunk = d.Lit("Pale trunk", C(0.80f, 0.74f, 0.78f));
        // the canopies are dream colours, not garden green (Astra: "nothing really that natural looking")
        var crowns = new[] { d.Lit("Mint crown", C(0.55f, 0.88f, 0.74f), C(0.03f, 0.08f, 0.06f), 0.6f),
                             d.Lit("Lilac crown", C(0.80f, 0.70f, 0.98f), C(0.06f, 0.04f, 0.10f), 0.6f),
                             d.Lit("Rose crown", C(1f, 0.74f, 0.88f), C(0.10f, 0.04f, 0.07f), 0.6f) };
        var trail = d.Lit("Trailing leaves", C(0.46f, 0.76f, 0.60f), C(0.02f, 0.05f, 0.03f), 0.6f);
        var bloom = d.Lit("White blossom", C(1f, 0.97f, 0.98f), C(0.15f, 0.12f, 0.15f));
        var line = d.Glass("Vine line", C(0.90f, 1f, 0.98f), 0.05f);
        var lawn = d.Surface("Lawn outside", C(0.62f, 0.82f, 0.70f), C(0.58f, 0.78f, 0.68f), default, 2.5f, 0f);
        d.Slab("Lawn outside", -400f, 400f, -0.6f, -0.32f, -300f, L + 400f, lawn, false);
        foreach (var z in Doorspace.Every(0f, L, 6f))
        {
            foreach (var s in new[] { -1f, 1f }) d.Box("Mullion", new Vector3(s * (W + 0.05f), Ht / 2f, z), new Vector3(0.12f, Ht, 0.12f), frame);
            d.Box("Roof beam", new Vector3(0f, Ht - 0.06f, z), new Vector3(2f * W, 0.12f, 0.12f), frame);
        }
        // the garden, upside down: white planters hang from the roof and everything in them grows down
        var rnd = new System.Random(9);
        int k = 0;
        foreach (var z in Doorspace.Every(5f, L - 5f, 4f))
            foreach (var x in new[] { -4.2f, 0f, 4.2f })
            {
                float y = Ht - 0.9f;
                d.Rod("Hanger", new Vector3(x, y + 0.2f, z), new Vector3(x, Ht - 0.06f, z), 0.015f, frame);
                d.Box("Planter " + k, new Vector3(x, y, z), new Vector3(1.6f, 0.4f, 0.7f), pot);
                var crown = crowns[(k * 7 + (int)(z / 4f)) % crowns.Length];
                if (k % 3 == 1)   // a little tree growing down out of the planter
                {
                    float lean = (float)(rnd.NextDouble() - 0.5) * 0.5f, drop = 1.3f + (float)rnd.NextDouble() * 0.6f;
                    var tip = new Vector3(x + lean, y - 0.2f - drop, z + 0.1f);
                    d.Rod("Hanging trunk", new Vector3(x, y - 0.2f, z), tip, 0.06f, trunk);
                    d.Ball("Crown (hanging)", tip + Vector3.down * 0.55f, new Vector3(1.5f, 1.1f, 1.4f), crown);
                    d.Ball("Crown (hanging)", tip + new Vector3(0.55f, -0.85f, 0.2f), new Vector3(0.9f, 0.8f, 0.9f), crown);
                    d.Ball("Crown (hanging)", tip + new Vector3(-0.5f, -0.95f, -0.25f), new Vector3(0.8f, 0.7f, 0.8f), crown);
                    d.Ball("Crown tip (hanging)", tip + new Vector3(0.05f, -1.35f, 0f), new Vector3(0.45f, 0.5f, 0.45f), crown);
                    for (int b = 0; b < 3; b++) d.Ball("Blossom", tip + new Vector3((float)(rnd.NextDouble() - 0.5) * 1.4f, -0.6f - (float)rnd.NextDouble() * 0.8f, (float)(rnd.NextDouble() - 0.5) * 1.2f), Vector3.one * 0.18f, bloom);
                }
                else              // leaves spilling over the rim and dripping down
                {
                    d.Ball("Trailing leaves", new Vector3(x, y - 0.35f, z), new Vector3(1.3f, 0.45f, 0.55f), trail);
                    d.Ball("Trailing leaves", new Vector3(x - 0.35f, y - 0.7f, z), new Vector3(0.45f, 0.5f, 0.35f), trail);
                    d.Ball("Trailing leaves", new Vector3(x + 0.4f, y - 0.8f, z + 0.05f), new Vector3(0.35f, 0.6f, 0.3f), trail);
                    d.Ball("Trailing leaves", new Vector3(x + 0.4f, y - 1.2f, z + 0.05f), new Vector3(0.2f, 0.35f, 0.2f), crown);
                }
                k++;
            }
        foreach (var z in Doorspace.Every(12f, L - 12f, 12f)) d.Box("Bench", new Vector3(-W + 1.4f, 0.4f, z), new Vector3(0.5f, 0.1f, 2.2f), frame, true);
        d.Disc("Low gravity circle (marked)", new Vector3(0f, 0.012f, L * 0.5f), 3f, 0.01f, Vector3.up, d.Glow("Low gravity ring", H(0.8f, 0.7f, 1f, 0.22f), 0.6f));
        // curtains of vines from the roof beams, as in the concept: each trails down a strip of clear glass so it can hang its
        // full length (Unity Unicorn's vine tool only lets a hanging vine dangle a little in the air). The tool grows true-to-life
        // vines (1 cm stems, 5-10 cm leaves) that read as wisps from across a 16 m room, so they grow on a copy of each line
        // 1/Grand the size and are then scaled up Grand times about the top of the line: thicker stems, bigger leaves and trumpets
        const float Grand = 1.7f;
        var seeds = new List<GameObject>();
        int li = 0;
        foreach (var z in Doorspace.Every(6f, 150f, 6f))
            foreach (var x in new[] { -6.3f, -2.1f, 2.1f, 6.3f })
            {
                if (rnd.NextDouble() < 0.3) continue;
                bool side = Mathf.Abs(x) > 5f;
                float bottom = side ? 0.9f + (float)rnd.NextDouble() * 1.4f : 2.4f + (float)rnd.NextDouble() * 1.2f;   // over the walkway they stop above your head
                float jx = x + (float)(rnd.NextDouble() - 0.5) * 0.8f, top = Ht - 0.12f, len = top - bottom;
                d.Box("Vine line", new Vector3(jx, top - len / 2f, z + 0.1f), new Vector3(0.22f, len, 0.02f), line);
                seeds.Add(d.Box("Doorspace 09 vine line " + (li++).ToString("00"), new Vector3(jx, top - len / Grand / 2f, z + 0.1f), new Vector3(0.22f, len, 0.02f) / Grand, line));
            }
        var vines = GrowVines(d, seeds, new[] { "#ff6fae", "#e040fb", "#9d6bff", "#fff4fb", "#ffb38a" }, Grand, t => t.transform.position + Vector3.up * t.transform.lossyScale.y * 0.5f);
        foreach (var g in seeds) UnityEngine.Object.DestroyImmediate(g);
        RoomDoors(d, L, look);
        d.Spot(new Vector3(4.2f, Ht - 1.6f, L * 0.58f));
        d.Motes("Pollen", new Vector3(0f, 3f, L / 2f), new Vector3(2f * W - 1f, 5f, L - 4f), H(1f, 1f, 0.85f, 0.8f), 40f, -0.02f, 0.025f, 400, true);
        d.Report.Add("- A " + L + " m glass conservatory; " + k + " white planters hang from the roof beams with their plants growing down; curtains of " + vines);
    }

    // calls UnityUnicorn.VineBuilder20261019.Grow (Assets/UnityUnicorn/Editor, installed from unity-unicorn PR #18) by name,
    // so the Doorspaces still build if the tool isn't there; the vines end up inside the Doorspace
    static string GrowVines(Doorspace d, List<GameObject> targets, string[] colours, float grand = 1f, Func<GameObject, Vector3> pivot = null)
    {
        Type T(string n) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(n)).FirstOrDefault(t => t != null);
        var tb = T("UnityUnicorn.VineBuilder20261019"); var ts = T("UnityUnicorn.VineSettings20261019"); var tg = T("UnityUnicorn.VineGrow20261019");
        if (tb == null || ts == null || tg == null || targets.Count == 0) return "no vines (vine-spawn-with-trumpet-flowers is not installed).";
        string dir = "Assets/UnityUnicorn/Generated/Vines", mine = "Liminal Doors - Doorspace " + d.N.ToString("00") + " ";
        if (AssetDatabase.IsValidFolder(dir))   // the meshes an earlier build of this Doorspace grew (the old root is gone, so nothing uses them)
            foreach (var guid in AssetDatabase.FindAssets("t:Mesh", new[] { dir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid); var file = System.IO.Path.GetFileName(path);
                if (file.StartsWith(mine) || (d.N == 9 && file.StartsWith("Liminal Doors - Planter "))) AssetDatabase.DeleteAsset(path);
            }
        var s = Activator.CreateInstance(ts);
        void Set(string f, object v) => ts.GetField(f).SetValue(s, v);
        Set("vinesPerObject", 3); Set("seed", 20261019 + d.N); Set("lengthM", 5f / grand); Set("flowersPerM", 4.5f); Set("leavesPerM", 22f);
        Set("glow", 0.35f); Set("flowerSize", 1.2f); Set("grow", Enum.Parse(tg, "Hang")); Set("leafColor", new Color(0.30f, 0.58f, 0.40f)); Set("maxTriangles", 300000);
        var cols = colours.Select(h => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.white).ToList();
        var report = new List<string>();
        var root = tb.GetMethod("Grow").Invoke(null, new object[] { targets.ToList(), s, cols, report }) as GameObject;
        if (root == null) return "no vines grew (" + string.Join("; ", report) + ").";
        if (grand != 1f && pivot != null)   // scale each target's vines about its pivot (the tool names them "<target> vines"; the root sits at the world origin)
        {
            var at = targets.GroupBy(t => t.name + " vines").ToDictionary(g => g.Key, g => pivot(g.First()));
            foreach (Transform c in root.transform)
                if (at.TryGetValue(c.name, out var p)) { c.localScale = Vector3.one * grand; c.position = p * (1f - grand); }
        }
        root.transform.SetParent(d.Root, true);
        root.name = "Vines with trumpet flowers (Unity Unicorn vine-spawn-with-trumpet-flowers)";
        int tris = root.GetComponentsInChildren<MeshFilter>().Sum(m => m.sharedMesh != null ? m.sharedMesh.triangles.Length / 3 : 0);
        return (grand != 1f ? "vines grown at 1/" + grand + " size and scaled up " + grand + "x, " : "vines ") + "with trumpet flowers on " + targets.Count + " " + (targets[0].name.Contains("vine line") ? "glass lines" : "planters") + " (" + tris + " triangles, colours " + string.Join(" ", colours) + ").";
    }

    // 10 Ibex / Salt Moon: "A cracked salt plain beneath an enormous moon. Distance comes from haze and a simple horizon."
    static void B10(Doorspace d)
    {
        d.Haze = C(0.80f, 0.72f, 0.86f); d.HazeStart = 40f; d.HazeEnd = 520f; d.Light = new Vector3(0.4f, 0.6f, 0.6f);
        d.Sky(C(0.12f, 0.10f, 0.30f), C(0.35f, 0.28f, 0.55f), C(0.80f, 0.62f, 0.75f), C(0.90f, 0.85f, 0.92f), new Vector3(0.3f, 0.1f, 1f), C(0.6f, 0.5f, 0.7f), 0.6f, 0.4f, 0.2f, 0.25f);
        var look = DoorLook.Make("Pink door", C(0.98f, 0.40f, 0.62f), C(1f, 0.5f, 0.8f));
        var salt = d.Surface("Salt crust", C(0.96f, 0.95f, 0.98f), C(0.90f, 0.89f, 0.95f), C(0.70f, 0.68f, 0.80f), 3.2f, 0.03f, 0.25f, H(0.85f, 0.85f, 1f, 1f));
        d.Slab("Salt plain", -700f, 700f, -0.3f, 0f, -300f, 1300f, salt);
        var mound = d.Lit("Salt mound", C(0.93f, 0.92f, 0.97f));
        var rnd = new System.Random(10);
        for (int i = 0; i < 30; i++) { float x = (float)(rnd.NextDouble() * 900 - 450), z = (float)(rnd.NextDouble() * 1100 - 100); if (Mathf.Abs(x) < 12f) continue; float s = 2f + (float)rnd.NextDouble() * 6f; d.Ball("Salt mound", new Vector3(x, 0f, z), new Vector3(s * 2f, s * 0.5f, s * 1.6f), mound); }
        d.Ball("The moon", new Vector3(420f, 140f, 1600f), Vector3.one * 520f, d.Lit("Moon", C(0.95f, 0.92f, 0.98f), H(0.55f, 0.52f, 0.62f, 1f), 1f, false));
        d.Quad("Moon halo", new Vector3(420f, 140f, 1300f), Quaternion.identity, new Vector2(1100f, 1100f), d.Glow("Moon halo", H(0.6f, 0.55f, 0.8f, 0.4f), 1.2f));
        FieldDoors(d, new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, 420f), look);
        d.Spot(new Vector3(-6f, 0.05f, 210f));
        d.Motes("Salt glitter", new Vector3(0f, 0.4f, 200f), new Vector3(60f, 0.8f, 420f), H(1f, 1f, 1.2f, 1.2f), 60f, 0.01f, 0.02f, 500, true);
        d.Report.Add("- A salt plain 1.4 km across under a moon 520 m wide; the door is 420 m out.");
    }

    // 11 Cat / Motel Without Rooms: "A weathered motel balcony overlooking empty sky. A flickering vacancy light and warm stucco."
    // Here: a 300 m balcony; the same red room door every 4 m, none of them opens; the sea far below.
    static void B11(Doorspace d)
    {
        const float L = 300f;
        d.Haze = C(0.86f, 0.80f, 0.76f); d.HazeStart = 30f; d.HazeEnd = 260f; d.Light = new Vector3(0.6f, 0.7f, -0.3f);
        d.Sky(C(0.28f, 0.55f, 0.80f), C(0.55f, 0.75f, 0.90f), C(0.98f, 0.85f, 0.70f), C(0.50f, 0.68f, 0.80f), new Vector3(0.8f, 0.3f, 0.5f), C(1f, 0.85f, 0.6f), 0f, 0f, 0f, 0.4f);
        var look = DoorLook.Make("Red door", C(0.86f, 0.18f, 0.24f), C(1f, 0.45f, 0.5f));
        var stucco = d.Surface("Stucco", C(0.96f, 0.76f, 0.58f), C(0.92f, 0.71f, 0.55f), default, 0.45f, 0f, 0f, default, 0f, default, C(0.06f, 0.03f, 0.01f));
        var concrete = d.Surface("Balcony concrete", C(0.78f, 0.72f, 0.68f), C(0.74f, 0.68f, 0.65f), C(0.6f, 0.55f, 0.52f), 1.2f, 0.02f);
        var red = d.Lit("Room door red", C(0.76f, 0.14f, 0.17f)); var brass = d.Lit("Brass number", C(0.92f, 0.75f, 0.35f), C(0.15f, 0.1f, 0.02f));
        var curtain = d.Lit("Window curtain", C(0.95f, 0.78f, 0.62f), C(0.08f, 0.05f, 0.02f));
        var rail = d.Lit("Rail", C(0.9f, 0.88f, 0.84f)); var neon = d.Lit("Vacancy neon", C(1f, 0.4f, 0.6f), H(1f, 0.25f, 0.55f, 2.2f), 1f, false);
        var lamp = d.Lit("Porch lamp", C(1f, 0.9f, 0.7f), H(1f, 0.8f, 0.5f, 1.4f), 1f, false);
        d.Slab("Balcony", 0f, 2.6f, -0.2f, 0f, -2f, L + 3f, concrete);
        d.Slab("Facade", -0.3f, 0f, -0.2f, 3.1f, -2f, L + 3f, stucco);
        d.Slab("Overhang", -0.3f, 2.8f, 3.0f, 3.15f, -2f, L + 3f, stucco, false);
        d.Slab("Rail top", 2.48f, 2.58f, 1.0f, 1.08f, -2f, L + 3f, rail);
        int rooms = 0;
        foreach (var z in Doorspace.Every(2f, L - 2f, 4f))
        {
            d.Box("Room door", new Vector3(0.02f, 1.05f, z), new Vector3(0.06f, 2.1f, 0.95f), red);
            d.Box("Room number", new Vector3(0.06f, 1.75f, z), new Vector3(0.02f, 0.12f, 0.22f), brass);
            d.Box("Room window", new Vector3(0.02f, 1.35f, z + 1.6f), new Vector3(0.05f, 0.9f, 1.2f), curtain);
            d.Box("Porch lamp", new Vector3(0.06f, 2.3f, z + 0.75f), new Vector3(0.08f, 0.14f, 0.1f), lamp);
            rooms++;
        }
        foreach (var z in Doorspace.Every(0f, L, 2f)) d.Box("Rail post", new Vector3(2.53f, 0.5f, z), new Vector3(0.05f, 1.0f, 0.05f), rail);
        d.Box("Rail (collider)", new Vector3(2.6f, 0.9f, L / 2f), new Vector3(0.1f, 1.8f, L + 6f), rail, true).GetComponent<MeshRenderer>().enabled = false;
        d.Box("VACANCY", new Vector3(2.7f, 3.6f, 40f), new Vector3(0.12f, 0.5f, 1.8f), neon);
        d.Slab("Sea far below", -200f, 1400f, -40.2f, -40f, -300f, L + 500f, d.Water("Sea", C(0.25f, 0.55f, 0.70f), H(1f, 0.85f, 0.75f, 1f), 0.2f), false);
        d.Slab("End wall", -0.3f, 2.8f, 0f, 3.1f, L + 2.4f, L + 2.8f, stucco);
        FieldDoors(d, new Vector3(1.3f, 0f, 2f), new Vector3(1.3f, 0f, L + 1.6f), look, false);
        d.Spot(new Vector3(2.2f, 0.05f, L * 0.48f));
        d.Report.Add("- A " + L + " m motel balcony, " + rooms + " red room doors that never open, the sea 40 m below.");
    }

    // 12 Manta Ray / Tidal Carpet: "A sunken lounge whose carpet appears to roll like the sea. Only the pattern moves."
    static void B12(Doorspace d)
    {
        const float W = 12f, L = 200f, Ht = 4.5f;
        d.Haze = C(0.30f, 0.20f, 0.40f); d.HazeStart = 14f; d.HazeEnd = 160f; d.Light = new Vector3(0.2f, 0.9f, -0.3f);
        d.Sky(C(0.1f, 0.06f, 0.16f), C(0.18f, 0.1f, 0.25f), C(0.3f, 0.18f, 0.38f), C(0.2f, 0.12f, 0.26f), Vector3.up, Color.black, 0f, 0f, 0f, 0f);
        var look = DoorLook.Make("Orchid door", C(0.85f, 0.45f, 0.95f), C(0.9f, 0.55f, 1f));
        var carpet = d.Surface("Tidal carpet", C(0.38f, 0.22f, 0.54f), C(0.34f, 0.19f, 0.50f), default, 0.6f, 0f, 0.15f, H(0.7f, 0.5f, 1f, 1f), 1f, H(0.75f, 0.45f, 1f, 1f));
        d.Room(W, L, Ht, carpet, d.Lit("Plum walls", C(0.40f, 0.20f, 0.42f)), d.Lit("Ceiling", C(0.22f, 0.12f, 0.26f)), DoorLook.WayBack.opening, look.opening);
        var sofa = d.Lit("Sofa", C(0.58f, 0.38f, 0.66f), C(0.04f, 0.02f, 0.05f)); var table = d.Lit("Table", C(0.25f, 0.16f, 0.30f));
        var candle = d.Lit("Candle", C(1f, 0.85f, 0.6f), H(1f, 0.6f, 0.3f, 2f), 1f, false); var step = d.Surface("Lounge step", C(0.30f, 0.17f, 0.44f), C(0.28f, 0.16f, 0.42f), default, 0.6f, 0f);
        foreach (var z in Doorspace.Every(14f, L - 14f, 18f))
            foreach (var x in new[] { -6f, 6f })
            {
                d.Box("Rim", new Vector3(x, 0.12f, z), new Vector3(7.2f, 0.24f, 7.2f), step, true);
                d.Box("Sofa back", new Vector3(x, 0.55f, z - 2.9f), new Vector3(5.6f, 0.9f, 0.6f), sofa, true);
                d.Box("Sofa seat", new Vector3(x, 0.4f, z - 2.3f), new Vector3(5.6f, 0.35f, 1f), sofa, true);
                d.Box("Sofa side", new Vector3(x - 2.9f, 0.4f, z), new Vector3(1f, 0.35f, 4.6f), sofa, true);
                d.Cyl("Coffee table", new Vector3(x, 0.45f, z), 0.9f, 0.18f, table, true);
                d.Cyl("Candle", new Vector3(x, 0.62f, z), 0.06f, 0.16f, candle);
            }
        RoomDoors(d, L, look);
        d.Spot(new Vector3(-6f, 0.5f, 104f));
        d.Report.Add("- A " + L + " m lounge whose carpet's light rolls like a tide; sunken sofa squares repeat every 18 m.");
    }

    // 13 Hedgehog / Vending Machine Clearing: "One vending machine in a small twilight clearing." Here the same vending
    // machine waits beside the forest path every 30 m, all the way to the door.
    static void B13(Doorspace d)
    {
        const float L = 330f;
        d.Haze = C(0.14f, 0.16f, 0.30f); d.HazeStart = 16f; d.HazeEnd = 180f; d.Light = new Vector3(-0.4f, 0.8f, 0.3f);
        d.Sky(C(0.05f, 0.05f, 0.18f), C(0.14f, 0.12f, 0.35f), C(0.42f, 0.30f, 0.55f), C(0.12f, 0.12f, 0.24f), new Vector3(0f, -0.1f, 1f), C(0.5f, 0.3f, 0.5f), 0.9f, 0.5f, 0.3f, 0.15f);
        var look = DoorLook.Make("Pink door", C(0.98f, 0.40f, 0.70f), C(1f, 0.5f, 0.85f));
        var grass = d.Surface("Night grass", C(0.13f, 0.17f, 0.26f), C(0.11f, 0.15f, 0.23f), default, 1.4f, 0f);
        var path = d.Surface("Forest path", C(0.26f, 0.24f, 0.32f), C(0.24f, 0.22f, 0.30f), default, 0.8f, 0f);
        var bark = d.Lit("Bark", C(0.16f, 0.14f, 0.20f)); var crown = d.Lit("Night leaves", C(0.10f, 0.18f, 0.26f), default, 0.5f);
        var machine = d.Lit("Vending machine", C(0.95f, 0.45f, 0.65f)); var panel = d.Lit("Vending light", C(1f, 0.9f, 0.95f), H(1f, 0.85f, 0.95f, 1.6f), 1f, false);
        var can = d.Lit("Cans", C(0.4f, 0.8f, 1f), H(0.2f, 0.5f, 0.7f, 1f), 1f, false);
        d.Slab("Clearing", -200f, 200f, -0.3f, 0f, -60f, L + 200f, grass);
        d.Slab("Path", -0.9f, 0.9f, 0f, 0.01f, -2f, L, path, false);
        var rnd = new System.Random(13);
        for (int i = 0; i < 260; i++)
        {
            float z = (float)(rnd.NextDouble() * (L + 60) - 20), x = (rnd.NextDouble() < 0.5 ? -1f : 1f) * (5f + (float)rnd.NextDouble() * 40f), h = 5f + (float)rnd.NextDouble() * 5f;
            d.Cyl("Tree", new Vector3(x, h / 2f, z), 0.22f, h, bark, true);
            d.Ball("Crown", new Vector3(x, h + 1f, z), new Vector3(3.4f, 4f, 3.4f), crown);
        }
        int machines = 0;
        foreach (var z in Doorspace.Every(14f, L - 6f, 30f))
        {
            d.Box("Vending machine", new Vector3(1.8f, 0.95f, z), new Vector3(0.9f, 1.9f, 0.8f), machine, true);
            d.Box("Vending front", new Vector3(1.34f, 1.15f, z), new Vector3(0.02f, 1.2f, 0.62f), panel);
            for (int r = 0; r < 4; r++) d.Box("Cans", new Vector3(1.33f, 0.75f + r * 0.27f, z), new Vector3(0.02f, 0.1f, 0.5f), can);
            d.Quad("Light pool", new Vector3(1.0f, 0.02f, z), Quaternion.Euler(90f, 0f, 0f), new Vector2(2.4f, 2.4f), d.Glow("Vending glow on the ground", H(1f, 0.6f, 0.8f, 0.5f), 1.2f));
            machines++;
        }
        FieldDoors(d, new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, L), look);
        d.Motes("Fireflies", new Vector3(0f, 1.2f, L / 2f), new Vector3(30f, 2f, L), H(0.9f, 1f, 0.5f, 1.4f), 30f, 0.02f, 0.04f, 300, true);
        d.Spot(new Vector3(2.4f, 0.05f, 134f));
        d.Report.Add("- A " + L + " m forest path at twilight; the same vending machine " + machines + " times, its hum and glow every 30 m.");
    }

    // 14 Lynx / Lavender Quarry: "Broad terraces of pale chalk and clean-cut stone. Fine dust rises instead of falling."
    static void B14(Doorspace d)
    {
        const int tiers = 12; const float rise = 2.4f, depth = 9f, stairRun = 6f, half = 70f;
        d.Haze = C(0.88f, 0.84f, 0.95f); d.HazeStart = 30f; d.HazeEnd = 320f; d.Light = new Vector3(0.5f, 0.8f, -0.4f);
        d.Sky(C(0.55f, 0.55f, 0.85f), C(0.75f, 0.72f, 0.92f), C(0.98f, 0.92f, 0.95f), C(0.85f, 0.80f, 0.92f), new Vector3(-0.4f, 0.5f, 0.7f), C(1f, 0.95f, 0.9f), 0f, 0f, 0f, 0.5f);
        var look = DoorLook.Make("Yellow door", C(1f, 0.88f, 0.30f), C(1f, 0.95f, 0.45f));
        var chalk = d.Surface("Lavender chalk", C(0.82f, 0.76f, 0.92f), C(0.76f, 0.70f, 0.88f), C(0.62f, 0.56f, 0.74f), 1.4f, 0.025f);
        var block = d.Lit("Cut stone", C(0.86f, 0.82f, 0.95f));
        d.Slab("Quarry floor", -half, half, -0.3f, 0f, -30f, 26f, chalk);
        float z0 = 26f;
        for (int k = 1; k <= tiers; k++)
        {
            float top = k * rise, front = z0 + (k - 1) * depth + stairRun;
            d.Slab("Tier " + k, -half, half, top - rise, top, front, front + depth * (tiers - k + 1) + 20f, chalk);
            // a stair up its face in the middle (steps are colliders too)
            for (int sIdx = 0; sIdx < 12; sIdx++)
            {
                float sy = top - rise + (sIdx + 1) * rise / 12f, sz = front - stairRun + sIdx * stairRun / 12f;
                d.Slab("Stair " + k + "." + sIdx, -2f, 2f, top - rise, sy, sz, sz + stairRun / 12f + 0.02f, chalk);
            }
            var rnd = new System.Random(140 + k);
            for (int b = 0; b < 8; b++) { float x = (float)(rnd.NextDouble() * 120 - 60); if (Mathf.Abs(x) < 4f) continue; float s = 1f + (float)rnd.NextDouble() * 2f; d.Box("Block", new Vector3(x, top + s / 2f, front + 2f + (float)rnd.NextDouble() * 5f), new Vector3(s * 1.6f, s, s), block, true); }
        }
        float doorZ = z0 + (tiers - 1) * depth + stairRun + 5f;
        FieldDoors(d, new Vector3(0f, 0f, 2f), new Vector3(0f, tiers * rise, doorZ), look);
        d.Motes("Rising dust", new Vector3(0f, 10f, 70f), new Vector3(120f, 20f, 140f), H(1f, 0.95f, 1.1f, 0.6f), 60f, 0.25f, 0.03f, 600);
        d.Spot(new Vector3(9f, 6f * rise, z0 + 5f * depth + stairRun + 3f));
        d.Report.Add("- " + tiers + " terraces of lavender chalk, " + rise + " m each, a stair up the middle of every one; the door is on the top, " + (tiers * rise) + " m up.");
    }
}
