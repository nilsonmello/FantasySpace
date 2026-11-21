using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
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
    [Range(0f, 1f)] [SerializeField] private float redMaxIntensity = 0.9f;
    [SerializeField] private float blackHoldTime = 0.6f;
    [SerializeField] private float gameOverClearDelay = 0.3f;

    [Header("Game Over Eye")]
    [SerializeField] private float gameOverEyeWidth = 320f;
    [SerializeField] private float gameOverEyeDuration = 0.9f;
    [SerializeField] private float gameOverEyeFadeIn = 0.08f;
    [SerializeField] private float gameOverEyeFadeOut = 0.1f;
    [SerializeField] private float shakeAmplitude = 14f;
    [SerializeField] private float shakeRotation = 6f;
    [SerializeField, FormerlySerializedAs("keepEyeDuringDamage")] private bool keepEyeDuringGlitch = false;

    [Header("Glitch")]
    [SerializeField] private RawImage glitchImage;
    [SerializeField] private bool glitchOnDamage = true;
    [SerializeField] private float glitchDuration = 0.5f;
    [SerializeField] private float glitchStepTime = 0.045f;
    [SerializeField, Range(24, 180)] private int glitchResolution = 90;
    [SerializeField] private AnimationCurve glitchCurve = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(0.3f, 0.45f), new Keyframe(1f, 1f));
    [SerializeField] private float glitchShake = 40f;
    [SerializeField] private Color glitchRed = new Color(0.9f, 0.05f, 0.05f, 1f);
    [SerializeField] private Color glitchBrightRed = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private Color glitchDarkRed = new Color(0.25f, 0f, 0f, 1f);
    [SerializeField] private float glitchFinalFlash = 0.07f;

    [Header("Pixel Damage")]
    [SerializeField, Range(0f, 0.5f)] private float damageCoreRadius = 0.04f;
    [SerializeField, Range(0.2f, 1.2f)] private float damageReach = 0.62f;
    [SerializeField, Range(0.5f, 3f)] private float damageFalloffPower = 1.5f;
    [SerializeField, Range(0f, 1f)] private float damageIrregularity = 0.2f;
    [SerializeField, Range(0f, 0.5f)] private float damageNoise = 0.1f;
    [SerializeField] private float damageNoiseScale = 4f;
    [SerializeField, Range(0.3f, 2f)] private float damageLobeScale = 0.9f;
    [SerializeField, Range(0f, 0.3f)] private float damageCenterOffset = 0.04f;
    [SerializeField, Range(0f, 0.4f)] private float damageStretch = 0.1f;
    [SerializeField] private float damagePunchScale = 1.25f;
    [SerializeField] private float damageBlackFadeIn = 0f;

    [Header("Red pixel")]
    [SerializeField, Range(0f, 1.2f)] private float damageGrain = 0.55f;
    [SerializeField, Range(2, 16)] private int damageLevels = 8;
    [SerializeField, Range(4, 60)] private float damageStepRate = 15f;
    [SerializeField, Range(0f, 1f)] private float damageFlickerIdle = 0.12f;
    [SerializeField, Range(0f, 1f)] private float damageFlickerBeat = 0.8f;

    [Header("Pulsing movement)")]
    [SerializeField, Range(1, 6)] private int heartbeatCount = 3;
    [SerializeField] private float heartbeatPeriod = 0.6f;
    [SerializeField, Range(0f, 1f)] private float heartbeatDepth = 0.15f;
    [SerializeField, Range(0f, 0.5f)] private float heartbeatScale = 0.15f;
    [SerializeField, Range(0f, 1f)] private float heartbeatWeaken = 0.6f;
    [SerializeField] private float redFadeOutTime = 0.9f;
    [SerializeField, Range(0.2f, 1f)] private float redFadeScale = 0.7f;

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
    private GlitchLayer glitch;
    private Texture2D damageTexture;
    private float builtAspect;

    private float[] shapeEff;
    private float[] grainBase;
    private Color32[] damagePixels;

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
                    float d = Mathf.Sqrt(nx * nx + ny * ny) / 0.7071f;
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

    private class GlitchLayer
    {
        private readonly Texture2D texture;
        private readonly Color32[] pixels;
        private readonly int width;
        private readonly int height;
        private readonly Color32[] palette;
        private readonly Color32 flashColor;

        public GlitchLayer(RawImage image, int width, int height, Color red, Color brightRed, Color darkRed)
        {
            this.width = width;
            this.height = height;

            palette = new Color32[]
            {
                new Color32(0, 0, 0, 255),
                new Color32(0, 0, 0, 255),
                new Color32(0, 0, 0, 255),
                red, red, red,
                darkRed, darkRed,
                brightRed
            };
            flashColor = red;

            texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            pixels = new Color32[width * height];

            image.raycastTarget = false;
            image.texture = texture;
            Clear();
        }

        public void Clear()
        {
            Array.Clear(pixels, 0, pixels.Length);
            Apply();
        }

        public void FillSolid()
        {
            for (int i = 0; i < pixels.Length; i++) pixels[i] = flashColor;
            Apply();
        }

        public void Draw(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);
            Array.Clear(pixels, 0, pixels.Length);

            int blocks = Mathf.RoundToInt(Mathf.Lerp(4f, 70f, intensity));
            int maxW = Mathf.Max(2, Mathf.RoundToInt(Mathf.Lerp(6f, width * 0.35f, intensity)));
            int maxH = Mathf.Max(2, Mathf.RoundToInt(Mathf.Lerp(3f, 12f, intensity)));

            for (int i = 0; i < blocks; i++)
            {
                int bw = UnityEngine.Random.Range(1, maxW + 1);
                int bh = UnityEngine.Random.Range(1, maxH + 1);
                int bx = UnityEngine.Random.Range(-bw / 2, width);
                int by = UnityEngine.Random.Range(-bh / 2, height);
                FillRect(bx, by, bw, bh, RandomColor());
            }

            int slices = Mathf.RoundToInt(Mathf.Lerp(1f, 9f, intensity));
            for (int i = 0; i < slices; i++)
            {
                int sy = UnityEngine.Random.Range(0, height);
                int sh = UnityEngine.Random.Range(1, 5);
                int x = UnityEngine.Random.Range(0, width / 3);
                int endX = UnityEngine.Random.value < 0.5f ? width : UnityEngine.Random.Range(width / 2, width + 1);

                while (x < endX)
                {
                    int run = UnityEngine.Random.Range(2, Mathf.Max(4, width / 5));
                    if (UnityEngine.Random.value < 0.25f)
                    {
                        x += run;
                        continue;
                    }
                    FillRect(x, sy, run, sh, RandomColor());
                    x += run;
                }
            }

            if (intensity > 0.85f && UnityEngine.Random.value < 0.3f)
            {
                Color32 c = UnityEngine.Random.value < 0.5f ? new Color32(0, 0, 0, 255) : flashColor;
                c.a = 200;
                for (int i = 0; i < pixels.Length; i++)
                    if (pixels[i].a == 0) pixels[i] = c;
            }

            Apply();
        }

        private Color32 RandomColor()
        {
            return palette[UnityEngine.Random.Range(0, palette.Length)];
        }

        private void FillRect(int x, int y, int w, int h, Color32 c)
        {
            int x0 = Mathf.Max(0, x);
            int y0 = Mathf.Max(0, y);
            int x1 = Mathf.Min(width, x + w);
            int y1 = Mathf.Min(height, y + h);

            for (int yy = y0; yy < y1; yy++)
            {
                int row = yy * width;
                for (int xx = x0; xx < x1; xx++)
                    pixels[row + xx] = c;
            }
        }

        private void Apply()
        {
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
        glitch?.Destroy();
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

    private void EnsureGlitchImage()
    {
        if (glitchImage != null) return;

        var go = new GameObject("GlitchLayer", typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(redVignette.transform.parent, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        go.transform.SetSiblingIndex(redVignette.transform.GetSiblingIndex());

        glitchImage = go.GetComponent<RawImage>();
    }

    private void BuildLayers()
    {
        black?.Destroy();
        glitch?.Destroy();
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

        EnsureGlitchImage();
        glitch = new GlitchLayer(
            glitchImage,
            WidthFor(glitchResolution, aspect),
            glitchResolution,
            glitchRed,
            glitchBrightRed,
            glitchDarkRed);
        ResetGlitch();

        damageTexture = new Texture2D(WidthFor(damageResolution, aspect), damageResolution, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };
        BuildDamageShape();
        RenderDamage(1f, 0f);

        redVignette.raycastTarget = false;
        redVignette.texture = damageTexture;
        SetDamage(0f, 1f);
    }

    private void ResetGlitch()
    {
        if (glitchImage == null) return;

        glitch?.Clear();
        RectTransform rt = glitchImage.rectTransform;
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        glitchImage.gameObject.SetActive(false);
    }

    public void Begin()
    {
        if (Mathf.Abs(GetAspect() - builtAspect) > 0.01f)
            BuildLayers();

        if (fade != null) StopCoroutine(fade);
        if (gameOverRoutine != null) StopCoroutine(gameOverRoutine);
        gameOverRoutine = null;
        ReleaseGameOverEye();
        ResetGlitch();

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
        ResetGlitch();

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
        ResetGlitch();
        SetDamage(0f, 1f);
        BuildDamageShape();
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

            if (!keepEyeDuringGlitch)
                yield return HideGameOverEye();
        }

        yield return GlitchBurst();

        yield return HideGameOverEye();

        yield return RedSequence();

        if (blackHoldTime > 0f)
            yield return new WaitForSecondsRealtime(blackHoldTime);

        onGameOver?.Invoke();

        if (gameOverClearDelay > 0f)
            yield return new WaitForSecondsRealtime(gameOverClearDelay);

        ReleaseGameOverEye();
        ResetGlitch();
        black.Render(0f);
        SetDamage(0f, 1f);

        gameOverRoutine = null;
    }

    private IEnumerator DamageSequence(Action onDone)
    {
        SetDamage(0f, 1f);
        BuildDamageShape();

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

        if (glitchOnDamage)
            yield return GlitchBurst();

        yield return RedSequence();

        onDone?.Invoke();

        if (gameOverClearDelay > 0f)
            yield return new WaitForSecondsRealtime(gameOverClearDelay);

        ResetGlitch();
        black.Render(0f);
        SetDamage(0f, 1f);

        gameOverRoutine = null;
    }

    private IEnumerator GlitchBurst()
    {
        if (glitch == null || glitchImage == null || glitchDuration <= 0f)
            yield break;

        glitchImage.gameObject.SetActive(true);
        RectTransform rt = glitchImage.rectTransform;

        float e = 0f;
        float nextStep = 0f;

        while (e < glitchDuration)
        {
            e += Time.unscaledDeltaTime;

            if (e >= nextStep)
            {
                nextStep = e + glitchStepTime * UnityEngine.Random.Range(0.6f, 1.4f);

                float t = Mathf.Clamp01(e / glitchDuration);
                float spike = UnityEngine.Random.value < 0.25f ? 0.3f : 0f;
                float intensity = Mathf.Clamp01(glitchCurve.Evaluate(t) + spike);

                glitch.Draw(intensity);

                rt.anchoredPosition = UnityEngine.Random.insideUnitCircle * glitchShake * intensity;
                float s = 1f + UnityEngine.Random.Range(-0.03f, 0.05f) * intensity;
                rt.localScale = new Vector3(s, s, 1f);

                if (gameOverEye != null)
                    ShakeEye(gameOverEye, 1f);
            }

            yield return null;
        }

        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        glitch.FillSolid();

        if (glitchFinalFlash > 0f)
            yield return new WaitForSecondsRealtime(glitchFinalFlash);

        ResetGlitch();
    }

    private IEnumerator RedSequence()
    {
        float period = Mathf.Max(0.05f, heartbeatPeriod);
        float pulseTime = heartbeatCount * period;
        float fadeTime = Mathf.Max(0.01f, redFadeOutTime);
        float total = pulseTime + fadeTime;

        float stepInterval = 1f / Mathf.Max(1f, damageStepRate);
        float stepTimer = stepInterval;

        float e = 0f;
        float lastAlpha = 0f;
        float lastReach = 1f;

        while (e < total)
        {
            float dt = Time.unscaledDeltaTime;
            e += dt;

            float alpha;
            float reachMul;
            float flicker;

            if (e < pulseTime)
            {
                float k = e / pulseTime;
                float beat = Beat((e / period) % 1f);

                float level = Mathf.Lerp(1f, heartbeatWeaken, k);
                float intro = Mathf.Clamp01(e / 0.06f);

                alpha = redMaxIntensity * level * Mathf.Lerp(1f - heartbeatDepth, 1f, beat) * intro;

                float punch = (damagePunchScale - 1f) * Mathf.Exp(-e / 0.12f);
                reachMul = 1f + punch + heartbeatScale * beat * level;

                flicker = Mathf.Lerp(damageFlickerIdle, damageFlickerBeat, beat);

                lastAlpha = alpha;
                lastReach = reachMul;
            }
            else
            {
                float k = Mathf.Clamp01((e - pulseTime) / fadeTime);
                float s = k * k * (3f - 2f * k);

                alpha = lastAlpha * (1f - s);
                reachMul = Mathf.Lerp(lastReach, redFadeScale, s);
                flicker = damageFlickerIdle;
            }

            stepTimer += dt;
            if (stepTimer >= stepInterval)
            {
                stepTimer = 0f;
                RenderDamage(reachMul, flicker);
            }

            SetDamage(alpha, 1f);
            yield return null;
        }

        SetDamage(0f, 1f);
    }

    private static float Beat(float p)
    {
        float a = Mathf.Exp(-Mathf.Pow((p - 0.08f) / 0.05f, 2f));
        float b = 0.65f * Mathf.Exp(-Mathf.Pow((p - 0.30f) / 0.06f, 2f));
        return Mathf.Max(a, b);
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

    private void ShakeEye(Image eye, float intensity)
    {
        RectTransform rt = (RectTransform)eye.transform;
        rt.anchoredPosition = UnityEngine.Random.insideUnitCircle * shakeAmplitude * intensity;
        rt.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-1f, 1f) * shakeRotation * intensity);
    }

    private IEnumerator HideGameOverEye()
    {
        if (gameOverEye == null) yield break;

        Image eye = gameOverEye;

        if (gameOverEyeFadeOut > 0f)
        {
            float startAlpha = eye.color.a;
            float e = 0f;

            while (e < gameOverEyeFadeOut)
            {
                e += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(e / gameOverEyeFadeOut);

                SetAlpha(eye, Mathf.Lerp(startAlpha, 0f, k));
                ShakeEye(eye, 1f);

                yield return null;
            }
        }

        ReleaseGameOverEye();
    }

    private void ReleaseGameOverEye()
    {
        if (gameOverEye == null) return;

        gameOverEye.gameObject.SetActive(false);
        freeEyes.Push(gameOverEye);
        gameOverEye = null;
    }

    private void BuildDamageShape()
    {
        if (damageTexture == null) return;

        int width = damageTexture.width;
        int height = damageTexture.height;
        int count = width * height;

        if (shapeEff == null || shapeEff.Length != count)
        {
            shapeEff = new float[count];
            grainBase = new float[count];
            damagePixels = new Color32[count];
        }

        float seed = UnityEngine.Random.Range(0f, 100f);

        Vector2 center = new Vector2(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f)) * damageCenterOffset;

        float stretch = UnityEngine.Random.Range(1f - damageStretch, 1f + damageStretch);
        float tilt = UnityEngine.Random.Range(0f, Mathf.PI);
        float cosT = Mathf.Cos(tilt);
        float sinT = Mathf.Sin(tilt);

        for (int y = 0; y < height; y++)
        {
            float ny = (y + 0.5f) / height - 0.5f;

            for (int x = 0; x < width; x++)
            {
                float nx = (x + 0.5f) / width - 0.5f;
                int i = y * width + x;

                float dx = nx - center.x;
                float dy = ny - center.y;

                float rx = (dx * cosT + dy * sinT) * stretch;
                float ry = (-dx * sinT + dy * cosT) / stretch;

                float d = Mathf.Sqrt(rx * rx + ry * ry) / 0.7071f;
                float angle = Mathf.Atan2(ry, rx);

                float lobes = Mathf.PerlinNoise(
                    Mathf.Cos(angle) * damageLobeScale + seed,
                    Mathf.Sin(angle) * damageLobeScale + seed * 0.7f) - 0.5f;

                float blotch = Mathf.PerlinNoise(
                    nx * damageNoiseScale + seed,
                    ny * damageNoiseScale + seed) - 0.5f;

                float shape = Mathf.Max(0.35f, 1f + lobes * damageIrregularity * 2f + blotch * damageNoise * 2f);

                shapeEff[i] = d / shape;
                grainBase[i] = UnityEngine.Random.value;
            }
        }
    }

    private void RenderDamage(float reachMul, float flicker)
    {
        if (damageTexture == null || shapeEff == null) return;

        int width = damageTexture.width;
        int height = damageTexture.height;

        float core = damageCoreRadius;
        float reach = Mathf.Max(core + 0.05f, damageReach * reachMul);
        Color32 c = redColor;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = y * width + x;

                float t = 1f - Mathf.InverseLerp(core, reach, shapeEff[i]);
                float falloff = Mathf.Pow(Mathf.Clamp01(t), damageFalloffPower);

                float n = Mathf.Lerp(grainBase[i], UnityEngine.Random.value, flicker);
                float a = falloff + (n - 0.5f) * damageGrain;

                damagePixels[i] = new Color32(c.r, c.g, c.b, Quantize(a, x, y, damageLevels, false));
            }
        }

        damageTexture.SetPixels32(damagePixels);
        damageTexture.Apply(false);
    }

    private void SetDamage(float alpha, float scale)
    {
        redVignette.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
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