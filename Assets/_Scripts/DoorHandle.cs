using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class DoorHandle : MonoBehaviour
{
    [Header("Setup")]
    public InventoryData inventoryData;
    public KeyCode interactKey = KeyCode.F;

    [Header("Dust FX")]
    public ParticleSystem doorDustPrefab;   // priraď PS_DoorDust prefab
    public Transform dustSpawnPoint;        // priraď DustPoint (alebo nechaj null a použijeme transform.position)
    public float dustScale = 1f;

    [Tooltip("Na odomknutie je potrebný kľúč?")]
    public bool requiresKey = true;

    [Tooltip("Spotrebovať kľúč pri odomknutí?")]
    public bool consumeKey = true;

    [Tooltip("Dvere začínajú už odomknuté (test)")]
    public bool startUnlocked = false;

    [Header("Animator")]
    public string animatorBoolName = "isOpen";

    [Header("UI Tooltip")]
    public GameObject tooltipUI;     // parent GO s textom/ikonou
    public TMP_Text tooltipText;     // voliteľné

    [Header("Interakcia bez triggeru")]
    public float interactRange = 1.6f;     // dosah pre tooltip a F
    public Transform playerOverride;       // voliteľné – ak necháš prázdne, nájde podľa tagu "Player"

    [Header("Persistence (voliteľné)")]
    public bool persistAcrossSessions = false;
    public string doorId;

    [Header("Delayed Collider Open")]
    [Tooltip("Po koľkých sekundách od odomknutia sa spriechodní kolízia.")]
    public float colliderOpenDelay = 3f;

    [Header("Camera Shake")]
    public bool cameraShake = true;
    [Tooltip("Ako dlho bude trvať mierny shake po odomknutí.")]
    public float shakeDuration = 3.25f;
    [Tooltip("Sila shake-u (0.05 – 0.2 sú jemné hodnoty).")]
    public float shakeMagnitude = 0.1f;

    private Animator anim;
    private Collider2D solidCollider; // ak je prítomný, prepíname isTrigger po oneskorení
    private bool isUnlocked;
    private int isOpenHash;
    private Transform player;

    // lokálny coroutine handler pre oneskorené spriechodnenie
    private Coroutine colliderDelayCo;

    // jednorazový „global“ lock na shake, aby sa nestackoval z viacerých dverí naraz
    private static bool s_ShakeRunning = false;

    // --- Dust auto-stop po 3.25s ---
    private ParticleSystem activeDust;
    private Coroutine dustStopCo;

    void Awake()
    {
        anim = GetComponent<Animator>();
        isOpenHash = Animator.StringToHash(animatorBoolName);

        solidCollider = GetComponent<Collider2D>();
        if (solidCollider != null && solidCollider.isTrigger)
            Debug.LogWarning("[DoorHandle] Na parente máš trigger collider. Nevadí, ale blokovanie rieš dieťaťom (Square).");

        if (string.IsNullOrEmpty(doorId))
        {
            var p = transform.position;
            doorId = $"{gameObject.scene.name}_{name}_{Mathf.RoundToInt(p.x)}_{Mathf.RoundToInt(p.y)}";
        }

        if (tooltipUI) tooltipUI.SetActive(false);

        // sanity check animator bool
        bool found = false;
        foreach (var p2 in anim.parameters)
            if (p2.type == AnimatorControllerParameterType.Bool && p2.name == animatorBoolName) { found = true; break; }
        if (!found) Debug.LogWarning($"[DoorHandle] Animator nemá bool '{animatorBoolName}'.");
    }

    void Start()
    {
        isUnlocked = startUnlocked;
        if (persistAcrossSessions)
        {
            int pref = PlayerPrefs.GetInt(GetPrefKey(), isUnlocked ? 1 : 0);
            isUnlocked = (pref == 1);
        }
        ApplyState(initial: true);

        // nájdi hráča
        player = playerOverride ? playerOverride : FindPlayerTransform();
        if (!player) Debug.LogWarning("[DoorHandle] Nenašiel som hráča (tag 'Player'). Tooltip pôjde OFF.");
    }

    void Update()
    {
        UpdateTooltipAndInteraction();
    }

    void UpdateTooltipAndInteraction()
    {
        if (!player) { if (tooltipUI) tooltipUI.SetActive(false); return; }

        float dist = Vector2.Distance(player.position, transform.position);
        bool inRange = dist <= interactRange;

        // tooltip ukazujeme iba ak sú dvere ZAMKNUTÉ a hráč je v dosahu
        bool show = inRange && !isUnlocked;
        if (tooltipUI) tooltipUI.SetActive(show);

        if (show && tooltipText)
        {
            if (!requiresKey)
            {
                tooltipText.text = $"{interactKey} — Open";
            }
            else
            {
                bool hasKey = (inventoryData != null && inventoryData.keys > 0);
                tooltipText.text = hasKey ? $"{interactKey} — Unlock" : $"Need  a  key";
            }
        }

        if (show && Input.GetKeyDown(interactKey))
            TryUnlock();
    }

    void TryUnlock()
    {
        if (isUnlocked) return;

        if (requiresKey)
        {
            if (inventoryData != null && inventoryData.keys > 0)
            {
                if (consumeKey) inventoryData.UseKey();
                SetUnlocked(true);
                PlaySfx("doorUnlock");
            }
            else
            {
                PlaySfx("doorLocked");
                if (tooltipText) tooltipText.text = $"{interactKey} — Need a key";
            }
        }
        else
        {
            SetUnlocked(true);
            PlaySfx("doorUnlock");
        }
    }

    public void SetUnlocked(bool unlocked)
    {
        if (isUnlocked == unlocked) return;
        isUnlocked = unlocked;

        if (persistAcrossSessions)
        {
            PlayerPrefs.SetInt(GetPrefKey(), isUnlocked ? 1 : 0);
            PlayerPrefs.Save();
        }

        ApplyState(initial: false);
        if (unlocked) SpawnDoorDust();

        if (tooltipUI) tooltipUI.SetActive(false);

        // spusti mierny camera shake (vizuálne potvrdenie otvárania)
        if (cameraShake) TryStartCameraShake();
    }

    void ApplyState(bool initial)
    {
        // animácia otvorenia/zatvorenia ide hneď
        anim.SetBool(isOpenHash, isUnlocked);

        if (!solidCollider) return;

        // pri zamknutí okamžite sprísni kolíziu
        if (!isUnlocked)
        {
            if (colliderDelayCo != null) { StopCoroutine(colliderDelayCo); colliderDelayCo = null; }
            solidCollider.isTrigger = false;
            return;
        }

        // pri odomknutí:
        // - Ak je to úvodný stav (zo save/startUnlocked), spriechodni hneď.
        // - Inak počkaj colliderOpenDelay sekúnd a až potom spriechodni.
        if (initial)
        {
            solidCollider.isTrigger = true;
        }
        else
        {
            if (colliderDelayCo != null) StopCoroutine(colliderDelayCo);
            colliderDelayCo = StartCoroutine(DelayedColliderOpen(colliderOpenDelay));
        }
    }

    System.Collections.IEnumerator DelayedColliderOpen(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (solidCollider) solidCollider.isTrigger = true;
        colliderDelayCo = null;
    }

    string GetPrefKey() => $"door_unlocked_{doorId}";

    void PlaySfx(string key)
    {
        if (!string.IsNullOrEmpty(key) && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(key);
    }

    Transform FindPlayerTransform()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        return go ? go.transform : null;
    }

    // --- Camera shake priamo tu, jemný a krátky ---
    void TryStartCameraShake()
    {
        if (s_ShakeRunning) return; // nespúšťaj ďalší kým beží aktuálny
        var cam = Camera.main;
        if (!cam) return;
        StartCoroutine(CameraShakeRoutine(cam.transform, shakeDuration, shakeMagnitude));
    }

    System.Collections.IEnumerator CameraShakeRoutine(Transform camT, float duration, float magnitude)
    {
        s_ShakeRunning = true;
        Vector3 startLocalPos = camT.localPosition;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = 1f - (elapsed / duration);
            Vector2 off2 = Random.insideUnitCircle * magnitude * t;
            camT.localPosition = startLocalPos + new Vector3(off2.x, off2.y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        camT.localPosition = startLocalPos;
        s_ShakeRunning = false;
    }

    void SpawnDoorDust()
    {
        if (!doorDustPrefab) return;

        // zruš predchádzajúci efekt (ak by bol)
        if (activeDust)
        {
#if UNITY_EDITOR
            DestroyImmediate(activeDust.gameObject);
#else
            Destroy(activeDust.gameObject);
#endif
            activeDust = null;
        }

        Vector3 pos = dustSpawnPoint ? dustSpawnPoint.position
                                     : (transform.position + Vector3.down * 0.05f);

        activeDust = Instantiate(doorDustPrefab, pos, Quaternion.identity);
        if (Mathf.Abs(dustScale - 1f) > 0.001f)
            activeDust.transform.localScale = Vector3.one * dustScale;

        var main = activeDust.main;
        main.stopAction = ParticleSystemStopAction.Destroy;

        activeDust.Play(true);

        // AUTO-STOP prachu po 3.25s
        if (dustStopCo != null) StopCoroutine(dustStopCo);
        dustStopCo = StartCoroutine(StopDustAfterSeconds(3.25f));
    }

    System.Collections.IEnumerator StopDustAfterSeconds(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (activeDust == null) yield break;

            // ak sa dvere hýbu a máš spawn point, drž efekt pod dverami
            if (dustSpawnPoint && activeDust)
                activeDust.transform.position = dustSpawnPoint.position;

            t += Time.deltaTime;
            yield return null;
        }

        if (activeDust)
        {
            activeDust.Stop(withChildren: true, stopBehavior: ParticleSystemStopBehavior.StopEmittingAndClear);
            activeDust = null;
        }
        dustStopCo = null;
    }

    void OnDisable()
    {
        if (dustStopCo != null) StopCoroutine(dustStopCo);
        if (activeDust)
        {
            activeDust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            activeDust = null;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}
