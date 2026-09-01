using UnityEngine;

[RequireComponent(typeof(LegIK2D))]
public class LegSpriteSkin : MonoBehaviour
{
    [Header("Position")]
    [SerializeField] private LegIK2D leg;
    [SerializeField] private Transform hip;

    [Header("Skin")]
    [SerializeField] private SpriteRenderer thighRenderer; 
    [SerializeField] private SpriteRenderer shinRenderer;

    [Header("Ajusts")]
    [SerializeField] private bool spriteLengthOnX = true;
    [SerializeField] private float thighAngleOffset = 0f;
    [SerializeField] private float shinAngleOffset = 0f;

    [Header("Stretch")]
    [SerializeField] private bool stretchToFit = false;

    [Header("Thickness")]
    [SerializeField] private float thighThickness = 1f;
    [SerializeField] private float shinThickness = 1f;

    [Header("Visual Order")]
    [SerializeField] private int thighOrder = 0;
    [SerializeField] private int shinOrder = 1;

    private float thighNativeLength = 1f;
    private float shinNativeLength = 1f;

    private void Reset()
    {
        leg = GetComponent<LegIK2D>();
    }

    private void Awake()
    {
        if (leg == null) leg = GetComponent<LegIK2D>();

        if (thighRenderer != null)
        {
            thighRenderer.sortingOrder = thighOrder;
            thighNativeLength = GetNativeLength(thighRenderer);
        }

        if (shinRenderer != null)
        {
            shinRenderer.sortingOrder = shinOrder;
            shinNativeLength = GetNativeLength(shinRenderer);
        }
    }

    private void LateUpdate()
    {
        if (leg == null || hip == null) return;

        Vector2 hipPos = hip.position;
        Vector2 kneePos = leg.KneeWorldPos;
        Vector2 footPos = leg.FootWorldPos;

        PositionSegment(thighRenderer, hipPos, kneePos, thighThickness, thighAngleOffset, thighNativeLength);
        PositionSegment(shinRenderer, kneePos, footPos, shinThickness, shinAngleOffset, shinNativeLength);
    }

    private float GetNativeLength(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null) return 1f;

        Vector2 size = renderer.sprite.bounds.size;
        float native = spriteLengthOnX ? size.x : size.y;
        return native > 0.0001f ? native : 1f;
    }

    private void PositionSegment(SpriteRenderer renderer, Vector2 from, Vector2 to, float thickness, float angleOffset, float nativeLength)
    {
        if (renderer == null) return;

        Transform segment = renderer.transform;

        Vector2 delta = to - from;
        float length = delta.magnitude;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        segment.position = from;
        segment.rotation = Quaternion.Euler(0f, 0f, angle + angleOffset);

        float scaleAlongLength = stretchToFit ? (length / nativeLength) : 1f;

        segment.localScale = spriteLengthOnX
            ? new Vector3(scaleAlongLength, thickness, 1f)
            : new Vector3(thickness, scaleAlongLength, 1f);
    }
}