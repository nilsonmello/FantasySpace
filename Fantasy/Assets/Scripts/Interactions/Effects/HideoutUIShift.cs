using System;
using System.Collections;
using UnityEngine;

public class HideoutUIShift : MonoBehaviour
{
    [Serializable]
    private class Entry
    {
        public RectTransform target;
        public Vector2 offset = new Vector2(0f, -200f);

        [NonSerialized] public Vector2 origin;
    }

    [Header("Objects")]
    [SerializeField] private Entry[] entries;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveInTime = 0.3f;
    [SerializeField, Min(0f)] private float moveOutTime = 0.25f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine routine;
    private float amount;
    private bool captured;

    private void OnDisable()
    {
        HideImmediate();
    }

    public void Show()
    {
        if (amount <= 0f) CaptureOrigins();

        MoveTo(1f, moveInTime);
    }

    public void Hide()
    {
        if (!captured) return;

        MoveTo(0f, moveOutTime);
    }

    public void HideImmediate()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;

        amount = 0f;
        Apply();
    }

    private void CaptureOrigins()
    {
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].target != null)
                entries[i].origin = entries[i].target.anchoredPosition;
        }

        captured = true;
    }

    private void MoveTo(float target, float time)
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;

        if (time <= 0f || !isActiveAndEnabled)
        {
            amount = target;
            Apply();
            return;
        }

        routine = StartCoroutine(MoveRoutine(target, time));
    }

    private IEnumerator MoveRoutine(float target, float time)
    {
        float start = amount;
        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.unscaledDeltaTime;
            amount = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / time));
            Apply();
            yield return null;
        }

        amount = target;
        Apply();
        routine = null;
    }

    private void Apply()
    {
        if (!captured || entries == null) return;

        float k = curve.Evaluate(amount);

        for (int i = 0; i < entries.Length; i++)
        {
            Entry e = entries[i];
            if (e.target == null) continue;

            e.target.anchoredPosition = e.origin + e.offset * k;
        }
    }
}
