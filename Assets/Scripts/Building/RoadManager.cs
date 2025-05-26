// RoadManager.cs（完整重構：新增無連接預設圖、全自動轉角更新）

using System.Collections.Generic;
using UnityEngine;

public enum RoadBuildMode
{
    Build,
    Delete
}

public class RoadManager : MonoBehaviour
{
    [Header("道路預製物件與樣式")]
    public GameObject roadTilePrefab; // 道路格子預製物件，用於生成道路
    // 將自 Resources/Sprites/Road 中載入（共 12 張）
    private Sprite[] roadSprites = new Sprite[12]; // 儲存道路各種樣式的圖片資源
    public Color previewColor = new Color(1, 1, 1, 0.4f); // 預覽道路顏色（半透明白）
    public Color deletePreviewColor = new Color(1f, 0.3f, 0.3f, 0.4f); // 預覽刪除時顏色（半透明紅）
    public Color deleteFinalColor = new Color(1f, 0.3f, 0.3f, 1f); // 確認刪除時顏色（不透明紅）

    private Dictionary<Vector2Int, RoadTile> builtRoads = new(); // 已建造的道路格子，key為格子座標
    private Dictionary<Vector2Int, RoadTile> previewTiles = new(); // 預覽中（尚未確認建造）的道路格子
    private HashSet<Vector2Int> markedForDelete = new(); // 標記為刪除的已建造道路格子
    private HashSet<Vector2Int> pendingEraseTiles = new(); // 預覽中標記為刪除的格子

    private Vector2Int? dragStart = null; // 滑鼠拖曳起點格子座標（null表示未拖曳）
    private Vector2Int? lastHoverPos = null; // 滑鼠目前懸停的格子座標（用於避免重複處理）
    private bool isPlacing = false; // 是否處於建造模式
    private bool isDeleteMode = false; // 是否處於刪除模式（根據拖曳起點判斷）

    private bool isConfirming = false; // 是否正在處理確認動作（避免重複觸發）
    private float modeToggleCooldown = 0f; // 切換模式的冷卻時間，避免連點切換過快

    private Camera cam; // 主相機
    private MapGridManager grid; // 地圖格子管理器，用於座標轉換與判斷建造區域

    private RoadBuildMode currentMode = RoadBuildMode.Build;

    private bool isDraggingBuild = false;

    private bool isDraggingDeletePreviewOnly = false;

    private List<GameObject> gridPreviews = new();
    private Sprite gridPreviewSprite;

    private RoadActive roadActive;

    /// <summary>
    /// 載入所有道路圖片資源，並依名稱對應到陣列索引
    /// </summary>
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

    /// <summary>
    /// 初始化，取得相機與地圖管理元件，並載入圖片資源
    /// </summary>
    void Start()
    {
        cam = Camera.main;
        grid = FindObjectOfType<MapGridManager>();
        LoadAllRoadSprites();
        gridPreviewSprite = Resources.Load<Sprite>("格子");
        roadActive = FindObjectOfType<RoadActive>();
    }

    /// <summary>
    /// 每幀更新，處理模式切換與建造輸入，只測試腳本時用
    /// </summary>
    void Update()
    {
        modeToggleCooldown -= Time.deltaTime;

        if (!isPlacing) return; // 非建造模式不處理建造輸入
        HandleBuildInput();
    }

    /// <summary>
    /// 切換建造模式，並重置相關狀態與UI互動設定
    /// </summary>
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

    /// <summary>
    /// 處理建造相關輸入，包括拖曳建造與刪除、確認建造
    /// </summary>
    void HandleBuildInput()
    {
        if (isConfirming) return; // 正在確認中不處理輸入

        if (Input.GetMouseButtonDown(0))
        {
            dragStart = GetGridPosUnderMouse();
            lastHoverPos = null;
            isDeleteMode = (currentMode == RoadBuildMode.Delete);

            if (currentMode == RoadBuildMode.Build && dragStart != null)
            {
                isDraggingBuild = !previewTiles.ContainsKey(dragStart.Value);
                isDraggingDeletePreviewOnly = previewTiles.ContainsKey(dragStart.Value);
            }
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
            isDraggingBuild = false;
            isDraggingDeletePreviewOnly = false;
            ProcessPendingErase();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            isConfirming = true;
            ConfirmPreview();
            isConfirming = false;
        }
    }

