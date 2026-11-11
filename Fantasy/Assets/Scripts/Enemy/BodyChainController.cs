using UnityEngine;

public class BodyChainController : MonoBehaviour
{
    [Header("Head")]
    [SerializeField] private Transform head;

    [Header("Segment Generation")]
    [SerializeField] private bool autoGenerateSegments = true;
    [SerializeField] private Transform segmentPrefab;
    [SerializeField] private Transform lastSegmentPrefab;
    [SerializeField] private int segmentCount = 10;
    [SerializeField] private Transform segmentsParent;

    [Header("Segments")]
    public Transform firstSegment;
    public Transform lastSegment;
    [SerializeField] private Transform[] segments;
    [SerializeField] private float segmentSpacing = 0.4f;
    [SerializeField] private float followSpeed = 12f;

    [Header("Segment Scale Profile")]
    [SerializeField] private float minSegmentScale = 0.6f;
    [SerializeField] private float maxSegmentScale = 1f;
    [SerializeField] private bool applyScaleProfileToLastSegment = true;
    [SerializeField] private AnimationCurve scaleProfile = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.4f, 1f),
        new Keyframe(0.6f, 1f),
        new Keyframe(1f, 0f)
    );

    [Header("Sorting Order")]
    [Tooltip("Se ligado, este script define a ordem da cabeça e dos segmentos, e fornece a ordem de dentes e pernas.")]
    [SerializeField] private bool controlSortingOrder = true;
    [Tooltip("SpriteRenderer do sprite da cabeça (o objeto com HeadSpriteFacing).")]
    [SerializeField] private SpriteRenderer headRenderer;
    [SerializeField] private int headSortingOrder = 100;
    [Tooltip("Quantas ordens abaixo da cabeça ficam os dentes.")]
    [SerializeField] private int mandibleOrderOffset = 1;
    [Tooltip("Quantas ordens abaixo da cabeça fica o primeiro segmento. Cada segmento seguinte fica 1 abaixo do anterior.")]
    [SerializeField] private int firstSegmentOrderOffset = 1;
    [Tooltip("Quantas ordens abaixo do último segmento ficam as coxas das pernas. As canelas ficam 1 abaixo das coxas.")]
    [SerializeField] private int legsOrderGap = 1;

    [Header("Compression")]
    [SerializeField] private float compressionMoveSpeed = 3f;
    [SerializeField] private float compressionRotationSpeed = 8f;
    [SerializeField] private float compressionPackingScale = 1f;

    [Header("Shake")]
    [SerializeField] private float shakeFrequency = 18f;
    [SerializeField] private float shakePhaseDelay = 0.6f;
    [SerializeField, Range(0f, 1f)] private float shakeJitterStrength = 0.65f;
    [SerializeField] private float shakeNoiseSpeed = 3f;
    [SerializeField] private float shakeSecondaryAxisStrength = 0.35f;
    [SerializeField] private float shakeRotationJitterDegrees = 8f;
    [SerializeField] private float shakeAmplitudeBase = 0.02f;
    [SerializeField] private float shakeAmplitudeGrowth = 0.015f;
    [SerializeField, Range(0f, 1f)] private float shakeIdleIntensity = 0.3f;
    [SerializeField] private float shakeIntensitySmoothing = 6f;

    private Vector2[] segmentPositions;
    private float[] segmentNoiseSeeds;

    private bool isCompressing;
    private Vector2 compressionCenter;
    private float compressionRadius;

    private float shakeIntensity = 1f;
    private float targetShakeIntensity = 1f;

    public Vector2 FirstSegmentPos => segmentPositions != null && segmentPositions.Length > 0
        ? segmentPositions[0]
        : (Vector2)transform.position;

    // ---------- Sorting order (lido por MandibleController e LegSpriteSkin) ----------

    public bool ControlsSortingOrder => controlSortingOrder;
    public int HeadSortingOrder => headSortingOrder;
    public int MandibleSortingOrder => headSortingOrder - mandibleOrderOffset;

    public int GetSegmentSortingOrder(int index)
    {
        return headSortingOrder - firstSegmentOrderOffset - index;
    }

    public int LowestBodySortingOrder
    {
        get
        {
            int count = segments != null ? segments.Length : 0;
            return count > 0
                ? GetSegmentSortingOrder(count - 1)
                : headSortingOrder - firstSegmentOrderOffset;
        }
    }

    public int ThighSortingOrder => LowestBodySortingOrder - legsOrderGap;
    public int ShinSortingOrder => ThighSortingOrder - 1;

    public static void SetSortingOrder(Transform root, int order)
    {
        if (root == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sortingOrder = order;
    }

    private void ApplySortingOrders()
    {
        if (!controlSortingOrder)
            return;

        if (headRenderer != null)
            headRenderer.sortingOrder = headSortingOrder;
        else
            Debug.LogWarning($"{name}: headRenderer não atribuído, a ordem da cabeça não foi aplicada", this);

        for (int i = 0; i < segments.Length; i++)
            SetSortingOrder(segments[i], GetSegmentSortingOrder(i));
    }

    // ------------------------------------------------------------------------------

    private void Awake()
    {
        if (autoGenerateSegments)
            GenerateSegments();

        if (segments == null || segments.Length == 0)
        {
            Debug.LogError($"{name}: nenhum segmento disponível", this);
            enabled = false;
            return;
        }

        firstSegment = segments[0];
        lastSegment = segments[^1];

        segmentPositions = new Vector2[segments.Length];
        segmentNoiseSeeds = new float[segments.Length];
        Vector2 pos = head.position;

        for (int i = 0; i < segments.Length; i++)
        {
            pos -= (Vector2)(head.right) * segmentSpacing;
            segmentPositions[i] = pos;
            segments[i].position = pos;
            segmentNoiseSeeds[i] = Random.Range(0f, 1000f);
        }

        ApplySegmentScales();
        ApplySortingOrders();
    }

    private void GenerateSegments()
    {
        if (segmentPrefab == null)
        {
            Debug.LogWarning($"{name}: missing prefab", this);
            return;
        }

        Transform parent = segmentsParent != null ? segmentsParent : transform;
        int count = Mathf.Max(1, segmentCount);
        segments = new Transform[count];

        for (int i = 0; i < count; i++)
        {
            bool isLast = i == count - 1;
            Transform prefab = (isLast && lastSegmentPrefab != null) ? lastSegmentPrefab : segmentPrefab;

            Transform seg = Instantiate(prefab, head.position, Quaternion.identity, parent);
            seg.name = isLast && lastSegmentPrefab != null ? $"Segment_{i:00}_Last" : $"Segment_{i:00}";
            segments[i] = seg;
        }
    }

    private void ApplySegmentScales()
    {
        int count = segments.Length;
        for (int i = 0; i < count; i++)
        {
            bool isLast = i == count - 1;
            if (isLast && !applyScaleProfileToLastSegment)
                continue;

            float t = count <= 1 ? 0f : (float)i / (count - 1);
            float scaleValue = Mathf.Lerp(minSegmentScale, maxSegmentScale, scaleProfile.Evaluate(t));
            segments[i].localScale = Vector3.one * scaleValue;
        }
    }

    private void LateUpdate()
    {
        if (isCompressing)
        {
            UpdateCompression();
            return;
        }

        shakeIntensity = Mathf.MoveTowards(shakeIntensity, targetShakeIntensity, shakeIntensitySmoothing * Time.deltaTime);

        Vector2 targetPos = head.position;

        for (int i = 0; i < segments.Length; i++)
        {
            Vector2 currentPos = segmentPositions[i];
            float dist = Vector2.Distance(targetPos, currentPos);

            if (dist > segmentSpacing)
            {
                Vector2 dir = (currentPos - targetPos).normalized;
                Vector2 desired = targetPos + dir * segmentSpacing;
                currentPos = Vector2.Lerp(currentPos, desired, followSpeed * Time.deltaTime);
            }

            segmentPositions[i] = currentPos;

            Vector2 lookDir = targetPos - currentPos;
            Vector2 facing = lookDir.sqrMagnitude > 0.0001f ? lookDir.normalized : (Vector2)segments[i].right;
            Vector2 perpendicular = new Vector2(-facing.y, facing.x);

            float seed = segmentNoiseSeeds[i];
            float phase = Time.time * shakeFrequency - i * shakePhaseDelay;
            float cleanWave = Mathf.Sin(phase);

            float perpNoise = Mathf.PerlinNoise(Time.time * shakeNoiseSpeed + seed, 0.17f) * 2f - 1f;
            float perpWave = Mathf.Lerp(cleanWave, perpNoise, shakeJitterStrength);
            float axisNoise = Mathf.PerlinNoise(seed + 50f, Time.time * shakeNoiseSpeed * 1.3f) * 2f - 1f;

            float amplitude = (shakeAmplitudeBase + shakeAmplitudeGrowth * i) * shakeIntensity;

            Vector2 renderedPos = currentPos;
            renderedPos += perpendicular * perpWave * amplitude;
            renderedPos += facing * axisNoise * amplitude * shakeSecondaryAxisStrength;

            segments[i].position = renderedPos;

            if (lookDir.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
                segments[i].rotation = Quaternion.Euler(0, 0, angle);
            }

            float rotNoise = Mathf.PerlinNoise(seed + 100f, Time.time * shakeNoiseSpeed * 1.6f) * 2f - 1f;
            float rotationAmplitude = shakeRotationJitterDegrees * shakeIntensity * (segments.Length <= 1 ? 1f : (float)i / (segments.Length - 1));
            segments[i].rotation *= Quaternion.Euler(0f, 0f, rotNoise * rotationAmplitude);

            targetPos = currentPos;
        }
    }

    private void UpdateCompression()
    {
        const float goldenAngle = 2.399963f;

        for (int i = 0; i < segments.Length; i++)
        {
            float angle = i * goldenAngle;
            float packRadius = compressionRadius * compressionPackingScale * Mathf.Sqrt((float)(i + 1) / segments.Length);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * packRadius;
            Vector2 desired = compressionCenter + offset;

            Vector2 currentPos = Vector2.MoveTowards(segmentPositions[i], desired, compressionMoveSpeed * Time.deltaTime);
            segmentPositions[i] = currentPos;
            segments[i].position = currentPos;

            Vector2 lookDir = desired - currentPos;
            if (lookDir.sqrMagnitude < 0.0001f)
                lookDir = compressionCenter - currentPos;

            if (lookDir.sqrMagnitude > 0.0001f)
            {
                float rotAngle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
                Quaternion targetRot = Quaternion.Euler(0, 0, rotAngle);
                segments[i].rotation = Quaternion.Slerp(segments[i].rotation, targetRot, compressionRotationSpeed * Time.deltaTime);
            }
        }
    }

    public void SetCompression(bool active, Vector2 center, float radius)
    {
        isCompressing = active;
        compressionCenter = center;
        compressionRadius = radius;
    }

    public void SetMoving(bool isMoving)
    {
        targetShakeIntensity = isMoving ? 1f : shakeIdleIntensity;
    }

    public void UpdateCompressionCenter(Vector2 center)
    {
        compressionCenter = center;
    }
}