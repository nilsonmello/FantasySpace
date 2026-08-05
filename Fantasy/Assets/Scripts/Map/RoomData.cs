using UnityEngine;

[CreateAssetMenu(fileName = "NewRoomData", menuName = "Dungeon/Room Data")]
public class RoomData : ScriptableObject
{
    public enum RoomType
    {
        Common,
        Spawn,
        Boss,
        Treasure,
        Secret
    }

    [Header("Identificação")]
    public string roomName;
    public RoomType roomType = RoomType.Common;

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Geração")]
    [Min(0f)]
    public float spawnWeight = 1f;

    public int maxInstances = -1;
}
