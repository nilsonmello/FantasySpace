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

    private VisionActivatedObject vision;
    private bool pinnedByUs;

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
        vision = GetComponentInParent<VisionActivatedObject>();
    }

    private void OnDestroy()
    {
        ReleaseVision();
    }

    private void ResolvePuzzle()
    {
        if (puzzle != null) return;

        if (cachedPuzzle == null)
        {
            cachedPuzzle = FindFirstObjectByType<WirePuzzle>(FindObjectsInactive.Include);

        }

        puzzle = cachedPuzzle;
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

        if (puzzle == null)
        {
            Press();
            return;
        }

        HoldVision();
        puzzle.Open(Press);

        if (!puzzle.IsOpen)
            ReleaseVision();
    }

    private void HoldVision()
    {
        if (pinnedByUs || puzzle == null) return;

        pinnedByUs = true;
        puzzle.Closed += OnPuzzleClosed;

        if (vision != null) vision.Pin();
    }

    private void ReleaseVision()
    {
        if (!pinnedByUs) return;
        pinnedByUs = false;

        if (puzzle != null) puzzle.Closed -= OnPuzzleClosed;
        if (vision != null) vision.Unpin();
    }

    private void OnPuzzleClosed() => ReleaseVision();

    private void Press()
    {
        if (IsPressed) return;

        IsPressed = true;
        onPressed?.Invoke();
        OnPressedChanged?.Invoke(this);
        renderer.color = color;
    }
}