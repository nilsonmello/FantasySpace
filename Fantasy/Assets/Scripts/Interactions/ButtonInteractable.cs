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

    private static WirePuzzle cachedPuzzle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cachedPuzzle = null;
    }

    protected override void Awake()
    {
        ResolvePuzzle();

        maxUses = puzzle != null ? -1 : 1;
        base.Awake();

        renderer = GetComponent<SpriteRenderer>();
    }

    private void ResolvePuzzle()
    {
        if (puzzle != null) return;

        if (cachedPuzzle == null)
        {
            cachedPuzzle = FindFirstObjectByType<WirePuzzle>(FindObjectsInactive.Include);

        }

        puzzle = cachedPuzzle;

        if (puzzle == null)
            Debug.LogWarning($"[{name}] Nenhum WirePuzzle na cena", this);
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

        ResolvePuzzle();

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