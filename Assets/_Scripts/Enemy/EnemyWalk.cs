using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyWalk : MonoBehaviour
{
    [Header("Patrol")]
    public float moveSpeed = 2f;
    public Transform groundCheck;
    public Transform wallCheck;
    public LayerMask groundLayer;
    public bool turnOnEdge = true;
    public bool turnOnWall = true;

    [Header("Chase")]
    public bool enableChase = true;
    public string targetTag = "Player";
    public Transform target;
    public float detectionRange = 6f;
    public bool requireLineOfSight = false;
    public float stoppingDistance = 1.1f;
    public float chaseSpeedMultiplier = 1.2f;

    [Tooltip("Maximálny rozdiel po výške, pri ktorom začne chase (hráč musí byť \"na mojej úrovni\").")]
    public float maxVerticalChaseDelta = 0.9f;

    [Tooltip("V chase móde NIKDY nevykračuj z platformy (nechoď do vzduchu).")]
    public bool obeyEdgesDuringChase = true;

    [Tooltip("V chase móde nezatláčaj do steny; radšej zastav.")]
    public bool obeyWallsDuringChase = true;

    [Header("Facing")]
    public bool isFacingRight = true;
    public float flipCooldown = 0.2f;

    [Header("Edge/Wall Cast Distances")]
    public float edgeCheckDistance = 0.25f; // dopredu od groundCheck bodu
    public float wallCheckDistance = 0.5f;  // horizontálny ray

    [Header("Target resolving")]
    public float reacquireEvery = 1.0f;

    [Header("Debug (read-only)")]
    public bool isChasing;
    public bool isAtStopDistance;   // alias kvôli iným skriptom
    public bool hasLOS;

    Rigidbody2D rb;
    float lastFlipTime;
    float _nextReacquireTime = -999f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ResolveTarget();
    }

    void OnEnable() => ResolveTarget();

    void FixedUpdate()
    {
        // ak bol omylom priradený prefab alebo hráč respawnol → fix
        if (Time.time >= _nextReacquireTime)
        {
            if (!IsSceneObject(target)) ResolveTarget();
            _nextReacquireTime = Time.time + reacquireEvery;
        }

        if (!enableChase || target == null || !CanChaseTargetByHeight())
        {
            PatrolTick();
            return;
        }

        hasLOS = !requireLineOfSight || HasLOS();
        float dx = target.position.x - transform.position.x;
        float absDx = Mathf.Abs(dx);
        isAtStopDistance = absDx <= Mathf.Max(0.01f, stoppingDistance);

        if (hasLOS && !isAtStopDistance)
            ChaseTick(dx);
        else if (isAtStopDistance)
        {
            // sme v „sweet spote“ → stoj
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            isChasing = true;
        }
        else
        {
            PatrolTick();
        }
    }

    // --- Ticks ---
    void PatrolTick()
    {
        isChasing = false;
        isAtStopDistance = false;

        if (turnOnEdge) CheckGroundAndFlip();
        if (turnOnWall) CheckWallAndFlip();

        rb.linearVelocity = new Vector2((isFacingRight ? 1f : -1f) * moveSpeed, rb.linearVelocity.y);
    }

    void ChaseTick(float dx)
    {
        isChasing = true;
        LookAtTarget(dx);

        // rešpektuj platformu a steny počas chase
        if (obeyEdgesDuringChase && !HasGroundAhead())
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        if (obeyWallsDuringChase && IsWallAhead())
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float dir = Mathf.Sign(dx);
        float speed = Mathf.Max(0.01f, moveSpeed * chaseSpeedMultiplier);
        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
    }

    // --- Helpers ---
    bool CanChaseTargetByHeight()
    {
        if (target == null) return false;
        float dy = Mathf.Abs(target.position.y - transform.position.y);
        if (dy > maxVerticalChaseDelta) return false;

        // a ešte aj vodorovná vzdialenosť v detekčnom rádiuse
        return Vector2.Distance(transform.position, target.position) <= detectionRange;
    }

    bool HasGroundAhead()
    {
        if (!groundCheck) return true;
        Vector2 origin = groundCheck.position + new Vector3(isFacingRight ? edgeCheckDistance : -edgeCheckDistance, 0f, 0f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 0.35f, groundLayer);
        return hit.collider != null;
    }

    bool IsWallAhead()
    {
        if (!wallCheck) return false;
        Vector2 dir = isFacingRight ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, dir, wallCheckDistance, groundLayer);
        return hit.collider != null;
    }

    void CheckGroundAndFlip()
    {
        if (!HasGroundAhead()) TryFlip();
    }

    void CheckWallAndFlip()
    {
        if (IsWallAhead()) TryFlip();
    }

    bool HasLOS()
    {
        Vector2 origin = transform.position;
        Vector2 toTarget = (target.position - transform.position);
        RaycastHit2D hit = Physics2D.Raycast(origin, toTarget.normalized, toTarget.magnitude, groundLayer);
        return hit.collider == null;
    }

    void LookAtTarget(float dx)
    {
        if (Mathf.Abs(dx) < 0.05f) return;
        if (Time.time - lastFlipTime < flipCooldown) return;

        bool targetOnRight = dx > 0f;
        if ((targetOnRight && !isFacingRight) || (!targetOnRight && isFacingRight))
            DoFlip();
    }

    void TryFlip()
    {
        if (Time.time - lastFlipTime < flipCooldown) return;
        DoFlip();
    }

    public void DoFlip()
    {
        isFacingRight = !isFacingRight;
        var s = transform.localScale; s.x *= -1f; transform.localScale = s;
        lastFlipTime = Time.time;
    }

    public void StopHorizontal() => rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

    bool IsSceneObject(Transform t) => t != null && t.gameObject.scene.IsValid();

    void ResolveTarget()
    {
        if (!IsSceneObject(target))
        {
            var go = GameObject.FindGameObjectWithTag(targetTag);
            if (go) target = go.transform;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * 0.35f);
            Vector3 ahead = groundCheck.position + new Vector3((isFacingRight ? 1f : -1f) * edgeCheckDistance, 0f, 0f);
            Gizmos.DrawLine(ahead, ahead + Vector3.down * 0.35f);
        }
        if (wallCheck)
        {
            Gizmos.color = Color.yellow;
            var dir = isFacingRight ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + dir * wallCheckDistance);
        }
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f); Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, stoppingDistance));
    }
#endif
}
