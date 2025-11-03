using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyWalk))]
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(Rigidbody2D))]
public class Wolf : MonoBehaviour
{
    [Header("Ranges")]
    [Tooltip("Ak je hr�� bli��ie ne� toto, vlk za�ne nah��a�.")]
    public float detectionRange = 6f;
    [Tooltip("Ak je hr�� bli��ie ne� toto, vlk spust� �tok.")]
    public float attackRange = 1.3f;
    [Tooltip("Vzdialenos�, pri ktorej vlk prestane �s� do hr��a (aby sa nelepili). Ak je 0, berie sa 0.8 * attackRange.")]
    public float stoppingDistance = 0f;

    [Header("Chase")]
    [Tooltip("N�sobi� r�chlosti pri nah��an� (1 = rovnak� ako patrol).")]
    public float chaseSpeedMultiplier = 1.2f;
    [Tooltip("Cooldown medzi flipmi, aby sa r�chlo neprekl�pal.")]
    public float flipCooldown = 0.4f;

    [Header("Combat")]
    public float attackCooldown = 0.9f;
    [Tooltip("Vrstva hr��a (pre fallback damage).")]
    public LayerMask playerLayer;
    [Tooltip("Pou�ije sa iba ak hitbox nem� EnemyAttackHitbox komponent.")]
    public int fallbackDamage = 1;

    [Header("Attack Hitbox (child with BoxCollider2D)")]
    [Tooltip("Collider zap�nan� po�as �toku (isTrigger = true).")]
    public BoxCollider2D hitboxCollider;

    private Animator animator;
    private Rigidbody2D rb;
    private EnemyWalk enemyWalk;
    private Transform player;

    private bool isAttacking = false;
    private bool canAttack = true;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        enemyWalk = GetComponent<EnemyWalk>();
        player = GameObject.FindWithTag("Player")?.transform;

        if (hitboxCollider != null)
            hitboxCollider.enabled = false;

        if (stoppingDistance <= 0f)
            stoppingDistance = attackRange * 0.8f; // aby sa neprilepil do hr��a
    }
    void FixedUpdate()
    {
        if (player == null) { enemyWalk.enabled = true; animator.SetBool("isMoving", Mathf.Abs(rb.linearVelocity.x) > 0.01f); return; }

        if (isAttacking)
        {
            enemyWalk.enabled = false;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            animator.SetBool("isMoving", false);
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackRange && canAttack && HasLineOfSight())
        {
            StartCoroutine(AttackRoutine());
            return;
        }

        enemyWalk.enabled = true; // patrol/chase: EnemyWalk
        animator.SetBool("isMoving", Mathf.Abs(rb.linearVelocity.x) > 0.01f);
    }

    bool HasLineOfSight()
    {
        if (player == null) return false;

        Vector2 origin = transform.position;
        Vector2 dir = (player.position - transform.position).normalized;
        float distance = Vector2.Distance(origin, player.position);

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, distance, enemyWalk.groundLayer);
        return hit.collider == null;
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        canAttack = false;

        if (enemyWalk.enabled) enemyWalk.enabled = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        animator.SetBool("isMoving", false);

        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
        canAttack = true;
    }

    public void EnableHitbox()
    {
        isAttacking = true;

        if (hitboxCollider != null)
        {
            var dealer = hitboxCollider.GetComponent<EnemyAttackHitbox>();
            if (dealer != null)
            {
                hitboxCollider.enabled = true;
                dealer.BeginWindow();
            }
            else
            {
                hitboxCollider.enabled = true; // nech sa d� vizu�lne debuggova�/trafi�
                DoFallbackDamage();
            }
        }
    }

    public void DisableHitbox()
    {
        if (hitboxCollider != null)
        {
            var dealer = hitboxCollider.GetComponent<EnemyAttackHitbox>();
            if (dealer != null)
            {
                dealer.EndWindow();
                hitboxCollider.enabled = false;
            }
            else
            {
                hitboxCollider.enabled = false;
            }
        }

        isAttacking = false;
    }

    void DoFallbackDamage()
    {
        // Vezmeme stred a polomer z hitboxu a traf�me 1 hr��a
        Vector2 center = hitboxCollider.bounds.center;
        float radius = Mathf.Max(hitboxCollider.bounds.extents.x, hitboxCollider.bounds.extents.y);

        Collider2D target = Physics2D.OverlapCircle(center, radius, playerLayer);
        if (target != null)
        {
            var ph = target.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(fallbackDamage);
            }
        }
    }

    // --- Debug vizualiz�cia ---
    void OnDrawGizmosSelected()
    {
        // detection range (�lt�) + attack range (�erven�) + stopping (oran�ov�)
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, stoppingDistance > 0 ? stoppingDistance : attackRange * 0.8f);

        // LOS �iara (len v playmode a ke� m�me referencie)
        if (player != null)
        {
            Gizmos.color = HasLineOfSight() ? Color.cyan : new Color(0.3f, 0.3f, 0.3f, 0.8f);
            Gizmos.DrawLine(transform.position, player.position);
        }

        // vizu�l hitboxu (ak existuje)
        if (hitboxCollider != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireCube(hitboxCollider.bounds.center, hitboxCollider.bounds.size);
        }
    }

    void OnValidate()
    {
        detectionRange = Mathf.Max(0f, detectionRange);
        attackRange = Mathf.Max(0.1f, attackRange);
        if (stoppingDistance <= 0f) stoppingDistance = attackRange * 0.8f;
    }
}
