using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public int maxHealth = 3;
    public int currentHealth;
    public bool IsDead { get; private set; }

    [Header("FX & Audio (optional)")]
    public bool blinkOnHit = true;
    public SpriteRenderer blinkRenderer;      // ak null, nájde sa sám
    public int blinkCount = 3;
    public float blinkDuration = 0.1f;
    public string hurtSfx;
    public string deathSfx = "enemyDeath1";   // alebo nechaj prázdne

    [Header("Animator (optional)")]
    public string hurtTrigger = "Hurt";
    public string deathTrigger = "Death";

    [Header("Death Handling")]
    public bool disableAllCollidersOnDeath = true;
    public float destroyDelay = 0f;           // Boss mal 3 sekundy
    public bool stopRigidbodyOnDeath = true;

    [Header("Unlock Overlay (optional)")]
    public SkillUnlockOverlay overlay;        // ak máš singleton, nechaj prázdne
    public bool showUnlockOverlay = false;
    public float overlayDelay = 0f;
    public string unlockedSkillName;
    public Sprite unlockedSkillIcon;

    [Header("Ability Unlock (optional)")]
    public PlayerAbilitiesData abilitiesData;
    public enum AbilityToUnlock { None, Dash, DoubleJump, WallSlide, WallJump }
    public AbilityToUnlock unlockAbility = AbilityToUnlock.None;

    Animator animator;
    Collider2D[] colliders;

    void Awake()
    {
        animator = GetComponent<Animator>();
        colliders = GetComponentsInChildren<Collider2D>(true);
        if (!blinkRenderer) blinkRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(int dmg)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(1, dmg));

        if (!string.IsNullOrEmpty(hurtSfx))
            AudioManager.Instance?.PlaySFX(hurtSfx);

        if (!string.IsNullOrEmpty(hurtTrigger))
            animator.SetTrigger(hurtTrigger);

        if (blinkOnHit && blinkRenderer) StartCoroutine(Blink());

        if (currentHealth == 0) Die();
    }

    System.Collections.IEnumerator Blink()
    {
        for (int i = 0; i < blinkCount; i++)
        {
            blinkRenderer.color = new Color(1, 1, 1, 0.2f);
            yield return new WaitForSeconds(blinkDuration);
            blinkRenderer.color = Color.white;
            yield return new WaitForSeconds(blinkDuration);
        }
    }

    void Die()
    {
        if (IsDead) return;
        IsDead = true;

        var walk = GetComponent<EnemyWalk>();
        if (walk) walk.enabled = false;

        if (!string.IsNullOrEmpty(deathSfx))
            AudioManager.Instance?.PlaySFX(deathSfx);

        if (!string.IsNullOrEmpty(deathTrigger))
            animator.SetTrigger(deathTrigger);

        if (stopRigidbodyOnDeath)
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb) rb.linearVelocity = Vector2.zero;
        }

        if (disableAllCollidersOnDeath)
            foreach (var c in colliders) c.enabled = false;

        // overlay + unlock (ako BossHealth)
        if (showUnlockOverlay)
            (overlay ? overlay : SkillUnlockOverlay.Instance)
                ?.Show(unlockedSkillName, unlockedSkillIcon, overlayDelay);

        if (abilitiesData && unlockAbility != AbilityToUnlock.None)
        {
            switch (unlockAbility)
            {
                case AbilityToUnlock.Dash: abilitiesData.canDash = true; break;
                case AbilityToUnlock.DoubleJump: abilitiesData.canDoubleJump = true; break;
                case AbilityToUnlock.WallSlide: abilitiesData.canWallSlide = true; break;
                case AbilityToUnlock.WallJump: abilitiesData.canWallJump = true; break;
            }
        }

        Destroy(gameObject, destroyDelay);
    }
}
