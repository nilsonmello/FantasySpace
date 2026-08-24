using UnityEngine;

public class AntennaIK : MonoBehaviour
{
    [Header("Socket (ancoragem)")]
    [SerializeField] private Transform socket;

    [Header("Visual")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float antennaWidth = 0.03f;

    [Header("Segmentos")]
    [SerializeField] private int segmentCount = 4;
    [SerializeField] private float segmentLength = 0.15f;
    [SerializeField] private int fabrikIterations = 8;

    [Header("Pose de descanso")]
    [SerializeField] private float splayAngleDeg = 30f;
    [SerializeField] private bool mirrorSide = false;
    [SerializeField] private float sideBiasDeg = 0f;
    [SerializeField, Range(0.1f, 1f)] private float restReachFactor = 0.75f;
    [SerializeField] private float targetFollowSpeed = 8f;

    [Header("Direção de referência")]
    [SerializeField] private bool followMovementDirection = true;
    [SerializeField] private float headingMinSpeed = 0.05f;
    [SerializeField] private float headingSmoothSpeed = 6f;

    [Header("Busca (a ponta vagueia ao redor do repouso)")]
    [SerializeField] private float searchRadius = 0.15f;
    [SerializeField] private float searchSpeed = 0.8f;

    [Header("Tilt / shake de busca (visual, não afeta o IK)")]
    [SerializeField] private float tiltFrequency = 3f;
    [SerializeField] private float tiltPhasePerSegment = 0.6f;
    [SerializeField] private float tiltAmplitudeBase = 0.01f;
    [SerializeField] private float tiltAmplitudeGrowth = 0.02f;

    private Vector2[] jointPositions;
    private Vector2 currentTarget;
    private Vector2 currentHeading = Vector2.up;
    private Vector2 lastSocketPos;
    private float noiseSeedX, noiseSeedY;
    private float totalReach;

    private void Awake()
    {
        jointPositions = new Vector2[segmentCount + 1];
        totalReach = segmentLength * segmentCount;
        lastSocketPos = socket.position;
        currentHeading = socket.up;

        currentTarget = GetRestTarget();

        for (int i = 0; i <= segmentCount; i++)
            jointPositions[i] = (Vector2)socket.position + currentHeading * (segmentLength * i);

        noiseSeedX = Random.Range(0f, 1000f);
        noiseSeedY = Random.Range(0f, 1000f);

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = jointPositions.Length;
            lineRenderer.startWidth = antennaWidth;
            lineRenderer.endWidth = antennaWidth;
            lineRenderer.useWorldSpace = true;
        }
    }

    private void Update()
    {
        Vector2 velocity = ((Vector2)socket.position - lastSocketPos) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastSocketPos = socket.position;

        if (followMovementDirection)
        {
            if (velocity.magnitude > headingMinSpeed)
            {
                Vector2 targetHeading = velocity.normalized;
                currentHeading = Vector2.Lerp(currentHeading, targetHeading, 1f - Mathf.Exp(-headingSmoothSpeed * Time.deltaTime));
                currentHeading.Normalize();
            }
        }
        else
        {
            currentHeading = socket.up;
        }

        Vector2 restTarget = GetRestTarget();

        float nx = Mathf.PerlinNoise(noiseSeedX, Time.time * searchSpeed) * 2f - 1f;
        float ny = Mathf.PerlinNoise(noiseSeedY, Time.time * searchSpeed) * 2f - 1f;
        Vector2 searchOffset = new Vector2(nx, ny) * searchRadius;

        Vector2 desiredTarget = restTarget + searchOffset;
        currentTarget = Vector2.Lerp(currentTarget, desiredTarget, 1f - Mathf.Exp(-targetFollowSpeed * Time.deltaTime));

        SolveFABRIK(currentTarget);
        RenderWithTilt();
    }

    private Vector2 GetRestTarget()
    {
        float signedSplay = mirrorSide ? -splayAngleDeg : splayAngleDeg;
        float totalAngle = signedSplay + sideBiasDeg;
        Vector2 dir = Quaternion.Euler(0, 0, totalAngle) * currentHeading;
        return (Vector2)socket.position + dir * (totalReach * restReachFactor);
    }

    private void SolveFABRIK(Vector2 target)
    {
        Vector2 root = socket.position;
        float distToTarget = Vector2.Distance(root, target);

        if (distToTarget >= totalReach)
        {
            Vector2 dir = (target - root).normalized;
            jointPositions[0] = root;
            for (int i = 1; i < jointPositions.Length; i++)
                jointPositions[i] = jointPositions[i - 1] + dir * segmentLength;
            return;
        }

        for (int iter = 0; iter < fabrikIterations; iter++)
        {
            jointPositions[jointPositions.Length - 1] = target;
            for (int i = jointPositions.Length - 2; i >= 0; i--)
            {
                Vector2 dir = (jointPositions[i] - jointPositions[i + 1]).normalized;
                jointPositions[i] = jointPositions[i + 1] + dir * segmentLength;
            }

            jointPositions[0] = root;
            for (int i = 1; i < jointPositions.Length; i++)
            {
                Vector2 dir = (jointPositions[i] - jointPositions[i - 1]).normalized;
                jointPositions[i] = jointPositions[i - 1] + dir * segmentLength;
            }

            if (Vector2.Distance(jointPositions[jointPositions.Length - 1], target) < 0.001f)
                break;
        }
    }

    private void RenderWithTilt()
    {
        if (lineRenderer == null) return;

        for (int i = 0; i < jointPositions.Length; i++)
        {
            Vector2 renderedPos = jointPositions[i];

            if (i > 0)
            {
                Vector2 segDir = (jointPositions[i] - jointPositions[i - 1]).normalized;
                Vector2 perp = new Vector2(-segDir.y, segDir.x);

                float phase = Time.time * tiltFrequency - i * tiltPhasePerSegment;
                float amplitude = tiltAmplitudeBase + tiltAmplitudeGrowth * i;
                renderedPos += perp * Mathf.Sin(phase) * amplitude;
            }

            lineRenderer.SetPosition(i, renderedPos);
        }
    }
}