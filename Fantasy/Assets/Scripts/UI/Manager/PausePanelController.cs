using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class PausePanelController : MonoBehaviour
{
    [Header("Screens")]
    public GameObject mainPauseScreen;
    public ConfigPanelController configController;
    public GameObject confirmQuitScreen;

    [Header("First Selection")]
    public GameObject firstButtonPauseScreen;
    public GameObject firstButtonConfigMainScreen;
    public GameObject firstButtonQuitScreen;

    private GameObject lastSelectedPauseMain;

    private bool canCancel = false;

    void OnEnable()
    {
        canCancel = false;

        StartCoroutine(EnableCancelRoutine());

        lastSelectedPauseMain = null;
    }

    private System.Collections.IEnumerator EnableCancelRoutine()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        canCancel = true;
    }

    public void ShowMain()
    {
        SetActiveScreen(mainPauseScreen);
        SetFocusToButton(lastSelectedPauseMain != null ? lastSelectedPauseMain : firstButtonPauseScreen);
    }

    public void ShowConfig()
    {
        if (mainPauseScreen.activeSelf) lastSelectedPauseMain = EventSystem.current.currentSelectedGameObject;

        TransitionManager.Instance.PlayPanelTransition(() =>
        {
            mainPauseScreen.SetActive(false);
            confirmQuitScreen.SetActive(false);
            
            configController.Open(() => ShowMain()); 
        });
    }

    public void ShowConfirmQuit()
    {
        if (mainPauseScreen.activeSelf) lastSelectedPauseMain = EventSystem.current.currentSelectedGameObject;

        TransitionManager.Instance.PlayPanelTransition(() =>
        {
            SetActiveScreen(confirmQuitScreen);
            SetFocusToButton(firstButtonQuitScreen);
        });
    }

    public void CancelConfirmQuit()
    {
        TransitionManager.Instance.PlayPanelTransition(() =>
        {
            ShowMain();
        });
    }

    public void GoToMainMenu()
    {
        lastSelectedPauseMain = null;
        SoundManager.Instance.StopAllSounds();
        UIManager.Instance.ShowMainMenu();
    }

    private void SetActiveScreen(GameObject target)
    {
        mainPauseScreen.SetActive(target == mainPauseScreen);
        confirmQuitScreen.SetActive(target == confirmQuitScreen);
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

    void Update()
    {
        if (EventSystem.current == null) return;

        if (configController != null && configController.gameObject.activeSelf) return;

        if (canCancel)
        {
            var cancelAction = UIManager.Instance.inputActions.FindAction("UI/Cancel");
            if (cancelAction != null && cancelAction.WasPressedThisFrame())
            {
                if (confirmQuitScreen.activeSelf) CancelConfirmQuit(); 
                else ClosePause(); 
            }
        }
    }

    public void ClosePause()
    {
        lastSelectedPauseMain = null;
        UIManager.Instance.Resume();
    }
}