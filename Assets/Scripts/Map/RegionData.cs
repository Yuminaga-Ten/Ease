using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegionData
{
    public string regionName; // 例如 "C2"等地區名
    public bool isExplored;   // 是否已開墾
    public bool allowConstruction; // 是否可建築
    public Vector2Int gridStartPos; // 區塊最左格子座標(基礎位置)
}