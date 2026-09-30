using UnityEngine;
using UnityEngine.Events;

public class CutsceneInteractable : MonoBehaviour, IInteractable
{
    public CutsceneData data;
    public UnityEvent onCutsceneFinished;

    [Header("One-Handed State")]
    public bool changeOneHandedState = false;
    public bool oneHandedValueOnFinish = true;

    //private PlayerAnimator playerAnimator;
    //private PlayerMovement playerMovement;
    private Transform playerTransform;

    private bool isBeingInteracted;

    public bool IsBeingInteracted => isBeingInteracted;
    public bool isInteractable = true;
    public bool HoldsPlayer => true;
    public bool CancelableByInteractButton => false;

    public void Interact(GameObject interactor)
    {
        if (!isInteractable) return;

        isInteractable = false;

        //playerAnimator = interactor.GetComponent<PlayerAnimator>();
        //playerAnimator.SetPullingLever(true);

        //playerMovement = interactor.GetComponent<PlayerMovement>();
        //playerMovement.enabled = false;

        playerTransform = interactor.transform;
        
        isBeingInteracted = true;
        CutsceneVideoManager.Instance.Play(data, OnCutsceneFinished);
    }

    private void OnCutsceneFinished()
    {
        isBeingInteracted = false;

        if (changeOneHandedState)
            //playerMovement.animator.SetOneHanded(oneHandedValueOnFinish);

        //float offsetLeft = 0.65f;
        //playerTransform.position = new Vector3(playerTransform.position.x - offsetLeft, playerTransform.position.y, playerTransform.position.z);
        
        onCutsceneFinished?.Invoke();

        //PlayerInteractable pi = playerMovement.GetComponent<PlayerInteractable>();
        //pi.NotifyHeldInteractionFinished();
    }

    public void EndInteraction() { }
}