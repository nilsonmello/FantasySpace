using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsGUI : MonoBehaviour
{
    [Header("Sliders")]
    public Slider masterVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider musicVolumeSlider;

    private void OnEnable()
    {
        masterVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.GetSavedMasterVolume());
        sfxVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.GetSavedSfxVolume());
        musicVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.GetSavedMusicVolume());

        masterVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetMasterVolume);
        sfxVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetSfxVolume);
        musicVolumeSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
    }

    private void OnDisable()
    {
        masterVolumeSlider.onValueChanged.RemoveListener(AudioManager.Instance.SetMasterVolume);
        sfxVolumeSlider.onValueChanged.RemoveListener(AudioManager.Instance.SetSfxVolume);
        musicVolumeSlider.onValueChanged.RemoveListener(AudioManager.Instance.SetMusicVolume);
    }

    public void ResetVolume()
    {
        masterVolumeSlider.value = 1.0f;
        sfxVolumeSlider.value = 1.0f;
        musicVolumeSlider.value = 1.0f;
    }
}