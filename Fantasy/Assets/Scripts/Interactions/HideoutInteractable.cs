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

    [Header("Effects")]
    [SerializeField] private HideoutEffects effects;

    [Header("Camera")]
    [SerializeField] private Transform cameraPoint;

    private bool isOccupied;
    private bool dead;
    private GameObject currentOccupant;
    private CameraFollow cameraFollow;

    private float hideTimer;
    private float nextEnterTime;

    public bool IsOccupied => isOccupied;
    public float HideProgress => maxHideTime > 0f ? Mathf.Clamp01(hideTimer / maxHideTime) : 0f;

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

        SetCameraFocus(true);

        if (effects != null && maxHideTime > 0f)
            effects.Begin();

        onPlayerEnter?.Invoke();
    }

    private void Exit(bool forced)
    {
        isOccupied = false;

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

        if (forced)
            nextEnterTime = Time.time + reentryDelay;

        hideTimer = 0f;

        onPlayerExit?.Invoke();
        currentOccupant = null;
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