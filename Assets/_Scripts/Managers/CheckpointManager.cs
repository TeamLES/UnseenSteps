using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("Refs (rovnaké, ako používa hráč)")]
    public PlayerController player;
    public InventoryData inventory;
    public PlayerAbilitiesData abilities;

    Vector3 lastCheckpointPos;
    bool hasCheckpoint;

    InventorySnap checkpointInv;
    AbilitiesSnap checkpointAbil;

    // flagy pre správy a typ reloadu
    bool pendingRespawn;              // respawn s checkpointom
    bool pendingDiedNoCheckpoint;     // smrť bez checkpointu
    bool pendingMenuRestart;          // tichý reload z menu

    // --- PlayerPrefs keys ---
    const string K_HAS = "CP_Has";
    const string K_X = "CP_X", K_Y = "CP_Y", K_Z = "CP_Z";
    const string K_HEAL = "CP_Heal", K_REVEAL = "CP_Reveal", K_COINS = "CP_Coins", K_KEYS = "CP_Keys";
    const string K_DJ = "CP_DJ", K_DASH = "CP_Dash", K_WJ = "CP_WJ", K_WS = "CP_WS";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromPrefs();
    }

    // Uloženie checkpointu (bez snapshotu scény – itemy sa vždy respawnú)
    [System.Obsolete]
    public void SaveCheckpoint(Checkpoint cp)
    {
        if (!player) player = FindObjectOfType<PlayerController>();
        lastCheckpointPos = cp.SpawnPos;
        hasCheckpoint = true;

        checkpointInv = InventorySnap.From(inventory);
        checkpointAbil = AbilitiesSnap.From(abilities);

        SaveToPrefs();
    }

    // Smrť → respawn / reload
    [System.Obsolete]
    public void RespawnPlayer()
    {
        var active = SceneManager.GetActiveScene();

        if (!hasCheckpoint)
        {
            // Smrť bez checkpointu → čistý reload + vynulovať SO dáta
            ResetDataToDefaults();
            pendingDiedNoCheckpoint = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(active.name);
            return;
        }

        // Máme checkpoint → reloadni scénu, potom aplikuj snapshoty
        pendingRespawn = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(active.name);
    }

    // Restart z menu
    [System.Obsolete]
    public void RestartLevel(bool fullReset = true)
    {
        if (fullReset)
        {
            ResetDataToDefaults();
            ClearSavedCheckpoint();
        }

        pendingMenuRestart = true;
        var s = SceneManager.GetActiveScene();
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(s.name);
    }

    // --- Scene callback ---
    [System.Obsolete]
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // tichý reload z menu
        if (pendingMenuRestart)
        {
            pendingMenuRestart = false;
            return;
        }

        // po čistom reloade bez CP: len „You died“
        if (pendingDiedNoCheckpoint)
        {
            pendingDiedNoCheckpoint = false;
            Toast.Show("You  died");
            return;
        }

        // respawn s checkpointom
        if (!pendingRespawn) return;
        pendingRespawn = false;

        // nájdi hráča
        player = FindObjectOfType<PlayerController>();
        if (player)
        {
            player.transform.position = lastCheckpointPos;
            var rb = player.GetComponent<Rigidbody2D>();
#if UNITY_6000_0_OR_NEWER
            if (rb) rb.linearVelocity = Vector2.zero;
#else
            if (rb) rb.velocity = Vector2.zero;
#endif
        }

        // obnova inventára + schopností
        checkpointInv.ApplyTo(inventory);
        checkpointAbil.ApplyTo(abilities);

        // doplň HP
        var ph = player ? player.GetComponent<PlayerHealth>() : null;
        if (ph) ph.health = ph.maxHealth;

        // aktivuj posledný checkpoint (aby nezobrazoval F)
        ForceActivateCheckpointAt(lastCheckpointPos);

        AudioManager.Instance?.PlaySFX("checkpoint");
        Toast.Show("Respawned");
    }

    // ---------- Persist ----------
    void SaveToPrefs()
    {
        PlayerPrefs.SetInt(K_HAS, hasCheckpoint ? 1 : 0);
        PlayerPrefs.SetFloat(K_X, lastCheckpointPos.x);
        PlayerPrefs.SetFloat(K_Y, lastCheckpointPos.y);
        PlayerPrefs.SetFloat(K_Z, lastCheckpointPos.z);

        PlayerPrefs.SetInt(K_HEAL, checkpointInv.heal);
        PlayerPrefs.SetInt(K_REVEAL, checkpointInv.reveal);
        PlayerPrefs.SetInt(K_COINS, checkpointInv.coins);
        PlayerPrefs.SetInt(K_KEYS, checkpointInv.keys);

        PlayerPrefs.SetInt(K_DJ, checkpointAbil.dj ? 1 : 0);
        PlayerPrefs.SetInt(K_DASH, checkpointAbil.dash ? 1 : 0);
        PlayerPrefs.SetInt(K_WJ, checkpointAbil.wj ? 1 : 0);
        PlayerPrefs.SetInt(K_WS, checkpointAbil.ws ? 1 : 0);

        PlayerPrefs.Save();
    }

    void LoadFromPrefs()
    {
        hasCheckpoint = PlayerPrefs.GetInt(K_HAS, 0) == 1;
        if (!hasCheckpoint) return;

        lastCheckpointPos = new Vector3(
            PlayerPrefs.GetFloat(K_X, 0),
            PlayerPrefs.GetFloat(K_Y, 0),
            PlayerPrefs.GetFloat(K_Z, 0)
        );

        checkpointInv = new InventorySnap
        {
            heal = PlayerPrefs.GetInt(K_HEAL, 0),
            reveal = PlayerPrefs.GetInt(K_REVEAL, 0),
            coins = PlayerPrefs.GetInt(K_COINS, 0),
            keys = PlayerPrefs.GetInt(K_KEYS, 0),
        };

        checkpointAbil = new AbilitiesSnap
        {
            dj = PlayerPrefs.GetInt(K_DJ, 0) == 1,
            dash = PlayerPrefs.GetInt(K_DASH, 0) == 1,
            wj = PlayerPrefs.GetInt(K_WJ, 0) == 1,
            ws = PlayerPrefs.GetInt(K_WS, 0) == 1,
        };
    }

    public static void ClearSavedCheckpoint()
    {
        PlayerPrefs.DeleteKey("CP_Has");
        PlayerPrefs.DeleteKey("CP_X");
        PlayerPrefs.DeleteKey("CP_Y");
        PlayerPrefs.DeleteKey("CP_Z");
        PlayerPrefs.DeleteKey("CP_Heal");
        PlayerPrefs.DeleteKey("CP_Reveal");
        PlayerPrefs.DeleteKey("CP_Coins");
        PlayerPrefs.DeleteKey("CP_Keys");
        PlayerPrefs.DeleteKey("CP_DJ");
        PlayerPrefs.DeleteKey("CP_Dash");
        PlayerPrefs.DeleteKey("CP_WJ");
        PlayerPrefs.DeleteKey("CP_WS");
        // starý kľúč pre snapshot ignorujeme, ale môžeme ho tiež zmazať, ak existuje:
        PlayerPrefs.DeleteKey("CP_PresentIdsV1");
        PlayerPrefs.DeleteKey("CP_PresentIdsV2");
        PlayerPrefs.Save();

        if (Instance != null)
        {
            Instance.hasCheckpoint = false;
            Instance.lastCheckpointPos = Vector3.zero;
        }
    }

    void ResetDataToDefaults()
    {
        if (inventory) inventory.ResetInventory();
        if (abilities)
        {
            abilities.canDoubleJump = false;
            abilities.canDash = false;
            abilities.canWallJump = false;
            abilities.canWallSlide = false;
        }
    }

    // ---------- Snapshots (len inventár/ability) ----------
    [System.Serializable]
    struct InventorySnap
    {
        public int heal, reveal, coins, keys;
        public static InventorySnap From(InventoryData d)
        {
            return new InventorySnap
            {
                heal = d ? d.healPotions : 0,
                reveal = d ? d.revealPotions : 0,
                coins = d ? d.coins : 0,
                keys = d ? d.keys : 0
            };
        }
        public void ApplyTo(InventoryData d)
        {
            if (!d) return;
            d.healPotions = heal;
            d.revealPotions = reveal;
            d.coins = coins;
            d.keys = keys;
            d.RaiseChanged(); // refresh UI
        }
    }

    [System.Serializable]
    struct AbilitiesSnap
    {
        public bool dj, dash, wj, ws;
        public static AbilitiesSnap From(PlayerAbilitiesData a)
        {
            return new AbilitiesSnap
            {
                dj = a && a.canDoubleJump,
                dash = a && a.canDash,
                wj = a && a.canWallJump,
                ws = a && a.canWallSlide
            };
        }
        public void ApplyTo(PlayerAbilitiesData a)
        {
            if (!a) return;
            a.canDoubleJump = dj;
            a.canDash = dash;
            a.canWallJump = wj;
            a.canWallSlide = ws;
        }
    }

    // ---------- Pomocné ----------
    [System.Obsolete]
    void ForceActivateCheckpointAt(Vector3 pos)
    {
        var cps = FindObjectsOfType<Checkpoint>(true);
        if (cps == null || cps.Length == 0) return;

        Checkpoint best = null;
        float bestD = float.MaxValue;

        foreach (var cp in cps)
        {
            var p = cp ? cp.SpawnPos : Vector3.zero;
            float d = (p - pos).sqrMagnitude;
            if (d < bestD) { bestD = d; best = cp; }
        }
        if (!best) return;

        try
        {
            
            var m = typeof(Checkpoint).GetMethod(
                "ForceActivateAfterRespawn",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (m != null) { m.Invoke(best, null); return; }

           
            if (best.animator && !string.IsNullOrEmpty(best.activateTrigger))
                best.animator.SetTrigger(best.activateTrigger);

            var f = typeof(Checkpoint).GetField("isActivated",
                     BindingFlags.Instance | BindingFlags.NonPublic);
            if (f != null) f.SetValue(best, true);
        }
        catch {  }
    }
}
