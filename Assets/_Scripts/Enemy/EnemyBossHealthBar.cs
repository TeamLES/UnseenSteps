using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBar : MonoBehaviour
{
    public static BossHealthBar Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Behavior")]
    public float valueLerpSpeed = 100f;
    public bool hideWhenDead = true;

    public EnemyHealth CurrentBoss { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        HideImmediate();
    }

    void Update()
    {
        if (!CurrentBoss || !slider) return;

        // sync max HP
        if (slider.maxValue != CurrentBoss.maxHealth)
            slider.maxValue = CurrentBoss.maxHealth;

        // lerp value
        float target = Mathf.Clamp(CurrentBoss.CurrentHealth, 0, CurrentBoss.maxHealth);
        slider.value = Mathf.MoveTowards(slider.value, target, valueLerpSpeed * Time.deltaTime);

        if (valueText)
            valueText.text = $"{target:0} / {CurrentBoss.maxHealth}";

        if (hideWhenDead && CurrentBoss.IsDead)
            Unbind();
    }

    // --- API pre bossa ---
    public void Bind(EnemyHealth boss)
    {
        CurrentBoss = boss;

        if (!CurrentBoss || !slider)
        {
            HideImmediate();
            return;
        }

        slider.minValue = 0;
        slider.maxValue = CurrentBoss.maxHealth;
        slider.value = CurrentBoss.CurrentHealth;

        if (valueText)
            valueText.text = $"{CurrentBoss.CurrentHealth:0} / {CurrentBoss.maxHealth}";

        ShowImmediate();
    }

    public void Unbind()
    {
        CurrentBoss = null;
        HideImmediate();
    }

    public void ShowImmediate()
    {
        if (slider) slider.gameObject.SetActive(true);
        if (valueText) valueText.gameObject.SetActive(true);

        if (!canvasGroup) return;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public void HideImmediate()
    {
        if (slider) slider.gameObject.SetActive(false);
        if (valueText) valueText.gameObject.SetActive(false);

        if (!canvasGroup) return;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
