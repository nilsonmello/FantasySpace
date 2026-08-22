using UnityEngine;
using System.Collections.Generic;

public class VisionActivatedObject : MonoBehaviour, IVisionTarget
{
    [Header("Collider usado pela detecção")]
    [SerializeField] private Collider2D detectionCollider;

    [Header("Componentes a desativar/reativar")]
    [SerializeField] private Behaviour[] behavioursToToggle;
    [SerializeField] private Renderer[] renderersToToggle;

    private void Awake()
    {
        if (detectionCollider == null)
            detectionCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        SetActiveState(false);
    }

    public void OnEnterVision() => SetActiveState(true);
    public void OnExitVision() => SetActiveState(false);

    private void SetActiveState(bool state)
    {
        foreach (var b in behavioursToToggle)
        {
            if (b == null) continue;
            if (b == detectionCollider) continue;
            b.enabled = state;
        }

        foreach (var r in renderersToToggle)
            if (r != null) r.enabled = state;
    }

    private void AutoFill()
    {
        if (detectionCollider == null)
            detectionCollider = GetComponent<Collider2D>();

        var allBehaviours = GetComponentsInChildren<Behaviour>(true);
        var filtered = new List<Behaviour>();

        foreach (var b in allBehaviours)
        {
            if (b == this) continue;
            if (b == detectionCollider) continue;
            filtered.Add(b);
        }

        behavioursToToggle = filtered.ToArray();
        renderersToToggle = GetComponentsInChildren<Renderer>(true);
    }
}