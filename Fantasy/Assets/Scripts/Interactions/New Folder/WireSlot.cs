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

    public void SetColor(Color color)
    {
        if (image == null) image = GetComponent<Image>();
        if (image != null) image.color = color;
    }
}