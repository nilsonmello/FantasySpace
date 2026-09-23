using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class DistanceScaleIndicator : MonoBehaviour
{
    [Header("Auto-find")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string targetTag = "Centipide";

    [Header("Distance")]
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 8f;

    [Header("Scale")]
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 1.5f;
    [SerializeField] private bool closerIsBigger = true;

    [Header("Smooth")]
    [SerializeField] private float smoothSpeed = 5f;

    private Transform player;
    private Transform target;
    private RectTransform rectTransform;
    private float currentScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        currentScale = minScale;
    }

    private void Start()
    {
        FindPlayer();
        FindTarget();
    }

    private void Update()
    {
        if (player == null) FindPlayer();
        if (target == null) FindTarget();

        if (player == null || target == null) return;

        float distance = Vector3.Distance(player.position, target.position);
        float t = Mathf.InverseLerp(minDistance, maxDistance, distance);

        if (closerIsBigger)
            t = 1f - t;

        float targetScale = Mathf.Lerp(minScale, maxScale, t);

        currentScale = Mathf.MoveTowards(currentScale, targetScale, smoothSpeed * Time.deltaTime);
        rectTransform.localScale = Vector3.one * currentScale;
    }

    private void FindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null) player = go.transform;
    }

    private void FindTarget()
    {
        var go = GameObject.FindGameObjectWithTag(targetTag);
        if (go != null) target = go.transform;
    }
}