using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
public class PlayerControll : Player
{
    public override float movSpeed => 5f;
    public override float runSpeed => 8f;
    public override float crouchSpeed => 2.5f;
    private InputSystem_Actions actions;
    private InputAction crouch;
    private InputAction run;
    private InputAction move;
    private float currentSpeed;


    void Start()
    {
        Debug.Log("aaa");
        actions = new InputSystem_Actions();
        crouch = actions.Player.Crouch;
        crouch.Enable();
        run = actions.Player.Sprint;
        run.Enable();
        move = actions.Player.Move;
        move.Enable();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        GetMoveInput();
    }
    void FixedUpdate()
    {
        Move();
    }
    void GetMoveInput()
    {
        if (run.IsPressed())//Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = runSpeed;
        }
        else if (crouch.IsPressed())
        {
            currentSpeed = crouchSpeed;
        }
        else
        {
            currentSpeed = movSpeed;
        }
    }

    void Move()
    {
        // 1. Descobre a velocidade atual com base nas teclas pressionadas
       
        Vector2 moveInput = move.ReadValue<Vector2>();    

        // 3. Move o Rigidbody2D
        rb.linearVelocity = moveInput * currentSpeed * 10 * Time.fixedDeltaTime;
    }
    
}
