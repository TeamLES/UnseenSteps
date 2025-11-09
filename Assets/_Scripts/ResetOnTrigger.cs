using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ResetOnTrigger : MonoBehaviour
{
    void Awake()
    {
        var bc = GetComponent<BoxCollider2D>();
        bc.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StatsManager.Instance?.stats?.AddPlayerDeath();
            if (CheckpointManager.Instance != null)
                CheckpointManager.Instance.RespawnPlayer();
        }
    }
}
