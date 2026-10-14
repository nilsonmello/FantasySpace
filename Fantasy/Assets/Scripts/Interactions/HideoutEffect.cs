using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HideoutEffects : MonoBehaviour
{
    [Header("Vignette")]
    [SerializeField] private RawImage vignette;
    [SerializeField, Range(16, 256)] private int vignetteResolution = 96;
    [SerializeField, Range(0.1f, 2f)] private float vignetteSoftness = 0.6f;
    [SerializeField] private AnimationCurve vignetteCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float fadeOutTime = 0.4f;

    [Header("Game Over")]
    [SerializeField] private RawImage redVignette;
    [SerializeField] private Color redColor = new Color(0.8f, 0.02f, 0.02f, 1f);
    [SerializeField] private float redDuration = 1.2f;
    [Range(0f, 1f)] [SerializeField] private float redMaxIntensity = 0.9f;
    [SerializeField] private Image flashImage;
    [SerializeField] private float flashRiseTime = 0.05f;
    [SerializeField] private float flashHold = 0.06f;
    [SerializeField] private float flashFadeTime = 0.3f;

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
    private VignetteLayer red;

    private float progress;
    private bool active;

    private Coroutine spawner;
    private Coroutine fade;
    private Coroutine gameOverRoutine;

    private readonly Stack<Image> freeEyes = new Stack<Image>();
    private int activeEyes;

    private class VignetteLayer
    {
        private readonly Texture2D texture;
        private readonly Color32[] pixels;
        private readonly int res;
        private readonly float softness;
        private readonly Color32 color;
        private float last = -1f;

        public float Last => last < 0f ? 0f : last;

        public VignetteLayer(RawImage image, int res, float softness, Color color)
        {
            this.res = res;
            this.softness = softness;
            this.color = color;

            texture = new Texture2D(res, res, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            pixels = new Color32[res * res];

            image.raycastTarget = false;
            image.texture = texture;
            Render(0f);
        }

        public void Render(float t)
        {
            if (Mathf.Abs(t - last) < 0.002f && t > 0f && t < 1f) return;
            last = t;

            float inner = Mathf.Lerp(1f, -softness, t);

            for (int y = 0; y < res; y++)
            {
                float ny = (y + 0.5f) / res - 0.5f;

                for (int x = 0; x < res; x++)
                {
                    float nx = (x + 0.5f) / res - 0.5f;
                    float d = Mathf.Sqrt(nx * nx + ny * ny) / 0.7071f; // 0 no centro, 1 no canto
                    float a = Mathf.InverseLerp(inner, inner + softness, d);
                    pixels[y * res + x] = new Color32(color.r, color.g, color.b, (byte)(a * 255f));
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
        if (vignette == null || redVignette == null || flashImage == null || eyesArea == null)
        {
            enabled = false;
            return;
        }

        black = new VignetteLayer(vignette, vignetteResolution, vignetteSoftness, Color.black);
        red = new VignetteLayer(redVignette, vignetteResolution, vignetteSoftness, redColor);

        flashImage.raycastTarget = false;
        SetFlash(0f);
    }

    private void OnDestroy()
    {
        black?.Destroy();
        red?.Destroy();
    }

    private void OnDisable()
    {
        active = false;
    }

    public void Begin()
    {
        if (fade != null) StopCoroutine(fade);
        if (gameOverRoutine != null) StopCoroutine(gameOverRoutine);
        gameOverRoutine = null;

        active = true;
        progress = 0f;

        black.Render(0f);
        red.Render(0f);
        SetFlash(0f);

        if (spawner != null) StopCoroutine(spawner);
        spawner = StartCoroutine(SpawnLoop());
    }

    // 0 a 1
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
        black.Render(1f);

        float elapsed = 0f;
        while (elapsed < redDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / redDuration);
            red.Render(vignetteCurve.Evaluate(k) * redMaxIntensity);
            yield return null;
        }
        red.Render(redMaxIntensity);

        elapsed = 0f;
        while (elapsed < flashRiseTime)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFlash(Mathf.Clamp01(elapsed / flashRiseTime));
            yield return null;
        }
        SetFlash(1f);

        active = false;
        if (spawner != null) StopCoroutine(spawner);
        spawner = null;

        black.Render(0f);
        red.Render(0f);

        onGameOver?.Invoke();

        yield return new WaitForSecondsRealtime(flashHold);

        elapsed = 0f;
        while (elapsed < flashFadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            SetFlash(1f - Mathf.Clamp01(elapsed / flashFadeTime));
            yield return null;
        }
        SetFlash(0f);

        gameOverRoutine = null;
    }

    private void SetFlash(float a)
    {
        flashImage.color = new Color(1f, 1f, 1f, a);
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