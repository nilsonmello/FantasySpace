using UnityEngine;

[RequireComponent(typeof(WorldBar))]
public class StaminaBarView : MonoBehaviour
{
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color exhaustedColor = new Color(0.9f, 0.25f, 0.25f);
    [SerializeField] private float searchInterval = 0.5f;

    private WorldBar bar;
    private PlayerMovementBase player;
    private PlayerHideState hideState;
    private float nextSearchTime;

    private void Awake()
    {
        bar = GetComponent<WorldBar>();
        bar.SetSuppressed(true);
    }

    private void Update()
    {
        if (player == null && !ResolvePlayer())
            return;

        bar.SetValue(player.StaminaNormalized);
        bar.SetColor(player.IsExhausted ? exhaustedColor : normalColor);
        bar.SetSuppressed(hideState != null && hideState.IsHidden);
    }

    private bool ResolvePlayer()
    {
        bar.SetSuppressed(true);

        player = GetComponentInParent<PlayerMovementBase>();

        if (player == null && Time.unscaledTime >= nextSearchTime)
        {
            nextSearchTime = Time.unscaledTime + searchInterval;
            player = FindFirstObjectByType<PlayerMovementBase>();
        }

        if (player == null) return false;

        hideState = player.GetComponent<PlayerHideState>();

        if (bar.Target == null)
            bar.SetTarget(player.transform);

        return true;
    }
}