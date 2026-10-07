using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Lê um Tilemap "fonte" pintado com tiles de cores básicas e, para cada célula,
/// procura a cor no ColorTileMapping e coloca os tiles correspondentes nas
/// três camadas de destino (chão, paredes, teto), nas MESMAS posições de célula.
///
/// Dá para alimentar o Apply() com qualquer grid de cores no futuro
/// (gerador procedural), sem depender do Tilemap fonte.
/// </summary>
public class ColorMapGenerator : MonoBehaviour
{
    public enum ColorSource
    {
        /// <summary>Lê o pixel central do sprite do tile (textura precisa de Read/Write).</summary>
        SpritePixel,
        /// <summary>Usa a cor do asset Tile multiplicada pela cor da célula no Tilemap.
        /// Bom se seus tiles são um sprite branco tingido.</summary>
        TileTint
    }

    [Header("Dados")]
    [SerializeField] private ColorTileMapping mapping;

    [Header("Fonte (Tilemap pintado com as cores)")]
    [SerializeField] private Tilemap sourceTilemap;
    [SerializeField] private ColorSource colorSource = ColorSource.SpritePixel;
    [Tooltip("Desativa o TilemapRenderer da fonte depois de gerar (o Clear reativa).")]
    [SerializeField] private bool hideSourceOnGenerate = true;

    [Header("Camadas de destino")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap ceilingTilemap;

    [Header("Preenchimento aleatório (opcional)")]
    [Tooltip("Sorteia tiles para as células vazias do mapa. Deixe vazio para desligar.")]
    [SerializeField] private EmptyCellFill emptyFill;

    [Header("Opções")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool logUnmappedColors = true;

    /// <summary>Disparado ao final de cada geração (camadas e preenchimento já aplicados).</summary>
    public event System.Action MapGenerated;

    private readonly Dictionary<Sprite, Color32> spriteColorCache = new Dictionary<Sprite, Color32>();

    private void Start()
    {
        if (generateOnStart) Generate();
    }

    /// <summary>Lê o Tilemap fonte e gera as três camadas.</summary>
    [ContextMenu("Generate")]
    public void Generate()
    {
        if (!ValidateReferences()) return;

        BoundsInt src = sourceTilemap.cellBounds;
        if (src.size.x <= 0 || src.size.y <= 0)
        {
            Debug.LogWarning("[ColorMapGenerator] O Tilemap fonte está vazio.", this);
            return;
        }

        // Só 2D: força z = 0 com profundidade 1.
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

    /// <summary>
    /// Preenche as três camadas a partir de um grid de cores.
    /// Índice = (x - bounds.xMin) + (y - bounds.yMin) * largura. Alpha 0 = célula vazia.
    /// </summary>
    public void Apply(Color32[] pixels, BoundsInt bounds)
    {
        if (mapping == null || floorTilemap == null || wallTilemap == null || ceilingTilemap == null)
        {
            Debug.LogError("[ColorMapGenerator] Falta atribuir o mapping ou alguma camada de destino.", this);
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
            else if (logUnmappedColors && unmapped.Add((px.r << 16) | (px.g << 8) | px.b))
            {
                Debug.LogWarning($"[ColorMapGenerator] Cor sem mapeamento: " +
                                 $"#{px.r:X2}{px.g:X2}{px.b:X2} " +
                                 $"(ex.: célula {bounds.xMin + i % width}, {bounds.yMin + i / width})", this);
            }
        }

        floorTilemap.SetTilesBlock(bounds, floorTiles);
        wallTilemap.SetTilesBlock(bounds, wallTiles);
        ceilingTilemap.SetTilesBlock(bounds, ceilingTiles);

        FillEmptyCells(pixels, bounds);

        MapGenerated?.Invoke();
    }

    /// <summary>
    /// Sorteia tiles para as células sem tile no fonte (alpha 0), mais a margem de padding.
    /// </summary>
    private void FillEmptyCells(Color32[] pixels, BoundsInt bounds)
    {
        if (emptyFill == null) return;

        if (!emptyFill.HasTiles)
        {
            Debug.LogWarning("[ColorMapGenerator] O Empty Fill não tem nenhum tile válido " +
                             "(tile vazio ou Weight = 0).", this);
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

                // Dentro do fonte, só preenche onde não havia tile.
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

    /// <summary>Limpa as camadas de destino e mostra o Tilemap fonte de novo.</summary>
    [ContextMenu("Clear")]
    public void Clear()
    {
        ClearDestinations();

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

    // ---------------------------------------------------------------- leitura

    private Color32[] ReadSourceColors(BoundsInt bounds)
    {
        spriteColorCache.Clear();

        // Garante que Rule Tiles do fonte já escolheram seus sprites antes da leitura.
        sourceTilemap.RefreshAllTiles();

        TileBase[] tiles = sourceTilemap.GetTilesBlock(bounds);
        var grid = new Color32[tiles.Length]; // default = alpha 0 (vazio)
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

            color.a = 255; // existe tile nessa célula
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
            Debug.LogError($"[ColorMapGenerator] A textura '{tex.name}' (sprite '{sprite.name}') " +
                           "precisa de Read/Write habilitado nas Import Settings, " +
                           "ou use o modo TileTint.", this);
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
        if (sourceTilemap == null)
        {
            Debug.LogError("[ColorMapGenerator] Nenhum Tilemap fonte atribuído.", this);
            return false;
        }

        if (sourceTilemap == floorTilemap || sourceTilemap == wallTilemap || sourceTilemap == ceilingTilemap)
        {
            Debug.LogError("[ColorMapGenerator] O Tilemap fonte não pode ser um dos Tilemaps de destino.", this);
            return false;
        }

        return true;
    }
}