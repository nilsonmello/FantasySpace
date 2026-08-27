using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionBlock : MonoBehaviour
{
    private InputSystem_Actions InputSystem;
    private InputAction interact;
    void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player") && interact.WasPressedThisFrame()) {
        // Ativa um botão, abre uma porta, etc.
        Debug.Log("Interagiu");
        }
    }
    void Awake()
    {
        InputSystem = new InputSystem_Actions();
    }
    void OnEnable()
    {
        interact = InputSystem.Player.Interact;
        interact.Enable();
    }
    void Disable()
    {
        interact.Disable();
    }
}

