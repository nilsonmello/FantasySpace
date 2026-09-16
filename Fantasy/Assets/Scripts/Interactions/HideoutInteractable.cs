using UnityEngine;
using UnityEngine.Events;

public class HideoutInteractable : InteractionManager
{
    [Header("Hideout")]
    [SerializeField] private Transform hidingSpot;
    [SerializeField] private UnityEvent onPlayerEnter;
    [SerializeField] private UnityEvent onPlayerExit;
    [SerializeField] private BoxCollider2D collider;


    private bool isOccupied;
    private GameObject currentOccupant;
    private Vector3 occupantPreviousPosition;

    public bool IsOccupied => isOccupied;

    void Awake()
    {
        collider = GetComponent<BoxCollider2D>();
    }

    protected override void OnInteract(GameObject interactor)
    {
        if (!isOccupied)
            Enter(interactor);
        else if (currentOccupant == interactor)
            Exit();
    }

    private void Enter(GameObject interactor)
    {
        collider.isTrigger = true;
        isOccupied = true;
        currentOccupant = interactor;
        occupantPreviousPosition = interactor.transform.position;

        var movement = interactor.GetComponent<PlayerMovementBase>();
        var rb = interactor.GetComponent<Rigidbody2D>();
        var hideState = interactor.GetComponent<PlayerHideState>();

        movement?.SetMovementLocked(true);
        hideState?.SetHidden(true);

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
        collider.isTrigger = false;
        isOccupied = false;

        if (currentOccupant != null)
        {
            var movement = currentOccupant.GetComponent<PlayerMovementBase>();
            var rb = currentOccupant.GetComponent<Rigidbody2D>();
            var hideState = currentOccupant.GetComponent<PlayerHideState>();

            if (rb != null)
                rb.position = occupantPreviousPosition;
            else
                currentOccupant.transform.position = occupantPreviousPosition;

            movement?.SetMovementLocked(false);
            hideState?.SetHidden(false);
        }

        onPlayerExit?.Invoke();
        currentOccupant = null;
    }
}