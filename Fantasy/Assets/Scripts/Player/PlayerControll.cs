using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerControll : Player
{
    public override float movSpeed => 5f;
    public override float runSpeed => 8f;
    public override float crouchSpeed => 2.5f;

    public Rigidbody2D rsb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        Move();
    }

    void Move()
    {
        // 1. Descobre a velocidade atual com base nas teclas pressionadas
        float currentSpeed = movSpeed;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = runSpeed;
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            currentSpeed = crouchSpeed;
        }

        // 2. Aplica a velocidade nos eixos de movimento
        speedX = Input.GetAxisRaw("Horizontal") * currentSpeed;
    

        // 3. Move o Rigidbody2D
        rb.linearVelocity = new Vector2(speedX, speedY);
    }
}
