using System;
using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class HideoutEffectsTester : MonoBehaviour
{
    [SerializeField] private HideoutEffects effects;

    [SerializeField] private string gameOverKey = "G";
    [SerializeField] private string damageKey = "H";
    [SerializeField] private string hideToggleKey = "J";

    [SerializeField] private float hideDuration = 6f;

    [SerializeField] private bool showHint = true;

    private class Binding
    {
        public string name;
        public bool valid;
#if ENABLE_INPUT_SYSTEM
        public Key key;
#else
        public KeyCode key;
#endif
    }

    private Binding bGameOver;
    private Binding bDamage;
    private Binding bHide;

    private Coroutine hideRoutine;

    private void Awake()
    {
        if (effects == null)
        {
#if UNITY_2023_1_OR_NEWER
            effects = FindFirstObjectByType<HideoutEffects>();
#else
            effects = FindObjectOfType<HideoutEffects>();
#endif
        }

        bGameOver = MakeBinding(gameOverKey);
        bDamage = MakeBinding(damageKey);
        bHide = MakeBinding(hideToggleKey);
    }

    private Binding MakeBinding(string keyName)
    {
        var b = new Binding { name = keyName };
        b.valid = Enum.TryParse(keyName, true, out b.key);

        return b;
    }

    private bool Pressed(Binding b)
    {
        if (b == null || !b.valid) return false;

#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        return kb != null && kb[b.key].wasPressedThisFrame;
#else
        return Input.GetKeyDown(b.key);
#endif
    }

    private void Update()
    {
        if (effects == null) return;

        if (Pressed(bGameOver)) PlayGameOver();
        if (Pressed(bDamage)) PlayDamage();
        if (Pressed(bHide)) ToggleHide();
    }

    public void PlayGameOver()
    {
        if (effects == null) return;

        StopHideSimulation(false);
        effects.PlayGameOver(() => Debug.Log("onGameOver"));
    }

    public void PlayDamage()
    {
        if (effects == null) return;

        StopHideSimulation(false);
        effects.PlayDamage(() => Debug.Log("onDone"));
    }

    public void ToggleHide()
    {
        if (effects == null) return;

        if (hideRoutine != null)
        {
            StopHideSimulation(true);
            return;
        }

        hideRoutine = StartCoroutine(HideRoutine());
    }

    private IEnumerator HideRoutine()
    {
        effects.Begin();

        float t = 0f;
        while (t < hideDuration)
        {
            t += Time.deltaTime;
            effects.SetProgress(t / hideDuration);
            yield return null;
        }

        effects.SetProgress(1f);
    }

    private void StopHideSimulation(bool callEnd)
    {
        if (hideRoutine == null) return;

        StopCoroutine(hideRoutine);
        hideRoutine = null;

        if (callEnd) effects.End();
    }

    private void OnGUI()
    {
        if (!showHint || bGameOver == null) return;

        GUI.Label(new Rect(10, 10, 420, 70),
            $"[{bGameOver.name}] Game Over   [{bDamage.name}] Damage   [{bHide.name}] Esconderijo (liga/desliga)");
    }
}