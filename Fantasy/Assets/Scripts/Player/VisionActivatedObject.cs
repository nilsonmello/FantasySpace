using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

public class VisionActivatedObject : MonoBehaviour, IVisionTarget
{
    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");

    [Header("Collider")]
    [SerializeField] private Collider2D detectionCollider;

    [SerializeField] private Behaviour[] behavioursToToggle;

    [Header("Vision Range (0 = usa o padrão do player)")]
    [Tooltip("Distância máxima em que o objeto é revelado dentro do cone")]
    [SerializeField, Min(0f)] private float visionRange = 0f;
    [Tooltip("Raio em que o objeto começa a brilhar parcialmente, fora do cone")]
    [SerializeField, Min(0f)] private float proximityRange = 0f;

    [Header("Highlight")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [FormerlySerializedAs("maxPartialAlpha")]
    [SerializeField, Range(0f, 1f)] private float maxPartialHighlight = 0.5f;
    [SerializeField] private float fadeSpeed = 2.5f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmo = true;

    private MaterialPropertyBlock propertyBlock;
    private float targetHighlight = 0f;
    private float currentHighlight = 0f;
    private bool behavioursActive = false;

    public float VisionRange => visionRange;
    public float ProximityRange => proximityRange;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        AutoFill();
    }

    private void Start()
    {
        currentHighlight = 0f;
        targetHighlight = 0f;
        ApplyHighlight(0f);
        SetBehavioursActive(false);
    }

    private void Update()
    {
        if (Mathf.Approximately(currentHighlight, targetHighlight)) return;

        currentHighlight = Mathf.MoveTowards(currentHighlight, targetHighlight, fadeSpeed * Time.deltaTime);
        ApplyHighlight(currentHighlight);
    }

    public void UpdateVision(bool inCone, float proximity01)
    {
        targetHighlight = inCone ? 1f : Mathf.Clamp01(proximity01) * maxPartialHighlight;

        if (inCone != behavioursActive)
            SetBehavioursActive(inCone);
    }

    private void ApplyHighlight(float amount)
    {
        if (spriteRenderer == null) return;

        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(OpacityId, amount);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

    private void SetBehavioursActive(bool state)
    {
        behavioursActive = state;
        foreach (var b in behavioursToToggle)
        {
            if (b == null) continue;
            if (b == detectionCollider) continue;
            if (ShouldSkip(b)) continue;
            b.enabled = state;
        }
    }

    private bool ShouldSkip(Behaviour b)
    {
        return b == this || b is InteractionHighlight;
    }

    private void AutoFill()
    {
        if (detectionCollider == null)
            detectionCollider = GetComponent<Collider2D>();

        var allBehaviours = GetComponentsInChildren<Behaviour>(true);
        var filtered = new List<Behaviour>();

        foreach (var b in allBehaviours)
        {
            if (b == detectionCollider) continue;
            if (ShouldSkip(b)) continue;
            filtered.Add(b);
        }

        behavioursToToggle = filtered.ToArray();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo) return;

        if (visionRange > 0f)
        {
            Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, visionRange);
        }

        if (proximityRange > 0f)
        {
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, proximityRange);
        }
    }
}