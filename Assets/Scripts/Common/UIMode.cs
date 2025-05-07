using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UIMode
{
    public static bool IsMouseDragAllowed = true;         // 可否用滑鼠拖曳相機
    public static bool IsMouseEdgeScrollAllowed = false;  // 是否開啟畫面邊緣捲動
}
/*
可在任何其他cs檔案裡使用 
 進入道路建設模式
UIMode.IsMouseDragAllowed = false;
UIMode.IsMouseEdgeScrollAllowed = true;

 回到預設操作
UIMode.IsMouseDragAllowed = true;
UIMode.IsMouseEdgeScrollAllowed = false;
*/