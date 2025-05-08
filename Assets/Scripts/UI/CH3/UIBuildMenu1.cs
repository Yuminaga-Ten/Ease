using UnityEngine;

/// <summary>
/// 控制建造選單面板的顯示與隱藏動畫與觸發行為
/// </summary>
public class UIBuildMenu1 : MonoBehaviour
{
    [Header("UI 元件參考")]
    public RectTransform panelRect;     // 面板 RectTransform
    public GameObject openButton;       // 開啟用按鈕

    [Header("滑動動畫設定")]
    public Vector2 shownPos = new Vector2(-100f, -50f);   // 顯示位置
    public Vector2 hiddenPos = new Vector2(300f, -50f);   // 隱藏位置
    public float slideSpeed = 10f;                        // 動畫速度

    private bool isOpen = false;

    void Start()
    {
        SetPanelOpen(false); // 一開始關閉
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (isOpen)
            {
                CloseAllAndExitBuildMode();
            }
            else
            {
                SetPanelOpen(true);
            }
        }

        // 動畫滑動
        Vector2 target = isOpen ? shownPos : hiddenPos;
        panelRect.anchoredPosition = Vector2.Lerp(panelRect.anchoredPosition, target, Time.deltaTime * slideSpeed);
    }

    /// <summary>
    /// 切換面板開關狀態，控制動畫與按鈕顯示
    /// </summary>
    public void SetPanelOpen(bool open)
    {
        isOpen = open;
        if (openButton != null)
            openButton.SetActive(!isOpen);
    }

    /// <summary>
    /// 主建築按鈕事件
    /// </summary>
    public void OnClickMainBuilding()
    {
        Debug.Log("主建築建造按鈕被點擊");
    }

    /// <summary>
    /// 道路建造按鈕事件
    /// </summary>
    public void OnClickRoad()
    {
        Debug.Log("道路建造按鈕被點擊");
        FindObjectOfType<RoadManager>().EnterBuildMode();
    }

    /// <summary>
    /// 關閉所有面板並退出建造模式
    /// </summary>
    public void CloseAllAndExitBuildMode()
    {
        SetPanelOpen(false);
        FindObjectOfType<RoadManager>().ExitBuildMode();
        Debug.Log("退出建造模式");
    }
}
