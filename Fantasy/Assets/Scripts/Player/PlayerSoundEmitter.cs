using System;
using UnityEngine;

public enum NoiseKind
{
    Crouch,
    Walk,
    Run
}

[RequireComponent(typeof(PlayerMovementBase))]
public class PlayerSoundEmitter : MonoBehaviour
{
    [SerializeField] private SoundEventChannelSO channel;

    [SerializeField] private PlayerMovementBase playerMovement;

    [SerializeField] private float movementThreshold = 0.05f;

    [Header("Crouch")]
    [SerializeField] private float crouchInterval = 0.7f;
    [SerializeField] private float crouchRadius = 2f;
    [SerializeField] private float crouchPriority = 0.5f;
    
    [SerializeField] private bool crouchEmitsSound = false;

    [Header("Walk")]
    [SerializeField] private float walkInterval = 0.5f;
    [SerializeField] private float walkRadius = 4f;
    [SerializeField] private float walkPriority = 1f;

    [Header("Run")]
    [SerializeField] private float runInterval = 0.3f;
    [SerializeField] private float runRadius = 7f;
    [SerializeField] private float runPriority = 2f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmo = true;

    public event Action<NoiseKind> OnNoisePulse;

    private float nextEmitTime;
    private float lastEmitFlashUntil = -1f;
    private const float EmitFlashDuration = 0.15f;

    private void Reset()
    {
        playerMovement = GetComponent<PlayerMovementBase>();
    }

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovementBase>();
    }

    private void Update()
    {
        if (playerMovement == null) return;

        bool isMoving = playerMovement.CurrentVelocity.sqrMagnitude >= movementThreshold * movementThreshold;
        if (!isMoving) return;

        if (Time.time < nextEmitTime) return;

        var state = playerMovement.CurrentMovementState;
        bool isCrouching = state == PlayerMovementBase.MovementState.Crouch;
        bool isRunning = state == PlayerMovementBase.MovementState.Run;

        NoiseKind kind;
        float interval, radius, priority;

        if (isCrouching)
        {
            kind = NoiseKind.Crouch;
            interval = crouchInterval;
            radius = crouchRadius;
            priority = crouchPriority;
        }
        else if (isRunning)
        {
            kind = NoiseKind.Run;
            interval = runInterval;
            radius = runRadius;
            priority = runPriority;
        }
        else
        {
            kind = NoiseKind.Walk;
            interval = walkInterval;
            radius = walkRadius;
            priority = walkPriority;
        }

        bool raiseOnChannel = !isCrouching || crouchEmitsSound;

        if (raiseOnChannel && channel != null)
        {
            channel.RaiseSound(transform.position, radius, priority, gameObject.GetInstanceID());
            lastEmitFlashUntil = Time.time + EmitFlashDuration;
        }

        OnNoisePulse?.Invoke(kind);

        nextEmitTime = Time.time + interval;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo || playerMovement == null) return;

        var state = playerMovement.CurrentMovementState;
        float radius = walkRadius;

        if (state == PlayerMovementBase.MovementState.Run) radius = runRadius;
        else if (state == PlayerMovementBase.MovementState.Crouch) radius = crouchRadius;

        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && Time.time < lastEmitFlashUntil)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}