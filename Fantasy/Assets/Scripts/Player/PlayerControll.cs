using UnityEngine;
using UnityEngine.InputSystem;

public abstract class PlayerMovementBase : MonoBehaviour
{
    // Propriedades abstratas: toda classe filha DEVE definir esses valores
    public float MovSpeed;
    public float RunSpeed;
    public float CrouchSpeed;
    // Variáveis protegidas: visíveis apenas para esta classe e para as classes filhas
    protected InputSystem_Actions actions;
    protected InputAction crouchAction;
    protected InputAction runAction;
    protected InputAction moveAction;
    protected float currentSpeed;
    protected Rigidbody2D rb;

    protected virtual void Awake()
    {
        // Inicializa e ativa o Input System
        actions = new InputSystem_Actions();

        crouchAction = actions.Player.Crouch;
        crouchAction.Enable();

        runAction = actions.Player.Sprint;
        runAction.Enable();

        moveAction = actions.Player.Move;
        moveAction.Enable();
    }

    protected virtual void Start()
    {
        // Certifique-se de que a classe pai 'Player' possui a variável 'rb'
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Update()
    {
        GetMoveInput();
    }

    protected virtual void FixedUpdate()
    {
        Move();
    }

    // Gerencia a troca de velocidades com base no input
    protected void GetMoveInput()
    {
        if (runAction.IsPressed())
        {
            currentSpeed = RunSpeed;
        }
        else if (crouchAction.IsPressed())
        {
            currentSpeed = CrouchSpeed;
        }
        else
        {
            currentSpeed = MovSpeed;
        }
    }

    // Aplica a velocidade ao Rigidbody2D
    protected void Move()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        rb.linearVelocity = moveInput * currentSpeed * 10f * Time.fixedDeltaTime;
    }

    protected void OnDisable()
    {
        // Desativa os inputs para evitar vazamento de memória quando o objeto sumir
        if (actions != null)
        {
            actions.Disable();
        }
    }
}
