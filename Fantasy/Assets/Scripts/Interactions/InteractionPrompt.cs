using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(InteractionManager))]
public class InteractionPrompt : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private string keyLabel = "E";
    [SerializeField] private string format = "[{0}]";
    [SerializeField] private float fontSize = 4f;
    [SerializeField] private Color color = Color.white;
    [SerializeField] private TMP_FontAsset font;

    [Header("Position")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private float bobAmount = 0.05f;
    [SerializeField] private float bobSpeed = 4f;

    [Header("Proximity")]
    [SerializeField] private float fadeDistance = 1.5f;
    [SerializeField] private float fadeSpeed = 8f;
    [SerializeField] private bool onlyClosest = true;

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 100;

    private static readonly List<InteractionPrompt> all = new List<InteractionPrompt>();
    private static Transform player;
    private static InteractionPrompt closest;
    private static int closestFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        all.Clear();
        player = null;
        closest = null;
        closestFrame = -1;
    }

    private InteractionManager interactable;
    private TextMeshPro label;
    private float alpha;

    private void Awake()
    {
        interactable = GetComponent<InteractionManager>();
        CreateLabel();
    }

    private void OnEnable()
    {
        if (!all.Contains(this)) all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
        alpha = 0f;
        if (label != null) label.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (label != null) Destroy(label.gameObject);
    }

    private void CreateLabel()
    {
        GameObject go = new GameObject($"{name}_Prompt");
        label = go.AddComponent<TextMeshPro>();

        if (font != null) label.font = font;

        label.text = string.Format(format, keyLabel);
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
        label.sortingOrder = sortingOrder;
        label.alpha = 0f;

        go.SetActive(false);
    }

    private void LateUpdate()
    {
        if (label == null) return;

        alpha = Mathf.MoveTowards(alpha, GetTargetAlpha(), fadeSpeed * Time.unscaledDeltaTime);

        bool visible = alpha > 0.01f;
        if (label.gameObject.activeSelf != visible)
            label.gameObject.SetActive(visible);

        if (!visible) return;

        label.alpha = alpha;

        float bob = Mathf.Sin(Time.unscaledTime * bobSpeed) * bobAmount;
        label.transform.position = transform.position + offset + new Vector3(0f, bob, 0f);
        label.transform.rotation = Quaternion.identity;
    }

    private float GetTargetAlpha()
    {
        if (!CanShow(out float distance)) return 0f;
        if (onlyClosest && GetClosest() != this) return 0f;

        float range = interactable.interactionRange;
        if (distance <= range) return 1f;

        return Mathf.InverseLerp(range + fadeDistance, range, distance);
    }

    private bool CanShow(out float distance)
    {
        distance = float.MaxValue;

        if (!interactable.PromptVisible) return false;
        if (!interactable.Caninteract()) return false;
        if (!ResolvePlayer()) return false;

        distance = Vector2.Distance(transform.position, player.position);
        return distance <= interactable.interactionRange + fadeDistance;
    }

    private static bool ResolvePlayer()
    {
        if (player != null) return true;

        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;

        return player != null;
    }

    private static InteractionPrompt GetClosest()
    {
        if (closestFrame == Time.frameCount) return closest;

        closestFrame = Time.frameCount;
        closest = null;
        float best = float.MaxValue;

        foreach (var p in all)
        {
            if (p == null || !p.CanShow(out float d)) continue;

            if (d < best)
            {
                best = d;
                closest = p;
            }
        }

        return closest;
    }
}