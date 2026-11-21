using UnityEngine;

/// <summary>
/// Base para qualquer "sinal" de funcionamento (luz, som, partícula...).
/// O TrapInteractable só chama Begin() e End(), sem saber o que cada sinal faz.
/// </summary>
public abstract class TrapSignal : MonoBehaviour
{
    public abstract void Begin();
    public abstract void End();
}