    // 未使用的方法，保留作為潛在工具
    /*
    /// <summary>
    /// 判斷該格是否處於刪除模式（已有預覽或建造道路）
    /// </summary>
    /// <param name="pos">格子座標</param>
    /// <returns>是否刪除模式</returns>
    bool DetermineDeleteMode(Vector2Int pos)
    {
        return previewTiles.ContainsKey(pos) || builtRoads.ContainsKey(pos);
    }
    */

    /// <summary>
    /// 根據目前模式，繪製預覽格子提示（紅白），再處理道路預覽或刪除標記
    /// </summary>
    /// <param name="pos">格子座標</param>
    void DrawPreviewPoint(Vector2Int pos)
    {
        // === 預覽格子提示：根據模式顯示紅白色 ===
        // 無論能否建造都會顯示
        if (gridPreviewSprite != null)
        {
            GameObject g = new GameObject("GridPreview");
            g.transform.position = grid.GridToWorld(pos);
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = gridPreviewSprite;
            sr.sortingOrder = 99;
            if (isDeleteMode)
            {
                // 刪除模式下：已建道路白色，否則紅色
                sr.color = builtRoads.ContainsKey(pos) ? new Color(1f, 1f, 1f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);
            }
            else
            {
                bool occupied = grid.IsOccupied(pos);
                bool buildable = grid.IsRegionBuildable(grid.GetRegionName(pos.x, pos.y));
                // 不可建造（紅色） / 可建造（白色）
                sr.color = (!buildable || occupied) ? new Color(1f, 0f, 0f, 0.4f) : new Color(1f, 1f, 1f, 0.4f);
            }
            gridPreviews.Add(g);
        }

        // === 刪除模式：處理預覽或已建造道路的刪除標記 ===
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
            return;
        }

        // === 建造模式：處理預覽道路生成 ===
        if (previewTiles.ContainsKey(pos))
        {
            if (!isDraggingBuild)
            {
                previewTiles[pos].SetColor(deletePreviewColor);
                pendingEraseTiles.Add(pos);
            }
            return;
        }
        if (builtRoads.ContainsKey(pos)) return;
        if (grid.IsOccupied(pos)) return;
        if (!grid.IsRegionBuildable(grid.GetRegionName(pos.x, pos.y))) return;
        if (!isDraggingBuild || isDraggingDeletePreviewOnly) return;

        // ✅ 可建造：產生預覽道路
        Vector3 worldPos = grid.GridToWorld(pos);
        GameObject go = Instantiate(roadTilePrefab, worldPos, Quaternion.identity, transform);
        RoadTile rt = go.GetComponent<RoadTile>();
        rt.gridPos = pos;
        rt.SetColor(previewColor);
        previewTiles[pos] = rt;

