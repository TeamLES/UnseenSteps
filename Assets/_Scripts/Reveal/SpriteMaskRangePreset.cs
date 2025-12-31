using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteMask))]
public class SpriteMaskRangePreset : MonoBehaviour
{
    public enum Preset
    {
        CursorOnly,       // Reveal_Cursor -> Reveal_Cursor
        LampOnlyOnly,     // Reveal_LampOnly -> Reveal_LampOnly   <-- PRIDA
        LampOnlyToCursor  // Reveal_LampOnly -> Reveal_Cursor
    }

    public Preset preset = Preset.CursorOnly;

    public int backOrder = -1000;
    public int frontOrder = 2000;

    void OnEnable() => Apply();
    void OnValidate() => Apply();

    void Apply()
    {
        var m = GetComponent<SpriteMask>();
        m.isCustomRangeActive = true;

        if (preset == Preset.CursorOnly)
        {
            int id = SortingLayer.NameToID("Reveal_Cursor");
            m.backSortingLayerID = id;
            m.frontSortingLayerID = id;
        }
        else if (preset == Preset.LampOnlyOnly)
        {
            int id = SortingLayer.NameToID("Reveal_LampOnly");
            m.backSortingLayerID = id;
            m.frontSortingLayerID = id;
        }
        else
        {
            m.backSortingLayerID = SortingLayer.NameToID("Reveal_LampOnly");
            m.frontSortingLayerID = SortingLayer.NameToID("Reveal_Cursor");
        }

        m.backSortingOrder = backOrder;
        m.frontSortingOrder = frontOrder;
    }
}
