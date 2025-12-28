using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SummonLayoutSpawner : MonoBehaviour
{
    public SummonLayoutAsset layout;
    public GameObject defaultPrefab;
    public Transform spawnRoot;
    public bool useRootRotation;
    public GameObject arenaReference;
    public bool useGridAuthoring;

    [Header("Debug Preview")]
    public bool drawGizmos = true;
    public Color gizmoColor = new Color(0.1f, 0.8f, 1f, 0.4f);

    public bool enableSummon = true;
    public int attacksBeforeSummon = 2;
    public float summonPauseDuration = 2f;
    public string summonTrigger = "";
    public Animator summonAnimator;

    bool isSummoning;
    int attacksSinceSummon;

    public bool IsSummoning => isSummoning;

    void Awake()
    {
        if (summonAnimator == null)
        {
            summonAnimator = GetComponent<Animator>();
            if (summonAnimator == null) summonAnimator = GetComponentInParent<Animator>();
        }
    }

    public IEnumerator OnAttackResolved()
    {
        if (!enableSummon) yield break;
        if (isSummoning) yield break;
        attacksSinceSummon++;
        if (attacksSinceSummon < attacksBeforeSummon) yield break;

        isSummoning = true;
        attacksSinceSummon = 0;
        if (!string.IsNullOrEmpty(summonTrigger) && summonAnimator != null)
        {
            summonAnimator.SetTrigger(summonTrigger);
        }
        if (summonPauseDuration > 0f)
        {
            yield return new WaitForSeconds(summonPauseDuration);
        }
        isSummoning = false;
        SpawnLayout();
    }

    public void SpawnLayout() => StartCoroutine(SpawnRoutine());

    IEnumerator SpawnRoutine()
    {
        if (layout == null) yield break;
        if (useGridAuthoring)
        {
            layout.entries = BuildEntriesFromGrid();
        }
        if (layout.startDelay > 0f) yield return new WaitForSeconds(layout.startDelay);

        int waves = Mathf.Max(1, layout.waveCount);
        for (int w = 0; w < waves; w++)
        {
            foreach (var entry in layout.Points)
            {
                if (entry.delay > 0f) yield return new WaitForSeconds(entry.delay);
                SpawnOne(entry);
                if (layout.spacingDelay > 0f) yield return new WaitForSeconds(layout.spacingDelay);
            }
            if (w < waves - 1 && layout.waveDelay > 0f)
            {
                yield return new WaitForSeconds(layout.waveDelay);
            }
        }
    }

    List<SpawnEntry> BuildEntriesFromGrid()
    {
        var list = new List<SpawnEntry>();
        Bounds arena = GetArenaBounds();
        Vector2 cellSize = new Vector2(arena.size.x / layout.gridColumns, arena.size.y / layout.gridRows);
        Vector3 arenaMin = arena.min;
        // gridOrigin acts as offset from arena min. Cells map to 0–100% across arena extents.
        for (int i = 0; i < layout.gridCells.Count; i++)
        {
            var cell = layout.gridCells[i];
            Vector2 basePos = new Vector2(arenaMin.x, arenaMin.y) + layout.gridOrigin;
            Vector2 pos = basePos + new Vector2((cell.column + 0.5f) * cellSize.x, (cell.row + 0.5f) * cellSize.y);
            list.Add(new SpawnEntry
            {
                localPosition = InverseTransformPoint(pos),
                delay = cell.delay > 0f ? cell.delay : layout.gridDefaultDelay,
                prefabOverride = cell.prefab
            });
        }
        return list;
    }

    Vector2 InverseTransformPoint(Vector3 world)
    {
        Transform root = spawnRoot != null ? spawnRoot : transform;
        return root.InverseTransformPoint(world);
    }

    void SpawnOne(SpawnEntry entry)
    {
        GameObject prefab = entry.prefabOverride != null ? entry.prefabOverride : defaultPrefab;
        if (prefab == null) return;

        Transform root = spawnRoot != null ? spawnRoot : transform;
        Vector3 worldPos = root.TransformPoint(entry.localPosition);
        Bounds arena = GetArenaBounds();
        worldPos = ClampWorldToArena(worldPos, arena);
        Quaternion rot = useRootRotation ? root.rotation : Quaternion.identity;
        Instantiate(prefab, worldPos, rot);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || layout == null) return;
        Transform root = spawnRoot != null ? spawnRoot : transform;
        Gizmos.color = gizmoColor;
        Bounds arena = GetArenaBounds();
        Gizmos.DrawWireCube(arena.center, arena.size);

        var entries = useGridAuthoring ? BuildEntriesFromGrid() : new List<SpawnEntry>(layout.Points);
        foreach (var entry in entries)
        {
            Vector3 p = root.TransformPoint(entry.localPosition);
            p = ClampWorldToArena(p, arena);
            Gizmos.DrawSphere(p, 0.1f);
        }
    }

    public Bounds GetArenaBounds()
    {
        if (arenaReference != null)
        {
            var renderer = arenaReference.GetComponent<Renderer>();
            if (renderer != null) return renderer.bounds;
            var col2d = arenaReference.GetComponent<Collider2D>();
            if (col2d != null) return col2d.bounds;
        }
        Transform root = spawnRoot != null ? spawnRoot : transform;
        return new Bounds(root.position, Vector3.one);
    }

    Vector3 ClampWorldToArena(Vector3 worldPos, Bounds arena)
    {
        worldPos.x = Mathf.Clamp(worldPos.x, arena.min.x, arena.max.x);
        worldPos.y = Mathf.Clamp(worldPos.y, arena.min.y, arena.max.y);
        worldPos.z = Mathf.Clamp(worldPos.z, arena.min.z, arena.max.z);
        return worldPos;
    }
}
