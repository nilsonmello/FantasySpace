using UnityEngine;

public class MandibleController : MonoBehaviour
{
    [Header("positions")]
    [SerializeField] private HeadStateMovement headMovement;
    [SerializeField] private Transform player;
    [SerializeField] private Transform leftMandible;
    [SerializeField] private Transform rightMandible;

    [Header("angles")]
    [SerializeField] private float closedAngle = 0f;
    [SerializeField] private float maxOpenAngle = 35f;
    [SerializeField] private float openSmoothTime = 0.08f;

    [Header("idle chatter)")]
    [SerializeField] private float chatterSpeed = 2f;
    [SerializeField, Range(0f, 1f)] private float chatterOpenAmount = 0.25f;
    [SerializeField] private float chatterNoiseSpeed = 1.5f;

    [Header("search")]
    [SerializeField, Range(0f, 1f)] private float patrolOpenAmount = 0.35f;

    [Header("chase")]
    [SerializeField] private float chompSpeed = 10f;
    [SerializeField] private float chaseRangeReference = 6f;
    [SerializeField] private float biteTriggerDistance = 0.6f;
    [SerializeField] private float biteSnapSpeed = 25f;
    [SerializeField] private float biteHoldTime = 0.15f;

    private float currentOpenAmount;
    private float openVelocity;
    private float noiseSeed;

    private bool isBiting;
    private float biteTimer;
    private float bitePhase;

    public bool IsBiting => isBiting;

    private void Reset()
    {
        headMovement = GetComponentInParent<HeadStateMovement>();
    }

    private void Awake()
    {
        noiseSeed = Random.Range(0f, 1000f);
    }

    private void Update()
    {
        if (headMovement == null)
            return;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        float targetOpen = ComputeTargetOpen();
        currentOpenAmount = Mathf.SmoothDamp(currentOpenAmount, targetOpen, ref openVelocity, openSmoothTime);
        ApplyMandibleAngle(currentOpenAmount);
    }

    private float ComputeTargetOpen()
    {
        switch (headMovement.CurrentState)
        {
            case HeadStateMovement.State.Hide:
                isBiting = false;
                return 0f;

            case HeadStateMovement.State.Chase:
                return HandleChase();

            case HeadStateMovement.State.Patrol:
                return HandleChatter(patrolOpenAmount);

            case HeadStateMovement.State.Wander:
            default:
                return HandleChatter(chatterOpenAmount);
        }
    }

    private float HandleChatter(float amplitude)
    {
        float noise = Mathf.PerlinNoise(Time.time * chatterNoiseSpeed + noiseSeed, 0f);
        float wave = Mathf.Sin(Time.time * chatterSpeed) * 0.5f + 0.5f;
        return Mathf.Lerp(0f, amplitude, wave * noise * 1.3f);
    }

    private float HandleChase()
    {
        if (isBiting)
        {
            biteTimer -= Time.deltaTime;
            bitePhase += Time.deltaTime * biteSnapSpeed;

            float snap = Mathf.Max(0f, Mathf.Sin(bitePhase));

            if (biteTimer <= 0f)
                isBiting = false;

            return snap;
        }

        float dist = player != null
            ? Vector2.Distance(transform.position, player.position)
            : Mathf.Infinity;

        if (dist <= biteTriggerDistance)
        {
            StartBite();
            return 1f;
        }

        float proximity = Mathf.InverseLerp(chaseRangeReference, biteTriggerDistance, dist);
        float chompWave = Mathf.Sin(Time.time * chompSpeed) * 0.5f + 0.5f;

        return Mathf.Clamp01(chompWave * 0.5f + proximity * 0.5f);
    }

    private void StartBite()
    {
        isBiting = true;
        biteTimer = biteHoldTime;
        bitePhase = 0f;
    }

    private void ApplyMandibleAngle(float openAmount)
    {
        float angle = Mathf.Lerp(closedAngle, maxOpenAngle, openAmount);

        if (leftMandible != null)
            leftMandible.localRotation = Quaternion.Euler(0f, 0f, angle);

        if (rightMandible != null)
            rightMandible.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }

    public void ForceBite()
    {
        StartBite();
    }
}
