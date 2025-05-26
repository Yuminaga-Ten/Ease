// TimeManager.cs
using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;// 限制這是唯一一個時間系統

    [Header("時間設定")]
    public int currentHour = 0; // 對應自定義的時辰（0~11）
    public int currentDay = 1; // 記天數用
    public int hoursPerDay = 9; // 一天可用的有9時辰

    // 事件：每小時與每日
    public event Action<int> OnHourChanged;
    public event Action<int> OnNewDay;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 推進一個時辰（由外部呼叫）
    public void AddHour()
    {
        currentHour++;

        if (currentHour >= hoursPerDay)//如果到了第9或以上的時辰
        {
            currentHour = 0;//時辰歸零
            currentDay++;//天數+1
            OnNewDay?.Invoke(currentDay);//告訴大家今天是第幾天
        }

        OnHourChanged?.Invoke(currentHour);//告訴大家現在是第幾時
    }

    // 重設（備用）
    public void ResetTime()
    {
        currentHour = 0;
        currentDay = 1;
    }
}
