using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIResourcePanel : MonoBehaviour
{
    public static UIResourcePanel Instance;

    public TMP_Text FoodText;// 糧食
    public TMP_Text ResourceText;// 資源
    public TMP_Text PeopleText;// 人口
    public TMP_Text HeartTxet;// 民心
    public TMP_Text PeopleLabel;// 民心可視化值
    public Slider PeopleSlider;// 民心滑塊

    void Awake()
    {
        // 單例模式，方便跨腳本呼叫
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void UpdateResources(int Resources, int Food, int People, int Heart)
    {
        ResourceText.text = $"資源：{Resources}";
        FoodText.text = $"糧食：{Food}";
        PeopleText.text = $"人口：{People}";
        UpdateHeart(Heart);
    }

    public void UpdateHeart(int value)
    {
        PeopleSlider.value = value;
        HeartTxet.text = $"民心：{value}/100";
        PeopleLabel.text = $"{value}/100";
    }

    
}