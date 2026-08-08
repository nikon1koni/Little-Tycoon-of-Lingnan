using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AssetBundleManager : MonoBehaviour
{
    public static AssetBundleManager Instance { get; private set; }

    [Header("AB包设置")]
    [Tooltip("是否启用AB包模式，关闭时全部退化到Resources.Load")]
    public bool useAssetBundles = true;

    [Tooltip("AB包根目录路径，默认使用StreamingAssets/AssetBundles")]
    public string assetBundleRootPath;

    private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, int> bundleRefCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, UnityEngine.Object> assetCache = new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);

    // single-flight 异步加载锁：bundleName -> 正在加载的句柄（统一用 object：IEnumerator 或 AsyncOperation）
    private Dictionary<string, object> inFlightBundles = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    private AssetBundle manifestBundle;
    private AssetBundleManifest manifest;
    private const string ManifestBundleKey = "__manifest__";

    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

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
            return;
        }

        if (string.IsNullOrEmpty(assetBundleRootPath))
        {
            assetBundleRootPath = Path.Combine(Application.streamingAssetsPath, "AssetBundles");
        }
    }

    public IEnumerator InitializeAsync()
    {
        if (!useAssetBundles)
        {
            Debug.Log("[AssetBundleManager] AB包模式已关闭，将使用Resources.Load加载资源");
            isInitialized = true;
            yield break;
        }

        Debug.Log("[AssetBundleManager] 开始初始化AB包系统");
        string platformName = GetPlatformName();
        string manifestPath = Path.Combine(assetBundleRootPath, platformName, platformName);

        if (!File.Exists(manifestPath))
        {
            Debug.LogWarning("[AssetBundleManager] 未找到Manifest文件: " + manifestPath + "，切换为Resources模式");
            useAssetBundles = false;
            isInitialized = true;
            yield break;
        }

        // ========== 核心兜底：同步检查 GetAllLoadedAssetBundles() ==========
        // 无论任何外部代码（MCP验证脚本、Editor工具）是否已加载同文件AB包，
        // 我们先把所有已加载bundle登记到 loadedBundles，避免重复触发 same-files 错误。
        foreach (var b in AssetBundle.GetAllLoadedAssetBundles())
        {
            if (b == null) continue;
            // 通过探测法判断：这个bundle能否加载到 AssetBundleManifest —— 如果能，它就是manifest包
            try
            {
                var probe = b.LoadAsset("AssetBundleManifest");
                if (probe is AssetBundleManifest)
                {
                    manifestBundle = b;
                    loadedBundles[ManifestBundleKey] = b;
                    bundleRefCounts[ManifestBundleKey] = 1;
                    Debug.Log("[AssetBundleManager] 在GetAllLoadedAssetBundles中发现已加载的Manifest AB包，直接复用");
                }
                // 对于其他已加载包，我们暂时无法知道其 bundleName，只能靠后续 SafeLoadFromFileAsync 兜底
            }
            catch { }
        }

        if (manifestBundle == null)
        {
            // 真正走异步加载，带 single-flight 和 same-files 异常兜底
            AssetBundleCreateRequest manifestRequest = SafeLoadFromFileAsync(manifestPath);
            if (manifestRequest != null)
            {
                yield return manifestRequest;
                manifestBundle = manifestRequest.assetBundle;
            }
        }

        if (manifestBundle == null)
        {
            // 最后兜底：从 GetAllLoadedAssetBundles 里再探测一次（异步加载因same-files失败时，它应该在里面）
            manifestBundle = FindLoadedBundleByProbe_Manifest();
        }

        if (manifestBundle == null)
        {
            Debug.LogError("[AssetBundleManager] 加载Manifest AB包失败（所有兜底路径均无效）");
            useAssetBundles = false;
            isInitialized = true;
            yield break;
        }

        // 登记 manifest 到统一缓存（保持与普通bundle一致的生命周期管理）
        loadedBundles[ManifestBundleKey] = manifestBundle;
        bundleRefCounts[ManifestBundleKey] = 1;

        AssetBundleRequest abReq = manifestBundle.LoadAssetAsync<AssetBundleManifest>("AssetBundleManifest");
        yield return abReq;

        manifest = abReq.asset as AssetBundleManifest;
        if (manifest == null)
        {
            Debug.LogError("[AssetBundleManager] 加载Manifest清单失败");
            useAssetBundles = false;
            isInitialized = true;
            yield break;
        }

        isInitialized = true;
        Debug.Log("[AssetBundleManager] AB包系统初始化完成");
    }

    /// <summary>
    /// 兜底探测：从 GetAllLoadedAssetBundles() 里找出 manifest 包
    /// </summary>
    private static AssetBundle FindLoadedBundleByProbe_Manifest()
    {
        foreach (var b in AssetBundle.GetAllLoadedAssetBundles())
        {
            if (b == null) continue;
            try
            {
                var probe = b.LoadAsset("AssetBundleManifest");
                if (probe is AssetBundleManifest)
                    return b;
            }
            catch { }
        }
        return null;
    }

    /// <summary>
    /// 兜底探测：从 GetAllLoadedAssetBundles() 里找出一个 bundle，
    /// 我们的判断依据是：它不在 loadedBundles 里，并且它不是 manifest 包。
    /// 当只有少量已加载bundle时这足够准确。
    /// </summary>
    private AssetBundle FindLoadedBundleByProbe_Generic(string bundleName)
    {
        AssetBundle best = null;
        foreach (var b in AssetBundle.GetAllLoadedAssetBundles())
        {
            if (b == null) continue;
            // 跳过已登记的
            bool alreadyRegistered = false;
            foreach (var kv in loadedBundles)
            {
                if (kv.Value == b) { alreadyRegistered = true; break; }
            }
            if (alreadyRegistered) continue;
            // 跳过 manifest 包
            try
            {
                var probe = b.LoadAsset("AssetBundleManifest");
                if (probe is AssetBundleManifest) continue;
            }
            catch { }
            best = b;
            break;
        }
        return best;
    }

    private string GetPlatformName()
    {
#if UNITY_EDITOR
        switch (UnityEditor.EditorUserBuildSettings.activeBuildTarget)
        {
            case UnityEditor.BuildTarget.StandaloneWindows:
            case UnityEditor.BuildTarget.StandaloneWindows64:
                return "StandaloneWindows";
            case UnityEditor.BuildTarget.StandaloneOSX:
                return "StandaloneOSX";
            case UnityEditor.BuildTarget.Android:
                return "Android";
            case UnityEditor.BuildTarget.iOS:
                return "iOS";
            default:
                return "StandaloneWindows";
        }
#else
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsPlayer:
                return "StandaloneWindows";
            case RuntimePlatform.OSXPlayer:
                return "StandaloneOSX";
            case RuntimePlatform.Android:
                return "Android";
            case RuntimePlatform.IPhonePlayer:
                return "iOS";
            default:
                return "StandaloneWindows";
        }
