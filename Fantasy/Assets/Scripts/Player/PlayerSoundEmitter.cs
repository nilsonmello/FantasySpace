using UnityEngine;

[RequireComponent(typeof(PlayerMovementBase))]
public class PlayerSoundEmitter : MonoBehaviour
{
    [SerializeField] private SoundEventChannelSO channel;

    [SerializeField] private PlayerMovementBase playerMovement;

    [SerializeField] private float movementThreshold = 0.05f;

    [SerializeField] private float walkInterval = 0.5f;
    [SerializeField] private float walkRadius = 4f;
    [SerializeField] private float walkPriority = 1f;

    [SerializeField] private float runInterval = 0.3f;
    [SerializeField] private float runRadius = 7f;
    [SerializeField] private float runPriority = 2f;

    [SerializeField] private bool drawGizmo = true;

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
        if (playerMovement == null || channel == null) return;

        if (playerMovement.CurrentMovementState == PlayerMovementBase.MovementState.Crouch)
            return;

        bool isMoving = playerMovement.CurrentVelocity.sqrMagnitude >= movementThreshold * movementThreshold;
        if (!isMoving) return;

        if (Time.time < nextEmitTime) return;

        bool isRunning = playerMovement.CurrentMovementState == PlayerMovementBase.MovementState.Run;

        float interval = isRunning ? runInterval : walkInterval;
        float radius = isRunning ? runRadius : walkRadius;
        float priority = isRunning ? runPriority : walkPriority;

        channel.RaiseSound(transform.position, radius, priority, gameObject.GetInstanceID());

        nextEmitTime = Time.time + interval;
        lastEmitFlashUntil = Time.time + EmitFlashDuration;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo || playerMovement == null) return;

        bool isRunning = playerMovement.CurrentMovementState == PlayerMovementBase.MovementState.Run;
        float radius = isRunning ? runRadius : walkRadius;

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