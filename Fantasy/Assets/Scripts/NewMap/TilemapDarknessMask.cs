using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapDarknessMask : MonoBehaviour
{
    public enum MaskSource
    {
        CeilingTilemap,
        EmptyCells
    }

    [Header("Fonte do mapa")]
    [SerializeField] private MaskSource maskSource = MaskSource.EmptyCells;

    [SerializeField] private Tilemap ceilingOverride;

    [Header("Materiais")]
    [SerializeField] private Material blurMaterial;
    [SerializeField] private Material darknessMaterial;

    [Header("Máscara")]
    [SerializeField] private int texelsPerCell = 8;

    [Range(0, 8)]
    [SerializeField] private int blurIterations = 3;

    [SerializeField] private int extraMarginCells = 4;

    [SerializeField] private bool marginIsMask = true;

    private RenderTexture _pingpongA;
    private RenderTexture _pingpongB;
    private InstantiateMap _mapSource;
    private ColorMapGenerator _generatorFallback;
    private bool _subscribed;
    private bool _hasBuilt;

    private void Awake()
    {
        ResolveSource();
    }

    private void OnEnable()
    {
        ResolveSource();
        Subscribe();
    }

    private void Start()
    {
        if (!_hasBuilt)
            CatchUp();
    }

    private void Update()
    {
        if (_subscribed) return;

        ResolveSource();
        if (Subscribe())
            CatchUp();
    }

    private void OnDisable()
    {
        Unsubscribe();
        ReleaseRTs();
    }

    private void ResolveSource()
    {
        if (_mapSource == null)
            _mapSource = FindFirstObjectByType<InstantiateMap>();

        if (_mapSource == null && _generatorFallback == null)
            _generatorFallback = transform.root.GetComponentInChildren<ColorMapGenerator>(true);
    }

    private bool Subscribe()
    {
        if (_subscribed) return true;

        if (_mapSource != null)
        {
            _mapSource.MapReady += HandleMapReady;
            _subscribed = true;
        }
        else if (_generatorFallback != null)
        {
            _generatorFallback.MapGenerated += HandleGeneratorGenerated;
            _subscribed = true;
        }

        return _subscribed;
    }

    private void Unsubscribe()
    {
        if (_mapSource != null) _mapSource.MapReady -= HandleMapReady;
        if (_generatorFallback != null) _generatorFallback.MapGenerated -= HandleGeneratorGenerated;
        _subscribed = false;
    }

    private void CatchUp()
    {
        if (maskSource == MaskSource.CeilingTilemap && ceilingOverride != null)
        {
            Build(ceilingOverride);
        }
        else if (_mapSource != null && _mapSource.CurrentMap != null)
        {
            HandleMapReady(_mapSource.CurrentMap);
        }
        else if (_generatorFallback != null)
        {
            HandleGeneratorGenerated();
        }
    }

    private void HandleGeneratorGenerated()
    {
        if (_generatorFallback == null) return;

        BuildForGenerator(_generatorFallback);
    }

    private void HandleMapReady(GameObject map)
    {
        if (map == null) return;

        var generator = map.GetComponentInChildren<ColorMapGenerator>(true);
        if (generator == null) return;

        BuildForGenerator(generator);
    }

    private void BuildForGenerator(ColorMapGenerator generator)
    {
        generator.ResolveReferences();

        if (maskSource == MaskSource.EmptyCells)
            BuildFromEmptyCells(generator);
        else
            Build(generator.CeilingTilemap);
    }

    [ContextMenu("Build Mask Now")]
    public void BuildNow()
    {
        ResolveSource();
        CatchUp();
    }

    public void Build(Tilemap ceiling)
    {
        if (ceiling == null) return;
        if (darknessMaterial == null || blurMaterial == null) return;

        ceiling.CompressBounds();
        BoundsInt bounds = ceiling.cellBounds;
        if (bounds.size.x <= 0 || bounds.size.y <= 0) return;

        int side = Mathf.Max(bounds.size.x, bounds.size.y) + extraMarginCells * 2;
        int minX = bounds.xMin - extraMarginCells;
        int minY = bounds.yMin - extraMarginCells;

        var cellIsMask = new bool[side * side];
        TileBase[] tiles = ceiling.GetTilesBlock(bounds);
        int bw = bounds.size.x;

        for (int cy = 0; cy < side; cy++)
        {
            for (int cx = 0; cx < side; cx++)
            {
                int wx = minX + cx;
                int wy = minY + cy;

                bool inside = wx >= bounds.xMin && wx < bounds.xMax &&
                              wy >= bounds.yMin && wy < bounds.yMax;

                bool value;
                if (inside)
                    value = tiles[(wx - bounds.xMin) + (wy - bounds.yMin) * bw] != null;
                else
                    value = marginIsMask;

                cellIsMask[cy * side + cx] = value;
            }
        }

        BuildTextures(cellIsMask, side, minX, minY, ceiling);
    }

    public void BuildFromEmptyCells(ColorMapGenerator generator)
    {
        if (generator == null) return;

        generator.ResolveReferences();

        Tilemap reference = generator.FloorTilemap != null ? generator.FloorTilemap : generator.CeilingTilemap;
        if (reference == null) return;
        if (darknessMaterial == null || blurMaterial == null) return;

        BoundsInt bounds = generator.EmptyBounds;
        if (bounds.size.x <= 0 || bounds.size.y <= 0) return;

        var cells = generator.EmptyCells;

        int side = Mathf.Max(bounds.size.x, bounds.size.y) + extraMarginCells * 2;
        int minX = bounds.xMin - extraMarginCells;
        int minY = bounds.yMin - extraMarginCells;

        var cellIsMask = new bool[side * side];

        for (int cy = 0; cy < side; cy++)
        {
            for (int cx = 0; cx < side; cx++)
            {
                int wx = minX + cx;
                int wy = minY + cy;

                bool inside = wx >= bounds.xMin && wx < bounds.xMax &&
                              wy >= bounds.yMin && wy < bounds.yMax;

                cellIsMask[cy * side + cx] = inside
                    ? cells.Contains(new Vector3Int(wx, wy, 0))
                    : marginIsMask;
            }
        }

        BuildTextures(cellIsMask, side, minX, minY, reference);
    }

    private void BuildTextures(bool[] cellIsMask, int side, int minX, int minY, Tilemap reference)
    {
        int texelsPerCellSafe = Mathf.Max(1, texelsPerCell);
        int texSize = side * texelsPerCellSafe;

        var data = new byte[texSize * texSize];
        for (int ty = 0; ty < texSize; ty++)
        {
            int cy = ty / texelsPerCellSafe;
            int row = ty * texSize;
            for (int tx = 0; tx < texSize; tx++)
            {
                int cx = tx / texelsPerCellSafe;
                data[row + tx] = cellIsMask[cy * side + cx] ? (byte)255 : (byte)0;
            }
        }

        var raw = new Texture2D(texSize, texSize, TextureFormat.R8, false, true)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        raw.SetPixelData(data, 0);
        raw.Apply();

        ReleaseRTs();

        var rtDesc = new RenderTextureDescriptor(texSize, texSize, RenderTextureFormat.R8, 0) { sRGB = false };

        _pingpongA = new RenderTexture(rtDesc) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        _pingpongA.Create();

        _pingpongB = new RenderTexture(rtDesc) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        _pingpongB.Create();

        Graphics.Blit(raw, _pingpongA);
        for (int i = 0; i < blurIterations; i++)
        {
            Graphics.Blit(_pingpongA, _pingpongB, blurMaterial, 0);
            Graphics.Blit(_pingpongB, _pingpongA, blurMaterial, 1);
        }

        RenderTexture.active = null;
        Destroy(raw);
        _hasBuilt = true;

        Vector3 originWorld = reference.CellToWorld(new Vector3Int(minX, minY, 0));
        Vector3 nextWorld = reference.CellToWorld(new Vector3Int(minX + 1, minY, 0));
        float cellWorldSize = Mathf.Abs(nextWorld.x - originWorld.x);
        if (cellWorldSize <= 0f) cellWorldSize = 1f;

        darknessMaterial.SetTexture("_MaskTex", _pingpongA);
        darknessMaterial.SetFloat("_WorldSize", side * cellWorldSize);
        darknessMaterial.SetVector("_WorldMin", new Vector2(originWorld.x, originWorld.y));
    }

    private void ReleaseRTs()
    {
        if (RenderTexture.active == _pingpongA || RenderTexture.active == _pingpongB)
            RenderTexture.active = null;

        if (_pingpongA != null) { _pingpongA.Release(); Destroy(_pingpongA); _pingpongA = null; }
        if (_pingpongB != null) { _pingpongB.Release(); Destroy(_pingpongB); _pingpongB = null; }
    }
}