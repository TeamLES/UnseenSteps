using UnityEngine;
using System;
using System.Linq;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Channels (auto-create if empty)")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Library")]
    public NamedClip[] musics; // napr. "MainMenu", "Level1Music"
    public NamedClip[] sfx;    // napr. "ButtonClick", "CoinPickup", "Jump", "SwordSlash"

    bool nextPlayerHitHigh;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        var ddol = GameObject.Find("/_Managers");
        if (ddol == null)
        {
            ddol = new GameObject("_Managers");
            DontDestroyOnLoad(ddol);
        }
        else
        {
            DontDestroyOnLoad(ddol);
        }
        transform.SetParent(ddol.transform, worldPositionStays: true);

        if (!musicSource) musicSource = gameObject.AddComponent<AudioSource>();
        if (!sfxSource) sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
    }

    public void PlayMusic(string name)
    {
        var m = musics.FirstOrDefault(x => x.name == name);
        if (m?.clip == null) return;

        musicSource.Stop();
        musicSource.clip = m.clip;
        musicSource.volume = m.volume;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic() => musicSource.Stop();

    public void PlaySFX(string name)
    {
        var s = sfx.FirstOrDefault(x => x.name == name);
        if (s?.clip == null) return;
        var volume = s.volume;
        if (name == "coin") volume *= 0.5f;
        float originalPitch = sfxSource.pitch;
        float originalPan = sfxSource.panStereo;
        if (name == "playerHit")
        {
            bool high = nextPlayerHitHigh;
            nextPlayerHitHigh = !nextPlayerHitHigh;
            sfxSource.pitch = high ? UnityEngine.Random.Range(1.4f, 1.7f) : UnityEngine.Random.Range(0.3f, 0.5f);
            volume *= UnityEngine.Random.Range(0.4f, 1.25f);
            sfxSource.panStereo = UnityEngine.Random.Range(-0.35f, 0.35f);
        }
        sfxSource.PlayOneShot(s.clip, volume);
        sfxSource.pitch = originalPitch;
        sfxSource.panStereo = originalPan;
    }

    // ---- Helper pre ziskanie klipu + hlasitosti (na lokalne prehravanie) ----
    public bool TryGetSFXClip(string name, out AudioClip clip, out float volume)
    {
        var s = sfx.FirstOrDefault(x => x.name == name);
        if (s?.clip == null) { clip = null; volume = 1f; return false; }
        clip = s.clip; volume = s.volume; return true;
    }
}

[Serializable]
public class NamedClip
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
}
