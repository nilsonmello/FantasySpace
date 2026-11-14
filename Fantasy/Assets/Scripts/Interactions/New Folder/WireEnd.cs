using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class WireEnd : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private WirePuzzle puzzle;
    [SerializeField] private RectTransform lineOrigin;
    [SerializeField] private LineRenderer lineRenderer;

    [SerializeField] private Sprite endSprite;
    [SerializeField] private Sprite slotSprite;
    [SerializeField] private Sprite originSprite;

    [SerializeField] private Color wireColor = Color.red;
    [SerializeField] private float lineWidth = 10f;
    [SerializeField] private float sag = 0f;
    [SerializeField, Range(2, 40)] private int segments = 20;

    private RectTransform rect;
    private Canvas rootCanvas;
    private Vector3 startPosition;
    private Vector3[] points;
    [SerializeField] private WireSlot targetSlot;

    public bool IsConnected { get; private set; }

    private void Awake()
    {
        rect = (RectTransform)transform;
        rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        startPosition = rect.position;

        if (puzzle == null)
            puzzle = GetComponentInParent<WirePuzzle>();

        if (puzzle == null)
            Debug.LogError($"{name}: nenhum WirePuzzle");

        if (rootCanvas.renderMode != RenderMode.ScreenSpaceCamera || rootCanvas.worldCamera == null)
            Debug.LogWarning($"{name}: o canvas precisa ser Overlay", this);

        SetupSorting();
        SetupLine();
        ApplyVisuals();
    }

    private void OnEnable() => UpdateLine();

    public void SetTarget(WireSlot slot)
    {
        targetSlot = slot;
        targetSlot.SetVisual(slotSprite, wireColor);
    }

    private void SetupSorting()
    {
        lineRenderer.gameObject.layer = rootCanvas.gameObject.layer;

        lineRenderer.sortingLayerID = rootCanvas.sortingLayerID;
        lineRenderer.sortingOrder = rootCanvas.sortingOrder + WireSorting.Line;

        Canvas endCanvas = GetComponent<Canvas>();
        if (endCanvas == null) endCanvas = gameObject.AddComponent<Canvas>();
        endCanvas.overrideSorting = true;
        endCanvas.sortingLayerID = rootCanvas.sortingLayerID;
        endCanvas.sortingOrder = rootCanvas.sortingOrder + WireSorting.End;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    private void SetupLine()
    {
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = segments + 1;
        points = new Vector3[segments + 1];
    }

    private void ApplyVisuals()
    {
        lineRenderer.startColor = wireColor;
        lineRenderer.endColor = wireColor;

        Image image = GetComponent<Image>();
        ApplySpriteOrColor(image, endSprite);

        if (lineOrigin != null && originSprite != null)
        {
            Image originImage = lineOrigin.GetComponent<Image>();
            ApplySpriteOrColor(originImage, originSprite);
        }
    }

    private void ApplySpriteOrColor(Image image, Sprite sprite)
    {
        if (image == null) return;

        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
        }
        else
        {
            image.color = wireColor;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsConnected)
        {
            IsConnected = false;
            puzzle.NotifyChanged();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                (RectTransform)rootCanvas.transform, eventData.position,
                eventData.pressEventCamera, out Vector3 world))
        {
            rect.position = world;
            UpdateLine();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        bool overTarget = targetSlot != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                targetSlot.Rect, eventData.position, eventData.pressEventCamera);

        if (overTarget)
        {
            IsConnected = true;
            rect.position = targetSlot.SnapPosition;
            UpdateLine();
        }
        else
        {
            ResetPosition();
        }

        puzzle.NotifyChanged();
    }

    public void ResetPosition()
    {
        IsConnected = false;
        rect.position = startPosition;
        UpdateLine();
    }

    private void UpdateLine()
    {
        if (lineRenderer == null || lineOrigin == null) return;

        float scale = rootCanvas.transform.lossyScale.x;
        lineRenderer.widthMultiplier = lineWidth * scale;

        Vector3 a = lineOrigin.position;
        Vector3 b = rect.position;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p = Vector3.Lerp(a, b, t);
            p.y -= sag * scale * 4f * t * (1f - t);
            points[i] = p;
        }

        lineRenderer.SetPositions(points);
    }
}