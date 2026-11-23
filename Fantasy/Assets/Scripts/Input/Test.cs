using UnityEngine;
using UnityEngine.InputSystem;

public class InputMapActivator : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    [SerializeField] private string actionMapName = "Gameplay";

    private void Awake()
    {
        ActivateMap(actionMapName);
    }

    public void ActivateMap(string mapName)
    {
        if (inputActions == null)
        {
            return;
        }

        InputActionMap map = inputActions.FindActionMap(mapName, throwIfNotFound: false);

        if (map == null)
        {
            return;
        }

        foreach (var otherMap in inputActions.actionMaps)
        {
            if (otherMap != map)
            {
                otherMap.Disable();
            }
        }

        map.Enable();
    }

    public void DeactivateMap(string mapName)
    {
        if (inputActions == null) return;

        InputActionMap map = inputActions.FindActionMap(mapName, throwIfNotFound: false);
        map?.Disable();
    }
}