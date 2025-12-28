using UnityEngine;

public class ProbeCoinSetup : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject coinProbePrefab;     // prefab, ktor˝ m· na sebe ProbeCoin script
    public Transform spawnPoint;           // napr. z ruky / pod hr·Ëom
    public float initialDownSpeed = 0f;    // 0 = nech to rieöi gravity

    public void Drop(Vector2 worldOrigin)
    {
        if (!coinProbePrefab) return;

        Vector3 pos = spawnPoint ? spawnPoint.position : (Vector3)worldOrigin;

        var coin = Instantiate(coinProbePrefab, pos, Quaternion.identity);
        coin.SetActive(true); // keÔûe tvoj prefab je uloûen˝ disabled

        // voliteæne mu daj ötartovaciu r˝chlosù dole
        var rb = coin.GetComponent<Rigidbody2D>();
        if (rb != null && initialDownSpeed != 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -Mathf.Abs(initialDownSpeed));
    }
}