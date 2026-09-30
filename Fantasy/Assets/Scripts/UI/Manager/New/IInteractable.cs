using UnityEngine;

public interface IInteractable
{
    void Interact(GameObject interactor);
    void EndInteraction();
    bool IsBeingInteracted { get; }
    bool HoldsPlayer { get; }
    bool CancelableByInteractButton { get; }
}