using UnityEngine;
using UnityEngine.Events;

public class ButtonInteractable : InteractionManager
{
    [Header("Button")]
    [SerializeField] private UnityEvent onPressed;

    [Header("Puzzle")]
    [SerializeField] private WirePuzzle puzzle;

    public bool IsPressed { get; private set; }

    public event System.Action<ButtonInteractable> OnPressedChanged;

    public Color color;

    public SpriteRenderer renderer;

    protected override void Awake()
    {
        maxUses = puzzle != null ? -1 : 1;
        base.Awake();

        renderer = GetComponent<SpriteRenderer>();
    }

    public override bool Caninteract()
    {
        if (IsPressed) return false;
        if (puzzle != null && puzzle.IsOpen) return false;

        return base.Caninteract();
    }

    protected override void OnInteract(GameObject interactor)
    {
        if (IsPressed) return;

        if (puzzle != null)
            puzzle.Open(Press);
        else
            Press();
    }

    private void Press()
    {
        if (IsPressed) return;

        IsPressed = true;
        onPressed?.Invoke();
        OnPressedChanged?.Invoke(this);
        renderer.color = color;
    }
}