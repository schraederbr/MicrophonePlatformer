using UnityEngine;
using UnityEngine.InputSystem;

public class FreezeToggle : MonoBehaviour
{
    public InputActionReference freezeAction;
    public bool freezeRotation = true;   // if true, keep rotation frozen when "unfrozen"

    private void OnEnable()
    {
        if (freezeAction != null) freezeAction.action.Enable();
    }

    private void OnDisable()
    {
        if (freezeAction != null) freezeAction.action.Disable();
    }

    void Update()
    {
        if (freezeAction != null &&
            freezeAction.action.WasPressedThisFrame())
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();

            // Are we currently *fully* frozen?
            bool fullyFrozen =
                (rb.constraints & RigidbodyConstraints2D.FreezeAll) ==
                RigidbodyConstraints2D.FreezeAll;

            if (fullyFrozen)
            {
                // Unfreeze: keep or drop rotation based on the flag
                rb.constraints = freezeRotation
                    ? RigidbodyConstraints2D.FreezeRotation
                    : RigidbodyConstraints2D.None;
            }
            else
            {
                // Freeze everything
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }

            // Wake the body so physics resumes if it was asleep
            rb.WakeUp();
        }
    }


}
