using UnityEngine;

public class HeadStateMovement : MonoBehaviour
{
    public enum State
    {
        Wander,
        Chase,
        Hide
    }

    [Header("Current State")]
    [SerializeField] private State currentState = State.Wander;
    public State CurrentState => currentState;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private BodyChainController bodyChain;

    [Header("Wander")]
    [SerializeField] private float wanderSpeed = 3f;
    [SerializeField] private float changeDirectionSmoothness = 1f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float chaseStopDistance = 0.3f;
    [SerializeField] private bool chaseMouseTarget = false;
    private Camera cachedCamera;

    [Header("Hide")]
    [SerializeField] private float hideCompressionRadius = 0.5f;
    [SerializeField] private float hideBreatheAmplitude = 0.05f;
    [SerializeField] private float hideBreatheSpeed = 2f;
    private Vector2 hideCenter;

    [Header("Limits")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 boundsCenter = Vector2.zero;
    [SerializeField] private Vector2 boundsSize = new Vector2(10f, 10f);

    [Header("Rotation")]
    [SerializeField] private bool rotateTowardsMovement = true;
    [SerializeField] private float rotationSpeed = 10f;

    private float noiseOffsetX;
    private float noiseOffsetY;
    private Vector2 currentVelocity;
    private State previousState;

    private void Awake()
    {
        noiseOffsetX = Random.Range(0f, 1000f);
        noiseOffsetY = Random.Range(0f, 1000f);
        cachedCamera = Camera.main;
        previousState = currentState;
        EnterState(currentState);
    }

    private void Update()
    {
        if (currentState != previousState)
        {
            ExitState(previousState);
            EnterState(currentState);
            previousState = currentState;
        }

        switch (currentState)
        {
            case State.Wander:
                UpdateWander();
                break;
            case State.Chase:
                UpdateChase();
                break;
            case State.Hide:
                UpdateHide();
                break;
        }

        if (bodyChain != null)
            bodyChain.SetMoving(currentVelocity.sqrMagnitude > 0.0001f);

        if (useBounds)
        {
            Vector2 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, boundsCenter.x - boundsSize.x / 2f, boundsCenter.x + boundsSize.x / 2f);
            pos.y = Mathf.Clamp(pos.y, boundsCenter.y - boundsSize.y / 2f, boundsCenter.y + boundsSize.y / 2f);
            transform.position = pos;
        }

        if (rotateTowardsMovement && currentVelocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(currentVelocity.y, currentVelocity.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    public void SetState(State newState)
    {
        currentState = newState;
    }

    private void EnterState(State state)
    {
        if (state == State.Hide)
        {
            hideCenter = transform.position;

            if (bodyChain != null)
                bodyChain.SetCompression(true, hideCenter, hideCompressionRadius);
        }
    }

    private void ExitState(State state)
    {
        if (state == State.Hide)
        {
            if (bodyChain != null)
                bodyChain.SetCompression(false, Vector2.zero, 0f);
        }
    }

    private void UpdateWander()
    {
        float t = Time.time * changeDirectionSmoothness;
        float dirX = Mathf.PerlinNoise(t, noiseOffsetX) * 2f - 1f;
        float dirY = Mathf.PerlinNoise(t, noiseOffsetY) * 2f - 1f;

        Vector2 direction = new Vector2(dirX, dirY);
        currentVelocity = direction * wanderSpeed;
        transform.position = (Vector2)transform.position + currentVelocity * Time.deltaTime;
    }

    private void UpdateChase()
    {
        if (!TryGetChaseTarget(out Vector2 targetPos))
        {
            currentVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = targetPos - (Vector2)transform.position;
        float dist = toTarget.magnitude;

        if (dist > chaseStopDistance)
        {
            Vector2 dir = toTarget / dist;
            currentVelocity = dir * chaseSpeed;
            transform.position = (Vector2)transform.position + currentVelocity * Time.deltaTime;
        }
        else
        {
            currentVelocity = Vector2.zero;
        }
    }

    private bool TryGetChaseTarget(out Vector2 targetPos)
    {
        if (chaseMouseTarget)
        {
            if (cachedCamera == null)
                cachedCamera = Camera.main;

            if (cachedCamera == null)
            {
                targetPos = Vector2.zero;
                return false;
            }

            Vector3 mouseWorld = cachedCamera.ScreenToWorldPoint(Input.mousePosition);
            targetPos = new Vector2(mouseWorld.x, mouseWorld.y);
            return true;
        }

        if (player == null)
        {
            targetPos = Vector2.zero;
            return false;
        }

        targetPos = player.position;
        return true;
    }

    private void UpdateHide()
    {
        float breathe = Mathf.Sin(Time.time * hideBreatheSpeed) * hideBreatheAmplitude;
        Vector2 pos = hideCenter + Vector2.up * breathe;

        currentVelocity = (pos - (Vector2)transform.position) / Mathf.Max(Time.deltaTime, 0.0001f);
        transform.position = pos;

        if (bodyChain != null)
            bodyChain.UpdateCompressionCenter(pos);
    }

    private void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
        }

        if (currentState == State.Hide && Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(hideCenter, hideCompressionRadius);
        }
    }
}