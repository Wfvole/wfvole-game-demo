using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SplashLoader : MonoBehaviour
{
    [Header("目标场景名称")]
    public string targetSceneName = "Demo mygame"; // 改为你的主菜单或游戏场景名称

    [Header("是否显示加载进度")]
    public bool showProgress = true;

    [Header("UI 进度条（可选）")]
    public UnityEngine.UI.Slider progressSlider; // 可拖拽赋值

    void Awake()
    {
        // 立即开始异步加载，不等待任何延时
        StartCoroutine(LoadTargetScene());
    }

    IEnumerator LoadTargetScene()
    {
        // 开始异步加载目标场景
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync(targetSceneName);
        asyncOp.allowSceneActivation = false; // 先不自动激活，等待加载完成

        // 等待加载进度达到 0.9（实际加载完成）
        while (asyncOp.progress < 0.9f)
        {
            if (showProgress && progressSlider != null)
            {
                // 进度值归一化（0~1）
                progressSlider.value = asyncOp.progress / 0.9f;
            }
            yield return null;
        }

        // 加载完成，显示 100% 进度
        if (showProgress && progressSlider != null)
            progressSlider.value = 1f;

        // 可选：短暂停留，让玩家看到加载完成（如 0.2 秒）
        yield return new WaitForSeconds(10f);

        // 激活目标场景
        asyncOp.allowSceneActivation = true;

        // 等待场景切换完成
        while (!asyncOp.isDone)
            yield return null;

        // 目标场景已激活，此场景会自动卸载（如果未标记为持久化）
        // 但为了保险，也可以手动销毁此 GameObject
        Destroy(gameObject);
    }
}