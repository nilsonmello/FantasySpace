using UnityEngine;
using UnityEngine.Events;

public class TrapInteractable : InteractionManager
{
    [Header("Trap")]
    [SerializeField] private UnityEvent onTriggered;
    [SerializeField] private bool disableColliderOnDepleted = true;
    [SerializeField] private Collider2D trapCollider;

    protected override void OnInteract(GameObject interactor)
    {
        onTriggered?.Invoke();
    }

    protected override void OnDepleted()
    {
        if (disableColliderOnDepleted && trapCollider != null)
            trapCollider.enabled = false;
    }
}