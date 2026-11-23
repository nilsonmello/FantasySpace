using UnityEngine;
using UnityEngine.InputSystem;

public abstract class PlayerMovementBase : MonoBehaviour
{
    public enum MovementState
    {
        Walk,
        Run,
        Crouch
    }

    public float MovSpeed;
    public float RunSpeed;
    public float CrouchSpeed;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 5f;
    [SerializeField] private float staminaRegenRate = 1f;
    [SerializeField] private float staminaRegenDelay = 1f;
    [SerializeField, Range(0f, 1f)] private float runResumeThreshold = 1f;

    protected InputSystem_Actions actions;
    protected InputAction crouchAction;
    protected InputAction runAction;
    protected InputAction moveAction;
    protected float currentSpeed;
    protected Rigidbody2D rb;

    private float regenTimer;

    public MovementState CurrentMovementState { get; private set; } = MovementState.Walk;

    public Vector2 CurrentVelocity => rb != null ? rb.linearVelocity : Vector2.zero;

    public bool MovementLocked { get; private set; }

    public float Stamina { get; private set; }
    public float StaminaNormalized => maxStamina > 0f ? Stamina / maxStamina : 0f;
    public bool IsExhausted { get; private set; }

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        actions = new InputSystem_Actions();

        crouchAction = actions.Player.Crouch;
        crouchAction.Enable();

        runAction = actions.Player.Sprint;
        runAction.Enable();

        moveAction = actions.Player.Move;
        moveAction.Enable();

        Stamina = maxStamina;
    }

    protected virtual void Update()
    {
        if (!MovementLocked)
            GetMoveInput();

        UpdateStamina();
    }

    protected virtual void FixedUpdate()
    {
        if (MovementLocked) return;

        Move();
    }

    protected void GetMoveInput()
    {
        if (runAction.IsPressed() && !IsExhausted)
        {
            currentSpeed = RunSpeed;
            CurrentMovementState = MovementState.Run;
        }
        else if (crouchAction.IsPressed())
        {
            currentSpeed = CrouchSpeed;
            CurrentMovementState = MovementState.Crouch;
        }
        else
        {
            currentSpeed = MovSpeed;
            CurrentMovementState = MovementState.Walk;
        }
    }

    private void UpdateStamina()
    {
        bool isRunning = !MovementLocked
                      && CurrentMovementState == MovementState.Run
                      && moveAction.ReadValue<Vector2>().sqrMagnitude > 0.01f;

        if (isRunning)
        {
            Stamina = Mathf.Max(0f, Stamina - Time.deltaTime);
            regenTimer = staminaRegenDelay;

            if (Stamina <= 0f) IsExhausted = true;
            return;
        }

        if (regenTimer > 0f)
            regenTimer -= Time.deltaTime;
        else
            Stamina = Mathf.Min(maxStamina, Stamina + staminaRegenRate * Time.deltaTime);

        if (IsExhausted && Stamina >= maxStamina * runResumeThreshold)
            IsExhausted = false;
    }

    protected void Move()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        rb.linearVelocity = moveInput * currentSpeed * 10f * Time.fixedDeltaTime;
    }

    public void SetMovementLocked(bool locked)
    {
        MovementLocked = locked;

        if (locked && rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    protected void OnDisable()
    {
        if (actions != null)
        {
            actions.Disable();
        }
    }
}