using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CursorRevealCircle : MonoBehaviour
{
    [Header("Circle Settings")]
    public int segments = 100;

    [Header("Follow Settings")]
    public float followSpeed = 10f;
    public float defaultSensitivity = 1f;

    [Header("Reveal Mask Settings")]
    public SpriteMask spriteMask;    
    public float revealRadius = 200f;

    private LineRenderer line;
    private Vector3 smoothPosition;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        line = GetComponent<LineRenderer>();
        line.positionCount = segments + 1;
        line.loop = true;

        smoothPosition = transform.position;
    }

    void Update()
    {
        float radius = revealRadius / 100;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        float sensitivity = defaultSensitivity;
        smoothPosition = Vector3.Lerp(smoothPosition, mouseWorld, Time.deltaTime * followSpeed * sensitivity);
        transform.position = smoothPosition;

        DrawCircle(smoothPosition, radius);

        transform.localScale = Vector3.one * (radius * 1.57f);
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
