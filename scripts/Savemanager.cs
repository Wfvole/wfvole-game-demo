using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UIElements;

public class SaveManager : UnitySingleton<SaveManager>
{

    [System.Serializable]
    public class PlayData
    {
        public string playerModel;
        public Vector3 playerPos;
        public Quaternion playerRot;
        public ThirdPersonShooterController.GameMode gameMode;
        public string weaponName;
        public bool iswear;
        public int mazeWidth;
        public int mazeLength;
        public Vector3 exitPos;
        public Vector3 startPos;
        public Vector3 airWallPos;
        public int[] mazeCells;  // 一维数组，按行存储，0=地板，1=墙壁
        public List<string> readyPlayerModels;
    }
    public class mazegrid // 0 = 地板, 1 = 墙壁
    {
        public int width;
        public int length;
        public int[,] mzgd;
    }
    public PlayData playData { get; private set; }
    private const string SAVE_FILE = "game.sav";
    public override void Awake()
    {
        base.Awake();
        // 初始化数据对象，避免空引用
        playData = new PlayData();

    }
    public WeaponData GetWeaponData(string name)
    {
        string path = "weapon/" + name;
        WeaponData wd= Resourcemanger.Instance.GetAssetCache<GameObject>(path).GetComponentInChildren<PickupWeapon>().weaponData;
        
        return wd;
    }
    public static void SaveToFile(string fileName, PlayData data)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        string json = JsonUtility.ToJson(data, true); // true 表示格式化输出
        File.WriteAllText(path, json);
    }
    public static PlayData LoadFromFile(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (!File.Exists(path)) return null;
        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<PlayData>(json);
    }
    public void LoadPlayDate(string s)//仅作特殊引用
    {
        playData= LoadFromFile(SAVE_FILE);
        if (playData.mazeCells == null) Debug.Log("数组为空"); ;
        switch (playData.gameMode) 
        {
            case ThirdPersonShooterController.GameMode.Paradise:
                string path = "Scene/浮光乐园";
                GameObject testT = Resourcemanger.Instance.GetAssetCache<GameObject>(path);
                testT.tag = "GamingObj";
                Instantiate(testT, new Vector3(-1.25f, -4.5999999f, 13.0299997f), Quaternion.identity);
                //实例化玩家
                string pn = playData.playerModel;
                path = "Player/"+pn;
                GameObject playerPrefab = Resourcemanger.Instance.GetAssetCache<GameObject>(path);
                Instantiate(playerPrefab, new Vector3(-1.25f, -4.5999999f, 12.5900002f), Quaternion.identity);
                string childUI = GameplayManager.Instance.uiStatus.ToString();
                UImanager.Instance.RemoveUIview(childUI);
                UImanager.Instance.RemoveUIview("mainUI");
                GameplayManager.Instance.TurnToGamingUI();
                UImanager.Instance.ShowUIView("gamingUI");
                ThirdPersonShooterController.Instance.gameMode = playData.gameMode ;
                break;
            case ThirdPersonShooterController.GameMode.maze:
                string pnm = playData.playerModel;
                path = "Player/" + pnm;
                GameObject mazePlayerPrefab = Resourcemanger.Instance.GetAssetCache<GameObject>(path);
                MazeGame.Instance.playerPrefab = mazePlayerPrefab;
                MazeGame.Instance.isReCreat = true;
                MazeGame.Instance.exitCell.position = playData.exitPos;
                MazeGame.Instance.startCell.position = playData.startPos;
                MazeGame.Instance.airWallPos = playData.airWallPos;
                MazeGame.Instance.generator.ReCreatMaze(TurnCtoM(playData.mazeCells, playData.mazeWidth, playData.mazeLength));
                string child1UI = GameplayManager.Instance.uiStatus.ToString();
                UImanager.Instance.RemoveUIview(child1UI);
                UImanager.Instance.RemoveUIview("mainUI");
                GameplayManager.Instance.TurnToGamingUI();
                UImanager.Instance.ShowUIView("gamingUI");
                ThirdPersonShooterController.Instance.gameMode = playData.gameMode;
                break;
        }
        if (playData.iswear)
        {
            WeaponData wd = GetWeaponData(playData.weaponName);
            PlayerWeaponManager.Instance.currentWeapon = wd;
            PlayerWeaponManager.Instance.EquipWeapon(wd);
            ThirdPersonShooterController.Instance.weapon = wd.weapontype;
        }
        StartCoroutine(LoadPos());
        GameplayManager.Instance.readyPlayerModels=playData.readyPlayerModels;
    }
    IEnumerator LoadPos()
    {
        yield return null;//等载入完成
        yield return new WaitForEndOfFrame();
        ThirdPersonShooterController.Instance.playerTransform.position = playData.playerPos;
        ThirdPersonShooterController.Instance.playerTransform.rotation = playData.playerRot;
        
    }
    public void LoadPlayDate()
    {

        playData = LoadFromFile(SAVE_FILE);
        ThirdPersonShooterController.Instance.playerTransform.position = playData.playerPos;
        ThirdPersonShooterController.Instance.playerTransform.rotation = playData.playerRot;
        ThirdPersonShooterController.Instance.gameMode=playData.gameMode;
        
        if (playData.iswear)
        {
            WeaponData wd = GetWeaponData(playData.weaponName);
            PlayerWeaponManager.Instance.currentWeapon = wd;
            PlayerWeaponManager.Instance.EquipWeapon(wd);
            ThirdPersonShooterController.Instance.weapon = wd.weapontype;
        }
    }
    public void SavePlayDate()
    {
        //LogSavePath();
        // 将当前游戏状态填入
        playData.playerModel = ThirdPersonShooterController.Instance.playerModel.ToString();
        playData.playerPos = ThirdPersonShooterController.Instance.playerTransform.position;
        playData.playerRot = ThirdPersonShooterController.Instance.playerTransform.rotation;
        playData.gameMode = ThirdPersonShooterController.Instance.gameMode;
        playData.iswear = PlayerWeaponManager.Instance.iswear;
        playData.readyPlayerModels=GameplayManager.Instance.readyPlayerModels;
        if(playData.iswear)
            playData.weaponName= PlayerWeaponManager.Instance.currentWeapon.weaponName;
        if (MazeGame.Instance.generator.grid!=null) 
        {
            mazegrid zs = ExtractMazeGridData(MazeGame.Instance.generator.grid);
            playData.mazeCells = TurnMtoC(zs);
            playData.mazeLength=zs.length;
            playData.mazeWidth = zs.width;
            playData.exitPos = MazeGame.Instance.exitCell.position;
            playData.startPos = MazeGame.Instance.startCell.position;
            playData.airWallPos=MazeGame.Instance.airWallPos;
        }
        // ... 其他数据填充
        SaveToFile(SAVE_FILE, playData);
    }
    public static mazegrid ExtractMazeGridData(MazeGrid source)
    {
        if (source == null || source.grid == null) return null;

        int rows = source.grid.GetLength(0);
        int cols = source.grid.GetLength(1);
        // 注意：MazeGrid 中 width 和 length 是房间数，而 grid 尺寸是 2*length+1, 2*width+1
        // 这里我们直接使用实际网格尺寸
        mazegrid result = new mazegrid();
        result.width = source.width;     // 保存房间尺寸，也可不保存
        result.length = source.length;
        result.mzgd = new int[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                MazeGrid.Cell cell = source.grid[r, c];
                // 根据你的 CellType 枚举转换
                result.mzgd[r, c] = (cell.type == MazeGrid.CellType.Floor) ? 0 : 1;
            }
        }
        return result;
    }
    public int[] TurnMtoC(mazegrid mg )
    {
        int[,] dT=mg.mzgd;
        int[] Cells = new int[(2 * mg.length + 1)* (2 * mg.width + 1)];
        for (int r = 0; r < (2 * mg.length + 1); r++)
        {
            for (int c = 0; c < (2 * mg.width + 1); c++)
            {
                Cells[r * (2 * mg.width + 1) + c] = dT[r, c];
            }
        }
        return Cells;
    }
    public int[,] TurnCtoM(int[] cells, int width, int length)
    {
        int rows = 2 * length + 1;
        int cols = 2 * width + 1;
        if (cells.Length != rows * cols)
            throw new System.ArgumentException("cells 长度与迷宫尺寸不匹配");

        int[,] result = new int[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                result[r, c] = cells[r * cols + c];
            }
        }
        return result;
    }
    public void LogSavePath()
    {
        // Application.persistentDataPath 就是 Unity 自动分配的路径
        string fullSavePath = System.IO.Path.Combine(Application.persistentDataPath, "game.sav");
        Debug.Log($"存档的完整路径是: {fullSavePath}");
    }

}
