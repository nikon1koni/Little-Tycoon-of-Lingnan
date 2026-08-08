using System;
using System.Collections;
using UnityEngine;

public static class ResourceLoader
{
    public static bool UseAssetBundles
    {
        get
        {
            if (AssetBundleManager.Instance == null) return false;
            return AssetBundleManager.Instance.useAssetBundles;
        }
        set
        {
            EnsureManagerExists();
            AssetBundleManager.Instance.useAssetBundles = value;
        }
    }

    public static bool IsReady
    {
        get
        {
            if (AssetBundleManager.Instance == null) return false;
            return AssetBundleManager.Instance.IsInitialized;
        }
    }

    private static void EnsureManagerExists()
    {
        if (AssetBundleManager.Instance == null)
        {
            GameObject go = new GameObject("AssetBundleManager");
            go.AddComponent<AssetBundleManager>();
        }
    }

    public static IEnumerator InitializeAsync()
    {
        EnsureManagerExists();
        yield return AssetBundleManager.Instance.InitializeAsync();
    }

    [Obsolete("同步加载会卡主线程，推荐使用 LoadAsync<T> 异步版本")]
    public static T Load<T>(string assetName) where T : UnityEngine.Object
    {
        return LoadInternal<T>(null, assetName);
    }

    [Obsolete("同步加载会卡主线程，推荐使用 LoadAsync<T> 异步版本")]
    public static T Load<T>(string bundleName, string assetName) where T : UnityEngine.Object
    {
        return LoadInternal<T>(bundleName, assetName);
    }

    private static T LoadInternal<T>(string bundleName, string assetName) where T : UnityEngine.Object
    {
        EnsureManagerExists();

        if (!UseAssetBundles || !IsReady)
        {
            return Resources.Load<T>(assetName);
        }

        if (string.IsNullOrEmpty(bundleName))
        {
            bundleName = ResolveBundleNameByType<T>(assetName);
        }

        return AssetBundleManager.Instance.LoadAsset<T>(bundleName, assetName);
    }

    public static T[] LoadAll<T>(string bundleName) where T : UnityEngine.Object
    {
        EnsureManagerExists();

        if (!UseAssetBundles || !IsReady)
        {
            return Resources.LoadAll<T>(bundleName);
        }

        return AssetBundleManager.Instance.LoadAllAssets<T>(bundleName);
    }

    public static AsyncLoadOperation<T> LoadAsync<T>(string assetName) where T : UnityEngine.Object
    {
        return LoadAsyncInternal<T>(null, assetName);
    }

    public static AsyncLoadOperation<T> LoadAsync<T>(string bundleName, string assetName) where T : UnityEngine.Object
    {
        return LoadAsyncInternal<T>(bundleName, assetName);
    }

    private static AsyncLoadOperation<T> LoadAsyncInternal<T>(string bundleName, string assetName) where T : UnityEngine.Object
    {
        EnsureManagerExists();

        AsyncLoadOperation<T> op = new AsyncLoadOperation<T>();

        if (!IsReady)
        {
            AssetBundleManager.Instance.StartCoroutine(WaitAndLoadAsync(bundleName, assetName, op));
        }
        else
        {
            AssetBundleManager.Instance.StartCoroutine(DoLoadAsync(bundleName, assetName, op));
        }

        return op;
    }

    private static IEnumerator WaitAndLoadAsync<T>(string bundleName, string assetName, AsyncLoadOperation<T> op) where T : UnityEngine.Object
    {
        while (!IsReady)
        {
            yield return null;
        }
        yield return DoLoadAsync(bundleName, assetName, op);
    }

    private static IEnumerator DoLoadAsync<T>(string bundleName, string assetName, AsyncLoadOperation<T> op) where T : UnityEngine.Object
    {
        if (!UseAssetBundles)
        {
            ResourceRequest rr = Resources.LoadAsync<T>(assetName);
            yield return rr;
            op.SetResult(rr.asset as T, rr.asset != null);
            yield break;
        }

        if (string.IsNullOrEmpty(bundleName))
        {
            bundleName = ResolveBundleNameByType<T>(assetName);
        }

        AsyncAssetLoadOperation<T> abOp = AssetBundleManager.Instance.LoadAssetAsync<T>(bundleName, assetName);
        yield return abOp.WaitForCompletion();

        // AB包异步加载失败时，回退 Resources.LoadAsync
        if (abOp.Result == null)
        {
            Debug.LogWarning("[ResourceLoader] AB包异步加载失败: " + bundleName + "/" + assetName + "，回退Resources.LoadAsync");
            ResourceRequest rr = Resources.LoadAsync<T>(assetName);
            yield return rr;
            op.SetResult(rr.asset as T, rr.asset != null);
            yield break;
        }

        op.SetResult(abOp.Result, abOp.Success);
    }

