using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainBuildingManager : MonoBehaviour
{
    [Header("格子預覽框")]
    public GameObject gridTilePrefab;
    private List<GameObject> previewTiles = new();
    [Header("主建築預置物")]
    public GameObject mainBuildingPrefab;  // 主建築的預置物，用於生成建築物實例
    [Header("建築物父物件")]
    public Transform buildingParent;       // 建築物的父物件，用於組織場景中的建築物

    [Header("建造花費（目前為 0，以後可調整）")]
    public int costFood = 0;               // 建造主建築所需的食物資源
    public int costWood = 0;               // 建造主建築所需的木材資源

    private bool hasBuilt = false;         // 是否已經建造完成主建築
    private bool isPlacing = false;        // 是否處於建造模式中（正在放置建築）
    private GameObject previewInstance;    // 建築預覽物件，用於顯示建築放置位置
    private Vector2Int previewOrigin;      // 預覽建築的格子起點座標

    private int size = 5;                  // 主建築佔用的格子大小（5x5）

    private MapGridManager mapGrid;

    private bool isMoving = false;

    private Vector2Int originalMoveOrigin;

    private Vector3 originalMovePosition;

    private RoadActive roadActive; // 用於看道路是否啟用

    void Start()
    {
        mapGrid = FindObjectOfType<MapGridManager>();
        roadActive = FindObjectOfType<RoadActive>();
        // 動態生成 25 個格子框線
        for (int i = 0; i < size * size; i++)
        {
            GameObject tile = new GameObject("PreviewTile");
            SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("格子");
            sr.sortingOrder = 100;
            sr.color = Color.clear;
            tile.transform.SetParent(transform);
            previewTiles.Add(tile);
        }
    }

    void Update()
    {
        // 玩家按下 Escape 鍵 → 取消建造或移動模式
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPlacing)
            {
                isPlacing = false;
                if (previewInstance != null)
                {
                    Destroy(previewInstance);
                    previewInstance = null;
                }
                foreach (var tile in previewTiles)
                    tile.SetActive(false);
                Debug.Log("取消主建築建造模式");
            }
            else if (isMoving)
            {
                isMoving = false;

                // 🔁 清除當前預覽位置的格子標記（可能尚未確認建造但移動過）
                for (int x = 0; x < size; x++)
                {
                    for (int y = 0; y < size; y++)
                    {
                        mapGrid.UnmarkOccupied(previewOrigin + new Vector2Int(x, y));
                    }
                }

                // ✅ 恢復建築位置與標記為原始起點
                previewInstance.transform.position = originalMovePosition;
                MarkOccupied(originalMoveOrigin);
                previewOrigin = originalMoveOrigin;

                foreach (var tile in previewTiles)
                    tile.SetActive(false);
                Debug.Log("取消主建築移動模式");
            }
        }

        // 若未處於建造模式或已建造完成，則不執行更新
        if (!isPlacing || hasBuilt)
        {
            if (isMoving && previewInstance != null)
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = 0f;

                Vector2Int gridPos = mapGrid.WorldToGrid(mouseWorldPos);
                Vector3 snappedPos = mapGrid.GridToWorld(gridPos);
                previewOrigin = gridPos - new Vector2Int(size / 2, size / 2);
                previewInstance.transform.position = snappedPos;

                // 預覽格更新
                bool canMove = CanPlaceAt(previewOrigin);
                int index = 0;
                foreach (var tile in previewTiles)
                {
                    tile.SetActive(true);
                    Vector2Int pos = previewOrigin + new Vector2Int(index % size, index / size);
                    tile.transform.position = mapGrid.GridToWorld(pos);
                    SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                    sr.color = canMove ? new Color(0f, 1f, 0f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);
                    index++;
                }

                if (Input.GetMouseButtonDown(0) && canMove)
                {
                    ConfirmMove(previewOrigin);
                }
            }
            return;
        }

        // 建造模式下：避免命名衝突，變數名稱改為 mouseWorldPos2 等
        Vector3 mouseWorldPos2 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos2.z = 0f;

        Vector2Int gridPos2 = mapGrid.WorldToGrid(mouseWorldPos2);
        Vector3 snappedPos2 = mapGrid.GridToWorld(gridPos2);

        previewOrigin = gridPos2 - new Vector2Int(size / 2, size / 2);

        if (previewInstance != null)
        {
            previewInstance.transform.position = snappedPos2;
        }
        else
        {
            previewInstance = Instantiate(mainBuildingPrefab, snappedPos2, Quaternion.identity, buildingParent);
            SetPreviewStyle(previewInstance);
        }

        // 預覽格子框線顯示與顏色
        bool canBuild = CanPlaceAt(previewOrigin);
        int index2 = 0;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2Int pos = previewOrigin + new Vector2Int(x, y);
                GameObject tile = previewTiles[index2++];
                tile.SetActive(true);
                tile.transform.position = mapGrid.GridToWorld(pos);
                SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                sr.color = canBuild ? new Color(0f, 1f, 0f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);
            }
        }

        // 當玩家按下滑鼠左鍵，且該位置可放置建築，則確認建造
        if (Input.GetMouseButtonDown(0) && CanPlaceAt(previewOrigin))
        {
            ConfirmPlacement(previewOrigin);
        }
    }

    // 進入建造模式，允許玩家放置主建築
    public void EnterBuildMode()
    {
        if (hasBuilt) return;  // 若已建造完成，則不允許再次進入建造模式
        isPlacing = true;
    }

    public void EnterMoveMode()
    {
        if (!hasBuilt)
            return;

        if (previewInstance == null)
        {
            GameObject found = GameObject.FindWithTag("MainBuilding");
            if (found != null)
                previewInstance = found;
        }

        if (previewInstance != null)
        {
            // 清除舊位置的標記
            Vector2Int currentGrid = mapGrid.WorldToGrid(previewInstance.transform.position);
            Vector2Int currentOrigin = currentGrid - new Vector2Int(size / 2, size / 2);
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    mapGrid.UnmarkOccupied(currentOrigin + new Vector2Int(x, y));
                }
            }
            originalMoveOrigin = currentOrigin;
            originalMovePosition = previewInstance.transform.position;
            isMoving = true;
        }
    }

    void ConfirmMove(Vector2Int origin)
    {
        isMoving = false;

        // 正確：清除移動前的格子標記（使用原位置）
        Vector2Int oldOrigin = previewOrigin;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                mapGrid.UnmarkOccupied(oldOrigin + new Vector2Int(x, y));

        // 設定新位置與新標記
        previewOrigin = origin;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;
        Vector2Int gridPos = mapGrid.WorldToGrid(mouseWorldPos);
        Vector3 snappedPos = mapGrid.GridToWorld(gridPos);
        previewInstance.transform.position = snappedPos;

        MarkOccupied(origin);

        if (roadActive != null)
            roadActive.RecalculateFromMainBuilding(origin, size);

        // 清除預覽格
        foreach (var tile in previewTiles)
            tile.SetActive(false);

        Debug.Log("主建築已移動");
        
    }

    // 確認建築放置，完成建造流程
    void ConfirmPlacement(Vector2Int origin)
    {
        isPlacing = false;
        hasBuilt = true;

        // 將預覽物件改為正式建築樣式，並清除預覽物件參考
        if (previewInstance != null)
        {
            SetFinalStyle(previewInstance);
            previewInstance = null;
        }
        // 標記該區域格子為已佔用
        MarkOccupied(origin);
        Debug.Log("主建築已建造");
        // 將所有預覽格隱藏
        foreach (var tile in previewTiles)
            tile.SetActive(false);

        if (roadActive != null)
            roadActive.RecalculateFromMainBuilding(origin, size);

        // 即時刷新 UI 狀態
        foreach (var menu in FindObjectsOfType<UIBuildMenu2>())
        {
            if (menu.currentCategory == "主建築")
            {
                menu.RefreshMainBuildingButtons();
                break;
            }
        }
    }

    // 檢查指定起點位置是否可以放置主建築（不與已佔用格子衝突）
    bool CanPlaceAt(Vector2Int origin)
    {
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2Int check = origin + new Vector2Int(x, y);
                if (mapGrid.IsOccupied(check)) return false;
                if (!mapGrid.IsRegionBuildable(mapGrid.GetRegionName(check.x, check.y))) return false;
            }
        }
        return true;
    }

    // 將指定起點位置的區域格子標記為已佔用，並同步標記 mapGrid
    void MarkOccupied(Vector2Int origin)
    {
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2Int pos = origin + new Vector2Int(x, y);
                mapGrid.MarkOccupied(pos, "MainBuilding");
            }
        }
    }

    // 設定建築預覽物件的樣式（半透明）
    void SetPreviewStyle(GameObject go)
    {
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(1f, 1f, 1f, 0.5f);
        }
    }

    // 設定正式建築物件的樣式（不透明）
    void SetFinalStyle(GameObject go)
    {
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.white;
        }
        go.tag = "MainBuilding";
    }

    // 供 UI 查詢是否已建造主建築（用來隱藏建造按鈕）
    public bool HasBuilt()
    {
        return hasBuilt;
    }


    // 手動關閉建造與移動模式（外部可調用）
    public void ExitAllModes()
    {
        if (isPlacing)
        {
            isPlacing = false;
            if (previewInstance != null)
            {
                Destroy(previewInstance);
                previewInstance = null;
            }
            foreach (var tile in previewTiles)
                tile.SetActive(false);
            Debug.Log("主建築建造模式已手動關閉");
        }
        else if (isMoving)
        {
            isMoving = false;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    mapGrid.UnmarkOccupied(previewOrigin + new Vector2Int(x, y));
                }
            }

            previewInstance.transform.position = originalMovePosition;
            previewOrigin = originalMoveOrigin;
            MarkOccupied(originalMoveOrigin);

            foreach (var tile in previewTiles)
                tile.SetActive(false);

            if (roadActive != null)
                roadActive.RecalculateFromMainBuilding(previewOrigin, size);

            Debug.Log("主建築移動模式已手動關閉");
        }
    }
}