// Claude, 2026-10-19. Pieces every Liminal Doors stop builds its doors and rooms from, so stops 02 and 03 (and later
// ones) look alike where they should: materials made once, plain boxes, the doorway portal and rising motes.
// The portal answers Astra (2026-10-19): "the door portals need to fill up the square, in unity you got that kind of
// weird". It is a quad exactly the size of the opening, inside the frame's inner edges, drawn with LiminalDoors/Portal.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class LiminalDoorKit
{
    public static Material Mat(string path, string shaderName)
    {
        var sh = Shader.Find(shaderName);
        if (sh == null) throw new Exception("Shader " + shaderName + " not found (check the console for shader errors).");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh) { name = Path.GetFileNameWithoutExtension(path) }; AssetDatabase.CreateAsset(m, path); }
        else m.shader = sh;
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    // a portal material for an opening of this size (metres); colours are HDR, so above 1 glows
    public static Material PortalMat(string path, Color veil, Color deep, Color rim, Vector2 size)
    {
        var m = Mat(path, "LiminalDoors/Portal");
        m.SetColor("_Color", veil); m.SetColor("_Deep", deep); m.SetColor("_Rim", rim);
        m.SetVector("_Size", new Vector4(size.x, size.y, 0, 0));
        return m;
    }

    // glitter paint for a door's frame and leaves (Astra, 2026-10-19: "the door more glittery")
    public static Material GlitterMat(string path, Color paint, Color glow, Color glitter, Vector3 lightDir, float density = 0.55f)
    {
        var m = Mat(path, "LiminalDoors/Glitter Paint");
        m.SetColor("_Color", paint); m.SetColor("_Emission", glow); m.SetColor("_GlitterColor", glitter);
        m.SetFloat("_Density", density); m.SetVector("_LightDir", lightDir);
        return m;
    }

    // a cloud of twinkling glitter hanging around a door, thickest at the doorway
    public static ParticleSystem GlitterHalo(Transform door, Material mat, Vector3 center, Vector3 box, float rate = 60f, int max = 500)
    {
        var go = new GameObject("Glitter halo"); go.transform.SetParent(door, false); go.transform.localPosition = center;
        var ps = Motes(go, mat, rate, box, 0.06f, 0.05f, max);
        var main = ps.main; main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 5f);
        var sz = ps.sizeOverLifetime; sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.12f, 1f), new Keyframe(0.25f, 0.2f), new Keyframe(0.4f, 1f), new Keyframe(0.55f, 0.25f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0f)));
        var noise = ps.noise; noise.strength = 0.15f; noise.frequency = 0.8f;
        return ps;
    }

    public static GameObject Box(Transform parent, string name, Vector3 at, Vector3 size, Material mat, bool solid = false)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
        if (!solid) UnityEngine.Object.DestroyImmediate(b.GetComponent<Collider>());
        b.name = name; b.transform.SetParent(parent, false);
        b.transform.localPosition = at; b.transform.localScale = size;
        var r = b.GetComponent<MeshRenderer>(); r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        return b;
    }

    public static GameObject Quad(Transform parent, string name, Vector3 at, Quaternion rot, Vector2 size, Material mat)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        UnityEngine.Object.DestroyImmediate(q.GetComponent<Collider>());
        q.name = name; q.transform.SetParent(parent, false);
        q.transform.localPosition = at; q.transform.localRotation = rot; q.transform.localScale = new Vector3(size.x, size.y, 1f);
        var r = q.GetComponent<MeshRenderer>(); r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        return q;
    }

    // the light standing in the doorway: bottom edge on the threshold, filling the opening to the frame on every side
    public static GameObject Portal(Transform door, float sillY, Vector2 opening, Material mat)
    {
        return Quad(door, "Doorway portal", new Vector3(0f, sillY + opening.y * 0.5f, 0f), Quaternion.identity, opening, mat);
    }

    public static ParticleSystem Motes(GameObject go, Material mat, float rate, Vector3 box, float rise = 0.2f, float sizeMax = 0.04f, int max = 200)
    {
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true; main.prewarm = true; main.duration = 8f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.05f);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMax * 0.38f, sizeMax);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, 1f, 1f, 0.7f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = max;
        var emission = ps.emission; emission.rateOverTime = rate;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = box;
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f); vel.y = new ParticleSystem.MinMaxCurve(rise * 0.6f, rise * 1.5f); vel.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.08f; noise.frequency = 0.4f; noise.scrollSpeed = 0.1f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.7f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat; r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        return ps;
    }

    // a trigger box in the doorway; LiminalDoorsTravel (World/Wire doors) puts a LiminalDoor on it by its name
    public static GameObject Trigger(Transform door, string name, Vector3 center, Vector3 size)
    {
        var t = new GameObject(name); t.transform.SetParent(door, false);
        t.transform.localPosition = center;
        var bc = t.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = size;
        return t;
    }
}
