using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class mazeUICtrl : UIcontroller
{
    int diywidth;
    int diylength;
    bool creatCtrl=true;
    // Start is called before the first frame update
    void Start()
    {
        SetChildUIActive(false);
        this.view["InputFieldLength"].gameObject.SetActive(false);
        this.view["InputFieldWidth"].gameObject.SetActive(false);
        add_button_listener("startmazeButton", OnStartMazeButton);
        add_button_listener("difficultyButton", OnDifficultyButton);
        add_button_listener("easyButton", OnEasyButton);
        add_button_listener("normalButton", OnNormalButton);
        add_button_listener("difficultButton", OnDifficultButton);
        add_button_listener("diyButton",OnDiyButton);
        add_inputfield_listener("InputFieldLength", OnWidthInput);
        add_inputfield_listener("InputFieldWidth", OnLengthInput);
    }
    void ChangeInputActive()
    {
        this.view["InputFieldLength"].gameObject.SetActive(!this.view["InputFieldLength"].gameObject.activeSelf);
        this.view["InputFieldWidth"].gameObject.SetActive(this.view["InputFieldLength"].gameObject.activeSelf);
    }
    // Update is called once per frame
    void SetChildUIActive(bool bl)
    {
        this.view["easyButton"].gameObject.SetActive(bl);
        this.view["normalButton"].gameObject.SetActive(bl);
        this.view["difficultButton"].gameObject.SetActive(bl);
        this.view["diyButton"].gameObject.SetActive(bl);
        
    }
    void OnStartMazeButton()
    {
        //Debug.Log("开始迷宫游戏按钮被点击");
        Eventmanager.Instance.Emit("CreateMaze",this.creatCtrl);
        UImanager.Instance.RemoveUIview("mazeUI");
        GameplayManager.Instance.TurnToMainUI();
        UImanager.Instance.RemoveUIview("mainUI");
        GameplayManager.Instance.TurnToGamingUI();
        UImanager.Instance.ShowUIView("gamingUI");
    }
    void OnDifficultyButton()
    {
        SetChildUIActive(!this.view["easyButton"].gameObject.activeSelf);
    }
    void OnEasyButton()
    {
        MazeGame.Instance.generator.width = 10;
        MazeGame.Instance.generator.length = 10;
        MazeGame.Instance.currentDifficulty = MazeGame.Difficulty.Easy;
    }
    void OnNormalButton()
    {
        MazeGame.Instance.generator.width = 20;
        MazeGame.Instance.generator.length = 20;
        MazeGame.Instance.currentDifficulty = MazeGame.Difficulty.Normal;

    }
    void OnDifficultButton()
    {
        MazeGame.Instance.generator.width = 50;
        MazeGame.Instance.generator.length = 50;
        MazeGame.Instance.currentDifficulty = MazeGame.Difficulty.Hard;
    }
    void OnDiyButton()
    {
        //this.view["InputFieldLength"].gameObject.SetActive(true);
        //this.view["InputFieldWidth"].gameObject.SetActive(true);
        ChangeInputActive();
        MazeGame.Instance.generator.width = diywidth;
        MazeGame.Instance.generator.length = diylength;
    }
    void OnWidthInput(string t)
    {
        diywidth=int.Parse(t);
        //Debug.Log($"输入结束：{diywidth}");
    }
    void OnLengthInput(string t)
    {
        diylength=int.Parse(t);
        //Debug.Log($"输入结束：{diylength}");
    }
}
