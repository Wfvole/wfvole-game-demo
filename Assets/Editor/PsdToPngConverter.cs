using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 将 PSD 转成 PNG（使用 Unity 原生 PSD 导入能力，无需外部工具）。
/// 用法1（命令行批处理）：Unity.exe -batchmode -projectPath "项目路径" -executeMethod PsdToPngConverter.ConvertAll -quit
/// 用法2（编辑器菜单）：Tools -> PSD 转 PNG
/// </summary>
public static class PsdToPngConverter
{
    [MenuItem("Tools/PSD 转 PNG")]
    public static void ConvertAll()
    {
        string[] psdPaths =
        {
            "Assets/terrain/_DynamicClouds/ClearSky.psd"
        };

        foreach (string psdPath in psdPaths)
            Convert(psdPath);

        AssetDatabase.Refresh();
        Debug.Log("PSD 转 PNG 全部完成");
    }

    public static void Convert(string psdPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(psdPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("无法获取纹理导入器: " + psdPath);
            return;
        }

        // 临时改为可读，才能用 EncodeToPNG 导出
        bool prevReadable = importer.isReadable;
        TextureImporterType prevType = importer.textureType;
        importer.isReadable = true;
        importer.textureType = TextureImporterType.Default;
        importer.SaveAndReimport();

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(psdPath);
        if (tex == null)
        {
            Debug.LogError("无法加载纹理: " + psdPath);
            return;
        }

        string outPath = Path.ChangeExtension(psdPath, ".png");
        byte[] pngBytes = tex.EncodeToPNG();
        File.WriteAllBytes(outPath, pngBytes);
        Debug.Log("已导出 " + outPath
            + "  尺寸=" + tex.width + "x" + tex.height
            + "  大小=" + (pngBytes.Length / 1024f / 1024f).ToString("F1") + " MB");

        // 恢复原始导入设置
        importer.isReadable = prevReadable;
        importer.textureType = prevType;
        importer.SaveAndReimport();
    }
}
