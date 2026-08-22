using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;

public interface IVisionTarget
{
    void OnEnterVision();
    void OnExitVision();
}

public class VisionCone : MonoBehaviour
{
    [Header("Cone")]
    [SerializeField] private float viewRadius = 6f;
    [SerializeField, Range(0f, 360f)] private float viewAngle = 60f;

    [Header("Origem do cone")]
    [Tooltip("Se vazio, usa a posição deste transform.")]
    [SerializeField] private Transform visionOrigin;

    [Header("Detecção")]
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private bool useLineOfSight = true;

    [Header("Câmera")]
    public CinemachineCamera mainCamera;
    private Camera renderCamera;

    private Vector2 aimDirection = Vector2.right;
    private readonly HashSet<IVisionTarget> currentlyVisible = new();
    private readonly HashSet<IVisionTarget> previouslyVisible = new();

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
        currentlyVisible.Clear();

        Collider2D[] candidates = Physics2D.OverlapCircleAll(Origin, viewRadius, targetMask);

        foreach (var col in candidates)
        {
            Vector2 dirToTarget = (Vector2)col.transform.position - (Vector2)Origin;
            float dist = dirToTarget.magnitude;
            dirToTarget.Normalize();

            if (Vector2.Angle(aimDirection, dirToTarget) > viewAngle / 2f)
                continue;

            if (useLineOfSight)
            {
                RaycastHit2D hit = Physics2D.Raycast(Origin, dirToTarget, dist, obstacleMask);
                if (hit.collider != null) continue;
            }

            if (col.TryGetComponent<IVisionTarget>(out var target))
                currentlyVisible.Add(target);
        }

        foreach (var t in currentlyVisible)
            if (!previouslyVisible.Contains(t)) t.OnEnterVision();

        foreach (var t in previouslyVisible)
            if (!currentlyVisible.Contains(t)) t.OnExitVision();

        previouslyVisible.Clear();
        previouslyVisible.UnionWith(currentlyVisible);
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