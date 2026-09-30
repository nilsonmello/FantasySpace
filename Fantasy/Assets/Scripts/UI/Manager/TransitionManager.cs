using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [Header("Setup")]
    public Image transitionImage;
    public Material transitionMaterial;

    [Header("Durações")]
    public float panelTransitionDuration = 0.3f;
    public float sceneTransitionDuration = 0.6f;

    private static readonly int FadeAmountId = Shader.PropertyToID("_FadeAmount");
    private static readonly int UseShuttersId = Shader.PropertyToID("_UseShuters");
    private static readonly int UseRadialWipeId = Shader.PropertyToID("_UseRadialWipe");
    private static readonly int UseGoopId = Shader.PropertyToID("_UseGoop");
    private static readonly int UsePlainBlackId = Shader.PropertyToID("_UsePlainBlack");

    private Coroutine activeRoutine;

    private enum TransitionType { PlainBlack, Goop, Shutters, RadialWipe }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            //Destroy(gameObject);
            return;
        }

        Instance = this;
        transitionImage.enabled = false;
    }

    private void SetTransitionType(TransitionType type)
    {
        transitionMaterial.SetFloat(UseShuttersId, type == TransitionType.Shutters ? 1f : 0f);
        transitionMaterial.SetFloat(UseRadialWipeId, type == TransitionType.RadialWipe ? 1f : 0f);
        transitionMaterial.SetFloat(UseGoopId, type == TransitionType.Goop ? 1f : 0f);
        transitionMaterial.SetFloat(UsePlainBlackId, type == TransitionType.PlainBlack ? 1f : 0f);
    }

    private void SetFade(float value)
    {
        transitionMaterial.SetFloat(FadeAmountId, value);
    }

    public void PlayPanelTransition(Action onMidTransition)
    {
        Play(TransitionType.PlainBlack, panelTransitionDuration, onMidTransition);
    }

    public void PlayPanelTransition(Action onMidTransition, float customDuration, float holdDuration = 0f)
    {
        Play(TransitionType.PlainBlack, customDuration, onMidTransition, holdDuration);
    }

    public void PlaySceneTransition(Action onMidTransition)
    {
        Play(TransitionType.PlainBlack, sceneTransitionDuration, onMidTransition);
    }

    public void PlaySceneTransition(Action onMidTransition, float holdDuration)
    {
        Play(TransitionType.PlainBlack, sceneTransitionDuration, onMidTransition, holdDuration);
    }

    private void Play(TransitionType type, float duration, Action onMidTransition, float holdDuration = 0f)
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(TransitionRoutine(type, duration, onMidTransition, holdDuration));
    }

    private IEnumerator TransitionRoutine(TransitionType type, float duration, Action onMidTransition, float holdDuration)
    {
        var uiMap = UIManager.Instance.inputActions.FindActionMap("UI");
        if (uiMap != null) uiMap.Disable();

        SetTransitionType(type);
        transitionImage.enabled = true;

        float half = duration * 0.5f;
        yield return FadeRoutine(0f, 1f, half);
        onMidTransition?.Invoke();

        if (holdDuration > 0f) yield return new WaitForSecondsRealtime(holdDuration);
        else yield return null;

        yield return FadeRoutine(1f, 0f, half);
        transitionImage.enabled = false;
        activeRoutine = null;

        if (uiMap != null) uiMap.Enable();
    }

    public void PlayPanelTransitionAsync(Action<Action> onMidTransitionReady, float customDuration, float maxHoldDuration)
    {
        PlayAsync(TransitionType.PlainBlack, customDuration, onMidTransitionReady, maxHoldDuration);
    }

    private void PlayAsync(TransitionType type, float duration, Action<Action> onMidTransitionReady, float maxHoldDuration)
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(TransitionRoutineAsync(type, duration, onMidTransitionReady, maxHoldDuration));
    }

    private IEnumerator TransitionRoutineAsync(TransitionType type, float duration, Action<Action> onMidTransitionReady, float maxHoldDuration)
    {
        var uiMap = UIManager.Instance.inputActions.FindActionMap("UI");
        if (uiMap != null) uiMap.Disable();

        SetTransitionType(type);
        transitionImage.enabled = true;
        float half = duration * 0.5f;
        yield return FadeRoutine(0f, 1f, half);

        bool ready = false;
        onMidTransitionReady?.Invoke(() => ready = true);

        float elapsed = 0f;
        while (!ready && elapsed < maxHoldDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return FadeRoutine(1f, 0f, half);
        transitionImage.enabled = false;
        activeRoutine = null;

        if (uiMap != null) uiMap.Enable();
    }

    private IEnumerator FadeRoutine(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            t += dt;
            float normalized = Mathf.Clamp01(t / duration);
            SetFade(Mathf.Lerp(from, to, normalized));
            yield return null;
        }
        SetFade(to);
    }
}