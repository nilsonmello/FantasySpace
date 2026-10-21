using System;
using UnityEngine;
using UnityEngine.UI;

public class NoiseLevelIndicator : MonoBehaviour
{
    [Serializable]
    public class NoiseProfile
    {
        public float peakHeight = 25f;
        public float riseTime = 0.06f;
        public float fallTime = 0.6f;

        public NoiseProfile(float peakHeight, float riseTime, float fallTime)
        {
            this.peakHeight = peakHeight;
            this.riseTime = riseTime;
            this.fallTime = fallTime;
        }
    }

    [Header("Autofind")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private PlayerSoundEmitter emitter;

    [Header("Bar")]
    [SerializeField] private RectTransform bar;
    [SerializeField] private float minHeight = 2f;
    [SerializeField] private bool growUpOnly = true;

    [Header("profile")]
    [SerializeField] private NoiseProfile crouch = new NoiseProfile(12f, 0.05f, 0.55f);
    [SerializeField] private NoiseProfile walk = new NoiseProfile(25f, 0.06f, 0.60f);
    [SerializeField] private NoiseProfile run = new NoiseProfile(40f, 0.04f, 0.50f);

    [Header("Color")]
    [SerializeField] private Image barImage;
    [SerializeField] private bool colorByLevel = false;
    [SerializeField] private Color lowColor = new Color(0.4f, 1f, 0.5f, 1f);
    [SerializeField] private Color highColor = new Color(1f, 0.3f, 0.2f, 1f);

    private float currentHeight;
    private float targetHeight;
    private float riseSpeed;
    private float fallSpeed;
    private bool rising;
    private bool subscribed;

    private void Awake()
    {
        currentHeight = minHeight;
        targetHeight = minHeight;

        if (growUpOnly)
            SetupPivotBottom();
    }

    private void OnEnable()
    {
        FindEmitter();
        Subscribe();
        ResetState();
        Apply();
    }

    private void OnDisable()
    {
        Unsubscribe();
        ResetState();
    }

    private void ResetState()
    {
        currentHeight = minHeight;
        targetHeight = minHeight;
        rising = false;
    }

    private void Update()
    {
        if (emitter == null)
        {
            FindEmitter();
            Subscribe();
        }

        if (rising)
        {
            currentHeight = Mathf.MoveTowards(currentHeight, targetHeight, riseSpeed * Time.deltaTime);

            if (Mathf.Approximately(currentHeight, targetHeight))
                rising = false;
        }
        else if (currentHeight > minHeight)
        {
            currentHeight = Mathf.MoveTowards(currentHeight, minHeight, fallSpeed * Time.deltaTime);
        }

        Apply();
    }

    private void HandlePulse(NoiseKind kind)
    {
        NoiseProfile p = GetProfile(kind);

        targetHeight = Mathf.Max(currentHeight, p.peakHeight);

        riseSpeed = Mathf.Max(0.01f, (targetHeight - currentHeight) / Mathf.Max(0.001f, p.riseTime));
        fallSpeed = Mathf.Max(0.01f, (p.peakHeight - minHeight) / Mathf.Max(0.001f, p.fallTime));

        rising = targetHeight > currentHeight;
    }

    private NoiseProfile GetProfile(NoiseKind kind)
    {
        switch (kind)
        {
            case NoiseKind.Crouch: return crouch;
            case NoiseKind.Run: return run;
            default: return walk;
        }
    }

    private void SetupPivotBottom()
    {
        if (bar == null) return;

        float oldPivotY = bar.pivot.y;
        if (Mathf.Approximately(oldPivotY, 0f)) return;

        float height = bar.rect.height;

        bar.pivot = new Vector2(bar.pivot.x, 0f);
        bar.anchoredPosition -= new Vector2(0f, oldPivotY * height);
    }

    private void Apply()
    {
        if (bar != null)
            bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentHeight);

        if (colorByLevel && barImage != null)
        {
            float top = Mathf.Max(crouch.peakHeight, walk.peakHeight, run.peakHeight);
            float level = Mathf.InverseLerp(minHeight, top, currentHeight);
            barImage.color = Color.Lerp(lowColor, highColor, level);
        }
    }

    private void FindEmitter()
    {
        if (emitter != null) return;

        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null) emitter = go.GetComponent<PlayerSoundEmitter>();
    }

    private void Subscribe()
    {
        if (subscribed || emitter == null) return;

        emitter.OnNoisePulse += HandlePulse;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;

        if (emitter != null)
            emitter.OnNoisePulse -= HandlePulse;

        subscribed = false;
    }
}