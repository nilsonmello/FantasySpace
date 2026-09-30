using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class CutsceneVideoManager : MonoBehaviour
{
    public static CutsceneVideoManager Instance { get; private set; }

    [Header("Setup")]
    public GameObject cutsceneCanvas;
    public VideoPlayer videoPlayer;
    public AudioSource videoAudioSource;
    public float cutsceneTransitionDuration = 1f;
    public float maxPrepareWait = 2f;

    [Header("Warm-up")]
    public VideoClip warmupClip;

    private InputSystem_Actions inputActions;
    private InputAction skipAction;

    private CutsceneData currentData;
    private Action onFinished;
    private int? pendingSceneIndex;
    private int clipIndex;
    private float elapsedSinceStart;
    private bool isPlaying;
    private bool endRequested;

    public bool IsPlaying => isPlaying;
    public bool CanSkip => isPlaying && !endRequested && elapsedSinceStart >= currentData.skipUnlockDelay;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            //Destroy(gameObject);
            return;
        }

        Instance = this;

        inputActions = new InputSystem_Actions();
    }

    // void OnEnable() // remover?
    // {
    //     skipAction = inputActions.Player.Interact;
    //     skipAction.Enable();
    // }

    // void OnDisable() // remover?
    // {
    //     skipAction?.Disable();
    // }

    private void Start()
    {
        StartCoroutine(WarmUpAudioPipeline());
    }

    void Update()
    {
        if (!isPlaying) return;

        elapsedSinceStart += Time.deltaTime;

        // // remover?
        // if (skipAction.WasPressedThisFrame() && CanSkip)
        // {
        //     RequestSkip();
        // }
    }

    private IEnumerator WarmUpAudioPipeline()
    {
        if (warmupClip == null) yield break;

        bool wasMuted = videoAudioSource.mute;
        videoAudioSource.mute = true;

        videoPlayer.clip = warmupClip;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, videoAudioSource);

        bool prepared = false;
        void OnPrepared(VideoPlayer vp) => prepared = true;
        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.Prepare();

        while (!prepared) yield return null;
        videoPlayer.prepareCompleted -= OnPrepared;

        videoPlayer.Play();
        yield return null;

        videoPlayer.Stop();
        videoPlayer.clip = null;
        videoAudioSource.mute = wasMuted;
    }

    public void Play(CutsceneData data, Action onFinishedCallback = null, int? loadSceneIndexOnFinish = null)
    {
        if (isPlaying || data == null || data.clips.Length == 0) return;

        StartCutscene(data, onFinishedCallback, loadSceneIndexOnFinish);

        TransitionManager.Instance.PlayPanelTransitionAsync(
            onReady => PrepareClip(clipIndex, () => { BeginPlayback(); onReady(); }),
            cutsceneTransitionDuration,
            maxPrepareWait
        );
    }

    public void PlayImmediate(CutsceneData data, Action onFinishedCallback = null)
    {
        if (isPlaying || data == null || data.clips.Length == 0) return;

        StartCutscene(data, onFinishedCallback);

        PrepareClip(clipIndex, BeginPlayback);
    }

    public void PlayImmediateAsync(CutsceneData data, Action onReady, Action onFinishedCallback = null)
    {
        if (isPlaying || data == null || data.clips.Length == 0)
        {
            onReady?.Invoke();
            return;
        }

        StartCutscene(data, onFinishedCallback);

        PrepareClip(clipIndex, () => { BeginPlayback(); onReady?.Invoke(); });
    }

    private void StartCutscene(CutsceneData data, Action onFinishedCallback, int? loadSceneIndexOnFinish = null)
    {
        currentData = data;
        onFinished = onFinishedCallback;
        pendingSceneIndex = loadSceneIndexOnFinish;
        clipIndex = 0;
        elapsedSinceStart = 0f;
        endRequested = false;
        isPlaying = true;

        videoPlayer.loopPointReached += OnClipFinished;
    }

    public void RequestSkip()
    {
        if (!isPlaying || endRequested) return;
        EndCutscene();
    }

    private void PrepareClip(int index, Action onReady)
    {
        videoPlayer.clip = currentData.clips[index];

        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, videoAudioSource);

        void Handler(VideoPlayer vp)
        {
            vp.prepareCompleted -= Handler;
            onReady?.Invoke();
        }

        videoPlayer.prepareCompleted += Handler;
        videoPlayer.Prepare();
    }

    private void BeginPlayback()
    {
        //PlayerMovement.Instance.isInCutscene = true;
        cutsceneCanvas.SetActive(true);
        videoPlayer.Play();
    }

    private void OnClipFinished(VideoPlayer vp)
    {
        clipIndex++;

        if (clipIndex < currentData.clips.Length)
        {
            TransitionManager.Instance.PlayPanelTransitionAsync(
                onReady => PrepareClip(clipIndex, () => { BeginPlayback(); onReady(); }),
                cutsceneTransitionDuration,
                maxPrepareWait
            );
        }
        else
        {
            EndCutscene();
        }
    }

    private void EndCutscene()
    {
        if (endRequested) return;
        endRequested = true;

        videoPlayer.loopPointReached -= OnClipFinished;
        videoPlayer.Stop();

        TransitionManager.Instance.PlayPanelTransition(FinishPlayback, cutsceneTransitionDuration);
    }

    private void FinishPlayback()
    {
        cutsceneCanvas.SetActive(false);
        isPlaying = false;

        Action callback = onFinished;
        onFinished = null;
        currentData = null;

        callback?.Invoke();

        if (pendingSceneIndex.HasValue)
        {
            int index = pendingSceneIndex.Value;
            pendingSceneIndex = null;
            SceneManager.LoadScene(index);
        }

        //PlayerMovement.Instance.isInCutscene = false;
    }

    public void PauseVideo()
    {
        if (isPlaying && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
    }

    public void ResumeVideo()
    {
        if (isPlaying && !videoPlayer.isPlaying)
        {
            videoPlayer.Play();
        }
    }

    public void StopVideo()
    {
        if (!isPlaying) return;

        endRequested = true;
        isPlaying = false;
        
        videoPlayer.loopPointReached -= OnClipFinished;
        videoPlayer.Stop();
        cutsceneCanvas.SetActive(false);
        
        onFinished = null;
        currentData = null;
        pendingSceneIndex = null;
        
        //if (PlayerMovement.Instance != null)
        //{
            //PlayerMovement.Instance.isInCutscene = false;
        //}
    }
}