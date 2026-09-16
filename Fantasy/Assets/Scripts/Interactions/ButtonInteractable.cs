using UnityEngine;
using UnityEngine.Events;

public class ButtonInteractable : InteractionManager
{
    [Header("Button")]
    [SerializeField] private UnityEvent onPressed;

    public bool IsPressed { get; private set; }

    public event System.Action<ButtonInteractable> OnPressedChanged;

    protected override void Awake()
    {
        maxUses = 1;
        base.Awake();
    }

    protected override void OnInteract(GameObject interactor)
    {
        

        if (IsPressed) 
        {

        Debug.Log($"já apertado");
            
        return ;

        }

        Debug.Log($"{name}: botão pressionado");

        IsPressed = true;
        onPressed?.Invoke();
        OnPressedChanged?.Invoke(this);
    }
}