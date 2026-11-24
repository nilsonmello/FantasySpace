using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class HideoutDoorOverlay : MonoBehaviour
{
    public enum Orientation
    {
        Horizontal,
        Vertical
    }

    [Header("Door")]
    [SerializeField] private Orientation orientation = Orientation.Horizontal;
    [SerializeField, Range(2, 24)] private int slitCount = 9;
    [SerializeField, Range(0.05f, 0.9f)] private float slitFraction = 0.3f;
    [SerializeField, Range(4, 64)] private int cellPixels = 16;

    [Header("Look")]
    [SerializeField] private Color slatColor = new Color(0.02f, 0.015f, 0.015f, 1f);
    [SerializeField, Range(0f, 1f)] private float slatOpacity = 0.92f;

    [Header("Fade")]
    [SerializeField, Min(0f)] private float fadeInTime = 0.25f;
    [SerializeField, Min(0f)] private float fadeOutTime = 0.2f;

    private RawImage image;
    private Texture2D texture;
    private Coroutine routine;
    private float alpha;

    private void Awake()
    {
        image = GetComponent<RawImage>();
        image.raycastTarget = false;

        Build();
        SetAlpha(0f);
    }

    private void OnDestroy()
    {
        if (texture != null) Destroy(texture);
    }

    public void Show()
    {
        Fade(1f, fadeInTime);
    }

    public void Hide()
    {
        Fade(0f, fadeOutTime);
    }

    public void HideImmediate()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;

        SetAlpha(0f);
    }

    private void Build()
    {
        if (texture != null) Destroy(texture);

        bool horizontal = orientation == Orientation.Horizontal;
        int length = slitCount * cellPixels;
        int slitPixels = Mathf.Clamp(Mathf.RoundToInt(cellPixels * slitFraction), 1, cellPixels - 1);
        float halfSlit = slitPixels * 0.5f;
        float center = length * 0.5f;

        texture = new Texture2D(horizontal ? 1 : length, horizontal ? length : 1, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };

        Color32 slat = slatColor;
        slat.a = 255;
        Color32 clear = new Color32(slat.r, slat.g, slat.b, 0);

        var pixels = new Color32[length];
        for (int i = 0; i < length; i++)
        {
            float dist = Mathf.Abs(i + 0.5f - center);

            float r = dist % cellPixels;
            r = Mathf.Min(r, cellPixels - r);

            bool inSlit = r < halfSlit;
            pixels[i] = inSlit ? clear : slat;
        }

        texture.SetPixels32(pixels);
        texture.Apply(false);

        image.texture = texture;
    }

    private void Fade(float target, float time)
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;

        if (time <= 0f || !isActiveAndEnabled)
        {
            SetAlpha(target);
            return;
        }

        routine = StartCoroutine(FadeRoutine(target, time));
    }

    private IEnumerator FadeRoutine(float target, float time)
    {
        float start = alpha;
        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(start, target, elapsed / time));
            yield return null;
        }

        SetAlpha(target);
        routine = null;
    }

    private void SetAlpha(float value)
    {
        alpha = value;
        image.color = new Color(1f, 1f, 1f, value * slatOpacity);
        image.enabled = value > 0.001f;
    }
}