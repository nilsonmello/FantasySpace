using UnityEngine;
using UnityEngine.InputSystem;

// coloca esse script em qualquer GameObject persistente (ex: GameManager, Player)
// e arrasta o seu Input Actions Asset e o nome do map que quer ativar
public class InputMapActivator : MonoBehaviour
{
    // arraste aqui o seu .inputactions asset
    [SerializeField] private InputActionAsset inputActions;

    // nome exato do Action Map que quer ativar, ex: "Gameplay"
    [SerializeField] private string actionMapName = "Gameplay";

    private void Awake()
    {
        ActivateMap(actionMapName);
    }

    // pode chamar isso de outros scripts também, ex: ao fechar um menu de pause
    public void ActivateMap(string mapName)
    {
        if (inputActions == null)
        {
            Debug.LogWarning("InputMapActivator: nenhum InputActionAsset atribuído.");
            return;
        }

        InputActionMap map = inputActions.FindActionMap(mapName, throwIfNotFound: false);

        if (map == null)
        {
            Debug.LogWarning($"InputMapActivator: Action Map '{mapName}' não encontrado no asset.");
            return;
        }

        // desativa os outros maps pra evitar inputs conflitantes (ex: UI e Gameplay ao mesmo tempo)
        foreach (var otherMap in inputActions.actionMaps)
        {
            if (otherMap != map)
            {
                otherMap.Disable();
            }
        }

        map.Enable();
    }

    // desativa o map atual, útil ao abrir um menu de pause por exemplo
    public void DeactivateMap(string mapName)
    {
        if (inputActions == null) return;

        InputActionMap map = inputActions.FindActionMap(mapName, throwIfNotFound: false);
        map?.Disable();
    }
}