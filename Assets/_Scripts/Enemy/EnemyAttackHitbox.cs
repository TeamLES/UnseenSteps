using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class EnemyAttackHitbox : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 1;
    public string targetTag = "Player";

    [Header("Window Mode")]
    [Tooltip("Ak je true: zavrie okno po prvom z·sahu (ötandard u menöÌch nepriateæov).")]
    public bool closeWindowOnFirstHit = true;
    [Tooltip("Ak je true: mÙûe zasiahnuù viac cieæov v r·mci jednÈho okna (Boss).")]
    public bool allowMultipleTargets = false;

    [Header("Knockback (optional)")]
    public float knockbackForce = 0f;
    public Vector2 knockbackDir = Vector2.right;

    // Eventy pre AI (kompatibilnÈ s Enemy.cs)
    public event Action OnSuccessfulHit;
    public event Action OnMiss;

    private bool windowOpen = false;
    private bool hitAnyThisWindow = false;
    private readonly HashSet<GameObject> hitSet = new HashSet<GameObject>();
    private Collider2D col;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    // Volaj z Animation Event: zaËiatok aktÌvneho okna
    public void BeginWindow()
    {
        windowOpen = true;
        hitAnyThisWindow = false;
        hitSet.Clear();
    }

    // Volaj z Animation Event: koniec aktÌvneho okna
    public void EndWindow()
    {
        windowOpen = false;
        if (!hitAnyThisWindow) OnMiss?.Invoke();
        hitSet.Clear();
    }

    void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    void OnTriggerStay2D(Collider2D other) => TryHit(other);

    private void TryHit(Collider2D other)
    {
        if (!windowOpen) return;
        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag)) return;

        // n·jdi PlayerHealth (ako doteraz) ñ je to tvoj hlavn˝ prijÌmaË DMG
        var ph = other.GetComponent<PlayerHealth>()
              ?? other.GetComponentInParent<PlayerHealth>();

        if (ph == null) return;

        // zabr·Ú duplicitnÈmu z·sahu rovnakÈho cieæa v tom istom okne
        if (!allowMultipleTargets && hitAnyThisWindow) return;
        if (allowMultipleTargets && hitSet.Contains(ph.gameObject)) return;

        // urob damage (zachov·me tvoj signature s pozÌciou zdroja)
        ph.TakeDamage(damage, transform.position);
        hitAnyThisWindow = true;
        hitSet.Add(ph.gameObject);
        OnSuccessfulHit?.Invoke();

        // voliteæn˝ knockback
        if (knockbackForce > 0f)
        {
            var rb = ph.GetComponent<Rigidbody2D>() ?? other.attachedRigidbody;
            if (rb != null)
            {
                float dirSign = Mathf.Sign(transform.lossyScale.x);
                Vector2 dir = new Vector2(knockbackDir.x * dirSign, knockbackDir.y).normalized;
                rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
            }
        }

        if (closeWindowOnFirstHit) windowOpen = false;
    }
}
