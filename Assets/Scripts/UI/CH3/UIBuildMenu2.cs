using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBuildMenu2 : MonoBehaviour
{
    public GameObject panel;

    public string currentCategory;

    public RectTransform panelRect;
    public Vector2 shownPos = new Vector2(-125f, 150f); // 路徑下方顯示
    public Vector2 hiddenPos = new Vector2(125f, 150f); // 畫面外右下
    public float slideSpeed = 10f;

    private bool isOpen = false;

    public Button buildButton;
    public Button moveButton;

    void Update()
    {
        if (panelRect != null)
        {
            Vector2 target = isOpen ? shownPos : hiddenPos;
            panelRect.anchoredPosition = Vector2.Lerp(panelRect.anchoredPosition, target, Time.deltaTime * slideSpeed);
        }
    }

    public void OpenPanel(string category)
    {
        if (panel == null)
            panel = this.gameObject;

        panel.SetActive(true);
        isOpen = true;
        currentCategory = category;
        Debug.Log("開啟第二層選單：" + category);
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (currentCategory == "主建築")
        {
            RefreshMainBuildingButtons();
        }
    }

    public void RefreshMainBuildingButtons()
    {
        var manager = FindObjectOfType<MainBuildingManager>();
        if (manager != null)
        {
            bool hasBuilt = manager.HasBuilt();
            if (buildButton != null) buildButton.gameObject.SetActive(!hasBuilt);
            if (moveButton != null) moveButton.gameObject.SetActive(hasBuilt);
        }
    }

    public void OnClickBuild()
    {
        Debug.Log($"建造：{currentCategory}");
        if (currentCategory == "道路")
            {FindObjectOfType<RoadManager>().EnterBuildMode(RoadBuildMode.Build);}
        else if(currentCategory == "主建築")
            {FindObjectOfType<MainBuildingManager>()?.EnterBuildMode(); }
    }

    public void OnClickDelete()
    {
        Debug.Log($"刪除：{currentCategory}");
        if (currentCategory == "道路")
            FindObjectOfType<RoadManager>().EnterBuildMode(RoadBuildMode.Delete);
    }

    public void OnClickMoveMainBuilding()
    {
        Debug.Log($"移動：{currentCategory}");
    var manager = FindObjectOfType<MainBuildingManager>();
    if (manager != null)
    {
        manager.EnterMoveMode();
    }
    }

    public void ClosePanel()
    {
        isOpen = false;
        FindObjectOfType<RoadManager>()?.ExitBuildMode();
        FindObjectOfType<MainBuildingManager>()?.ExitAllModes(); // 新增主建築退出方法
    }

    public void OnClickConfirmBuild()
    {
        var roadManager = FindObjectOfType<RoadManager>();
        if (roadManager != null)
        {
            roadManager.ConfirmPreviewExternally(); // 觸發確認建造
        }
    }
}