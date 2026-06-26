using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Eventmanager : UnitySingleton<Eventmanager>
{
    // 定义事件处理委托：接收事件名称和用户自定义数据
    public delegate void event_handler(string event_name, object udata);

    // 字典，用于存储事件名称对应的委托链（多播委托）
    private Dictionary<string, event_handler> dic = new Dictionary<string, event_handler>();

    // 初始化方法（可扩展）
    public void init() { }

    /// <summary>
    /// 添加事件监听器
    /// </summary>
    /// <param name="event_name">事件名称</param>
    /// <param name="h">要添加的委托</param>
    public void AddListener(string event_name, event_handler h)
    {
        if (this.dic.ContainsKey(event_name))
        {
            // 如果事件已存在，将新委托附加到现有委托链上（多播）
            this.dic[event_name] += h;
        }
        else
        {
            // 否则创建新的事件条目
            this.dic.Add(event_name, h);
        }
    }

    /// <summary>
    /// 移除事件监听器
    /// </summary>
    /// <param name="event_name">事件名称</param>
    /// <param name="h">要移除的委托</param>
    public void RemoveListener(string event_name, event_handler h)
    {
        if (!this.dic.ContainsKey(event_name))
        {
            return; // 事件不存在，直接返回
        }

        // 从委托链中移除指定委托
        this.dic[event_name] -= h;

        // 如果移除后委托链为空（null），则删除该事件条目，避免空条目残留
        if (this.dic[event_name] == null)
        {
            this.dic.Remove(event_name);
        }
    }

    /// <summary>
    /// 触发事件
    /// </summary>
    /// <param name="event_name">事件名称</param>
    /// <param name="udata">用户数据（任意类型）</param>
    public void Emit(string event_name, object udata)
    {
        if (!this.dic.ContainsKey(event_name))
        {
            return; // 没有监听器，直接返回
        }

        // 调用委托链，所有注册的监听器将依次执行
        this.dic[event_name](event_name, udata);
    }
}