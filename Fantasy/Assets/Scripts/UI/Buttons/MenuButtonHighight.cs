using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MenuButtonHighLight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Textos")]
    [SerializeField] private TMP_Text[] textos;

    [Header("Formas")]
    [SerializeField] private Image[] formas;

    [SerializeField] private Image quadradoBranco;

    [Header("Materiais para textos")]
    [SerializeField] private Material materialTextoNormal;
    [SerializeField] private Material materialTextoStencil;

    [Header("Materiais para formas")]
    [SerializeField] private Material materialFormaNormal;
    [SerializeField] private Material materialFormaStencil;

    private void Awake()
    {
        if (textos.Length > 0 && textos[0].font != null)
        {
            materialTextoStencil.mainTexture = textos[0].font.atlasTexture;
        }

        ResetHighlight();
    }

    private void OnDisable()
    {
        ResetHighlight();
    }

    // --- MOUSE ---

    public void OnPointerEnter(PointerEventData eventData)
    {
        UIManager.Instance.isUsingMouse = true;
        
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }
        
        ApplyHighlight();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        
        ResetHighlight();
    }

    // --- GAMEPAD / TECLADO ---

    public void OnSelect(BaseEventData eventData)
    {
        ApplyHighlight();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        ResetHighlight();
    }

    protected virtual void ApplyHighlight()
    {
        foreach (var texto in textos)
        {
            if (texto != null) texto.fontMaterial = materialTextoStencil;
        }

        foreach (var forma in formas)
        {
            if (forma != null) forma.material = materialFormaStencil;
        }

        if (quadradoBranco != null) quadradoBranco.enabled = true;
    }

    protected virtual void ResetHighlight()
    {
        foreach (var texto in textos)
        {
            if (texto != null) texto.fontMaterial = materialTextoNormal;
        }

        foreach (var forma in formas)
        {
            if (forma != null) forma.material = materialFormaNormal;
        }

        if (quadradoBranco != null) quadradoBranco.enabled = false;
    }
}