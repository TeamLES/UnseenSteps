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
    [Tooltip("Ako ďaleko pred seba pozrie na hranu (vodorovne)")]
    public float edgeLookAhead = 0.5f;
    [Tooltip("Ako hlboko dolu hľadá zem z bodu pred sebou")]
    public float edgeRayDown = 1.0f;
    [Tooltip("Kontrola steny pred sebou")]
    public float wallCheckDistance = 0.2f;

    private Transform player;
    private Animator animator;
    private EnemyWalk enemyWalk;
    private Rigidbody2D rb;
    private EnemyHealth health;
    private BoxCollider2D box;
    private bool canAttack = true;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        enemyWalk = GetComponent<EnemyWalk>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
        box = GetComponent<BoxCollider2D>();

        if (attackOrigin == null) attackOrigin = transform;

        // Pohyb – rešpektuj Inspector:
        enemyWalk.enableChase = true;                 // OK ponechať
                                                      // N E N A S T A V U J  requireLineOfSight  TU   (nechaj podľa Inspectoru)
        enemyWalk.detectionRange = detectionRange;
        enemyWalk.maxVerticalChaseDelta = 2.5f;            // boss je vysoký – povol väčšiu odchýlku
        enemyWalk.target = player;              // istota: prepíš na živého hráča

        // --- HITBOX ---
        if (meleeHitbox == null)
            meleeHitbox = GetComponentInChildren<EnemyAttackHitbox>(true);

        if (meleeHitbox)
        {
            var col = meleeHitbox.GetComponent<Collider2D>();
            if (col) { col.isTrigger = true; col.enabled = false; }
            // zabíjal na 1 ranu? drž damage nízky (alebo nastav v Inspectore)
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
            enemyWalk.enabled = false; // zastav sa v útočnom okne
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            animator.SetBool("IsMoving", false);
            if (canAttack) StartCoroutine(AttackRoutine());
            return;
        }

        enemyWalk.enabled = true; // mimo útoku rieši pohyb motor
        animator.SetBool("IsMoving", Mathf.Abs(rb.linearVelocity.x) > 0.01f);
    }

    IEnumerator AttackRoutine()
    {
        canAttack = false;
        animator.SetTrigger("Attack");
        AudioManager.Instance?.PlaySFX("bossSwing");
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    // -------- Animation Events from Animator --------
    public void Anim_OpenHitbox()
    {
        if (meleeHitbox == null) return;
        var col = meleeHitbox.GetComponent<Collider2D>();
        if (col) col.enabled = true;
        meleeHitbox.BeginWindow();  // tvoje BeginWindow
    }

    public void Anim_CloseHitbox()
    {
        if (meleeHitbox == null) return;
        meleeHitbox.EndWindow();    // tvoje EndWindow
        var col = meleeHitbox.GetComponent<Collider2D>();
        if (col) col.enabled = false;
    }
}
