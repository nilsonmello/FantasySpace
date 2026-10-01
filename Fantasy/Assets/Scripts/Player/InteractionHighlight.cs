using UnityEngine;

public class InteractionHighlight : MonoBehaviour
{
    private static readonly int HighlightAmountId = Shader.PropertyToID("_HighlightAmount");

    [Header("References")]
    [SerializeField] private InteractionManager interactable;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerInteract player;

    [Header("Fade")]
    [SerializeField] private float fadeInSpeed = 8f;
    [SerializeField] private float fadeOutSpeed = 4f;

    [Header("Preview (opcional)")]
    [SerializeField] private float previewRadius = 0f;
    [SerializeField, Range(0f, 1f)] private float maxPreviewHighlight = 0.4f;

    [SerializeField] private PlayerHideState playerHideState;
    private MaterialPropertyBlock block;
    private float current;

    private void Awake()
    {
        block = new MaterialPropertyBlock();

        if (interactable == null) interactable = GetComponent<InteractionManager>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        Apply(0f);

        playerHideState = FindObjectOfType<PlayerHideState>();
    }

    private void Update()
    {
        Debug.Log(current);

        if (interactable == null || spriteRenderer == null) return;

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerInteract>();
            if (player == null) return;
        }

        HandleHidePlayer();
    }

    void HandleHidePlayer()
    {
        if(playerHideState == null) return;

        if(!playerHideState.IsHidden)
        {
            float target = ComputeTarget();
            if (Mathf.Approximately(current, target)) return;

            float speed = target > current ? fadeInSpeed : fadeOutSpeed;
            current = Mathf.MoveTowards(current, target, speed * Time.deltaTime);


            Apply(current);
        }
        else
        {
            current = 0;
            Apply(current);  
        }
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