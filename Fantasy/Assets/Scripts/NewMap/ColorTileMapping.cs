using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "ColorTileMapping", menuName = "Map/Color Tile Mapping")]
public class ColorTileMapping : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string label;
        [Tooltip("map color")]
        public Color32 color = new Color32(255, 255, 255, 255);

        [Header("layers")]
        public TileBase floor;
        public TileBase wall;
        public TileBase ceiling;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();
    [Range(0, 64)]
    [SerializeField] private int tolerance = 0;

    private Dictionary<int, Entry> lookup;

    private static int Key(Color32 c) => (c.r << 16) | (c.g << 8) | c.b;

    private void OnEnable() => BuildLookup();
    private void OnValidate() => BuildLookup();

    private void BuildLookup()
    {
        lookup = new Dictionary<int, Entry>(entries.Count);
        foreach (var e in entries)
        {
            if (e == null) continue;
            lookup[Key(e.color)] = e;
        }
    }

    public bool TryGetEntry(Color32 pixel, out Entry entry)
    {
        if (lookup == null) BuildLookup();

        if (lookup.TryGetValue(Key(pixel), out entry))
            return true;

        if (tolerance > 0)
        {
            foreach (var e in entries)
            {
                if (e == null) continue;
                if (Mathf.Abs(e.color.r - pixel.r) <= tolerance &&
                    Mathf.Abs(e.color.g - pixel.g) <= tolerance &&
                    Mathf.Abs(e.color.b - pixel.b) <= tolerance)
                {
                    entry = e;
                    return true;
                }
            }
        }
        entry = null;
        return false;
    }
}