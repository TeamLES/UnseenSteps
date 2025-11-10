using UnityEngine;
using System.Collections;
using System.Reflection;

[RequireComponent(typeof(Animator))]
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public int maxHealth = 3;
    public int currentHealth;
    public bool IsDead { get; private set; }

    [Header("Stats")]
    public string enemyId = "Enemy"; // pre GameStats per-typ (nepovinné)

    [Header("Boss Persistence")]
    [Tooltip("Ak je zapnuté, boss po zabití ostane navždy màtvy (k¾úè v PlayerPrefs).")]
    public bool isBoss = false;
    [Tooltip("Unikátne ID pre tohto bossa, napr. 'ForestBoss01'. MUSÍ by jedineèné v projekte.")]
    public string bossId = "";

    [Header("FX & Audio (optional)")]
    public bool blinkOnHit = true;
    public SpriteRenderer blinkRenderer;      // ak null, nájde sa sám
    public int blinkCount = 3;
    public float blinkDuration = 0.1f;
    public string hurtSfx;
    public string deathSfx = "enemyDeath1";

    [Header("Animator (optional)")]
    public string hurtTrigger = "Hurt";
    public string deathTrigger = "Death";

    [Header("Death Handling")]
    public bool disableAllCollidersOnDeath = true;
    public float destroyDelay = 0f;
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

    [Header("Checkpoint on Death (optional)")]
    [Tooltip("Po smrti uloží checkpoint bez potreby checkpoint prefabu.")]
    public bool saveCheckpointOnDeath = false;
    [Tooltip("Ak je priradené, checkpoint sa uloží presne na túto pozíciu.")]
    public Transform checkpointSpawnOverride;

    Animator animator;
    Collider2D[] colliders;

    string BossKey => string.IsNullOrEmpty(bossId) ? null : ("BOSS_" + bossId);

    void Awake()
    {
        // Ak už bol boss niekedy zabitý, hneï ho odstráò (neudelia sa odmeny 2x, niè sa nespustí).
        if (isBoss && !string.IsNullOrEmpty(BossKey) && PlayerPrefs.GetInt(BossKey, 0) == 1)
        {
            Destroy(gameObject);
            return;
        }

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

    IEnumerator Blink()
    {
        for (int i = 0; i < blinkCount; i++)
        {
            if (blinkRenderer) blinkRenderer.color = new Color(1, 1, 1, 0.2f);
            yield return new WaitForSeconds(blinkDuration);
            if (blinkRenderer) blinkRenderer.color = Color.white;
            yield return new WaitForSeconds(blinkDuration);
        }
        if (blinkRenderer) blinkRenderer.color = Color.white;
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
#if UNITY_6000_0_OR_NEWER
            if (rb) rb.linearVelocity = Vector2.zero;
#else
            if (rb) rb.velocity = Vector2.zero;
#endif
        }

        if (disableAllCollidersOnDeath)
            foreach (var c in colliders) if (c) c.enabled = false;

        // Overlay (skill unlock)
        if (showUnlockOverlay)
            (overlay ? overlay : SkillUnlockOverlay.Instance)
                ?.Show(unlockedSkillName, unlockedSkillIcon, overlayDelay);

        // Ability unlock do PlayerAbilitiesData
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

        // Štatistiky: kill
        var id = !string.IsNullOrEmpty(enemyId) ? enemyId : gameObject.tag;
        StatsManager.Instance?.stats?.AddEnemyKill(string.IsNullOrEmpty(id) ? "Enemy" : id);

        // Oznaè bossa ako zabitého (pre budúce naèítania scény)
        if (isBoss && !string.IsNullOrEmpty(BossKey))
        {
            PlayerPrefs.SetInt(BossKey, 1);
            PlayerPrefs.Save();
        }

        // Ulož ad-hoc checkpoint (ak chceš „save“ po smrti bossa)
        if (saveCheckpointOnDeath)
            SaveCheckpointAfterDeath();

        Destroy(gameObject, destroyDelay);
    }

    void SaveCheckpointAfterDeath()
    {
        var cm = CheckpointManager.Instance;
        if (cm == null) return;

        // Pozícia pre respawn
        Vector3 pos = checkpointSpawnOverride
            ? checkpointSpawnOverride.position
            : (FindObjectOfType<PlayerController>() ? FindObjectOfType<PlayerController>().transform.position
                                                    : transform.position + Vector3.up * 0.5f);

        // Skús priamy helper v CheckpointManageri, ak existuje
        try
        {
            var m = cm.GetType().GetMethod("SaveAdHocCheckpoint",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new[] { typeof(Vector3), typeof(bool), typeof(bool) }, null);

            if (m != null)
            {
                m.Invoke(cm, new object[] { pos, true, true });
                return;
            }
        }
        catch { /* fallback nižšie */ }

        // Fallback – doèasný Checkpoint a SaveCheckpoint
        try
        {
            var go = new GameObject("TEMP_AdHocCheckpoint");
            go.transform.position = pos;

            var cp = go.AddComponent<Checkpoint>();
            // ruène spawn point
            var sp = new GameObject("SpawnPoint");
            sp.transform.SetParent(go.transform, false);
            sp.transform.position = pos;
            cp.spawnPoint = sp.transform;

#pragma warning disable CS0618
            cm.SaveCheckpoint(cp);
#pragma warning restore CS0618

            AudioManager.Instance?.PlaySFX("checkpoint");
            Toast.Show("Saved!");

#if UNITY_EDITOR
            Object.DestroyImmediate(go);
#else
            Object.Destroy(go);
#endif
        }
        catch { /* ignore */ }
    }
}
