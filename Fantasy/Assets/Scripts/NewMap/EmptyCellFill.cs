using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "EmptyCellFill", menuName = "Map/Empty Cell Fill")]
public class EmptyCellFill : ScriptableObject
{
    public enum Layer { Floor, Wall, Ceiling }

    [Serializable]
    public class Option
    {
        public TileBase tile;
        [Min(0f)] public float weight = 1f;
    }

    [Tooltip("Layer")]
    public Layer layer = Layer.Floor;

    [Tooltip("chance of creating the tile")]
    [Range(0f, 1f)] public float fillChance = 1f;

    [Tooltip("cells besides the map")]
    [Min(0)] public int padding = 0;

    [Header("randomness")]
    [Tooltip("if marked use a random seed")]
    public bool useRandomSeed = true;
    public int seed = 12345;

    [Header("sorting tiles")]
    public List<Option> options = new List<Option>();

    public bool HasTiles
    {
        get
        {
            foreach (var o in options)
                if (o != null && o.tile != null && o.weight > 0f) return true;
            return false;
        }
    }

    public TileBase Pick(System.Random rng)
    {
        float total = 0f;
        foreach (var o in options)
            if (o != null && o.tile != null && o.weight > 0f) total += o.weight;

        if (total <= 0f) return null;

        double roll = rng.NextDouble() * total;
        foreach (var o in options)
        {
            if (o == null || o.tile == null || o.weight <= 0f) continue;
            roll -= o.weight;
            if (roll <= 0.0) return o.tile;
        }

        return null;
    }
}
