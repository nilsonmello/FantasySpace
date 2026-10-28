using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapMaskBuilder : MonoBehaviour
{
    [SerializeField] private string targetName = "Ground";

    [SerializeField] private ColorMapGenerator generator;
    [SerializeField] private List<Tilemap> sources = new List<Tilemap>();
    [SerializeField] private Tilemap target;

    [SerializeField] private TileBase whiteTile;
    [SerializeField] private Color maskColor = Color.white;
    [SerializeField] private bool buildOnStart = true;

    private Tile runtimeTile;
    private Sprite runtimeSprite;
    private Texture2D runtimeTexture;
    private ColorMapGenerator subscribedTo;

    private Transform anchor;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Start()
    {
        if (generator == null)
        {
            ResolveReferences();
            Subscribe();
        }

        if (generator == null && buildOnStart) Build();
    }

    private void OnDestroy()
    {
        Unsubscribe();

        if (runtimeTile != null) Destroy(runtimeTile);
        if (runtimeSprite != null) Destroy(runtimeSprite);
        if (runtimeTexture != null) Destroy(runtimeTexture);
    }

    public void ResolveReferences()
    {
        if (generator == null) generator = FindNearestGenerator(out anchor);
        if (anchor == null) anchor = generator != null ? generator.transform : transform.root;

        if (target == null && !string.IsNullOrEmpty(targetName))
        {
            target = FindTilemapByName(anchor);
            if (target == null) target = FindTilemapByName(transform.root);
        }

        if (sources.Count == 0 && generator != null)
        {
            generator.ResolveReferences();
            AddIfValid(generator.FloorTilemap);
            AddIfValid(generator.WallTilemap);
            AddIfValid(generator.CeilingTilemap);
        }
    }

    private ColorMapGenerator FindNearestGenerator(out Transform foundAt)
    {
        for (Transform t = transform; t != null; t = t.parent)
        {
            var found = t.GetComponentInChildren<ColorMapGenerator>(true);
            if (found != null)
            {
                foundAt = t;
                return found;
            }
        }
        foundAt = null;
        return null;
    }

    private Tilemap FindTilemapByName(Transform root)
    {
        if (root == null) return null;

        foreach (Tilemap t in root.GetComponentsInChildren<Tilemap>(true))
        {
            if (t.gameObject.name == targetName) return t;
        }
        return null;
    }

    private void AddIfValid(Tilemap t)
    {
        if (t != null && t != target && !sources.Contains(t)) sources.Add(t);
    }

    private void Subscribe()
    {
        if (generator == null || subscribedTo == generator) return;

        Unsubscribe();
        generator.MapGenerated += Build;
        subscribedTo = generator;
    }

    private void Unsubscribe()
    {
        if (subscribedTo != null) subscribedTo.MapGenerated -= Build;
        subscribedTo = null;
    }

    [ContextMenu("Build")]
    public void Build()
    {
        ResolveReferences();

        if (target == null)
        {
            return;
        }

        if (sources.Count == 0)
        {
            return;
        }

        target.ClearAllTiles();

        var occupied = new HashSet<Vector3Int>();
        foreach (Tilemap source in sources)
        {
            if (source == null || source == target) continue;

            foreach (Vector3Int pos in source.cellBounds.allPositionsWithin)
            {
                if (source.HasTile(pos)) occupied.Add(pos);
            }
        }

        if (occupied.Count == 0) return;

        TileBase tile = GetMaskTile();

        var positions = new Vector3Int[occupied.Count];
        var tiles = new TileBase[occupied.Count];
        int i = 0;
        foreach (Vector3Int pos in occupied)
        {
            positions[i] = pos;
            tiles[i] = tile;
            i++;
        }

        target.color = maskColor;
        target.SetTiles(positions, tiles);
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        ResolveReferences();
        if (target != null) target.ClearAllTiles();
    }

    private TileBase GetMaskTile()
    {
        if (whiteTile != null) return whiteTile;
        if (runtimeTile != null) return runtimeTile;

        runtimeTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };
        runtimeTexture.SetPixel(0, 0, Color.white);
        runtimeTexture.Apply();

        float cellSize = target.layoutGrid != null ? target.layoutGrid.cellSize.x : 1f;
        runtimeSprite = Sprite.Create(runtimeTexture, new Rect(0, 0, 1, 1),
                                      new Vector2(0.5f, 0.5f), 1f / cellSize);

        runtimeTile = ScriptableObject.CreateInstance<Tile>();
        runtimeTile.sprite = runtimeSprite;
        return runtimeTile;
    }
}