using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(SummonLayoutAsset))]
public class SummonLayoutAssetEditor : Editor
{
    ReorderableList list;
    float paintDelay;
    bool showAdvanced;

    void OnEnable()
    {
        list = new ReorderableList(serializedObject, serializedObject.FindProperty("entries"), true, true, true, true);
        list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Spawn Entries");
        list.drawElementCallback = DrawElement;
        list.onChangedCallback = l => serializedObject.ApplyModifiedProperties();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("Arena bounds sa berú z objektu nastaveného na SummonLayoutSpawner.arenaReference (renderer alebo collider).", MessageType.Info);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("attackType"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startDelay"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("spacingDelay"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("waveDelay"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("waveCount"));
        EditorGUILayout.Space();
        DrawGridSection();
        EditorGUILayout.Space();
        list.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }

    void DrawGridSection()
    {
        var cols = serializedObject.FindProperty("gridColumns");
        var rows = serializedObject.FindProperty("gridRows");
        var useBounds = serializedObject.FindProperty("gridUseArenaBounds");
        var cellSize = serializedObject.FindProperty("gridCellSize");
        var origin = serializedObject.FindProperty("gridOrigin");
        var defaultDelay = serializedObject.FindProperty("gridDefaultDelay");
        var cells = serializedObject.FindProperty("gridCells");

        EditorGUILayout.LabelField("Grid Layout", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(cols);
        EditorGUILayout.PropertyField(rows);
        EditorGUILayout.PropertyField(useBounds, new GUIContent("Use Arena Bounds For Cell Size"));
        if (!useBounds.boolValue)
        {
            EditorGUILayout.PropertyField(cellSize);
        }

        showAdvanced = EditorGUILayout.BeginFoldoutHeaderGroup(showAdvanced, "Advanced Settings");
        if (showAdvanced)
        {
            if (useBounds.boolValue)
            {
                EditorGUILayout.PropertyField(origin, new GUIContent("Grid Origin (offset from arena min)"));
            }
            else
            {
                EditorGUILayout.PropertyField(origin, new GUIContent("Grid Origin"));
            }
            EditorGUILayout.PropertyField(defaultDelay);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        paintDelay = EditorGUILayout.FloatField("Paint Delay", paintDelay);

        if (GUILayout.Button("Clear Grid"))
        {
            cells.ClearArray();
        }

        DrawGridPainter(cols.intValue, rows.intValue, cells);

        if (GUILayout.Button("Update Entries From Grid"))
        {
            ApplyGridToEntries();
        }
    }

    void DrawGridPainter(int cols, int rows, SerializedProperty cells)
    {
        float cell = 24f;
        var style = new GUIStyle(GUI.skin.button);
        style.fixedWidth = cell;
        style.fixedHeight = cell;
        style.margin = new RectOffset(1, 1, 1, 1);

        for (int r = rows - 1; r >= 0; r--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < cols; c++)
            {
                bool active = HasCell(cells, c, r, out int index);
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = active ? new Color(0.2f, 0.8f, 0.3f, 1f) : Color.gray;
                if (GUILayout.Button(active ? "X" : "", style))
                {
                    if (active)
                    {
                        cells.DeleteArrayElementAtIndex(index);
                    }
                    else
                    {
                        int newIndex = cells.arraySize;
                        cells.InsertArrayElementAtIndex(newIndex);
                        var el = cells.GetArrayElementAtIndex(newIndex);
                        el.FindPropertyRelative("column").intValue = c;
                        el.FindPropertyRelative("row").intValue = r;
                        el.FindPropertyRelative("prefab").objectReferenceValue = null;
                        el.FindPropertyRelative("delay").floatValue = paintDelay;
                    }
                }
                GUI.backgroundColor = prev;
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    bool HasCell(SerializedProperty cells, int c, int r, out int index)
    {
        for (int i = 0; i < cells.arraySize; i++)
        {
            var el = cells.GetArrayElementAtIndex(i);
            if (el.FindPropertyRelative("column").intValue == c && el.FindPropertyRelative("row").intValue == r)
            {
                index = i;
                return true;
            }
        }
        index = -1;
        return false;
    }

    void ApplyGridToEntries()
    {
        var asset = (SummonLayoutAsset)target;
        asset.entries.Clear();
        foreach (var cell in asset.gridCells)
        {
            Vector2 pos = asset.gridOrigin + new Vector2(cell.column * asset.gridCellSize, cell.row * asset.gridCellSize);
            asset.entries.Add(new SpawnEntry
            {
                localPosition = pos,
                delay = cell.delay > 0f ? cell.delay : asset.gridDefaultDelay,
                prefabOverride = cell.prefab
            });
        }
        EditorUtility.SetDirty(asset);
    }

    void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        var element = list.serializedProperty.GetArrayElementAtIndex(index);
        rect.y += 2;
        float line = EditorGUIUtility.singleLineHeight;
        float third = rect.width / 3f;
        EditorGUI.PropertyField(new Rect(rect.x, rect.y, third, line), element.FindPropertyRelative("localPosition"), GUIContent.none);
        EditorGUI.PropertyField(new Rect(rect.x + third + 4, rect.y, third - 4, line), element.FindPropertyRelative("delay"), GUIContent.none);
        EditorGUI.PropertyField(new Rect(rect.x + 2 * third + 8, rect.y, third - 8, line), element.FindPropertyRelative("prefabOverride"), GUIContent.none);
    }
}
