using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DungeonDarknessMask : MonoBehaviour
{
    [SerializeField] private DungeonGenerator dungeonGenerator;
    [SerializeField] private Material blurMaterial;
    [SerializeField] private Material darknessMaterial;

    [SerializeField] private int texelsPerCell = 8;

    [Range(0, 8)]
    [SerializeField] private int blurIterations = 3;

    [SerializeField] private float extraMarginCells = 4f;

    private RenderTexture _pingpongA;
    private RenderTexture _pingpongB;
    private SpriteRenderer _spriteRenderer;

    private SpriteRenderer SpriteRenderer =>
        _spriteRenderer != null ? _spriteRenderer : (_spriteRenderer = GetComponent<SpriteRenderer>());

    private void OnEnable()
    {
        if (dungeonGenerator != null)
            dungeonGenerator.OnGenerationComplete += BuildMask;
    }

    private void OnDisable()
    {
        if (dungeonGenerator != null)
            dungeonGenerator.OnGenerationComplete -= BuildMask;

        ReleaseRTs();
    }

    [ContextMenu("Build Mask Now")]
    public void BuildMask()
    {
        if (dungeonGenerator == null || dungeonGenerator.Grid == null)
        {
            return;
        }

        if (darknessMaterial == null || blurMaterial == null)
        {
            return;
        }

        var grid = dungeonGenerator.Grid;
        float cellSize = grid.CellSize;

        if (!dungeonGenerator.GetWallSquareBounds(out int squareMinX, out int squareMinY, out int side))
        {
            return;
        }

        int marginCells = Mathf.RoundToInt(extraMarginCells);
        squareMinX -= marginCells;
        squareMinY -= marginCells;
        side += marginCells * 2;

        int texSize = side * texelsPerCell;

        var raw = new Texture2D(texSize, texSize, TextureFormat.R8, false, true);
        var pixels = new Color[texSize * texSize];

        for (int tx = 0; tx < texSize; tx++)
        {
            int cx = squareMinX + tx / texelsPerCell;
            for (int ty = 0; ty < texSize; ty++)
            {
                int cy = squareMinY + ty / texelsPerCell;
                DungeonGrid.CellType type = grid.GetCell(new Vector2Int(cx, cy));
                bool isWall = type != DungeonGrid.CellType.Room && type != DungeonGrid.CellType.Corridor;
                pixels[ty * texSize + tx] = isWall ? Color.white : Color.black;
            }
        }

        raw.SetPixels(pixels);
        raw.Apply();

        ReleaseRTs();
        var format = RenderTextureFormat.R8;

        RenderTexture rawRT = RenderTexture.GetTemporary(texSize, texSize, 0, format, RenderTextureReadWrite.Linear);

        var rtDesc = new RenderTextureDescriptor(texSize, texSize, format, 0) { sRGB = false };

        _pingpongA = new RenderTexture(rtDesc) { filterMode = FilterMode.Bilinear };
        _pingpongA.Create();

        _pingpongB = new RenderTexture(rtDesc) { filterMode = FilterMode.Bilinear };
        _pingpongB.Create();

        Graphics.Blit(raw, rawRT);
        Graphics.Blit(rawRT, _pingpongA);
        for (int i = 0; i < blurIterations; i++)
        {
            Graphics.Blit(_pingpongA, _pingpongB, blurMaterial, 0);
            Graphics.Blit(_pingpongB, _pingpongA, blurMaterial, 1);
        }

        RenderTexture.ReleaseTemporary(rawRT);
        Destroy(raw);

        darknessMaterial.SetTexture("_MaskTex", _pingpongA);

        float worldSize = side * cellSize;

        darknessMaterial.SetFloat("_WorldSize", worldSize);

        // Canto inferior-esquerdo do dungeon em coordenadas de mundo.
        // O shader usa isso pra converter world-space -> espaço da máscara,
        // independente do tamanho/posição do quad que desenha o efeito
        // (agora esse quad segue a câmera, ver DarknessMaskCameraFollow).
        var worldMin = new Vector2(squareMinX * cellSize, squareMinY * cellSize);
        darknessMaterial.SetVector("_WorldMin", worldMin);

        // Removido: antes este objeto era escalado/posicionado pra cobrir
        // o dungeon inteiro (transform.position = centro do mundo,
        // transform.localScale = worldSize / nativeSize). Agora quem
        // controla posição/escala deste sprite é o DarknessMaskCameraFollow,
        // que mantém o quad do tamanho da tela e o move com a câmera —
        // isso permite frustum culling e limita o custo do fragment shader
        // à área visível.
    }

    private void ReleaseRTs()
    {
        if (_pingpongA != null) { _pingpongA.Release(); _pingpongA = null; }
        if (_pingpongB != null) { _pingpongB.Release(); _pingpongB = null; }
    }
}
