using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseMenuStatsHook : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;  // panel s Resume/Settings/Exit…
    public GameObject statsPanel; // panel so štatistikami

    [Header("First selected (optional)")]
    public Selectable firstMainSelected;
    public Selectable firstStatsSelected;

    public void OpenStats()
    {
        if (mainPanel)  mainPanel.SetActive(false);
        if (statsPanel) statsPanel.SetActive(true);
        statsPanel?.GetComponentInChildren<StatsUI>()?.Refresh();
        if (firstStatsSelected) EventSystem.current?.SetSelectedGameObject(firstStatsSelected.gameObject);
    }

    public void BackFromStats()
    {
        if (statsPanel) statsPanel.SetActive(false);
        if (mainPanel)  mainPanel.SetActive(true);
        if (firstMainSelected) EventSystem.current?.SetSelectedGameObject(firstMainSelected.gameObject);
    }
}
