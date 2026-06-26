using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazePass : UnitySingleton<MazePass>
{
    private bool isrange;
    private void Start()
    {
        Eventmanager.Instance.AddListener("touchPick", Mazepass);
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isrange = true;
            // 显示UI提示（例如按E拾取）
            if (!GameplayManager.Instance.readyPlayerModels.Contains("小凯撒"))
            {
                UImanager.Instance.DialogTextMgr("解锁新人物");
                GameplayManager.Instance.readyPlayerModels.Add("小凯撒");
            }
            UImanager.Instance.ShowPickupTip("点击通过本层迷宫：");
            //Debug.Log("玩家触碰到出口");
        }
        //else Debug.Log("错误接触");
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isrange = false;
            UImanager.Instance.HidePickupTip();
            UImanager.Instance.OffDialog();
        }
    }
    void Mazepass(string a,object b)
    {
        if (b is bool o) 
        { 
            if (o&&isrange)
            {
                Eventmanager.Instance.Emit("touchPick", false);
                UImanager.Instance.OffDialog();
                isrange = false;
                Eventmanager.Instance.Emit("Mazepass",true);
                //UImanager.Instance.DialogTextMgr("即将进入下一关,难度提升");
            }
        }
    }
}
