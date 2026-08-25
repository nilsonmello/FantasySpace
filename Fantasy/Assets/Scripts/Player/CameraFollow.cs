using UnityEngine;
using Unity.Cinemachine;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    public CinemachineCamera mainCamera;
    public Transform playerTarget;
    public Transform roomPoint;

    [Header("Movement")]
    public float followSpeed = 5f;

    [Header("Detection")]
    public LayerMask targetLayer;
    public float detectionRadius = 20f;
    public float exitBuffer = 2f;

    [Header("CamPoint")]
    public string camPointTag = "CamPoint";

    [Header("Mouse Influence")]
    [Range(0f, 1f)] public float mouseInfluence = 0.5f;
    public float maxMouseDistance = 5f;

    public Transform cameraTarget;
    private Transform followProxy;
    private Collider2D activeRoomCollider;
    private Camera mainCam;

    void Awake()
    {
        HandleCamera();
        CreateFollowProxy();
        mainCam = Camera.main;
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
            Transform camPoint = FindCamPoint(hit.transform);

            if (camPoint != null)
            {
                roomPoint = camPoint;
                activeRoomCollider = hit;
            }
            else
            {
                roomPoint = null;
                activeRoomCollider = null;
            }
        }
        else
        {
            roomPoint = null;
            activeRoomCollider = null;
        }
    }

    private Transform FindCamPoint(Transform room)
    {
        foreach (Transform t in room.GetComponentsInChildren<Transform>(true))
        {
            if (t.CompareTag(camPointTag))
                return t;
        }
        return null;
    }

    private Vector3 GetPlayerMouseTarget()
    {
        if (mainCam == null)
            mainCam = Camera.main;

        if (mainCam == null || playerTarget == null)
            return playerTarget != null ? playerTarget.position : transform.position;

        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = mainCam.WorldToScreenPoint(playerTarget.position).z;
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = playerTarget.position.z;

        Vector3 offset = mouseWorldPos - playerTarget.position;
        if (maxMouseDistance > 0f && offset.magnitude > maxMouseDistance)
            offset = offset.normalized * maxMouseDistance;

        return playerTarget.position + offset * mouseInfluence;
    }

    private void UpdateFollowProxy()
    {
        Vector3 targetPosition;

        if (roomPoint != null)
        {
            cameraTarget = roomPoint;
            targetPosition = roomPoint.position;
        }
        else if (playerTarget != null)
        {
            cameraTarget = playerTarget;
            targetPosition = GetPlayerMouseTarget();
        }
        else
        {
            return;
        }

        if (followProxy == null) return;

        followProxy.position = Vector3.Lerp(
            followProxy.position,
            targetPosition,
            1f - Mathf.Exp(-followSpeed * Time.deltaTime)
        );
    }
}