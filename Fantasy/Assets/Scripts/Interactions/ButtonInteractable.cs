using UnityEngine;
using UnityEngine.Events;

public class ButtonInteractable : InteractionManager
{
    [Header("Button")]
    [SerializeField] private UnityEvent onPressed;

    public bool IsPressed { get; private set; }

    public event System.Action<ButtonInteractable> OnPressedChanged;

    public Color color;

    public SpriteRenderer renderer;

    protected override void Awake()
    {
        maxUses = 1;
        base.Awake();

        renderer = GetComponent<SpriteRenderer>();
    }

    protected override void OnInteract(GameObject interactor)
    {
        if (IsPressed) return;

        IsPressed = true;
        onPressed?.Invoke();
        OnPressedChanged?.Invoke(this);
        renderer.color = color;
    }
}