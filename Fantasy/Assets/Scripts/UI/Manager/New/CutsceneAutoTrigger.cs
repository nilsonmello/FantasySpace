using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.Collections;

public class CutsceneAutoTrigger : MonoBehaviour
{
    public CutsceneData data;

    public UnityEvent onCutsceneFinished;
    public bool triggerOnce = true;

    [Header("On Finished")]
    public bool loadNextSceneOnFinish = false;

    [Header("One-Handed State")]
    public bool changeOneHandedState = false;
    public bool oneHandedValueOnFinish = true;

    private bool triggered;

    //PlayerMovement playerMovement;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered && triggerOnce) return;
        if (!other.CompareTag("Player")) return;

        //playerMovement = other.GetComponent<PlayerMovement>();
        //if (playerMovement == null) return;

        triggered = true;
        //playerMovement.enabled = false;

        int? nextSceneIndex = loadNextSceneOnFinish
            ? SceneManager.GetActiveScene().buildIndex + 1
            : (int?)null;


        CutsceneVideoManager.Instance.Play(
            data,
            OnCutsceneFinished,
            nextSceneIndex
        );
    }

    public void OnCutsceneFinished()
    {
        if (changeOneHandedState)
            //playerMovement.animator.SetOneHanded(oneHandedValueOnFinish);

        onCutsceneFinished?.Invoke();
    }

    public void EndInteraction() { }
}