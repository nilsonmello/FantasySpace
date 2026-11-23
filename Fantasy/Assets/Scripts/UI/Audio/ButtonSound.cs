using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ButtonSound : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    [SerializeField] private string hoverSound = "UI_Hover";
    [SerializeField] private string clickSound = "UI_Click";

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float hoverVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 1f;

    [SerializeField] private float ignoreSelectAfterEnable = 0.15f;

    private Button button;
    private Selectable selectable;
    private float enabledAt;

    private void Awake()
    {
        button = GetComponent<Button>();
        selectable = GetComponent<Selectable>();
    }

    private void OnEnable()
    {
        enabledAt = Time.unscaledTime;

        if (button != null)
            button.onClick.AddListener(PlayClick);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayClick);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (Time.unscaledTime - enabledAt < ignoreSelectAfterEnable) return;

        PlayHover();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (selectable != null && !selectable.IsInteractable()) return;

        PlayHover();
    }

    private void PlayHover()
    {
        if (string.IsNullOrEmpty(hoverSound) || SoundManager.Instance == null) return;
        SoundManager.Instance.PlayUI(hoverSound, hoverVolume);
    }

    private void PlayClick()
    {
        if (string.IsNullOrEmpty(clickSound) || SoundManager.Instance == null) return;
        SoundManager.Instance.PlayUI(clickSound, clickVolume);
    }
}