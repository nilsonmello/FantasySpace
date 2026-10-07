using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapMaskBuilder : MonoBehaviour
{
    [Header("Origem")]
    [Tooltip("Opcional. Se atribuído, a máscara é refeita sempre que o gerador terminar de gerar o mapa.")]
    [SerializeField] private ColorMapGenerator generator;
    [Tooltip("Tilemaps lidos. Só o chão = 1 item; as três camadas = 3 itens.")]
    [SerializeField] private List<Tilemap> sources = new List<Tilemap>();

    [Header("Destino")]
    [Tooltip("Tilemap que recebe os quadrados brancos (um objeto separado, filho do Grid).")]
    [SerializeField] private Tilemap target;
    [Tooltip("Opcional. Se vazio, o script cria um tile de quadrado branco em runtime.")]
    [SerializeField] private TileBase whiteTile;
    [Tooltip("Cor aplicada ao Tilemap de destino inteiro (branco = sem alteração).")]
    [SerializeField] private Color maskColor = Color.white;

    [Header("Opções")]
    [Tooltip("Só vale quando nenhum gerador está atribuído.")]
    [SerializeField] private bool buildOnStart = true;

    private Tile runtimeTile;
    private Sprite runtimeSprite;
    private Texture2D runtimeTexture;

    private void OnEnable()
    {
        if (generator != null) generator.MapGenerated += Build;
    }

    private void OnDisable()
    {
        if (generator != null) generator.MapGenerated -= Build;
    }

    private void Start()
    {
        if (generator == null && buildOnStart) Build();
    }

    private void OnDestroy()
    {
        if (runtimeTile != null) Destroy(runtimeTile);
        if (runtimeSprite != null) Destroy(runtimeSprite);
        if (runtimeTexture != null) Destroy(runtimeTexture);
    }

    /// <summary>Refaz a máscara a partir do estado atual dos Tilemaps de origem.</summary>
    [ContextMenu("Build")]
    public void Build()
    {
        if (target == null)
        {
            Debug.LogError("[TilemapMaskBuilder] Nenhum Tilemap de destino atribuído.", this);
            return;
        }

        if (sources.Count == 0)
        {
            Debug.LogWarning("[TilemapMaskBuilder] A lista de origens está vazia.", this);
            return;
        }

        target.ClearAllTiles();

        // União das células ocupadas em todas as origens.
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

    /// <summary>Apaga a máscara.</summary>
    [ContextMenu("Clear")]
    public void Clear()
    {
        if (target != null) target.ClearAllTiles();
    }

    private TileBase GetMaskTile()
    {
        if (whiteTile != null) return whiteTile;
        if (runtimeTile != null) return runtimeTile;

        // Textura 1x1 branca; o PPU é ajustado para o sprite ocupar exatamente 1 célula.
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
