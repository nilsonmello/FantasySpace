using UnityEngine;

/// <summary>
/// Detecta o player dentro de um raio circular, considerando oclusão por
/// paredes (Linecast contra wallMask). Não conhece estados nem NavMesh —
/// só reporta "vi" / "perdi" via eventos, pra quem quiser reagir a isso
/// (ex: EnemyStateMachine) decidir o que fazer.
/// </summary>
public class EnemyVision : MonoBehaviour
{
    [Header("Detecção")]
    [SerializeField] private float visionRadius = 6f;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private float checkInterval = 0.1f;

    [Tooltip("Deslocamento do ponto de origem da visão em relação ao transform (ex: altura dos 'olhos').")]
    [SerializeField] private Vector2 eyeOffset = Vector2.zero;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    /// <summary>Disparado a cada check enquanto o player está visível, com a posição atual dele.</summary>
    public event System.Action<Vector3> OnPlayerSpotted;

    /// <summary>Disparado uma vez, no instante em que o player deixa de estar visível, com a última posição conhecida.</summary>
    public event System.Action<Vector3> OnPlayerLost;

    public bool IsPlayerVisible { get; private set; }

    private Vector3 _lastSeenPosition;
    private float _timer;

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        _timer = checkInterval;
        CheckVision();
    }

    private void CheckVision()
    {
        Vector2 eyePos = (Vector2)transform.position + eyeOffset;
        Collider2D playerCollider = Physics2D.OverlapCircle(eyePos, visionRadius, playerMask);

        bool visibleNow = false;

        if (playerCollider != null)
        {
            Vector2 targetPos = playerCollider.transform.position;
            RaycastHit2D wallHit = Physics2D.Linecast(eyePos, targetPos, wallMask);

            visibleNow = wallHit.collider == null;

            if (visibleNow)
                _lastSeenPosition = playerCollider.transform.position;
        }

        if (visibleNow)
        {
            IsPlayerVisible = true;
            OnPlayerSpotted?.Invoke(_lastSeenPosition);
        }
        else if (IsPlayerVisible)
        {
            IsPlayerVisible = false;
            OnPlayerLost?.Invoke(_lastSeenPosition);
        }
    }

private void OnDrawGizmos()
{
    if (!showGizmos) return;

    Vector2 eyePos = (Vector2)transform.position + eyeOffset;
    Gizmos.color = IsPlayerVisible ? new Color(1f, 0.2f, 0.2f, 0.8f) : new Color(1f, 0.9f, 0.2f, 0.5f);
    Gizmos.DrawWireSphere(eyePos, visionRadius);

    // Linha até o último ponto avaliado, colorida pelo resultado do Linecast
    if (Application.isPlaying)
    {
        Collider2D playerCollider = Physics2D.OverlapCircle(eyePos, visionRadius, playerMask);
        if (playerCollider != null)
        {
            Vector2 targetPos = playerCollider.transform.position;
            RaycastHit2D wallHit = Physics2D.Linecast(eyePos, targetPos, wallMask);

            Gizmos.color = wallHit.collider == null ? Color.red : Color.green;
            Gizmos.DrawLine(eyePos, targetPos);

            if (wallHit.collider != null)
                Gizmos.DrawWireSphere(wallHit.point, 0.15f); // onde bateu
        }
    }
}
}