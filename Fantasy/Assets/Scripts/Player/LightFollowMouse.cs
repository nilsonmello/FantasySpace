using UnityEngine;

public class LightFollowMouse : MonoBehaviour
{
    [SerializeField] private float angleOffset = -90f;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector2 direction = (mouseWorldPos - transform.position);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle + angleOffset);
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = mainCamera.WorldToScreenPoint(transform.position).z;
        return mainCamera.ScreenToWorldPoint(mouseScreenPos);
    }

    // void OnDrawGizmos()
    // {
    //     if (!Application.isPlaying) return;

    //     Vector3 mouseWorldPos = GetMouseWorldPosition();

    //     Gizmos.color = Color.red;
    //     Gizmos.DrawLine(transform.position, mouseWorldPos);

    //     Gizmos.color = Color.green;
    //     Gizmos.DrawLine(transform.position, transform.position + transform.right * 2f);
    // }
}