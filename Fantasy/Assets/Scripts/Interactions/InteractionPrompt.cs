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

    [Header("Description")]
    [SerializeField] private float descriptionFontSize = 2.5f;
    [SerializeField] private Color descriptionColor = Color.white;
    [SerializeField] private float descriptionOffsetY = 0.35f;

    [Header("Position")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.4f, 0f);
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
    private TextMeshPro descriptionLabel;
    private string currentDescription;
    private float alpha;

    private void Awake()
    {
        interactable = GetComponent<InteractionManager>();
        CreateLabels();
    }

    private void OnEnable()
    {
        if (!all.Contains(this)) all.Add(this);
    }

    private void OnDisable()
    {
        all.Remove(this);
        alpha = 0f;
        SetLabelsActive(false);
    }

    private void OnDestroy()
    {
        if (label != null) Destroy(label.gameObject);
        if (descriptionLabel != null) Destroy(descriptionLabel.gameObject);
    }

    private void CreateLabels()
    {
        label = CreateLabel($"{name}_Prompt", string.Format(format, keyLabel), fontSize, color);
        descriptionLabel = CreateLabel($"{name}_PromptDescription", string.Empty, descriptionFontSize, descriptionColor);
        currentDescription = null;
    }

    private TextMeshPro CreateLabel(string objectName, string text, float size, Color textColor)
    {
        GameObject go = new GameObject(objectName);
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();

        if (font != null) tmp.font = font;

        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
        tmp.sortingOrder = sortingOrder;
        tmp.alpha = 0f;

        go.SetActive(false);
        return tmp;
    }

    private void LateUpdate()
    {
        if (label == null) return;

        alpha = Mathf.MoveTowards(alpha, GetTargetAlpha(), fadeSpeed * Time.unscaledDeltaTime);

        bool visible = alpha > 0.01f;

        if (label.gameObject.activeSelf != visible)
            label.gameObject.SetActive(visible);

        UpdateDescription(visible);

        if (!visible) return;

        label.alpha = alpha;

        float bob = Mathf.Sin(Time.unscaledTime * bobSpeed) * bobAmount;
        Vector3 basePos = transform.position + offset + new Vector3(0f, bob, 0f);

        label.transform.position = basePos;
        label.transform.rotation = Quaternion.identity;

        if (descriptionLabel != null && descriptionLabel.gameObject.activeSelf)
        {
            descriptionLabel.alpha = alpha;
            descriptionLabel.transform.position = basePos + new Vector3(0f, -descriptionOffsetY, 0f);
            descriptionLabel.transform.rotation = Quaternion.identity;
        }
    }

    private void UpdateDescription(bool visible)
    {
        if (descriptionLabel == null) return;

        string desc = interactable.PromptDescription;

        if (desc != currentDescription)
        {
            currentDescription = desc;
            descriptionLabel.text = desc;
        }

        bool show = visible && !string.IsNullOrEmpty(desc);

        if (descriptionLabel.gameObject.activeSelf != show)
            descriptionLabel.gameObject.SetActive(show);
    }

    private void SetLabelsActive(bool value)
    {
        if (label != null) label.gameObject.SetActive(value);
        if (descriptionLabel != null) descriptionLabel.gameObject.SetActive(value);
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