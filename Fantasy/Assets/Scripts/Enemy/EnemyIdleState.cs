public class EnemyIdleState : IEnemyState
{
    public void Enter(EnemyStateMachine machine)
    {
        machine.Agent.isStopped = true;
        machine.Agent.ResetPath();
    }

    public void Tick(EnemyStateMachine machine)
    {
    }

    public void Exit(EnemyStateMachine machine)
    {
        machine.Agent.isStopped = false;
    }
}