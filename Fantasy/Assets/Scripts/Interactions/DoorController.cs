using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class DoorController : MonoBehaviour
{
    [Header("Door")]
    [SerializeField] private string buttonTag = "PuzzleButton";
    [SerializeField] private UnityEvent onDoorOpened;
    [SerializeField] private ShadowCaster2D shadow;

    public Sprite openedDoor;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    private readonly List<ButtonInteractable> buttons = new List<ButtonInteractable>();
    private bool isOpen;

    public bool IsOpen => isOpen;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        shadow = GetComponent<ShadowCaster2D>();
    }

    private void Start()
    {
        FindButtons();

        foreach (var button in buttons)
            button.OnPressedChanged += HandleButtonChanged;

        CheckAllPressed();
    }

    private void FindButtons()
    {
        GameObject[] found = GameObject.FindGameObjectsWithTag(buttonTag);

        buttons.Clear();
        foreach (var go in found)
        {
            if (go.TryGetComponent(out ButtonInteractable button))
                buttons.Add(button);
        }
    }

    private void HandleButtonChanged(ButtonInteractable button)
    {
        CheckAllPressed();
    }

    private void CheckAllPressed()
    {
        if (isOpen || buttons.Count == 0) return;

        foreach (var button in buttons)
        {
            if (!button.IsPressed) return;
        }

        Open();
    }

    private void Open()
    {
        if (isOpen) return;

        spriteRenderer.sprite = openedDoor;

        if (boxCollider != null)
            boxCollider.enabled = false;

        if (shadow != null)
            shadow.enabled = false;
            
        isOpen = true;
        onDoorOpened?.Invoke();
    }

    private void OnDestroy()
    {
        foreach (var button in buttons)
        {
            if (button != null)
                button.OnPressedChanged -= HandleButtonChanged;
        }
    }
}