    [Obsolete("同步实例化会卡主线程，推荐使用 InstantiatePrefabAsync 异步版本")]
    public static GameObject InstantiatePrefab(string assetName)
    {
        return InstantiatePrefab(assetName, null, false);
    }

    [Obsolete("同步实例化会卡主线程，推荐使用 InstantiatePrefabAsync 异步版本")]
    public static GameObject InstantiatePrefab(string assetName, Transform parent, bool worldPositionStays = false)
    {
#pragma warning disable CS0618
        GameObject prefab = Load<GameObject>(null, assetName);
#pragma warning restore CS0618
        if (prefab == null)
        {
            Debug.LogError("[ResourceLoader] 未找到预制体: " + assetName);
            return null;
        }
        return parent == null
            ? UnityEngine.Object.Instantiate(prefab)
            : UnityEngine.Object.Instantiate(prefab, parent, worldPositionStays);
    }

    public static AsyncInstantiateOperation InstantiatePrefabAsync(string assetName, Transform parent = null, bool worldPositionStays = false)
    {
        return InstantiatePrefabAsync(null, assetName, parent, worldPositionStays);
    }

    public static AsyncInstantiateOperation InstantiatePrefabAsync(string bundleName, string assetName, Transform parent = null, bool worldPositionStays = false)
    {
        AsyncInstantiateOperation op = new AsyncInstantiateOperation();
        EnsureManagerExists();
        AssetBundleManager.Instance.StartCoroutine(DoInstantiateAsync(bundleName, assetName, parent, worldPositionStays, op));
        return op;
    }

    private static IEnumerator DoInstantiateAsync(string bundleName, string assetName, Transform parent, bool worldPositionStays, AsyncInstantiateOperation op)
    {
        AsyncLoadOperation<GameObject> loadOp = LoadAsync<GameObject>(bundleName, assetName);
        yield return loadOp.WaitForCompletion();

        if (loadOp.Result == null)
        {
            Debug.LogError("[ResourceLoader] 异步实例化失败，未找到预制体: " + assetName);
            op.SetResult(null, false);
            yield break;
        }

        GameObject instance = parent == null
            ? UnityEngine.Object.Instantiate(loadOp.Result)
            : UnityEngine.Object.Instantiate(loadOp.Result, parent, worldPositionStays);
        op.SetResult(instance, true);
    }

    public static void UnloadBundle(string bundleName, bool force = false)
    {
        if (AssetBundleManager.Instance == null) return;
        AssetBundleManager.Instance.UnloadBundle(bundleName, force);
    }

    public static void UnloadAll()
    {
        if (AssetBundleManager.Instance == null) return;
        AssetBundleManager.Instance.UnloadAllBundles();
        Resources.UnloadUnusedAssets();
    }

    private static string ResolveBundleNameByType<T>(string assetName) where T : UnityEngine.Object
    {
        if (typeof(T) == typeof(Sprite) || typeof(T) == typeof(Texture2D) ||
            typeof(T) == typeof(Texture) || typeof(T) == typeof(Material))
        {
            // 贴图/材质类 — 默认归到 ui_art（若包含地形关键字可扩展区分terrain_art）
            return "ui_art";
        }

        if (typeof(T) == typeof(GameObject))
        {
            return "buildings_prefabs";
        }

        if (typeof(T) == typeof(AudioClip))
        {
            return "audio_art";
        }

        if (typeof(T) == typeof(ScriptableObject))
        {
            return "config_data";
        }

        return "config_data";
    }
}

public class AsyncLoadOperation<T> where T : UnityEngine.Object
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
        try { OnCompleted?.Invoke(result, success); }
        catch (Exception e) { Debug.LogError("[AsyncLoadOperation] 回调异常: " + e); }
    }

    public IEnumerator WaitForCompletion()
    {
        while (!IsDone) yield return null;
    }
}

public class AsyncInstantiateOperation
{
    public bool IsDone { get; private set; }
    public GameObject Result { get; private set; }
    public bool Success { get; private set; }
    public event Action<GameObject, bool> OnCompleted;

    public void SetResult(GameObject result, bool success)
    {
        Result = result;
        Success = success;
        IsDone = true;
        try { OnCompleted?.Invoke(result, success); }
        catch (Exception e) { Debug.LogError("[AsyncInstantiateOperation] 回调异常: " + e); }
    }

    public IEnumerator WaitForCompletion()
    {
        while (!IsDone) yield return null;
    }
}
