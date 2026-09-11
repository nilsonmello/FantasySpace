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

        if (hidingSpot != null)
            interactor.transform.position = hidingSpot.position;

        onPlayerEnter?.Invoke();
    }

    private void Exit()
    {
        isOccupied = false;

        if (currentOccupant != null)
            currentOccupant.transform.position = occupantPreviousPosition;

        onPlayerExit?.Invoke();
        currentOccupant = null;
    }
}