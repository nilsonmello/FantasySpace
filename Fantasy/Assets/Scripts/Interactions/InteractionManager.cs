using UnityEngine;

public abstract class InteractionManager : MonoBehaviour
{
    [Header("Base")]
    [SerializeField] private float InteractionRange = 1.5f;
    public float interactionRange => InteractionRange;

    [Header("Usage Limits")]
    [SerializeField] protected int maxUses = -1;
    [SerializeField] protected float cooldownDuration = 0f;

    private int usesRemaining;
    private float nextAvailableTime;
    private bool depletedNotified;

    protected virtual void Awake()
    {
        usesRemaining = maxUses;
    }

    public void interact(GameObject interactor)
    {
        if (!Caninteract()) return;

        OnInteract(interactor);

        if (maxUses >= 0)
        {
            usesRemaining--;

            if (usesRemaining <= 0 && !depletedNotified)
            {
                depletedNotified = true;
                OnDepleted();
            }
        }

        if (cooldownDuration > 0f)
            nextAvailableTime = Time.time + cooldownDuration;
    }

    protected abstract void OnInteract(GameObject interactor);

    protected virtual void OnDepleted() { }

    public virtual bool Caninteract()
    {
        if (maxUses >= 0 && usesRemaining <= 0) return false;
        if (cooldownDuration > 0f && Time.time < nextAvailableTime) return false;

        return true;
    }

    public int UsesRemaining => usesRemaining;
    public bool IsOnCooldown => cooldownDuration > 0f && Time.time < nextAvailableTime;
    public float CooldownRemaining => Mathf.Max(0f, nextAvailableTime - Time.time);
}