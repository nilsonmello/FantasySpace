using System.Collections.Generic;
using UnityEngine;

public static class CorridorCarver
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private class Node
    {
        public Vector2Int Position;
        public Node Parent;
        public Vector2Int DirFromParent;
        public float G;
        public float H;
        public float F => G + H;
    }

    public static List<Vector2Int> FindPath(
        DungeonGrid grid,
        Vector2Int start,
        Vector2Int end,
        float hugPenaltyWeight = 4f)
    {
        var open = new List<Node>();
        var closed = new HashSet<Vector2Int>();
        var allNodes = new Dictionary<Vector2Int, Node>();

        var startNode = new Node { Position = start, G = 0f, H = Heuristic(start, end) };
        open.Add(startNode);
        allNodes[start] = startNode;

        const int maxIterations = 20000;
        int iterations = 0;

        while (open.Count > 0 && iterations++ < maxIterations)
        {
            open.Sort((a, b) => a.F.CompareTo(b.F));
            Node current = open[0];
            open.RemoveAt(0);

            if (current.Position == end)
            {
                List<Vector2Int> rawPath = ReconstructPath(current);

                return SimplifyStaircase(rawPath, grid);
            }

            closed.Add(current.Position);

            foreach (var dir in Directions)
            {
                Vector2Int neighborPos = current.Position + dir;

                if (closed.Contains(neighborPos)) continue;

                bool isDestination = neighborPos == end;
                if (!isDestination && !grid.IsWalkable(neighborPos)) continue;

                bool isTurn = current.DirFromParent != Vector2Int.zero && current.DirFromParent != dir;
                float turnPenalty = isTurn ? 0.5f : 0f;
                float hugPenalty = hugPenaltyWeight > 0f
                    ? HugPenalty(grid, current.Position, neighborPos, hugPenaltyWeight)
                    : 0f;
                float tentativeG = current.G + 1f + turnPenalty + hugPenalty;

                if (!allNodes.TryGetValue(neighborPos, out var neighborNode))
                {
                    neighborNode = new Node { Position = neighborPos };
                    allNodes[neighborPos] = neighborNode;
                    open.Add(neighborNode);
                }
                else if (tentativeG >= neighborNode.G)
                {
                    continue;
                }

                neighborNode.Parent = current;
                neighborNode.DirFromParent = dir;
                neighborNode.G = tentativeG;
                neighborNode.H = Heuristic(neighborPos, end);
            }
        }

        return null;
    }

    private static int FindStaircaseRunEnd(List<Vector2Int> path, int start)
    {
        if (start + 1 >= path.Count) return start;

        Vector2Int dirPrev = path[start + 1] - path[start];
        int end = start + 1;

        for (int j = start + 1; j < path.Count - 1; j++)
        {
            Vector2Int dirNext = path[j + 1] - path[j];

            if (dirNext == dirPrev) break;

            end = j + 1;
            dirPrev = dirNext;
        }

        return end;
    }

    private static List<Vector2Int> BuildLShape(Vector2Int from, Vector2Int to)
    {
        var result = new List<Vector2Int> { from };
        Vector2Int cursor = from;

        int stepX = (int)Mathf.Sign(to.x - from.x);
        while (cursor.x != to.x)
        {
            cursor += new Vector2Int(stepX, 0);
            result.Add(cursor);
        }

        int stepY = (int)Mathf.Sign(to.y - from.y);
        while (cursor.y != to.y)
        {
            cursor += new Vector2Int(0, stepY);
            result.Add(cursor);
        }

        return result;
    }

    private static bool IsSafeReplacement(List<Vector2Int> cells, DungeonGrid grid)
    {
        foreach (var cell in cells)
        {
            if (grid.GetCell(cell) == DungeonGrid.CellType.Room)
                return false;
        }
        return true;
    }

    private static List<Vector2Int> SimplifyStaircase(List<Vector2Int> path, DungeonGrid grid)
    {
        if (path.Count < 3) return path;

        var result = new List<Vector2Int> { path[0] };
        int i = 0;

        while (i < path.Count - 1)
        {
            int runEnd = FindStaircaseRunEnd(path, i);

            if (runEnd - i < 3)
            {
                result.Add(path[i + 1]);
                i++;
                continue;
            }

            Vector2Int segStart = path[i];
            Vector2Int segEnd = path[runEnd];
            List<Vector2Int> straightened = BuildLShape(segStart, segEnd);

            if (IsSafeReplacement(straightened, grid))
            {
                for (int k = 1; k < straightened.Count; k++)
                    result.Add(straightened[k]);
            }
            else
            {
                for (int k = i + 1; k <= runEnd; k++)
                    result.Add(path[k]);
            }

            i = runEnd;
        }

        return result;
    }

    private static float HugPenalty(DungeonGrid grid, Vector2Int current, Vector2Int candidate, float weight)
    {
        if (grid.GetCell(candidate) == DungeonGrid.CellType.Corridor)
            return 0f;

        float penalty = 0f;
        foreach (var dir in Directions)
        {
            Vector2Int neighbor = candidate + dir;
            if (neighbor == current) continue;

            if (grid.GetCell(neighbor) == DungeonGrid.CellType.Corridor)
                penalty += weight;
        }
        return penalty;
    }

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static List<Vector2Int> ReconstructPath(Node endNode)
    {
        var path = new List<Vector2Int>();
        Node current = endNode;
        while (current != null)
        {
            path.Add(current.Position);
            current = current.Parent;
        }
        path.Reverse();
        return path;
    }
}