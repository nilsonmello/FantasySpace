using UnityEngine;

public class AntennaIK : MonoBehaviour
{
    public enum TargetMode
    {
        Idle,
        IdleInverted,
        Search,
        Mouse
    }

    public enum AntennaRole
    {
        Front,
        Rear
    }

    [Header("Role")]
    [SerializeField] private AntennaRole role = AntennaRole.Front;

    [SerializeField] private HeadStateMovement bodyState;

    [Header("Body State Mapping (Front only)")]
    [SerializeField] private TargetMode wanderMode = TargetMode.Idle;
    [SerializeField] private TargetMode chaseMode = TargetMode.Search;
    [SerializeField] private TargetMode hideMode = TargetMode.IdleInverted;
    [SerializeField] private TargetMode patrolMode = TargetMode.Search;

    [Header("Socket")]
    [SerializeField] private Transform socket;

    [Header("Visual")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private AnimationCurve widthCurve = AnimationCurve.Linear(0f, 0.05f, 1f, 0.015f);

    [Header("Curve Resolution")]
    [SerializeField] private int segmentCount = 7;
    [SerializeField] private float segmentLength = 0.15f;

    [Header("Rest Pose")]
    [SerializeField] private float splayAngleDeg = 30f;
    [SerializeField] private bool mirrorSide = false;
    [SerializeField] private float sideBiasDeg = 0f;
    [SerializeField, Range(0.1f, 1f)] private float restReachFactor = 0.75f;
    [SerializeField] private float targetFollowSpeed = 4f;

    [Header("Reference Direction")]
    [SerializeField] private bool followMovementDirection = true;
    [SerializeField] private float headingMinSpeed = 0.05f;
    [SerializeField] private float headingSmoothSpeed = 6f;
    [SerializeField] private float velocityFilterSpeed = 10f;

    [Header("Search Variables")]
    [SerializeField] private float searchRadius = 0.05f;
    [SerializeField] private float searchSpeed = 0.35f;

    [Header("TTap Info")]
    [SerializeField] private float tapRadius = 0.09f;
    [SerializeField] private float tapIntervalMin = 0.12f;
    [SerializeField] private float tapIntervalMax = 0.4f;
    [SerializeField] private float tapFollowSpeed = 12f;
    [SerializeField, Range(0.1f, 1f)] private float tapMaxReachFactor = 0.85f;

    [Header("Bezier")]
    [SerializeField, Range(0.05f, 0.6f)] private float controlNearT = 0.33f;
    [SerializeField, Range(0.4f, 0.95f)] private float controlFarT = 0.7f;

    [SerializeField] private float bendAmountNear = 0.09f;
    [SerializeField] private float bendAmountFar = -0.045f;

    [SerializeField] private float elbowNearFollowSpeed = 3.5f;
    [SerializeField] private float elbowFarFollowSpeed = 1.4f;

    [SerializeField] private bool bendNoiseEnabled = true;
    [SerializeField, Range(0f, 1f)] private float bendNoiseScale = 0.25f;
    [SerializeField] private float bendNoiseSpeed = 0.2f;

    [Header("Aim Mode")]
    [SerializeField] private TargetMode targetMode = TargetMode.Idle;
    [SerializeField] private Camera targetCamera;
    [Range(0.1f, 1f)]
    [SerializeField] private float mouseReachFactor = 0.95f;
    [SerializeField] private bool addSearchNoiseInMouseMode = false;
    [SerializeField, Range(0f, 1f)] private float mouseSearchNoiseScale = 0.3f;

    [Header("Tilt")]
    [SerializeField] private float tiltFrequency = 1.5f;
    [SerializeField] private float tiltPhasePerSegment = 0.5f;
    [SerializeField] private float tiltAmplitudeBase = 0.002f;
    [SerializeField] private float tiltAmplitudeGrowth = 0.003f;

    private Vector2[] jointPositions;
    private Vector2 currentTarget;
    private Vector2 elbowNearPos;
    private Vector2 elbowFarPos;
    private Vector2 currentHeading = Vector2.up;
    private Vector2 lastSocketPos;
    private Vector2 filteredVelocity;
    private float noiseSeedX, noiseSeedY, noiseSeedBendNear, noiseSeedBendFar;
    private float totalReach;

    private Vector2 tapAnchor;
    private float nextTapTime;

    private void Awake()
    {
        jointPositions = new Vector2[segmentCount + 1];
        totalReach = segmentLength * segmentCount;
        lastSocketPos = socket.position;
        currentHeading = socket.up;

        if (targetCamera == null)
            targetCamera = Camera.main;

        noiseSeedX = Random.Range(0f, 1000f);
        noiseSeedY = Random.Range(0f, 1000f);
        noiseSeedBendNear = Random.Range(0f, 1000f);
        noiseSeedBendFar = Random.Range(0f, 1000f);

        if (role == AntennaRole.Rear)
        {
            targetMode = TargetMode.IdleInverted;
        }
        else if (bodyState != null)
        {
            bodyState.OnStateChanged += HandleBodyStateChanged;
            HandleBodyStateChanged(bodyState.CurrentState);
        }

        currentTarget = GetRestTarget();
        elbowNearPos = GetDesiredControlPoint(currentTarget, controlNearT, bendAmountNear, noiseSeedBendNear);
        elbowFarPos = GetDesiredControlPoint(currentTarget, controlFarT, bendAmountFar, noiseSeedBendFar);

        tapAnchor = currentTarget;
        nextTapTime = 0f;

        SampleBezier();

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = jointPositions.Length;
            lineRenderer.widthCurve = widthCurve;
            lineRenderer.widthMultiplier = 1f;
            lineRenderer.useWorldSpace = true;
        }
    }

