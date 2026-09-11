using UnityEngine;
using UnityEngine.Events;

public class SimpleInteractable : InteractionManager
{
    [Header("Simple Interaction")]
    [SerializeField] private UnityEvent m_OnClick;

    [Header("Reuse Settings")]
    [SerializeField] private bool reusable = true;

    [SerializeField] private float reuseCooldown = 0f;

    protected override void Awake()
    {
        maxUses = reusable ? -1 : 1; cooldownDuration = reusable ? reuseCooldown : 0f;

        base.Awake();
    }

    protected override void OnInteract(GameObject interactor)
    {
        m_OnClick.Invoke();
    }
}