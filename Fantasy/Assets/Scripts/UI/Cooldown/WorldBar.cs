using UnityEngine;

[RequireComponent(typeof(VisionIgnore))]
public class WorldBar : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = new Vector2(0f, -0.9f);

    [Header("Size")]
    [SerializeField] private Vector2 size = new Vector2(1.2f, 0.14f);
    [SerializeField] private float border = 0.03f;

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);
    [SerializeField] private Color fillColor = Color.white;

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 100;

    [Header("Visibility")]
    [SerializeField] private bool hideWhenFull;
    [SerializeField] private bool hideWhenEmpty;

    private SpriteRenderer background;
    private SpriteRenderer fill;
    private float value = 1f;
    private bool suppressed;

    private static Sprite centeredSprite;
    private static Sprite leftPivotSprite;

    public Transform Target => target;

    private void Awake()
    {
        background = CreateRenderer("Background", GetCenteredSprite(), sortingOrder);
        fill = CreateRenderer("Fill", GetLeftPivotSprite(), sortingOrder + 1);

        background.color = backgroundColor;
        fill.color = fillColor;

        background.transform.localScale = new Vector3(size.x + border * 2f, size.y + border * 2f, 1f);
        fill.transform.localPosition = new Vector3(-size.x * 0.5f, 0f, 0f);

        ApplyFill();
        ApplyVisibility();
    }

    private void LateUpdate()
    {
        Transform parent = transform.parent;
        if (parent != null)
        {
            Vector3 s = parent.lossyScale;
            transform.localScale = new Vector3(
                Mathf.Approximately(s.x, 0f) ? 1f : 1f / s.x,
                Mathf.Approximately(s.y, 0f) ? 1f : 1f / s.y,
                1f);
        }

        if (target == null) return;

        transform.SetPositionAndRotation(target.position + (Vector3)offset, Quaternion.identity);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetValue(float normalized)
    {
        value = Mathf.Clamp01(normalized);
        ApplyFill();
        ApplyVisibility();
    }

    public void SetColor(Color color)
    {
        fillColor = color;
        if (fill != null) fill.color = color;
    }

    public void SetSuppressed(bool value)
    {
        if (suppressed == value) return;

        suppressed = value;
        ApplyVisibility();
    }

    private void ApplyFill()
    {
        if (fill == null) return;

        fill.transform.localScale = new Vector3(size.x * value, size.y, 1f);
    }

    private void ApplyVisibility()
    {
        if (background == null) return;

        bool visible = !suppressed
                    && !(hideWhenFull && value >= 0.999f)
                    && !(hideWhenEmpty && value <= 0.001f);

        background.enabled = visible;
        fill.enabled = visible;
    }

    private SpriteRenderer CreateRenderer(string childName, Sprite sprite, int order)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(transform, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = order;
        return sr;
    }

    private static Sprite GetCenteredSprite()
    {
        if (centeredSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            centeredSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        }
        return centeredSprite;
    }

    private static Sprite GetLeftPivotSprite()
    {
        if (leftPivotSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            leftPivotSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 0.5f), tex.width);
        }
        return leftPivotSprite;
    }
}