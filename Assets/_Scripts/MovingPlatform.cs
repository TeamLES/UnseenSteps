using UnityEngine;
using System.Collections;

public class MovingPlatform : MonoBehaviour
{
    [Header("Path Settings")]
    public Transform[] points;
    public float speed = 2f;
    public float waitTime = 2f;

    private int currentPointIndex = 0;
    private bool isWaiting = false;

    void Update()
    {
        if (points.Length == 0 || isWaiting) return;

        Transform target = points[currentPointIndex];
        transform.position = Vector2.MoveTowards(
            transform.position,
            target.position,
            speed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, target.position) < 0.05f)
        {
            StartCoroutine(WaitAndMoveNext());
        }
    }

    IEnumerator WaitAndMoveNext()
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);

        currentPointIndex = (currentPointIndex + 1) % points.Length;
        isWaiting = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            collision.collider.transform.SetParent(transform);
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            collision.collider.transform.SetParent(null);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        if (points != null && points.Length > 1)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 from = points[i].position;
                Vector3 to = points[(i + 1) % points.Length].position;
                Gizmos.DrawLine(from, to);
            }
        }
    }
}
