using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour
{
    [Header("Texto de porcentagem")]
    [SerializeField] private TMP_Text percentageText;

    [Header("Aumento pelas setas")]
    [SerializeField] private float step = 0.1f;

    private Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    void Start()
    {
        slider.onValueChanged.AddListener(UpdatePercentageText);
        UpdatePercentageText(slider.value);
    }

    void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(UpdatePercentageText);
    }

    private void UpdatePercentageText(float value)
    {
        if (percentageText == null) return;
        int percent = Mathf.RoundToInt(value * 100f);
        percentageText.text = $"{percent}%";
    }

    public void Decrease()
    {
        slider.value = Mathf.Clamp(slider.value - step, slider.minValue, slider.maxValue);
    }

    public void Increase()
    {
        slider.value = Mathf.Clamp(slider.value + step, slider.minValue, slider.maxValue);
    }
}