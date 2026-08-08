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

        // 注意：AddDataAssets 必须在 AddArtAssets 之后调用，因为 .asset 应该归类为配置数据
        // 会覆盖掉 AddArtAssets 中对 .asset 文件的错误分配（如 Assets/Music/SFX/SFXConfig.asset 被归为 audio_art）
        AddDataAssets(ref pathToBundle, "Assets", "config_data");

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
                Debug.Log("[ABBuilder] 变更标签: " + assetPath + " -> " + targetLower);
            }
        }

        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.Refresh();

        Debug.Log("[ABBuilder] 标签设置完成，共扫描 " + originalCount + " 个资源，更新 " + changedCount + " 个标签");
        EditorUtility.DisplayDialog("AB包标签完成",
            "扫描资源 " + originalCount + " 个\n更新标签 " + changedCount + " 个", "确定");
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
        Debug.Log("[ABBuilder] 清除了 " + cleared + " 个资源的AB包标签");
        EditorUtility.DisplayDialog("清除完成", "清除了 " + cleared + " 个标签", "确定");
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
            sb.AppendLine("  " + name + ": " + assets.Length + " 个资源, 约 " + FormatSize(bundleSize));
        }

        sb.AppendLine();
        sb.AppendLine("总计 " + names.Length + " 个AB包，约 " + FormatSize(totalSize));

        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("AB包统计", sb.ToString(), "确定");
    }

    [MenuItem("Tools/AssetBundle/8. 导出当前标签配置JSON")]
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
            EditorUtility.DisplayDialog("导出成功", "配置已保存到:\n" + savePath + "\n(UTF-8编码，中文不会乱码)", "确定");
            Debug.Log("[ABBuilder] 配置已导出: " + savePath);
        }
    }

    private static void BuildForTarget(BuildTarget target)
    {
        if (EditorApplication.isCompiling)
        {
            EditorUtility.DisplayDialog("错误", "Unity 正在编译脚本，请稍后再试", "确定");
            return;
        }

        string platformFolder = GetPlatformFolder(target);
        string outputPath = Path.Combine(OutputRoot, platformFolder);

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        Debug.Log("[ABBuilder] 开始构建 " + platformFolder + " AB包到: " + outputPath);

        // 注意：不要使用 DisableWriteTypeTree，否则 Editor 中无法加载 AB 包
        // TypeTree 在 Editor 运行时和跨平台兼容时必需
        BuildAssetBundleOptions options =
            BuildAssetBundleOptions.ChunkBasedCompression |
            BuildAssetBundleOptions.ForceRebuildAssetBundle;

        try
        {
            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(outputPath, options, target);

            if (manifest == null)
            {
                Debug.LogError("[ABBuilder] 构建失败，请查看 Console 报错（Unity弹窗/无法识别的资产类型）");
                EditorUtility.DisplayDialog("构建失败", "BuildPipeline.BuildAssetBundles 返回 null\n请查看 Console 中的错误详情", "确定");
                return;
            }

            string[] bundles = manifest.GetAllAssetBundles();
            long totalSize = 0;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("构建成功！");
            sb.AppendLine("平台: " + platformFolder);
            sb.AppendLine("AB包数量: " + bundles.Length);
            sb.AppendLine();
            foreach (string b in bundles)
            {
                string fullPath = Path.Combine(outputPath, b);
                long size = 0;
                if (File.Exists(fullPath))
                {
                    FileInfo fi = new FileInfo(fullPath);
                    size = fi.Length;
                    totalSize += size;
                }
                sb.AppendLine("  " + b + " (" + FormatSize(size) + ")");
            }
            sb.AppendLine();
            sb.AppendLine("总大小: " + FormatSize(totalSize));
            sb.AppendLine();
            sb.AppendLine("输出目录: " + outputPath);

            Debug.Log("[ABBuilder] 构建完成: " + bundles.Length + " 个AB包, 总大小: " + FormatSize(totalSize));
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("构建成功", sb.ToString(), "确定");
        }
        catch (Exception e)
        {
            Debug.LogError("[ABBuilder] 构建抛出异常: " + e);
            EditorUtility.DisplayDialog("构建异常", e.Message, "确定");
        }
    }

    private static string GetPlatformFolder(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return "StandaloneWindows";
            case BuildTarget.StandaloneOSX:
                return "StandaloneOSX";
            case BuildTarget.Android:
                return "Android";
            case BuildTarget.iOS:
                return "iOS";
            default:
                return target.ToString();
        }
    }

    private static readonly HashSet<string> ExcludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Assets/TextMesh Pro",
        "Assets/Editor",
        "Assets/StreamingAssets",
        "Assets/Plugins",
        "Assets/Standard Assets",
        "Assets/ProjectSettings",
        "Assets/Scenes",
    };

    private static readonly HashSet<string> ExcludedAssetFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Unity 编辑器专用配置，不能包含在 AssetBundles 中
        "LightingData.asset",
        "EditorBuildSettings.asset",
        "EditorSettings.asset",
        "UnityConnectSettings.asset",
        "ProjectSettings.asset",
        "TagManager.asset",
        "GraphicsSettings.asset",
        "PhysicsSettings.asset",
        "Physics2DSettings.asset",
        "QualitySettings.asset",
        "InputManager.asset",
        "AudioManager.asset",
        "AudioMixers.asset",
        "TimeManager.asset",
        "NavMeshAreas.asset",
        "DynamicsManager.asset",
        "PresetManager.asset",
        "VFXManager.asset",
        "HDRenderPipelineGlobalSettings.asset",
        "URPProjectSettings.asset",
        "EditorResources.asset",
    };

    private static bool IsExcludedPath(string path)
    {
        foreach (string folder in ExcludedFolders)
        {
            if (path.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        string fileName = Path.GetFileName(path);
        if (ExcludedAssetFiles.Contains(fileName))
        {
            return true;
        }
        return false;
    }

    private static bool ShouldIncludeFile(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();

        // 注意：.asset（ScriptableObject 配置）不在这里处理，由 AddDataAssets 统一分配到 config_data
        // shadergraph 在某些 Unity 版本无法直接打包到 AB，排除
        if (ext == ".shadergraph" || ext == ".asset") return false;

        HashSet<string> includeExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".gif", ".psd",
            ".fbx", ".obj", ".max", ".blend", ".mb", ".ma",
            ".mat",
            ".prefab",
            ".wav", ".mp3", ".ogg", ".aiff", ".flac",
            ".shader",
            ".ttf", ".otf",
            ".lighting",
            ".exr", ".hdr",
        };
        return includeExt.Contains(ext);
    }

    private static void AddArtAssets(ref Dictionary<string, string> mapping, string path, string bundleName)
    {
        if (string.IsNullOrEmpty(path)) return;

        if (File.Exists(path))
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".shadergraph") return;
            // 单个文件（非目录），只允许特定扩展名
            HashSet<string> allAcceptable = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".gif", ".psd",
                ".fbx", ".obj", ".max", ".blend", ".mb", ".ma",
                ".mat", ".prefab", ".wav", ".mp3", ".ogg", ".aiff", ".flac",
                ".shader", ".ttf", ".otf", ".lighting", ".exr", ".hdr"
            };
            if (!allAcceptable.Contains(ext)) return;
            mapping[path] = bundleName;
            return;
        }

        if (!Directory.Exists(path)) return;

        string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
        foreach (string f in files)
        {
            string assetPath = f.Replace(Path.DirectorySeparatorChar, '/');
            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                int idx = assetPath.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    assetPath = assetPath.Substring(idx);
                }
                else
                {
                    continue;
                }
            }
            if (assetPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
            if (IsExcludedPath(assetPath)) continue;
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
            if (IsExcludedPath(assetPath)) continue;
            // 覆盖写入：确保 .asset 一律归到 config_data，即使被之前的 Art 归类错
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
            if (IsExcludedPath(assetPath)) continue;
            if (mapping.ContainsKey(assetPath)) continue;
            mapping[assetPath] = bundleName;
        }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024)
        {
            return bytes + " B";
        }
        if (bytes < 1024 * 1024)
        {
            return (bytes / 1024.0).ToString("F2") + " KB";
        }
        if (bytes < 1024 * 1024 * 1024)
        {
            return (bytes / (1024.0 * 1024.0)).ToString("F2") + " MB";
        }
        return (bytes / (1024.0 * 1024.0 * 1024.0)).ToString("F2") + " GB";
    }
}
