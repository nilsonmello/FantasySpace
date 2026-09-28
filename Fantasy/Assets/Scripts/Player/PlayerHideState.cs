using UnityEngine;

public class PlayerHideState : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer[] renderersToHide;

    [Header("Light")]
    [SerializeField] private GameObject[] objectsToDisableWhenHidden;

    public bool IsHidden { get; private set; }

    private void Awake()
    {
        if (renderersToHide == null || renderersToHide.Length == 0)
            renderersToHide = GetComponentsInChildren<SpriteRenderer>(true);
    }

    public void SetHidden(bool hidden)
    {
        IsHidden = hidden;

        foreach (var sr in renderersToHide)
            sr.enabled = !hidden;

        foreach (var obj in objectsToDisableWhenHidden)
        {
            if (obj != null)
                obj.SetActive(!hidden);
        }
    }
}