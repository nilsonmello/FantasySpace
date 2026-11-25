using System;
using System.Collections;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource loopingSfxSource;
    public AudioSource uiSource;

    [System.Serializable] public class SoundClip
    {
        public string SoundName;
        public AudioClip audio;
    }

    [Header("Audio Clips")]
    public SoundClip[] MusicClips;
    public SoundClip[] SFXClips;
    public SoundClip[] UIClips;
    private AudioClip currentClip_SFX;
    private AudioClip currentClip_LoopingSFX;

    [Header("SFX Randomization")]
    [Range(0.1f, 2.0f)] public float minPitch = 0.9f;
    [Range(0.1f, 2.0f)] public float maxPitch = 1.1f;

    [Header("Pause")]
    public bool pauseMusicWhenPaused = false;
    [Range(0f, 1f)] public float pausedMusicVolume = 1f;
    public float musicDuckTime = 0.25f;

    [Header("Debug")]
    public bool warnOnMissing = true;
    private Coroutine pauseRoutine;
    private Coroutine duckRoutine;

    private bool musicDucked;
    private float musicVolumeBeforePause = 1f;

    private Coroutine musicFadeRoutine;
    private float musicVolumeBeforeFade = 1f;

    protected override void Awake()
    {
        base.Awake();

        if (uiSource != null) uiSource.ignoreListenerPause = true;

        if (musicSource != null) musicSource.ignoreListenerPause = !pauseMusicWhenPaused;
    }

    private SoundClip FindClip(SoundClip[] list, string name)
    {
        if (list == null) return null;
        return Array.Find(list, sound => sound.SoundName == name);
    }

    private void WarnMissing(string kind, string name)
    {
        if (warnOnMissing)
            Debug.LogWarning($"[SoundManager] {kind} '{name}' não encontrado.", this);
    }

    public void PlayMusic(string name)
    {
        CancelMusicFade();

        SoundClip s = FindClip(MusicClips, name);
        if (s == null)
        {
            WarnMissing("Música", name);
            return;
        }

        if (musicSource.clip == s.audio && musicSource.isPlaying) return;

        musicSource.clip = s.audio;
        musicSource.Play();
    }

    public void StopMusic()
    {
        CancelMusicFade();
        musicSource.Stop();
    }

    public void StopMusicWithFade(float fadeTime)
    {
        if (musicSource == null || !musicSource.isPlaying) return;

        if (musicFadeRoutine != null) StopCoroutine(musicFadeRoutine);

        if (fadeTime <= 0f)
        {
            StopMusic();
            return;
        }

        musicFadeRoutine = StartCoroutine(StopMusicRoutine(fadeTime));
    }

    private IEnumerator StopMusicRoutine(float fadeTime)
    {
        float start = musicSource.volume;
        musicVolumeBeforeFade = start;
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(start, 0f, t / fadeTime);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = start;
        musicFadeRoutine = null;
    }

    private void CancelMusicFade()
    {
        if (musicFadeRoutine == null) return;

        StopCoroutine(musicFadeRoutine);
        musicFadeRoutine = null;
        musicSource.volume = musicVolumeBeforeFade;
    }

    public IEnumerator FadeMusic(float targetVolume, float fadeDuration)
    {
        float timer = 0f;
        float startVolume = musicSource.volume;

        while (timer < fadeDuration)
        {
            float newVolume = Mathf.Lerp(startVolume, targetVolume, timer / fadeDuration);

            musicSource.volume = newVolume;

            timer += Time.deltaTime;

            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    public void PlaySFX(string name, float volume = 1.0f, bool randomPitch = false)
    {
        SoundClip s = FindClip(SFXClips, name);
        if (s == null)
        {
            WarnMissing("SFX", name);
            return;
        }

        if (randomPitch) sfxSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
        else sfxSource.pitch = 1.0f;
        currentClip_SFX = s.audio;
        sfxSource.PlayOneShot(s.audio, volume);
    }

    public void PlayUI(string name, float volume = 1.0f)
    {
        SoundClip s = FindClip(UIClips, name);
        if (s == null) s = FindClip(SFXClips, name);

        if (s == null)
        {
            WarnMissing("UI", name);
            return;
        }

        if (uiSource == null)
        {
            if (warnOnMissing) Debug.LogWarning("uiSource não encontrado", this);
            return;
        }

        uiSource.pitch = 1f;
        uiSource.PlayOneShot(s.audio, volume);
    }

    public void StopSFX()
    {
        sfxSource.Stop();
    }

    public void PlayLoopingSFX(string name)
    {
        SoundClip s = FindClip(SFXClips, name);
        if (s == null)
        {
            WarnMissing("SFX em loop", name);
            return;
        }

        if (loopingSfxSource.clip == s.audio && loopingSfxSource.isPlaying)
        {
            return;
        }

        loopingSfxSource.clip = s.audio;
        currentClip_LoopingSFX = s.audio;
        loopingSfxSource.Play();
    }

    public void StopLoopingSFX()
    {
        loopingSfxSource.Stop();
    }

    public void StopAllSounds()
    {
        StopMusic();
        StopSFX();
        StopLoopingSFX();
    }

    public void PauseAllSounds()
    {
        if (pauseRoutine != null) StopCoroutine(pauseRoutine);
        pauseRoutine = StartCoroutine(FadeAndPauseRoutine(true));
    }

    public void ResumeAllSounds()
    {
        if (pauseRoutine != null) StopCoroutine(pauseRoutine);
        pauseRoutine = StartCoroutine(FadeAndPauseRoutine(false));
    }

    private IEnumerator FadeAndPauseRoutine(bool isPausing)
    {
        float duration = 0.05f;
        float timer = 0f;

        musicSource.ignoreListenerPause = !pauseMusicWhenPaused;

        if (!isPausing)
        {
            AudioListener.pause = false;

            sfxSource.UnPause();
            loopingSfxSource.UnPause();

            if (pauseMusicWhenPaused) musicSource.UnPause();
            else DuckMusic(false);
        }
        else if (!pauseMusicWhenPaused)
        {
            DuckMusic(true);
        }

        float startVol = isPausing ? 1f : 0f;
        float targetVol = isPausing ? 0f : 1f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float v = Mathf.Lerp(startVol, targetVol, timer / duration);

            sfxSource.volume = v;
            loopingSfxSource.volume = v;

            yield return null;
        }

        sfxSource.volume = targetVol;
        loopingSfxSource.volume = targetVol;

        if (isPausing)
        {
            AudioListener.pause = true;

            sfxSource.Pause();
            loopingSfxSource.Pause();

            if (pauseMusicWhenPaused) musicSource.Pause();
        }
    }

    private void DuckMusic(bool duck)
    {
        if (duck)
        {
            if (musicDucked) return;

            musicDucked = true;
            musicVolumeBeforePause = musicSource.volume;

            if (Mathf.Approximately(pausedMusicVolume, 1f)) return;

            StartDuck(musicVolumeBeforePause * pausedMusicVolume);
        }
        else
        {
            if (!musicDucked) return;

            musicDucked = false;

            if (Mathf.Approximately(pausedMusicVolume, 1f)) return;

            StartDuck(musicVolumeBeforePause);
        }
    }

    private void StartDuck(float target)
    {
        if (duckRoutine != null) StopCoroutine(duckRoutine);
        duckRoutine = StartCoroutine(DuckRoutine(target));
    }

    private IEnumerator DuckRoutine(float target)
    {
        float start = musicSource.volume;
        float t = 0f;
        float time = Mathf.Max(0.01f, musicDuckTime);

        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(start, target, t / time);
            yield return null;
        }

        musicSource.volume = target;
        duckRoutine = null;
    }
}