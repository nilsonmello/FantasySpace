using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Orquestra os três estados do inimigo (Idle, Chase, Patrol). Toda a lógica
/// de "quando trocar de estado" fica centralizada aqui, reagindo aos eventos
/// do EnemyVision — os estados individuais só cuidam do próprio comportamento
/// (ver EnemyIdleState / EnemyChaseState / EnemyPatrolState).
///
/// Requer um NavMeshAgent já configurado na cena (NavMesh baked, seja via
/// NavMesh 3D "achatado" ou via NavMeshPlus para um NavMesh 2D de verdade).
/// </summary>
[RequireComponent(typeof(GridAgent))]   // era NavMeshAgent
[RequireComponent(typeof(EnemyVision))]
public class EnemyStateMachine : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Quantos segundos o inimigo fica 'olhando ao redor' no último ponto onde viu o player, antes de voltar pra Idle.")]
    [SerializeField] private float patrolLookDuration = 2.5f;

    public GridAgent Agent { get; private set; } 
    public EnemyVision Vision { get; private set; }

    public Vector3 LastKnownPlayerPosition { get; private set; }
    public float PatrolLookDuration => patrolLookDuration;

    public IEnemyState CurrentState { get; private set; }

    public readonly EnemyIdleState IdleState = new EnemyIdleState();
    public readonly EnemyChaseState ChaseState = new EnemyChaseState();
    public readonly EnemyPatrolState PatrolState = new EnemyPatrolState();

    private void Awake()
    {
        Agent = GetComponent<GridAgent>();          // era GetComponent<NavMeshAgent>()
        Vision = GetComponent<EnemyVision>();
    }

    private void OnEnable()
    {
        Vision.OnPlayerSpotted += HandlePlayerSpotted;
        Vision.OnPlayerLost += HandlePlayerLost;
    }

    private void OnDisable()
    {
        Vision.OnPlayerSpotted -= HandlePlayerSpotted;
        Vision.OnPlayerLost -= HandlePlayerLost;
    }

    private void Start()
    {
        ChangeState(IdleState);
    }

    private void Update()
    {
        CurrentState?.Tick(this);
    }

    public void ChangeState(IEnemyState newState)
    {
        if (CurrentState == newState) return;

        CurrentState?.Exit(this);
        CurrentState = newState;
        CurrentState.Enter(this);
    }

    private void HandlePlayerSpotted(Vector3 position)
    {
        LastKnownPlayerPosition = position;

        // Avistou o player: entra (ou continua) em Chase, não importa de
        // qual estado veio (Idle ou Patrol).
        if (CurrentState != ChaseState)
            ChangeState(ChaseState);
    }

    private void HandlePlayerLost(Vector3 lastPosition)
    {
        LastKnownPlayerPosition = lastPosition;

        // Só reage à perda de visão se estava de fato perseguindo.
        if (CurrentState == ChaseState)
            ChangeState(PatrolState);
    }
}
