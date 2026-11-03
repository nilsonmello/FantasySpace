using UnityEngine;

public class DirectionRadarIndicator : MonoBehaviour
{
    public enum WorldPlane
    {
        XY,
        XZ
    }

    public enum DisplayMode
    {
        FullPosition,
        DirectionOnly
    }

    [Header("Auto-find")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string targetTag = "Centipide";

    [Header("Points")]
    [SerializeField] private RectTransform playerDot;
    [SerializeField] private RectTransform targetDot;

    [SerializeField] private Camera minimapCamera;
    [SerializeField] private RectTransform minimapDisplay;
    [SerializeField] private bool clampInsideMinimap = true;

    [Header("Modo")]
    [SerializeField] private DisplayMode displayMode = DisplayMode.FullPosition;

    [Header("Distance")]
    [SerializeField, Min(0f)] private float minRadius = 0f;
    [SerializeField, Min(0f)] private float maxRadius = 12f;
    [SerializeField] private bool hideOutsideRadius = true;

    [Header("FullPosition")]
    [SerializeField] private bool clampTargetToEdge = true;

    [Header("DirectionOnly")]
    [SerializeField] private float fixedRadius = 40f;
    [SerializeField] private float smoothSpeed = 10f;

    [Header("Direction")]
    [SerializeField] private WorldPlane worldPlane = WorldPlane.XY;
    [SerializeField] private Camera referenceCamera;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmo = true;

    private Transform player;
    private Transform target;

    private Vector2 currentOffset;
    private Vector2 lastDir = Vector2.up;

    private bool SyncWithMinimap => minimapCamera != null && minimapDisplay != null;

    private void Awake()
    {
        if (!SyncWithMinimap)
            Debug.LogWarning($"{name}: camera or display out", this);
    }

    private void Start()
    {
        FindPlayer();
        FindTarget();
    }

    private void LateUpdate()
    {
        if (player == null) FindPlayer();
        if (target == null) FindTarget();

        if (player == null || target == null || targetDot == null || !SyncWithMinimap) return;

        if (playerDot != null)
            SyncPlayerDot();

        Vector3 delta = target.position - player.position;
        float distance = PlanarDistance(delta);

        bool outsideRange = distance > maxRadius || distance < minRadius;
        bool visible = !(hideOutsideRadius && outsideRange);

        if (targetDot.gameObject.activeSelf != visible)
            targetDot.gameObject.SetActive(visible);

        if (!visible) return;

        if (displayMode == DisplayMode.FullPosition)
            UpdateFullPosition(delta, distance);
        else
            UpdateDirectionOnly(delta);
    }

    private void UpdateFullPosition(Vector3 delta, float distance)
    {
        Vector3 worldPos = target.position;

        if (!hideOutsideRadius && distance > 0.0001f)
        {
            float clamped = Mathf.Clamp(distance, minRadius, maxRadius);
            worldPos = player.position + delta * (clamped / distance);
        }

        Vector3 vp = minimapCamera.WorldToViewportPoint(worldPos);
        bool inside = vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f && vp.z > 0f;

        if (!inside && !clampTargetToEdge)
        {
            if (targetDot.gameObject.activeSelf) targetDot.gameObject.SetActive(false);
            return;
        }

        Rect r = minimapDisplay.rect;
        var local = new Vector2(
            r.xMin + vp.x * r.width,
            r.yMin + vp.y * r.height);

        if (!inside)
        {
            local.x = Mathf.Clamp(local.x, r.xMin, r.xMax);
            local.y = Mathf.Clamp(local.y, r.yMin, r.yMax);
        }

        targetDot.position = minimapDisplay.TransformPoint(local);
    }

    private void UpdateDirectionOnly(Vector3 delta)
    {
        Vector2 dir = GetScreenDirection(delta);
        if (dir.sqrMagnitude > 0.0001f)
            lastDir = dir.normalized;

        Vector2 desiredOffset = lastDir * fixedRadius;

        float k = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        currentOffset = Vector2.Lerp(currentOffset, desiredOffset, k);

        Vector2 center = playerDot != null ? playerDot.anchoredPosition : Vector2.zero;
        targetDot.anchoredPosition = center + currentOffset;
    }

    private void SyncPlayerDot()
    {
        Vector3 vp = minimapCamera.WorldToViewportPoint(player.position);
        Rect r = minimapDisplay.rect;

        var local = new Vector2(
            r.xMin + vp.x * r.width,
            r.yMin + vp.y * r.height);

        if (clampInsideMinimap)
        {
            local.x = Mathf.Clamp(local.x, r.xMin, r.xMax);
            local.y = Mathf.Clamp(local.y, r.yMin, r.yMax);
        }

        playerDot.position = minimapDisplay.TransformPoint(local);
    }

    private float PlanarDistance(Vector3 delta)
    {
        return worldPlane == WorldPlane.XZ
            ? new Vector2(delta.x, delta.z).magnitude
            : new Vector2(delta.x, delta.y).magnitude;
    }

    private Vector2 GetScreenDirection(Vector3 delta)
    {
        if (referenceCamera == null)
        {
            return worldPlane == WorldPlane.XZ
                ? new Vector2(delta.x, delta.z)
                : new Vector2(delta.x, delta.y);
        }

        Vector3 right = referenceCamera.transform.right;
        Vector3 up;

        if (worldPlane == WorldPlane.XZ)
        {
            right.y = 0f;
            up = referenceCamera.transform.forward;
            up.y = 0f;
        }
        else
        {
            right.z = 0f;
            up = referenceCamera.transform.up;
            up.z = 0f;
        }

        right.Normalize();
        up.Normalize();

        return new Vector2(Vector3.Dot(delta, right), Vector3.Dot(delta, up));
    }

    public void SetDisplayMode(DisplayMode mode) => displayMode = mode;
    public void SetDirectionOnly(bool value) => displayMode = value ? DisplayMode.DirectionOnly : DisplayMode.FullPosition;
    public void SetHideOutsideRadius(bool value) => hideOutsideRadius = value;

    private void OnDisable()
    {
        currentOffset = Vector2.zero;
    }

    private void FindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null) player = go.transform;
    }

    private void FindTarget()
    {
        var go = GameObject.FindGameObjectWithTag(targetTag);
        if (go != null) target = go.transform;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo) return;

        Transform p = player;
        if (p == null)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) p = go.transform;
        }
        if (p == null) return;

        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.8f);
        DrawCircle(p.position, maxRadius);

        if (minRadius > 0f)
        {
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
            DrawCircle(p.position, minRadius);
        }
    }

    private void DrawCircle(Vector3 center, float radius)
    {
        const int segments = 48;
        bool xz = worldPlane == WorldPlane.XZ;
        Vector3 prev = center + (xz ? new Vector3(radius, 0f, 0f) : new Vector3(radius, 0f, 0f));

        for (int i = 1; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            float c = Mathf.Cos(a) * radius;
            float s = Mathf.Sin(a) * radius;
            Vector3 next = center + (xz ? new Vector3(c, 0f, s) : new Vector3(c, s, 0f));
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}