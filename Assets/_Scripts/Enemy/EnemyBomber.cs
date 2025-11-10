using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyWalk))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBomber : MonoBehaviour
{
    [Header("Detection / Chase")]
    public float detectionRange = 6f;
    public float explodeRange = 1.25f;
    public bool requireLineOfSight = false;

    [Header("Movement")]
    public float patrolSpeed = 1.4f;
    public float chaseSpeed = 2.4f;
    public float flipCooldown = 0.35f;

    [Header("Explosion")]
    public int explosionDamage = 2;
    public float explosionRadius = 1.75f;
    public float explosionForce = 12f;

    [Header("Fuse")]
    [Tooltip("Safety delay right after fuse starts before counting down.")]
    public float armingDelay = 0.1f;
    [Tooltip("How long it hisses before exploding.")]
    public float fuseTime = 0.8f;
    [Tooltip("Time before a new fuse can start after a cancel.")]
    public float rearmCooldown = 2.0f;
    [Tooltip("Cancel fuse if target goes this much farther than explodeRange.")]
    public float cancelExtraDistance = 0.6f;
    [Tooltip("Cancel fuse if LOS is required and then lost.")]
    public bool cancelOnLostLOS = true;

    [Header("Fuse Movement")]
    [Tooltip("If true, bomber keeps moving/chasing while the fuse is burning.")]
    public bool moveWhileFusing = true;

    [Header("FX / SFX")]
    public GameObject explosionVfx;
    public string explodeSfx = "enemyExplode";
    [Tooltip("Hissing loop played while the fuse is active (local AudioSource).")]
    public string fuseHissSfx = "enemyFuse";

    private EnemyWalk enemyWalk;
    private EnemyHealth enemyHealth;
    private Rigidbody2D rb;
    private Animator animator;
    private Transform player;

    private bool isFusing = false;
    private bool isExploding = false;
    private bool fuseCanceled = false;
    private float lastFuseEndTime = -Mathf.Infinity;

    // lok·lny zdroj pre z·palnicu (tvrdÈ stopnutie = Stop()+Destroy)
    private AudioSource fuseLoopSrc;

    void Awake()
    {
        enemyWalk = GetComponent<EnemyWalk>();
        enemyHealth = GetComponent<EnemyHealth>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void FixedUpdate()
    {
        if (player == null || isExploding) return;

        if (isFusing)
        {
            // Ak nechceme pohyb poËas fuse, zafixuj horizont·lnu r˝chlosù
            if (!moveWhileFusing)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            float dist = Vector2.Distance(transform.position, player.position);
            bool lostTooFar = dist > (explodeRange + cancelExtraDistance);
            bool lostLOS = requireLineOfSight && cancelOnLostLOS && !HasLineOfSight();

            if (lostTooFar || lostLOS)
            {
                CancelFuse();
            }
            return;
        }

        float d = Vector2.Distance(transform.position, player.position);
        bool canStart = d <= explodeRange && (!requireLineOfSight || HasLineOfSight());
        bool offCooldown = (Time.time - lastFuseEndTime) >= rearmCooldown;

        if (canStart && offCooldown)
        {
            StartFuse();
            return;
        }
    }

    bool HasLineOfSight()
    {
        Vector2 origin = transform.position;
        Vector2 dir = (player.position - transform.position).normalized;
        float distance = Vector2.Distance(origin, player.position);
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, distance, enemyWalk.groundLayer);
        return hit.collider == null;
    }

    void StartFuse()
    {
        if (isFusing || isExploding) return;

        isFusing = true;
        fuseCanceled = false;

        // Ak chceme pohyb poËas fuse, nechaj EnemyWalk beûaù
        enemyWalk.enabled = true;
        if (!moveWhileFusing)
            rb.linearVelocity = Vector2.zero;

        // NENASTAVUJ IsMoving na false, nech si anim·cie rieöi EnemyWalk
        animator.SetTrigger(EnemyBomberAnimationStrings.Fuse);

        // HISS ñ spusti lok·lne na tomto objekte
        if (!string.IsNullOrEmpty(fuseHissSfx) &&
            AudioManager.Instance.TryGetSFXClip(fuseHissSfx, out var clip, out var vol))
        {
            fuseLoopSrc = gameObject.AddComponent<AudioSource>();
            fuseLoopSrc.clip = clip;
            fuseLoopSrc.volume = vol;
            fuseLoopSrc.loop = true;
            fuseLoopSrc.playOnAwake = false;
            fuseLoopSrc.spatialBlend = 0f; // 2D
            fuseLoopSrc.Play();
        }

        StartCoroutine(FuseRoutine());
    }

    IEnumerator FuseRoutine()
    {
        yield return new WaitForSeconds(armingDelay);

        float t = 0f;
        while (t < fuseTime && !fuseCanceled)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // NATVRDO zastav a zniË z·palnicu po fuse okne
        StopFuseLoopHard();

        if (fuseCanceled) yield break; // zruöenÈ -> sp‰ù k chase

        Explode();
    }

    void CancelFuse()
    {
        if (!isFusing || isExploding) return;

        isFusing = false;
        fuseCanceled = true;
        lastFuseEndTime = Time.time;

        animator.ResetTrigger(EnemyBomberAnimationStrings.Fuse);

        // nechaj EnemyWalk fungovaù
        enemyWalk.enabled = true;

        // NATVRDO stop hneÔ pri zruöenÌ
        StopFuseLoopHard();
    }

    void Explode()
    {
        if (isExploding) return;
        isExploding = true;

        // istota ñ vûdy stopni z·palnicu pred v˝buchom
        StopFuseLoopHard();

        if (!string.IsNullOrEmpty(explodeSfx))
            AudioManager.Instance?.PlaySFX(explodeSfx);

        animator.SetTrigger(EnemyBomberAnimationStrings.Explode);

        if (explosionVfx) Instantiate(explosionVfx, transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var col in hits)
        {
            if (!col || col.gameObject == gameObject) continue;

            var dmg = col.GetComponent<IDamageable>();
            if (dmg != null) dmg.TakeDamage(explosionDamage);

            var body = col.attachedRigidbody;
            if (body != null)
            {
                Vector2 dir = (col.transform.position - transform.position).normalized;
                body.AddForce(dir * explosionForce, ForceMode2D.Impulse);
            }
        }

        Destroy(gameObject, 0.02f);
    }

    void OnDisable()
    {
        // cleanup pre prÌpad zmeny scÈny / disable poËas fuse
        StopFuseLoopHard();
    }

    void StopFuseLoopHard()
    {
        if (!fuseLoopSrc) return;
        fuseLoopSrc.Stop();            // natvrdo zastav
#if UNITY_EDITOR
        DestroyImmediate(fuseLoopSrc);
#else
        Destroy(fuseLoopSrc);
#endif
        fuseLoopSrc = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, explodeRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}

public static class EnemyBomberAnimationStrings
{
    public const string IsMoving = "isMoving";
    public const string Fuse = "fuse";
    public const string Explode = "explode";
}
