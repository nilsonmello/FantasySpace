using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class ConfigPanelController : MonoBehaviour
{
    [Header("Screens")]
    public GameObject configMainScreen;
    public GameObject audioScreen;
    public GameObject controlsScreen;

    public GameObject keyboardControlsScreen;
    public GameObject gamepadControlsScreen;

    [Header("First Selection")]
    public GameObject firstButtonConfigMainScreen;
    public GameObject firstButtonAudioScreen;
    public GameObject firstButtonControlsScreen;
    public GameObject firstButtonKeyboardControlsScreen;
    public GameObject firstButtonGamepadControlsScreen;

    private GameObject lastSelectedMain;
    private GameObject lastSelectedControls;

    private Action onBack;
    private bool canCancel = false;

    public void Open(Action backCallback = null)
    {
        gameObject.SetActive(true);

        canCancel = false;

        StartCoroutine(EnableCancelRoutine());

        onBack = backCallback;
        ShowMain();
    }

    private System.Collections.IEnumerator EnableCancelRoutine()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        canCancel = true;
    }

    public void ShowMain()
    {
        if (controlsScreen.activeSelf) lastSelectedControls = null;

        SetActiveScreen(configMainScreen);

        SetFocusToButton(lastSelectedMain != null ? lastSelectedMain : firstButtonConfigMainScreen);
    }

    public void ShowAudio()
    {
        if (configMainScreen.activeSelf) lastSelectedMain = EventSystem.current.currentSelectedGameObject;

        SetActiveScreen(audioScreen);

        SetFocusToButton(firstButtonAudioScreen);
    }

    public void ShowControls()
    {
        if (configMainScreen.activeSelf) lastSelectedMain = EventSystem.current.currentSelectedGameObject;

        SetActiveScreen(controlsScreen);

        SetFocusToButton(lastSelectedControls != null ? lastSelectedControls : firstButtonControlsScreen);
    }

    public void ShowKeyboardControls()
    {
        if (controlsScreen.activeSelf) lastSelectedControls = EventSystem.current.currentSelectedGameObject;

        SetActiveScreen(keyboardControlsScreen);
        SetFocusToButton(firstButtonKeyboardControlsScreen);
    }

    public void ShowGamepadControls()
    {
        if (controlsScreen.activeSelf) lastSelectedControls = EventSystem.current.currentSelectedGameObject;

        SetActiveScreen(gamepadControlsScreen);
        SetFocusToButton(firstButtonGamepadControlsScreen);
    }

    private void SetActiveScreen(GameObject target)
    {
        configMainScreen.SetActive(target == configMainScreen);
        audioScreen.SetActive(target == audioScreen);
        controlsScreen.SetActive(target == controlsScreen);
        keyboardControlsScreen.SetActive(target == keyboardControlsScreen);
        gamepadControlsScreen.SetActive(target == gamepadControlsScreen);
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

        if (canCancel)
        {
            var cancelAction = UIManager.Instance.inputActions.FindAction("UI/Cancel");
            if (cancelAction != null && cancelAction.WasPressedThisFrame())
            {
                if (audioScreen.activeSelf || controlsScreen.activeSelf) ShowMain();
                else if (keyboardControlsScreen.activeSelf || gamepadControlsScreen.activeSelf) ShowControls();
                else Back();
            }
        }
    }

    public void Back()
    {
        TransitionManager.Instance.PlayPanelTransition(() =>
        {
            lastSelectedMain = null;
            gameObject.SetActive(false);
            onBack?.Invoke();
            onBack = null;
        });
    }
}