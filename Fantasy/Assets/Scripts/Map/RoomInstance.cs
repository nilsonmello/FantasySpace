using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class RoomInstance : MonoBehaviour
{
    public enum Direction { North, South, East, West }

    [Header("Corridor")]
    [SerializeField] private Transform entrancePoint;

    [Tooltip("Entrance")]
    [SerializeField] private Direction entranceDirection;

    [Header("Spawn/Exit points")]
    [SerializeField] private string spawnPointTag = "SpawnPoint";
    [SerializeField] private string exitPointTag = "ExitPoint";

    private BoxCollider2D _bounds;

    public RoomData SourceData { get; private set; }

    public Vector2 EntrancePosition => entrancePoint != null ? (Vector2)entrancePoint.position : (Vector2)transform.position;

    public Direction EntranceDirection => entranceDirection;

    public Bounds WorldBounds
    {
        get
        {
            if (_bounds == null) _bounds = GetComponent<BoxCollider2D>();
            return _bounds.bounds;
        }
    }

    public Vector2 EntranceOutwardDir
    {
        get
        {
            switch (entranceDirection)
            {
                case Direction.North: return Vector2.up;
                case Direction.South: return Vector2.down;
                case Direction.East: return Vector2.right;
                default: return Vector2.left;
            }
        }
    }

    public Vector3 SpawnPosition => FindPointByTag(spawnPointTag, WorldBounds.center);
    public Vector3 ExitPosition => FindPointByTag(exitPointTag, WorldBounds.center);

    public void Initialize(RoomData sourceData)
    {
        SourceData = sourceData;
    }

    private Vector3 FindPointByTag(string tag, Vector3 fallback)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child.CompareTag(tag))
                return child.position;
        }
        return fallback;
    }

    private void OnDrawGizmosSelected()
    {
        if (entrancePoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(entrancePoint.position, 0.3f);
        Gizmos.DrawLine(entrancePoint.position, entrancePoint.position + (Vector3)EntranceOutwardDir * 1f);
    }
}