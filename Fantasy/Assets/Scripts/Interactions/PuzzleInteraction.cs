// using UnityEngine;
// using UnityEngine.Events;

// public class PuzzleInteractions : InteractionManager
// {
//     [Header("Button")]
//     [SerializeField] private UnityEvent onPressed;
//     [SerializeField] private Canvas puzzleCanvas;


//     public bool IsPressed { get; private set; }
//     public event System.Action<ButtonInteractable> OnPressedChanged;

//     protected override void Awake()
//     {
//         maxUses = -1;
//         base.Awake();

//         renderer = GetComponent<SpriteRenderer>();
//     }

//     protected override void OnInteract(GameObject interactor)
//     {
//         if (IsPressed) return;

//         IsPressed = true;
//         onPressed?.Invoke();
//         OnPressedChanged?.Invoke(this);
//     }
// }