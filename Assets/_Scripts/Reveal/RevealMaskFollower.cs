using UnityEngine;

public class RevealMaskFollower : MonoBehaviour
{
    public float revealRadius = 200f;
    public float followSpeed = 10f;
    public float defaultSensitivity = 1f;

    private Camera cam;
    private Vector3 smoothPosition;

    void Start()
    {
        cam = Camera.main;
        smoothPosition = transform.position;
    }

    void Update()
    {
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        float sensitivity = defaultSensitivity;
        

        smoothPosition = Vector3.Lerp(smoothPosition, mouseWorld, Time.deltaTime * followSpeed * sensitivity);
        transform.position = smoothPosition;
    }
}
