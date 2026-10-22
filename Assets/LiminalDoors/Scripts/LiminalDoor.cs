using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

// Claude, 2026-10-18. One Liminal Doors door: a trigger box in a doorway. Walking into it sends you on (a forward door,
// random) or back the way you came (an arrival door). Only the local player travels; LiminalTraveler does the moving.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LiminalDoor : UdonSharpBehaviour
{
    public LiminalTraveler traveler;
    public int stop;      // index of the stop this door stands in (0 = the pink booth)
    public bool back;     // true: arrival door (the way back); false: forward door (somewhere random)

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(player) || !player.isLocal || traveler == null) return;
        if (back) traveler.Back(stop);
        else traveler.Forward(stop);
    }
}
