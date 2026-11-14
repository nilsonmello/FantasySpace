using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WireSortingOverride : MonoBehaviour
{
    [SerializeField] private int orderOffset = WireSorting.Image;

    [SerializeField] private bool needsRaycaster = false;

    private void Awake()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogWarning($"{name}: nenhum canvas pai", this);
            return;
        }

        Canvas root = parentCanvas.rootCanvas;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingLayerID = root.sortingLayerID;
        canvas.sortingOrder = root.sortingOrder + orderOffset;

        if (needsRaycaster)
        {
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }
        else
        {
            Graphic graphic = GetComponent<Graphic>();
            if (graphic != null) graphic.raycastTarget = false;
        }
    }
}