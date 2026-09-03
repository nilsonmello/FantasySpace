using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteract : MonoBehaviour
{
    [SerializeField] private float detectionRadius = 3f;
    [SerializeField] private LayerMask interactableLayer;

    private InputAction  interactAction;
    private InputSystem_Actions actions;

    public InteractionManager CurrentTarget { get; private set; }

    private void OnEnable()
    {
        actions = new InputSystem_Actions();
        interactAction = actions.Player.Interact;
        interactAction.Enable();
    }

    private void OnDisable()
    {
        interactAction.Disable();
    }


    private void Update()
    {
        CurrentTarget = FindClosestInteractable();

        if (CurrentTarget != null && interactAction.WasPressedThisFrame())
        {
            CurrentTarget.interact();
        }

    }

    private InteractionManager FindClosestInteractable()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, interactableLayer);

        InteractionManager closest = null;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent(out InteractionManager interactable)) continue;

            if (!interactable.Caninteract()) continue;

            float distance = Vector2.Distance(transform.position, hit.transform.position);

            if (distance > interactable.interactionRange) continue;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = interactable;
            }
        }

        return closest;
    }
}