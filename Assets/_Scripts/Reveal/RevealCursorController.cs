using System.Collections.Generic;
using UnityEngine;

public class RevealCursorController : MonoBehaviour
{
    public static RevealCursorController Instance { get; private set; }

    [Header("Refs (auto-find if empty)")]
    [SerializeField] private CursorRevealCircle cursorCircle;
    [SerializeField] private SpriteMask revealMask;
    [SerializeField] private LineRenderer circleLine;

    [Header("Behaviour")]
    [Tooltip("When inside NoRevealZone, also disable CursorRevealCircle script (stops updates).")]
    [SerializeField] private bool disableCursorCircleScriptInZone = true;

    private readonly HashSet<object> blockers = new HashSet<object>();

    public bool IsCursorRevealEnabled => blockers.Count == 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!cursorCircle) cursorCircle = GetComponent<CursorRevealCircle>();
        if (!revealMask) revealMask = GetComponent<SpriteMask>();
        if (!circleLine) circleLine = GetComponent<LineRenderer>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddBlock(object key)
    {
        if (key == null) return;
        blockers.Add(key);
        Apply();
    }

    public void RemoveBlock(object key)
    {
        if (key == null) return;
        blockers.Remove(key);
        Apply();
    }

    private void Apply()
    {
        bool allowReveal = blockers.Count == 0;

        // schová ring
        if (circleLine) circleLine.enabled = allowReveal;

        // vypne masku (defog)
        if (revealMask) revealMask.enabled = allowReveal;

        // volite¾ne vypne celý update-follow/draw (perf + žiadny “nevidite¾ný” kruh)
        if (cursorCircle && disableCursorCircleScriptInZone)
            cursorCircle.enabled = allowReveal;
    }
}
