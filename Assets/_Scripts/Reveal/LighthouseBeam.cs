using UnityEngine;

public class LighthouseBeam : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 30f; // stupne za sekundu

    void Update()
    {
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
    }
}
