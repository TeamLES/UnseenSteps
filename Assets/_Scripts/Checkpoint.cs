using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Setup")]
    public Transform spawnPoint;

    [Header("VFX/Audio/Anim")]
    public Animator animator;
    public string activateTrigger = "Activate";

    bool isActivated;

    void Reset()
    {
        var sp = new GameObject("SpawnPoint");
        sp.transform.SetParent(transform);
        sp.transform.localPosition = Vector3.up * 0.5f;
        spawnPoint = sp.transform;
    }

    void Start()
    {
        if (!animator) animator = GetComponent<Animator>();
    }

    void Activate()
    {
        if (isActivated) return;          
        isActivated = true;

        if (animator && !string.IsNullOrEmpty(activateTrigger))
            animator.SetTrigger(activateTrigger);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("checkpoint");

        CheckpointManager.Instance.SaveCheckpoint(this);
        Toast.Show("Saved!");
    }

    public void ForceActivateAfterRespawn()
    {
        if (isActivated) return;
        isActivated = true;

        if (animator && !string.IsNullOrEmpty(activateTrigger))
            animator.SetTrigger(activateTrigger);
    }

    void OnTriggerEnter2D(Collider2D c)
    {
        if (!isActivated && c.CompareTag("Player"))
            Activate();                
    }

    public Vector3 SpawnPos => (spawnPoint ? spawnPoint.position : transform.position);
}
