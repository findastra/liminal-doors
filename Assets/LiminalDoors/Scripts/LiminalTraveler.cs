using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

// Claude, 2026-10-18. Liminal Doors travel. Astra, 2026-10-18: "Spawn in the booth, and stepping out through the booth
// door sends you straight to a random door." Idea inbox, 2026-10-15: forward doors choose random destinations, arrival
// doors keep the real way back, fewer immediate repeats, the pink booth stays the start.
//
// One traveler per world; every door calls it (LiminalDoor). Each player travels on their own: only the stop you are
// in is switched on, and only on your screen, so remote players at other stops are thousands of metres away and unseen.
// Stop [0] is the start (stop 01, the pink booth) and is never a random destination.
// The way back is a list of the stops you came through: a forward door adds to it, an arrival door takes the last one.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LiminalTraveler : UdonSharpBehaviour
{
    public GameObject[] stops;      // stop roots in stop order; [0] = the pink booth
    public Transform[] arrivals;    // where a forward door lands you: just in front of that stop's arrival door
    public Transform[] returns;     // where you land when you come back to a stop through an arrival door
    public Material[] skies;        // each stop's sky (an empty entry keeps the sky as it is)
    public int avoidRecent = 3;     // a forward door avoids this many of your last stops, once there are enough stops

    private int current;
    private int[] path = new int[256];
    private int depth;
    private int[] recent = new int[16];
    private int recentN;
    private float lastTrip = -10f;

    void Start()
    {
        depth = 0;
        Show(0, null);
    }

    public void Forward(int from)
    {
        if (from != current || Time.time - lastTrip < 0.75f) return;
        int dest = Pick();
        if (dest < 0)
        {
            // no other stop exists yet: the door leads back to the booth and the way back starts over
            depth = 0;
            Show(0, returns[0]);
            return;
        }
        if (depth < path.Length) { path[depth] = current; depth++; }
        Show(dest, arrivals[dest]);
    }

    public void Back(int from)
    {
        if (from != current || Time.time - lastTrip < 0.75f) return;
        int dest = 0;
        if (depth > 0) { depth--; dest = path[depth]; }
        Show(dest, returns[dest]);
    }

    public override void OnPlayerRespawn(VRCPlayerApi player)
    {
        // VRChat puts you back at the world spawn, which is inside the booth
        if (!Utilities.IsValid(player) || !player.isLocal) return;
        depth = 0;
        Show(0, null);
    }

    public int CurrentStop() { return current; }
    public int WayBackLength() { return depth; }

    private int Pick()
    {
        int n = stops.Length;
        int avoid = Mathf.Min(avoidRecent, Mathf.Max(0, n - 3));
        int[] options = new int[n];
        int count = 0;
        for (int i = 1; i < n; i++) if (i != current && !Recent(i, avoid)) { options[count] = i; count++; }
        if (count == 0) for (int i = 1; i < n; i++) if (i != current) { options[count] = i; count++; }
        if (count == 0) return -1;
        return options[Random.Range(0, count)];
    }

    private bool Recent(int stop, int avoid)
    {
        for (int k = 0; k < avoid && k < recentN; k++) if (recent[k] == stop) return true;
        return false;
    }

    // switch the destination on first, move the player, then switch every other stop off
    private void Show(int dest, Transform at)
    {
        if (dest < 0 || dest >= stops.Length) return;
        if (stops[dest] != null) stops[dest].SetActive(true);
        VRCPlayerApi me = Networking.LocalPlayer;
        if (at != null && Utilities.IsValid(me)) me.TeleportTo(at.position, at.rotation);
        for (int i = 0; i < stops.Length; i++) if (i != dest && stops[i] != null) stops[i].SetActive(false);
        if (skies != null && dest < skies.Length && skies[dest] != null) RenderSettings.skybox = skies[dest];
        current = dest;
        lastTrip = Time.time;
        for (int k = recent.Length - 1; k > 0; k--) recent[k] = recent[k - 1];
        recent[0] = dest;
        if (recentN < recent.Length) recentN++;
    }
}
