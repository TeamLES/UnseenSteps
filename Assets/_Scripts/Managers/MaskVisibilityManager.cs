using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class MaskVisibilityManager : MonoBehaviour
{
    [Header("Masked world (prep�na sa pod�a Show)")]
    [Tooltip("Ak je zapnut�, objekty bud� vidite�n� vo vn�tri masky, inak bud� ignorova� masku.")]
    public bool Show = true;

    [Tooltip("Rodi�ovsk� objekty (alebo samostatn�) ktor�ch Sprite/Tilemap renderery sa maj� prepn��.")]
    public List<GameObject> targetObjects = new List<GameObject>();

    [Header("Always visible (tooltipy, r�m�eky, texty...)")]
    [Tooltip("Tieto objekty bud� V�DY vidite�n� (ignoruj� masku) a dostan� vysok� sorting.")]
    public List<GameObject> alwaysVisibleObjects = new List<GameObject>();

    [Tooltip("Sorting layer pre always-visible veci (typicky Reveal_Cursor).")]
    public string alwaysVisibleSortingLayer = "Reveal_Cursor";

    [Tooltip("Order pre always-visible Sprite/Mesh renderery (nad void 1000).")]
    public int alwaysVisibleOrder = 1700;

    [Tooltip("Order pre always-visible LineRenderery (trochu vy��ie ne� text).")]
    public int alwaysVisibleLineOrder = 1750;

    [Header("Auto-fix sorting layer pre world hidden")]
    public bool forceDefaultSortingLayer = true;
    public string defaultHiddenSortingLayer = "Reveal_Cursor";

    private bool _lastShow;

    void OnEnable()
    {
        _lastShow = Show;
        UpdateVisibility(true);
    }

    void OnValidate()
    {
        UpdateVisibility(true);
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (_lastShow != Show)
            {
                _lastShow = Show;
                UpdateVisibility(true);
            }
            return;
        }
#endif
        UpdateVisibility();
    }

    public void UpdateVisibility(bool forceEditorRefresh = false)
    {
        // 1) WORLD (maskovan�)
        var mode = Show ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None;

        foreach (var root in targetObjects)
        {
            if (!root) continue;

            // SpriteRenderery
            var spriteRenderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                var r = spriteRenderers[i];
                if (!r) continue;

                r.maskInteraction = mode;

                if (Show && forceDefaultSortingLayer && r.sortingLayerName == "Default")
                    r.sortingLayerName = defaultHiddenSortingLayer;

#if UNITY_EDITOR
                if (forceEditorRefresh) EditorUtility.SetDirty(r);
#endif
            }

            // TilemapRenderery
            var tilemapRenderers = root.GetComponentsInChildren<TilemapRenderer>(true);
            for (int i = 0; i < tilemapRenderers.Length; i++)
            {
                var r = tilemapRenderers[i];
                if (!r) continue;

                r.maskInteraction = mode;

                if (Show && forceDefaultSortingLayer && r.sortingLayerName == "Default")
                    r.sortingLayerName = defaultHiddenSortingLayer;

#if UNITY_EDITOR
                if (forceEditorRefresh) EditorUtility.SetDirty(r);
#endif
            }
        }

        // 2) ALWAYS VISIBLE (tooltipy: TMP + r�m�eky + pozadia)
        foreach (var root in alwaysVisibleObjects)
        {
            if (!root) continue;

            // SpriteRenderery (pozadia tooltipov, ikonky, at�.)
            var spriteRenderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                var r = spriteRenderers[i];
                if (!r) continue;

                r.maskInteraction = SpriteMaskInteraction.None;
                r.sortingLayerName = alwaysVisibleSortingLayer;
                r.sortingOrder = alwaysVisibleOrder;

#if UNITY_EDITOR
                if (forceEditorRefresh) EditorUtility.SetDirty(r);
#endif
            }

            // LineRenderery (�lt� r�m�eky/kr��ky)
            var lineRenderers = root.GetComponentsInChildren<LineRenderer>(true);
            for (int i = 0; i < lineRenderers.Length; i++)
            {
                var lr = lineRenderers[i];
                if (!lr) continue;

                lr.maskInteraction = SpriteMaskInteraction.None;
                lr.sortingLayerName = alwaysVisibleSortingLayer;
                lr.sortingOrder = alwaysVisibleLineOrder;

#if UNITY_EDITOR
                if (forceEditorRefresh) EditorUtility.SetDirty(lr);
#endif
            }

            // TextMeshPro world text (TMP_Text m� Renderer)
            var tmps = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < tmps.Length; i++)
            {
                var t = tmps[i];
                if (!t) continue;

                var rend = t.GetComponent<Renderer>(); // MeshRenderer (world TMP) alebo CanvasRenderer (UGUI) -> renderer m��e by� null pri niektor�ch UGUI
                if (rend != null)
                {
                    rend.sortingLayerName = alwaysVisibleSortingLayer;
                    rend.sortingOrder = alwaysVisibleOrder + 1; // nech je nad pozad�m
#if UNITY_EDITOR
                    if (forceEditorRefresh) EditorUtility.SetDirty(rend);
#endif
                }

#if UNITY_EDITOR
                if (forceEditorRefresh) EditorUtility.SetDirty(t);
#endif
            }

            // Canvas (ak by si mal world-space UI tooltips)
            var canvases = root.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                if (!c) continue;

                c.overrideSorting = true;
                c.sortingLayerName = alwaysVisibleSortingLayer;
                c.sortingOrder = alwaysVisibleOrder;

#if UNITY_EDITOR
                if (forceEditorRefresh) EditorUtility.SetDirty(c);
#endif
            }
        }

#if UNITY_EDITOR
        if (forceEditorRefresh)
            SceneView.RepaintAll();
#endif
    }

    [ContextMenu("Refresh Visibility Now")]
    public void RefreshNow()
    {
        _lastShow = Show;
        UpdateVisibility(true);
    }
}