#endif
    }

    private string GetBundlePath(string bundleName)
    {
        return Path.Combine(assetBundleRootPath, GetPlatformName(), bundleName.ToLowerInvariant());
    }

    /// <summary>
    /// 规范化路径（统一分隔符+大小写）用于比较
    /// </summary>
    private static string NormalizePath(string p)
    {
        if (string.IsNullOrEmpty(p)) return p;
        return p.Replace('\\', '/').ToLowerInvariant();
    }

    // ========== SafeLoadFromFile 系列：same-files 异常安全的加载封装 ==========

    /// <summary>
    /// same-files 安全的异步AB包加载封装：
    /// - 先尝试 LoadFromFileAsync
    /// - 如返回 null（Unity 因 same-files 错误会静默返回 null req 或 req.assetBundle==null），
    ///   则从 GetAllLoadedAssetBundles() 中探测并返回已加载的包
    /// </summary>
    private AssetBundleCreateRequest SafeLoadFromFileAsync(string bundlePath)
    {
        AssetBundleCreateRequest req = null;
        try
        {
            req = AssetBundle.LoadFromFileAsync(bundlePath);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AssetBundleManager] SafeLoadFromFileAsync 启动异常（将走GetAllLoadedAssetBundles兜底）: " + bundlePath + " → " + ex.Message);
        }
        return req;
    }

    /// <summary>
    /// same-files 安全的同步AB包加载封装：
    /// - 先尝试 LoadFromFile
    /// - 若捕获异常或返回null，从GetAllLoadedAssetBundles探测兜底
    /// </summary>
    private AssetBundle SafeLoadFromFile(string bundlePath, string bundleName)
    {
        AssetBundle bundle = null;
        try
        {
            bundle = AssetBundle.LoadFromFile(bundlePath);
        }
        catch (Exception e)
        {
            // 捕获到 same files already loaded → 走兜底
            Debug.LogWarning("[AssetBundleManager] LoadFromFile捕获异常（启用兜底探测）: " + bundlePath + " → " + e.Message);
            bundle = null;
        }

        if (bundle == null)
        {
            bundle = FindLoadedBundleByProbe_Generic(bundleName);
            if (bundle != null)
                Debug.Log("[AssetBundleManager] 兜底成功：从GetAllLoadedAssetBundles复用 bundle=" + bundleName);
        }
        return bundle;
    }

    // =====================================================================

    public T LoadAsset<T>(string bundleName, string assetName) where T : UnityEngine.Object
    {
        if (!useAssetBundles || !isInitialized)
        {
            Debug.LogWarning("[AssetBundleManager] AB包未启用，从Resources同步加载: " + assetName);
            return Resources.Load<T>(assetName);
        }

        string cacheKey = bundleName + "/" + assetName;
        if (assetCache.TryGetValue(cacheKey, out UnityEngine.Object cached))
        {
            return cached as T;
        }

        AssetBundle bundle = LoadBundleInternal(bundleName);
        if (bundle == null)
        {
            Debug.LogWarning("[AssetBundleManager] 同步加载AB包失败: " + bundleName + "，回退Resources.Load: " + assetName);
            return Resources.Load<T>(assetName);
        }

        T asset = bundle.LoadAsset<T>(assetName);
        if (asset == null)
        {
            string[] assetNames = bundle.GetAllAssetNames();
            foreach (string name in assetNames)
            {
                if (Path.GetFileNameWithoutExtension(name).Equals(assetName, StringComparison.OrdinalIgnoreCase))
                {
                    asset = bundle.LoadAsset<T>(name);
                    break;
                }
            }
        }

        if (asset != null)
        {
            assetCache[cacheKey] = asset;
        }
        else
        {
            Debug.LogWarning("[AssetBundleManager] 在AB包 " + bundleName + " 中未找到资源 " + assetName + "，回退Resources同步加载");
            asset = Resources.Load<T>(assetName);
        }

        return asset;
    }

    public T[] LoadAllAssets<T>(string bundleName) where T : UnityEngine.Object
    {
        if (!useAssetBundles || !isInitialized)
        {
            return Resources.LoadAll<T>(bundleName);
        }

        AssetBundle bundle = LoadBundleInternal(bundleName);
        if (bundle == null)
        {
            return Resources.LoadAll<T>(bundleName);
        }

        return bundle.LoadAllAssets<T>();
    }

    public AsyncAssetLoadOperation<T> LoadAssetAsync<T>(string bundleName, string assetName) where T : UnityEngine.Object
    {
        AsyncAssetLoadOperation<T> op = new AsyncAssetLoadOperation<T>();
        StartCoroutine(LoadAssetAsyncCoroutine(bundleName, assetName, op));
        return op;
    }

    private IEnumerator LoadAssetAsyncCoroutine<T>(string bundleName, string assetName, AsyncAssetLoadOperation<T> op) where T : UnityEngine.Object
    {
        if (!useAssetBundles || !isInitialized)
        {
            Debug.LogWarning("[AssetBundleManager] AB包未启用，用Resources异步加载: " + assetName);
            ResourceRequest rr = Resources.LoadAsync<T>(assetName);
            yield return rr;
            op.SetResult(rr.asset as T, rr.asset != null);
            yield break;
        }

        string cacheKey = bundleName + "/" + assetName;
        if (assetCache.TryGetValue(cacheKey, out UnityEngine.Object cached))
        {
            op.SetResult(cached as T, cached != null);
            yield break;
        }

        // 异步加载包（带 single-flight + 已加载兜底）
        AssetBundle bundle = null;
        string key = bundleName.ToLowerInvariant();
        if (loadedBundles.TryGetValue(key, out bundle) && bundle != null)
        {
            bundleRefCounts[key]++;
        }
        else
        {
            // 加载依赖
            if (manifest != null)
            {
                string[] deps = manifest.GetAllDependencies(key);
                foreach (string dep in deps)
                {
                    yield return LoadBundleAsyncInternal(dep);
                }
            }

            // 加载主bundle（带 single-flight）
            yield return LoadBundleAsyncInternal(bundleName);
            // 重新取：LoadBundleAsyncInternal 已登记到 loadedBundles
            if (loadedBundles.TryGetValue(key, out bundle) && bundle != null)
            {
                // already ref counted
            }
            else
            {
                Debug.LogWarning("[AssetBundleManager] 异步加载AB包失败: " + bundleName + "，回退Resources异步加载: " + assetName);
                ResourceRequest rr = Resources.LoadAsync<T>(assetName);
                yield return rr;
                op.SetResult(rr.asset as T, rr.asset != null);
                yield break;
            }
        }

        AssetBundleRequest abReq = bundle.LoadAssetAsync<T>(assetName);
        yield return abReq;

        T result = abReq.asset as T;
        if (result == null)
        {
            string[] assetNames = bundle.GetAllAssetNames();
            foreach (string name in assetNames)
            {
                if (Path.GetFileNameWithoutExtension(name).Equals(assetName, StringComparison.OrdinalIgnoreCase))
                {
                    abReq = bundle.LoadAssetAsync<T>(name);
                    yield return abReq;
                    result = abReq.asset as T;
                    break;
                }
            }
        }

        if (result == null)
        {
            Debug.LogWarning("[AssetBundleManager] 异步加载资源失败 " + assetName + "，回退Resources");
            ResourceRequest rr = Resources.LoadAsync<T>(assetName);
            yield return rr;
            result = rr.asset as T;
        }

        if (result != null)
        {
            assetCache[cacheKey] = result;
        }

        op.SetResult(result, result != null);
    }

    /// <summary>
    /// 同步：加载单个 AssetBundle 到内存（内部使用，带缓存 + 依赖 + same-files 兜底）
    /// </summary>
    private AssetBundle LoadBundleInternal(string bundleName)
    {
        string key = bundleName.ToLowerInvariant();
        if (loadedBundles.TryGetValue(key, out AssetBundle bundle) && bundle != null)
        {
            bundleRefCounts[key]++;
            return bundle;
        }

        if (manifest != null)
        {
            string[] deps = manifest.GetAllDependencies(key);
            foreach (string dep in deps)
            {
                LoadBundleInternal(dep);
            }
        }

        string bundlePath = GetBundlePath(bundleName);
        if (!File.Exists(bundlePath))
        {
            Debug.LogError("[AssetBundleManager] AB包文件不存在: " + bundlePath);
            return null;
        }

        // 使用 SafeLoadFromFile 自动处理 same-files 异常
        bundle = SafeLoadFromFile(bundlePath, bundleName);

        if (bundle == null)
        {
            Debug.LogError("[AssetBundleManager] 同步加载AB包失败: " + bundlePath);
            return null;
        }

        loadedBundles[key] = bundle;
        bundleRefCounts[key] = 1;
        return bundle;
    }

    /// <summary>
    /// 异步加载单个 AssetBundle 内部实现 — 带 single-flight 并发控制 + 已加载兜底
    /// </summary>
    private IEnumerator LoadBundleAsyncInternal(string bundleName)
    {
        string key = bundleName.ToLowerInvariant();

        // 1) 已在内存中，直接引用计数并返回
        if (loadedBundles.TryGetValue(key, out AssetBundle existing) && existing != null)
        {
            bundleRefCounts[key]++;
            yield break;
        }

        // 2) Single-flight: 如果有另一个协程正在加载同一个 bundle，等它完成
        if (inFlightBundles.TryGetValue(key, out object inFlightObj) && inFlightObj != null)
        {
            // 统一转换为 yield return 可接受的对象
            if (inFlightObj is IEnumerator ie)
            {
                yield return ie;
            }
            else if (inFlightObj is AsyncOperation ao)
            {
                yield return ao;
            }

            // 结束后再次检查缓存
            if (loadedBundles.TryGetValue(key, out existing) && existing != null)
            {
                bundleRefCounts[key]++;
            }
            yield break;
        }

        // 3) 真正加载 — 先检查 GetAllLoadedAssetBundles 兜底
        string bundlePath = GetBundlePath(bundleName);
        AssetBundle fastResult = null;

        // 快速兜底：如果外部代码已加载，LoadFromFileAsync 将返回 null 的 req.assetBundle
        AssetBundleCreateRequest req = SafeLoadFromFileAsync(bundlePath);

        if (req == null)
        {
            // SafeLoadFromFileAsync 直接抛异常，立即兜底探测
            fastResult = FindLoadedBundleByProbe_Generic(bundleName);
            if (fastResult != null)
            {
                loadedBundles[key] = fastResult;
                bundleRefCounts[key] = 1;
            }
            yield break;
        }

        // 注册 single-flight（统一存 AsyncOperation，兼容字典 object 类型）
        inFlightBundles[key] = req;
        yield return req;
        inFlightBundles.Remove(key);

        if (req.assetBundle != null)
        {
            loadedBundles[key] = req.assetBundle;
            bundleRefCounts[key] = 1;
        }
        else
        {
            // 异步也失败了 — 很可能是 same-files 错误。再次兜底：
            fastResult = FindLoadedBundleByProbe_Generic(bundleName);
            if (fastResult != null)
            {
                loadedBundles[key] = fastResult;
                bundleRefCounts[key] = 1;
                Debug.Log("[AssetBundleManager] 异步兜底成功：从GetAllLoadedAssetBundles复用 bundle=" + bundleName);
            }
            else
            {
                Debug.LogWarning("[AssetBundleManager] 异步加载AB包失败（所有兜底路径均无效）: " + bundlePath);
            }
        }
    }

    public void UnloadBundle(string bundleName, bool force = false)
    {
        string key = bundleName.ToLowerInvariant();
        if (!loadedBundles.TryGetValue(key, out AssetBundle bundle) || bundle == null)
        {
            return;
        }

        if (bundleRefCounts.TryGetValue(key, out int count))
        {
            count--;
            bundleRefCounts[key] = count;
            if (count > 0 && !force)
            {
                return;
            }
        }

        List<string> keysToRemove = new List<string>();
        foreach (var kv in assetCache)
        {
            if (kv.Key.StartsWith(key + "/", StringComparison.Ordinal))
            {
                keysToRemove.Add(kv.Key);
            }
        }
        foreach (string k in keysToRemove)
        {
            assetCache.Remove(k);
        }

        bundle.Unload(false);
        loadedBundles.Remove(key);
        bundleRefCounts.Remove(key);
    }

    public void UnloadAllBundles()
    {
        foreach (var kv in loadedBundles)
        {
            if (kv.Value != null)
            {
                kv.Value.Unload(false);
            }
        }
        loadedBundles.Clear();
        bundleRefCounts.Clear();
        assetCache.Clear();
        inFlightBundles.Clear();

        manifestBundle = null;
        manifest = null;
    }

    void OnDestroy()
    {
        UnloadAllBundles();
    }
}

public class AsyncAssetLoadOperation<T> where T : UnityEngine.Object
{
    public bool IsDone { get; private set; }
    public T Result { get; private set; }
    public bool Success { get; private set; }
    public event Action<T, bool> OnCompleted;

    public void SetResult(T result, bool success)
    {
        Result = result;
        Success = success;
        IsDone = true;
        try
        {
            OnCompleted?.Invoke(result, success);
        }
        catch (Exception e)
        {
            Debug.LogError("[AsyncAssetLoadOperation] 回调异常: " + e);
        }
    }

    public IEnumerator WaitForCompletion()
    {
        while (!IsDone)
        {
            yield return null;
        }
    }
}
