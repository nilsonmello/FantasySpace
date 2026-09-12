using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundEventChannel", menuName = "Dungeon/Sound Event Channel")]
public class SoundEventChannelSO : ScriptableObject
{

    public event Action<Vector2, float, float, int> OnSoundRaised;

    public void RaiseSound(Vector2 position, float searchRadius, float priority = 1f, int sourceId = 0)
    {
        OnSoundRaised?.Invoke(position, searchRadius, priority, sourceId);
    }
}