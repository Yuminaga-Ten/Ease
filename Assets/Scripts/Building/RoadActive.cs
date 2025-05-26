using System.Collections.Generic;
using UnityEngine;

public class RoadActive : MonoBehaviour
{
    private MapGridManager grid;
    public HashSet<Vector2Int> ActiveRoads { get; private set; } = new();

    private readonly List<Vector2Int> directions = new()
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    void Start()
    {
        grid = FindObjectOfType<MapGridManager>();
    }

    public void RecalculateFromMainBuilding(Vector2Int unused, int size)
    {
        ActiveRoads.Clear();
        HashSet<Vector2Int> visited = new();
        Queue<Vector2Int> queue = new();

        // 找出所有主建築格子
        Dictionary<Vector2Int, string> allTiles = grid.GetAllOccupiedTiles();
        List<Vector2Int> mainBuildingTiles = new();
        foreach (var kvp in allTiles)
        {
            if (kvp.Value == "MainBuilding")
                mainBuildingTiles.Add(kvp.Key);
        }

        if (mainBuildingTiles.Count == 0) return;

        // 找到最左下角作為新的 origin
        Vector2Int origin = mainBuildingTiles[0];
        foreach (var pos in mainBuildingTiles)
        {
            if (pos.x < origin.x || (pos.x == origin.x && pos.y < origin.y))
                origin = pos;
        }

        // 從主建築邊緣（不含斜角）搜尋道路起點
        for (int x = -1; x <= size; x++)
        {
            for (int y = -1; y <= size; y++)
            {
                if (x >= 0 && x < size && y >= 0 && y < size) continue;
                if ((x == -1 && y == -1) || (x == -1 && y == size) || (x == size && y == -1) || (x == size && y == size)) continue;

                Vector2Int pos = origin + new Vector2Int(x, y);
                if (grid.GetBuildingType(pos) == "Road")
                {
                    queue.Enqueue(pos);
                    visited.Add(pos);
                }
            }
        }

        // BFS 遍歷道路
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            ActiveRoads.Add(current);

            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = current + dir;
                if (visited.Contains(next)) continue;
                if (grid.GetBuildingType(next) != "Road") continue;

                queue.Enqueue(next);
                visited.Add(next);
            }
        }

        Debug.Log($"[RoadActive] 啟用道路數量：{ActiveRoads.Count}");
    }

    public bool IsRoadActive(Vector2Int pos)
    {
        return ActiveRoads.Contains(pos);
    }
}
