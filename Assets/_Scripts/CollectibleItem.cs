using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    public enum ItemType { HealPotion, RevealPotion, Coin, Key }

    [Header("Item Settings")]
    public ItemType itemType;
    public int amount = 1;

    [Header("Inventory Reference")]
    public InventoryData inventoryData;

    [Header("Pickup Effect")]
    public GameObject pickupEffectPrefab;

    [Header("Floating Animation")]
    public float floatAmplitude = 0.25f;
    public float floatFrequency = 1f;
    private Vector3 startPos;
    private float phaseOffset;

    private void Start()
    {
        startPos = transform.position;
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float newY = startPos.y + Mathf.Sin((Time.time * floatFrequency) + phaseOffset) * floatAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Collect();
    }

    void Collect()
    {
        switch (itemType)
        {
            case ItemType.HealPotion:   inventoryData.AddHealPotion(amount); PlaySfx("item"); break;
            case ItemType.RevealPotion: inventoryData.AddRevealPotion(amount); PlaySfx("item"); break;
            case ItemType.Coin:         inventoryData.AddCoins(amount); PlaySfx("coin"); StatsManager.Instance?.stats?.AddCoins(1); break;
            case ItemType.Key:          inventoryData.AddKeys(amount); PlaySfx("item"); break;
        }

        if (pickupEffectPrefab) Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    void PlaySfx(string key)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(key);
    }
}