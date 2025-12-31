using UnityEngine;

public class ProbeCoinSetup : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject coinProbePrefab;
    public Transform spawnPoint;

    [Header("Throw (arc)")]
    public float forwardOffset = 0.35f;   // kde sa coin objaví pred hráèom
    public float upwardOffset = 0.15f;
    public float throwForceX = 4.5f;      // rýchlos dopredu
    public float throwForceY = 3.5f;      // rýchlos hore
    public bool useMouseXAsDirection = false; // ak chceš hodi smerom ku kurzoru (iba X)

    public void Drop(Vector2 fallbackWorldOrigin, Transform playerTransform = null)
    {
        if (!coinProbePrefab) return;

        // zober smer (default: pod¾a scale.x ako u teba v PlayerController)
        int dir = 1;
        if (playerTransform != null)
            dir = playerTransform.localScale.x >= 0 ? 1 : -1;

        if (useMouseXAsDirection && Camera.main != null && playerTransform != null)
        {
            float mx = Camera.main.ScreenToWorldPoint(Input.mousePosition).x;
            dir = (mx >= playerTransform.position.x) ? 1 : -1;
        }

        Vector3 basePos = spawnPoint ? spawnPoint.position : (Vector3)fallbackWorldOrigin;
        Vector3 spawnPos = basePos + new Vector3(forwardOffset * dir, upwardOffset, 0f);

        var coin = Instantiate(coinProbePrefab, spawnPos, Quaternion.identity);
        coin.SetActive(true);

        // hod oblúèikom
        var rb = coin.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(throwForceX * dir, throwForceY), ForceMode2D.Impulse);
        }
    }
}
