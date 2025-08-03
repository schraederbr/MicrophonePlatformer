

using UnityEngine;
using UnityEngine.UIElements;

public class TimeController : MonoBehaviour
{
    [Header("UI Toolkit")]
    public UIDocument uiDoc;      // set this in Inspector

    private Label timeLabel;      // reference to the label
    private const string labelName = "Time";   // must match the name set in UI Builder

    private void Awake()
    {
        // Start fresh whenever this script is enabled
        Globals.time = 0f;
    }

    private void Start()
    {
        if (uiDoc != null)
        {
            // Query the root VisualElement for the label called "Time"
            timeLabel = uiDoc.rootVisualElement.Q<Label>(labelName);
        }
    }

    private void Update()
    {
        // Count up in real-time seconds
        Globals.time += Time.deltaTime;

        // Show whole seconds in the label (e.g., "12 s")
        if (timeLabel != null)
        {
            timeLabel.text = Mathf.FloorToInt(Globals.time).ToString() + " s";
        }
    }
}
