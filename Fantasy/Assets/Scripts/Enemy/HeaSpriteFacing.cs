using UnityEngine;

public class HeadSpriteFacing : MonoBehaviour
{
    [SerializeField] private HeadStateMovement headMovement;

    [SerializeField] private float rotationOffsetDegrees = 0f;

    private void Reset()
    {
        headMovement = GetComponentInParent<HeadStateMovement>();
    }

    private void LateUpdate()
    {
        if (headMovement == null) return;

        Vector2 dir = headMovement.FacingDirection;
        if (dir.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffsetDegrees);
    }
}