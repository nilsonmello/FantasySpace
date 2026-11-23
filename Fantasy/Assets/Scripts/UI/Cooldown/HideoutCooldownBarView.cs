using UnityEngine;

[RequireComponent(typeof(WorldBar))]
public class HideoutCooldownBarView : MonoBehaviour
{
    [SerializeField] private HideoutInteractable hideout;

    private WorldBar bar;

    private void Awake()
    {
        bar = GetComponent<WorldBar>();
    }

    private void Start()
    {
        if (hideout == null)
            hideout = GetComponentInParent<HideoutInteractable>();

        if (hideout == null)
        {
            enabled = false;
            return;
        }

        if (bar.Target == null)
            bar.SetTarget(hideout.transform);
    }

    private void Update()
    {
        bar.SetValue(hideout.CooldownNormalized);
    }
}
