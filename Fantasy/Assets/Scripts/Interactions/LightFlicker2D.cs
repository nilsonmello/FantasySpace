using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightFlicker2D : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Light2D light2D;

    [Header("Interval")]
    [SerializeField] private float minInterval = 0.1f;
    [SerializeField] private float maxInterval = 1.5f;

    [Header("Duration")]
    [SerializeField] private float minOffTime = 0.03f;
    [SerializeField] private float maxOffTime = 0.15f;

    private float originalIntensity;

    private void Start()
    {
        if (light2D == null)
            light2D = GetComponent<Light2D>();

        originalIntensity = light2D.intensity;

        StartCoroutine(Flicker());
    }

    private IEnumerator Flicker()
    {
        while (true)
        {
            float interval = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(interval);

            light2D.intensity = 0f;

            float offTime = Random.Range(minOffTime, maxOffTime);
            yield return new WaitForSeconds(offTime);

            light2D.intensity = originalIntensity;
        }
    }
}