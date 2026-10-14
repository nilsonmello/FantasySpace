using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WirePuzzle))]
public class PuzzleLock : MonoBehaviour
{
    [Header("Opcional")]
    [SerializeField] private Behaviour[] disableWhileOpen;

    private WirePuzzle puzzle;
    private PlayerMovementBase player;
    private CameraFollow cameraFollow;
    private readonly List<Behaviour> disabledByUs = new List<Behaviour>();
    private bool locked;

    private void Awake() => puzzle = GetComponent<WirePuzzle>();

    private void OnEnable()
    {
        puzzle.Opened += Lock;
        puzzle.Closed += Unlock;
    }

    private void OnDisable()
    {
        puzzle.Opened -= Lock;
        puzzle.Closed -= Unlock;

        if (locked) Unlock();
    }

    private void Lock()
    {
        if (locked) return;
        locked = true;

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.GetComponent<PlayerMovementBase>();
        }

        if (cameraFollow == null)
            cameraFollow = FindFirstObjectByType<CameraFollow>();

        if (player != null) player.SetMovementLocked(true);
        if (cameraFollow != null) cameraFollow.Freeze();

        disabledByUs.Clear();
        foreach (var b in disableWhileOpen)
        {
            if (b != null && b.enabled)
            {
                b.enabled = false;
                disabledByUs.Add(b);
            }
        }
    }

    private void Unlock()
    {
        if (!locked) return;
        locked = false;

        if (player != null) player.SetMovementLocked(false);
        if (cameraFollow != null) cameraFollow.Unfreeze();

        foreach (var b in disabledByUs)
            if (b != null) b.enabled = true;

        disabledByUs.Clear();
    }
}