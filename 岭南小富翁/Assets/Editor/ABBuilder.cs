using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ABBuilder
{
    [Serializable]
    public class BundleGroup
    {
        public string bundleName;
        public List<string> assetPaths = new List<string>();
    }

    [Serializable]
    public class BundleConfig
    {
        public List<BundleGroup> groups = new List<BundleGroup>();
    }

    private const string OutputRoot = "Assets/StreamingAssets/AssetBundles";

    [MenuItem("Tools/AssetBundle/1. 自动设置AB包标签")]
    public static void AutoAssignBundleTags()
    {
        int originalCount = 0;
        int changedCount = 0;

        Dictionary<string, string> pathToBundle = new Dictionary<string, string>();

        AddArtAssets(ref pathToBundle, "Assets/Art/UI", "ui_art");
        AddArtAssets(ref pathToBundle, "Assets/Art/弹窗", "ui_art");
        AddArtAssets(ref pathToBundle, "Assets/Art/资产", "ui_art");
        AddArtAssets(ref pathToBundle, "Assets/Art/按键总资产", "ui_art");
        AddArtAssets(ref pathToBundle, "Assets/Art/骰子", "ui_art");
        AddArtAssets(ref pathToBundle, "Assets/Icon", "ui_art");

        AddArtAssets(ref pathToBundle, "Assets/Art/建筑", "buildings_art");
        AddArtAssets(ref pathToBundle, "Assets/Model", "buildings_art");
        AddArtAssets(ref pathToBundle, "Assets/Art/棋子01.fbx", "buildings_art");
        AddArtAssets(ref pathToBundle, "Assets/Art/玉佩.fbx", "buildings_art");

        AddArtAssets(ref pathToBundle, "Assets/Art/地形", "terrain_art");
        AddArtAssets(ref pathToBundle, "Assets/Material", "terrain_art");

        AddArtAssets(ref pathToBundle, "Assets/Art/rain", "effects_art");
        AddArtAssets(ref pathToBundle, "Assets/Shader", "effects_art");

        AddArtAssets(ref pathToBundle, "Assets/Music", "audio_art");

        AddArtAssets(ref pathToBundle, "Assets/Data/BoardData/BuidingPrefabs", "buildings_prefabs");
        AddArtAssets(ref pathToBundle, "Assets/Prefabs", "buildings_prefabs");

        AddDataAssets(ref pathToBundle, "Assets/Data", "config_data");

        foreach (var kv in pathToBundle)
        {
            string assetPath = kv.Key;
            string targetBundle = kv.Value;

            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null) continue;

            originalCount++;
            string currentBundle = importer.assetBundleName;
            string targetLower = targetBundle.ToLower();

            if (!string.Equals(currentBundle, targetLower, StringComparison.Ordinal))
            {
                importer.assetBundleName = targetLower;
                importer.assetBundleVariant = string.Empty;
                importer.SaveAndReimport();
                changedCount++;
                Debug.Log($"[ABBuilder] 设置标签: {assetPath} -> {targetLower}");
            }
        }

        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.Refresh();

        Debug.Log($"[ABBuilder] 标签设置完成！共扫描 {originalCount} 个资源，更新 {changedCount} 个标签");
        EditorUtility.DisplayDialog("AB包标签设置", $"扫描资源 {originalCount} 个\n更新标签 {changedCount} 个", "确定");
    }

    [MenuItem("Tools/AssetBundle/2. 构建AB包 (Windows)")]
    public static void BuildAssetBundles_Windows()
    {
        BuildForTarget(BuildTarget.StandaloneWindows64);
    }

    [MenuItem("Tools/AssetBundle/3. 构建AB包 (Android)")]
    public static void BuildAssetBundles_Android()
    {
        BuildForTarget(BuildTarget.Android);
    }

    [MenuItem("Tools/AssetBundle/4. 构建AB包 (iOS)")]
    public static void BuildAssetBundles_iOS()
    {
        BuildForTarget(BuildTarget.iOS);
    }

    [MenuItem("Tools/AssetBundle/5. 一键：设置标签 + 构建 (Windows)")]
    public static void OneClickBuild_Windows()
    {
        AutoAssignBundleTags();
        EditorApplication.delayCall += () =>
        {
            BuildForTarget(BuildTarget.StandaloneWindows64);
        };
    }

    [MenuItem("Tools/AssetBundle/6. 清除所有AB包标签")]
    public static void ClearAllBundleTags()
    {
        int cleared = 0;
        string[] allBundleNames = AssetDatabase.GetAllAssetBundleNames();
        foreach (string bundleName in allBundleNames)
        {
            string[] assets = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
            foreach (string asset in assets)
            {
                AssetImporter imp = AssetImporter.GetAtPath(asset);
                if (imp != null)
                {
                    imp.assetBundleName = string.Empty;
                    imp.assetBundleVariant = string.Empty;
                    imp.SaveAndReimport();
                    cleared++;
                }
            }
        }
        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.Refresh();
        Debug.Log($"[ABBuilder] 已清除 {cleared} 个资源的AB包标签");
        EditorUtility.DisplayDialog("清除完成", $"已清除 {cleared} 个标签", "确定");
    }

    [MenuItem("Tools/AssetBundle/7. 查看当前AB包统计")]
    public static void ShowBundleStats()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("===== 当前AB包统计 =====");
        string[] names = AssetDatabase.GetAllAssetBundleNames();
        long totalSize = 0;

        foreach (string name in names)
        {
            string[] assets = AssetDatabase.GetAssetPathsFromAssetBundle(name);
            long bundleSize = 0;
            foreach (string a in assets)
            {
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), a);
                if (File.Exists(fullPath))
                {
                    FileInfo fi = new FileInfo(fullPath);
                    bundleSize += fi.Length;
                }
            }
            totalSize += bundleSize;
            sb.AppendLine($"  {name}: {assets.Length} 个资源, 约 {FormatSize(bundleSize)}");
        }

        sb.AppendLine();
        sb.AppendLine($"总计 {names.Length} 个AB包, 约 {FormatSize(totalSize)}");

        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("AB包统计", sb.ToString(), "确定");
    }

    [MenuItem("Tools/AssetBundle/8. 导出标签配置到JSON")]
    public static void ExportBundleConfig()
    {
        BundleConfig config = new BundleConfig();
        string[] names = AssetDatabase.GetAllAssetBundleNames();
        foreach (string name in names)
        {
            BundleGroup g = new BundleGroup { bundleName = name };
            g.assetPaths.AddRange(AssetDatabase.GetAssetPathsFromAssetBundle(name));
            config.groups.Add(g);
        }
        string json = JsonUtility.ToJson(config, true);
        string savePath = EditorUtility.SaveFilePanel("导出AB包配置", "", "bundle_config.json", "json");
        if (!string.IsNullOrEmpty(savePath))
        {
            File.WriteAllText(savePath, json, Encoding.UTF8);
            EditorUtility.DisplayDialog("导出成功", $"配置已保存到:\n{savePath}\n(UTF-8编码,中文无乱码)", "确定");
            Debug.Log($"[ABBuilder] 配置已导出: {savePath}");
        }
    }

    private static void BuildForTarget(BuildTarget target)
    {
        if (EditorApplication.isCompiling)
        {
            EditorUtility.DisplayDialog("错误", "Unity正在编译脚本，请稍后再试", "确定");
            return;
        }

        if (EditorApplication.isPlaying)
        {
            if (!EditorUtility.DisplayDialog("警告", "当前正在运行游戏模式，是否退出后再构建？", "退出并构建", "取消"))
            {
                return;
            }
            EditorApplication.isPlaying = false;
        }

        try
        {
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
            string platformFolder = GetPlatformFolderName(target);
            string outputPath = Path.Combine(OutputRoot, platformFolder);

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            Debug.Log($"[ABBuilder] 开始构建 {platformFolder} AB包到: {outputPath}");
            EditorUtility.DisplayProgressBar("构建AB包", $"准备 {platformFolder} ...", 0.1f);

            BuildAssetBundleOptions options =
                BuildAssetBundleOptions.ChunkBasedCompression |
                BuildAssetBundleOptions.DisableWriteTypeTree;

            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
                outputPath,
                options,
                target
            );

            EditorUtility.ClearProgressBar();

            if (manifest == null)
            {
                Debug.LogError("[ABBuilder] 构建失败，请查看Console中的错误信息");
                EditorUtility.DisplayDialog("构建失败", "请查看Console错误详情", "确定");
                return;
            }

            string[] bundles = manifest.GetAllAssetBundles();
            StringBuilder report = new StringBuilder();
            report.AppendLine($"===== {platformFolder} AB包构建完成 =====");
            report.AppendLine($"输出路径: {Path.GetFullPath(outputPath)}");
            report.AppendLine($"AB包数量: {bundles.Length}");
            report.AppendLine();

            long totalBuildSize = 0;
            foreach (string b in bundles)
            {
                string filePath = Path.Combine(outputPath, b);
                long size = 0;
                if (File.Exists(filePath))
                {
                    FileInfo fi = new FileInfo(filePath);
                    size = fi.Length;
                    totalBuildSize += size;
                }
                string[] deps = manifest.GetDirectDependencies(b);
                report.AppendLine($"  {b} ({FormatSize(size)}) deps=[{string.Join(", ", deps)}]");
            }
            report.AppendLine();
            report.AppendLine($"构建总大小: {FormatSize(totalBuildSize)}");

            string manifestPath = Path.Combine(outputPath, platformFolder + ".manifest");
            if (File.Exists(manifestPath))
            {
                string manifestText = File.ReadAllText(manifestPath, Encoding.UTF8);
                report.AppendLine();
                report.AppendLine("Manifest内容:");
                report.AppendLine(manifestText);
            }

            Debug.Log(report.ToString());
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "构建成功",
                $"平台: {platformFolder}\n" +
                $"AB包数量: {bundles.Length}\n" +
                $"总大小: {FormatSize(totalBuildSize)}\n" +
                $"输出目录:\n{Path.GetFullPath(outputPath)}",
                "确定"
            );

            CopyToProjectOutputIfNeeded(outputPath, target);
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[ABBuilder] 构建异常: {e}");
            EditorUtility.DisplayDialog("构建异常", e.Message, "确定");
        }
    }

    private static void CopyToProjectOutputIfNeeded(string sourceFolder, BuildTarget target)
    {
        try
        {
            string buildDir = EditorUserBuildSettings.GetBuildLocation(target);
            if (string.IsNullOrEmpty(buildDir) || !File.Exists(buildDir))
            {
                return;
            }

            buildDir = Path.GetDirectoryName(buildDir);
            if (string.IsNullOrEmpty(buildDir) || !Directory.Exists(buildDir))
            {
                return;
            }

            string platformFolder = GetPlatformFolderName(target);
            string productName = Application.productName;
            string streamingTarget;

            if (target == BuildTarget.StandaloneWindows64 || target == BuildTarget.StandaloneWindows)
            {
                streamingTarget = Path.Combine(buildDir, productName + "_Data", "StreamingAssets", "AssetBundles", platformFolder);
            }
            else if (target == BuildTarget.StandaloneOSX)
            {
                streamingTarget = Path.Combine(buildDir, productName + ".app", "Contents", "Resources", "Data", "StreamingAssets", "AssetBundles", platformFolder);
            }
            else
            {
                return;
            }

            string parentDir = Path.GetDirectoryName(streamingTarget);
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            if (Directory.Exists(streamingTarget))
            {
                Directory.Delete(streamingTarget, true);
            }
            DirectoryCopy(sourceFolder, streamingTarget, true);
            Debug.Log($"[ABBuilder] 已同步AB包到构建输出目录: {streamingTarget}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ABBuilder] 同步到输出目录失败(可忽略): {e.Message}");
        }
    }

    private static void DirectoryCopy(string sourceDir, string destDir, bool copySubDirs)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists) throw new DirectoryNotFoundException(sourceDir);
        Directory.CreateDirectory(destDir);

        foreach (FileInfo file in dir.GetFiles())
        {
            string tPath = Path.Combine(destDir, file.Name);
            file.CopyTo(tPath, true);
        }
        if (copySubDirs)
        {
            foreach (DirectoryInfo sub in dir.GetDirectories())
            {
                string t = Path.Combine(destDir, sub.Name);
                DirectoryCopy(sub.FullName, t, copySubDirs);
            }
        }
    }

    private static void AddArtAssets(ref Dictionary<string, string> mapping, string path, string bundleName)
    {
        if (!Directory.Exists(path) && !File.Exists(path))
        {
            return;
        }

        FileAttributes attr = File.GetAttributes(path);
        if ((attr & FileAttributes.Directory) != FileAttributes.Directory)
        {
            if (ShouldIncludeFile(path))
            {
                mapping[path] = bundleName;
            }
            return;
        }

        string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
        foreach (string f in files)
        {
            string assetPath = f.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
            if (assetPath.StartsWith("./") || assetPath.StartsWith(".\\"))
            {
                assetPath = assetPath.Substring(2);
            }
            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                int idx = assetPath.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0) assetPath = assetPath.Substring(idx);
            }

            if (assetPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
            if (!ShouldIncludeFile(assetPath)) continue;

            mapping[assetPath] = bundleName;
        }
    }

    private static void AddDataAssets(ref Dictionary<string, string> mapping, string path, string bundleName)
    {
        if (!Directory.Exists(path)) return;

        string[] files = Directory.GetFiles(path, "*.asset", SearchOption.AllDirectories);
        foreach (string f in files)
        {
            string assetPath = f.Replace(Path.DirectorySeparatorChar, '/');
            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                int idx = assetPath.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0) assetPath = assetPath.Substring(idx);
            }
            if (assetPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
            mapping[assetPath] = bundleName;
        }

        string[] prefabFiles = Directory.GetFiles(path, "*.prefab", SearchOption.AllDirectories);
        foreach (string f in prefabFiles)
        {
            string assetPath = f.Replace(Path.DirectorySeparatorChar, '/');
            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                int idx = assetPath.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0) assetPath = assetPath.Substring(idx);
            }
            if (assetPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
            if (mapping.ContainsKey(assetPath)) continue;
            mapping[assetPath] = bundleName;
        }
    }

    private static bool ShouldIncludeFile(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        HashSet<string> includeExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".gif", ".psd",
            ".fbx", ".obj", ".max", ".blend", ".mb", ".ma",
            ".mat",
            ".prefab",
            ".wav", ".mp3", ".ogg", ".aiff", ".flac",
            ".shader", ".shadergraph",
            ".asset",
            ".ttf", ".otf",
            ".lighting",
            ".exr", ".hdr",
        };
        return includeExt.Contains(ext);
    }

    private static string GetPlatformFolderName(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return "StandaloneWindows";
            case BuildTarget.StandaloneOSX:
                return "StandaloneOSX";
            case BuildTarget.StandaloneLinux64:
                return "StandaloneLinux64";
            case BuildTarget.Android:
                return "Android";
            case BuildTarget.iOS:
                return "iOS";
            case BuildTarget.WebGL:
                return "WebGL";
            default:
                return target.ToString();
        }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024f:F2} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / 1024f / 1024f:F2} MB";
        return $"{bytes / 1024f / 1024f / 1024f:F2} GB";
    }
}
