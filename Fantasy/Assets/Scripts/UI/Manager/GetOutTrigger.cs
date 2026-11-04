using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class GetOutTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    [Header("Input")]
    [SerializeField] private InputActionReference interactAction;

    [SerializeField] private GameObject promptObject;

    private bool playerInside;
    private bool used;
    private PlayerMovementBase playerMovement;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        if (promptObject != null) promptObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used || !other.CompareTag(playerTag)) return;

        playerInside = true;
        playerMovement = other.GetComponentInParent<PlayerMovementBase>();

        if (promptObject != null) promptObject.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = false;
        if (promptObject != null) promptObject.SetActive(false);
    }

    private void Update()
    {
        if (!playerInside || used) return;
        if (UIManager.Instance == null || UIManager.Instance.paused || UIManager.Instance.isGameOver) return;

        if (!InteractPressed()) return;

        used = true;
        if (promptObject != null) promptObject.SetActive(false);

        if (playerMovement != null) playerMovement.SetMovementLocked(true);

        UIManager.Instance.RestartLevel();
    }

    private bool InteractPressed()
    {
        if (interactAction != null && interactAction.action != null)
            return interactAction.action.WasPressedThisFrame();

        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
    }
}