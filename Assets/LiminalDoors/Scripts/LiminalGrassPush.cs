using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

// Claude, 2026-10-19. Lets the Faraway Field's grass bend out of your way (Astra: "work on the grass physics").
// Every frame it hands the grass shader (LiminalDoors/Frond Grass) where your feet are, as the global
// _UdonLiminalPlayer (VRChat only lets Udon set globals whose names start with _Udon). Only on your own screen: each
// player's grass parts for them. It sits inside stop 02, so it runs only while that stop is switched on; when it switches
// off it tells the grass nobody is there.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LiminalGrassPush : UdonSharpBehaviour
{
    int id;

    void Start() { id = VRCShader.PropertyToID("_UdonLiminalPlayer"); }

    void Update()
    {
        var me = Networking.LocalPlayer;
        if (!Utilities.IsValid(me)) return;
        Vector3 p = me.GetPosition();
        VRCShader.SetGlobalVector(id, new Vector4(p.x, p.y, p.z, 1f));
    }

    void OnDisable()
    {
        if (id != 0) VRCShader.SetGlobalVector(id, Vector4.zero);
    }
}
