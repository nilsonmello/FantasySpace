using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VignetteController : MonoBehaviour
{
    public Camera mainCamera;
    public Volume volume;
    
    private Transform player;
    private Vignette vignette;

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        var uac = mainCamera.GetComponent<UniversalAdditionalCameraData>();
        if (uac != null) uac.renderPostProcessing = true;

        // if (PlayerMovement.Instance != null)
        // {
        //     player = PlayerMovement.Instance.transform;
        // }

        volume.profile.TryGet<Vignette>(out vignette);
    }

    void Update()
    {
        if (player != null && vignette != null && mainCamera != null)
        {
            Vector3 viewportPoint = mainCamera.WorldToViewportPoint(player.position);
            vignette.center.value = new Vector2(viewportPoint.x, viewportPoint.y);
        }
    }
}