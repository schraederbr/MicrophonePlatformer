using UnityEngine;

public class SpawnPrefab : MonoBehaviour
{
    [Header("Coin settings")]
    [SerializeField] private GameObject coinPrefab;     // Assign your coin prefab in the Inspector

    [Tooltip("Horizontal margin (world units) so coins don’t clip the left/right edges")]
    [SerializeField] private float xPadding = 0.5f;

    [Tooltip("Vertical margin (world units) so coins don’t clip the top/bottom edges")]
    [SerializeField] private float yPadding = 0.5f;

    [Tooltip("Viewport Y offset (0-1) for the bottom bound")]
    public float yOffset = 0.3f;
    public int Count = 0;

    private Camera _cam;

    // --------------------------------------------------
    private void Awake()
    {
        _cam = Camera.main;
        if (_cam == null) Debug.LogError("SpawnPrefab: No Camera tagged MainCamera found.");
    }

    private void Start()
    {
        SpawnCoins();
    }

    // --------------------------------------------------
    private void SpawnCoins()
    {
        if (coinPrefab == null || _cam == null) return;

        // World-space extents of the current viewport
        Vector3 bottomLeft = _cam.ViewportToWorldPoint(new Vector3(0f, yOffset, _cam.nearClipPlane));
        Vector3 topRight = _cam.ViewportToWorldPoint(new Vector3(1f, 1f, _cam.nearClipPlane));

        // Ensure padding fits inside the visible area
        float usableWidth = (topRight.x - bottomLeft.x) - 2f * xPadding;
        float usableHeight = (topRight.y - bottomLeft.y) - 2f * yPadding;

        if (usableWidth <= 0f || usableHeight <= 0f)
        {
            Debug.LogError("SpawnPrefab: Padding is larger than the visible area.");
            return;
        }
        if(Count == 0)
        {
            Count = Globals.coinsPerLevel; 
        } 
        for (int i = 0; i < Count; i++)
        {
            float x = Random.Range(bottomLeft.x + xPadding, bottomLeft.x + xPadding + usableWidth);
            float y = Random.Range(bottomLeft.y + yPadding, bottomLeft.y + yPadding + usableHeight);

            Vector3 spawnPos = new Vector3(x, y, 0f);   // z = 0 for 2D
            Instantiate(coinPrefab, spawnPos, Quaternion.identity, transform);
        }
    }
}
