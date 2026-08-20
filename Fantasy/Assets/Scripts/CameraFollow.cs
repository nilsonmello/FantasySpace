using UnityEngine;
using Unity.Cinemachine;

public class CameraFollow : MonoBehaviour
{
    [Header("Referências")]
    public CinemachineCamera mainCamera;
    public Transform playerTarget;
    public Transform roomPoint;

    [Header("Movimento")]
    public float followSpeed = 5f;

    [Header("Detecção")]
    public LayerMask targetLayer;
    public float detectionRadius = 20f;
    public float exitBuffer = 2f;

    public Transform cameraTarget;
    private Transform followProxy;
    private Collider2D activeRoomCollider;

    void Awake()
    {
        HandleCamera();
        CreateFollowProxy();
    }

    void Update()
    {
        FindPlayerTransform();
        FindRoomPosition();
        UpdateFollowProxy();
    }

    void HandleCamera()
    {
        if (mainCamera == null)
            mainCamera = GetComponent<CinemachineCamera>();
    }

    private void CreateFollowProxy()
    {
        if (followProxy != null) return;

        GameObject proxyObj = new GameObject("CameraFollowProxy");
        followProxy = proxyObj.transform;

        if (mainCamera != null)
            mainCamera.Follow = followProxy;
    }

    private void FindPlayerTransform()
    {
        if (playerTarget != null) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerTarget = player.transform;
    }

    private void FindRoomPosition()
    {
        if (playerTarget == null)
        {
            roomPoint = null;
            activeRoomCollider = null;
            return;
        }

        float radius = activeRoomCollider != null ? detectionRadius + exitBuffer : detectionRadius;

        Collider2D hit = Physics2D.OverlapCircle(playerTarget.position, radius, targetLayer);

        if (hit != null)
        {
            roomPoint = hit.transform;
            activeRoomCollider = hit;
        }
        else
        {
            roomPoint = null;
            activeRoomCollider = null;
        }
    }

    private void UpdateFollowProxy()
    {
        cameraTarget = roomPoint != null ? roomPoint : playerTarget;

        if (cameraTarget == null || followProxy == null) return;

        followProxy.position = Vector3.Lerp(followProxy.position, cameraTarget.position, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
    }
}