/// <summary>
/// Contrato de um estado da máquina de estados do inimigo.
/// Cada estado é responsável só pelo próprio comportamento;
/// as transições entre estados ficam centralizadas no EnemyStateMachine.
/// </summary>
public interface IEnemyState
{
    void Enter(EnemyStateMachine machine);
    void Tick(EnemyStateMachine machine);
    void Exit(EnemyStateMachine machine);
}
