using System;
using UnityEngine;

public class MandibleController : MonoBehaviour
{
    public enum MandibleSide
    {
        Left,
        Right
    }

    [Serializable]
    public class Mandible
    {
        public Transform transform;
        public MandibleSide side = MandibleSide.Left;
        public float openMultiplier = 1f;
        public float angleOffset = 0f;
    }

    [Header("positions")]
    [SerializeField] private HeadStateMovement headMovement;
    [SerializeField] private Transform player;

    [Header("mandibles")]
    [SerializeField] private Mandible[] mandibles = new Mandible[0];

    [SerializeField, HideInInspector] private Transform leftMandible;
    [SerializeField, HideInInspector] private Transform rightMandible;

    [Header("sorting order")]
    [Tooltip("Se vazio, procura nos pais e depois na cena.")]
    [SerializeField] private BodyChainController bodyChain;
    [SerializeField] private bool useBodyChainOrder = true;

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

    private bool overrideActive;
    private float overrideTarget;
    private float overrideSmoothTime;

    public bool IsBiting => isBiting;
    public bool HasOverride => overrideActive;

    public event Action OnBiteFinished;

    private void Reset()
    {
        headMovement = GetComponentInParent<HeadStateMovement>();
        bodyChain = GetComponentInParent<BodyChainController>();
    }

    private void OnValidate()
    {
        MigrateLegacyMandibles();
    }

    private void Awake()
    {
        noiseSeed = UnityEngine.Random.Range(0f, 1000f);
        MigrateLegacyMandibles();
    }

    // Start (e não Awake) pra garantir que o BodyChainController já gerou os segmentos
    private void Start()
    {
        ApplySortingOrder();
    }

    private void ApplySortingOrder()
    {
        if (!useBodyChainOrder || mandibles == null)
            return;

        if (bodyChain == null)
            bodyChain = GetComponentInParent<BodyChainController>();

        if (bodyChain == null)
            bodyChain = FindFirstObjectByType<BodyChainController>();

        if (bodyChain == null || !bodyChain.ControlsSortingOrder)
            return;

        int order = bodyChain.MandibleSortingOrder;

        for (int i = 0; i < mandibles.Length; i++)
        {
            if (mandibles[i] == null)
                continue;

            BodyChainController.SetSortingOrder(mandibles[i].transform, order);
        }
    }

    private void MigrateLegacyMandibles()
    {
        if (mandibles != null && mandibles.Length > 0)
            return;

        if (leftMandible == null && rightMandible == null)
            return;

        int count = (leftMandible != null ? 1 : 0) + (rightMandible != null ? 1 : 0);
        mandibles = new Mandible[count];

        int i = 0;
        if (leftMandible != null)
            mandibles[i++] = new Mandible { transform = leftMandible, side = MandibleSide.Left };
        if (rightMandible != null)
            mandibles[i++] = new Mandible { transform = rightMandible, side = MandibleSide.Right };

        leftMandible = null;
        rightMandible = null;
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

        float targetOpen;
        float smooth = openSmoothTime;

        if (overrideActive)
        {
            targetOpen = overrideTarget;
            smooth = overrideSmoothTime;
        }
        else
        {
            targetOpen = ComputeTargetOpen();
        }

        currentOpenAmount = Mathf.SmoothDamp(currentOpenAmount, targetOpen, ref openVelocity, Mathf.Max(smooth, 0.0001f));
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
            {
                isBiting = false;
                OnBiteFinished?.Invoke();
            }

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
        if (mandibles == null)
            return;

        for (int i = 0; i < mandibles.Length; i++)
        {
            Mandible m = mandibles[i];
            if (m == null || m.transform == null)
                continue;

            float angle = Mathf.LerpUnclamped(closedAngle, maxOpenAngle, openAmount * m.openMultiplier) + m.angleOffset;
            float sign = m.side == MandibleSide.Left ? 1f : -1f;

            m.transform.localRotation = Quaternion.Euler(0f, 0f, angle * sign);
        }
    }

    public void ForceBite()
    {
        StartBite();
    }

    public void SetOpenOverride(float openAmount, float smoothTime)
    {
        overrideActive = true;
        overrideTarget = openAmount;
        overrideSmoothTime = smoothTime;
        isBiting = false;
    }

    public void ClearOpenOverride()
    {
        overrideActive = false;
    }
}