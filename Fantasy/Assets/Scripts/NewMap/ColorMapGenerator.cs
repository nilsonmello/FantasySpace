using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ColorMapGenerator : MonoBehaviour
{
    public enum ColorSource
    {
        SpritePixel,
        TileTint
    }

    [SerializeField] private ColorTileMapping mapping;

    [SerializeField] private string sourceName = "Source";
    [SerializeField] private string floorName = "Floor";
    [SerializeField] private string wallName = "Wall";
    [SerializeField] private string ceilingName = "Ceiling";

    [SerializeField] private Tilemap sourceTilemap;
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap ceilingTilemap;

    [SerializeField] private ColorSource colorSource = ColorSource.SpritePixel;
    [SerializeField] private bool hideSourceOnGenerate = true;
    [SerializeField] private EmptyCellFill emptyFill;
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool logUnmappedColors = true;

    public event System.Action MapGenerated;

    public Tilemap SourceTilemap => sourceTilemap;
    public Tilemap FloorTilemap => floorTilemap;
    public Tilemap WallTilemap => wallTilemap;
    public Tilemap CeilingTilemap => ceilingTilemap;

    private readonly Dictionary<Sprite, Color32> spriteColorCache = new Dictionary<Sprite, Color32>();
    private bool hasGenerated;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        if (generateOnStart && !hasGenerated) Generate();
    }

    public void ResolveReferences()
    {
        Tilemap[] all = GetComponentsInChildren<Tilemap>(true);

        if (sourceTilemap == null) sourceTilemap = FindByName(all, sourceName);
        if (floorTilemap == null) floorTilemap = FindByName(all, floorName);
        if (wallTilemap == null) wallTilemap = FindByName(all, wallName);
        if (ceilingTilemap == null) ceilingTilemap = FindByName(all, ceilingName);
    }

    private static Tilemap FindByName(Tilemap[] tilemaps, string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return null;

        foreach (Tilemap t in tilemaps)
        {
            if (t.gameObject.name == objectName) return t;
        }
        return null;
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        if (!ValidateReferences()) return;

        BoundsInt src = sourceTilemap.cellBounds;
        if (src.size.x <= 0 || src.size.y <= 0)
        {
            return;
        }

        var bounds = new BoundsInt(src.xMin, src.yMin, 0, src.size.x, src.size.y, 1);

        Color32[] grid = ReadSourceColors(bounds);
        if (grid == null) return;

        Apply(grid, bounds);

        if (hideSourceOnGenerate)
        {
            var sourceRenderer = sourceTilemap.GetComponent<TilemapRenderer>();
            if (sourceRenderer != null) sourceRenderer.enabled = false;
        }
    }

    public void Apply(Color32[] pixels, BoundsInt bounds)
    {
        ResolveReferences();

        if (mapping == null || floorTilemap == null || wallTilemap == null || ceilingTilemap == null)
        {
            return;
        }

        ClearDestinations();

        int count = pixels.Length;
        int width = bounds.size.x;
        var floorTiles = new TileBase[count];
        var wallTiles = new TileBase[count];
        var ceilingTiles = new TileBase[count];
        var unmapped = new HashSet<int>();

        for (int i = 0; i < count; i++)
        {
            Color32 px = pixels[i];
            if (px.a == 0) continue;

            if (mapping.TryGetEntry(px, out var entry))
            {
                floorTiles[i] = entry.floor;
                wallTiles[i] = entry.wall;
                ceilingTiles[i] = entry.ceiling;
            }
        }

        floorTilemap.SetTilesBlock(bounds, floorTiles);
        wallTilemap.SetTilesBlock(bounds, wallTiles);
        ceilingTilemap.SetTilesBlock(bounds, ceilingTiles);

        FillEmptyCells(pixels, bounds);

        hasGenerated = true;
        MapGenerated?.Invoke();
    }

    private void FillEmptyCells(Color32[] pixels, BoundsInt bounds)
    {
        if (emptyFill == null) return;

        if (!emptyFill.HasTiles)
        {
            return;
        }

        Tilemap target = GetLayer(emptyFill.layer);
        if (target == null) return;

        int pad = emptyFill.padding;
        int width = bounds.size.x;
        var rng = emptyFill.useRandomSeed ? new System.Random() : new System.Random(emptyFill.seed);

        var positions = new List<Vector3Int>();
        var tiles = new List<TileBase>();

        for (int y = bounds.yMin - pad; y < bounds.yMax + pad; y++)
        {
            for (int x = bounds.xMin - pad; x < bounds.xMax + pad; x++)
            {
                bool insideSource = x >= bounds.xMin && x < bounds.xMax &&
                                    y >= bounds.yMin && y < bounds.yMax;

                if (insideSource && pixels[(x - bounds.xMin) + (y - bounds.yMin) * width].a != 0)
                    continue;

                if (rng.NextDouble() > emptyFill.fillChance) continue;

                TileBase tile = emptyFill.Pick(rng);
                if (tile == null) continue;

                positions.Add(new Vector3Int(x, y, 0));
                tiles.Add(tile);
            }
        }

        target.SetTiles(positions.ToArray(), tiles.ToArray());
    }

    private Tilemap GetLayer(EmptyCellFill.Layer layer)
    {
        switch (layer)
        {
            case EmptyCellFill.Layer.Floor: return floorTilemap;
            case EmptyCellFill.Layer.Wall: return wallTilemap;
            default: return ceilingTilemap;
        }
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        ResolveReferences();
        ClearDestinations();
        hasGenerated = false;

        if (sourceTilemap != null)
        {
            var sourceRenderer = sourceTilemap.GetComponent<TilemapRenderer>();
            if (sourceRenderer != null) sourceRenderer.enabled = true;
        }
    }

    private void ClearDestinations()
    {
        if (floorTilemap != null) floorTilemap.ClearAllTiles();
        if (wallTilemap != null) wallTilemap.ClearAllTiles();
        if (ceilingTilemap != null) ceilingTilemap.ClearAllTiles();
    }

    private Color32[] ReadSourceColors(BoundsInt bounds)
    {
        spriteColorCache.Clear();

        sourceTilemap.RefreshAllTiles();

        TileBase[] tiles = sourceTilemap.GetTilesBlock(bounds);
        var grid = new Color32[tiles.Length];
        int width = bounds.size.x;

        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i] == null) continue;

            var cell = new Vector3Int(bounds.xMin + i % width, bounds.yMin + i / width, 0);
            Color32 color;

            if (colorSource == ColorSource.SpritePixel)
            {
                Sprite sprite = sourceTilemap.GetSprite(cell);
                if (sprite == null) continue;
                if (!TryGetSpriteColor(sprite, out color)) return null;
            }
            else
            {
                Color tileColor = tiles[i] is Tile t ? t.color : Color.white;
                color = tileColor * sourceTilemap.GetColor(cell);
            }

            color.a = 255;
            grid[i] = color;
        }

        return grid;
    }

    private bool TryGetSpriteColor(Sprite sprite, out Color32 color)
    {
        if (spriteColorCache.TryGetValue(sprite, out color)) return true;

        Texture2D tex = sprite.texture;
        if (!tex.isReadable)
        {
            return false;
        }

        Rect r = sprite.textureRect;
        int cx = Mathf.FloorToInt(r.x + r.width * 0.5f);
        int cy = Mathf.FloorToInt(r.y + r.height * 0.5f);
        color = tex.GetPixel(cx, cy);

        spriteColorCache[sprite] = color;
        return true;
    }

    private bool ValidateReferences()
    {
        ResolveReferences();

        if (sourceTilemap == null)
        {
            return false;
        }

        if (floorTilemap == null || wallTilemap == null || ceilingTilemap == null)
        {
            return false;
        }

        if (sourceTilemap == floorTilemap || sourceTilemap == wallTilemap || sourceTilemap == ceilingTilemap)
        {
            return false;
        }

        return true;
    }
}