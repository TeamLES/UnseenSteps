using UnityEngine;

public class BlurCapture : MonoBehaviour
{
    [Header("Camera that renders the world to RT")]
    public Camera blurCamera;               // druhá kamera
    [Range(1, 4)] public int downsample = 2; // 2 = polovica rozlíšenia
    [Range(1, 6)] public int iterations = 2;
    [Range(0.8f, 3f)] public float offsetBase = 1.2f;

    [Header("Material with 'Hidden/Unseen/KawaseBlur'")]
    public Material kawaseMat;

    [Header("Freeze (optional)")]
    public bool freezeAfterFirstFrame = false;

    RenderTexture sourceRT, ping, pong, finalRT;
    int lastW, lastH;

    void Start() { SetupRTs(); }
    void OnDisable() { ReleaseRTs(); Shader.SetGlobalTexture("_Unseen_BlurTex", Texture2D.blackTexture); }

    void SetupRTs()
    {
        ReleaseRTs();

        int w = Mathf.Max(1, Screen.width / downsample);
        int h = Mathf.Max(1, Screen.height / downsample);

        // COLOR + DEPTH pre kameru (Render Graph to vyžaduje)
        // 24-bit depth (D24S8) staèí, MSAA vypnuté.
        sourceRT = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
        {
            useMipMap = false,
            autoGenerateMips = false,
            antiAliasing = 1
        };

        // Ping/pong/finál nepotrebujú depth (len blit)
        ping = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        pong = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        finalRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);

        if (blurCamera) blurCamera.targetTexture = sourceRT;

        lastW = w; lastH = h;
    }

    void ReleaseRTs()
    {
        void R(ref RenderTexture rt) { if (rt) { rt.Release(); Destroy(rt); rt = null; } }
        R(ref sourceRT); R(ref ping); R(ref pong); R(ref finalRT);
    }

    void LateUpdate()
    {
        if (!blurCamera || !kawaseMat) return;

        if (Screen.width / downsample != lastW || Screen.height / downsample != lastH)
            SetupRTs();

        Graphics.Blit(sourceRT, ping);

        for (int i = 0; i < iterations; i++)
        {
            kawaseMat.SetFloat("_Offset", offsetBase + i * 0.75f);
            Graphics.Blit(ping, pong, kawaseMat, 0);
            var t = ping; ping = pong; pong = t;
        }

        Graphics.Blit(ping, finalRT);
        Shader.SetGlobalTexture("_Unseen_BlurTex", finalRT);

        if (freezeAfterFirstFrame) enabled = false;
    }
}
