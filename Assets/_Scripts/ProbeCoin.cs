using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ProbeCoin : MonoBehaviour
{
    [Header("Lifetime")]
    public float maxLifetime = 3f;

    [Header("Detection")]
    public LayerMask groundMask;

    [Header("Reveal FX")]
    public GameObject revealZonePrefab;  
    public float revealRadius = 300f;
    public float revealHoldTime = 0.8f;

    [Header("SFX (optional)")]
    public string sfxSafe = ""; 

    void Start()
    {
        Destroy(gameObject, maxLifetime);
    }

    void OnTriggerEnter2D(Collider2D other) => HandleHit(other);
    void OnCollisionEnter2D(Collision2D col) => HandleHit(col.collider);

    void HandleHit(Collider2D other)
    {
        if (!other) return;

        // Ignoruj LevelBorder (ResetOnTrigger)
        if (other.GetComponent<ResetOnTrigger>() != null)
        {
            Destroy(gameObject);
            return;
        }

        // Je to ground?
        int layerBit = 1 << other.gameObject.layer;
        bool isGround = (groundMask.value & layerBit) != 0;
        if (!isGround) return;

        // (volite¾né) zvuk – odporúèam vypnú, ak ho už hrá RevealBombZone
        if (!string.IsNullOrEmpty(sfxSafe) && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(sfxSafe);

        // Reveal
        if (revealZonePrefab != null)
        {
            var go = Instantiate(revealZonePrefab, transform.position, Quaternion.identity);
            go.SetActive(true);

            var zone = go.GetComponent<RevealBombZone>();
            if (zone != null)
                zone.Init(revealRadius, revealHoldTime);
        }

        Destroy(gameObject);
    }
}
