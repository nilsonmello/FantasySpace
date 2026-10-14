using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class WireEnd : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private WirePuzzle puzzle;
    [SerializeField] private RectTransform lineOrigin;
    [SerializeField] private LineRenderer lineRenderer;

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

        SetupSorting();
        SetupLine();
        ApplyColor();
    }

    private void OnEnable() => UpdateLine();

    public void SetTarget(WireSlot slot)
    {
        targetSlot = slot;
        targetSlot.SetColor(wireColor);
    }

    private void SetupSorting()
    {
        lineRenderer.sortingLayerID = rootCanvas.sortingLayerID;
        lineRenderer.sortingOrder = rootCanvas.sortingOrder + 1;

        Canvas endCanvas = GetComponent<Canvas>();
        if (endCanvas == null) endCanvas = gameObject.AddComponent<Canvas>();
        endCanvas.overrideSorting = true;
        endCanvas.sortingLayerID = rootCanvas.sortingLayerID;
        endCanvas.sortingOrder = rootCanvas.sortingOrder + 2;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    private void SetupLine()
    {
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = segments + 1;
        points = new Vector3[segments + 1];
    }

    private void ApplyColor()
    {
        lineRenderer.startColor = wireColor;
        lineRenderer.endColor = wireColor;

        Image image = GetComponent<Image>();
        if (image != null) image.color = wireColor;
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