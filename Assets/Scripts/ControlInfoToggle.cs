// ControlInfoToggle.cs  (attach to any GameObject in the scene)
// If the GameObject already has a UIDocument, the script will grab it automatically.

using UnityEngine;
using UnityEngine.UIElements;

public class ControlInfoToggle : MonoBehaviour
{
    [Tooltip("UIDocument that owns the HUD. Leave empty to auto-grab from this GameObject.")]
    public UIDocument uiDoc;

    private Toggle _toggleControls;
    private VisualElement _controlInfo;

    // ------------------------------------------------------------ //
    // Unity lifecycle
    // ------------------------------------------------------------ //
    private void Start()
    {

        // Query elements defined in UXML by their 'name' attribute
        VisualElement root = uiDoc.rootVisualElement;
        _toggleControls = root.Q<Toggle>("ToggleControls");
        _controlInfo = root.Q<VisualElement>("ControlInfo");

        if (_toggleControls == null || _controlInfo == null)
        {
            Debug.LogError("ControlInfoToggle: Could not find ToggleControls or ControlInfo in UXML.");
            return;
        }
        _toggleControls.value = true;

    }

    void Update()
    {
        if (_toggleControls.value)
        {
            _controlInfo.style.display = DisplayStyle.Flex;
        }
        else
        {
            _controlInfo.style.display = DisplayStyle.None;
        }
    }


}
