// ZoomOutOnStart.cs
// Attach to a Camera. Multiplier > 1 = zoom-out, < 1 = zoom-in
using UnityEngine;

// super-late start execution
[DefaultExecutionOrder(9999)]
[RequireComponent(typeof(Camera))]
public class ZoomOutOnStart : MonoBehaviour
{
    // --------------------------------------------------------- //
    // Inspector
    // --------------------------------------------------------- //
    [Header("Zoom Settings")]
    [Tooltip("Multiplier > 1 zooms OUT, < 1 zooms IN")]
    public float zoomMultiplier = 1.5f;

    [Tooltip("Also wait one frame so this runs AFTER everyone else’s Start()")]
    public bool waitOneFrame = true;

    [Header("Debug Outline")]
    public Color boxColor = Color.red;
    [Tooltip("Pixels thick")]
    public int lineThickness = 2;

    // --------------------------------------------------------- //
    // private
    // --------------------------------------------------------- //
    Rect originalViewport;       // in screen pixels
    bool outlineReady;           // draw only once the rect is set
    static Texture2D whiteTex;    // 1-pixel white texture for GUI lines
    public bool drawBox = false;
    // --------------------------------------------------------- //
    void Start()
    {
        if (waitOneFrame)
            StartCoroutine(ApplyZoomNextFrame());
        else
            ApplyZoom();
    }

    System.Collections.IEnumerator ApplyZoomNextFrame()
    {
        yield return null;        // wait one full frame
        ApplyZoom();
    }

    void ApplyZoom()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null) return;

        //------------------------------------------------------//
        // 1.  Capture original viewport rect in *screen space*
        //------------------------------------------------------//
        float scale = 1f / zoomMultiplier;
        float w = Screen.width * scale;
        float h = Screen.height * scale;
        float x = (Screen.width - w) * 0.5f;
        float y = (Screen.height - h) * 0.5f;
        originalViewport = new Rect(x, y, w, h);
        outlineReady = true;

        //------------------------------------------------------//
        // 2.  Apply zoom
        //------------------------------------------------------//
        if (cam.orthographic)
        {
            cam.orthographicSize *= zoomMultiplier;
        }
        else
        {
            cam.fieldOfView *= zoomMultiplier;
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, 1f, 179f);
        }
    }

    // --------------------------------------------------------- //
    // Draw outline in GUI phase so it sits on top of everything
    // --------------------------------------------------------- //
    void OnGUI()
    {
        if (!outlineReady) return;

        if (whiteTex == null)
        {
            whiteTex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();
        }
        if (drawBox)
        {
            GUI.color = boxColor;

            // top
            GUI.DrawTexture(
                new Rect(originalViewport.x, originalViewport.y,
                         originalViewport.width, lineThickness), whiteTex);
            // bottom
            GUI.DrawTexture(
                new Rect(originalViewport.x,
                         originalViewport.y + originalViewport.height - lineThickness,
                         originalViewport.width, lineThickness), whiteTex);
            // left
            GUI.DrawTexture(
                new Rect(originalViewport.x, originalViewport.y,
                         lineThickness, originalViewport.height), whiteTex);
            // right
            GUI.DrawTexture(
                new Rect(originalViewport.x + originalViewport.width - lineThickness,
                         originalViewport.y, lineThickness,
                         originalViewport.height), whiteTex);

            GUI.color = Color.white; // reset
        }

        
    }
}
