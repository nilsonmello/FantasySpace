using UnityEngine;

public class SearchAreaIndicator : MonoBehaviour
{
    [SerializeField] private HeadStateMovement head;
    [SerializeField] private SpriteRenderer marker;
    [SerializeField] private LineRenderer circle;

    [Header("Circle")]
    [SerializeField] private int circleSegments = 64;
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private Color circleColor = new Color(1f, 0.3f, 0.2f, 0.8f);
    [SerializeField] private int circleSortingOrder = 5;

    [Header("Marker")]
    [SerializeField] private Color markerColor = Color.white;

    [Header("Animation")]
    [SerializeField] private float fadeSpeed = 6f;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField, Range(0f, 0.5f)] private float pulseAmount = 0.15f;

    [Header("Auto Find")]
    [SerializeField] private float findInterval = 0.5f;

    private float nextFindTime;
    private float alpha;
    private Vector2 cachedCenter;
    private float cachedRadius;

    private void Reset()
    {
        head = FindFirstObjectByType<HeadStateMovement>();
    }

    private void Awake()
    {
        if (circle != null)
        {
            circle.useWorldSpace = true;
            circle.loop = true;
            circle.positionCount = circleSegments;
            circle.startWidth = lineWidth;
            circle.endWidth = lineWidth;
            circle.sortingOrder = circleSortingOrder;
        }

        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (head == null)
            TryFindHead();

        bool active = head != null && head.HasPlayerSearchArea;

        if (active)
        {
            cachedCenter = head.SearchCenter;
            cachedRadius = head.SearchRadius;
        }

        alpha = Mathf.MoveTowards(alpha, active ? 1f : 0f, fadeSpeed * Time.deltaTime);

        if (alpha <= 0.001f)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        if (marker != null)
        {
            marker.transform.position = cachedCenter;
            Color c = markerColor;
            c.a *= alpha;
            marker.color = c;
        }

        if (circle != null)
        {
            DrawCircle(cachedCenter, cachedRadius);

            Color c = circleColor;
            c.a *= alpha * pulse;
            circle.startColor = c;
            circle.endColor = c;
        }
    }

    private void TryFindHead()
    {
        if (Time.time < nextFindTime) return;
        nextFindTime = Time.time + findInterval;

        head = FindFirstObjectByType<HeadStateMovement>();
    }

    private void DrawCircle(Vector2 center, float radius)
    {
        for (int i = 0; i < circleSegments; i++)
        {
            float a = (i / (float)circleSegments) * Mathf.PI * 2f;
            circle.SetPosition(i, new Vector3(
                center.x + Mathf.Cos(a) * radius,
                center.y + Mathf.Sin(a) * radius,
                0f));
        }
    }

    private void SetVisible(bool visible)
    {
        if (marker != null && marker.enabled != visible) marker.enabled = visible;
        if (circle != null && circle.enabled != visible) circle.enabled = visible;
    }
}