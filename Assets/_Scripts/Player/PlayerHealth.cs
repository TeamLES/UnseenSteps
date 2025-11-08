using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public int maxHealth = 5;
    public int health = 5;

    private SpriteRenderer sr;

    [Header("Knockback")]
    public float knockbackForce = 10f;
    private PlayerController controller;

    [Header("Invulnerability")]
    public float invulnerabilityDuration = 2f;
    private bool isInvulnerable = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        controller = GetComponent<PlayerController>();

        health = Mathf.Clamp(health, 0, maxHealth);
    }
    public void GrantTempInvulnerabilityRealtime(float seconds)
    {
        StopCoroutine(nameof(_TempInvulRt));
        StartCoroutine(_TempInvulRt(seconds));
    }

    IEnumerator _TempInvulRt(float seconds)
    {
        isInvulnerable = true;
        yield return new WaitForSecondsRealtime(seconds);
        isInvulnerable = false;
    }

    public void TakeDamage(int dmg) => TakeDamage(dmg, transform.position);

    public void TakeDamage(int dmg, Vector2 attackerPosition)
    {
        if (PauseMenu.IsPaused) return;
        if (health <= 0 || isInvulnerable) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("enemyHit");

        health = Mathf.Clamp(health - dmg, 0, maxHealth);
        Debug.Log("Player hit! HP: " + health);

        Vector2 knockDirection = (transform.position - (Vector3)attackerPosition).normalized;
        controller.StartCoroutine(controller.Stun(0.2f));

        var rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(knockDirection * knockbackForce, ForceMode2D.Impulse);

        StartCoroutine(DamageRoutine());

        if (health <= 0)
        {
            if (CheckpointManager.Instance != null)
                CheckpointManager.Instance.RespawnPlayer();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || health <= 0) return;
        int old = health;
        health = Mathf.Clamp(health + amount, 0, maxHealth);
        if (health != old)
        {
            Debug.Log($"Healed to {health}/{maxHealth}");
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX("heal");
        }
    }

    IEnumerator DamageRoutine()
    {
        StartCoroutine(BlinkEffect());

        if (invulnerabilityDuration <= 0f)
            yield break;

        isInvulnerable = true;

        yield return new WaitForSecondsRealtime(invulnerabilityDuration);

        isInvulnerable = false;
    }

    IEnumerator BlinkEffect()
    {
        int blinkCount = 4;
        float blinkDuration = 0.1f;

        for (int i = 0; i < blinkCount; i++)
        {
            sr.color = new Color(1, 1, 1, 0.2f);
            yield return new WaitForSeconds(blinkDuration);
            sr.color = Color.white;
            yield return new WaitForSeconds(blinkDuration);
        }
    }
}
