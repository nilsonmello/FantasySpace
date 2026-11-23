using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LoopSoundSignal : TrapSignal
{
    [SerializeField] private AudioSource source;

    private void Awake()
    {
        if (source == null) source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
    }

    public override void Begin()
    {
        if (source != null && !source.isPlaying)
            source.Play();
    }

    public override void End()
    {
        if (source != null)
            source.Stop();
    }

    private void OnDisable()
    {
        if (!gameObject.activeInHierarchy)
            End();
    }
}