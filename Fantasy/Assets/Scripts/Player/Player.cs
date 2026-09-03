using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class Player : MonoBehaviour
{
    public abstract float movSpeed { get; }
    public abstract float runSpeed { get; }
    public abstract float crouchSpeed { get; }


    protected float speedX, speedY;
    protected Rigidbody2D rb;

}
