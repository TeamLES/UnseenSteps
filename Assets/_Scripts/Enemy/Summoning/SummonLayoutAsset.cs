using System.Collections.Generic;
using UnityEngine;

public enum SummonAttackType
{
    Generic,
    RockBarrage,
    ProjectileFan,
    Custom
}

[CreateAssetMenu(fileName = "SummonLayout", menuName = "Summoning/Summon Layout", order = 0)]
public class SummonLayoutAsset : ScriptableObject
{
    public SummonAttackType attackType = SummonAttackType.Generic;
    [Min(0f)] public float startDelay = 0f;
    [Min(0f)] public float spacingDelay = 0.1f;
    [Min(0f)] public float waveDelay = 0f;
    [Min(1)] public int waveCount = 1;
    public List<SpawnEntry> entries = new();
    [Header("Grid Authoring")]
    [Min(1)] public int gridColumns = 5;
    [Min(1)] public int gridRows = 3;
    public bool gridUseArenaBounds = true;
    [Min(0.1f)] public float gridCellSize = 1f;
    public Vector2 gridOrigin;
    public float gridDefaultDelay = 0f;
    public List<GridCellConfig> gridCells = new();

    public IReadOnlyList<SpawnEntry> Points => entries;
}

[System.Serializable]
public struct SpawnEntry
{
    public Vector2 localPosition;
    [Min(0f)] public float delay;
    public GameObject prefabOverride;
}

[System.Serializable]
public struct GridCellConfig
{
    public int column;
    public int row;
    public GameObject prefab;
    [Min(0f)] public float delay;
}
