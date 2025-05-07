// Assets/Editor/SpriteImportProcessor.cs
// 自動套用 Sprites/Roads 圖片的匯入設定

using UnityEngine;
using UnityEditor;
using System.IO;

// 這段程式只在 Unity 編輯器中運作，不會包含進 exe 或 build 中
public class RoadEditor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        // 取得圖片匯入器
        TextureImporter importer = (TextureImporter)assetImporter;

        // 只針對特定路徑下的素材生效
        if (assetPath.Contains("/Sprites/Roads/"))
        {
            importer.textureType = TextureImporterType.Sprite;        // 設為 Sprite 模式
            importer.spritePixelsPerUnit = 256;                       // 每單位像素數

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            importer.filterMode = FilterMode.Bilinear;                // 雙線性過濾（平滑縮放）
            importer.alphaSource = TextureImporterAlphaSource.FromInput; // Alpha 來源為輸入貼圖的 alpha
            importer.alphaIsTransparency = true;                      // 將 alpha 當作透明處理
            importer.textureCompression = TextureImporterCompression.Uncompressed; // 禁止壓縮，避免圖壓壞
            importer.mipmapEnabled = false;                           // 關閉 mipmap
        }
    }

    [MenuItem("Tools/Roads/Reimport All Road Sprites")]
    public static void ReimportAllRoadSprites()
    {
        string folderPath = "Assets/Resources/Sprites/Roads";
        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning("[RoadEditor] 資料夾不存在：" + folderPath);
            return;
        }

        string[] files = Directory.GetFiles(folderPath, "*.png", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            Debug.Log("[RoadEditor] Reimporting: " + file);
            AssetDatabase.ImportAsset(file, ImportAssetOptions.ForceUpdate);
        }

        AssetDatabase.Refresh();
        Debug.Log($"[RoadEditor] 道路圖片重新導入完成，共 {files.Length} 張。");
    }
}
/*
// 自動在編輯器啟動時重新導入一次所有 Road Sprites，但只執行一次
#if UNITY_EDITOR

[InitializeOnLoad]
public static class AutoRoadSpriteReimporter
{
    static AutoRoadSpriteReimporter()
    {
        if (!EditorPrefs.GetBool("RoadSprites_ReimportedOnce", false))
        {
            RoadEditor.ReimportAllRoadSprites();
            EditorPrefs.SetBool("RoadSprites_ReimportedOnce", true);
        }
    }
}
#endif
*/