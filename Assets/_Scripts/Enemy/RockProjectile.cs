using System.Collections;
using UnityEngine;

public class RockProjectile : MonoBehaviour
{
    public int damage = 1;
    public float activationDelay = 0.1f;
    private bool hasHit;
    private bool active;

    void OnEnable()
    {
        hasHit = false;
        active = false;
        StartCoroutine(ActivateAfterDelay());
    }

    IEnumerator ActivateAfterDelay()
    {
        if (activationDelay > 0f)
            yield return new WaitForSeconds(activationDelay);
        active = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider);
    }

    void HandleHit(Collider2D col)
    {
        if (hasHit || !active) return;
        var player = col.GetComponent<PlayerHealth>();
        if (col.isTrigger && player == null) return;
        hasHit = true;
        if (player != null)
            player.TakeDamage(damage, transform.position);
        Destroy(gameObject);
    }
}
