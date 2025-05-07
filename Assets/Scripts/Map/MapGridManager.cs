using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 定義地圖格子的類型
/// </summary>
public enum TileType {
    Unexplored, // 未開拓格子
    Explored,   // 已開拓格子
    Road        // 道路格子
}

/// <summary>
/// 儲存單格格子資訊的資料類別
/// </summary>
public class TileData {
    public Vector2Int position;   // 格子在地圖中的座標（以格子為單位）
    public TileType tileType;     // 格子的類型
    public bool isActivated;      // 是否已啟用（是否連接至主建築）
}

/// <summary>
/// 管理整個地圖格子生成與點擊轉換的主腳本
/// </summary>
public class MapGridManager : MonoBehaviour
{
    private Dictionary<string, RegionData> regionTable = new Dictionary<string, RegionData>(); // 所有區塊的資料表
    private const int regionSize = 7;          // 每個區塊大小為 7x7 格
    private const int regionCount = 5;         // 總共 5x5 區塊
    private const int mapSize = regionSize * regionCount; // 整張地圖的總邊長為 35 格

    public GameObject unexploredTilePrefab;    // 尚未開拓的格子預製物件
    public GameObject exploredTilePrefab;      // 已開拓的格子預製物件

    private Vector3 centerOffset;              // 用來讓地圖中央對齊畫面中心的偏移值
    private GameObject[,] tileObjects;         // 存放所有 tile 物件的陣列

    private HashSet<string> exploredRegions = new HashSet<string>(); // 記錄哪些區塊是已開拓的

    // 滑鼠懸停與點擊相關
    private Camera mainCamera;
    private Vector2Int? hoveredTile = null;    // 滑鼠當前懸停的格子座標
    public GameObject highlightPrefab;         // 用於顯示滑鼠懸停位置的高亮物件
    private GameObject highlightInstance;      // 實際生成的高亮物件

    void Start()
    {
        exploredRegions.Add("B1");
        exploredRegions.Add("B2");

        Debug.Log("GridToWorld(17, 17) = " + GridToWorld(new Vector2Int(17, 17)));

        mainCamera = Camera.main;
        InitRegions();      // 🔸 初始化所有區塊的基本資料
        GenerateMap();      // 🔸 產生地圖格子
        InitHighlight();    // 🔸 初始化滑鼠高亮顯示器
    }

    /// <summary>
    /// 初始化滑鼠懸停格子的高亮物件
    /// </summary>
    void InitHighlight()
    {
        if (highlightPrefab != null)
        {
            highlightInstance = Instantiate(highlightPrefab);
            highlightInstance.SetActive(false); // 預設隱藏
        }
    }

    /// <summary>
    /// 每幀更新：處理滑鼠懸停格子與點擊事件
    /// </summary>
    void Update()
    {
        UpdateHoveredTile();
    }

