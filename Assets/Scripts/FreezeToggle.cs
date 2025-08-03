// FreezeToggle.cs – show “Press … to play” only after first curve is done
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UIElements;

public class FreezeToggle : MonoBehaviour
{
    // --------------------------------------------------------- //
    // Inspector fields
    // --------------------------------------------------------- //
    [Header("Links")]
    public InputActionReference freezeAction;
    public UIDocument uiDoc;
    public GenerateTerrain terrainRecorder;   // drag, or auto-find

    [Header("Options")]
    public bool freezeRotation = true;
    public bool startFrozen = true;
    public string freezeKey;

    // --------------------------------------------------------- //
    // private
    // --------------------------------------------------------- //
    Label startPlay;
    string pressToPlayMsg = "";
    bool msgReady;          // becomes true when curveIndex > 0
    public Vector3 startPosition;

    // --------------------------------------------------------- //
    [System.Obsolete]
    void OnEnable()
    {
        if (freezeAction != null) freezeAction.action.Enable();
        if (terrainRecorder == null) terrainRecorder = FindObjectOfType<GenerateTerrain>();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.constraints = startFrozen
            ? RigidbodyConstraints2D.FreezeAll
            : (freezeRotation ? RigidbodyConstraints2D.FreezeRotation
                              : RigidbodyConstraints2D.None);
    }

    void OnDisable()
    {
        if (freezeAction != null) freezeAction.action.Disable();
    }

    // --------------------------------------------------------- //
    void Start()
    {
        startPosition = transform.position;
        if (uiDoc != null)
        {
            startPlay = uiDoc.rootVisualElement.Q<Label>("StartPlay");

            if (startPlay != null)
            {
                freezeKey = "?";
                if (freezeAction != null && freezeAction.action.bindings.Count > 0)
                {
                    var b = freezeAction.action.bindings[0];
                    freezeKey = InputControlPath.ToHumanReadableString(
                        b.effectivePath,
                        InputControlPath.HumanReadableStringOptions.OmitDevice);
                }
                pressToPlayMsg = "Press " + freezeKey + " to play";
                startPlay.text = pressToPlayMsg;
                
                //startPlay.style.display = DisplayStyle.None;   // hide at launch
            }
        }
    }

    public void ResetPosition()
    {
        transform.position = startPosition;
    }

    public void Freeze()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        ResetPosition();
        if (msgReady && startPlay != null)
        {
            startPlay.text = "Press " + freezeKey + " to play";
            startPlay.style.display = DisplayStyle.Flex;
        }
    }

    public void UnFreeze()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        startPlay.text = "Press " + freezeKey + " to reset player position";

        rb.constraints = freezeRotation
            ? RigidbodyConstraints2D.FreezeRotation
            : RigidbodyConstraints2D.None;
    }
    public void ToggleFreeze()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        bool frozenNow = IsFullyFrozen(rb);

        if (frozenNow)            // UNFREEZE
        {

            UnFreeze();
        }
        else                       // FREEZE
        {
            Freeze();

        }

        rb.WakeUp();
    }

    // --------------------------------------------------------- //
    void Update()
    {
        //------------------------------------------------------//
        // 1.  Has the first curve finished yet?
        //------------------------------------------------------//
        if (!msgReady &&
            terrainRecorder != null &&
            terrainRecorder.curveIndex > 0)
        {
            msgReady = true;

            // If the body is currently fully frozen, show the prompt now
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (IsFullyFrozen(rb) && startPlay != null)
                startPlay.style.display = DisplayStyle.Flex;
        }

        //------------------------------------------------------//
        // 2.  Handle key press
        //------------------------------------------------------//
        if (freezeAction != null &&
            freezeAction.action.WasPressedThisFrame())
        {
            ToggleFreeze();
        }
    }

    // --------------------------------------------------------- //
    bool IsFullyFrozen(Rigidbody2D rb)
    {
        return (rb.constraints & RigidbodyConstraints2D.FreezeAll)
               == RigidbodyConstraints2D.FreezeAll;
    }
}