        UpdateSurrounding(pos); // 🔄 建造時立刻更新樣式
    }

    /// <summary>
    /// 處理拖曳過程中標記刪除的預覽與已建造道路，並更新周圍樣式
    /// </summary>
    void ProcessPendingErase()
    {
        foreach (Vector2Int pos in pendingEraseTiles)
        {
            if (previewTiles.ContainsKey(pos))
            {
                Destroy(previewTiles[pos].gameObject);
                previewTiles.Remove(pos);
                if(grid != null){grid.UnmarkOccupied(pos);} // 解除標記
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
                grid.UnmarkOccupied(pos); // 解除標記
                UpdateSurrounding(pos);
            }
        }
        markedForDelete.Clear();

        foreach (var go in gridPreviews)
            Destroy(go);
        gridPreviews.Clear();

        // 新增：重新計算道路啟用狀態
        if (roadActive != null)
        {
            Vector2Int origin = FindMainBuildingOrigin();
            if (origin != Vector2Int.zero)
                roadActive.RecalculateFromMainBuilding(origin, 5);
        }
    }

    /// <summary>
    /// 確認建造，將預覽道路轉為正式建造，並優先處理刪除標記
    /// </summary>
    void ConfirmPreview()
    {
        // 🧹 先刪除拖曳期間標記為「要刪除」的預覽道路
        foreach (Vector2Int pos in pendingEraseTiles)
        {
            if (previewTiles.ContainsKey(pos))
            {
                Destroy(previewTiles[pos].gameObject);
                previewTiles.Remove(pos);
                UpdateSurrounding(pos);
            }
        }

        // 🧹 刪除尚未釋放滑鼠時標記為刪除的已建成道路
        foreach (Vector2Int pos in markedForDelete)
        {
            if (builtRoads.ContainsKey(pos))
            {
                Destroy(builtRoads[pos].gameObject);
                builtRoads.Remove(pos);
                UpdateSurrounding(pos);
            }
        }

        // ✅ 將剩下的預覽轉為正式建造（排除已標記要刪除的格子）
        foreach (var kvp in previewTiles)
        {
            Vector2Int pos = kvp.Key;
            if (markedForDelete.Contains(pos)) continue; // 優先刪除，不建造

            RoadTile tile = kvp.Value;
            tile.SetColor(Color.white);
            builtRoads[pos] = tile;

            // 新增佔用標記
            grid.MarkOccupied(pos, "Road");
        }

        // 🔄 更新周圍圖示樣式
        foreach (var kvp in previewTiles)
        {
            if (markedForDelete.Contains(kvp.Key)) continue;
            UpdateSurrounding(kvp.Key);
        }

        // 🧹 清理狀態
        previewTiles.Clear();
        pendingEraseTiles.Clear();
        markedForDelete.Clear();
        lastHoverPos = null;

        foreach (var go in gridPreviews)
            Destroy(go);
        gridPreviews.Clear();

        if (roadActive != null)
        {
            Vector2Int origin = FindMainBuildingOrigin();
            if (origin != Vector2Int.zero)
                roadActive.RecalculateFromMainBuilding(origin, 5);
        }

        ExitBuildMode(); // ✅ 建造完成後自動離開建造模式
    }

    /// <summary>
    /// 取得滑鼠目前所在的格子座標
    /// </summary>
    /// <returns>格子座標</returns>
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

    /// <summary>
    /// 更新指定格子及其上下左右鄰居的道路圖示樣式
    /// </summary>
    /// <param name="center">中心格子座標</param>
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

    /// <summary>
    /// 根據上下左右是否有道路的布林值，回傳對應的道路圖片
    /// </summary>
    /// <param name="u">上方是否有道路</param>
    /// <param name="d">下方是否有道路</param>
    /// <param name="l">左方是否有道路</param>
    /// <param name="r">右方是否有道路</param>
    /// <returns>對應的道路圖片</returns>
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

    /// <summary>
    /// 清除所有預覽道路物件，釋放資源並清空預覽字典，並更新周圍道路樣式
    /// </summary>
    void ClearPreview()
    {
        // 先刪除所有預覽格子，並更新周圍格子的樣式
        foreach (var kvp in previewTiles)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value.gameObject);
                UpdateSurrounding(kvp.Key);
            }
        }
        previewTiles.Clear();

        // 再次確認所有已建成道路的樣式正確
        foreach (var pos in builtRoads.Keys)
        {
            UpdateSurrounding(pos);
        }
    }

    public void EnterBuildMode(RoadBuildMode mode)
    {
        currentMode = mode;
        isPlacing = true;
        dragStart = null;
        lastHoverPos = null;
        pendingEraseTiles.Clear();
        markedForDelete.Clear();
        ClearPreview();

        UIMode.IsMouseDragAllowed = false;
        UIMode.IsMouseEdgeScrollAllowed = true;
    }

    public void ExitBuildMode()
    {
        isPlacing = false;
        dragStart = null;
        lastHoverPos = null;

        ProcessPendingErase(); // 先處理所有標記刪除的預覽與建成道路
        pendingEraseTiles.Clear();
        markedForDelete.Clear();
        ClearPreview();

        UIMode.IsMouseDragAllowed = true;
        UIMode.IsMouseEdgeScrollAllowed = false;

        StartCoroutine(DelayedResetCamera());
    }

    /// <summary>
    /// 供外部（如UI按鈕）呼叫的確認建造方法，具保護避免重複執行
    /// </summary>
    public void ConfirmPreviewExternally()
    {
        if (!isPlacing) return;
        if (isConfirming) return;

        isConfirming = true;
        ConfirmPreview();
        isConfirming = false;
    }

    private System.Collections.IEnumerator DelayedResetCamera()
    {
        yield return null;
        CameraController camCtrl = FindObjectOfType<CameraController>();
        if (camCtrl != null)
        {
            camCtrl.ResetCameraPosition();
        }
    }

    private Vector2Int FindMainBuildingOrigin()
    {
        foreach (var kvp in grid.GetAllOccupiedTiles())
        {
            if (grid.GetBuildingType(kvp.Key) == "MainBuilding")
            {
                return kvp.Key; // 回傳第一個找到的主建築格（假設從左下角建起）
            }
        }
        return Vector2Int.zero;
    }
}