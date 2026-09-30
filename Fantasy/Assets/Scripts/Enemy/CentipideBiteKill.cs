using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CentipedeBiteKill : MonoBehaviour
{
    private enum Phase { Idle, Open, Snap, Cooldown }

    [Header("References")]
    [SerializeField] private MandibleController mandibles;
    [SerializeField] private HeadStateMovement headMovement;
    [SerializeField] private Transform biteOrigin;

    [Header("Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask detectionMask = ~0;
    [SerializeField] private float triggerRadius = 1.5f;
    [SerializeField] private float hitRadius = 0.7f;

    [Header("Mouth")]
    [SerializeField] private float wideOpenAmount = 1.6f;
    [SerializeField] private float openSmoothTime = 0.12f;
    [SerializeField] private float snapSmoothTime = 0.02f;

    [Header("Bite")]
    [SerializeField] private float snapDuration = 0.08f;
    [SerializeField] private float cooldown = 0.5f;

    [Header("Death")]
    [SerializeField] private float restartDelay = 0.1f;

    [Header("Gizmos")]
    [SerializeField] private bool drawGizmo = true;

    public event Action OnPlayerKilled;

    private Phase phase = Phase.Idle;
    private float phaseTimer;
    private PlayerHideState playerHideState;
    private bool killed;

    private void Reset()
    {
        mandibles = GetComponentInChildren<MandibleController>();
        if (mandibles == null) mandibles = GetComponentInParent<MandibleController>();
        headMovement = GetComponentInParent<HeadStateMovement>();
    }

    private void OnDisable()
    {
        if (mandibles != null)
            mandibles.ClearOpenOverride();
    }

    private Vector2 Origin => biteOrigin != null ? (Vector2)biteOrigin.position : (Vector2)transform.position;

    private void Update()
    {
        if (killed) return;

        if (headMovement != null && headMovement.CurrentState == HeadStateMovement.State.Hide)
        {
            if (phase != Phase.Idle) SetPhase(Phase.Idle);
            return;
        }

        switch (phase)
        {
            case Phase.Idle:
                if (FindPlayerInCircle(triggerRadius))
                    SetPhase(Phase.Open);
                break;

            case Phase.Open:
                if (!FindPlayerInCircle(triggerRadius))
                {
                    SetPhase(Phase.Idle);
                }
                else if (FindPlayerInCircle(hitRadius))
                {
                    SetPhase(Phase.Snap);
                }
                break;

            case Phase.Snap:
                phaseTimer -= Time.deltaTime;
                if (phaseTimer <= 0f)
                {
                    if (FindPlayerInCircle(hitRadius))
                    {
                        KillPlayer();
                        return;
                    }

                    SetPhase(Phase.Cooldown);
                }
                break;

            case Phase.Cooldown:
                phaseTimer -= Time.deltaTime;
                if (phaseTimer <= 0f)
                    SetPhase(Phase.Idle);
                break;
        }
    }

    private void SetPhase(Phase newPhase)
    {
        phase = newPhase;

        switch (newPhase)
        {
            case Phase.Idle:
                if (mandibles != null) mandibles.ClearOpenOverride();
                break;

            case Phase.Open:
                if (mandibles != null) mandibles.SetOpenOverride(wideOpenAmount, openSmoothTime);
                break;

            case Phase.Snap:
                phaseTimer = snapDuration;
                if (mandibles != null) mandibles.SetOpenOverride(0f, snapSmoothTime);
                break;

            case Phase.Cooldown:
                phaseTimer = cooldown;
                break;
        }
    }

    private bool FindPlayerInCircle(float radius)
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(Origin, radius, detectionMask);

        for (int i = 0; i < cols.Length; i++)
        {
            if (!IsPlayerCollider(cols[i])) continue;

            if (playerHideState == null)
                playerHideState = cols[i].GetComponentInParent<PlayerHideState>();

            if (playerHideState != null && playerHideState.IsHidden)
                return false;

            return true;
        }

        return false;
    }

    private bool IsPlayerCollider(Collider2D col)
    {
        for (Transform t = col.transform; t != null; t = t.parent)
        {
            if (t.CompareTag(playerTag))
                return true;
        }
        return false;
    }

    private void KillPlayer()
    {
        killed = true;
        OnPlayerKilled?.Invoke();

        if (restartDelay > 0f) Invoke(nameof(RestartScene), restartDelay);
        else RestartScene();
    }

    private void RestartScene()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.buildIndex >= 0)
        {
            SceneManager.LoadScene(scene.buildIndex);
            return;
        }

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#endif
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo) return;

        Vector3 o = biteOrigin != null ? biteOrigin.position : transform.position;

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
        Gizmos.DrawWireSphere(o, triggerRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(o, hitRadius);
    }
}