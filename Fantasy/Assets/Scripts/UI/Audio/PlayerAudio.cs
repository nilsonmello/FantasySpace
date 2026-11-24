using UnityEngine;

[RequireComponent(typeof(PlayerMovementBase))]
public class PlayerAudio : MonoBehaviour
{
    [Header("SFX Names")]
    [SerializeField] private string stepSoundName = "PlayerStep";
    [SerializeField] private string exhaustedSoundName = "PlayerExhausted";

    [Header("Walk")]
    [SerializeField, Min(0.05f)] private float walkInterval = 0.5f;
    [SerializeField, Range(0f, 1f)] private float walkVolume = 0.5f;

    [Header("Run")]
    [SerializeField, Min(0.05f)] private float runInterval = 0.3f;
    [SerializeField, Range(0f, 1f)] private float runVolume = 0.8f;

    [Header("Crouch")]
    [SerializeField, Min(0.05f)] private float crouchInterval = 0.7f;
    [SerializeField, Range(0f, 1f)] private float crouchVolume = 0.2f;

    [Header("Exhausted")]
    [SerializeField, Range(0f, 1f)] private float exhaustedVolume = 1f;

    [Header("Detection")]
    [SerializeField, Min(0f)] private float minMoveSpeed = 0.05f;

    private PlayerMovementBase movement;
    private float stepTimer;

    private void Awake()
    {
        movement = GetComponent<PlayerMovementBase>();
    }

    private void OnEnable()
    {
        movement.OnExhausted += HandleExhausted;
    }

    private void OnDisable()
    {
        movement.OnExhausted -= HandleExhausted;
    }

    private void Update()
    {
        if (!IsMoving())
        {
            stepTimer = 0f;
            return;
        }

        float interval;
        float volume;

        switch (movement.CurrentMovementState)
        {
            case PlayerMovementBase.MovementState.Run:
                interval = runInterval;
                volume = runVolume;
                break;

            case PlayerMovementBase.MovementState.Crouch:
                interval = crouchInterval;
                volume = crouchVolume;
                break;

            default:
                interval = walkInterval;
                volume = walkVolume;
                break;
        }

        stepTimer += Time.deltaTime;
        if (stepTimer < interval) return;

        stepTimer = 0f;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(stepSoundName, volume, true);
    }

    private bool IsMoving()
    {
        return !movement.MovementLocked
            && movement.CurrentVelocity.sqrMagnitude > minMoveSpeed * minMoveSpeed;
    }

    private void HandleExhausted()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(exhaustedSoundName, exhaustedVolume);
    }
}