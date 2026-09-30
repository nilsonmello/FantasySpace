using UnityEngine;
using UnityEngine.EventSystems;

public class GameOverPanelController : MonoBehaviour
{
    [Header("First Selection")]
    public GameObject firstButtonGameOver;

    public void Open()
    {
        SetFocusToButton(firstButtonGameOver);
    }

    public void Retry()
    {
        UIManager.Instance.RestartLevel();
    }

    public void GoToMainMenu()
    {
        SoundManager.Instance.StopAllSounds();
        UIManager.Instance.ShowMainMenu();
    }

    private void SetFocusToButton(GameObject targetButton)
    {
        if (EventSystem.current == null) return;

        EventSystem.current.SetSelectedGameObject(null);

        if (!UIManager.Instance.isUsingMouse)
        {
            EventSystem.current.SetSelectedGameObject(targetButton);
        }

        UIManager.Instance.lastSelectedButton = targetButton;
    }
}