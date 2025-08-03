// ScoreController.cs
// Counts score, shows Game-Over message, and waits for Jump to restart (coroutine version).

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class ScoreController : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector fields
    // ------------------------------------------------------------------
    [Header("Input")]
    public InputActionReference jumpAction;          // Drag the Jump action here

    [Header("UI Toolkit")]
    public UIDocument uiDoc;                         // Drag your UIDocument here
    public VisualTreeAsset uxmlAsset;                // Assign your UXML here

    // ------------------------------------------------------------------
    // Private state
    // ------------------------------------------------------------------
    private Label scoreLabel;                        // Shows running score
    private Label gameOverLabel;                     // Shows restart text
    private VisualElement gameOverVE;                // Parent container

    private Coroutine restartCo;                     // Reference to running coroutine

    public static int score;
    private bool gameOver;                           // True once level complete

    // ------------------------------------------------------------------
    private void OnEnable()
    {
        if (jumpAction != null) jumpAction.action.Enable();

        // Rebuild the UI fresh each enable
        if (uxmlAsset != null && uiDoc != null)
            uiDoc.visualTreeAsset = uxmlAsset;

        var root = uiDoc.rootVisualElement;

        scoreLabel = root.Q<Label>("Score");
        gameOverLabel = root.Q<Label>("GameOver");        // The label inside the container
        gameOverVE = root.Q<VisualElement>("GameOver"); // The container itself

        HideGameOver();
    }

    private void OnDisable()
    {
        if (jumpAction != null) jumpAction.action.Disable();
        if (restartCo != null) StopCoroutine(restartCo);
    }

    // ------------------------------------------------------------------
    private void Update()
    {
        // Trigger game-over when score target reached
        if (!gameOver && Globals.coinsPerLevel == score)
            TriggerGameOver();

        if (scoreLabel != null)
            scoreLabel.text = $"Score: {score}";
    }

    // ------------------------------------------------------------------
    // Show game-over UI and start waiting for restart
    // ------------------------------------------------------------------
    public void TriggerGameOver()
    {
        if (gameOver) return;                    // Prevent double-trigger
        gameOver = true;

        // Compose restart message
        if (gameOverLabel != null)
        {
            string keyName = jumpAction.action.GetBindingDisplayString();
            gameOverLabel.text =
                $"Level Completed in {Globals.time:F2} seconds. Press {keyName} to play again";
        }

        // Show container
        if (gameOverVE != null)
            gameOverVE.style.display = DisplayStyle.Flex;

        // Start coroutine that waits for jump
        if (restartCo != null) StopCoroutine(restartCo);
        restartCo = StartCoroutine(WaitForJumpThenRestart());
    }

    // ------------------------------------------------------------------
    // Coroutine: poll jumpAction each frame until pressed
    // ------------------------------------------------------------------
    private IEnumerator WaitForJumpThenRestart()
    {
        // Wait until the jump button/action is performed
        while (!jumpAction.action.WasPerformedThisFrame())
            yield return null;

        HideGameOver();

        // Optionally reset static score
        score = 0;
        gameOver = false;

        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ------------------------------------------------------------------
    // Helper: hide Game-Over container safely
    // ------------------------------------------------------------------
    private void HideGameOver()
    {
        if (gameOverVE != null)
            gameOverVE.style.display = DisplayStyle.None;
    }
}
