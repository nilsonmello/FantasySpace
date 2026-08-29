using System;
using UnityEngine;

public class HeadStateMovement : MonoBehaviour
{
    public enum State
    {
        Wander,
        Chase,
        Hide,
        Patrol
    }

    [Header("Current State")]
    [SerializeField] private State currentState = State.Wander;
    public State CurrentState => currentState;

    public event Action<State> OnStateChanged;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private BodyChainController bodyChain;

    [Header("Vision / Detection")]
    [SerializeField] private float visionRadius = 6f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private bool drawVisionGizmo = true;
    private Vector2 lastKnownPlayerPos;
    private bool hasLastKnownPos;

    [Header("Wander")]
    [SerializeField] private float wanderSpeed = 3f;
    [SerializeField] private float changeDirectionSmoothness = 1f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float chaseStopDistance = 0.3f;

    [Header("Hide")]
    [SerializeField] private float hideCompressionRadius = 0.5f;
    [SerializeField] private float hideBreatheAmplitude = 0.05f;
    [SerializeField] private float hideBreatheSpeed = 2f;
    private Vector2 hideCenter;

    [Header("Patrol")]
    [SerializeField] private float patrolSpeed = 3.5f;
    [SerializeField] private float patrolSearchRadius = 3f;
    [SerializeField] private int patrolPointCount = 3;
    [SerializeField] private float patrolPointArriveDistance = 0.2f;
    [SerializeField] private float patrolWaitTime = 1f;
    private Vector2 patrolCenter;
    private Vector2 patrolTarget;
    private int patrolPointsVisited;
    private bool patrolWaiting;
    private float patrolWaitTimer;

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


    private Transform backPoint;


    private void Awake()
    {
        noiseOffsetX = UnityEngine.Random.Range(0f, 1000f);
        noiseOffsetY = UnityEngine.Random.Range(0f, 1000f);
        previousState = currentState;
        EnterState(currentState);
        OnStateChanged?.Invoke(currentState);

    }

    private void Update()
    {
        if (backPoint == null)
        {
            backPoint = bodyChain.lastSegment;
        }

        if (currentState != State.Hide)
            UpdateDetection();

        if (currentState != previousState)
        {
            ExitState(previousState);
            EnterState(currentState);
            previousState = currentState;
            OnStateChanged?.Invoke(currentState);
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
            case State.Patrol:
                UpdatePatrol();
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
        else if (state == State.Patrol)
        {
            patrolCenter = hasLastKnownPos ? lastKnownPlayerPos : (Vector2)transform.position;
            patrolPointsVisited = 0;
            patrolWaiting = false;
            patrolTarget = PickNewPatrolPoint();
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

    private void UpdateDetection()
    {
        if (CanSeePlayer() || BackVision())
        {
            lastKnownPlayerPos = player.position;
            hasLastKnownPos = true;

            if (currentState != State.Chase)
                SetState(State.Chase);
        }
    }

    private bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector2 origin = transform.position;
        Vector2 toPlayer = (Vector2)player.position - origin;
        float dist = toPlayer.magnitude;

        if (dist > visionRadius)
            return false;

        if (dist < 0.0001f)
            return true;

        RaycastHit2D hit = Physics2D.Raycast(origin, toPlayer / dist, dist, obstacleMask);
        return hit.collider == null;
    }

    private bool BackVision()
    {
        Vector2 backOrigin = backPoint.position;
        Vector2 toPlayer = (Vector2)player.position - backOrigin;
        float dist = toPlayer.magnitude;

        if (dist > visionRadius)
            return false;

        if (dist < 0.0001f)
            return true;

        RaycastHit2D hit = Physics2D.Raycast(backOrigin, toPlayer / dist, dist, obstacleMask);
        return hit.collider == null;
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
        bool visible = CanSeePlayer();
        if (!hasLastKnownPos)
        {
            currentVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = lastKnownPlayerPos - (Vector2)transform.position;
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

            if (!visible)
                SetState(State.Patrol);
        }
    }

    private void UpdatePatrol()
    {
        currentVelocity = Vector2.zero;

        if (patrolWaiting)
        {
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0f)
            {
                patrolWaiting = false;
                patrolPointsVisited++;

                if (patrolPointsVisited >= patrolPointCount)
                {
                    SetState(State.Wander);
                    return;
                }

                patrolTarget = PickNewPatrolPoint();
            }
            return;
        }

        Vector2 toTarget = patrolTarget - (Vector2)transform.position;
        float dist = toTarget.magnitude;

        if (dist > patrolPointArriveDistance)
        {
            Vector2 dir = toTarget / dist;
            currentVelocity = dir * patrolSpeed;
            transform.position = (Vector2)transform.position + currentVelocity * Time.deltaTime;
        }
        else
        {
            patrolWaiting = true;
            patrolWaitTimer = patrolWaitTime;
        }
    }

    private Vector2 PickNewPatrolPoint()
    {
        Vector2 offset = UnityEngine.Random.insideUnitCircle * patrolSearchRadius;
        return patrolCenter + offset;
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

    private void OnDrawGizmos()
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

        if (drawVisionGizmo)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, visionRadius);

            if (backPoint != null)
            Gizmos.DrawWireSphere(backPoint.position, visionRadius);
        }

        if (Application.isPlaying && hasLastKnownPos && (currentState == State.Chase || currentState == State.Patrol))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastKnownPlayerPos, 0.2f);

            if (currentState == State.Patrol)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
                Gizmos.DrawWireSphere(patrolCenter, patrolSearchRadius);
            }
        }
    }
}