using System.Collections.Generic;
using UnityEngine;

public class DungeonGrid
{
    public enum CellType { Empty, Room, Corridor, Buffer }

    public float CellSize { get; }

    private readonly Dictionary<Vector2Int, CellType> _cells = new Dictionary<Vector2Int, CellType>();

    public DungeonGrid(float cellSize)
    {
        CellSize = cellSize;
    }

    public IEnumerable<KeyValuePair<Vector2Int, CellType>> AllCells => _cells;

    public Vector2Int WorldToCell(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / CellSize),
            Mathf.RoundToInt(worldPos.y / CellSize));
    }

    public Vector2 CellToWorld(Vector2Int cell)
    {
        return new Vector2(cell.x * CellSize, cell.y * CellSize);
    }

    public CellType GetCell(Vector2Int cell)
    {
        return _cells.TryGetValue(cell, out var type) ? type : CellType.Empty;
    }

    public void SetCell(Vector2Int cell, CellType type)
    {
        _cells[cell] = type;
    }

    public void MarkRoomBounds(Bounds worldBounds)
    {
        Vector2Int centerCell = WorldToCell(worldBounds.center);
        int widthCells = Mathf.Max(1, Mathf.RoundToInt(worldBounds.size.x / CellSize));
        int heightCells = Mathf.Max(1, Mathf.RoundToInt(worldBounds.size.y / CellSize));

        int minX = centerCell.x - widthCells / 2;
        int maxX = minX + widthCells - 1;
        int minY = centerCell.y - heightCells / 2;
        int maxY = minY + heightCells - 1;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                SetCell(new Vector2Int(x, y), CellType.Room);
            }
        }
    }

    public void MarkRoomBuffer(Bounds worldBounds, int bufferCells)
    {
        if (bufferCells <= 0) return;

        Vector2Int min = WorldToCell(worldBounds.min);
        Vector2Int max = WorldToCell(worldBounds.max);

        for (int x = min.x - bufferCells; x <= max.x + bufferCells; x++)
        {
            for (int y = min.y - bufferCells; y <= max.y + bufferCells; y++)
            {
                var cell = new Vector2Int(x, y);
                if (GetCell(cell) == CellType.Room) continue;
                if (GetCell(cell) == CellType.Buffer) continue;

                SetCell(cell, CellType.Buffer);
            }
        }
    }

    public void MarkCorridorBuffer(IEnumerable<Vector2Int> corridorCells, int bufferCells)
    {
        if (bufferCells <= 0) return;

        foreach (var origin in corridorCells)
        {
            for (int x = -bufferCells; x <= bufferCells; x++)
            {
                for (int y = -bufferCells; y <= bufferCells; y++)
                {
                    var cell = origin + new Vector2Int(x, y);
                    CellType existing = GetCell(cell);
                    if (existing == CellType.Room || existing == CellType.Corridor || existing == CellType.Buffer)
                        continue;

                    SetCell(cell, CellType.Buffer);
                }
            }
        }
    }

    public void MarkEntrance(Vector2Int cell)
    {
        SetCell(cell, CellType.Empty);
    }

    public void MarkEntrance(Vector2Int cell, Vector2Int exitDirection, int clearanceCells)
    {
        MarkEntrance(cell, exitDirection, clearanceCells, 1);
    }

    public void MarkEntrance(Vector2Int cell, Vector2Int exitDirection, int clearanceCells, int width)
    {
        ClearDoorwayStrip(cell, exitDirection, width);

        Vector2Int cursor = cell;
        for (int i = 0; i < clearanceCells; i++)
        {
            cursor += exitDirection;
            ClearDoorwayStrip(cursor, exitDirection, width);
        }
    }

    private void ClearDoorwayStrip(Vector2Int center, Vector2Int exitDirection, int width)
    {
        if (width <= 1)
        {
            if (GetCell(center) != CellType.Room)
                SetCell(center, CellType.Empty);
            return;
        }

        var perpendicular = new Vector2Int(-exitDirection.y, exitDirection.x);
        int startOffset = -(width - 1) / 2;
        int endOffset = width / 2;

        for (int offset = startOffset; offset <= endOffset; offset++)
        {
            Vector2Int c = center + perpendicular * offset;
            if (GetCell(c) != CellType.Room)
                SetCell(c, CellType.Empty);
        }
    }

    public bool IsWalkable(Vector2Int cell)
    {
        CellType type = GetCell(cell);
        return type == CellType.Empty || type == CellType.Corridor;
    }
}