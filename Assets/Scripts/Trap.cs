// Trap.cs
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class Trap : MonoBehaviour
{
    // ------------------------------------------------------------ //
    // Tunables
    // ------------------------------------------------------------ //
    [Tooltip("Name of the Jump action inside the PlayerInput actions asset.")]
    [SerializeField] private string jumpActionName = "Jump";

    // ------------------------------------------------------------ //
    // Private state
    // ------------------------------------------------------------ //
    private int _playerLayer;
    private InputAction _jumpAction;       // Resolved at runtime
    private UIDocument _uiDoc;             // Resolved at runtime
    private Label _gameOverLabel;
    private VisualElement _gameOverVE;

    private Coroutine _restartCo;
    private bool _hasTriggered;            // Prevent double-fire

    // ------------------------------------------------------------ //
    private void Awake()
    {
        _playerLayer = LayerMask.NameToLayer("Player");
    }

    [System.Obsolete]
    private void OnEnable()
    {
        ResolveRuntimeRefs();
        EnableJumpAction(true);
        HideGameOver();
    }

    private void OnDisable()
    {
        EnableJumpAction(false);
        if (_restartCo != null) StopCoroutine(_restartCo);
    }

    // ------------------------------------------------------------ //
    // Collision
    // ------------------------------------------------------------ //
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasTriggered || other.gameObject.layer != _playerLayer) return;

        _hasTriggered = true;
        GetComponent<Collider2D>().enabled = false;   // No further hits
        ShowTrapGameOver();
    }

    // ------------------------------------------------------------ //
    // Game-over workflow
    // ------------------------------------------------------------ //
    private void ShowTrapGameOver()
    {
        if (_gameOverLabel != null && _jumpAction != null)
        {
            string keyName = _jumpAction.GetBindingDisplayString();
            _gameOverLabel.text =
                $"You hit a trap and died! Press {keyName} to try again";
        }

        if (_gameOverVE != null)
            _gameOverVE.style.display = DisplayStyle.Flex;

        if (_restartCo != null) StopCoroutine(_restartCo);
        _restartCo = StartCoroutine(WaitForJumpThenRestart());
    }

    private IEnumerator WaitForJumpThenRestart()
    {
        while (!_jumpAction.WasPerformedThisFrame())
            yield return null;

        HideGameOver();

        // Reset shared game state if needed
        ScoreController.score = 0;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ------------------------------------------------------------ //
    // Helpers
    // ------------------------------------------------------------ //
    [System.Obsolete]
    private void ResolveRuntimeRefs()
    {
        // Grab Jump action from the scene’s PlayerInput
        if (_jumpAction == null)
        {
            var playerInput = FindObjectOfType<PlayerInput>();
            if (playerInput != null)
                _jumpAction = playerInput.actions?.FindAction(jumpActionName, true);
        }

        // Grab the first UIDocument (your HUD) and cache elements
        if (_uiDoc == null)
        {
            _uiDoc = FindObjectOfType<UIDocument>();
            if (_uiDoc != null)
            {
                var root = _uiDoc.rootVisualElement;
                _gameOverLabel = root.Q<Label>("GameOver");
                _gameOverVE = root.Q<VisualElement>("GameOver");
            }
        }
    }

    private void EnableJumpAction(bool enable)
    {
        if (_jumpAction == null) return;
        if (enable) _jumpAction.Enable();
        else _jumpAction.Disable();
    }

    private void HideGameOver()
    {
        if (_gameOverVE != null)
            _gameOverVE.style.display = DisplayStyle.None;
    }
}