    private void OnDestroy()
    {
        if (role == AntennaRole.Front && bodyState != null)
            bodyState.OnStateChanged -= HandleBodyStateChanged;
    }

    private void HandleBodyStateChanged(HeadStateMovement.State state)
    {
        switch (state)
        {
            case HeadStateMovement.State.Wander:
                targetMode = wanderMode;
                break;
            case HeadStateMovement.State.Chase:
                targetMode = chaseMode;
                break;
            case HeadStateMovement.State.Hide:
                targetMode = hideMode;
                break;
            case HeadStateMovement.State.Patrol:
                targetMode = patrolMode;
                break;
        }
    }

    private void Update()
    {
        Vector2 rawVelocity = ((Vector2)socket.position - lastSocketPos) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastSocketPos = socket.position;
        filteredVelocity = Vector2.Lerp(filteredVelocity, rawVelocity, 1f - Mathf.Exp(-velocityFilterSpeed * Time.deltaTime));

        if (followMovementDirection)
        {
            if (filteredVelocity.magnitude > headingMinSpeed)
            {
                Vector2 targetHeading = filteredVelocity.normalized;
                currentHeading = Vector2.Lerp(currentHeading, targetHeading, 1f - Mathf.Exp(-headingSmoothSpeed * Time.deltaTime));
                currentHeading.Normalize();
            }
        }
        else
        {
            currentHeading = socket.up;
        }

        Vector2 desiredTip;
        float followSpeed;
        float reachFactor;

        switch (targetMode)
        {
            case TargetMode.Mouse:
                desiredTip = GetMouseTarget();
                followSpeed = targetFollowSpeed;
                reachFactor = mouseReachFactor;
                break;

            case TargetMode.Search:
                desiredTip = GetSearchTapTarget();
                followSpeed = tapFollowSpeed;
                reachFactor = tapMaxReachFactor;
                break;

            case TargetMode.IdleInverted:
                desiredTip = GetIdleTarget(-currentHeading);
                followSpeed = targetFollowSpeed;
                reachFactor = restReachFactor;
                break;

            default:
                desiredTip = GetIdleTarget(currentHeading);
                followSpeed = targetFollowSpeed;
                reachFactor = restReachFactor;
                break;
        }

        currentTarget = Vector2.Lerp(currentTarget, desiredTip, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
        currentTarget = ClampToReach(currentTarget, socket.position, totalReach * reachFactor);

        Vector2 desiredNear = GetDesiredControlPoint(currentTarget, controlNearT, bendAmountNear, noiseSeedBendNear);
        Vector2 desiredFar = GetDesiredControlPoint(currentTarget, controlFarT, bendAmountFar, noiseSeedBendFar);

        elbowNearPos = Vector2.Lerp(elbowNearPos, desiredNear, 1f - Mathf.Exp(-elbowNearFollowSpeed * Time.deltaTime));
        elbowFarPos = Vector2.Lerp(elbowFarPos, desiredFar, 1f - Mathf.Exp(-elbowFarFollowSpeed * Time.deltaTime));

        SampleBezier();
        RenderWithTilt();
    }

    private Vector2 GetIdleTarget()
    {
        return GetIdleTarget(currentHeading);
    }

    private Vector2 GetIdleTarget(Vector2 headingDir)
    {
        Vector2 restTarget = GetRestTarget(headingDir);

        float nx = Mathf.PerlinNoise(noiseSeedX, Time.time * searchSpeed) * 2f - 1f;
        float ny = Mathf.PerlinNoise(noiseSeedY, Time.time * searchSpeed) * 2f - 1f;
        Vector2 searchOffset = new Vector2(nx, ny) * searchRadius;

        return restTarget + searchOffset;
    }

    private Vector2 GetSearchTapTarget()
    {
        if (Time.time >= nextTapTime)
        {
            Vector2 restTarget = GetRestTarget();

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(0f, tapRadius);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            tapAnchor = restTarget + offset;
            nextTapTime = Time.time + Random.Range(tapIntervalMin, tapIntervalMax);
        }

        return tapAnchor;
    }

    private Vector2 GetMouseTarget()
    {
        Vector2 root = socket.position;
        Vector2 mouseWorld = GetMouseWorldPosition();

        float maxReach = totalReach * mouseReachFactor;
        Vector2 toMouse = mouseWorld - root;
        float dist = toMouse.magnitude;

        Vector2 target = dist > maxReach
            ? root + toMouse.normalized * maxReach
            : mouseWorld;

        if (addSearchNoiseInMouseMode)
        {
            float nx = Mathf.PerlinNoise(noiseSeedX, Time.time * searchSpeed) * 2f - 1f;
            float ny = Mathf.PerlinNoise(noiseSeedY, Time.time * searchSpeed) * 2f - 1f;
            target += new Vector2(nx, ny) * searchRadius * mouseSearchNoiseScale;
        }

        return target;
    }

    private Vector2 GetMouseWorldPosition()
    {
        if (targetCamera == null)
            return GetRestTarget();

        Vector3 screenPos = Input.mousePosition;
        screenPos.z = Mathf.Abs(targetCamera.transform.position.z - socket.position.z);
        Vector3 worldPos = targetCamera.ScreenToWorldPoint(screenPos);
        return new Vector2(worldPos.x, worldPos.y);
    }

    private Vector2 GetRestTarget()
    {
        return GetRestTarget(currentHeading);
    }

    private Vector2 GetRestTarget(Vector2 headingDir)
    {
        float signedSplay = mirrorSide ? -splayAngleDeg : splayAngleDeg;
        float totalAngle = signedSplay + sideBiasDeg;
        Vector2 dir = Quaternion.Euler(0, 0, totalAngle) * headingDir;
        return (Vector2)socket.position + dir * (totalReach * restReachFactor);
    }

    private Vector2 GetDesiredControlPoint(Vector2 tip, float chordT, float bend, float noiseSeed)
    {
        Vector2 root = socket.position;
        Vector2 chord = tip - root;
        float chordLen = chord.magnitude;
        Vector2 chordDir = chordLen > 0.0001f ? chord / chordLen : currentHeading;
        Vector2 perp = new Vector2(-chordDir.y, chordDir.x);

        float side = mirrorSide ? -1f : 1f;
        float signedBend = bend * side;

        if (bendNoiseEnabled)
        {
            float n = Mathf.PerlinNoise(noiseSeed, Time.time * bendNoiseSpeed) * 2f - 1f;
            signedBend += n * Mathf.Abs(bend) * bendNoiseScale;
        }

        Vector2 basePoint = root + chordDir * (chordLen * chordT);
        return basePoint + perp * signedBend;
    }

    private static Vector2 ClampToReach(Vector2 point, Vector2 root, float maxReach)
    {
        Vector2 toPoint = point - root;
        float dist = toPoint.magnitude;
        if (dist <= maxReach || dist < 0.0001f)
            return point;

        return root + toPoint / dist * maxReach;
    }

    private void SampleBezier()
    {
        Vector2 p0 = socket.position;
        Vector2 c1 = elbowNearPos;
        Vector2 c2 = elbowFarPos;
        Vector2 p3 = currentTarget;

        int count = jointPositions.Length;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            float u = 1f - t;

            float w0 = u * u * u;
            float w1 = 3f * u * u * t;
            float w2 = 3f * u * t * t;
            float w3 = t * t * t;

            jointPositions[i] = w0 * p0 + w1 * c1 + w2 * c2 + w3 * p3;
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
                Vector2 segDir = (jointPositions[i] - jointPositions[i - 1]);
                float segLen = segDir.magnitude;
                if (segLen > 0.0001f)
                {
                    segDir /= segLen;
                    Vector2 perp = new Vector2(-segDir.y, segDir.x);

                    float phase = Time.time * tiltFrequency - i * tiltPhasePerSegment;
                    float amplitude = tiltAmplitudeBase + tiltAmplitudeGrowth * i;
                    renderedPos += perp * Mathf.Sin(phase) * amplitude;
                }
            }

            lineRenderer.SetPosition(i, renderedPos);
        }
    }
}