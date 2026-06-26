using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class gamelanch : UnitySingleton<gamelanch>
{
    // Start is called before the first frame update
    public override void Awake()
    {
        base.Awake();

        // 初始化游戏框架:
        this.gameObject.AddComponent<Resourcemanger>();
        this.gameObject.AddComponent<Eventmanager>();
        this.gameObject.AddComponent<UImanager>();
        this.gameObject.AddComponent<GameplayManager>();
        this.gameObject.AddComponent<SaveManager>();
        // end

        // 初始化游戏逻辑
        //this.gameObject.AddComponent<GameApp>().Init();
        // end
    }

    void Start()
    {
        StartCoroutine(DelayedInit());
    }
    IEnumerator DelayedInit()
    {
        yield return null; // 先显示主菜单
        UImanager.Instance.ShowUIView("mainUI");
        yield return new WaitForEndOfFrame(); // 再执行耗时操作
                                              // 原先的迷宫管理器创建等移到此处
        UImanager.Instance.ShowUIView("hudUI");
        UImanager.Instance.ShowUIView("dialogUI");
        GameObject mzm = new GameObject();
        mzm.name = "MazeManager";
        mzm.AddComponent<MazeGenerator>();
        mzm.AddComponent<MazeGame>();
        Instantiate(mzm);
        GetComponent<GameplayManager>().gameStatus = GameplayManager.GameStatus.InUI;
        Eventmanager.Instance.AddListener("Destroy", DestroyGamePre);
        // 其他UI按需加载
    }
    public void DestroyGamePre(string a, object b) 
    {
        if (b is bool c)
        {
            if (c) 
            {
                GameRD("GamingObj");
                PlayerRD();
            }

        }
    }
    public void GameRD(string tag)
    {
        GameObject[] gamers = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject gamer in gamers)
        {
            Destroy(gamer);
        }
    }
    public void PlayerRD()
    {
        Destroy(GameObject.FindGameObjectWithTag("Player"));
    }
    public void MazeGameRL()
    {

    }
    public void MazeGameRD()
    {

    }

}
