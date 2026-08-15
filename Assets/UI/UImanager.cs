using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using TMPro;


public class UIcontroller : MonoBehaviour
//管理一个UI界面内部的控件。
{
    // 核心数据结构：存储UI界面中所有游戏对象的引用
    // 键：游戏对象在UI树中的完整路径（如"Panel/Content/Button"）
    // 值：对应的GameObject引用
    public Dictionary<string, GameObject> view = new Dictionary<string, GameObject>();
    /// 递归加载UI界面中的所有游戏对象到view字典
    /// name="root"当前遍历的根节点
    /// name="path"当前节点的路径前缀
    private void load_all_object(GameObject root, string path)
    {
        // 遍历当前节点的所有直接子节点
        foreach (Transform tf in root.transform)
        {
            // 构建当前节点的完整路径
            string fullPath = path + tf.gameObject.name;
            // 检查是否已存在（避免重复添加）
            if (this.view.ContainsKey(fullPath))
            {
                // 已存在则跳过（避免循环引用或重复项）
                continue;
            }
            // 将当前节点添加到字典
            this.view.Add(fullPath, tf.gameObject);
            // 递归遍历子节点，路径后加"/"作为分隔符
            load_all_object(tf.gameObject, fullPath + "/");
        }
    }

        /// Unity生命周期函数：组件唤醒时自动调用
        /// 自动注册该UI界面下的所有控件
    public virtual void Awake()
    {
        // 从当前游戏对象开始，递归加载所有子对象到字典
        this.load_all_object(this.gameObject, "");
       // 可选：打印前10个对象
        //int count = 0;
        //foreach (var kvp in view)
        //{
        //    Debug.Log($"  {kvp.Key}");
        //    //if (count++ < 10)
        //    //{
        //    //    Debug.Log($"  {kvp.Key}");
        //    //}
        //    //else
        //    //{
        //    //    Debug.Log($"  ... 还有 {view.Count - 10} 个对象");
        //    //    break;
        //    //}
        //}
    }

    /// 为指定按钮添加点击事件监听器
    /// <param name="view_name">按钮在view字典中的路径键
    /// <param name="onclick">点击事件回调函数
    public void add_button_listener(string view_name, UnityAction onclick)
    {
        // 从字典中获取按钮GameObject
        Button bt = this.view[view_name].GetComponent<Button>();

        // 安全检查：确保找到的GameObject上有Button组件
        if (bt == null)
        {
            Debug.LogWarning("UI_manager add_button_listener: not Button Component!");
            return;
        }

        // 添加点击事件监听
        bt.onClick.AddListener(onclick);
    }
    /// 为指定滑动条添加值改变事件监听器
    /// <param name="view_name">滑动条在view字典中的路径键
    /// <param name="on_value_changle">值改变事件回调函数
    public void add_slider_listener(string view_name, UnityAction<float> on_value_changle)
    {
        // 从字典中获取滑动条GameObject
        Slider s = this.view[view_name].GetComponent<Slider>();

        // 安全检查
        if (s == null)
        {
            //Debug.LogWarning("UI_manager add_slider_listener: not Slider Component!");
            return;
        }

        // 添加值改变事件监听
        s.onValueChanged.AddListener(on_value_changle);
    }
    //添加输入文本监听
    public void add_inputfield_listener(string view_name, UnityAction<string> on_string_changed)
    {
        TMP_InputField inf = this.view[view_name].GetComponent<TMP_InputField>();
        inf.onEndEdit.AddListener(on_string_changed);
    }
    public void add_toggle_listener(string view_name, UnityAction<bool> on_isOn_changed)
    { 
        Toggle tog = this.view[view_name].GetComponent<Toggle>();
        tog.onValueChanged.AddListener(on_isOn_changed);
    }
    public void add_dropdowne_listener(string view_name, UnityAction<int> on_value_changed)
    {
        TMP_Dropdown pd = this.view[view_name].GetComponent< TMP_Dropdown>();
        pd.onValueChanged.AddListener(on_value_changed);
    }

    /// 打印view字典中的所有键值对
    public void PrintViewDictionary()
    {
        foreach (var kvp in view)
        {
            Debug.Log($"Key: {kvp.Key}, Value: {kvp.Value.name}");
        }
    }

}

