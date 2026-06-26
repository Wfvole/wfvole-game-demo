using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class startUICtrl : UIcontroller
{
    // Start is called before the first frame update
    void Start()
    {
        add_button_listener("newgameButton", OnNewGametButton);
        add_button_listener("loadButton",OnLoadButton);
    }
    void OnLoadButton()
    {
        SaveManager.Instance.LoadPlayDate("s");
    }
    void OnNewGametButton()
    {
        //Debug.Log("新游戏按钮被点击");
        //加载测试地图//实例化测试地图
        //string path = "test";
        string path = "Scene/浮光乐园";
        GameObject testT=Resourcemanger.Instance.GetAssetCache<GameObject>(path);
        testT.tag = "GamingObj";
        Instantiate(testT, new Vector3(-1.25f, -4.5999999f, 13.0299997f), Quaternion.identity);
        //实例化玩家
        path = "Player/妖精爱莉";
        GameObject playerPrefab = Resourcemanger.Instance.GetAssetCache<GameObject>(path);
        Instantiate(playerPrefab,new Vector3(-1.25f, -4.5999999f, 12.5900002f),Quaternion.identity);
        string childUI = GameplayManager.Instance.uiStatus.ToString();
        UImanager.Instance.RemoveUIview(childUI);
        UImanager.Instance.RemoveUIview("mainUI");
        GameplayManager.Instance.TurnToGamingUI();
        UImanager.Instance.ShowUIView("gamingUI");
        ThirdPersonShooterController.Instance.gameMode = ThirdPersonShooterController.GameMode.Paradise;
    }
}
