using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : Singleton<AudioManager>
{
    [Header("Mixer")]
    public AudioMixer mainMixer;

    [Header("Parameter Names")]
    public string masterVolumeParam = "MasterVolume";
    public string sfxVolumeParam = "SFXVolume";
    public string musicVolumeParam = "MusicVolume";

    [Header("PlayerPrefs Keys")]
    private const string MASTER_VOLUME_KEY = "masterVolume";
    private const string SFX_VOLUME_KEY = "sfxVolume";
    private const string MUSIC_VOLUME_KEY = "musicVolume";

    [Header("Default Volume")]
    [Range(0f, 1f)] public float defaultMasterVolume = 1f;
    [Range(0f, 1f)] public float defaultSfxVolume = 1f;
    [Range(0f, 1f)] public float defaultMusicVolume = 1f;

    private void Start()
    {
        float savedMaster = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, defaultMasterVolume);
        float savedSfx = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, defaultSfxVolume);
        float savedMusic = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, defaultMusicVolume);

        SetMasterVolume(savedMaster);
        SetSfxVolume(savedSfx);
        SetMusicVolume(savedMusic);
    }

    public void SetMasterVolume(float linearVolume)
    {
        mainMixer.SetFloat(masterVolumeParam, LinearToDecibel(linearVolume));
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, linearVolume);
    }

    public void SetSfxVolume(float linearVolume)
    {
        mainMixer.SetFloat(sfxVolumeParam, LinearToDecibel(linearVolume));
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, linearVolume);
    }

    public void SetMusicVolume(float linearVolume)
    {
        mainMixer.SetFloat(musicVolumeParam, LinearToDecibel(linearVolume));
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, linearVolume);
    }

    public float GetSavedMasterVolume()
    {
        return PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, defaultMasterVolume);
    }

    public float GetSavedSfxVolume()
    {
        return PlayerPrefs.GetFloat(SFX_VOLUME_KEY, defaultSfxVolume);
    }

    public float GetSavedMusicVolume()
    {
        return PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, defaultMusicVolume);
    }

    private float LinearToDecibel(float linearVolume)
    {
        if (linearVolume <= 0.0001f)
        {
            return -80f;
        }

        return Mathf.Log10(linearVolume) * 20f;
    }
}