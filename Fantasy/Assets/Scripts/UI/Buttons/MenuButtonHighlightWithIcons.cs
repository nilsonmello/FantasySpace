using UnityEngine;
using UnityEngine.UI;

public class MenuButtonHighlightWithIcons : MenuButtonHighLight
{
    [Header("Button images")]
    [SerializeField] private Image decreaseButtonImage;
    [SerializeField] private Image increaseButtonImage;

    [Header("Buttons without highlight")]
    [SerializeField] private Sprite decreaseButtonSprite;
    [SerializeField] private Sprite increaseButtonSprite;

    [Header("Buttons with highlight")]
    [SerializeField] private Sprite highlightedDecreaseButtonSprite;
    [SerializeField] private Sprite highlightedIncreaseButtonSprite;

    protected override void ApplyHighlight()
    {
        base.ApplyHighlight();
        
        if (decreaseButtonImage != null) decreaseButtonImage.sprite = highlightedDecreaseButtonSprite;
        if (increaseButtonImage != null) increaseButtonImage.sprite = highlightedIncreaseButtonSprite;
    }

    protected override void ResetHighlight()
    {
        base.ResetHighlight();

        if (decreaseButtonImage != null) decreaseButtonImage.sprite = decreaseButtonSprite;
        if (increaseButtonImage != null) increaseButtonImage.sprite = increaseButtonSprite;
    }
}