using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    Rigidbody2D rb;
    Animator animator;
    bool wasGroundedLastFrame;

    [Header("Movement")]
    public float speed = 8f;
    public float jumpForce = 15f;
    public float maxFallSpeed = -25f;

    [Header("Double Jump")]
    public int maxJumpCount = 2;
    public int currentJumpCount;
    private Coroutine jumpResetCoroutine;

    [Header("Dash")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    bool isDashing;
    [Header("Dash Cooldown")]
    public float dashCooldown = 4f;
    private float lastDashTime = -Mathf.Infinity;

    [Header("Wall Slide & Jump")]
    public Transform wallCheck;
    public Transform groundCheck;
    public float wallCheckDistance = 0.5f;
    public float groundCheckDistance = 0.2f;
    public LayerMask wallLayer;
    public LayerMask groundLayer;
    public float wallSlideSpeed = 2f;
    public float wallJumpForceX = 12f;
    public float wallJumpForceY = 15f;
    public float stickTime = 3f;
    public float wallJumpCooldown = 0.3f;

    [Header("Movement Block")]
    public float movementCheckDistance = 0.6f;
    bool isGrounded;
    bool isTouchingWall;
    bool isWallSliding;
    bool canWallStick = true;
    float wallStickCounter;
    float horizontal;

    [Header("Dragging")]
    [Tooltip("Layers containing dragable objects")]
    public LayerMask dragableLayer;
    public float dragRange = 1.5f;

    private Dragable currentDrag;
    private bool isDragging;

    [Header("Attack Settings")]
    public GameObject attackHitbox;
    private bool isAttacking;

    [Header("VFX")]
    public GameObject dashEffectPrefab;
    public GameObject jumpEffectPrefab;
    public GameObject healEffectPrefab;
    public bool isStunned = false;
    public int damage = 1;

    [Header("Ground + Draggable")]
    [SerializeField] private LayerMask groundAndDragLayer;

    [Header("Player Abilities Data")]
    public PlayerAbilitiesData abilitiesData;

    [Header("Reveal Potion")]
    public bool revealActivated = false;
    public CursorRevealCircle revealCircle;
    public float fullRevealDuration = 5f;
    public float fullRevealRadius = 4000f;
    public KeyCode revealPotionKey = KeyCode.Q;
    private Coroutine fullRevealCo;
    public float revealPotionCooldown = 5f;
    private float lastRevealPotionTime = -Mathf.Infinity;

    [Header("Health Potion")]
    public KeyCode healthPotionKey = KeyCode.E;
    public int healAmount = 2;
    private PlayerHealth playerHealth;
    public float healPotionCooldown = 3f;
    private float lastHealPotionTime = -Mathf.Infinity;

    [Header("Inventory")]
    public InventoryData inventoryData;
    public InventoryManager hud;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        wallStickCounter = stickTime;
        groundAndDragLayer = groundLayer | dragableLayer;
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (PauseMenu.IsPaused) return;  

        if (Input.GetKeyDown(revealPotionKey))
        {
            if (revealActivated == false && Time.time - lastRevealPotionTime >= revealPotionCooldown)
            {
                UseRevealPotion();
                revealActivated = true;
            }
        }
       
        if (Input.GetKeyDown(healthPotionKey))
        {
            if (Time.time - lastHealPotionTime >= healPotionCooldown)
            {
                UseHealthPotion();
            }
        }
        horizontal = Input.GetAxisRaw("Horizontal");
        
        if (isAttacking)
        {
            horizontal = 0f;
            return;
        }

        if (isDragging)
        {

            FaceDragable();


            if (Input.GetMouseButtonUp(1))
                EndDrag();

            rb.linearVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);
            return;
        }

        if (isStunned) return;

        if (DialogueManager.GetInstance().dialogueIsPlaying)
        {
            return;
        }

        if (horizontal > 0 && !isFacingRight()) Flip();
        else if (horizontal < 0 && isFacingRight()) Flip();

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W))
        {
            if ((isWallSliding || isTouchingWall) && abilitiesData.canWallJump)
            {
                StartCoroutine(WallJump());
            }
            else if (isGrounded)
            {

                Jump();
            }
            else if (currentJumpCount > 0 && abilitiesData.canDoubleJump)
            {
                Jump();
            }
        }

        if (Input.GetMouseButtonDown(0) && !isAttacking && isGrounded)
        {
            FaceMouse(); 
            isAttacking = true;
            animator.SetTrigger(AnimationStrings.Attack);
            PlaySfx("playerHit");
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && abilitiesData.canDash && !isDashing && Time.time - lastDashTime >= dashCooldown)
        {
            StartCoroutine(Dash());
        }

        if (Input.GetMouseButtonDown(1))
            TryStartDrag();

        WallSlideCheck();
        UpdateAnimator();
        UpdateCooldownUI();
    }

    void PlaySfx(string key)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(key);
    }

    void FixedUpdate()
    {
        if (PauseMenu.IsPaused) return;

        void FixedUpdate()
        {

            if (rb.linearVelocity.y < maxFallSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);

        }

        if (isAttacking)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (isDashing) return;

        if (DialogueManager.GetInstance().dialogueIsPlaying)
        {
            return;
        }

        if (!isWallSliding)
        {
            if ((horizontal > 0 && !CanMoveInDirection(1)) || (horizontal < 0 && !CanMoveInDirection(-1)))
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
            else
            {
                Vector2 targetVelocity = new Vector2(horizontal * speed, rb.linearVelocity.y);
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, 0.2f);
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, -wallSlideSpeed);
        }

        CheckGround();
        CheckWall();
    }
    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        PlaySfx("jump");
        Vector3 spawnPos = groundCheck.position;
        Instantiate(jumpEffectPrefab, spawnPos, Quaternion.identity);
        currentJumpCount--;
    }
    IEnumerator WallJump()
    {
        canWallStick = false;
        float jumpDirection = isFacingRight() ? -1 : 1;
        rb.linearVelocity = new Vector2(wallJumpForceX * jumpDirection, wallJumpForceY);
        PlaySfx("jump");
        isWallSliding = false;
        wallStickCounter = stickTime;

        yield return new WaitForSeconds(wallJumpCooldown);
        canWallStick = true;
    }
    IEnumerator Dash()
    {

        isDashing = true;
        lastDashTime = Time.time; // tu sa spust cooldown
        Instantiate(dashEffectPrefab, transform.position, Quaternion.identity);
        PlaySfx("dash");
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(horizontal * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;
        isDashing = false;
    }

    void WallSlideCheck()
    {
        if (!abilitiesData.canWallSlide) return;

        if (isTouchingWall && !isGrounded && horizontal != 0 && canWallStick)
        {
            if (wallStickCounter > 0)
            {
                wallStickCounter -= Time.deltaTime;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                isWallSliding = false;
            }
            else
            {
                isWallSliding = true;
            }
        }
        else
        {
            isWallSliding = false;
            wallStickCounter = stickTime;
        }
    }

    void CheckGround()
    {
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundAndDragLayer);

        bool wasGrounded = isGrounded;
        isGrounded = hit.collider != null;

        // Skuto�n� dopad: z vo vzduchu -> na zem
        if (!wasGrounded && isGrounded)
        {
            SpawnLandingEffect();

            if (jumpResetCoroutine != null)
                StopCoroutine(jumpResetCoroutine);
            jumpResetCoroutine = StartCoroutine(ResetJumpCountAfterDelay(0.05f));
        }

        wasGroundedLastFrame = isGrounded;
    }

    void SpawnLandingEffect()
    {
        if (jumpEffectPrefab != null)
        {
            Instantiate(jumpEffectPrefab, groundCheck.position, Quaternion.identity);
        }
    }

    void CheckWall()
    {
        Vector2 direction = isFacingRight() ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, direction, wallCheckDistance, wallLayer);
        isTouchingWall = hit.collider != null;
    }

    bool CanMoveInDirection(float dir)
    {
        Vector2 direction = dir > 0 ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, movementCheckDistance, groundAndDragLayer);
        return hit.collider == null;
    }

    void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    bool isFacingRight()
    {
        return transform.localScale.x > 0;
    }

    void UpdateAnimator()
    {
        animator.SetBool(AnimationStrings.IsMoving, horizontal != 0);
        animator.SetBool(AnimationStrings.IsGrounded, isGrounded);
        animator.SetFloat(AnimationStrings.YVelocity, rb.linearVelocity.y);
    }

    void OnDrawGizmos()
    {
        if (wallCheck != null)
        {
            Gizmos.color = Color.red;
            Vector2 direction = isFacingRight() ? Vector2.right : Vector2.left;
            Gizmos.DrawLine(wallCheck.position, (Vector2)wallCheck.position + direction * wallCheckDistance);
        }

        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(groundCheck.position, (Vector2)groundCheck.position + Vector2.down * groundCheckDistance);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + Vector2.right * movementCheckDistance);
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + Vector2.left * movementCheckDistance);
    }

    public void EnableAttackHitbox()
    {
        // resetneme a zapneme hitbox collider
        attackHitbox.SetActive(false);
        attackHitbox.SetActive(true);

        // zskame BoxCollider2D a prepotame pozciu do world space
        BoxCollider2D col = attackHitbox.GetComponent<BoxCollider2D>();
        Vector2 worldPos = attackHitbox.transform.TransformPoint(col.offset);
        Vector2 size = col.size;
        float angle = attackHitbox.transform.eulerAngles.z;

        // OBSAHOVO HADME VETKY objekty v hitboxe
        Collider2D[] hits = Physics2D.OverlapBoxAll(worldPos, size, angle);

        foreach (var h in hits)
        {
            IDamageable damageable = h.GetComponent<IDamageable>();
            if (damageable != null && h.transform.root != transform.root)
            {
                damageable.TakeDamage(damage);
            }
        }
    }

    public void DisableAttackHitbox()
    {
        attackHitbox.SetActive(false);
        isAttacking = false;
    }

    public IEnumerator Stun(float duration)
    {
        isStunned = true;
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    private void TryStartDrag()
    {
        // Najprv njdi najbli collider v okruhu
        Collider2D hit = Physics2D.OverlapCircle(transform.position, dragRange, dragableLayer);
        if (hit == null) return;

        Dragable dr = hit.GetComponent<Dragable>();
        if (dr != null && dr.IsInRange(transform.position))
        {
            // Zistme skuton bod dotyku na kolderi
            Vector2 worldAnchor = hit.ClosestPoint(transform.position);

            // A poleme ho do StartDrag
            dr.StartDrag(rb, worldAnchor);

            currentDrag = dr;
            isDragging = true;
        }
    }

    private void EndDrag()
    {
        if (currentDrag != null)
        {
            currentDrag.EndDrag();
            currentDrag = null;
        }
        isDragging = false;
    }
    private void FaceMouse()
    {
        if (Camera.main == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float dx = mouseWorld.x - transform.position.x;

        if (dx > 0f && !isFacingRight()) Flip();
        else if (dx < 0f && isFacingRight()) Flip();
    }

    private void FaceDragable()
    {
        if (currentDrag == null) return;
        bool objectOnRight = currentDrag.transform.position.x > transform.position.x;

        if (objectOnRight && !isFacingRight()) Flip();

        else if (!objectOnRight && isFacingRight()) Flip();
    }

    private IEnumerator ResetJumpCountAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        currentJumpCount = maxJumpCount;
    }

    public void UseRevealPotion()
    {
        if (revealCircle == null || inventoryData == null) return;
        if (!inventoryData.UseRevealPotion()) return;
        if (fullRevealCo != null) StopCoroutine(fullRevealCo);
        fullRevealCo = StartCoroutine(FullRevealRoutine());
    }

    public void UseHealthPotion()
    {
        if (inventoryData == null || playerHealth == null) return;
        if (playerHealth.health >= playerHealth.maxHealth) return;

        if (inventoryData.UseHealPotion())
        {
            playerHealth.Heal(healAmount);
            lastHealPotionTime = Time.time;

            // Spustenie efektu healu iba teraz
            if (healEffectPrefab != null)
            {
                var fx = Instantiate(
                    healEffectPrefab,
                    transform.position + Vector3.up * 0.1f,
                    Quaternion.identity
                    //transform // nech sa to „vezie“ s hráčom
                );

                // explicitne pustíme particle (keď je Play On Awake OFF)
                var ps = fx.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.stopAction = ParticleSystemStopAction.Destroy;

                    ps.Play(true);
                }
            }
        }
    }

    private IEnumerator FullRevealRoutine()
    {
        float originalRadius = revealCircle.revealRadius;
        float targetRadius = fullRevealRadius;

        float growTime = 1f;
        float timer = 0f;
        revealActivated = true;
        while (timer < growTime)
        {
            timer += Time.deltaTime;
            float t = timer / growTime;
            revealCircle.revealRadius = Mathf.Lerp(originalRadius, targetRadius, t);
            yield return null;
        }

        yield return new WaitForSeconds(fullRevealDuration - growTime);

        timer = 0f;
        while (timer < growTime)
        {
            timer += Time.deltaTime;
            float t = timer / growTime;
            revealCircle.revealRadius = Mathf.Lerp(targetRadius, originalRadius, t);
            yield return null;
        }

        revealCircle.revealRadius = originalRadius;

        fullRevealCo = null;
        lastRevealPotionTime = Time.time;
        revealActivated = false;
    }

    private void UpdateCooldownUI()
    {
        if (!hud) return;

        float healRemaining = Mathf.Max(0f, healPotionCooldown - (Time.time - lastHealPotionTime));
        float revealRemaining = Mathf.Max(0f, revealPotionCooldown - (Time.time - lastRevealPotionTime));
        hud.SetCooldowns(healRemaining, revealRemaining);
    }
}