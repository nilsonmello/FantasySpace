using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct CorridorEdge
    {
        public RoomInstance roomA;
        public RoomInstance roomB;
        public float distance;
    }

    [Header("Salas disponíveis")]
    [SerializeField] private List<RoomData> availableRooms;

    [Header("Configuração de geração")]
    [SerializeField] private int roomCount = 10;
    [SerializeField] private float placementRadius = 40f;
    [SerializeField] private float minSpacing = 2f;
    [SerializeField] private int maxAttemptsPerRoom = 50;
    [SerializeField] private int seed = 0;
    [SerializeField] private bool useRandomSeed = true;

    [Header("Loops extras (opcional)")]
    [SerializeField] private int extraLoopEdges = 0;

    [Header("Corredores (grid)")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float minCorridorRoomDistance = 1f;

    [Header("Visual dos corredores (prefabs)")]
    [SerializeField] private GameObject corridorTilePrefab;
    [SerializeField] private Transform corridorContainer;

    private readonly List<List<Vector2Int>> _corridorPaths = new List<List<Vector2Int>>();
    public IReadOnlyList<List<Vector2Int>> CorridorPaths => _corridorPaths;
    public DungeonGrid Grid { get; private set; }

    [Header("Runtime")]
    [SerializeField] private bool generateOnStart = false;
    [SerializeField] private bool regenerateOnEnterKey = true;

    private readonly List<RoomInstance> _placedRooms = new List<RoomInstance>();
    private readonly List<CorridorEdge> _mstEdges = new List<CorridorEdge>();
    private readonly List<GameObject> _spawnedCorridorVisuals = new List<GameObject>();

    public IReadOnlyList<RoomInstance> PlacedRooms => _placedRooms;
    public IReadOnlyList<CorridorEdge> ConnectionGraph => _mstEdges;

    private void Start()
    {
        if (generateOnStart) Generate();
    }

    private void Update()
    {
        if (!regenerateOnEnterKey) return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            Generate();
    }

    public void Generate()
    {
        Clear();

        var rng = useRandomSeed
            ? new System.Random()
            : new System.Random(seed);

        PlaceRooms(rng);
        BuildGrid();
        BuildConnectionGraph();

        bool carved = CarveCorridors();

        if (!carved || !ValidateConnectivity())
        {
            Debug.LogWarning("Algo falhou, recriando");
            Generate();
            return;
        }

        SpawnCorridorVisuals();
    }

    private void Clear()
    {
        foreach (var room in _placedRooms)
            if (room != null) DestroyImmediate(room.gameObject);

        foreach (var visual in _spawnedCorridorVisuals)
            if (visual != null) DestroyImmediate(visual);

        _placedRooms.Clear();
        _mstEdges.Clear();
        _corridorPaths.Clear();
        _spawnedCorridorVisuals.Clear();
        Grid = null;
    }

    private void BuildGrid()
    {
        Grid = new DungeonGrid(cellSize);

        foreach (var room in _placedRooms)
            Grid.MarkRoomBounds(room.WorldBounds);

        int bufferCells = Mathf.Max(0, Mathf.RoundToInt(minCorridorRoomDistance / cellSize));
        if (bufferCells > 0)
        {
            foreach (var room in _placedRooms)
                Grid.MarkRoomBuffer(room.WorldBounds, bufferCells);
        }

        foreach (var room in _placedRooms)
        {
            Vector2Int entranceCell = Grid.WorldToCell(room.EntrancePosition);
            Vector2Int exitDir = ComputeExitDirection(room.WorldBounds, room.EntrancePosition);
            Grid.MarkEntrance(entranceCell, exitDir, bufferCells);
        }
    }

    private static Vector2Int ComputeExitDirection(Bounds bounds, Vector2 entrancePos)
    {
        float distLeft = Mathf.Abs(entrancePos.x - bounds.min.x);
        float distRight = Mathf.Abs(entrancePos.x - bounds.max.x);
        float distBottom = Mathf.Abs(entrancePos.y - bounds.min.y);
        float distTop = Mathf.Abs(entrancePos.y - bounds.max.y);

        float minDist = Mathf.Min(Mathf.Min(distLeft, distRight), Mathf.Min(distBottom, distTop));

        if (minDist == distLeft) return Vector2Int.left;
        if (minDist == distRight) return Vector2Int.right;
        if (minDist == distBottom) return Vector2Int.down;
        return Vector2Int.up;
    }

    private bool CarveCorridors()
    {
        _corridorPaths.Clear();

        foreach (var edge in _mstEdges)
        {
            Vector2Int startCell = Grid.WorldToCell(edge.roomA.EntrancePosition);
            Vector2Int endCell = Grid.WorldToCell(edge.roomB.EntrancePosition);

            List<Vector2Int> path = CorridorCarver.FindPath(Grid, startCell, endCell);

            if (path == null)
            {
                Debug.LogWarning($"sem caminho entre '{edge.roomA.name}' e '{edge.roomB.name}'");
                return false;
            }

            foreach (var cell in path)
            {
                if (Grid.GetCell(cell) != DungeonGrid.CellType.Room)
                    Grid.SetCell(cell, DungeonGrid.CellType.Corridor);
            }

            _corridorPaths.Add(path);
        }

        return true;
    }

    private void SpawnCorridorVisuals()
    {
        if (corridorTilePrefab == null || Grid == null) return;

        Transform parent = corridorContainer != null ? corridorContainer : transform;

        foreach (var kvp in Grid.AllCells)
        {
            if (kvp.Value != DungeonGrid.CellType.Corridor) continue;

            Vector2 worldPos = Grid.CellToWorld(kvp.Key);
            GameObject visual = Instantiate(corridorTilePrefab, worldPos, Quaternion.identity, parent);
            _spawnedCorridorVisuals.Add(visual);
        }
    }

    private void PlaceRooms(System.Random rng)
    {
        for (int i = 0; i < roomCount; i++)
        {
            RoomData data = PickWeightedRoom(rng);
            if (data == null || data.prefab == null) continue;

            bool placed = TryPlaceRoom(data, rng);
            if (!placed)
                Debug.LogWarning($"Não consegui colocar '{data.roomName}' depois de {maxAttemptsPerRoom} tentativas");
        }
    }

    private bool TryPlaceRoom(RoomData data, System.Random rng)
    {
        for (int attempt = 0; attempt < maxAttemptsPerRoom; attempt++)
        {
            Vector2 candidatePos = RandomPointInCircle(rng, placementRadius);

            GameObject go = Instantiate(data.prefab, candidatePos, Quaternion.identity, transform);
            RoomInstance instance = go.GetComponent<RoomInstance>();

            if (instance == null)
            {
                Debug.LogError($"prefab '{data.prefab.name}' não tem RoomInstance.");
                DestroyImmediate(go);
                return false;
            }

            if (OverlapsExisting(instance))
            {
                DestroyImmediate(go);
                continue;
            }

            instance.Initialize(data);
            _placedRooms.Add(instance);
            return true;
        }

        return false;
    }

    private bool OverlapsExisting(RoomInstance candidate)
    {
        Bounds candidateBounds = candidate.WorldBounds;
        candidateBounds.Expand(minSpacing);

        foreach (var existing in _placedRooms)
        {
            if (candidateBounds.Intersects(existing.WorldBounds))
                return true;
        }
        return false;
    }

    private RoomData PickWeightedRoom(System.Random rng)
    {
        float totalWeight = availableRooms.Sum(r => r.spawnWeight);
        if (totalWeight <= 0f) return null;

        float roll = (float)(rng.NextDouble() * totalWeight);
        float cumulative = 0f;

        foreach (var room in availableRooms)
        {
            cumulative += room.spawnWeight;
            if (roll <= cumulative) return room;
        }
        return availableRooms.LastOrDefault();
    }

    private Vector2 RandomPointInCircle(System.Random rng, float radius)
    {
        float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
        float r = radius * Mathf.Sqrt((float)rng.NextDouble());
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
    }

    private void BuildConnectionGraph()
    {
        _mstEdges.Clear();
        if (_placedRooms.Count < 2) return;

        var allEdges = new List<CorridorEdge>();
        for (int i = 0; i < _placedRooms.Count; i++)
        {
            for (int j = i + 1; j < _placedRooms.Count; j++)
            {
                float dist = Vector2.Distance(
                    _placedRooms[i].EntrancePosition,
                    _placedRooms[j].EntrancePosition);

                allEdges.Add(new CorridorEdge
                {
                    roomA = _placedRooms[i],
                    roomB = _placedRooms[j],
                    distance = dist
                });
            }
        }

        allEdges.Sort((a, b) => a.distance.CompareTo(b.distance));

        var unionFind = new UnionFind(_placedRooms.Count);
        var roomIndex = _placedRooms
            .Select((room, idx) => (room, idx))
            .ToDictionary(p => p.room, p => p.idx);

        foreach (var edge in allEdges)
        {
            int a = roomIndex[edge.roomA];
            int b = roomIndex[edge.roomB];

            if (unionFind.Find(a) != unionFind.Find(b))
            {
                unionFind.Union(a, b);
                _mstEdges.Add(edge);
            }
        }

        AddExtraLoopEdges(allEdges);
    }

    private void AddExtraLoopEdges(List<CorridorEdge> allEdges)
    {
        if (extraLoopEdges <= 0) return;

        var remaining = allEdges.Except(_mstEdges)
            .OrderBy(e => e.distance)
            .Take(extraLoopEdges);

        _mstEdges.AddRange(remaining);
    }

    private bool ValidateConnectivity()
    {
        if (_placedRooms.Count == 0) return true;

        var adjacency = new Dictionary<RoomInstance, List<RoomInstance>>();
        foreach (var room in _placedRooms) adjacency[room] = new List<RoomInstance>();

        foreach (var edge in _mstEdges)
        {
            adjacency[edge.roomA].Add(edge.roomB);
            adjacency[edge.roomB].Add(edge.roomA);
        }

        var visited = new HashSet<RoomInstance>();
        var queue = new Queue<RoomInstance>();
        queue.Enqueue(_placedRooms[0]);
        visited.Add(_placedRooms[0]);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in adjacency[current])
            {
                if (visited.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }

        return visited.Count == _placedRooms.Count;
    }

    private class UnionFind
    {
        private readonly int[] _parent;

        public UnionFind(int size)
        {
            _parent = new int[size];
            for (int i = 0; i < size; i++) _parent[i] = i;
        }

        public int Find(int x)
        {
            if (_parent[x] != x) _parent[x] = Find(_parent[x]);
            return _parent[x];
        }

        public void Union(int a, int b)
        {
            int rootA = Find(a);
            int rootB = Find(b);
            if (rootA != rootB) _parent[rootA] = rootB;
        }
    }

    private void OnDrawGizmos()
    {
        if (Grid == null) return;

        float half = cellSize * 0.45f;
        var size = new Vector3(half * 2f, half * 2f, 0f);

        foreach (var kvp in Grid.AllCells)
        {
            if (kvp.Value != DungeonGrid.CellType.Corridor) continue;
            if (corridorTilePrefab != null) continue;

            Vector2 worldPos = Grid.CellToWorld(kvp.Key);
            Gizmos.color = Color.cyan;
            Gizmos.DrawCube(worldPos, size);
        }
    }
}