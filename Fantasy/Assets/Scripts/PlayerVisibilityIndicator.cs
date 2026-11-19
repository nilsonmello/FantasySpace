using UnityEngine;

public class PlayerVisibilityIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer icon;
    [SerializeField] private Transform followTarget;

    [Header("Sprites")]
    [SerializeField] private Sprite unseenSprite;
    [SerializeField] private Sprite seenSprite;

    [Header("Position")]
    [SerializeField] private Vector2 offset = new Vector2(0f, 1.2f);
    [SerializeField] private int sortingOrder = 200;
    [SerializeField] private string sortingLayerName = "Default";

    [Header("Behavior")]
    [SerializeField] private bool hideWhenNoCreature = true;
    [SerializeField] private float findInterval = 0.5f;

    [Header("Pop")]
    [SerializeField] private float popScale = 1.3f;
    [SerializeField] private float popRecoverSpeed = 8f;

    private Transform overrideTarget;
    private Vector2 overrideOffset;

    private HeadStateMovement[] creatures = new HeadStateMovement[0];
    private float nextFindTime;
    private bool wasSeen;
    private bool initialized;
    private float currentScale = 1f;
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        if (followTarget == null)
            followTarget = transform;

        if (icon == null)
        {
            GameObject go = new GameObject("VisibilityIcon");
            go.transform.SetParent(transform, false);
            icon = go.AddComponent<SpriteRenderer>();
        }

        icon.sortingLayerName = sortingLayerName;
        icon.sortingOrder = sortingOrder;
        baseScale = icon.transform.localScale;
    }

    private void LateUpdate()
    {
        RefreshCreatures();

        bool anyCreature = false;
        bool seen = false;

        for (int i = 0; i < creatures.Length; i++)
        {
            if (creatures[i] == null) continue;

            anyCreature = true;
            if (creatures[i].SeesPlayer)
            {
                seen = true;
                break;
            }
        }

        if (hideWhenNoCreature && !anyCreature)
        {
            icon.enabled = false;
            initialized = false;
            return;
        }

        icon.enabled = true;

        if (!initialized || seen != wasSeen)
        {
            icon.sprite = seen ? seenSprite : unseenSprite;

            if (initialized)
                currentScale = popScale;

            wasSeen = seen;
            initialized = true;
        }

        currentScale = Mathf.MoveTowards(currentScale, 1f, popRecoverSpeed * Time.deltaTime);

        Transform target = followTarget;
        Vector2 currentOffset = offset;

        if (overrideTarget != null)
        {
            target = overrideTarget;
            currentOffset = overrideOffset;
        }

        Transform t = icon.transform;
        t.position = (Vector2)target.position + currentOffset;
        t.rotation = Quaternion.identity;
        t.localScale = baseScale * currentScale;
    }

    public void SetFollowOverride(Transform target, Vector2 targetOffset)
    {
        overrideTarget = target;
        overrideOffset = targetOffset;
    }

    public void ClearFollowOverride()
    {
        overrideTarget = null;
    }

    private void RefreshCreatures()
    {
        if (Time.time < nextFindTime) return;
        nextFindTime = Time.time + findInterval;

        creatures = FindObjectsByType<HeadStateMovement>(FindObjectsSortMode.None);
    }
}