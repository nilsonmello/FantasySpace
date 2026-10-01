using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

public interface IVisionTarget
{
    void UpdateVision(bool inCone, float proximity01);

    float VisionRange => 0f;
    float ProximityRange => 0f;
}

public class VisionCone : MonoBehaviour
{
    [Header("Cone")]
    [SerializeField, Min(0f)] private float defaultViewRadius = 6f;
    [SerializeField, Range(0f, 360f)] private float viewAngle = 60f;

    [Header("Busca")]
    [SerializeField, Min(0f)] private float searchRadius = 12f;

    [Header("Origin")]
    [SerializeField] private Transform visionOrigin;

    [Header("Detection")]
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private bool useLineOfSight = true;

    [Header("Camera")]
    public CinemachineCamera mainCamera;
    private Camera renderCamera;

    private Vector2 aimDirection = Vector2.right;
    private readonly HashSet<IVisionTarget> trackedTargets = new();
    private readonly HashSet<IVisionTarget> currentFrame = new();

    private Vector3 Origin => visionOrigin != null ? visionOrigin.position : transform.position;
    [SerializeField] private PlayerHideState playerHideState;

    private void Awake()
    {
        playerHideState = FindObjectOfType<PlayerHideState>();
    }

    private void Update()
    {
        EnsureCameraReference();
        UpdateAimDirection();
        HandleHidePlayer();
    }

    void HandleHidePlayer()
    {
        if(playerHideState == null) return;

        if(!playerHideState.IsHidden)
        {
            UpdateVision();
            Debug.Log("oi");
        }
        else
        {

        }
    }

    private void EnsureCameraReference()
    {
        if (renderCamera == null)
            renderCamera = Camera.main;

        if (mainCamera == null)
            mainCamera = FindFirstObjectByType<CinemachineCamera>();
    }

    private void UpdateAimDirection()
    {
        if (renderCamera == null) return;

        Vector3 mouseWorld = renderCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = Origin.z;
        aimDirection = ((Vector2)(mouseWorld - Origin)).normalized;
    }

    private void UpdateVision()
    {
        currentFrame.Clear();

        float radius = Mathf.Max(searchRadius, defaultViewRadius);
        Collider2D[] candidates = Physics2D.OverlapCircleAll(Origin, radius, targetMask);

        foreach (var col in candidates)
        {
            if (!col.TryGetComponent<IVisionTarget>(out var target)) continue;

            Vector2 toTarget = (Vector2)col.transform.position - (Vector2)Origin;
            float dist = toTarget.magnitude;
            Vector2 dirToTarget = toTarget.normalized;

            float visionRange = target.VisionRange > 0f ? target.VisionRange : defaultViewRadius;
            float proximityRange = target.ProximityRange > 0f ? target.ProximityRange : defaultViewRadius;

            bool blocked = false;
            if (useLineOfSight)
            {
                RaycastHit2D hit = Physics2D.Raycast(Origin, dirToTarget, dist, obstacleMask);
                blocked = hit.collider != null;
            }

            float proximity01 = blocked || proximityRange <= 0f
                ? 0f
                : Mathf.Clamp01(1f - dist / proximityRange);

            bool inCone = !blocked
                          && dist <= visionRange
                          && Vector2.Angle(aimDirection, dirToTarget) <= viewAngle / 2f;

            target.UpdateVision(inCone, proximity01);
            currentFrame.Add(target);
        }

        foreach (var t in trackedTargets)
        {
            if (!currentFrame.Contains(t))
                t.UpdateVision(false, 0f);
        }

        trackedTargets.Clear();
        trackedTargets.UnionWith(currentFrame);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 pos = Origin;

        Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
        Gizmos.DrawWireSphere(pos, Mathf.Max(searchRadius, defaultViewRadius));

        Gizmos.color = Color.yellow;
        Vector3 left = DirFromAngle(-viewAngle / 2f);
        Vector3 right = DirFromAngle(viewAngle / 2f);

        Gizmos.DrawLine(pos, pos + left * defaultViewRadius);
        Gizmos.DrawLine(pos, pos + right * defaultViewRadius);
        Gizmos.DrawWireSphere(pos, defaultViewRadius);
    }

    private Vector3 DirFromAngle(float angleDeg)
    {
        float rad = Mathf.Atan2(aimDirection.y, aimDirection.x) + angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
    }
}