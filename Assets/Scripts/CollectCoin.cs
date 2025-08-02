// Coin.cs
using UnityEngine;

public class CollectCoin : MonoBehaviour
{
    [SerializeField] private int value = 1;          // How many points this coin is worth
    private int _playerLayer;

    private void Awake()
    {
        _playerLayer = LayerMask.NameToLayer("Player");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Accept trigger only from objects on the Player layer
        if (other.gameObject.layer != _playerLayer) return;

        ScoreController.score += value;            // Bump the score
        Destroy(gameObject);                         // Remove the coin
    }
}
