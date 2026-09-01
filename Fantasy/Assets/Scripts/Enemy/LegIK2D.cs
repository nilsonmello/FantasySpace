using UnityEngine;

public class LegIK2D : MonoBehaviour
{
    [Header("Hip")]
    [SerializeField] private Transform hip;

    [Header("Visual")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private AnimationCurve widthCurve = AnimationCurve.Linear(0f, 0.08f, 1f, 0.03f);

    [Header("Length")]
    [SerializeField] private float upperLength = 0.5f;
    [SerializeField] private float lowerLength = 0.5f;

    [Header("Elbow")]
    [SerializeField] private float elbowSide = 1f;
    [SerializeField] private float kneeSmoothSpeed = 15f;

    [Header("Rest Position")]
    [SerializeField] private Vector2 restOffset = new Vector2(0.5f, 0f);
    [SerializeField] private float stepTriggerDistance = 0.6f;
    [SerializeField] private float stepDuration = 0.15f;
    [SerializeField] private float stepOvershoot = 0.15f;
    [SerializeField] private LegIK2D waveParent;

    [Header("Rithm")]
    [SerializeField] private float phaseOffset = 0f;
    [SerializeField, Range(0f, 0.5f)] private float cadenceJitter = 0.15f;
    [SerializeField] private int randomSeed = 0;

    public bool IsStepping { get; private set; }
    public Vector2 KneeWorldPos => currentKneePos;
    public Vector2 FootWorldPos => currentFootPos;

    private Vector2 currentFootPos;
    private Vector2 currentKneePos;
    private Vector2 stepStartPos, stepEndPos;
    private float stepTimer;
    private Vector2 lastHipPos, hipVelocityDir;

    private float nextEligibleStepTime;
    private float effectiveTriggerDistance;
    private float effectiveStepDuration;

    private void Awake()
    {
        currentFootPos = (Vector2)hip.position + RotatedRestOffset();
        stepEndPos = currentFootPos;
        lastHipPos = hip.position;

        float autoElbowSide = Mathf.Sign(restOffset.y == 0f ? 1f : restOffset.y) * elbowSide;
        Vector2 initialDir = (currentFootPos - (Vector2)hip.position).normalized;
        Vector2 initialPerp = new Vector2(-initialDir.y, initialDir.x) * autoElbowSide;
        currentKneePos = (Vector2)hip.position + initialDir * upperLength * 0.5f + initialPerp * 0.1f;

        int seed = randomSeed != 0 ? randomSeed : GetInstanceID();
        var rng = new System.Random(seed);
        float jitterT = (float)(rng.NextDouble() * 2.0 - 1.0);

        effectiveTriggerDistance = stepTriggerDistance * (1f + jitterT * cadenceJitter);
        effectiveStepDuration = stepDuration * (1f + jitterT * cadenceJitter);

        nextEligibleStepTime = Time.time + phaseOffset;

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 3;
            lineRenderer.widthCurve = widthCurve;
            lineRenderer.widthMultiplier = 1f;
            lineRenderer.useWorldSpace = true;
        }
    }

    private void Update()
    {
        Vector2 hipDelta = (Vector2)hip.position - lastHipPos;
        if (hipDelta.sqrMagnitude > 0.00001f)
            hipVelocityDir = hipDelta.normalized;
        lastHipPos = hip.position;

        Vector2 desiredRest = (Vector2)hip.position + RotatedRestOffset();
        bool waveClear = waveParent == null || !waveParent.IsStepping;
        bool timeClear = Time.time >= nextEligibleStepTime;

        if (!IsStepping && waveClear && timeClear &&
            Vector2.Distance(currentFootPos, desiredRest) > effectiveTriggerDistance)
        {
            stepStartPos = currentFootPos;
            stepEndPos = desiredRest + hipVelocityDir * stepOvershoot;
            stepTimer = 0f;
            IsStepping = true;
        }

        if (IsStepping)
        {
            stepTimer += Time.deltaTime;
            float t = Mathf.Clamp01(stepTimer / effectiveStepDuration);
            t = t * t * (3f - 2f * t);
            currentFootPos = Vector2.Lerp(stepStartPos, stepEndPos, t);

            if (t >= 1f)
            {
                IsStepping = false;
                nextEligibleStepTime = Time.time;
            }
        }

        SolveAndDraw(currentFootPos);
    }

    private Vector2 RotatedRestOffset() => hip.rotation * restOffset;

    private void SolveAndDraw(Vector2 targetWorldPos)
    {
        Vector2 hipPos = hip.position;
        Vector2 toTarget = targetWorldPos - hipPos;
        float maxReach = upperLength + lowerLength - 0.001f;
        float c = Mathf.Clamp(toTarget.magnitude, 0.0001f, maxReach);
        Vector2 dir = toTarget / Mathf.Max(toTarget.magnitude, 0.0001f);

        float a = upperLength, b = lowerLength;
        float a2 = Mathf.Clamp((c * c + a * a - b * b) / (2f * c), 0f, a);
        float h = Mathf.Sqrt(Mathf.Max(0f, a * a - a2 * a2));

        Vector2 mid = hipPos + dir * a2;
        Vector2 perp = new Vector2(-dir.y, dir.x);

        Vector2 candidateA = mid + perp * h;
        Vector2 candidateB = mid - perp * h;

        Vector2 targetKnee =
            (candidateA - currentKneePos).sqrMagnitude <= (candidateB - currentKneePos).sqrMagnitude
            ? candidateA
            : candidateB;

        currentKneePos = Vector2.Lerp(currentKneePos, targetKnee, kneeSmoothSpeed * Time.deltaTime);

        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, hipPos);
            lineRenderer.SetPosition(1, currentKneePos);
            lineRenderer.SetPosition(2, targetWorldPos);
        }
    }
}