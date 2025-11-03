using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("References")]
    public EnemyHealth boss;
    public Slider slider;
    public TMP_Text valueText;
    public CanvasGroup canvasGroup;

    [Header("Behavior")]
    public bool autoFindBoss = true;
    public float valueLerpSpeed = 100f;
    public bool hideWhenDead = true;
    public bool startHidden = true;   // NEW

    void Awake()
    {
        if (autoFindBoss && boss == null)
            boss = FindObjectOfType<EnemyHealth>();
    }

    void Start()
    {
        if (!boss || !slider) return;

        slider.minValue = 0;
        slider.maxValue = boss.maxHealth;
        slider.value = boss.currentHealth;
        UpdateText();

        if (canvasGroup)
        {
            if (startHidden) HideImmediate();
            else ShowImmediate();
        }
    }

    void Update()
    {
        if (!boss || !slider) return;

        if (slider.maxValue != boss.maxHealth)
            slider.maxValue = boss.maxHealth;

        slider.value = Mathf.MoveTowards(
            slider.value,
            Mathf.Clamp(boss.currentHealth, 0, boss.maxHealth),
            valueLerpSpeed * Time.deltaTime
        );

        UpdateText();

        if (hideWhenDead && boss.IsDead && canvasGroup)
            HideImmediate();
    }

    void UpdateText()
    {
        if (valueText)
            valueText.text = $"{Mathf.Max(0, boss.currentHealth)} / {boss.maxHealth}";
    }

    // --- Visibility API ---
    public void ShowImmediate()
    {
        if (!canvasGroup) return;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void HideImmediate()
    {
        if (!canvasGroup) return;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void Show() => ShowImmediate();
    public void Hide() => HideImmediate();

    public void SetBoss(EnemyHealth newBoss)
    {
        boss = newBoss;
        if (boss && slider)
        {
            slider.minValue = 0;
            slider.maxValue = boss.maxHealth;
            slider.value = boss.currentHealth;
            UpdateText();
        }
    }
}
