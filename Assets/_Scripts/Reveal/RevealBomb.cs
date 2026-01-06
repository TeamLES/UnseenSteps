using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RevealBomb : MonoBehaviour
{
    [Header("Movement")]
    public float throwSpeed = 10f;
    public float maxLifetime = 5f;

    [Header("Reveal Zone")]
    public GameObject revealZonePrefab;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // zavolá Player pri hodení
    public void Throw(Vector2 direction)
    {
        direction.Normalize();
        rb.linearVelocity = direction * throwSpeed;
        Destroy(gameObject, maxLifetime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // tu môžeš filtrova èo a zaujíma, ale default staèí:
        Vector2 hitPoint = collision.contacts.Length > 0
            ? collision.contacts[0].point
            : (Vector2)transform.position;

        SpawnRevealZone(hitPoint);
        Destroy(gameObject);
    }

    void SpawnRevealZone(Vector2 position)
    {
        if (!revealZonePrefab) return;

        // poistka na Z (2D)
        var pos3 = new Vector3(position.x, position.y, 0f);

        var zoneObj = Instantiate(revealZonePrefab, pos3, Quaternion.identity);

        // ak bol prefab omylom uložený ako inactive, klon bude tiež inactive
        if (!zoneObj.activeSelf)
            zoneObj.SetActive(true);
    }
}
