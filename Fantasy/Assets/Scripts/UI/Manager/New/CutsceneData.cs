using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "New Cutscene", menuName = "Cutscenes/Cutscene Data")]
public class CutsceneData : ScriptableObject
{
    public VideoClip[] clips;
    public float skipUnlockDelay = 3f;
}