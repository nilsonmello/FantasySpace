using System;
using System.Collections;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource loopingSfxSource;

    [System.Serializable] public class SoundClip
    {
        public string SoundName;
        public AudioClip audio;
    }

    [Header("Audio Clips")]
    public SoundClip[] MusicClips;
    public SoundClip[] SFXClips;
    private AudioClip currentClip_SFX;
    private AudioClip currentClip_LoopingSFX;

    [Header("SFX Randomization")]
    [Range(0.1f, 2.0f)] public float minPitch = 0.9f;
    [Range(0.1f, 2.0f)] public float maxPitch = 1.1f;

    private Coroutine pauseRoutine;

    public void PlayMusic(string name)
    {
        SoundClip s = Array.Find(MusicClips, sound => sound.SoundName == name);
        if (s != null)
        {
            if (musicSource.clip == s.audio && musicSource.isPlaying)
            {
                return;
            }
            musicSource.clip = s.audio;
            musicSource.Play();
        }
    }

    public IEnumerator FadeMusic(float targetVolume, float fadeDuration)
    {
        float timer = 0f;
        float startVolume = musicSource.volume;

        while (timer < fadeDuration)
        {
            // timer / fadeDuration = 0.0 to 1.0
            // Mathf.Lerp(start, end, percentage)
            float newVolume = Mathf.Lerp(startVolume, targetVolume, timer / fadeDuration);

            musicSource.volume = newVolume;

            timer += Time.deltaTime;

            // Wait for the next frame
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }
    public void PlaySFX(string name, float volume = 1.0f, bool randomPitch = false)
    {
        SoundClip s = Array.Find(SFXClips, sound => sound.SoundName == name);
        if (s != null)
        {
            if (randomPitch) sfxSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
            else sfxSource.pitch = 1.0f;
            currentClip_SFX = s.audio;
            sfxSource.PlayOneShot(s.audio, volume);
        }
    }

    public void StopSFX()
    {
        sfxSource.Stop();
    }

    public void PlayLoopingSFX(string name)
    {
        SoundClip s = Array.Find(SFXClips, sound => sound.SoundName == name);
        if (s != null)
        {
            if (loopingSfxSource.clip == s.audio && loopingSfxSource.isPlaying)
            {
                return;
            }

            loopingSfxSource.clip = s.audio;
            currentClip_LoopingSFX = s.audio;
            loopingSfxSource.Play();
        }
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
        
        if (!isPausing)
        {
            AudioListener.pause = false;

            sfxSource.UnPause();
            loopingSfxSource.UnPause();
            musicSource.UnPause();
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
            musicSource.Pause();
        }
    }
}
