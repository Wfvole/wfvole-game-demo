using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class mainUICtrl : UIcontroller
{
    // Start is called before the first frame update
    void Start()
    {
        GameplayManager.Instance.gameStatus = GameplayManager.GameStatus.InUI;
        // 例如给开始游戏等按钮绑定事件
        add_button_listener("startgameButton", OnStartButton);
        add_button_listener("mazeButton",OnMazeButton);
        add_button_listener("settingButton", OnSettingButton);
        add_button_listener("escButton", OnEscButton);
    }

    void OnStartButton()
    {
        //Debug.Log("开始游戏按钮被点击");
        string childUI=GameplayManager.Instance.uiStatus.ToString();
        if(childUI!="mainUI")UImanager.Instance.RemoveUIview(childUI);
        GameplayManager.Instance.TurnToStartUI();
        UImanager.Instance.ShowUIView("startUI");
    }
    void OnMazeButton()
    {
        //Debug.Log("无限迷宫游戏按钮被点击");
        string childUI = GameplayManager.Instance.uiStatus.ToString();
        if (childUI != "mainUI") UImanager.Instance.RemoveUIview(childUI);
        GameplayManager.Instance.TurnToMazeUI();
        UImanager.Instance.ShowUIView("mazeUI");
    }
    void OnSettingButton()
    {
        //Debug.Log("设置按钮被点击");
        string childUI = GameplayManager.Instance.uiStatus.ToString();
        if (childUI != "mainUI") UImanager.Instance.RemoveUIview(childUI);
        GameplayManager.Instance.TurnToSettingUI();
        UImanager.Instance.ShowUIView("settingUI");
    }
    void OnEscButton()
    {
        //Debug.Log("退出游戏按钮被点击");
        string childUI = GameplayManager.Instance.uiStatus.ToString();
        if (childUI != "mainUI") UImanager.Instance.RemoveUIview(childUI);
        UImanager.Instance.RemoveUIview("mainUI");
        UImanager.Instance.RemoveUIview("hudUI");
        UImanager.Instance.RemoveUIview("dialogUI");
        Application.Quit();
    }
}
