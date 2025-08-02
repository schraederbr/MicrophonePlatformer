// FPSCounterToolkit.cs  (ASCII-only)
using UnityEngine;
using UnityEngine.UIElements;   // UI Toolkit namespace

[RequireComponent(typeof(UIDocument))]
public class FPSCounterToolkit : MonoBehaviour
{
    [SerializeField] private float smooth = 0.1f;   // lower = snappier

    private float dt;           // smoothed deltaTime
    private Label fpsLabel;     // UI Toolkit label

    void Awake()
    {
        // Get the root VisualElement from this UIDocument
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Find the Label whose name (or UXML id) is "FPSCounter"
        fpsLabel = root.Q<Label>("FPSCounter");

        if (fpsLabel == null)
            Debug.LogWarning("Label 'FPSCounter' not found in UIDocument.");
    }

    void Update()
    {
        if (fpsLabel == null) return;   // bail if not assigned

        dt += (Time.unscaledDeltaTime - dt) * smooth;
        int fps = Mathf.RoundToInt(1f / dt);
        fpsLabel.text = fps + " FPS";
    }
}
