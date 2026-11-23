using System.Collections;
using UnityEngine;

public class SirenLightSignal : TrapSignal
{
    [Header("Light")]
    [SerializeField] private GameObject[] lightsA;
    [SerializeField] private GameObject[] lightsB;

    [Header("Blink")]
    [SerializeField] private float blinkInterval = 0.25f;
    [SerializeField] private bool alternate = true;

    private Coroutine routine;

    private void Awake()
    {
        SetAll(false);
    }

    public override void Begin()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Blink());
    }

    public override void End()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        SetAll(false);
    }

    private void OnDisable()
    {
        if (!gameObject.activeInHierarchy)
            End();
    }

    private IEnumerator Blink()
    {
        var wait = new WaitForSeconds(Mathf.Max(0.02f, blinkInterval));
        bool aOn = true;

        while (true)
        {
            SetGroup(lightsA, aOn);
            SetGroup(lightsB, alternate ? !aOn : aOn);
            aOn = !aOn;
            yield return wait;
        }
    }

    private void SetAll(bool value)
    {
        SetGroup(lightsA, value);
        SetGroup(lightsB, value);
    }

    private static void SetGroup(GameObject[] group, bool value)
    {
        if (group == null) return;

        for (int i = 0; i < group.Length; i++)
        {
            if (group[i] != null)
                group[i].SetActive(value);
        }
    }
}