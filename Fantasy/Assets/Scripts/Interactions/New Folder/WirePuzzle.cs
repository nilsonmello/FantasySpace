using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class WirePuzzle : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private WireEnd[] wires;
    [SerializeField] private WireSlot[] slots;

    [Header("close")]
    [SerializeField] private Key closeKey = Key.E;

    [Header("events")]
    [SerializeField] private UnityEvent onCompleted;

    public event Action Opened;
    public event Action Closed;

    private Action onSolved;
    private bool completed;
    private int openedFrame = -1;
    private int closedFrame = -1;

    public bool IsOpen => panel != null && panel.activeSelf;

    public int ConnectedCount
    {
        get
        {
            int count = 0;
            foreach (var w in wires) if (w.IsConnected) count++;
            return count;
        }
    }

    private void Awake()
    {
        if (panel == gameObject)
        {
            return;
        }

        panel.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen || Keyboard.current == null) return;

        if (Time.frameCount == openedFrame) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame ||
            Keyboard.current[closeKey].wasPressedThisFrame)
        {
            Close();
        }
    }

    public void Open() => Open(null);

    public void Open(Action onSolvedCallback)
    {
        if (IsOpen) return;

        if (Time.frameCount == closedFrame) return;

        onSolved = onSolvedCallback;
        completed = false;
        openedFrame = Time.frameCount;

        panel.SetActive(true);
        AssignRandomSlots();
        foreach (var w in wires) w.ResetPosition();

        Opened?.Invoke();
    }

    public void Close()
    {
        bool wasOpen = IsOpen;

        onSolved = null;
        panel.SetActive(false);

        if (wasOpen)
        {
            closedFrame = Time.frameCount;
            Closed?.Invoke();
        }
    }

    private void AssignRandomSlots()
    {
        if (slots.Length < wires.Length)
        {
            return;
        }

        WireSlot[] shuffled = (WireSlot[])slots.Clone();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        for (int i = 0; i < shuffled.Length; i++)
        {
            bool used = i < wires.Length;
            shuffled[i].gameObject.SetActive(used);
            if (used) wires[i].SetTarget(shuffled[i]);
        }
    }

    public void NotifyChanged()
    {
        if (completed || ConnectedCount < wires.Length) return;

        completed = true;

        Action callback = onSolved;
        Close();

        onCompleted.Invoke();
        callback?.Invoke();
    }
}