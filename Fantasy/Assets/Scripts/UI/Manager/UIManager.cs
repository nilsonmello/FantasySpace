using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System;

public class UIManager : Singleton<UIManager>
{
    [Header("Canvas")]
    public Canvas mainCanvas;
    public Canvas pauseCanvas;
    public Canvas configCanvas;

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject playPanel;
    public GameObject creditsPanel;
    public GameObject confirmQuitPanel;

    [Header("First Selection")]
    public GameObject firstButtonMainMenu;
    public GameObject firstButtonOptions;
    public GameObject firstButtonCredits;
    public GameObject firstButtonQuit;

    [Header("End Screen")]
    public Canvas endScreenCanvas;
    public GameObject firstButtonEndScreen;

    [Header("Game Over")]
    public Canvas gameOverCanvas;
    public GameOverPanelController gameOverPanelController;
    
    public float gameOverFadeDuration = 0.25f;
    public bool isGameOver { get; private set; }

    [Header("Controllers")]
    public PausePanelController pausePanelController;
    public ConfigPanelController configPanelController;

    [Header("Input")]
    public InputActionAsset inputActions;
    private InputAction pauseAction;
    public bool paused = false;

    [Header("Cutscene")]
    public CutsceneData introCutscene;
    public Action onCutsceneFinished;

    [Header("Smart Input Tracking")]
    public bool isUsingMouse = false;
    public GameObject lastSelectedButton;
    private GameObject lastSelectedMainMenu;

    protected override void Awake()
    {
        base.Awake();

        ToggleCanvas(mainCanvas, true);
        ToggleCanvas(pauseCanvas, false);
        ToggleCanvas(gameOverCanvas, false);
        TurOffPanels();

        if (inputActions != null)
        {
            pauseAction = inputActions.FindAction("Pause");
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (pauseAction == null) return;
        pauseAction.Enable();
        pauseAction.performed += OnPausePerformed;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (pauseAction == null) return;
        pauseAction.performed -= OnPausePerformed;
        pauseAction.Disable();
    }

    void Update()
    {
        if (EventSystem.current == null) return;

        if (EventSystem.current.currentSelectedGameObject != null)
        {
            lastSelectedButton = EventSystem.current.currentSelectedGameObject;
        }

        if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 1f)
        {
            if (!isUsingMouse) isUsingMouse = true;
        }
        else if ((Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) || 
                 (Gamepad.current != null && (Gamepad.current.dpad.ReadValue() != Vector2.zero || Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.5f)))
        {
            if (isUsingMouse)
            {
                isUsingMouse = false;
                
                if (lastSelectedButton != null && lastSelectedButton.activeInHierarchy)
                {
                    EventSystem.current.SetSelectedGameObject(lastSelectedButton);
                }
            }
        }

        if (mainCanvas.gameObject.activeInHierarchy && !paused)
        {
            var cancelAction = inputActions.FindAction("UI/Cancel");
            if (cancelAction != null && cancelAction.WasPressedThisFrame())
            {
                if (creditsPanel.activeSelf || confirmQuitPanel.activeSelf) ReturnToMenu();
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;

        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            SoundManager.Instance.PlayMusic("M.I.K.A.");
            ToggleCanvas(mainCanvas, true);
            ShowMainPanelImmediate();
            return;
        }

        if (SceneManager.GetActiveScene().name == "Agradecimentos")
        {
            ShowEndScreen();
            return;
        }

        ToggleCanvas(mainCanvas, false);
    }

    private void SetFocusToButton(GameObject targetButton)
    {
        if (EventSystem.current == null) return;
        
        EventSystem.current.SetSelectedGameObject(null);
        
        if (!isUsingMouse)
        {
            EventSystem.current.SetSelectedGameObject(targetButton);
        }
        
        lastSelectedButton = targetButton; 
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (mainCanvas.gameObject.activeInHierarchy || isGameOver) return;
        SetPause(!paused);
    }

    private void SetPause(bool value)
    {
        bool wasPaused = paused;
        paused = value;
        Time.timeScale = paused ? 0 : 1;
        ToggleCanvas(pauseCanvas, paused);

        if (paused)
        {
            SoundManager.Instance.PauseAllSounds();
        }
        else
        {
            SoundManager.Instance.ResumeAllSounds();
        }

        if (inputActions != null)
        {
            var playerMap = inputActions.FindActionMap("Player");
            if (playerMap != null)
            {
                if (paused) playerMap.Disable();
                else playerMap.Enable();
            }
        }

        if (CutsceneVideoManager.Instance.IsPlaying)
        {
            if (paused) CutsceneVideoManager.Instance.PauseVideo();
            else CutsceneVideoManager.Instance.ResumeVideo();
        }

        if (paused && !wasPaused) pausePanelController.ShowMain();
        else if (!paused) configCanvas.gameObject.SetActive(false);
    }

    private void ResetPause()
    {
        isGameOver = false;
        ToggleCanvas(gameOverCanvas, false);
        SetPause(false);
    }

    public void Resume()
    {
        SetPause(false);
    }

    public void ToggleCanvas(Canvas canvas, bool active)
    {
        canvas.gameObject.SetActive(active);
    }

    public void StartGame()
    {
        TransitionManager.Instance.PlayPanelTransitionAsync(
            onReady =>
            {
                lastSelectedMainMenu = null;
                ToggleCanvas(mainCanvas, false);
                SceneLoader.instance.LoadGame();
                SoundManager.Instance.StopAllSounds();
                CutsceneVideoManager.Instance.PlayImmediateAsync(introCutscene, onReady, OnCutsceneFinished);
            },
            CutsceneVideoManager.Instance.cutsceneTransitionDuration,
            3f
        );
    }

    private void OnCutsceneFinished()
    {
        if (SceneManager.GetActiveScene().name == "Fase 1")
        {
            //PlayerMovement.Instance.enabled = true;
            SoundManager.Instance.PlayLoopingSFX("BG_01");
        }
        
        onCutsceneFinished?.Invoke();
    }

    public void ShowGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        TransitionManager.Instance.PlayPanelTransition(() =>
        {
            Time.timeScale = 0f;
            SoundManager.Instance.PauseAllSounds();

            if (inputActions != null)
            {
                var playerMap = inputActions.FindActionMap("Player");
                if (playerMap != null) playerMap.Disable();
            }

            ToggleCanvas(gameOverCanvas, true);
            gameOverPanelController.Open();
        }, gameOverFadeDuration);
    }

