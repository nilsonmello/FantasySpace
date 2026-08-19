using UnityEngine;

/// <summary>
/// Persegue LastKnownPlayerPosition, que é mantida atualizada pelo
/// EnemyStateMachine a cada evento OnPlayerSpotted. Recalcula o destino
/// em intervalos, em vez de todo frame, pra não sobrecarregar o NavMesh.
/// </summary>
public class EnemyChaseState : IEnemyState
{
    private const float RepathInterval = 0.15f;

    private float _repathTimer;

    public void Enter(EnemyStateMachine machine)
    {
        machine.Agent.isStopped = false;
        _repathTimer = 0f;
        machine.Agent.SetDestination(machine.LastKnownPlayerPosition);
    }

    public void Tick(EnemyStateMachine machine)
    {
        _repathTimer -= Time.deltaTime;
        if (_repathTimer > 0f) return;

        _repathTimer = RepathInterval;
        machine.Agent.SetDestination(machine.LastKnownPlayerPosition);
    }

    public void Exit(EnemyStateMachine machine)
    {
    }
}
