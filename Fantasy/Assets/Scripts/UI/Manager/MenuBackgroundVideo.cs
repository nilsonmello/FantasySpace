using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class MenuBackgroundVideo : MonoBehaviour
{
    [SerializeField] private float stallTimeout = 0.75f;
    [SerializeField] private float prepareTimeout = 4f;

    private VideoPlayer player;

    private double lastTime = -1;
    private double lastGoodTime;
    private float stallTimer;
    private bool restarting;

    private void Awake()
    {
        player = GetComponent<VideoPlayer>();
    }

    private void OnEnable()
    {
        stallTimer = 0f;
        lastTime = -1;
        restarting = false;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;

        StartCoroutine(CheckAfterFocus());
    }

    private IEnumerator CheckAfterFocus()
    {
        yield return null;
        yield return null;

        if (!isActiveAndEnabled || restarting) yield break;

        EnsureRenderTexture();

        StartCoroutine(RestartRoutine());
    }

    private void Update()
    {
        if (restarting || player == null || !player.enabled) return;

        float dt = Time.unscaledDeltaTime;
        double t = player.time;

        if (player.isPlaying && t > 0.0 && Math.Abs(t - lastTime) > 0.0001)
        {
            lastGoodTime = t;
            stallTimer = 0f;
        }
        else
        {
            stallTimer += dt;

            float limit = player.isPrepared ? stallTimeout : prepareTimeout;
            if (stallTimer >= limit)
            {
                StartCoroutine(RestartRoutine());
            }
        }

        lastTime = t;
    }

    private void EnsureRenderTexture()
    {
        RenderTexture rt = player.targetTexture;
        if (rt != null && !rt.IsCreated())
        {
            rt.Create();
        }
    }

    private IEnumerator RestartRoutine()
    {
        if (restarting) yield break;
        restarting = true;
        stallTimer = 0f;

        double resumeAt = lastGoodTime;

        EnsureRenderTexture();

        player.Stop();
        yield return null;

        player.Prepare();

        float wait = prepareTimeout;
        while (!player.isPrepared && wait > 0f)
        {
            wait -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (player.isPrepared)
        {
            if (player.canSetTime && player.length > 0.0 && resumeAt > 0.0 && resumeAt < player.length)
                player.time = resumeAt;

            player.Play();
        }

        lastTime = -1;
        restarting = false;
    }
}