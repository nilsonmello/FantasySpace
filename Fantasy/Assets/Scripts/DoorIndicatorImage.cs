using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DoorIndicatorImage : MonoBehaviour
{
    [SerializeField] private Image firstImage;
    [SerializeField] private Image secondImage;
    [SerializeField] private Image thirdImage;
    [SerializeField] private Image fourthImage;

    [SerializeField] private int expectedButtonCount = 4;
    [SerializeField] private float searchInterval = 0.25f;

    [SerializeField] private Color incompleteColor = Color.red;
    [SerializeField] private Color completeColor = Color.green;

    private readonly HashSet<ButtonInteractable> registeredButtons = new HashSet<ButtonInteractable>();
    private Image[] indicatorImages;
    private Coroutine searchCoroutine;

    private void Start()
    {
        indicatorImages = new Image[] { firstImage, secondImage, thirdImage, fourthImage };
        
        UpdateIndicators();

        searchCoroutine = StartCoroutine(SearchForButtonsRoutine());
    }

    private IEnumerator SearchForButtonsRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(searchInterval);

        while (registeredButtons.Count < expectedButtonCount)
        {
            ButtonInteractable[] foundButtons = FindObjectsByType<ButtonInteractable>(
                FindObjectsInactive.Include, 
                FindObjectsSortMode.None
            );

            bool foundNewButton = false;

            foreach (ButtonInteractable button in foundButtons)
            {
                if (button != null && registeredButtons.Add(button))
                {
                    button.OnPressedChanged += OnButtonStateChanged;
                    foundNewButton = true;
                }
            }

            if (foundNewButton)
            {
                UpdateIndicators();
            }

            if (registeredButtons.Count >= expectedButtonCount)
            {
                break;
            }

            yield return wait;
        }

        searchCoroutine = null;
    }

    private void OnDestroy()
    {
        if (searchCoroutine != null)
        {
            StopCoroutine(searchCoroutine);
        }

        foreach (ButtonInteractable button in registeredButtons)
        {
            if (button != null)
            {
                button.OnPressedChanged -= OnButtonStateChanged;
            }
        }
        
        registeredButtons.Clear();
    }

    private void OnButtonStateChanged(ButtonInteractable button)
    {
        UpdateIndicators();
    }

    private void UpdateIndicators()
    {
        int solvedCount = 0;

        foreach (ButtonInteractable button in registeredButtons)
        {
            if (button != null && button.IsPressed)
            {
                solvedCount++;
            }
        }

        for (int i = 0; i < indicatorImages.Length; i++)
        {
            if (indicatorImages[i] != null)
            {
                indicatorImages[i].color = (i < solvedCount) ? completeColor : incompleteColor;
            }
        }
    }
}