public class UImanager :  UnitySingleton<UImanager> {
    public GameObject canvas;
    private Dictionary<string, GameObject> shownViews = new Dictionary<string, GameObject>();
    public override void Awake()
    {
        base.Awake();
        this.canvas = GameObject.Find("Canvas");
        if (this.canvas == null)
        {
            Debug.LogError("UI manager load  Canvas failed!!!!!");
        }
    }
    public UIcontroller GetUIView(string viewName)
    {
        if (shownViews.TryGetValue(viewName, out GameObject go) && go != null)
        {
            return go.GetComponent<UIcontroller>();
        }
        return null;
    }
    public UIcontroller ShowUIView(string name)
    {
        if (shownViews.ContainsKey(name)) return null;
        // 1. 构建资源路径（假设预制体放在Resources/UI目录下）
        string path = "UI/" + name;

        // 2. 通过资源管理器加载UI预制体
        GameObject ui_prefab = (GameObject)Resourcemanger.Instance.GetAssetCache<GameObject>(path);

        // 3. 实例化UI预制体
        GameObject ui_view = GameObject.Instantiate(ui_prefab);
        ui_view.name = ui_prefab.name; // 保持原名

        // 4. 将UI实例设置为Canvas的子对象
        // false参数表示保持UI的局部坐标系，不继承父级缩放
        ui_view.transform.SetParent(this.canvas.transform, false);

        // 5. 处理可能包含路径的name参数
        // 例如：传入"Login/LoginPanel"，只取"LoginPanel"
        int lastIndex = name.LastIndexOf("/");
        if (lastIndex > 0)
        {
            name = name.Substring(lastIndex + 1);
        }

        // 6. 使用反射动态添加对应的控制器组件到UI实例
        // 命名约定：UI预制体名 + "Ctrl"
        Type type = Type.GetType(name + "Ctrl");
        UIcontroller ctrl = (UIcontroller)ui_view.AddComponent(type);

        // 7.记录到字典
        shownViews[name] = ui_view;

        // 8. 返回控制器实例，供外部进一步初始化
        return ctrl;
    }
    public void RemoveUIview(string name)
    {
        if (shownViews.ContainsKey(name))
        {
            GameObject uiView = shownViews[name];
            if (uiView != null)
            {
                // 销毁实例
                Destroy(uiView);
            }
            // 从字典中移除记录
            shownViews.Remove(name);
        }
        //else
        //{
        //    Debug.LogWarning($"尝试移除不存在的UI视图: {name}");
        //}
    }
    public void ShowPickupTip(string message)
    {
        var hud = GetUIView("hudUI") as hudUICtrl;
        if (hud != null)
            hud.ShowPickupTip(message);
        else
            Debug.LogWarning("hudUI 未显示或没有 hudUICtrl 组件");
    }

    public void HidePickupTip()
    {
        var hud = GetUIView("hudUI") as hudUICtrl;
        if (hud != null)
            hud.HidePickupTip();
    }

    public void UpdateWeaponUI(Sprite icon, string info)
    {
        var hud = GetUIView("hudUI") as hudUICtrl;
        if (hud != null)
            hud.UpdateWeaponUI(icon, info);
    }

    public void DialogTextMgr(string t)
    {
        var dialog = GetUIView("dialogUI") as dialogUICtrl;
        if (dialog != null)
        {
            dialog.SetUIActive(true);
            dialog.ChangeDText(t);
            StartCoroutine(HidediaUI());
        }
    }
    public void OffDialog()
    {
        var dialog = GetUIView("dialogUI") as dialogUICtrl;
        if (dialog != null)
        {
            dialog.SetUIActive(false);
        }
    }
    IEnumerator HidediaUI()
    {
        yield return new WaitForSeconds(5f);
        OffDialog();
    }
    public void InitTLJ()
    {
        var tlj = GetUIView("gamingUI") as gamingUICtrl;
        if(tlj != null)
        {
            tlj.TLJg();
        }
    }
}

