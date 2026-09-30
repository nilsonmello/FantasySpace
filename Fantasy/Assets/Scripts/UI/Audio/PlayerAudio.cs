// using UnityEngine;

// [RequireComponent(typeof(PlayerMovement))]
// public class PlayerAudio : MonoBehaviour
// {
//     private PlayerMovement playerMovement;

//     [Header("Clipes")]
//     public AudioClip jumpClip;
//     public AudioClip landClip;

//     [Header("Volume")]
//     [Range(0f, 1f)] public float jumpVolume = 1f;
//     [Range(0f, 1f)] public float landVolume = 1f;
//     [Range(0f, 1f)] public float walkVolume = 0.5f;
//     [Range(0f, 1f)] public float runVolume = 0.5f;

//     private void Awake()
//     {
//         playerMovement = GetComponent<PlayerMovement>();
//     }

//     private void OnEnable()
//     {
//         playerMovement.OnJumped += HandleJumped;
//         playerMovement.OnLanded += HandleLanded;
//     }

//     private void OnDisable()
//     {
//         playerMovement.OnJumped -= HandleJumped;
//         playerMovement.OnLanded -= HandleLanded;
//     }

//     private void HandleJumped()
//     {
//         SoundManager.Instance.PlaySFX("PlayerJump", jumpVolume, true);
//     }

//     private void HandleLanded()
//     {
//         SoundManager.Instance.PlaySFX("PlayerLand", landVolume, true);
//     }

//     public void PlayWalkingFootstep()
//     {
//         SoundManager.Instance.PlaySFX("PlayerWalk", walkVolume, true);
//     }

//     public void PlayRunningFootstep()
//     {
//         SoundManager.Instance.PlaySFX("PlayerRun", runVolume, true);
//     }
// }