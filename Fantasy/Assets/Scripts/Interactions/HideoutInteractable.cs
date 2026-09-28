using UnityEngine;
using UnityEngine.Events;

public class HideoutInteractable : InteractionManager
{
    [Header("Hideout")]
    [SerializeField] private UnityEvent onPlayerEnter;
    [SerializeField] private UnityEvent onPlayerExit;

    [Header("Camera")]
    [SerializeField] private Transform cameraPoint;

    private bool isOccupied;
    private GameObject currentOccupant;
    private CameraFollow cameraFollow;

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

        var movement = interactor.GetComponent<PlayerMovementBase>();
        var rb = interactor.GetComponent<Rigidbody2D>();
        var hideState = interactor.GetComponent<PlayerHideState>();

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        movement?.SetMovementLocked(true);
        hideState?.SetHidden(true);

        SetCameraFocus(true);

        onPlayerEnter?.Invoke();
    }

    private void Exit()
    {
        isOccupied = false;

        if (currentOccupant != null)
        {
            var movement = currentOccupant.GetComponent<PlayerMovementBase>();
            var hideState = currentOccupant.GetComponent<PlayerHideState>();

            movement?.SetMovementLocked(false);
            hideState?.SetHidden(false);
        }

        SetCameraFocus(false);

        onPlayerExit?.Invoke();
        currentOccupant = null;
    }

    private void SetCameraFocus(bool focus)
    {
        if (cameraPoint == null) return;

        if (cameraFollow == null)
            cameraFollow = FindFirstObjectByType<CameraFollow>();

        if (cameraFollow == null) return;

        if (focus)
            cameraFollow.SetOverrideTarget(cameraPoint);
        else
            cameraFollow.ClearOverrideTarget(cameraPoint);
    }
}