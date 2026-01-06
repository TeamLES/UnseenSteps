using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RevealCursorDisableZone : MonoBehaviour
{
    [Tooltip("Ak nemáš Player tag, daj sem LayerMask a sprav si vlastnú kontrolu.")]
    public string playerTag = "Player";

    void OnDisable()
    {
        RevealCursorController.Instance?.RemoveBlock(this);
    }

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        RevealCursorController.Instance?.AddBlock(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        RevealCursorController.Instance?.RemoveBlock(this);
    }
}
