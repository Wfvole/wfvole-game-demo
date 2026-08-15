using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resourcemanger : UnitySingleton<Resourcemanger>
{
    public override void Awake()
    {
        base.Awake();
    }

    // 资源缓存字典，避免重复加载
    private Dictionary<string, UnityEngine.Object> resourceCache = new Dictionary<string, UnityEngine.Object>();

    public T GetAssetCache<T>(string name) where T : UnityEngine.Object
    {
        // 检查缓存中是否已有该资源
        if (resourceCache.ContainsKey(name))
        {
            return resourceCache[name] as T;
        }

        // 从Resources文件夹加载资源
        // 注意：实际使用时需要确保资源已放在Resources文件夹中，或者使用其他加载方式
        T target = Resources.Load<T>(name);

        // 如果加载成功，加入缓存
        if (target != null)
        {
            resourceCache[name] = target;
            return target;
        }
        else
        {
            Debug.LogError($"资源加载失败: {name}");
            return null;
        }
    }

    // 可选：添加清理缓存的方法
    public void ClearCache()
    {
        resourceCache.Clear();
        Resources.UnloadUnusedAssets();
    }

    // 可选：移除特定资源的缓存
    public void RemoveFromCache(string name)
    {
        if (resourceCache.ContainsKey(name))
        {
            resourceCache.Remove(name);
        }
    }

    // ========== 新增：异步加载 ==========
    // 用法: Resourcemanger.Instance.LoadAssetAsync<GameObject>("Prefabs/MyPrefab", (obj) => { Instantiate(obj); });
    public void LoadAssetAsync<T>(string name, Action<T> onCompleted) where T : UnityEngine.Object
    {
        StartCoroutine(LoadAssetAsyncCoroutine(name, onCompleted));
    }

    private IEnumerator LoadAssetAsyncCoroutine<T>(string name, Action<T> onCompleted) where T : UnityEngine.Object
    {
        // 如果已经缓存，直接返回缓存资源
        if (resourceCache.ContainsKey(name))
        {
            onCompleted?.Invoke(resourceCache[name] as T);
            yield break;
        }

        // 异步加载
        ResourceRequest request = Resources.LoadAsync<T>(name);
        yield return request;

        T asset = request.asset as T;
        if (asset != null)
        {
            resourceCache[name] = asset;
            onCompleted?.Invoke(asset);
        }
        else
        {
            Debug.LogError($"异步资源加载失败: {name}");
            onCompleted?.Invoke(null);
        }
    }

}
