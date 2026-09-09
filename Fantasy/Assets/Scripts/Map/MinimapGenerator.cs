using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapGenerator : MonoBehaviour
{
    [Header("Map")]
    [SerializeField] private DungeonGenerator dungeonGenerator;

    [Header("UI")]
    [SerializeField] private RawImage mapImage;

    [SerializeField] private RectTransform playerMarker;

    [SerializeField] private Transform playerTransform;

    [SerializeField] private bool rotateMarkerWithPlayer = true;

    [Header("Minimap Window")]
    [SerializeField] private float minimapRadiusCells = 12f;
    [SerializeField] private bool rotateMapWithPlayer = false;
    [SerializeField] private bool clampWindowToBounds = false;

    [Header("Color")]
    [SerializeField] private Color32 backgroundColor = new Color32(0, 0, 0, 0);
    [SerializeField] private Color32 corridorColor = new Color32(120, 120, 120, 255);
    [SerializeField] private Color32 roomCommonColor = new Color32(200, 200, 200, 255);
    [SerializeField] private Color32 roomSpawnColor = new Color32(80, 200, 100, 255);
    [SerializeField] private Color32 roomBossColor = new Color32(200, 60, 60, 255);
    [SerializeField] private Color32 roomTreasureColor = new Color32(230, 200, 60, 255);
    [SerializeField] private Color32 roomSecretColor = new Color32(140, 90, 200, 255);

    private Texture2D _texture;
    private Vector2Int _cellBoundsMin;
    private Vector2Int _cellBoundsMax;
    private bool _hasValidBounds;

    private void OnEnable()
    {
        if (dungeonGenerator != null)
            dungeonGenerator.OnGenerationComplete += BuildMinimap;
    }

    private void OnDisable()
    {
        if (dungeonGenerator != null)
            dungeonGenerator.OnGenerationComplete -= BuildMinimap;
    }

    private void Update()
    {
        if (_hasValidBounds) UpdateMinimapWindow();

        FindPlayerTransform();
    }

    private void FindPlayerTransform()
    {
        if (playerTransform != null) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    public void BuildMinimap()
    {
        DungeonGrid grid = dungeonGenerator.Grid;
        if (grid == null || mapImage == null) return;

        if (!TryComputeCellBounds(grid, out _cellBoundsMin, out _cellBoundsMax))
        {
            _hasValidBounds = false;
            return;
        }

        int padding = Mathf.Max(1, Mathf.CeilToInt(minimapRadiusCells));
        _cellBoundsMin -= new Vector2Int(padding, padding);
        _cellBoundsMax += new Vector2Int(padding, padding);

        int width = _cellBoundsMax.x - _cellBoundsMin.x + 1;
        int height = _cellBoundsMax.y - _cellBoundsMin.y + 1;

        var pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = backgroundColor;

        foreach (var kvp in grid.AllCells)
        {
            if (kvp.Value != DungeonGrid.CellType.Corridor) continue;
            SetPixelSafe(pixels, width, height, kvp.Key, corridorColor);
        }

        foreach (var room in dungeonGenerator.PlacedRooms)
        {
            Color32 color = GetRoomColor(room.SourceData != null ? room.SourceData.roomType : RoomData.RoomType.Common);

            Vector2Int cellMin = grid.WorldToCell(room.WorldBounds.min);
            Vector2Int cellMax = grid.WorldToCell(room.WorldBounds.max);

            for (int x = cellMin.x; x <= cellMax.x; x++)
                for (int y = cellMin.y; y <= cellMax.y; y++)
                    SetPixelSafe(pixels, width, height, new Vector2Int(x, y), color);
        }

        if (_texture == null || _texture.width != width || _texture.height != height)
        {
            if (_texture != null) Destroy(_texture);
            _texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        _texture.SetPixels32(pixels);
        _texture.Apply();

        mapImage.texture = _texture;
        _hasValidBounds = true;
    }

    private bool TryComputeCellBounds(DungeonGrid grid, out Vector2Int min, out Vector2Int max)
    {
        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        bool any = false;

        foreach (var kvp in grid.AllCells)
        {
            if (kvp.Value != DungeonGrid.CellType.Room && kvp.Value != DungeonGrid.CellType.Corridor)
                continue;

            any = true;
            if (kvp.Key.x < minX) minX = kvp.Key.x;
            if (kvp.Key.x > maxX) maxX = kvp.Key.x;
            if (kvp.Key.y < minY) minY = kvp.Key.y;
            if (kvp.Key.y > maxY) maxY = kvp.Key.y;
        }

        min = new Vector2Int(minX, minY);
        max = new Vector2Int(maxX, maxY);
        return any;
    }

    private void SetPixelSafe(Color32[] pixels, int width, int height, Vector2Int cell, Color32 color)
    {
        int x = cell.x - _cellBoundsMin.x;
        int y = cell.y - _cellBoundsMin.y;
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        pixels[y * width + x] = color;
    }

    private Color32 GetRoomColor(RoomData.RoomType type)
    {
        switch (type)
        {
            case RoomData.RoomType.Spawn: return roomSpawnColor;
            case RoomData.RoomType.Boss: return roomBossColor;
            case RoomData.RoomType.Treasure: return roomTreasureColor;
            case RoomData.RoomType.Secret: return roomSecretColor;
            default: return roomCommonColor;
        }
    }

    private void UpdateMinimapWindow()
    {
        if (playerTransform == null || mapImage == null || dungeonGenerator.Grid == null) return;

        Vector2Int playerCell = dungeonGenerator.Grid.WorldToCell(playerTransform.position);

        int totalWidth = _cellBoundsMax.x - _cellBoundsMin.x + 1;
        int totalHeight = _cellBoundsMax.y - _cellBoundsMin.y + 1;

        float u = (playerCell.x - _cellBoundsMin.x) / (float)totalWidth;
        float v = (playerCell.y - _cellBoundsMin.y) / (float)totalHeight;

        float uvWidth = Mathf.Clamp01((minimapRadiusCells * 2f) / totalWidth);
        float uvHeight = Mathf.Clamp01((minimapRadiusCells * 2f) / totalHeight);

        float uMin = u - uvWidth * 0.5f;
        float vMin = v - uvHeight * 0.5f;

        uMin = Mathf.Clamp(uMin, 0f, 1f - uvWidth);
        vMin = Mathf.Clamp(vMin, 0f, 1f - uvHeight);

        mapImage.uvRect = new Rect(uMin, vMin, uvWidth, uvHeight);

        if (playerMarker == null) return;

        playerMarker.anchoredPosition = Vector2.zero;

        float angle = Mathf.Atan2(playerTransform.up.y, playerTransform.up.x) * Mathf.Rad2Deg - 90f;

        if (rotateMapWithPlayer)
        {
            mapImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
            playerMarker.localRotation = Quaternion.identity;
        }
        else if (rotateMarkerWithPlayer)
        {
            playerMarker.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}