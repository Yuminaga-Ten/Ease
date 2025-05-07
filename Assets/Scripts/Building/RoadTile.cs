// RoadTile.cs
// 附加於道路 prefab 上的元件，用於記錄格子座標並控制外觀

using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RoadTile : MonoBehaviour
{
    [Header("格子座標（由 RoadManager 設定）")]
    public Vector2Int gridPos;  // 此 tile 對應的格子位置

    private SpriteRenderer sr;  // 快取的 SpriteRenderer 元件

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 設定此 tile 顯示的圖片
    /// </summary>
    public void SetSprite(Sprite sprite)
    {
        if (sr != null)
            sr.sprite = sprite;
        else
    Debug.LogWarning("SetSprite() 傳入的 sprite 是 null！");
    }

    /// <summary>
    /// 設定此 tile 顯示的顏色（可用來實作預覽透明等效果）
    /// </summary>
    public void SetColor(Color color)
    {
        if (sr != null)
            sr.color = color;
    }
}