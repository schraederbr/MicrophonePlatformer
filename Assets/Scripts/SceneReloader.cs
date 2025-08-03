// SceneReloader.cs
// Reloads the active scene when the "ResetScene" action is performed.

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerInput))] // optional but handy
public class SceneReloader : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference resetSceneAction; // Button - "ResetScene"

    // -------------------------------------------------------------
    private void OnEnable()
    {
        if (resetSceneAction != null)
        {
            resetSceneAction.action.Enable();
            resetSceneAction.action.performed += OnResetPerformed;
        }
    }

    private void OnDisable()
    {
        if (resetSceneAction != null)
        {
            resetSceneAction.action.performed -= OnResetPerformed;
            resetSceneAction.action.Disable();
        }
    }

    // -------------------------------------------------------------
    private void OnResetPerformed(InputAction.CallbackContext ctx)
    {
        // Simply reload the active scene
        Scene active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.buildIndex);
    }
}
