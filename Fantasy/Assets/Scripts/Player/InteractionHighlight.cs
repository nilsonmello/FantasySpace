using UnityEngine;

public class InteractionHighlight : MonoBehaviour
{
    private static readonly int HighlightAmountId = Shader.PropertyToID("_HighlightAmount");

    [Header("References")]
    [SerializeField] private InteractionManager interactable;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerInteract player;
    [SerializeField] private PlayerHideState playerHideState;

    [Header("Fade")]
    [SerializeField] private float fadeInSpeed = 8f;
    [SerializeField] private float fadeOutSpeed = 4f;

    [Header("Preview")]
    [SerializeField] private float previewRadius = 0f;
    [SerializeField, Range(0f, 1f)] private float maxPreviewHighlight = 0.4f;

    [Header("Auto Find")]
    [SerializeField] private float findRetryInterval = 0.5f;

    private MaterialPropertyBlock block;
    private float current;
    private float nextFindTime;

    private void Awake()
    {
        block = new MaterialPropertyBlock();

        if (interactable == null) interactable = GetComponent<InteractionManager>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            Apply(0f);

        TryFindReferences();
    }

    private void Update()
    {
        if (interactable == null || spriteRenderer == null) return;

        if (player == null || playerHideState == null)
        {
            if (Time.time >= nextFindTime)
            {
                nextFindTime = Time.time + findRetryInterval;
                TryFindReferences();
            }

            if (player == null) return;
        }

        HandleHidePlayer();
    }

    private void TryFindReferences()
    {
        if (player == null)
            player = FindAnyObjectByType<PlayerInteract>();

        if (playerHideState == null)
        {
            if (player != null)
            {
                playerHideState = player.GetComponentInParent<PlayerHideState>();
                if (playerHideState == null)
                    playerHideState = player.GetComponentInChildren<PlayerHideState>();
            }

            if (playerHideState == null)
                playerHideState = FindAnyObjectByType<PlayerHideState>();
        }
    }

    private void HandleHidePlayer()
    {
        bool hidden = playerHideState != null && playerHideState.IsHidden;

        if (hidden)
        {
            if (current != 0f)
            {
                current = 0f;
                Apply(current);
            }
            return;
        }

        float target = ComputeTarget();
        if (Mathf.Approximately(current, target)) return;

        float speed = target > current ? fadeInSpeed : fadeOutSpeed;
        current = Mathf.MoveTowards(current, target, speed * Time.deltaTime);

        Apply(current);
    }

    private float ComputeTarget()
    {
        if (!interactable.Caninteract()) return 0f;

        if (player.CurrentTarget == interactable) return 1f;

        if (previewRadius > interactable.interactionRange)
        {
            float dist = Vector2.Distance(player.transform.position, transform.position);
            if (dist < previewRadius)
                return Mathf.InverseLerp(previewRadius, interactable.interactionRange, dist) * maxPreviewHighlight;
        }

        return 0f;
    }

    private void Apply(float amount)
    {
        spriteRenderer.GetPropertyBlock(block);
        block.SetFloat(HighlightAmountId, amount);
        spriteRenderer.SetPropertyBlock(block);
    }
}