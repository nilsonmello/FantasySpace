using UnityEngine;
using UnityEngine.Events;

public class HideoutInteractable : InteractionManager
{
    [Header("Hideout")]
    [SerializeField] private UnityEvent onPlayerEnter;
    [SerializeField] private UnityEvent onPlayerExit;

    [Header("Time Limit")]
    [SerializeField] private float maxHideTime = 10f;
    [SerializeField] private UnityEvent onTimeExpired;

    [Header("Time Over")]
    [SerializeField] private bool gameOverOnTimeExpired = true;
    [SerializeField] private UnityEvent onGameOver;
    [SerializeField] private float reentryDelay = 2f;

    [Header("Exit Cooldown")]
    [SerializeField] private float exitCooldown = 3f;

    [Header("Audio")]
    [SerializeField] private string enterSoundName = "HideoutEnter";
    [SerializeField] private string exitSoundName = "HideoutExit";
    [SerializeField, Range(0f, 1f)] private float soundVolume = 1f;

    [Header("Effects")]
    [SerializeField] private HideoutEffects effects;

    [Header("Camera")]
    [SerializeField] private Transform cameraPoint;

    [Header("Visibility Indicator")]
    [SerializeField] private Transform indicatorAnchor;
    [SerializeField] private Vector2 indicatorOffset = new Vector2(0f, 1f);

    private bool isOccupied;
    private bool dead;
    private GameObject currentOccupant;
    private PlayerVisibilityIndicator occupantIndicator;
    private CameraFollow cameraFollow;

    private float hideTimer;
    private float nextEnterTime;
    private float cooldownDuration;

    private static HideoutEffects cachedEffects;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cachedEffects = null;
    }

    public bool IsOccupied => isOccupied;
    public float CooldownRemaining => isOccupied ? 0f : Mathf.Max(0f, nextEnterTime - Time.time);
    public float CooldownNormalized => cooldownDuration > 0f ? Mathf.Clamp01(CooldownRemaining / cooldownDuration) : 0f;
    public float HideProgress => maxHideTime > 0f ? Mathf.Clamp01(hideTimer / maxHideTime) : 0f;

    public override bool PromptVisible => !isOccupied;
    public override string PromptDescription => "esconder-se";

    protected override void Awake()
    {
        base.Awake();
        ResolveEffects();
    }

    private void ResolveEffects()
    {
        if (effects != null) return;

        if (cachedEffects == null)
        {
            cachedEffects = FindFirstObjectByType<HideoutEffects>(FindObjectsInactive.Include);
        }

        effects = cachedEffects;
    }

    private void Update()
    {
        if (!isOccupied || dead || maxHideTime <= 0f) return;

        hideTimer += Time.deltaTime;

        if (effects != null)
            effects.SetProgress(HideProgress);

        if (hideTimer >= maxHideTime)
        {
            onTimeExpired?.Invoke();

            if (gameOverOnTimeExpired)
                TriggerGameOver();
            else
                Exit(true);
        }
    }

    public override bool Caninteract()
    {
        if (dead) return false;

        if (!isOccupied && Time.time < nextEnterTime) return false;

        return base.Caninteract();
    }

    protected override void OnInteract(GameObject interactor)
    {
        if (!isOccupied)
            Enter(interactor);
        else if (currentOccupant == interactor)
            Exit(false);
    }

    private void Enter(GameObject interactor)
    {
        isOccupied = true;
        currentOccupant = interactor;
        hideTimer = 0f;

        var movement = interactor.GetComponent<PlayerMovementBase>();
        var rb = interactor.GetComponent<Rigidbody2D>();
        var hideState = interactor.GetComponent<PlayerHideState>();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        movement?.SetMovementLocked(true);
        hideState?.SetHidden(true);

        occupantIndicator = interactor.GetComponentInChildren<PlayerVisibilityIndicator>();
        if (occupantIndicator != null)
        {
            Transform anchor = indicatorAnchor != null ? indicatorAnchor : transform;
            occupantIndicator.SetFollowOverride(anchor, indicatorOffset);
        }

        SetCameraFocus(true);

        ResolveEffects();

        if (effects != null && maxHideTime > 0f)
            effects.Begin();

        PlaySound(enterSoundName);

        onPlayerEnter?.Invoke();
    }

    private void Exit(bool forced)
    {
        isOccupied = false;

        if (occupantIndicator != null)
            occupantIndicator.ClearFollowOverride();
        occupantIndicator = null;

        if (currentOccupant != null)
        {
            var movement = currentOccupant.GetComponent<PlayerMovementBase>();
            var hideState = currentOccupant.GetComponent<PlayerHideState>();

            movement?.SetMovementLocked(false);
            hideState?.SetHidden(false);
        }

        SetCameraFocus(false);

        if (effects != null)
            effects.End();

        cooldownDuration = forced ? reentryDelay : exitCooldown;
        nextEnterTime = Time.time + cooldownDuration;

        hideTimer = 0f;

        PlaySound(exitSoundName);

        onPlayerExit?.Invoke();
        currentOccupant = null;
    }

    private void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName) || SoundManager.Instance == null) return;

        SoundManager.Instance.PlaySFX(soundName, soundVolume, true);
    }

    private void TriggerGameOver()
    {
        dead = true;
        onGameOver?.Invoke();

        if (effects != null)
            effects.PlayGameOver(ShowGameOver);
        else
            ShowGameOver();
    }

    private void ShowGameOver()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameOver();
    }

    private void SetCameraFocus(bool focus)
    {
        if (cameraPoint == null) return;

        if (cameraFollow == null)
            cameraFollow = FindFirstObjectByType<CameraFollow>();

        if (cameraFollow == null) return;

        if (focus)
            cameraFollow.SetOverrideTarget(cameraPoint);
        else
            cameraFollow.ClearOverrideTarget(cameraPoint);
    }
}