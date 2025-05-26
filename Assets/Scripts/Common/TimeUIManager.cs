using UnityEngine;
using UnityEngine.UI;

public class TimeUIManager : MonoBehaviour
{
    public Image[] hourIcons; // 在 Inspector 填入 9 格

    [Range(0f, 1f)]
    public float dimmedAlpha = 0.7f;

    void Start()
    {
        TimeManager.Instance.OnHourChanged += UpdateTimeUI;
        Refresh(); // 避免剛載入時錯格
    }

    void UpdateTimeUI(int hour)
    {
        Refresh();
    }

    void Refresh()
    {
        int currentHour = TimeManager.Instance.currentHour;

        for (int i = 0; i < hourIcons.Length; i++)
        {
            Color c = hourIcons[i].color;
            c.a = (i < currentHour) ? dimmedAlpha : 1f; // 已過去的變暗
            hourIcons[i].color = c;
        }
    }
}