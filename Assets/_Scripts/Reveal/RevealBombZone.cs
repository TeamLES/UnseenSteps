using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class RevealBombZone : MonoBehaviour
{
    [Header("Circle Settings")]
    public int segments = 80;

    [Header("Reveal Settings")]
    public SpriteMask spriteMask;
    public float maxRevealRadius = 1200f;      // menöie ako fullRevealRadius 4000f
    public float growTime = 0.3f;
    public float holdTime = 1.0f;
    public float fadeTime = 1.5f;

    private LineRenderer line;
    private float currentRadius;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.positionCount = segments + 1;
        line.loop = true;
    }

    void OnEnable()
    {
        AudioManager.Instance?.PlaySFX("reveal");
        StartCoroutine(RevealRoutine());
    }

    public void Init(float maxRadius, float totalDuration)
    {
        maxRevealRadius = maxRadius;
        // voliteæne: z totalDuration si mÙûeö dopoËÌtaù hold/fade, ale kæudne nechaj default hodnoty
    }

    IEnumerator RevealRoutine()
    {
        // GROW
        float t = 0f;
        while (t < growTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / growTime);
            currentRadius = Mathf.Lerp(0f, maxRevealRadius, k);
            ApplyRadius();
            yield return null;
        }

        // HOLD
        currentRadius = maxRevealRadius;
        ApplyRadius();
        yield return new WaitForSeconds(holdTime);

        // FADE OUT (zmenöujeme radius)
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeTime);
            currentRadius = Mathf.Lerp(maxRevealRadius, 0f, k);
            ApplyRadius();
            yield return null;
        }

        Destroy(gameObject);
    }

    void ApplyRadius()
    {
        float worldRadius = currentRadius / 100f;

        // kruh pre LineRenderer
        DrawCircle(transform.position, worldRadius);

        // scale masky (rovnak˝ trik ako pri CursorRevealCircle)
        transform.localScale = Vector3.one * (worldRadius * 1.57f);
        if (spriteMask != null)
            spriteMask.transform.localScale = transform.localScale;
    }

    void DrawCircle(Vector3 center, float radius)
    {
        float angleStep = 360f / segments;
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Deg2Rad * angleStep * i;
            Vector3 point = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            line.SetPosition(i, center + point);
        }
    }
}
