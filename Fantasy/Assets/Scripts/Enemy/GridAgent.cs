using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Substitui o NavMeshAgent. Segue um caminho A* sobre o DungeonGrid do
/// DungeonGenerator. Mantém a mesma API mínima usada pelos estados
/// (isStopped, SetDestination, ResetPath, pathPending, remainingDistance,
/// stoppingDistance) pra não precisar tocar em EnemyIdleState/Chase/Patrol.
/// </summary>
[DisallowMultipleComponent]
public class GridAgent : MonoBehaviour
{
    [Header("Referência")]
    [SerializeField] private DungeonGenerator dungeonGenerator;

    [Header("Movimento")]
    public float speed = 3.5f;
    public float stoppingDistance = 0.15f;
    [SerializeField] private float waypointThreshold = 0.08f;
    [SerializeField] private float minDestinationDelta = 0.05f;

    [Header("Pathfinding")]
    [Tooltip("Raio (em células) usado pra 'encaixar' start/goal na célula caminhável mais próxima, " +
             "caso a posição exata caia numa célula Empty/Buffer (ex: beirada de sala, entrada não coberta pelo corredor).")]
    [SerializeField] private int nearestWalkableSearchRadius = 3;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color pathColor = Color.cyan;
    [SerializeField] private Color agentColor = new Color(0.2f, 1f, 0.4f);
    [SerializeField] private Color targetColor = Color.red;
    [SerializeField] private bool logDebug = true;

    public bool isStopped = true;
    public bool pathPending { get; private set; }
    public float remainingDistance { get; private set; }

    private readonly List<Vector3> _path = new List<Vector3>();
    private int _waypointIndex;
    private Vector3 _lastDestinationRequested;
    private bool _hasDestination;

    // Cache da última célula de destino usada num replanejamento completo.
    // Enquanto o novo destino cair na MESMA célula (arredondada), não vale
    // a pena jogar fora o caminho e recalcular tudo — isso é o que causava
    // o inimigo "hesitar"/andar pra trás: como o transform.position dele
    // fica se movendo continuamente entre centros de célula, um replanejamento
    // completo a cada 0.15s (via EnemyChaseState) podia arredondar o ponto de
    // partida ora pra uma célula, ora pra outra, gerando um caminho novo que
    // às vezes começava "puxando" o inimigo de volta uma célula antes de
    // seguir em frente de novo.
    private bool _hasPlannedGoalCell;
    private Vector2Int _plannedGoalCell;

    public void SetDestination(Vector3 worldPosition)
    {
        if (_hasDestination && Vector3.Distance(worldPosition, _lastDestinationRequested) < minDestinationDelta)
            return;

        _lastDestinationRequested = worldPosition;
        _hasDestination = true;

        DungeonGrid grid = dungeonGenerator != null ? dungeonGenerator.Grid : null;
        if (grid == null)
        {
            if (logDebug)
                Debug.LogWarning($"[GridAgent] {name}: grid é NULL (dungeonGenerator vazio ou Grid ainda não gerado). dungeonGenerator={dungeonGenerator}");

            _path.Clear();
            remainingDistance = 0f;
            return;
        }

        Vector2Int rawGoal = grid.WorldToCell(worldPosition);
        Vector2Int goalCell = GridPathfinder.FindNearestWalkable(grid, rawGoal, nearestWalkableSearchRadius);

        // Destino ainda cai na mesma célula de quando planejamos o caminho
        // atual: não recalcula do zero, só refina o último waypoint pra
        // seguir a posição exata do player dentro dessa célula. Isso evita
        // o replanejamento constante que causava o "ir e voltar".
        if (_hasPlannedGoalCell && goalCell == _plannedGoalCell && _path.Count > 0)
        {
            _path[_path.Count - 1] = worldPosition;
            RecalculateRemainingDistance();
            return;
        }

        Vector2Int rawStart = grid.WorldToCell(transform.position);
        Vector2Int startCell = GridPathfinder.FindNearestWalkable(grid, rawStart, nearestWalkableSearchRadius);

        List<Vector2Int> cellPath = GridPathfinder.FindPath(grid, startCell, goalCell);

        if (logDebug)
        {
            string result = cellPath != null ? $"{cellPath.Count} waypoints" : "NULL (sem caminho)";
            Debug.Log($"[GridAgent] {name}: rawStart={rawStart}->{startCell} rawGoal={rawGoal}->{goalCell} path={result}");
        }

        _path.Clear();
        _waypointIndex = 0;

        if (cellPath == null || cellPath.Count == 0)
        {
            remainingDistance = 0f;
            _hasPlannedGoalCell = false;
            return;
        }

        foreach (Vector2Int cell in cellPath)
            _path.Add(dungeonGenerator.GetAlignedWorldPos(cell)); // era grid.CellToWorld(cell) — cru, causava desalinhamento com o wallTilemap real

        // Substitui o último waypoint (centro da célula) pelo destino real,
        // pra parar exatamente onde foi pedido.
        _path[_path.Count - 1] = worldPosition;

        _hasPlannedGoalCell = true;
        _plannedGoalCell = goalCell;

        RecalculateRemainingDistance();
    }

    public void ResetPath()
    {
        _path.Clear();
        _waypointIndex = 0;
        _hasDestination = false;
        _hasPlannedGoalCell = false;
        remainingDistance = 0f;
    }

    private void Update()
    {
        if (isStopped || _waypointIndex >= _path.Count) return;

        Vector3 target = _path[_waypointIndex];
        Vector3 current = transform.position;
        Vector3 toTarget = target - current;
        float dist = toTarget.magnitude;

        if (dist <= waypointThreshold)
        {
            _waypointIndex++;
        }
        else
        {
            transform.position = current + (toTarget / dist) * speed * Time.deltaTime;
        }

        RecalculateRemainingDistance();
    }

    private void RecalculateRemainingDistance()
    {
        if (_waypointIndex >= _path.Count)
        {
            remainingDistance = 0f;
            return;
        }

        float total = Vector3.Distance(transform.position, _path[_waypointIndex]);
        for (int i = _waypointIndex; i < _path.Count - 1; i++)
            total += Vector3.Distance(_path[i], _path[i + 1]);

        remainingDistance = total;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Inimigo (posição atual)
        Gizmos.color = agentColor;
        Gizmos.DrawWireSphere(transform.position, 0.25f);

        // Destino final
        if (_hasDestination)
        {
            Gizmos.color = targetColor;
            Gizmos.DrawWireSphere(_lastDestinationRequested, 0.2f);
            Gizmos.DrawLine(transform.position, _lastDestinationRequested);
        }

        // Caminho calculado (waypoints restantes)
        if (_path.Count == 0) return;

        Gizmos.color = pathColor;

        if (_waypointIndex < _path.Count)
            Gizmos.DrawLine(transform.position, _path[_waypointIndex]);

        for (int i = _waypointIndex; i < _path.Count - 1; i++)
            Gizmos.DrawLine(_path[i], _path[i + 1]);

        for (int i = _waypointIndex; i < _path.Count; i++)
            Gizmos.DrawSphere(_path[i], 0.08f);
    }
}