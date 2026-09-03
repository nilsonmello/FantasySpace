using UnityEngine;

public abstract class InteractionManager : MonoBehaviour
{
    [SerializeField] private float InteractionRange = 1.5f;
    public float interactionRange => InteractionRange;
    public abstract void interact();
    public virtual bool Caninteract() => true;
}