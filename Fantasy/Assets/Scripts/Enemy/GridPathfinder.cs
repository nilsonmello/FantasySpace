using System.Collections.Generic;
using UnityEngine;

public static class GridPathfinder
{
    private class Node
    {
        public Vector2Int Cell;
        public Node Parent;
        public float G;
        public float H;
        public float F => G + H;
    }

    private static readonly Vector2Int[] Neighbors4 =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    public static List<Vector2Int> FindPath(DungeonGrid grid, Vector2Int start, Vector2Int goal)
    {
        if (!IsWalkable(grid, start) || !IsWalkable(grid, goal))
            return null;

        var open = new List<Node>();
        var openLookup = new Dictionary<Vector2Int, Node>();
        var closed = new HashSet<Vector2Int>();

        var startNode = new Node { Cell = start, G = 0f, H = Heuristic(start, goal) };
        open.Add(startNode);
        openLookup[start] = startNode;

        while (open.Count > 0)
        {
            open.Sort((a, b) => a.F.CompareTo(b.F));
            Node current = open[0];

            if (current.Cell == goal)
                return ReconstructPath(current);

            open.RemoveAt(0);
            openLookup.Remove(current.Cell);
            closed.Add(current.Cell);

            foreach (Vector2Int dir in Neighbors4)
            {
                Vector2Int neighborCell = current.Cell + dir;
                if (closed.Contains(neighborCell)) continue;
                if (!IsWalkable(grid, neighborCell)) continue;

                float tentativeG = current.G + 1f;

                if (!openLookup.TryGetValue(neighborCell, out Node neighborNode))
                {
                    neighborNode = new Node
                    {
                        Cell = neighborCell,
                        G = tentativeG,
                        H = Heuristic(neighborCell, goal),
                        Parent = current
                    };
                    open.Add(neighborNode);
                    openLookup[neighborCell] = neighborNode;
                }
                else if (tentativeG < neighborNode.G)
                {
                    neighborNode.G = tentativeG;
                    neighborNode.Parent = current;
                }
            }
        }

        return null; // sem caminho possível
    }

    /// <summary>
    /// Procura a célula caminhável mais próxima de 'origin', expandindo em
    /// anéis quadrados até maxRadius. Útil quando a posição exata (start ou
    /// goal) cai numa célula que nunca foi marcada (Empty por padrão) ou
    /// numa célula de buffer/entrada que não faz parte do path do corredor,
    /// mas que está fisicamente encostada numa área andável.
    /// Se nada for encontrado dentro do raio, retorna a própria origin
    /// (FindPath vai falhar e retornar null nesse caso, o que já é tratado
    /// pelos chamadores).
    /// </summary>
    public static Vector2Int FindNearestWalkable(DungeonGrid grid, Vector2Int origin, int maxRadius)
    {
        if (IsWalkable(grid, origin)) return origin;

        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius) continue; // só o "anel" externo

                    var candidate = origin + new Vector2Int(dx, dy);
                    if (IsWalkable(grid, candidate)) return candidate;
                }
            }
        }

        return origin;
    }

    private static bool IsWalkable(DungeonGrid grid, Vector2Int cell)
    {
        DungeonGrid.CellType type = grid.GetCell(cell);
        return type == DungeonGrid.CellType.Room || type == DungeonGrid.CellType.Corridor;
    }

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); // Manhattan, condiz com movimento 4-direcional
    }

    private static List<Vector2Int> ReconstructPath(Node node)
    {
        var path = new List<Vector2Int>();
        while (node != null)
        {
            path.Add(node.Cell);
            node = node.Parent;
        }
        path.Reverse();
        return path;
    }
}