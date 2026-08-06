using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonGenerator : MonoBehaviour
{
    [System.Serializable]
    public struct CorridorEdge
    {
        public RoomInstance roomA;
        public RoomInstance roomB;
        public float distance;
    }

    [System.Serializable]
    public class CorridorPropSpawnData
    {
        public GameObject prefab;

        [Range(0f, 1f)] public float chancePerCell = 0.1f;

        [Min(0)] public int maxCount = 5;

        [Min(0)] public int minSpacingCells = 0;
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

    [Header("Loops extras")]
    [SerializeField] private int extraLoopEdges = 0;

    [Header("Corredores")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float minCorridorRoomDistance = 1f;
    [SerializeField] private float corridorHugPenalty = 4f;

    [Header("corredores (prefabs)")]
    [SerializeField] private GameObject corridorTilePrefab;
    [SerializeField] private Transform corridorContainer;

    [Header("Objetos em corredores")]
    [SerializeField] private List<CorridorPropSpawnData> corridorProps = new List<CorridorPropSpawnData>();

    [Header("Paredes (Tilemap)")]
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private TileBase wallTile;
    [SerializeField] private int wallPadding = 2;

    private readonly List<List<Vector2Int>> _corridorPaths = new List<List<Vector2Int>>();
    public IReadOnlyList<List<Vector2Int>> CorridorPaths => _corridorPaths;
    public DungeonGrid Grid { get; private set; }

    [Header("Runtime")]
    [SerializeField] private bool generateOnStart = false;
    [SerializeField] private bool regenerateOnEnterKey = true;
    [SerializeField] private int maxGenerationAttempts = 30;

    private bool _isGenerating;
    public bool IsGenerating => _isGenerating;

    private readonly List<RoomInstance> _placedRooms = new List<RoomInstance>();
    private readonly List<CorridorEdge> _mstEdges = new List<CorridorEdge>();
    private readonly List<GameObject> _spawnedCorridorVisuals = new List<GameObject>();
    private readonly List<GameObject> _spawnedCorridorProps = new List<GameObject>();

    public IReadOnlyList<RoomInstance> PlacedRooms => _placedRooms;
    public IReadOnlyList<CorridorEdge> ConnectionGraph => _mstEdges;

    public event System.Action OnGenerationComplete;

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
        if (_isGenerating) return;

        if (Application.isPlaying)
        {

            StartCoroutine(GenerateRoutine());
        }
        else
        {
            GenerateSync();
        }
    }

    private IEnumerator GenerateRoutine()
    {
        _isGenerating = true;

        for (int attempt = 1; attempt <= maxGenerationAttempts; attempt++)
        {
            if (TryGenerateOnce(attempt))
            {
                _isGenerating = false;
                OnGenerationComplete?.Invoke();
                yield break;
            }

            Debug.LogWarning($"tentativa {attempt}/{maxGenerationAttempts} falhou, reiniciando");
            yield return null;
        }

        Debug.LogError($"cansei na {maxGenerationAttempts} tentativa, Ajuste as variáveis de geração");
        _isGenerating = false;
    }

    private void GenerateSync()
    {
        _isGenerating = true;

        for (int attempt = 1; attempt <= maxGenerationAttempts; attempt++)
        {
            if (TryGenerateOnce(attempt))
            {
                _isGenerating = false;
                OnGenerationComplete?.Invoke();
                return;
            }

            Debug.LogWarning($"tentativa {attempt}/{maxGenerationAttempts} falhou também, reiniciando");
        }

        Debug.LogError($"cansei na {maxGenerationAttempts} tentativa, Ajuste as variáveis de geração");
        _isGenerating = false;
    }

    private bool TryGenerateOnce(int attemptNumber)
    {
        Clear();

        var rng = useRandomSeed
            ? new System.Random()
            : new System.Random(seed + attemptNumber - 1);

        PlaceRooms(rng);
        BuildGrid();
        BuildConnectionGraph();

        bool carved = CarveCorridors();

        if (!carved || !ValidateConnectivity())
            return false;

        ThinCorridorBlobs();
        SpawnCorridorVisuals();
        SpawnCorridorProps(rng);
        FillWalls();
        return true;
    }

    private void Clear()
    {
        foreach (var room in _placedRooms)
            if (room != null) DestroyImmediate(room.gameObject);

        foreach (var visual in _spawnedCorridorVisuals)
            if (visual != null) DestroyImmediate(visual);

        foreach (var prop in _spawnedCorridorProps)
            if (prop != null) DestroyImmediate(prop);

        if (wallTilemap != null) wallTilemap.ClearAllTiles();

        _placedRooms.Clear();
        _mstEdges.Clear();
        _corridorPaths.Clear();
        _spawnedCorridorVisuals.Clear();
        _spawnedCorridorProps.Clear();
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

            List<Vector2Int> path = CorridorCarver.FindPath(Grid, startCell, endCell, corridorHugPenalty);

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

    private void ThinCorridorBlobs()
    {
        if (Grid == null) return;

        var corridorCells = new HashSet<Vector2Int>();
        foreach (var kvp in Grid.AllCells)
            if (kvp.Value == DungeonGrid.CellType.Corridor)
                corridorCells.Add(kvp.Key);

        foreach (var cell in corridorCells.ToList())
        {
            if (!corridorCells.Contains(cell)) continue;

            var quad = new[]
            {
                cell,
                cell + Vector2Int.right,
                cell + Vector2Int.up,
                cell + new Vector2Int(1, 1)
            };

            if (!quad.All(c => corridorCells.Contains(c))) continue;

            Vector2Int? toRemove = PickSafeRemoval(quad, corridorCells);
            if (toRemove == null) continue;

            Grid.SetCell(toRemove.Value, DungeonGrid.CellType.Empty);
            corridorCells.Remove(toRemove.Value);
        }
    }

    private static Vector2Int? PickSafeRemoval(Vector2Int[] quad, HashSet<Vector2Int> corridorCells)
    {
        foreach (var corner in quad)
        {
            bool hasExternalConnection = false;

            foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                Vector2Int neighbor = corner + dir;
                if (quad.Contains(neighbor)) continue;

                if (corridorCells.Contains(neighbor))
                {
                    hasExternalConnection = true;
                    break;
                }
            }

            if (!hasExternalConnection) return corner;
        }

        return null;
    }

    private Vector3 GetAlignedWorldPos(Vector2Int cell)
    {
        Vector3 rawWorldPos = Grid.CellToWorld(cell);
        if (wallTilemap == null) return rawWorldPos;

        Vector3Int tileCell = wallTilemap.WorldToCell(rawWorldPos);
        return wallTilemap.GetCellCenterWorld(tileCell);
    }

    private void SpawnCorridorVisuals()
    {
        if (corridorTilePrefab == null || Grid == null) return;

        Transform parent = corridorContainer != null ? corridorContainer : transform;

        foreach (var kvp in Grid.AllCells)
        {
            if (kvp.Value != DungeonGrid.CellType.Corridor) continue;

            Vector3 worldPos = GetAlignedWorldPos(kvp.Key);
            GameObject visual = Instantiate(corridorTilePrefab, worldPos, Quaternion.identity, parent);
            _spawnedCorridorVisuals.Add(visual);
        }
    }

    private void SpawnCorridorProps(System.Random rng)
    {
        if (Grid == null || corridorProps == null || corridorProps.Count == 0) return;

        var corridorCells = new List<Vector2Int>();
        foreach (var kvp in Grid.AllCells)
            if (kvp.Value == DungeonGrid.CellType.Corridor)
                corridorCells.Add(kvp.Key);

        Shuffle(corridorCells, rng);

        var counts = new Dictionary<CorridorPropSpawnData, int>();
        var placedCells = new Dictionary<CorridorPropSpawnData, List<Vector2Int>>();
        foreach (var prop in corridorProps)
        {
            counts[prop] = 0;
            placedCells[prop] = new List<Vector2Int>();
        }

        Transform parent = corridorContainer != null ? corridorContainer : transform;

        foreach (var cell in corridorCells)
        {
            var candidateProps = new List<CorridorPropSpawnData>(corridorProps);
            Shuffle(candidateProps, rng);

            foreach (var prop in candidateProps)
            {
                if (prop.prefab == null) continue;
                if (counts[prop] >= prop.maxCount) continue;
                if ((float)rng.NextDouble() > prop.chancePerCell) continue;
                if (prop.minSpacingCells > 0 && IsTooCloseToSameProp(cell, placedCells[prop], prop.minSpacingCells)) continue;

                Vector3 worldPos = GetAlignedWorldPos(cell);
                GameObject instance = Instantiate(prop.prefab, worldPos, Quaternion.identity, parent);
                _spawnedCorridorProps.Add(instance);

                counts[prop]++;
                placedCells[prop].Add(cell);
                break;
            }
        }
    }

    private static bool IsTooCloseToSameProp(Vector2Int cell, List<Vector2Int> existingCells, int minSpacing)
    {
        foreach (var other in existingCells)
        {
            int manhattanDist = Mathf.Abs(cell.x - other.x) + Mathf.Abs(cell.y - other.y);
            if (manhattanDist < minSpacing) return true;
        }
        return false;
    }

    private static void Shuffle<T>(IList<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void FillWalls()
    {
        if (wallTilemap == null || wallTile == null || Grid == null) return;

        if (!TryGetContentCellBounds(out int minX, out int maxX, out int minY, out int maxY))
            return;

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;
        int side = Mathf.Max(width, height) + wallPadding * 2;

        int centerX = Mathf.RoundToInt((minX + maxX) / 2f);
        int centerY = Mathf.RoundToInt((minY + maxY) / 2f);
        int half = side / 2;

        int squareMinX = centerX - half;
        int squareMinY = centerY - half;

        for (int x = 0; x < side; x++)
        {
            for (int y = 0; y < side; y++)
            {
                var cell = new Vector2Int(squareMinX + x, squareMinY + y);

                DungeonGrid.CellType type = Grid.GetCell(cell);
                if (type == DungeonGrid.CellType.Room || type == DungeonGrid.CellType.Corridor) continue;

                Vector3 worldPos = Grid.CellToWorld(cell);
                Vector3Int tileCell = wallTilemap.WorldToCell(worldPos);
                wallTilemap.SetTile(tileCell, wallTile);
            }
        }
    }

    private bool TryGetContentCellBounds(out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = minY = int.MaxValue;
        maxX = maxY = int.MinValue;

        bool any = false;
        foreach (var kvp in Grid.AllCells)
        {
            any = true;
            if (kvp.Key.x < minX) minX = kvp.Key.x;
            if (kvp.Key.x > maxX) maxX = kvp.Key.x;
            if (kvp.Key.y < minY) minY = kvp.Key.y;
            if (kvp.Key.y > maxY) maxY = kvp.Key.y;
        }

        return any;
    }

    private void PlaceRooms(System.Random rng)
    {
        for (int i = 0; i < roomCount; i++)
        {
            RoomData data = PickWeightedRoom(rng);
            if (data == null || data.prefab == null) continue;

            bool placed = TryPlaceRoom(data, rng);
            if (!placed)
                Debug.LogWarning($"não deu pra colocar '{data.roomName}' mesmo com {maxAttemptsPerRoom} tentativas");
        }
    }

    private bool TryPlaceRoom(RoomData data, System.Random rng)
    {
        for (int attempt = 0; attempt < maxAttemptsPerRoom; attempt++)
        {
            Vector2 candidatePos = RandomPointInCircle(rng, placementRadius);
            candidatePos = SnapToGrid(candidatePos);

            GameObject go = Instantiate(data.prefab, candidatePos, Quaternion.identity, transform);
            RoomInstance instance = go.GetComponent<RoomInstance>();

            if (instance == null)
            {
                Debug.LogError($"prefab '{data.prefab.name}' não tem RoomInstance");
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

    private Vector2 SnapToGrid(Vector2 worldPos)
    {
        return new Vector2(
            Mathf.Round(worldPos.x / cellSize) * cellSize,
            Mathf.Round(worldPos.y / cellSize) * cellSize);
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