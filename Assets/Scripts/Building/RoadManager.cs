// RoadManager.cs（完整重構：新增無連接預設圖、全自動轉角更新）

using System.Collections.Generic;
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    [Header("道路預製物件與樣式")]
    public GameObject roadTilePrefab;
    // 將自 Resources/Sprites/Road 中載入（共 12 張）
    private Sprite[] roadSprites = new Sprite[12];
    public Color previewColor = new Color(1, 1, 1, 0.4f);
    public Color deletePreviewColor = new Color(1f, 0.3f, 0.3f, 0.4f);
    public Color deleteFinalColor = new Color(1f, 0.3f, 0.3f, 1f);

    private Dictionary<Vector2Int, RoadTile> builtRoads = new();
    private Dictionary<Vector2Int, RoadTile> previewTiles = new();
    private HashSet<Vector2Int> markedForDelete = new();
    private HashSet<Vector2Int> pendingEraseTiles = new();

    private Vector2Int? dragStart = null;
    private Vector2Int? lastHoverPos = null;
    private bool isPlacing = false;
    private bool isDeleteMode = false;

    private Camera cam;
    private MapGridManager grid;


    void LoadAllRoadSprites()
    {
        var nameToIndex = new Dictionary<string, int>
        {
            { "下轉", 0 },
            { "上轉", 1 },
            { "直右撇", 2 },
            { "左轉", 3 },
            { "右轉", 4 },
            { "直左撇", 5 },
            { "左T", 6 },
            { "上T", 7 },
            { "下T", 8 },
            { "右T", 9 },
            { "十字", 10 },
            { "無連接預設圖", 11 }
        };

        Sprite[] loaded = Resources.LoadAll<Sprite>("Sprites/Roads");
        int countLoaded = 0;
        foreach (Sprite s in loaded)
        {
            if (nameToIndex.TryGetValue(s.name, out int idx))
            {
                roadSprites[idx] = s;
                countLoaded++;
            }
            else
            {
                Debug.LogWarning($"[RoadManager] 無法對應圖片名稱：{s.name}");
            }
        }
        Debug.Log($"[RoadManager] 已載入 {countLoaded} 張道路圖（總資源檔案：{loaded.Length}）");
    }

    void Start()
    {
        cam = Camera.main;
        grid = FindObjectOfType<MapGridManager>();
        LoadAllRoadSprites();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1)) ToggleBuildMode();
        if (!isPlacing) return;
        HandleBuildInput();
    }

    void ToggleBuildMode()
    {
        isPlacing = !isPlacing;
        dragStart = null;
        lastHoverPos = null;
        pendingEraseTiles.Clear();
        markedForDelete.Clear();
        ClearPreview();

        UIMode.IsMouseDragAllowed = !isPlacing;
        UIMode.IsMouseEdgeScrollAllowed = isPlacing;
    }

    void HandleBuildInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragStart = GetGridPosUnderMouse();
            lastHoverPos = null;
            isDeleteMode = DetermineDeleteMode(dragStart.Value);
        }

        if (Input.GetMouseButton(0) && dragStart != null)
        {
            Vector2Int pos = GetGridPosUnderMouse();
            if (lastHoverPos == null || pos != lastHoverPos.Value)
            {
                DrawPreviewPoint(pos);
                lastHoverPos = pos;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            dragStart = null;
            lastHoverPos = null;
            ProcessPendingErase();
        }

        if (Input.GetKeyDown(KeyCode.Space)) ConfirmPreview();
    }

    bool DetermineDeleteMode(Vector2Int pos)
    {
        return previewTiles.ContainsKey(pos) || builtRoads.ContainsKey(pos);
    }

    void DrawPreviewPoint(Vector2Int pos)
    {
        if (isDeleteMode)
        {
            if (previewTiles.ContainsKey(pos))
            {
                previewTiles[pos].SetColor(deletePreviewColor);
                pendingEraseTiles.Add(pos);
                return;
            }

            if (builtRoads.ContainsKey(pos) && !markedForDelete.Contains(pos))
            {
                markedForDelete.Add(pos);
                builtRoads[pos].SetColor(deleteFinalColor);
                return;
            }
        }
        else
        {
            if (previewTiles.ContainsKey(pos) || builtRoads.ContainsKey(pos)) return;
            if (!grid.IsRegionBuildable(grid.GetRegionName(pos.x, pos.y))) return;

            Vector3 worldPos = grid.GridToWorld(pos);
            GameObject go = Instantiate(roadTilePrefab, worldPos, Quaternion.identity, transform);
            RoadTile rt = go.GetComponent<RoadTile>();
            rt.gridPos = pos;
            rt.SetColor(previewColor);
            previewTiles[pos] = rt;

            UpdateSurrounding(pos); // 🔄 建造時立刻更新樣式
        }
    }

    void ProcessPendingErase()
    {
        foreach (Vector2Int pos in pendingEraseTiles)
        {
            if (previewTiles.ContainsKey(pos))
            {
                Destroy(previewTiles[pos].gameObject);
                previewTiles.Remove(pos);
                UpdateSurrounding(pos);
            }
        }
        pendingEraseTiles.Clear();

        foreach (Vector2Int pos in markedForDelete)
        {
            if (builtRoads.ContainsKey(pos))
            {
                Destroy(builtRoads[pos].gameObject);
                builtRoads.Remove(pos);
                UpdateSurrounding(pos);
            }
        }
        markedForDelete.Clear();
    }

    void ConfirmPreview()
    {
        foreach (var kvp in previewTiles)
        {
            Vector2Int pos = kvp.Key;
            RoadTile tile = kvp.Value;

            tile.SetColor(Color.white);
            builtRoads[pos] = tile;
        }

        foreach (var kvp in previewTiles)
        {
            UpdateSurrounding(kvp.Key); // ✅ 每個格都會更新樣式
        }

        previewTiles.Clear();
        pendingEraseTiles.Clear();
        markedForDelete.Clear();
        lastHoverPos = null;
    }

    Vector2Int GetGridPosUnderMouse()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        if (plane.Raycast(ray, out float enter))
        {
            Vector3 worldPos = ray.GetPoint(enter);
            return grid.WorldToGrid(worldPos);
        }
        return new Vector2Int(-1, -1);
    }

    void UpdateSurrounding(Vector2Int center)
    {
        List<Vector2Int> area = new()
        {
            center,
            center + Vector2Int.up,
            center + Vector2Int.down,
            center + Vector2Int.left,
            center + Vector2Int.right
        };

        foreach (Vector2Int pos in area)
        {
            RoadTile rt = null;
            if (builtRoads.TryGetValue(pos, out rt) || previewTiles.TryGetValue(pos, out rt))
            {
                bool u = builtRoads.ContainsKey(pos + Vector2Int.up) || previewTiles.ContainsKey(pos + Vector2Int.up);
                bool d = builtRoads.ContainsKey(pos + Vector2Int.down) || previewTiles.ContainsKey(pos + Vector2Int.down);
                bool l = builtRoads.ContainsKey(pos + Vector2Int.left) || previewTiles.ContainsKey(pos + Vector2Int.left);
                bool r = builtRoads.ContainsKey(pos + Vector2Int.right) || previewTiles.ContainsKey(pos + Vector2Int.right);
                rt.SetSprite(GetCorrectSprite(u, d, l, r));
            }
        }
    }

    Sprite GetCorrectSprite(bool u, bool d, bool l, bool r)
    {
        string key = $"{(u ? "1" : "0")}{(d ? "1" : "0")}{(l ? "1" : "0")}{(r ? "1" : "0")}";
        return key switch
        {
            "0101" => roadSprites[0],  // 直右撇
            "1010" => roadSprites[1],  // 直左撇
            "0011" => roadSprites[2],  // 右轉
            "0110" => roadSprites[3],  // 上轉
            "1001" => roadSprites[4],  // 下轉
            "1100" => roadSprites[5],  // 左轉
            "1110" => roadSprites[6],  // 上T
            "1011" => roadSprites[7],  // 右T
            "0111" => roadSprites[8],  // 下T
            "1101" => roadSprites[9],  // 左T
            "1111" => roadSprites[10], // 十字
            _ => roadSprites[11],       // 無連接預設圖（十字）
        };
    }

    void ClearPreview()
    {
        foreach (var kvp in previewTiles)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }
        previewTiles.Clear();
    }
}