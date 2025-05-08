using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public UIResourcePanel resourcePanel;   // 資源面板控制器
    public GameObject buildMenuPanel;       // 建造選單面板
    public TMP_Text modeHintText;           // 模式提示文字

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // 更新資源數值面板
    public void UpdateResourceUI(int resource, int food, int people, int heart)
    {
        resourcePanel.UpdateResources(resource, food, people, heart);
    }

    // 顯示／隱藏建造選單
    public void ToggleBuildMenu(bool show)
    {
        buildMenuPanel.SetActive(show);
    }

    // 設定模式提示文字
    public void SetModeHint(string message)
    {
        modeHintText.text = message;
    }
}
