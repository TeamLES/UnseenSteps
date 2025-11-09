using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "UnseenSteps/Game Stats", fileName = "GameStats")]
public class GameStats : ScriptableObject
{
    [Header("Totals")]
    [SerializeField] private int totalEnemyKills = 0;
    [SerializeField] private int playerDeaths = 0;
    [SerializeField] private int coinsCollected = 0;
    [SerializeField] private int potionsUsed = 0;
    [SerializeField] private int abilitiesUsed = 0;
    [SerializeField] private float timePlayedSeconds = 0f;

    [Serializable] public class EnemyKillEntry { public string id; public int count; }
    [Serializable] public class StringCount { public string id; public int count; } // potions / abilities

    [Header("Per-Enemy Kills (Inspector)")]
    [SerializeField] private List<EnemyKillEntry> perEnemyList = new List<EnemyKillEntry>();

    [Header("Per-Potion Uses (Inspector)")]
    [SerializeField] private List<StringCount> perPotionList = new List<StringCount>();

    [Header("Per-Ability Uses (Inspector)")]
    [SerializeField] private List<StringCount> perAbilityList = new List<StringCount>();

    // runtime lookups
    [NonSerialized] private Dictionary<string, int> perEnemy = new(StringComparer.Ordinal);
    [NonSerialized] private Dictionary<string, int> perPotion = new(StringComparer.Ordinal);
    [NonSerialized] private Dictionary<string, int> perAbility = new(StringComparer.Ordinal);

    const string PREF_KEY = "US_GameStats_v2"; // bump verziu (v1 -> v2)

    // --- Public getters ---
    public int TotalEnemyKills => totalEnemyKills;
    public int PlayerDeaths => playerDeaths;
    public int CoinsCollected => coinsCollected;
    public int PotionsUsed => potionsUsed;
    public int AbilitiesUsed => abilitiesUsed;
    public float TimePlayedSeconds => timePlayedSeconds;

    void OnEnable()
    {
        RebuildDictsFromLists();
        LoadFromPrefs();
    }

    // ---------- Rebuild helpers ----------
    void RebuildDictsFromLists()
    {
        perEnemy.Clear();
        foreach (var e in perEnemyList) if (e != null && !string.IsNullOrEmpty(e.id)) perEnemy[e.id] = e.count;

        perPotion.Clear();
        foreach (var p in perPotionList) if (p != null && !string.IsNullOrEmpty(p.id)) perPotion[p.id] = p.count;

        perAbility.Clear();
        foreach (var a in perAbilityList) if (a != null && !string.IsNullOrEmpty(a.id)) perAbility[a.id] = a.count;
    }

    void RebuildListsFromDicts()
    {
        perEnemyList.Clear();
        foreach (var kv in perEnemy) perEnemyList.Add(new EnemyKillEntry { id = kv.Key, count = kv.Value });

        perPotionList.Clear();
        foreach (var kv in perPotion) perPotionList.Add(new StringCount { id = kv.Key, count = kv.Value });

        perAbilityList.Clear();
        foreach (var kv in perAbility) perAbilityList.Add(new StringCount { id = kv.Key, count = kv.Value });
    }

    // ---------- Public API (incrementy) ----------
    public void AddEnemyKill(string enemyId)
    {
        if (string.IsNullOrEmpty(enemyId)) enemyId = "Enemy";
        totalEnemyKills++;
        perEnemy[enemyId] = perEnemy.TryGetValue(enemyId, out var c) ? c + 1 : 1;
        RebuildListsFromDicts(); SaveToPrefs();
    }

    public void AddPlayerDeath() { playerDeaths++; SaveToPrefs(); }

    public void AddCoins(int amount) { if (amount <= 0) return; coinsCollected += amount; SaveToPrefs(); }

    /// <summary>potionId: napr. "Heal", "Reveal"</summary>
    public void AddPotionUse(string potionId, int amount = 1)
    {
        if (amount <= 0) return;
        potionsUsed += amount;
        if (string.IsNullOrEmpty(potionId)) potionId = "Potion";
        perPotion[potionId] = perPotion.TryGetValue(potionId, out var c) ? c + amount : amount;
        RebuildListsFromDicts(); SaveToPrefs();
    }

