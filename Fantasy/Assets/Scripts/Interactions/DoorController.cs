using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DoorController : MonoBehaviour
{
    [Header("Door")]
    [SerializeField] private string buttonTag = "PuzzleButton";
    [SerializeField] private UnityEvent onDoorOpened;

    private readonly List<ButtonInteractable> buttons = new List<ButtonInteractable>();
    private bool isOpen;

    public bool IsOpen => isOpen;

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

        if (buttons.Count == 0)
            Debug.LogWarning($"{name}: nenhum botão com tag '{buttonTag}' encontrado na cena.");
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

        isOpen = true;
        onDoorOpened?.Invoke();
        gameObject.SetActive(false);
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