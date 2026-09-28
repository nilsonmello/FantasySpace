using UnityEngine;

public class PlayerHideState : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] renderersToHide;

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
    }
}