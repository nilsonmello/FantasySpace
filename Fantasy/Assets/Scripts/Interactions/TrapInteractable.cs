using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TrapInteractable : InteractionManager
{
    [Header("Trap")]
    [SerializeField] private UnityEvent onTriggered;
    [SerializeField] private bool disableColliderOnDepleted = true;
    [SerializeField] private Collider2D trapCollider;

    [Header("Duration")]
    [SerializeField] private float activeDuration = 5f;
    [SerializeField] private bool blockInteractionWhileActive = true;
    [SerializeField] private UnityEvent onDeactivated;

    [Header("Signals")]
    [SerializeField] private TrapSignal[] signals;

    [Header("Active Objects")]
    [SerializeField] private GameObject[] activeObjects;
    [SerializeField] private bool startDisabled = true;

    private Coroutine timer;

    public bool IsActive { get; private set; }

    public override bool PromptVisible => !(blockInteractionWhileActive && IsActive);
    public override string PromptDescription => "alarme";

    protected override void Awake()
    {
        base.Awake();

        if (signals == null || signals.Length == 0)
            signals = GetComponentsInChildren<TrapSignal>(true);

        if (startDisabled)
            SetActiveObjects(false);
    }

    public override bool Caninteract()
    {
        if (blockInteractionWhileActive && IsActive) return false;

        return base.Caninteract();
    }

    protected override void OnInteract(GameObject interactor)
    {
        onTriggered?.Invoke();
        Activate();
    }

    protected override void OnDepleted()
    {
        if (disableColliderOnDepleted && trapCollider != null)
            trapCollider.enabled = false;
    }

    private void Activate()
    {
        IsActive = true;

        SetActiveObjects(true);

        for (int i = 0; i < signals.Length; i++)
        {
            if (signals[i] != null)
                signals[i].Begin();
        }

        if (timer != null)
        {
            StopCoroutine(timer);
            timer = null;
        }

        if (activeDuration > 0f)
            timer = StartCoroutine(DeactivateAfter(activeDuration));
    }

    public void Deactivate()
    {
        if (!IsActive) return;

        StopSignals();
        onDeactivated?.Invoke();
    }

    private IEnumerator DeactivateAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        timer = null;
        Deactivate();
    }

    private void StopSignals()
    {
        IsActive = false;

        if (timer != null)
        {
            StopCoroutine(timer);
            timer = null;
        }

        for (int i = 0; i < signals.Length; i++)
        {
            if (signals[i] != null)
                signals[i].End();
        }

        SetActiveObjects(false);
    }

    private void SetActiveObjects(bool value)
    {
        if (activeObjects == null) return;

        for (int i = 0; i < activeObjects.Length; i++)
        {
            if (activeObjects[i] != null)
                activeObjects[i].SetActive(value);
        }
    }

    private void OnDisable()
    {
        if (IsActive && !gameObject.activeInHierarchy)
            StopSignals();
    }
}