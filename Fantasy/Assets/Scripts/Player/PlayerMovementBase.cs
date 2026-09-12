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

    protected InputSystem_Actions actions;
    protected InputAction crouchAction;
    protected InputAction runAction;
    protected InputAction moveAction;
    protected float currentSpeed;
    protected Rigidbody2D rb;

    public MovementState CurrentMovementState { get; private set; } = MovementState.Walk;

    public Vector2 CurrentVelocity => rb != null ? rb.linearVelocity : Vector2.zero;

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
    }

    protected virtual void Update()
    {
        GetMoveInput();
    }

    protected virtual void FixedUpdate()
    {
        Move();
    }

    protected void GetMoveInput()
    {
        if (runAction.IsPressed())
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

    protected void Move()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        rb.linearVelocity = moveInput * currentSpeed * 10f * Time.fixedDeltaTime;
    }

    protected void OnDisable()
    {
        if (actions != null)
        {
            actions.Disable();
        }
    }
}