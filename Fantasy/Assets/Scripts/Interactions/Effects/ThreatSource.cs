using System.Collections.Generic;
using UnityEngine;

public class ThreatSource : MonoBehaviour
{
    [SerializeField] private Transform pivot;

    private static readonly List<ThreatSource> active = new List<ThreatSource>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        active.Clear();
    }

    public Vector2 Position => pivot != null ? (Vector2)pivot.position : (Vector2)transform.position;

    private void OnEnable()
    {
        active.Add(this);
    }

    private void OnDisable()
    {
        active.Remove(this);
    }

    public static bool TryGetNearest(Vector2 from, out float distance)
    {
        distance = float.MaxValue;

        for (int i = 0; i < active.Count; i++)
        {
            float d = Vector2.Distance(from, active[i].Position);
            if (d < distance) distance = d;
        }

        return distance < float.MaxValue;
    }
}
