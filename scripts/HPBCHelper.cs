using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class HPBCHelper : EditorWindow
{
    private string searchString = "";
    private bool includeInactive = true;      // 是否包含未激活的物体
    private bool searchInPrefabs = false;     // 是否搜索预制体（仅场景物体）

    [MenuItem("Tools/按名称选中物体")]
    public static void ShowWindow()
    {
        GetWindow<HPBCHelper>("按名称选中物体");
    }

    private void OnGUI()
    {
        GUILayout.Label("根据名称关键字选中物体", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        searchString = EditorGUILayout.TextField("关键字:", searchString);
        includeInactive = EditorGUILayout.Toggle("包含未激活物体", includeInactive);
        searchInPrefabs = EditorGUILayout.Toggle("搜索预制体资源", searchInPrefabs);

        EditorGUILayout.Space();

        if (GUILayout.Button("选中匹配物体", GUILayout.Height(30)))
        {
            SelectObjectsByName();
        }

        if (GUILayout.Button("选中并记录日志", GUILayout.Height(30)))
        {
            SelectObjectsByName(true);
        }
    }

    private void SelectObjectsByName(bool logResult = false)
    {
        if (string.IsNullOrEmpty(searchString))
        {
            EditorUtility.DisplayDialog("提示", "请输入搜索关键字", "确定");
            return;
        }

        List<GameObject> matchedObjects = new List<GameObject>();

        // 1. 搜索当前场景中所有根物体及子物体
        if (!searchInPrefabs)
        {
            GameObject[] allRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in allRoots)
            {
                CollectMatchedObjects(root, matchedObjects);
            }
        }
        else
        {
            // 2. 搜索项目中的预制体资源（谨慎，可能耗时较长）
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.name.IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matchedObjects.Add(prefab);
                }
            }
        }

        if (matchedObjects.Count == 0)
        {
            Debug.Log($"未找到名称包含 “{searchString}” 的物体{(searchInPrefabs ? "（预制体）" : "（场景中）")}");
            return;
        }

        // 选中所有匹配物体
        Selection.objects = matchedObjects.ToArray();

        if (logResult)
        {
            Debug.Log($"已选中 {matchedObjects.Count} 个物体：\n" +
                      string.Join("\n", matchedObjects.Select(go => go.name)));
        }

        // 可选：将选中物体在 Hierarchy 中高亮显示
        EditorGUIUtility.PingObject(matchedObjects[0]);
    }

    private void CollectMatchedObjects(GameObject obj, List<GameObject> list)
    {
        // 检查当前物体
        if (obj.name.IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            if (includeInactive || obj.activeInHierarchy)
            {
                list.Add(obj);
            }
        }

        // 递归检查子物体
        foreach (Transform child in obj.transform)
        {
            CollectMatchedObjects(child.gameObject, list);
        }
    }
}