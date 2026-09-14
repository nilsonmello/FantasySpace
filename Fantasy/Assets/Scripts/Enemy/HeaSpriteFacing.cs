using UnityEngine;

public class HeadSpriteFacing : MonoBehaviour
{
    [SerializeField] private HeadStateMovement headMovement;

    [SerializeField] private float rotationOffsetDegrees = 0f;

    [Header("tremble")]
    [SerializeField] private bool enableTremble = true;
    [SerializeField] private float trembleIntervalMin = 3f;
    [SerializeField] private float trembleIntervalMax = 6f;
    [SerializeField] private float trembleDuration = 0.3f;
    [SerializeField] private float trembleAmplitudeDegrees = 8f;
    [SerializeField] private float trembleFrequency = 30f;

    private float nextTrembleTime;
    private float trembleEndTime = -1f;
    private float trembleSeed;

    public float TrembleOffsetDegrees { get; private set; }

    private void Reset()
    {
        headMovement = GetComponentInParent<HeadStateMovement>();
    }

    private void Awake()
    {
        trembleSeed = Random.Range(0f, 1000f);
        ScheduleNextTremble();
    }

    private void LateUpdate()
    {
        if (headMovement == null) return;

        Vector2 dir = headMovement.FacingDirection;
        if (dir.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        TrembleOffsetDegrees = GetTrembleOffset();
        angle += TrembleOffsetDegrees;

        transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffsetDegrees);
    }

    private float GetTrembleOffset()
    {
        if (!enableTremble) return 0f;

        if (trembleEndTime < 0f)
        {
            if (Time.time < nextTrembleTime)
                return 0f;

            trembleEndTime = Time.time + trembleDuration;
        }

        if (Time.time >= trembleEndTime)
        {
            trembleEndTime = -1f;
            ScheduleNextTremble();
            return 0f;
        }

        float remaining = (trembleEndTime - Time.time) / trembleDuration;
        float decay = Mathf.Clamp01(remaining);
        float noise = Mathf.PerlinNoise(Time.time * trembleFrequency + trembleSeed, 0f) * 2f - 1f;

        return noise * trembleAmplitudeDegrees * decay;
    }

    private void ScheduleNextTremble()
    {
        nextTrembleTime = Time.time + Random.Range(trembleIntervalMin, trembleIntervalMax);
    }
}