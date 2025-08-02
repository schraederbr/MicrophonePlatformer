using UnityEngine;
using UnityEngine.UIElements;

public class ScoreController : MonoBehaviour
{
    public static float score = 0f;
    //score label
    private Label scoreLabel;
    public UIDocument uiDocument; // Reference to the UIDocument component

    void Awake()
    {
        // Get the root VisualElement from this UIDocument
        var root = uiDocument.rootVisualElement;

        // Find the Label whose name (or UXML id) is "FPSCounter"
        scoreLabel = root.Q<Label>("Score");

        if (scoreLabel == null)
            Debug.LogWarning("Label 'Score' not found in UIDocument.");
    }

    void Update()
    {
        if (scoreLabel == null) return;   // bail if not assigned

        scoreLabel.text = score.ToString();
    }

}
