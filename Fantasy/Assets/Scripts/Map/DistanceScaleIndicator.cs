using UnityEngine;

public class DirectionRadarIndicator : MonoBehaviour
{
    public enum WorldPlane
    {
        XY,
        XZ
    }

    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string targetTag = "Centipide";

    [SerializeField] private RectTransform playerDot;
    [SerializeField] private RectTransform targetDot;

    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 8f;

    [SerializeField] private float minRadius = 6f;
    [SerializeField] private float maxRadius = 40f;

    [SerializeField] private bool directionOnly = false;
    [SerializeField] private float fixedRadius = 40f;

    [SerializeField] private WorldPlane worldPlane = WorldPlane.XY;
    [SerializeField] private Camera referenceCamera;

    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private bool hideWhenBeyondMax = false;

    private Transform player;
    private Transform target;

    private Vector2 center;
    private Vector2 currentPos;
    private Vector2 lastDir = Vector2.up;

    private void Awake()
    {
        center = playerDot != null ? playerDot.anchoredPosition : Vector2.zero;
        currentPos = center;

        if (targetDot != null)
            targetDot.anchoredPosition = center;
    }

    private void Start()
    {
        FindPlayer();
        FindTarget();
    }

    private void Update()
    {
        if (player == null) FindPlayer();
        if (target == null) FindTarget();

        if (player == null || target == null || targetDot == null) return;

        Vector3 delta = target.position - player.position;

        Vector2 planar = worldPlane == WorldPlane.XZ
            ? new Vector2(delta.x, delta.z)
            : new Vector2(delta.x, delta.y);

        float distance = planar.magnitude;

        Vector2 dir = GetScreenDirection(delta);
        if (dir.sqrMagnitude > 0.0001f)
            lastDir = dir.normalized;

        float radius;
        if (directionOnly)
        {
            radius = fixedRadius;
        }
        else
        {
            float t = Mathf.InverseLerp(minDistance, maxDistance, distance);
            radius = Mathf.Lerp(minRadius, maxRadius, t);
        }

        Vector2 desiredPos = center + lastDir * radius;

        float k = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        currentPos = Vector2.Lerp(currentPos, desiredPos, k);
        targetDot.anchoredPosition = currentPos;

        bool visible = directionOnly || !hideWhenBeyondMax || distance <= maxDistance;
        if (targetDot.gameObject.activeSelf != visible)
            targetDot.gameObject.SetActive(visible);
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

    public void SetDirectionOnly(bool value) => directionOnly = value;

    private void OnDisable()
    {
        if (targetDot != null)
            targetDot.anchoredPosition = center;

        currentPos = center;
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
        var rt = playerDot != null ? playerDot : transform as RectTransform;
        if (rt == null) return;

        Vector3 pos = rt.position;
        float scale = rt.lossyScale.x;

        if (directionOnly)
        {
            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.8f);
            DrawCircle(pos, fixedRadius * scale);
            return;
        }

        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.8f);
        DrawCircle(pos, maxRadius * scale);

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
        DrawCircle(pos, minRadius * scale);
    }

    private static void DrawCircle(Vector3 center, float radius)
    {
        const int segments = 48;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            Vector3 next = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}