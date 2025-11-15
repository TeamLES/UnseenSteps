using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(BoxCollider2D))]
public class EnemyBoss : MonoBehaviour
{
    [Header("Movement / Targeting")]
    public float moveSpeed = 2f;
    public float detectionRange = 6f;
    public float attackRange = 1.6f;
    public float attackCooldown = 2f;
    public Transform attackOrigin;

    [Header("Refs")]
    public EnemyAttackHitbox meleeHitbox;

    [Header("Ground/Wall Check")]
    public LayerMask groundLayer;
    public float edgeLookAhead = 0.5f;
    public float edgeRayDown = 1.0f;
    public float wallCheckDistance = 0.2f;

    [Header("Boss UI")]
    public bool showHealthBarWhenVisible = true;
    [Tooltip("Koľko môže boss/hráč \"trčať\" mimo obraz a stále to berieme ako viditeľné")]
    [Range(0f, 0.5f)] public float viewportMargin = 0.05f;

    private Transform player;
    private Animator animator;
    private EnemyWalk enemyWalk;
    private Rigidbody2D rb;
    private EnemyHealth health;
    private BoxCollider2D box;
    private bool canAttack = true;
    private bool healthBarVisible;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        enemyWalk = GetComponent<EnemyWalk>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
        box = GetComponent<BoxCollider2D>();

        if (attackOrigin == null) attackOrigin = transform;

        enemyWalk.enableChase = true;
        enemyWalk.detectionRange = detectionRange;
        enemyWalk.maxVerticalChaseDelta = 2.5f;
        enemyWalk.target = player;

        if (meleeHitbox == null)
            meleeHitbox = GetComponentInChildren<EnemyAttackHitbox>(true);

        if (meleeHitbox)
        {
            var col = meleeHitbox.GetComponent<Collider2D>();
            if (col) { col.isTrigger = true; col.enabled = false; }
            meleeHitbox.damage = Mathf.Clamp(meleeHitbox.damage, 1, 2);
        }
    }

    void FixedUpdate()
    {
        if (health != null && health.IsDead) { rb.linearVelocity = Vector2.zero; return; }
        if (player == null) { animator.SetBool("IsMoving", false); rb.linearVelocity = Vector2.zero; return; }

        float dist = Vector2.Distance(attackOrigin.position, player.position);

        if (dist <= attackRange)
        {
            enemyWalk.enabled = false;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            animator.SetBool("IsMoving", false);
            if (canAttack) StartCoroutine(AttackRoutine());
            return;
        }

        enemyWalk.enabled = true;
        animator.SetBool("IsMoving", Mathf.Abs(rb.linearVelocity.x) > 0.01f);
    }

    // UI riešime v Update (frame-based, nie physics)
    void Update()
    {
        HandleHealthBar();
    }

    void HandleHealthBar()
    {
        if (!showHealthBarWhenVisible) return;
        if (!BossHealthBar.Instance) return;
        if (!player || !health) return;

        // ak boss zomrel, schovaj bar a skonči
        if (health.IsDead)
        {
            if (healthBarVisible)
            {
                BossHealthBar.Instance.Unbind();
                healthBarVisible = false;
            }
            return;
        }

        bool shouldBeVisible = PlayerAndBossInSameView();

        if (shouldBeVisible && !healthBarVisible)
        {
            BossHealthBar.Instance.Bind(health);
            healthBarVisible = true;
        }
        else if (!shouldBeVisible && healthBarVisible)
        {
            BossHealthBar.Instance.HideImmediate();
            healthBarVisible = false;
        }
    }

    bool PlayerAndBossInSameView()
    {
        Camera cam = Camera.main;
        if (!cam) return false;

        Vector3 bossVP = cam.WorldToViewportPoint(transform.position);
        Vector3 playerVP = cam.WorldToViewportPoint(player.position);

        bool BossVisible =
            bossVP.z > 0 &&
            bossVP.x >= -viewportMargin && bossVP.x <= 1f + viewportMargin &&
            bossVP.y >= -viewportMargin && bossVP.y <= 1f + viewportMargin;

        bool PlayerVisible =
            playerVP.z > 0 &&
            playerVP.x >= -viewportMargin && playerVP.x <= 1f + viewportMargin &&
            playerVP.y >= -viewportMargin && playerVP.y <= 1f + viewportMargin;

        return BossVisible && PlayerVisible;
    }

    IEnumerator AttackRoutine()
    {
        canAttack = false;
        animator.SetTrigger("Attack");
        AudioManager.Instance?.PlaySFX("bossSwing");
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    public void Anim_OpenHitbox()
    {
        if (meleeHitbox == null) return;
        var col = meleeHitbox.GetComponent<Collider2D>();
        if (col) col.enabled = true;
        meleeHitbox.BeginWindow();
    }

    public void Anim_CloseHitbox()
    {
        if (meleeHitbox == null) return;
        meleeHitbox.EndWindow();
        var col = meleeHitbox.GetComponent<Collider2D>();
        if (col) col.enabled = false;
    }
}
