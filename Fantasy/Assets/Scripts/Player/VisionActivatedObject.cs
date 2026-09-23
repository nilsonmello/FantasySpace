using UnityEngine;
using System.Collections.Generic;

public class VisionActivatedObject : MonoBehaviour, IVisionTarget
{
    [Header("Collider")]
    [SerializeField] private Collider2D detectionCollider;

    [SerializeField] private Behaviour[] behavioursToToggle;

    [Header("Fade")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField, Range(0f, 1f)] private float maxPartialAlpha = 0.5f;
    [SerializeField] private float fadeSpeed = 2.5f;

    private float targetAlpha = 0f;
    private float currentAlpha = 0f;
    private bool behavioursActive = false;

    private void Awake()
    {
        AutoFill();
    }

    private void Start()
    {
        currentAlpha = 0f;
        targetAlpha = 0f;
        ApplyAlpha(0f);
        SetBehavioursActive(false);
    }

    private void Update()
    {
        if (Mathf.Approximately(currentAlpha, targetAlpha)) return;

        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
        ApplyAlpha(currentAlpha);
    }

    public void UpdateVision(bool inCone, float proximity01)
    {
        targetAlpha = inCone ? 1f : Mathf.Clamp01(proximity01) * maxPartialAlpha;

        if (inCone != behavioursActive)
            SetBehavioursActive(inCone);
    }

    private void ApplyAlpha(float alpha)
    {
        if (spriteRenderer == null) return;

            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
    }

    private void SetBehavioursActive(bool state)
    {
        behavioursActive = state;
        foreach (var b in behavioursToToggle)
        {
            if (b == null) continue;
            if (b == detectionCollider) continue;
            b.enabled = state;
        }
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
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
}