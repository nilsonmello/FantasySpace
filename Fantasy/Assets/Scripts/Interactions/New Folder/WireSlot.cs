using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class WireSlot : MonoBehaviour
{
    [SerializeField] private RectTransform snapPoint;

    private Image image;

    public RectTransform Rect => (RectTransform)transform;
    public Vector3 SnapPosition => snapPoint != null ? snapPoint.position : transform.position;

    private void Awake() => image = GetComponent<Image>();

    public void SetVisual(Sprite sprite, Color fallbackColor)
    {
        if (image == null) image = GetComponent<Image>();
        if (image == null) return;

        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
        }
        else
        {
            image.color = fallbackColor;
        }
    }
}