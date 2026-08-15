using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SGDelegateEvent : MonoBehaviour
{
    static SGDelegateEvent _instance;
    static object mutex = new object();
    public static SGDelegateEvent Instance
    {//供外部访问本单例
        get
        {
            if (_instance == null)
            {
                lock (mutex)
                {//防止多线程访问，虽然应该不会发生，但还是加锁了
                    if (_instance == null)
                    {
                        _instance = new SGDelegateEvent();
                    }
                }
            }
            return _instance;
        }
    }
    //定义本游戏事件挂载委托方法的签名//无返回值，规范参数：事件名称str，事件数据t
    //public delegate void SGDelegate(string eventName, object udata);
    public Dictionary<string, Action> dic = new();
    public void InitDic()=>dic.Clear();
    public void AddListener(string eventName, Action h){
        if(dic.ContainsKey(eventName)){
            dic[eventName] += h;
        }
        else{
            dic.Add(eventName, h);
        }
    }
    public void RemoveListener(string eventName, Action h){
        if(dic.ContainsKey(eventName)){
            dic[eventName] -= h;
            if(dic[eventName] == null){
                dic.Remove(eventName);
            }
        }
        else return;
    }
    public void Emit(string eventName){
        if(dic.ContainsKey(eventName)){
            dic[eventName].Invoke();
        }
    }
    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        if(_instance == null){
            _instance = this;
        }
        else{
            GameObject.Destroy(this.gameObject);
        } 
    }

}
