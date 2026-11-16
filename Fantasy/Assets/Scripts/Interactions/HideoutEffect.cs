using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HideoutEffects : MonoBehaviour
{
    [Header("Vignette")]
    [SerializeField] private RawImage vignette;
    [SerializeField, Range(16, 256)] private int vignetteResolution = 64;
    [SerializeField, Range(0.1f, 2f)] private float vignetteSoftness = 0.6f;
    [SerializeField] private AnimationCurve vignetteCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float fadeOutTime = 0.4f;

    [Header("Pixel")]
    [SerializeField, Range(0, 8)] private int alphaLevels = 4;
    [SerializeField] private bool useDither = true;
    [SerializeField, Range(16, 128)] private int damageResolution = 48;

    [Header("Game Over")]
    [SerializeField] private RawImage redVignette;
    [SerializeField] private Color redColor = new Color(0.8f, 0.02f, 0.02f, 1f);
    [SerializeField] private float redDuration = 1.2f;
    [Range(0f, 1f)] [SerializeField] private float redMaxIntensity = 0.9f;
    [SerializeField] private float gameOverClearDelay = 0.3f;

    [Header("Eyes")]
    [SerializeField] private float gameOverEyeWidth = 320f;
    [SerializeField] private float gameOverEyeDuration = 0.9f;
    [SerializeField] private float gameOverEyeFadeIn = 0.08f;
    [SerializeField] private float shakeAmplitude = 14f;
    [SerializeField] private float shakeRotation = 6f;
    [SerializeField] private bool keepEyeDuringDamage = false;

    [Header("Damage")]
    [Range(0.3f, 0.95f)] [SerializeField] private float damageInnerRadius = 0.6f;
    [Range(0f, 0.5f)] [SerializeField] private float damageNoise = 0.25f;
    [SerializeField] private float damageNoiseScale = 4f;
    [Range(1, 5)] [SerializeField] private int damagePulses = 3;
    [SerializeField] private float damagePunchScale = 1.25f;
    [SerializeField] private float damageBlackFadeIn = 0f;

    [Header("Eyes")]
    [SerializeField] private RectTransform eyesArea;
    [SerializeField] private Sprite[] eyeSprites;
    [Range(0f, 1f)] [SerializeField] private float eyesStartAt = 0.25f;
    [SerializeField] private Vector2 spawnInterval = new Vector2(0.15f, 1.2f);
    [SerializeField] private Vector2 flashDuration = new Vector2(0.15f, 0.5f);
    [SerializeField] private Vector2 eyeSize = new Vector2(80f, 160f);
    [Range(0f, 1f)] [SerializeField] private float eyeMaxAlpha = 0.9f;
    [SerializeField] private float maxRotation = 15f;
    [SerializeField] private int maxEyes = 8;

    private VignetteLayer black;
    private Texture2D damageTexture;
    private float builtAspect;

    private float progress;
    private bool active;

    private Coroutine spawner;
    private Coroutine fade;
    private Coroutine gameOverRoutine;

    private readonly Stack<Image> freeEyes = new Stack<Image>();
    private int activeEyes;
    private Image gameOverEye;

    private static readonly float[] Bayer4 =
    {
         0,  8,  2, 10,
        12,  4, 14,  6,
         3, 11,  1,  9,
        15,  7, 13,  5
    };

    private static byte Quantize(float a, int x, int y, int levels, bool dither)
    {
        a = Mathf.Clamp01(a);
        if (levels <= 0) return (byte)(a * 255f);

        float v = a * levels;
        if (dither)
        {
            float threshold = (Bayer4[(y & 3) * 4 + (x & 3)] + 0.5f) / 16f;
            v = Mathf.Floor(v + threshold);
        }
        else
        {
            v = Mathf.Round(v);
        }

        return (byte)(Mathf.Clamp01(v / levels) * 255f);
    }

    private class VignetteLayer
    {
        private readonly Texture2D texture;
        private readonly Color32[] pixels;
        private readonly int width;
        private readonly int height;
        private readonly float softness;
        private readonly Color32 color;
        private readonly int levels;
        private readonly bool dither;
        private float last = -1f;

        public float Last => last < 0f ? 0f : last;

        public VignetteLayer(RawImage image, int width, int height, float softness, Color color, int levels, bool dither)
        {
            this.width = width;
            this.height = height;
            this.softness = softness;
            this.color = color;
            this.levels = levels;
            this.dither = dither;

            texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            pixels = new Color32[width * height];

            image.raycastTarget = false;
            image.texture = texture;
            Render(0f);
        }

        public void Render(float t)
        {
            if (Mathf.Abs(t - last) < 0.002f && t > 0f && t < 1f) return;
            last = t;

            float inner = Mathf.Lerp(1f, -softness, t);

            for (int y = 0; y < height; y++)
            {
                float ny = (y + 0.5f) / height - 0.5f;

                for (int x = 0; x < width; x++)
                {
                    float nx = (x + 0.5f) / width - 0.5f;
                    float d = Mathf.Sqrt(nx * nx + ny * ny) / 0.7071f; // 0 no centro, 1 no canto
                    float a = Mathf.InverseLerp(inner, inner + softness, d);
                    pixels[y * width + x] = new Color32(color.r, color.g, color.b, Quantize(a, x, y, levels, dither));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false);
        }

        public void Destroy()
        {
            if (texture != null) UnityEngine.Object.Destroy(texture);
        }
    }

    private void Awake()
    {
        if (vignette == null || redVignette == null || eyesArea == null)
        {
            enabled = false;
            return;
        }

        BuildLayers();
    }

    private void OnDestroy()
    {
        black?.Destroy();
        if (damageTexture != null) Destroy(damageTexture);
    }

    private void OnDisable()
    {
        active = false;
    }

    private float GetAspect()
    {
        Rect r = vignette.rectTransform.rect;
        if (r.width > 1f && r.height > 1f) return r.width / r.height;

        if (Screen.height > 0) return (float)Screen.width / Screen.height;
        return 16f / 9f;
    }

    private static int WidthFor(int rows, float aspect)
    {
        return Mathf.Max(8, Mathf.RoundToInt(rows * aspect));
    }

    private void BuildLayers()
    {
        black?.Destroy();
        if (damageTexture != null) Destroy(damageTexture);

        float aspect = GetAspect();
        builtAspect = aspect;

        black = new VignetteLayer(
            vignette,
            WidthFor(vignetteResolution, aspect),
            vignetteResolution,
            vignetteSoftness,
            Color.black,
            alphaLevels,
            useDither);

        damageTexture = BuildDamageTexture(WidthFor(damageResolution, aspect), damageResolution);
        redVignette.raycastTarget = false;
        redVignette.texture = damageTexture;
        SetDamage(0f, 1f);
    }

    public void Begin()
    {
        if (Mathf.Abs(GetAspect() - builtAspect) > 0.01f)
            BuildLayers();

        if (fade != null) StopCoroutine(fade);
        if (gameOverRoutine != null) StopCoroutine(gameOverRoutine);
        gameOverRoutine = null;
        ReleaseGameOverEye();

        active = true;
        progress = 0f;

        black.Render(0f);
        SetDamage(0f, 1f);

        if (spawner != null) StopCoroutine(spawner);
        spawner = StartCoroutine(SpawnLoop());
    }

    public void SetProgress(float value)
    {
        if (gameOverRoutine != null) return;

        progress = Mathf.Clamp01(value);
        black.Render(vignetteCurve.Evaluate(progress));
    }

    public void End()
    {
        if (gameOverRoutine != null) return;

        active = false;

        if (spawner != null) StopCoroutine(spawner);
        spawner = null;

        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(FadeOut());
    }

    public void PlayGameOver(Action onGameOver)
    {
        if (fade != null) StopCoroutine(fade);
        if (gameOverRoutine != null) StopCoroutine(gameOverRoutine);

        gameOverRoutine = StartCoroutine(GameOverSequence(onGameOver));
    }

    public void PlayDamage(Action onDone)
    {
        if (damageTexture == null)
        {
            onDone?.Invoke();
            return;
        }

        if (fade != null) StopCoroutine(fade);
        fade = null;
        if (gameOverRoutine != null) StopCoroutine(gameOverRoutine);

        active = false;
        if (spawner != null) StopCoroutine(spawner);
        spawner = null;

        ReleaseGameOverEye();

        gameOverRoutine = StartCoroutine(DamageSequence(onDone));
    }

    private IEnumerator FadeOut()
    {
        float start = black.Last;
        float elapsed = 0f;

        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            black.Render(Mathf.Lerp(start, 0f, elapsed / fadeOutTime));
            yield return null;
        }

        black.Render(0f);
        fade = null;
    }

    private IEnumerator GameOverSequence(Action onGameOver)
    {
        active = false;
        if (spawner != null) StopCoroutine(spawner);
        spawner = null;

        ReleaseGameOverEye();
        black.Render(1f);

        if (eyeSprites != null && eyeSprites.Length > 0)
        {
            gameOverEye = ShowGameOverEye();
            RectTransform rt = (RectTransform)gameOverEye.transform;

            float e = 0f;
            while (e < gameOverEyeDuration)
            {
                e += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(e / gameOverEyeDuration);

                float intensity = Mathf.Lerp(0.4f, 1f, t);

                rt.anchoredPosition = UnityEngine.Random.insideUnitCircle * shakeAmplitude * intensity;
                rt.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-1f, 1f) * shakeRotation * intensity);

                float alpha = gameOverEyeFadeIn > 0f ? Mathf.Clamp01(e / gameOverEyeFadeIn) : 1f;
                SetAlpha(gameOverEye, alpha);

                yield return null;
            }

            if (!keepEyeDuringDamage)
                ReleaseGameOverEye();
        }

        yield return RedPulses();

        onGameOver?.Invoke();

        if (gameOverClearDelay > 0f)
            yield return new WaitForSecondsRealtime(gameOverClearDelay);

        ReleaseGameOverEye();
        black.Render(0f);
        SetDamage(0f, 1f);

        gameOverRoutine = null;
    }

    private IEnumerator DamageSequence(Action onDone)
    {
        if (damageBlackFadeIn > 0f)
        {
            float start = black.Last;
            float e = 0f;
            while (e < damageBlackFadeIn)
            {
                e += Time.unscaledDeltaTime;
                black.Render(Mathf.Lerp(start, 1f, Mathf.Clamp01(e / damageBlackFadeIn)));
                yield return null;
            }
        }
        black.Render(1f);

        yield return RedPulses();

        onDone?.Invoke();

        if (gameOverClearDelay > 0f)
            yield return new WaitForSecondsRealtime(gameOverClearDelay);

        black.Render(0f);
        SetDamage(0f, 1f);

        gameOverRoutine = null;
    }

    private IEnumerator RedPulses()
    {
        float pulseTime = redDuration / damagePulses;

        for (int p = 0; p < damagePulses; p++)
        {
            float k = damagePulses > 1 ? p / (float)(damagePulses - 1) : 1f;
            float peak = Mathf.Lerp(0.45f, 1f, k) * redMaxIntensity;

            float e = 0f;
            while (e < pulseTime)
            {
                e += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(e / pulseTime);

                float env = t < 0.2f ? t / 0.2f : Mathf.Lerp(1f, 0.55f, (t - 0.2f) / 0.8f);
                float scale = Mathf.Lerp(damagePunchScale, 1f, Mathf.Clamp01(t / 0.35f));

                SetDamage(peak * env, scale);
                yield return null;
            }
        }

        SetDamage(redMaxIntensity, 1f);
    }

    private Image ShowGameOverEye()
    {
        Image eye = GetEye();
        Sprite sprite = eyeSprites[UnityEngine.Random.Range(0, eyeSprites.Length)];

        eye.sprite = sprite;
        eye.preserveAspect = true;

        float w = gameOverEyeWidth;
        float h = w * (sprite.rect.height / sprite.rect.width);

        RectTransform rt = (RectTransform)eye.transform;
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.identity;
        rt.SetAsLastSibling();

        SetAlpha(eye, 0f);
        eye.gameObject.SetActive(true);
        return eye;
    }

    private void ReleaseGameOverEye()
    {
        if (gameOverEye == null) return;

        gameOverEye.gameObject.SetActive(false);
        freeEyes.Push(gameOverEye);
        gameOverEye = null;
    }

    private Texture2D BuildDamageTexture(int width, int height)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        var px = new Color32[width * height];
        var c = (Color32)redColor;
        float seed = UnityEngine.Random.Range(0f, 100f);

        for (int y = 0; y < height; y++)
        {
            float ny = (y + 0.5f) / height - 0.5f;
            for (int x = 0; x < width; x++)
            {
                float nx = (x + 0.5f) / width - 0.5f;
                float d = Mathf.Sqrt(nx * nx + ny * ny) / 0.7071f;

                float n = Mathf.PerlinNoise(nx * damageNoiseScale + seed, ny * damageNoiseScale + seed) - 0.5f;
                float inner = damageInnerRadius + n * damageNoise;

                float a = Mathf.Clamp01(Mathf.InverseLerp(inner, 1f, d));
                a *= a;

                px[y * width + x] = new Color32(c.r, c.g, c.b, Quantize(a, x, y, alphaLevels, useDither));
            }
        }

        tex.SetPixels32(px);
        tex.Apply(false);
        return tex;
    }

    private void SetDamage(float alpha, float scale)
    {
        redVignette.color = new Color(1f, 1f, 1f, alpha);
        redVignette.rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (progress >= eyesStartAt && activeEyes < maxEyes)
                StartCoroutine(Flash());

            float k = Mathf.InverseLerp(eyesStartAt, 1f, progress);
            float interval = Mathf.Lerp(spawnInterval.y, spawnInterval.x, k) * UnityEngine.Random.Range(0.6f, 1.4f);
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator Flash()
    {
        Image eye = GetEye();
        activeEyes++;

        float w = UnityEngine.Random.Range(eyeSize.x, eyeSize.y);
        Sprite sprite = eyeSprites != null && eyeSprites.Length > 0
            ? eyeSprites[UnityEngine.Random.Range(0, eyeSprites.Length)]
            : null;

        eye.sprite = sprite;
        eye.preserveAspect = sprite != null;

        float h = sprite != null ? w * (sprite.rect.height / sprite.rect.width) : w;
        RectTransform rt = (RectTransform)eye.transform;
        rt.sizeDelta = new Vector2(w, h);

        Rect area = eyesArea.rect;
        rt.anchoredPosition = new Vector2(
            UnityEngine.Random.Range(area.xMin + w * 0.5f, area.xMax - w * 0.5f),
            UnityEngine.Random.Range(area.yMin + h * 0.5f, area.yMax - h * 0.5f));
        rt.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-maxRotation, maxRotation));

        eye.gameObject.SetActive(true);

        float duration = UnityEngine.Random.Range(flashDuration.x, flashDuration.y);
        float elapsed = 0f;

        while (elapsed < duration && active)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float a = t < 0.15f ? t / 0.15f : 1f - (t - 0.15f) / 0.85f;
            SetAlpha(eye, a * eyeMaxAlpha);
            yield return null;
        }

        eye.gameObject.SetActive(false);
        freeEyes.Push(eye);
        activeEyes--;
    }

    private Image GetEye()
    {
        if (freeEyes.Count > 0) return freeEyes.Pop();

        var go = new GameObject("Eye", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(eyesArea, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    private static void SetAlpha(Image img, float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }
}