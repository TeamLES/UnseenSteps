using UnityEngine;
using TMPro;

public class StatsUI : MonoBehaviour
{
    [Header("Source (optional)")]
    public GameStats stats; // ak je null, vezme z StatsManager.Instance

    [Header("Texts")]
    public TMP_Text timeText;
    public TMP_Text deathsText;
    public TMP_Text killsText;
    public TMP_Text coinsText;
    public TMP_Text potionsText;
    public TMP_Text abilitiesText;

    [Tooltip("Aktualizovať čas hrania raz za sekundu, keď je panel otvorený.")]
    public bool autoRefreshEachSecond = true;

    float nextTick;

    GameStats S => stats ? stats : StatsManager.Instance ? StatsManager.Instance.stats : null;

    void OnEnable()
    {
        Refresh();
        nextTick = 0f;
    }

    void Update()
    {
        if (!autoRefreshEachSecond) return;
        if (Time.unscaledTime >= nextTick) { nextTick = Time.unscaledTime + 1f; RefreshQuick(); }
    }

    public void Refresh()
    {
        var s = S; if (s == null) return;
        if (timeText)      timeText.text      = "Time played: " + FormatTime(s.TimePlayedSeconds);
        if (deathsText)    deathsText.text    = "Deaths: " + s.PlayerDeaths.ToString();
        if (killsText)     killsText.text     = "Enemies killed: " + s.TotalEnemyKills.ToString();
        if (coinsText)     coinsText.text     = "Coins collected: " + s.CoinsCollected.ToString();
        if (potionsText)   potionsText.text   = "Potions used: " + s.PotionsUsed.ToString();
        if (abilitiesText) abilitiesText.text = "Abilities used: " + s.AbilitiesUsed.ToString();
    }

    void RefreshQuick()
    {
        var s = S; if (s == null || !timeText) return;
        timeText.text = "Time played: " + FormatTime(s.TimePlayedSeconds);
    }

    string FormatTime(float secs)
    {
        int t = Mathf.Max(0, Mathf.FloorToInt(secs));
        int h = t / 3600, m = (t % 3600) / 60, s = t % 60;
        return h > 0 ? $"{h:00}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
    }
}
