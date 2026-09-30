using UnityEngine;

public class DoorAnimatorController : MonoBehaviour
{
    public SpriteRenderer lever;
    public Sprite leverDownSprite;
    public Animator animator;

    public void Open()
    {
        animator.SetBool("Open", true);
        lever.sprite = leverDownSprite; 
    }

    public void Close()
    {
        animator.SetBool("Open", false);
    }
}