using UnityEngine;
using Unity.Cinemachine;

public class CameraFollow : MonoBehaviour
{

    public CinemachineCamera mainCamera;

    public Transform playerTarget;

    public Transform roomPoint;

    public float followSpeed = 5f;

    public LayerMask targetLayer;

    public int maxDist = 20;

    public Transform cameraTarget;


    void Awake()
    {
        HandleCamera();

    }

    void Update()
    {
        FindPlayerTransform();
        FindRoomPosition();
        SetTarget();
    }

    void HandleCamera()
    {
        mainCamera = GetComponent<CinemachineCamera>();
    }

    private void FindPlayerTransform()
    {
        if (playerTarget != null) return;

        Transform player = GameObject.FindWithTag("Player").transform;
        {
            if (player != null)
                playerTarget = player.transform;         
        }

    }


    private void FindRoomPosition()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, maxDist, targetLayer);

        if (hit != null)
        {
            roomPoint = hit.transform;  
        }
        else
        {
            roomPoint = null;
        }
    }

    private void SetTarget()
    {
        if (roomPoint != null)
        {
            cameraTarget = roomPoint;
        }
        else
        {
            cameraTarget = playerTarget;
        }

        mainCamera.Follow  = cameraTarget;
    }
}