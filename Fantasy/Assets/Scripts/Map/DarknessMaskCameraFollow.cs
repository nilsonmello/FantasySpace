using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DarknessMaskCameraFollow : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float marginCells = 2f;
    [SerializeField] private float cellSize = 1f;

    private SpriteRenderer _spriteRenderer;

    private SpriteRenderer SpriteRendererCached =>
        _spriteRenderer != null ? _spriteRenderer : (_spriteRenderer = GetComponent<SpriteRenderer>());

    private void Reset()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (targetCamera == null || SpriteRendererCached.sprite == null)
            return;

        transform.position = new Vector3(
            targetCamera.transform.position.x,
            targetCamera.transform.position.y,
            transform.position.z);

        float visibleHeight;
        float visibleWidth;

        if (targetCamera.orthographic)
        {
            visibleHeight = targetCamera.orthographicSize * 2f;
            visibleWidth = visibleHeight * targetCamera.aspect;
        }
        else
        {
            float distance = Mathf.Abs(targetCamera.transform.position.z - transform.position.z);
            visibleHeight = 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            visibleWidth = visibleHeight * targetCamera.aspect;
        }

        float sizeX = visibleWidth + marginCells * cellSize * 2f;
        float sizeY = visibleHeight + marginCells * cellSize * 2f;

        Vector2 nativeSize = SpriteRendererCached.sprite.bounds.size;
        if (nativeSize.x <= 0f || nativeSize.y <= 0f)
            return;

        transform.localScale = new Vector3(sizeX / nativeSize.x, sizeY / nativeSize.y, 1f);
    }
}
