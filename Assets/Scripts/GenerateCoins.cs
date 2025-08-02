// GenerateCoins.cs  (ASCII-only)
using UnityEngine;

public class GenerateCoins : MonoBehaviour
{
    [Header("Coin settings")]
    [SerializeField] private GameObject coinPrefab; // Assign your coin prefab in the Inspector
    [SerializeField] private int coinAmount = 25;   // How many coins to create
    [SerializeField] private float padding = 0.5f;  // World-unit margin so coins don’t clip screen edges
    public float yOffset = 0.3f;

    private Camera _cam;

    // --------------------------------------------------
    private void Awake()
    {
        _cam = Camera.main;
        if (_cam == null) Debug.LogError("GenerateCoins: No Camera tagged MainCamera found.");
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

        for (int i = 0; i < coinAmount; i++)
        {
            float x = Random.Range(bottomLeft.x + padding, topRight.x - padding);
            float y = Random.Range(bottomLeft.y + padding, topRight.y - padding);

            Vector3 spawnPos = new Vector3(x, y, 0f);              // z = 0 for 2D
            Instantiate(coinPrefab, spawnPos, Quaternion.identity, transform);
        }
    }
}