    /// <summary>abilityId: napr. "Dash","DoubleJump","WallSlide","WallJump","Attack"</summary>
    public void AddAbilityUse(string abilityId, int amount = 1)
    {
        if (amount <= 0) return;
        abilitiesUsed += amount;
        if (string.IsNullOrEmpty(abilityId)) abilityId = "Ability";
        perAbility[abilityId] = perAbility.TryGetValue(abilityId, out var c) ? c + amount : amount;
        RebuildListsFromDicts(); SaveToPrefs();
    }

    /// <summary>Pripíš èas hrania (sekundy). Volaj napr. z managera každým frame: AddPlayTime(Time.unscaledDeltaTime)</summary>
    public void AddPlayTime(float seconds)
    {
        if (seconds <= 0f) return;
        timePlayedSeconds += seconds;
        // nechceme spamova PlayerPrefs každý frame:
        // ulož si to napr. v menu alebo pri checkpointoch (ponúkame aj manuálne SaveToPrefs())
    }

    // ---------- Queries ----------
    public int GetEnemyKills(string enemyId) => perEnemy.TryGetValue(enemyId, out var c) ? c : 0;
    public int GetPotionUses(string potionId) => perPotion.TryGetValue(potionId, out var c) ? c : 0;
    public int GetAbilityUses(string abilityId) => perAbility.TryGetValue(abilityId, out var c) ? c : 0;

    public IReadOnlyDictionary<string, int> GetAllEnemyKills() => perEnemy;
    public IReadOnlyDictionary<string, int> GetAllPotionUses() => perPotion;
    public IReadOnlyDictionary<string, int> GetAllAbilityUses() => perAbility;

    [ContextMenu("Reset All Stats")]
    public void ResetAll()
    {
        totalEnemyKills = 0;
        playerDeaths = 0;
        coinsCollected = 0;
        potionsUsed = 0;
        abilitiesUsed = 0;
        timePlayedSeconds = 0f;

        perEnemy.Clear(); perEnemyList.Clear();
        perPotion.Clear(); perPotionList.Clear();
        perAbility.Clear(); perAbilityList.Clear();
        SaveToPrefs();
    }

    // ---------- Persist cez PlayerPrefs (JSON) ----------
    [Serializable]
    class SaveData
    {
        public int totalKills;
        public int deaths;
        public int coins;
        public int potions;
        public int abilities;
        public float time;
        public List<EnemyKillEntry> perEnemy;
        public List<StringCount> perPotion;
        public List<StringCount> perAbility;
    }

    public void SaveToPrefs()
    {
        var data = new SaveData
        {
            totalKills = totalEnemyKills,
            deaths = playerDeaths,
            coins = coinsCollected,
            potions = potionsUsed,
            abilities = abilitiesUsed,
            time = timePlayedSeconds,
            perEnemy = perEnemyList,
            perPotion = perPotionList,
            perAbility = perAbilityList
        };
        var json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(PREF_KEY, json);
        PlayerPrefs.Save();
    }

    void LoadFromPrefs()
    {
        if (!PlayerPrefs.HasKey(PREF_KEY)) return;
        var json = PlayerPrefs.GetString(PREF_KEY, "");
        if (string.IsNullOrEmpty(json)) return;

        var data = JsonUtility.FromJson<SaveData>(json);
        if (data == null) return;

        totalEnemyKills = data.totalKills;
        playerDeaths = data.deaths;
        coinsCollected = data.coins;
        potionsUsed = data.potions;
        abilitiesUsed = data.abilities;
        timePlayedSeconds = data.time;

        perEnemyList = data.perEnemy ?? new List<EnemyKillEntry>();
        perPotionList = data.perPotion ?? new List<StringCount>();
        perAbilityList = data.perAbility ?? new List<StringCount>();
        RebuildDictsFromLists();
    }
}