    public void RestartLevel()
    {
        TransitionManager.Instance.PlaySceneTransition(() =>
        {
            if (CutsceneVideoManager.Instance.IsPlaying)
            {
                CutsceneVideoManager.Instance.StopVideo(); 
            }
            
            ResetPause();
            ToggleCanvas(mainCanvas, false);
            SceneLoader.instance.ReloadCurrent();
        });
    }

    public void ShowMainMenu()
    {
        TransitionManager.Instance.PlaySceneTransition(() =>
        {
            if (CutsceneVideoManager.Instance.IsPlaying)
            {
                CutsceneVideoManager.Instance.StopVideo();
            }

            ResetPause();
            SceneLoader.instance.LoadMainMenu();
        });
    }

    public void ReturnToMenu()
    {
        TransitionManager.Instance.PlayPanelTransition(() =>
        {
            ResetPause();
            ToggleCanvas(mainCanvas, true);
            ShowMainPanelImmediate();
        });
    }

    public void ShowPanel(GameObject panel, bool active)
    {
        panel.SetActive(active);
    }

    public void ShowMainPanel()
    {
        TransitionManager.Instance.PlayPanelTransition(ShowMainPanelImmediate);
    }

    private void ShowMainPanelImmediate()
    {
        TurOffPanels();

        SetFocusToButton(lastSelectedMainMenu != null ? lastSelectedMainMenu : firstButtonMainMenu);
    }

    public void ShowPlayPanel()
    {
        if (mainPanel.activeSelf) lastSelectedMainMenu = EventSystem.current.currentSelectedGameObject;

        TransitionManager.Instance.PlayPanelTransition(() =>
        {
            ShowPanel(mainPanel, false);
            ShowPanel(playPanel, true);
        });
    }

    public void ShowConfigPanel()
    {
        if (mainPanel.activeSelf) lastSelectedMainMenu = EventSystem.current.currentSelectedGameObject;

        TransitionManager.Instance.PlayPanelTransition(() =>
        {
            ShowPanel(mainPanel, false);
            configPanelController.Open(() => ShowMainPanelImmediate());
            SetFocusToButton(firstButtonOptions);
        });
    }

    public void ShowCreditsPanel()
    {
        if (mainPanel.activeSelf) lastSelectedMainMenu = EventSystem.current.currentSelectedGameObject;

        TransitionManager.Instance.PlayPanelTransition(() =>
        {
            ShowPanel(mainPanel, false);
            ShowPanel(creditsPanel, true);
            SetFocusToButton(firstButtonCredits);
        });
    }

    public void ShowConfirmQuit()
    {
        if (mainPanel.activeSelf) lastSelectedMainMenu = EventSystem.current.currentSelectedGameObject;

        TransitionManager.Instance.PlayPanelTransition(() =>
        {
            ShowPanel(mainPanel, false);
            ShowPanel(confirmQuitPanel, true);
            SetFocusToButton(firstButtonQuit);
        });
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void TurOffPanels()
    {
        ShowPanel(mainPanel, true);
        ShowPanel(playPanel, false);
        ShowPanel(creditsPanel, false);
        ShowPanel(confirmQuitPanel, false);
        ToggleCanvas(configCanvas, false);
        ToggleCanvas(endScreenCanvas, false);
    }

    public void ShowEndScreen()
    {
        Time.timeScale = 0f;

        ToggleCanvas(mainCanvas, true); // Bloqueia Pause
        ToggleCanvas(endScreenCanvas, true);

        SetFocusToButton(firstButtonEndScreen);

        // EventSystem.current.SetSelectedGameObject(null);

        // if (!isUsingMouse)
        // {
        //     EventSystem.current.SetSelectedGameObject(firstButtonEndScreen);
        // }
    }
}