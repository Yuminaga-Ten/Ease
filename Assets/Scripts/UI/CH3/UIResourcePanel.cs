using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIResourcePanel : MonoBehaviour
{
    public static UIResourcePanel Instance;

    public TMP_Text FoodText;// 糧食
    public int food = 0;
    public TMP_Text ResourceText;// 資源
    public int resource = 0;
    public TMP_Text PeopleText;// 人口
    public int people = 100;
    public TMP_Text HeartTxet;// 民心
    public int heart = 50;
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

    public void UpdateResource(int Resources)
    {
        resource = Resources;
        ResourceText.text = $"資源：{Resources}";
    }

    public void UpdateFood(int Food)
    {
        food = Food;
        FoodText.text = $"糧食：{Food}";
    }

    public void UpdatePeople(int People)
    {
        people = People;
        PeopleText.text = $"人口：{People}";
    }
    public void UpdateAllResource(int Resources, int Food, int People, int Heart)
    {
        resource = Resources;
        ResourceText.text = $"資源：{Resources}";
        food = Food;
        FoodText.text = $"糧食：{Food}";
        people = People;
        PeopleText.text = $"人口：{People}";

        UpdateHeart(Heart);
    }

    public void UpdateHeart(int value)
    {
        heart = value;
        PeopleSlider.value = value;
        HeartTxet.text = $"民心：{value}/100";
        PeopleLabel.text = $"{value}/100";
    }

    
}