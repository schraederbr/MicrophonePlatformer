using UnityEngine;
using UnityEngine.InputSystem;

public class FreezeToggle : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            // true if *all* three FreezeAll bits are present
            bool isFrozen =
                (rb.constraints & RigidbodyConstraints2D.FreezeAll) == RigidbodyConstraints2D.FreezeAll;

            // switch between fully frozen and fully free
            rb.constraints = isFrozen
                ? RigidbodyConstraints2D.None
                : RigidbodyConstraints2D.FreezeAll;

            // if the body was asleep while frozen, wake it so physics resumes
            rb.WakeUp();



        }

    }

}
