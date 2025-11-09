using UnityEngine;
using UnityEngine.SceneManagement;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; }

    [Header("Assign the GameStats asset here")]
    public GameStats stats;

    [Header("Scenes considered gameplay (optional)")]
    public string[] gameplayScenes; // leave empty to auto-detect by PlayerController

    bool isGameplayScene;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        RecomputeIsGameplay();
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RecomputeIsGameplay();

    void RecomputeIsGameplay()
    {
        // 1) If you listed gameplay scenes, use that
        if (gameplayScenes != null && gameplayScenes.Length > 0)
        {
            string active = SceneManager.GetActiveScene().name;
            isGameplayScene = System.Array.Exists(gameplayScenes, s => s == active);
            return;
        }

        // 2) Otherwise auto-detect: if there's a PlayerController in scene, we’re in gameplay
        isGameplayScene = FindObjectOfType<PlayerController>() != null;
    }

    void Update()
    {
        if (stats == null) return;
        if (!isGameplayScene) return;            
        if (!PauseMenu.IsPaused && SceneManager.GetActiveScene().name != "Menu")
        stats.AddPlayTime(Time.deltaTime);
    }
}
