using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    private RoadActive roadActive;

    void Start()
    {
        roadActive = FindObjectOfType<RoadActive>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))//手動推進時辰並告訴我
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.AddHour();
                Debug.Log($"[test] 手動推進時辰：目前是第 {TimeManager.Instance.currentDay} 天，第 {TimeManager.Instance.currentHour} 時辰");
            }
        }

        if (Input.GetKeyDown(KeyCode.T))//告訴我時間
        {
            if (TimeManager.Instance != null)
            {
                Debug.Log($"[test] 手動推進時辰：目前是第 {TimeManager.Instance.currentDay} 天，第 {TimeManager.Instance.currentHour} 時辰");
            }
        }
        if (Input.GetKey(KeyCode.T))//告訴我道路是否啟用
        {
            RoadTile[] allRoads = FindObjectsOfType<RoadTile>();

            foreach (var road in allRoads)
            {
                Vector2Int pos = road.gridPos;
                bool isActive = roadActive != null && roadActive.IsRoadActive(pos);

                // 只更改 alpha 進行視覺區分，避免覆蓋原本顏色
                Color baseColor = isActive ? new Color(0f, 1f, 0f, 0.4f) : new Color(1f, 1f, 0f, 0.4f);
                road.GetComponent<SpriteRenderer>().material.color = baseColor;
            }
        }
        else
        {
            RoadTile[] allRoads = FindObjectsOfType<RoadTile>();

            foreach (var road in allRoads)
            {
                // 恢復為原本材質色（透明白）
                road.GetComponent<SpriteRenderer>().material.color = Color.white;
            }
        }
    }
}
