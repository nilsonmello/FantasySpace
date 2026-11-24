using Unity.Cinemachine;
using UnityEngine;

public class HideoutCameraFX : MonoBehaviour
{
    [Header("Push / Pull")]
    [SerializeField, Range(0f, 0.2f)] private float pulseAmplitudeMin = 0.01f;
    [SerializeField, Range(0f, 0.3f)] private float pulseAmplitudeMax = 0.06f;
    [SerializeField, Min(0f)] private float pulseFrequencyMin = 0.5f;
    [SerializeField, Min(0f)] private float pulseFrequencyMax = 1.4f;
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Proximity Shake")]
    [SerializeField, Min(0f)] private float shakeStartDistance = 8f;
    [SerializeField, Min(0f)] private float shakeFullDistance = 2f;
    [SerializeField, Min(0f)] private float shakeMaxAmplitude = 0.25f;
    [SerializeField, Min(0f)] private float shakeFrequency = 25f;
    [SerializeField, Min(0.01f)] private float shakeResponseTime = 0.3f;

    [Header("Blend")]
    [SerializeField, Min(0f)] private float blendInTime = 0.4f;
    [SerializeField, Min(0f)] private float blendOutTime = 0.4f;

    private Camera cam;
    private CinemachineCamera vcam;
    private CinemachineCameraOffset offsetExtension;
    private Transform origin;

    private bool ortho;
    private float baseSize;
    private Vector3 baseOffset;

    private float progress;
    private float blend;
    private float phase;
    private float shakeClock;
    private float shakeIntensity;

    private bool running;
    private bool ending;

    public void Begin(Transform source)
    {
        origin = source;

        if (running)
        {
            ending = false;
            return;
        }

        ResolveCamera();
        if (cam == null && vcam == null) return;

        if (origin == null)
        {
            var player = FindFirstObjectByType<PlayerHideState>();
            if (player != null) origin = player.transform;
        }

        ortho = cam == null || cam.orthographic;
        baseSize = GetSize();

        offsetExtension = null;
        if (vcam != null)
        {
            offsetExtension = vcam.GetComponent<CinemachineCameraOffset>();
            if (offsetExtension == null)
                offsetExtension = vcam.gameObject.AddComponent<CinemachineCameraOffset>();

            baseOffset = offsetExtension.Offset;
        }

        progress = 0f;
        blend = 0f;
        shakeIntensity = 0f;
        ending = false;
        running = true;
    }

    public void SetProgress(float value)
    {
        progress = Mathf.Clamp01(value);
    }

    public void End()
    {
        if (running) ending = true;
    }

    public void StopImmediate()
    {
        Restore();
    }

    private void OnDisable()
    {
        Restore();
    }

    private void Update()
    {
        if (!running) return;

        float dt = Time.deltaTime;

        float blendTarget = ending ? 0f : 1f;
        float blendTime = ending ? blendOutTime : blendInTime;
        blend = blendTime > 0f ? Mathf.MoveTowards(blend, blendTarget, dt / blendTime) : blendTarget;

        if (ending && blend <= 0f)
        {
            Restore();
            return;
        }

        float k = pulseCurve.Evaluate(progress);
        float amplitude = Mathf.Lerp(pulseAmplitudeMin, pulseAmplitudeMax, k);
        float frequency = Mathf.Lerp(pulseFrequencyMin, pulseFrequencyMax, k);

        phase += Mathf.PI * 2f * frequency * dt;
        SetSize(baseSize * (1f + Mathf.Sin(phase) * amplitude * blend));

        UpdateShake(dt);
    }

    private void UpdateShake(float dt)
    {
        if (offsetExtension == null) return;

        float target = 0f;
        if (origin != null && ThreatSource.TryGetNearest(origin.position, out float distance))
            target = Mathf.InverseLerp(shakeStartDistance, shakeFullDistance, distance);

        shakeIntensity = Mathf.MoveTowards(shakeIntensity, target * target, dt / shakeResponseTime);

        shakeClock += dt * shakeFrequency;
        float x = (Mathf.PerlinNoise(shakeClock, 0.17f) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(0.53f, shakeClock) - 0.5f) * 2f;

        offsetExtension.Offset = baseOffset + new Vector3(x, y, 0f) * shakeMaxAmplitude * shakeIntensity * blend;
    }

    private void Restore()
    {
        if (!running) return;

        running = false;
        ending = false;
        blend = 0f;
        shakeIntensity = 0f;

        SetSize(baseSize);

        if (offsetExtension != null)
            offsetExtension.Offset = baseOffset;

        offsetExtension = null;
    }

    private void ResolveCamera()
    {
        cam = Camera.main;
        vcam = null;

        if (cam != null && cam.TryGetComponent(out CinemachineBrain brain))
            vcam = brain.ActiveVirtualCamera as CinemachineCamera;

        if (vcam == null)
            vcam = FindFirstObjectByType<CinemachineCamera>();
    }

    private float GetSize()
    {
        if (vcam != null)
        {
            var lens = vcam.Lens;
            return ortho ? lens.OrthographicSize : lens.FieldOfView;
        }

        return ortho ? cam.orthographicSize : cam.fieldOfView;
    }

    private void SetSize(float value)
    {
        if (vcam != null)
        {
            var lens = vcam.Lens;
            if (ortho) lens.OrthographicSize = value;
            else lens.FieldOfView = value;
            vcam.Lens = lens;
            return;
        }

        if (cam == null) return;

        if (ortho) cam.orthographicSize = value;
        else cam.fieldOfView = value;
    }
}
