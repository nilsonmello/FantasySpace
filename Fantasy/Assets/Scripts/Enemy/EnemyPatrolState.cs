using UnityEngine;

/// <summary>
/// Comportamento de "busca": vai até LastKnownPlayerPosition (o ponto onde
/// o player foi visto pela última vez), espera lá por PatrolLookDuration
/// segundos (simulando o inimigo checando ao redor) e então volta pra Idle.
/// Se o player for avistado de novo nesse meio tempo, o EnemyStateMachine
/// interrompe esse estado e troca pra Chase antes mesmo de chegar no ponto.
/// </summary>
public class EnemyPatrolState : IEnemyState
{
    private bool _arrived;
    private float _lookTimer;

    public void Enter(EnemyStateMachine machine)
    {
        _arrived = false;
        _lookTimer = 0f;

        machine.Agent.isStopped = false;
        machine.Agent.SetDestination(machine.LastKnownPlayerPosition);
    }

    public void Tick(EnemyStateMachine machine)
    {
        if (!_arrived)
        {
            if (machine.Agent.pathPending) return;
            if (machine.Agent.remainingDistance > machine.Agent.stoppingDistance) return;

            _arrived = true;
            _lookTimer = machine.PatrolLookDuration;
            machine.Agent.isStopped = true;
            return;
        }

        _lookTimer -= Time.deltaTime;
        if (_lookTimer <= 0f)
        {
            machine.ChangeState(machine.IdleState);
        }
    }

    public void Exit(EnemyStateMachine machine)
    {
        machine.Agent.isStopped = false;
    }
}
