using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance { get; private set; }

    [Header("Assign the GameStats asset here")]
    public GameStats stats;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        stats?.AddPlayTime(Time.unscaledDeltaTime);
    }
}
