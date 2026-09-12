using System.Collections.Generic;
using UnityEngine;

public class SoundPerception : MonoBehaviour
{
    [SerializeField] private SoundEventChannelSO channel;

    [SerializeField] private float maxHearingDistance = 15f;
    [SerializeField] private float soundLifetime = 8f;

    [SerializeField] private bool drawHearingRangeGizmo = true;
    [SerializeField] private bool drawPendingSoundsGizmo = true;
    [SerializeField] private Color hearingRangeColor = new Color(0.3f, 0.7f, 1f, 0.4f);
    [SerializeField] private Color pendingSoundColor = new Color(1f, 0.2f, 0.9f, 0.6f);
    [SerializeField] private Color consumedFlashColor = new Color(1f, 1f, 1f, 0.9f);

    private struct SoundPoint
    {
        public Vector2 position;
        public float searchRadius;
        public float priority;
        public float expireTime;
        public int sourceId;
    }

    private readonly List<SoundPoint> pendingSounds = new List<SoundPoint>();

    public bool HasPendingSound => pendingSounds.Count > 0;

    private void OnEnable()
    {
        if (channel != null)
            channel.OnSoundRaised += HandleSoundRaised;
    }

    private void OnDisable()
    {
        if (channel != null)
            channel.OnSoundRaised -= HandleSoundRaised;
    }

    private void Update()
    {
        if (pendingSounds.Count == 0) return;

        float now = Time.time;
        pendingSounds.RemoveAll(s => now > s.expireTime);
    }

    private void HandleSoundRaised(Vector2 position, float searchRadius, float priority, int sourceId)
    {
        float dist = Vector2.Distance(transform.position, position);
        if (dist > maxHearingDistance) return;

        var point = new SoundPoint
        {
            position = position,
            searchRadius = searchRadius,
            priority = priority,
            expireTime = Time.time + soundLifetime,
            sourceId = sourceId
        };

        if (sourceId != 0)
        {
            int existingIndex = pendingSounds.FindIndex(s => s.sourceId == sourceId);
            if (existingIndex >= 0)
            {
                pendingSounds[existingIndex] = point;
                return;
            }
        }

        pendingSounds.Add(point);
    }

    public bool TryConsumeBestSound(out Vector2 position, out float searchRadius, out int sourceId)
    {
        position = default;
        searchRadius = default;
        sourceId = 0;

        if (pendingSounds.Count == 0) return false;

        int bestIndex = 0;
        for (int i = 1; i < pendingSounds.Count; i++)
        {
            if (pendingSounds[i].priority >= pendingSounds[bestIndex].priority)
                bestIndex = i;
        }

        position = pendingSounds[bestIndex].position;
        searchRadius = pendingSounds[bestIndex].searchRadius;
        sourceId = pendingSounds[bestIndex].sourceId;
        pendingSounds.RemoveAt(bestIndex);

        lastConsumedPosition = position;
        lastConsumedFlashUntil = Time.time + ConsumedFlashDuration;

        return true;
    }

    public bool TryPeekActiveTrackedPosition(int sourceId, out Vector2 position, out float searchRadius)
    {
        position = default;
        searchRadius = default;

        if (sourceId == 0) return false;

        int index = pendingSounds.FindIndex(s => s.sourceId == sourceId);
        if (index < 0) return false;

        position = pendingSounds[index].position;
        searchRadius = pendingSounds[index].searchRadius;
        return true;
    }

    public void ClearAll() => pendingSounds.Clear();

    private Vector2 lastConsumedPosition;
    private float lastConsumedFlashUntil = -1f;
    private const float ConsumedFlashDuration = 0.6f;

    private void OnDrawGizmosSelected()
    {
        if (drawHearingRangeGizmo)
        {
            Gizmos.color = hearingRangeColor;
            Gizmos.DrawWireSphere(transform.position, maxHearingDistance);
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawPendingSoundsGizmo) return;

        foreach (var sound in pendingSounds)
        {
            Gizmos.color = pendingSoundColor;
            Gizmos.DrawWireSphere(sound.position, sound.searchRadius);
            Gizmos.DrawLine(transform.position, sound.position);

            Gizmos.DrawSphere(sound.position, 0.12f);
        }

        if (Application.isPlaying && Time.time < lastConsumedFlashUntil)
        {
            Gizmos.color = consumedFlashColor;
            Gizmos.DrawWireSphere(lastConsumedPosition, 0.3f);
        }
    }
}