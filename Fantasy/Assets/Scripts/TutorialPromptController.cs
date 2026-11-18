using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TutorialPromptController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private bool fadeOutOnExit = true;
    [SerializeField] private bool triggerOnlyOnce = false;

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    private Coroutine fadeCoroutine;
    private bool hasTriggered = false;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        SetAlpha(0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (triggerOnlyOnce && hasTriggered) return;

        hasTriggered = true;
        StartFade(1f);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!fadeOutOnExit) return;
        if (!other.CompareTag(playerTag)) return;

        StartFade(0f);
    }

    private void StartFade(float targetAlpha)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = GetAlpha();
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }

    private float GetAlpha()
    {
        if (spriteRenderer != null) return spriteRenderer.color.a;
        if (canvasGroup != null) return canvasGroup.alpha;
        return 0f;
    }
}