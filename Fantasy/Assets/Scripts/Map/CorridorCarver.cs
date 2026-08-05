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

    public static List<Vector2Int> FindPath(DungeonGrid grid, Vector2Int start, Vector2Int end)
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
                return ReconstructPath(current);

            closed.Add(current.Position);

            foreach (var dir in Directions)
            {
                Vector2Int neighborPos = current.Position + dir;

                if (closed.Contains(neighborPos)) continue;

                bool isDestination = neighborPos == end;
                if (!isDestination && !grid.IsWalkable(neighborPos)) continue;

                bool isTurn = current.DirFromParent != Vector2Int.zero && current.DirFromParent != dir;
                float turnPenalty = isTurn ? 0.5f : 0f;
                float tentativeG = current.G + 1f + turnPenalty;

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