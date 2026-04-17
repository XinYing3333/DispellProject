// using UnityEngine;
//
// /// <summary>
// /// Bridge component placed on your Player root to cooperate with CutsceneManager.
// /// Hook your own input/movement scripts in the inspector.
// /// </summary>
// public class PlayerCutsceneHooks : MonoBehaviour
// {
//     [Header("References")]
//     [Tooltip("Your input script; will be enabled/disabled during cutscenes.")]
//     [SerializeField] private MonoBehaviour inputScript;
//     [Tooltip("Movement-related scripts to enable/disable during cutscenes.")]
//     [SerializeField] private MonoBehaviour[] movementScripts;
//     [SerializeField] private Rigidbody rb;
//
//     private void Awake()
//     {
//         if (CutsceneManager.Instance == null) return;
//         CutsceneManager.Instance.OnTogglePlayerInput += ToggleInput;
//         CutsceneManager.Instance.OnTogglePlayerMovement += ToggleMovement;
//         CutsceneManager.Instance.OnStopPlayerVelocity += StopVel;
//     }
//
//     private void OnDestroy()
//     {
//         if (CutsceneManager.Instance == null) return;
//         CutsceneManager.Instance.OnTogglePlayerInput -= ToggleInput;
//         CutsceneManager.Instance.OnTogglePlayerMovement -= ToggleMovement;
//         CutsceneManager.Instance.OnStopPlayerVelocity -= StopVel;
//     }
//
//     private void ToggleInput(bool enabled)
//     {
//         if (inputScript) inputScript.enabled = enabled;
//     }
//
//     private void ToggleMovement(bool enabled)
//     {
//         if (movementScripts == null) return;
//         foreach (var s in movementScripts)
//             if (s) s.enabled = enabled;
//     }
//
//     private void StopVel()
//     {
//         if (rb) rb.linearVelocity = Vector3.zero;
//     }
// }
