using UnityEngine;
using UnityEngine.Events;

public class HideoutInteractable : InteractionManager
{
    [Header("Hideout")]
    [SerializeField] private Transform hidingSpot;
    [SerializeField] private UnityEvent onPlayerEnter;
    [SerializeField] private UnityEvent onPlayerExit;

    private bool isOccupied;
    private GameObject currentOccupant;
    private Vector3 occupantPreviousPosition;

    public bool IsOccupied => isOccupied;

    protected override void OnInteract(GameObject interactor)
    {
        if (!isOccupied)
            Enter(interactor);
        else if (currentOccupant == interactor)
            Exit();
    }

    private void Enter(GameObject interactor)
    {
        isOccupied = true;
        currentOccupant = interactor;
        occupantPreviousPosition = interactor.transform.position;

        var movement = interactor.GetComponent<PlayerMovementBase>();
        var rb = interactor.GetComponent<Rigidbody2D>();

        movement?.SetMovementLocked(true);

        if (hidingSpot != null)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.position = hidingSpot.position;
            }
            else
            {
                interactor.transform.position = hidingSpot.position;
            }
        }

        onPlayerEnter?.Invoke();
    }

    private void Exit()
    {
        isOccupied = false;

        if (currentOccupant != null)
        {
            var movement = currentOccupant.GetComponent<PlayerMovementBase>();
            var rb = currentOccupant.GetComponent<Rigidbody2D>();

            if (rb != null)
                rb.position = occupantPreviousPosition;
            else
                currentOccupant.transform.position = occupantPreviousPosition;

            movement?.SetMovementLocked(false);
        }

        onPlayerExit?.Invoke();
        currentOccupant = null;
    }
}