using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AssetBundleManager : MonoBehaviour
{
    public static AssetBundleManager Instance { get; private set; }

    [Header("AB包设置")]
    [Tooltip("是否启用AB包模式，关闭时回退到Resources.Load")]
    public bool useAssetBundles = true;

    [Tooltip("AB包加载根路径，默认使用StreamingAssets/AssetBundles")]
    public string assetBundleRootPath;

    private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
    private Dictionary<string, int> bundleRefCounts = new Dictionary<string, int>();
    private Dictionary<string, UnityEngine.Object> assetCache = new Dictionary<string, UnityEngine.Object>();

    private AssetBundle manifestBundle;
    private AssetBundleManifest manifest;

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
            Debug.LogWarning($"[AssetBundleManager] 未找到Manifest文件: {manifestPath}，切换到Resources模式");
            useAssetBundles = false;
            isInitialized = true;
            yield break;
        }

        AssetBundleCreateRequest manifestRequest = AssetBundle.LoadFromFileAsync(manifestPath);
        yield return manifestRequest;

        manifestBundle = manifestRequest.assetBundle;
        if (manifestBundle == null)
        {
            Debug.LogError("[AssetBundleManager] 加载Manifest AB包失败");
            useAssetBundles = false;
            isInitialized = true;
            yield break;
        }

        AssetBundleRequest abReq = manifestBundle.LoadAssetAsync<AssetBundleManifest>("AssetBundleManifest");
        yield return abReq;

        manifest = abReq.asset as AssetBundleManifest;
        if (manifest == null)
        {
            Debug.LogError("[AssetBundleManager] 加载Manifest失败");
            useAssetBundles = false;
            isInitialized = true;
            yield break;
        }

        isInitialized = true;
        Debug.Log("[AssetBundleManager] AB包系统初始化完成");
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
        return Path.Combine(assetBundleRootPath, GetPlatformName(), bundleName.ToLower());
    }

    public T LoadAsset<T>(string bundleName, string assetName) where T : UnityEngine.Object
    {
        if (!useAssetBundles || !isInitialized)
        {
            Debug.LogWarning($"[AssetBundleManager] AB包未启用，尝试从Resources加载: {assetName}");
            return Resources.Load<T>(assetName);
        }

        string cacheKey = $"{bundleName}/{assetName}";
        if (assetCache.TryGetValue(cacheKey, out UnityEngine.Object cached))
        {
            return cached as T;
        }

        AssetBundle bundle = LoadBundleInternal(bundleName);
        if (bundle == null)
        {
            Debug.LogWarning($"[AssetBundleManager] 加载AB包失败: {bundleName}，尝试Resources.Load: {assetName}");
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
            Debug.LogWarning($"[AssetBundleManager] 在AB包 {bundleName} 中未找到资源 {assetName}，尝试Resources加载");
            asset = Resources.Load<T>(assetName);
        }

        return asset;
    }

    public T[] LoadAllAssets<T>(string bundleName) where T : UnityEngine.Object
    {
        if (!useAssetBundles || !isInitialized)
        {
            Debug.LogWarning($"[AssetBundleManager] AB包未启用，从Resources加载全部: {bundleName}");
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
            Debug.LogWarning($"[AssetBundleManager] AB包未启用，从Resources异步加载: {assetName}");
            ResourceRequest rr = Resources.LoadAsync<T>(assetName);
            yield return rr;
            op.SetResult(rr.asset as T, rr.asset != null);
            yield break;
        }

        string cacheKey = $"{bundleName}/{assetName}";
        if (assetCache.TryGetValue(cacheKey, out UnityEngine.Object cached))
        {
            op.SetResult(cached as T, cached != null);
            yield break;
        }

        AssetBundle bundle = null;
        if (loadedBundles.TryGetValue(bundleName.ToLower(), out bundle) && bundle != null)
        {
            bundleRefCounts[bundleName.ToLower()]++;
        }
        else
        {
            if (manifest != null)
            {
                string[] deps = manifest.GetAllDependencies(bundleName.ToLower());
                foreach (string dep in deps)
                {
                    yield return LoadBundleAsyncInternal(dep);
                }
            }

            string bundlePath = GetBundlePath(bundleName);
            AssetBundleCreateRequest req = AssetBundle.LoadFromFileAsync(bundlePath);
            yield return req;
            bundle = req.assetBundle;
            if (bundle == null)
            {
                Debug.LogWarning($"[AssetBundleManager] 异步加载AB包失败: {bundlePath}，尝试Resources加载");
                ResourceRequest rr = Resources.LoadAsync<T>(assetName);
                yield return rr;
                op.SetResult(rr.asset as T, rr.asset != null);
                yield break;
            }
            loadedBundles[bundleName.ToLower()] = bundle;
            bundleRefCounts[bundleName.ToLower()] = 1;
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
            Debug.LogWarning($"[AssetBundleManager] 异步加载资源失败 {assetName}，回退Resources");
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

    private AssetBundle LoadBundleInternal(string bundleName)
    {
        string key = bundleName.ToLower();
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
            Debug.LogError($"[AssetBundleManager] AB包文件不存在: {bundlePath}");
            return null;
        }

        bundle = AssetBundle.LoadFromFile(bundlePath);
        if (bundle == null)
        {
            Debug.LogError($"[AssetBundleManager] 加载AB包失败: {bundlePath}");
            return null;
        }

        loadedBundles[key] = bundle;
        bundleRefCounts[key] = 1;
        return bundle;
    }

    private IEnumerator LoadBundleAsyncInternal(string bundleName)
    {
        string key = bundleName.ToLower();
        if (loadedBundles.TryGetValue(key, out AssetBundle existing) && existing != null)
        {
            bundleRefCounts[key]++;
            yield break;
        }

        string bundlePath = GetBundlePath(bundleName);
        AssetBundleCreateRequest req = AssetBundle.LoadFromFileAsync(bundlePath);
        yield return req;

        if (req.assetBundle != null)
        {
            loadedBundles[key] = req.assetBundle;
            bundleRefCounts[key] = 1;
        }
        else
        {
            Debug.LogWarning($"[AssetBundleManager] 依赖AB包加载失败: {bundlePath}");
        }
    }

    public void UnloadBundle(string bundleName, bool force = false)
    {
        string key = bundleName.ToLower();
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
            if (kv.Key.StartsWith(key + "/"))
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

        if (manifestBundle != null)
        {
            manifestBundle.Unload(false);
            manifestBundle = null;
        }
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
            Debug.LogError($"[AsyncAssetLoadOperation] 回调异常: {e}");
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
