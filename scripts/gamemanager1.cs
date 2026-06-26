using System;
using UnityEngine;
using System.Collections.Generic;

public class GameplayManager : UnitySingleton<GameplayManager>
{
    // ========== ÓÎÏ·×´Ì¬¹ÜÀí (×´Ì¬»ú) ==========
    public enum PlayerModel
    {
        Ñý¾«°®Àò,
        Ð¡¿­Èö
    }
    public enum GameStatus
    {
        InGame,InUI,InPause
    }
    public enum UIStatus
    {
        mainUI,startUI,settingUI,pauseUI,mazeUI,gamingUI,bagUI,taskUI
    }
    public PlayerModel playerModel;
    public GameStatus gameStatus=GameStatus.InUI;
    public UIStatus uiStatus=UIStatus.mainUI;
    public List<string> readyPlayerModels;
    private void Start()
    {
        readyPlayerModels = new List<string>();
        Eventmanager.Instance.AddListener("ListenPause",ChangePause);
    }
    void ChangePause(string eventName, object udata)
    {
        if (udata is bool b)
        {
            if (b)
            {
                Time.timeScale = 0f;                // ÔÝÍ£ÓÎÏ·Âß¼­
                string ui = "pauseUI";
                UImanager.Instance.ShowUIView(ui);
                TurnToPauseUI();
            }
            else
            {
                Time.timeScale = 1f;                // »Ö¸´
                string ui = uiStatus.ToString();
                if (ui != "gamingUI")UImanager.Instance.RemoveUIview(ui); 
                TurnToGamingUI();
            }
        }
    }

    GameStatus CheckGameStatus()
    {
        return gameStatus;
    }
    UIStatus CheckUIStatus() 
    {
        return uiStatus;
    }

    public void TurnToMainUI() 
    {
        Time.timeScale = 1f;
        if (gameStatus == GameStatus.InGame) return;
        uiStatus = UIStatus.mainUI;
    }
    public void TurnToStartUI()
    {
        Time.timeScale = 1f;
        if (gameStatus == GameStatus.InGame) return;
        uiStatus = UIStatus.startUI;
    }
    public void TurnToSettingUI()
    {
        if (gameStatus == GameStatus.InGame) return;
        uiStatus = UIStatus.settingUI;
    }
    public void TurnToPauseUI()
    {
        if (gameStatus == GameStatus.InUI) return;
        uiStatus = UIStatus.pauseUI;
        gameStatus = GameStatus.InPause;
    }
    public void TurnToMazeUI()
    {
        Time.timeScale = 1f;
        if (gameStatus == GameStatus.InGame) return;
        uiStatus = UIStatus.mazeUI;//,,
    }
    public void TurnToGamingUI()
    {
        Time.timeScale = 1f;
        if (gameStatus == GameStatus.InGame) return;
        uiStatus = UIStatus.gamingUI;
        gameStatus = GameStatus.InGame;
    }
    public void TurnToBagUI()
    {
        if (gameStatus == GameStatus.InGame) return;
        uiStatus = UIStatus.bagUI;
    }
    public void TurnToTaskUI()
    {
        if (gameStatus == GameStatus.InGame) return;
        uiStatus = UIStatus.taskUI;
    }
}