    /// <summary>
    /// 根據滑鼠位置更新懸停格子並移動高亮顯示物件
    /// </summary>
    void UpdateHoveredTile()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.forward, Vector3.zero); // z=0 平面

        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 worldPos = ray.GetPoint(enter);

            // 反算出格子座標
            Vector2Int gridPos = WorldToGrid(worldPos);

            if (IsValidGrid(gridPos))
            {
                hoveredTile = gridPos;
                Vector3 highlightPos = GridToWorld(gridPos);
                if (highlightInstance != null)
                {
                    highlightInstance.SetActive(true);
                    highlightInstance.transform.position = highlightPos;
                }
            }
            else
            {
                hoveredTile = null;
                if (highlightInstance != null)
                    highlightInstance.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 檢查格子是否在地圖範圍內
    /// </summary>
    public bool IsValidGrid(Vector2Int grid)
    {
        return grid.x >= 0 && grid.y >= 0 && grid.x < mapSize && grid.y < mapSize;
    }

    /// <summary>
    /// 初始化所有區塊資料
    /// </summary>
    void InitRegions()
    {
        for (int rx = 0; rx < regionCount; rx++)
        {
            for (int ry = 0; ry < regionCount; ry++)
            {
                char col = (char)('A' + rx);
                int row = (regionCount - 1) - ry;
                string name = $"{col}{row}";

                bool isExplored = exploredRegions.Contains(name);
                bool allowConstruction = isExplored;

                RegionData region = new RegionData
                {
                    regionName = name,
                    isExplored = isExplored,
                    allowConstruction = allowConstruction,
                    gridStartPos = new Vector2Int(rx * regionSize, ry * regionSize)
                };

                regionTable.Add(name, region);
            }
        }
    }

    /// <summary>
    /// 生成整張地圖的格子，並依照探索狀態選用不同的預製物件
    /// </summary>
    void GenerateMap()
    {
        // 將格子(17,17)對齊世界座標(0,0)，作為視覺中心
        centerOffset = new Vector3(17 * 0.5f + 17 * 0.5f, -17 * 0.25f + 17 * 0.25f, 0);

        tileObjects = new GameObject[mapSize, mapSize];

        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                string regionName = GetRegionName(x, y);
                bool isExplored = exploredRegions.Contains(regionName);

                Vector3 localPos = new Vector3(x * 0.5f + y * 0.5f, -x * 0.25f + y * 0.25f, 0);
                Vector3 pos = localPos - centerOffset;

                GameObject prefab = isExplored ? exploredTilePrefab : unexploredTilePrefab;
                GameObject tile = Instantiate(prefab, pos, Quaternion.identity, transform);
                tile.name = $"Tile_{x}_{y}_({regionName})";

                tileObjects[x, y] = tile;
//Debug用，加入小球標記於 tile 中心位置
var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
marker.transform.position = pos;
marker.transform.localScale = Vector3.one * 0.05f;
marker.GetComponent<Renderer>().material.color = Color.red;
marker.name = "DebugMarker";

            }
        }
    }

    /// <summary>
    /// 取得某區塊是否可建造
    /// </summary>
    public bool IsRegionBuildable(string regionName)
    {
        if (regionTable.ContainsKey(regionName))
        {
            return regionTable[regionName].allowConstruction;
        }
        return false;
    }

    /// <summary>
    /// 將格子座標轉換成世界座標，用於建置位置對齊
    /// </summary>
    public Vector3 GridToWorld(Vector2Int grid)
    {
        int x = grid.x;
        int y = grid.y;
        Vector3 localPos = new Vector3(x * 0.5f + y * 0.5f, -x * 0.25f + y * 0.25f, 0);
        return localPos - centerOffset;
    }

    /// <summary>
    /// 將世界座標轉換成格子座標，用於滑鼠點擊位置定位，並限制邊界不溢出
    /// 滑鼠只要進入格子可見區域（左下角開始），就算選中該格。
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 localPos = worldPos + centerOffset;

    // 先推估最接近的格子中心
    float gx = (localPos.x / 0.5f - localPos.y / 0.25f) / 2f;
    float gy = (localPos.x / 0.5f + localPos.y / 0.25f) / 2f;

    int guessX = Mathf.RoundToInt(gx);
    int guessY = Mathf.RoundToInt(gy);

    // 把滑鼠位置轉成「以該格中心為基準」的相對位置
    Vector3 center = GridToWorld(new Vector2Int(guessX, guessY));
    Vector3 delta = worldPos - center;

    // 菱形範圍判定，範圍為：|x| * 2 + |y| * 4 < 1，調整係數視圖塊大小而定
    float dx = Mathf.Abs(delta.x / 0.5f);  // tile 寬度
    float dy = Mathf.Abs(delta.y / 0.25f); // tile 高度

    if (dx + dy <= 1.0f)
    {
        return new Vector2Int(guessX, guessY);
    }

    // 不在這格，就要判斷落在哪個鄰近格子（使用方向修正）
    if (delta.x > 0 && delta.y > 0) return new Vector2Int(guessX + 1, guessY + 1);
    if (delta.x > 0 && delta.y < 0) return new Vector2Int(guessX + 1, guessY - 1);
    if (delta.x < 0 && delta.y > 0) return new Vector2Int(guessX - 1, guessY + 1);
    return new Vector2Int(guessX - 1, guessY - 1);
    }

    /// <summary>
    /// 根據格子座標取得對應的區塊代號（如 B1）
    /// </summary>
    public string GetRegionName(int x, int y)
    {
        int rx = x / regionSize;
        int ry = y / regionSize;
        char col = (char)('A' + rx);
        int row = (regionCount - 1) - ry;
        return $"{col}{row}";
    }
}
