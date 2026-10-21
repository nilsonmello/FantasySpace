using UnityEngine;

[RequireComponent(typeof(PlayerMovementBase))]
public class PlayerAnimationController : MonoBehaviour
{
    public enum FacingMode
    {
        FourDirections,
        EightDirections,
        Free
    }

    [SerializeField] private PlayerMovementBase movement;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private FacingMode facingMode = FacingMode.FourDirections;
    [SerializeField] private bool useFlipX = false;
    [SerializeField] private Vector2 initialFacing = Vector2.down;

    [SerializeField] private float movingThreshold = 0.05f;

    [SerializeField] private float walkAnimSpeed = 1f;
    [SerializeField] private float runAnimSpeed = 1.4f;
    [SerializeField] private float crouchAnimSpeed = 0.7f;

    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string moveYParam = "MoveY";
    [SerializeField] private string isMovingParam = "IsMoving";
    [SerializeField] private string isRunningParam = "IsRunning";
    [SerializeField] private string isCrouchingParam = "IsCrouching";

    private int moveXId, moveYId, isMovingId, isRunningId, isCrouchingId;

    private Vector2 facing;

    private void Reset()
    {
        movement = GetComponent<PlayerMovementBase>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        if (movement == null) movement = GetComponent<PlayerMovementBase>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        moveXId = Animator.StringToHash(moveXParam);
        moveYId = Animator.StringToHash(moveYParam);
        isMovingId = Animator.StringToHash(isMovingParam);
        isRunningId = Animator.StringToHash(isRunningParam);
        isCrouchingId = Animator.StringToHash(isCrouchingParam);

        facing = initialFacing.sqrMagnitude > 0.001f ? initialFacing.normalized : Vector2.down;
    }

    private void Update()
    {
        if (movement == null || animator == null) return;

        Vector2 velocity = movement.CurrentVelocity;
        bool moving = !movement.MovementLocked
                      && velocity.sqrMagnitude >= movingThreshold * movingThreshold;

        var state = movement.CurrentMovementState;
        bool running = moving && state == PlayerMovementBase.MovementState.Run;
        bool crouching = state == PlayerMovementBase.MovementState.Crouch;

        if (moving)
            facing = Quantize(velocity);

        float animSpeed = 1f;
        if (moving)
        {
            if (running) animSpeed = runAnimSpeed;
            else if (crouching) animSpeed = crouchAnimSpeed;
            else animSpeed = walkAnimSpeed;
        }
        animator.speed = animSpeed;

        float x = facing.x;
        float y = facing.y;

        if (useFlipX)
        {
            if (spriteRenderer != null && Mathf.Abs(facing.x) > 0.01f)
                spriteRenderer.flipX = facing.x < 0f;

            x = Mathf.Abs(x);
        }

        animator.SetFloat(moveXId, x);
        animator.SetFloat(moveYId, y);
        animator.SetBool(isMovingId, moving);
        animator.SetBool(isRunningId, running);
        animator.SetBool(isCrouchingId, crouching);
    }

    private Vector2 Quantize(Vector2 v)
    {
        switch (facingMode)
        {
            case FacingMode.FourDirections:
                if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
                    return new Vector2(Mathf.Sign(v.x), 0f);
                return new Vector2(0f, Mathf.Sign(v.y));

            case FacingMode.EightDirections:
                float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                float snapped = Mathf.Round(angle / 45f) * 45f * Mathf.Deg2Rad;
                return new Vector2(Mathf.Round(Mathf.Cos(snapped)), Mathf.Round(Mathf.Sin(snapped)));

            default:
                return v.normalized;
        }
    }

    public void SetFacing(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;
        facing = Quantize(direction);
    }
}