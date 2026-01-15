using UnityEngine;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("Refs")]
    public InventoryData inventoryData;

    [Header("Texts")]
    public TMP_Text healText;
    public TMP_Text revealText;
    public TMP_Text coinsText;
    public TMP_Text keysText;
    public TMP_Text revealBombText;

    [Header("Cooldown Texts")]
    public TMP_Text healCooldownText;
    public TMP_Text revealCooldownText;

    void OnEnable()
    {
        if (inventoryData != null)
            inventoryData.OnInventoryChanged += UpdateCounts;
        UpdateCounts();
        SetCooldowns(0, 0);
    }

    void OnDisable()
    {
        if (inventoryData != null)
            inventoryData.OnInventoryChanged -= UpdateCounts;
    }

    public void UpdateCounts()
    {
        if (!inventoryData) return;
        if (healText) healText.text = $"{inventoryData.healPotions}";
        if (revealText) revealText.text = $"{inventoryData.revealPotions}";
        if (coinsText) coinsText.text = inventoryData.coins.ToString();
        if (keysText) keysText.text = inventoryData.keys.ToString();
        if (revealBombText) revealBombText.text = inventoryData.revealBombs.ToString();
    }

    public void SetCooldowns(float healSeconds, float revealSeconds)
    {
        if (healCooldownText)
        {
            bool showHeal = healSeconds > 0.05f;
            healCooldownText.gameObject.SetActive(showHeal);
            if (showHeal) healCooldownText.text = $"{healSeconds:0.0}";
        }
        if (revealCooldownText)
        {
            bool showReveal = revealSeconds > 0.05f;
            revealCooldownText.gameObject.SetActive(showReveal);
            if (showReveal) revealCooldownText.text = $"{revealSeconds:0.0}";
        }
    }
}
