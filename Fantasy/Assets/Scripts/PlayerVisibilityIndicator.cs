using UnityEngine;

/// <summary>
/// Ícone acima da cabeça do player: um sprite quando alguma centopeia está vendo ele,
/// outro quando ninguém está. Coloque no prefab do player (como filho ou no próprio objeto).
/// As centopeias são procuradas em runtime, então podem nascer depois do player.
/// </summary>
public class PlayerVisibilityIndicator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("SpriteRenderer do ícone. Se vazio, é criado automaticamente.")]
    [SerializeField] private SpriteRenderer icon;
    [Tooltip("Objeto que o ícone acompanha. Se vazio, usa este transform.")]
    [SerializeField] private Transform followTarget;

    [Header("Sprites")]
    [SerializeField] private Sprite unseenSprite;
    [SerializeField] private Sprite seenSprite;

    [Header("Position")]
    [SerializeField] private Vector2 offset = new Vector2(0f, 1.2f);
    [SerializeField] private int sortingOrder = 200;
    [SerializeField] private string sortingLayerName = "Default";

    [Header("Behavior")]
    [Tooltip("Esconde o ícone enquanto não existe nenhuma centopeia na cena.")]
    [SerializeField] private bool hideWhenNoCreature = true;
    [SerializeField] private float findInterval = 0.5f;

    [Header("Pop (ao mudar de estado)")]
    [SerializeField] private float popScale = 1.3f;
    [SerializeField] private float popRecoverSpeed = 8f;

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

        // Posição no mundo: não herda rotação/flip do player
        Transform t = icon.transform;
        t.position = (Vector2)followTarget.position + offset;
        t.rotation = Quaternion.identity;
        t.localScale = baseScale * currentScale;
    }

    private void RefreshCreatures()
    {
        if (Time.time < nextFindTime) return;
        nextFindTime = Time.time + findInterval;

        creatures = FindObjectsByType<HeadStateMovement>(FindObjectsSortMode.None);
    }
}
