using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

public interface IVisionTarget
{
    void UpdateVision(bool inCone, float proximity01);
}

public class VisionCone : MonoBehaviour
{
    [Header("Cone")]
    [SerializeField] private float viewRadius = 6f;
    [SerializeField, Range(0f, 360f)] private float viewAngle = 60f;

    [Header("Origin")]
    [SerializeField] private Transform visionOrigin;

    [Header("Detection")]
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private bool useLineOfSight = true;

    [Header("Câmera")]
    public CinemachineCamera mainCamera;
    private Camera renderCamera;

    private Vector2 aimDirection = Vector2.right;
    private readonly HashSet<IVisionTarget> trackedTargets = new();

    private Vector3 Origin => visionOrigin != null ? visionOrigin.position : transform.position;

    private void Update()
    {
        EnsureCameraReference();
        UpdateAimDirection();
        UpdateVision();
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
        var currentFrame = new HashSet<IVisionTarget>();

        Collider2D[] candidates = Physics2D.OverlapCircleAll(Origin, viewRadius, targetMask);

        foreach (var col in candidates)
        {
            if (!col.TryGetComponent<IVisionTarget>(out var target)) continue;

            Vector2 toTarget = (Vector2)col.transform.position - (Vector2)Origin;
            float dist = toTarget.magnitude;
            Vector2 dirToTarget = toTarget.normalized;

            bool blocked = false;
            if (useLineOfSight)
            {
                RaycastHit2D hit = Physics2D.Raycast(Origin, dirToTarget, dist, obstacleMask);
                blocked = hit.collider != null;
            }

            float proximity01 = blocked ? 0f : Mathf.Clamp01(1f - dist / viewRadius);
            bool inCone = !blocked && Vector2.Angle(aimDirection, dirToTarget) <= viewAngle / 2f;

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
        Gizmos.color = Color.yellow;
        Vector3 pos = Origin;
        Vector3 left = DirFromAngle(-viewAngle / 2f);
        Vector3 right = DirFromAngle(viewAngle / 2f);

        Gizmos.DrawLine(pos, pos + left * viewRadius);
        Gizmos.DrawLine(pos, pos + right * viewRadius);
        Gizmos.DrawWireSphere(pos, viewRadius);
    }

    private Vector3 DirFromAngle(float angleDeg)
    {
        float rad = Mathf.Atan2(aimDirection.y, aimDirection.x) + angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
    }
}