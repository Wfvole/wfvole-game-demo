using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class MazeGame : UnitySingleton<MazeGame>
{
    public GameObject staffPrefab;
    [Header("迷宫守卫预制体")]
    public GameObject guardPrefab;
    [Header("加速道具预制体")]
    public GameObject speedBoostPrefab;       //
    public MazeGenerator generator;          // 迷宫生成器
    public GameObject playerPrefab;          // 玩家预制体
    public GameObject exitMarker;            // 出口标记（可选）
    public MazeGrid.Cell startCell;
    public MazeGrid.Cell exitCell;
    public Vector3 airWallPos;
    public bool isReCreat=false;
    public bool initialized = false;
    public enum Difficulty 
    { 
        Easy,
        Normal,
        Hard
    }
    public Difficulty currentDifficulty=Difficulty.Easy;
    void Start()
    {
        if (initialized) return;
        initialized = true;
        if (generator == null)
        {
            //Debug.LogError("MazeGenerator not assigned!");
            generator=transform.GetComponent<MazeGenerator>();
            if (generator.floorPrefab ==null)//通过资源管理器添加地格预制体
            {
                string path = "Maze/floor";
                //generator.floorPrefab = Resourcemanger.Instance.GetAssetCache<GameObject>(path);
                Resourcemanger.Instance.LoadAssetAsync<GameObject>(path, prefab =>    //异步加载
                {
                    generator.floorPrefab = prefab;
                });
            }
            if (generator.wallPrefab == null)//通过资源管理器添加地格预制体
            {
                string path = "Maze/wall";
                //generator.wallPrefab = Resourcemanger.Instance.GetAssetCache<GameObject>(path);
                Resourcemanger.Instance.LoadAssetAsync<GameObject>(path, prefab =>    //异步加载
                {
                    generator.wallPrefab = prefab;
                });
            }
            //return;
        }
        if (playerPrefab==null)
        {
            string path = "Player/妖精爱莉";
            //playerPrefab = Resourcemanger.Instance.GetAssetCache<GameObject>(path);
            Resourcemanger.Instance.LoadAssetAsync<GameObject>(path, prefab =>    //异步加载试验
            {
                playerPrefab = prefab;
                //Debug.Log("资源加载完成，等待玩家实例化");
            });
            
        }
        if (exitMarker==null)
        {
            string path = "Maze/mask";
            //exitMarker = Resourcemanger.Instance.GetAssetCache<GameObject>(path);
            Resourcemanger.Instance.LoadAssetAsync<GameObject>(path, prefab =>    //异步加载
            {
                exitMarker = prefab;
            });
        }
        if (speedBoostPrefab==null)
        {
            string path = "Maze/加速骰子";
            //speedBoostPrefab= Resourcemanger.Instance.GetAssetCache<GameObject>(path);
            Resourcemanger.Instance.LoadAssetAsync<GameObject>(path, prefab =>    //异步加载
            {
                speedBoostPrefab = prefab;
            });
        }
        if (guardPrefab == null)
        {
            string path = "Maze/guard";
            //guardPrefab = Resourcemanger.Instance.GetAssetCache<GameObject>(path);
            Resourcemanger.Instance.LoadAssetAsync<GameObject>(path, prefab =>    //异步加载
            {
                guardPrefab = prefab;
            });
        }
        if (staffPrefab == null)
        {
            string path = "weapon/妖精法杖";
            //staffPrefab = Resourcemanger.Instance.GetAssetCache<GameObject>(path);
            Resourcemanger.Instance.LoadAssetAsync<GameObject>(path, prefab =>    //异步加载
            {
                staffPrefab = prefab;
            });
        }
        // 订阅生成完成事件
        generator.OnGenerationFinished.AddListener(Setup);
        //订阅出口传递的通关信号
        Eventmanager.Instance.AddListener("Mazepass",NewMaze);
    }
    void NewMaze(string a,object b)
    {
        if(b is bool o)
        {
            if (o)
            {
                UImanager.Instance.DialogTextMgr("即将进入下一关,难度提升");
                Eventmanager.Instance.Emit("Destroy", true);//发送销毁信号给MazeGame和gamelanch
                generator.width += 5;
                generator.length += 5;
                if (generator.length + generator.width > 20) currentDifficulty = Difficulty.Normal;
                else if (generator.length + generator.length > 40) currentDifficulty = Difficulty.Hard;
                Eventmanager.Instance.Emit("CreateMaze",true);
                UImanager.Instance.DialogTextMgr("进入新迷宫");
                UImanager.Instance.HidePickupTip();
                UImanager.Instance.InitTLJ();
                Setup();
            }
        }
    }
    void Setup()
    {
        MazeGrid grid = generator.grid;
        int width = grid.width;
        int length = grid.length;
        if (!isReCreat)
        {
            // 随机选择一个边界方向作为出生点方向
            int startSide = UnityEngine.Random.Range(0, 4); // 0:左, 1:右, 2:下, 3:上
            int exitSide = GetOppositeSide(startSide); // 出口在出生点对面边界
                                                       // 获取出生点（边界上的地板单元格）
            startCell = GetRandomBoundaryFloor(grid, startSide);
            // 获取出口（对面边界上的墙单元格）
            exitCell = GetRandomBoundaryWallWithAdjacentFloor(grid, exitSide, 100);
            // 确保出口是墙（理论上都是墙，但为了安全，如果不是则重新随机）
            int maxAttempts = 100;
            while (exitCell.type != MazeGrid.CellType.Wall && maxAttempts-- > 0)
            {
                exitCell = GetRandomBoundaryWall(grid, exitSide);
            }
            if (exitCell.type != MazeGrid.CellType.Wall)
            {
                Debug.LogError("Failed to find a wall on the exit side!");
                return;
            }
            // 将出口墙变为地板
            grid.ChangeCellType(exitCell, MazeGrid.CellType.Floor);
            //出口外围添加空气墙并实例化
            Vector3 d = Vector3.zero;
            switch (exitSide)
            {
                case 0: d = new Vector3(-1f, 0f, 0f); break; // 左
                case 1: d = new Vector3(1f, 0f, 0f); break; // 右
                case 2: d = new Vector3(0f, 0f, -1f); ; break; // 下
                case 3: d = new Vector3(0f, 0f, 1f); ; break; // 上
                default: break;
            }
            airWallPos=exitCell.position + d;  
        }
        //实例化空气墙
        GameObject airwall = Instantiate(grid.wallPrefab, airWallPos, Quaternion.identity);
        airwall.tag = "mazegb";
        MeshRenderer amwr = airwall.GetComponent<MeshRenderer>();
        if (amwr != null) amwr.enabled = false;
        // 实例化玩家
        if (playerPrefab != null)
        {
            Instantiate(playerPrefab, startCell.position + Vector3.up * 0.5f, Quaternion.identity);
        }
        // 实例化出口标记
        if (exitMarker != null)
        {
            GameObject em= Instantiate(exitMarker, exitCell.position + Vector3.up * 0.5f, Quaternion.identity);
            em.AddComponent<MazePass>();
            em.tag = "mazegb";
        }
        if (guardPrefab != null)
        {
            GuardGenerate();
        }
        if(staffPrefab != null)
        {
            WeaponGen();
        }
        //Debug.Log($"Game setup: Start at {startCell.position}, Exit at {exitCell.position}");
        int boostCount = GetBoostCountByDifficulty();
        SpawnSpeedBoosts(boostCount);//实例化生成加速道具
        Eventmanager.Instance.AddListener("Destroy", DestroyPre);//信号来源：1.settingUICtrl；2.
        isReCreat = false;
    }
    void WeaponGen()
    {
        List<MazeGrid.Cell> ls = new List<MazeGrid.Cell>();
        ls = GetAllFloorCellsExcludingStartAndExit(generator.grid);
        Vector3 pw = ls[Random.Range(0, ls.Count)].position;
        GameObject staff= Instantiate(staffPrefab, pw, Quaternion.identity);
        staff.tag = "mazegb";
    }
    void GuardGenerate()
    {
        int guardCount=GetGuardCountByDifficulty();
        for (int i=0; i<guardCount; i++)
        {
            List<Vector3> patrolPath = GetRandomPatrolPath(15);
            GameObject guard = Instantiate(guardPrefab, patrolPath[0], Quaternion.identity);
            guard.tag = "mazegb";
            GuardPatrol patrol = guard.AddComponent<GuardPatrol>();
            patrol.waypoints = patrolPath;
            patrol.moveSpeed = 1.5f;
            patrol.pingPong = true;
        }
    }
    private int GetGuardCountByDifficulty()
    {
        // 根据当前难度返回道具数量
        switch (currentDifficulty)
        {
            case Difficulty.Easy: return 0;
            case Difficulty.Normal: return 3;
            case Difficulty.Hard: return 5;
            default: return 0;
        }
    }
    private int GetBoostCountByDifficulty()
    {
        // 根据当前难度返回道具数量
        switch (currentDifficulty)
        {
            case Difficulty.Easy: return 0;
            case Difficulty.Normal: return 3;
            case Difficulty.Hard: return 5;
            default: return 0;
        }
    }
    private void SpawnSpeedBoosts(int count)
    {
        MazeGrid grid = generator.grid;
        // 获取所有地板单元格（排除起点和出口）
        List<MazeGrid.Cell> floorCells = GetAllFloorCellsExcludingStartAndExit(grid);
        if (floorCells.Count == 0) return;
        // 随机打乱floorCell列表顺序
        for (int i = 0; i < floorCells.Count; i++)
        {
            int rand = Random.Range(i, floorCells.Count);
            var temp = floorCells[i];
            floorCells[i] = floorCells[rand];
            floorCells[rand] = temp;
        }
        // 生成道具
        for (int i = 0; i < Mathf.Min(count, floorCells.Count); i++)
        {
            int rand = Random.Range(i, floorCells.Count);
            MazeGrid.Cell cell = floorCells[rand];
            Vector3 pos = cell.position + Vector3.up * 0.5f;
            GameObject sgo=Instantiate(speedBoostPrefab, pos, Quaternion.identity);
            sgo.tag = "mazegb";
            SpeedBoostItem sbi=sgo.GetComponent<SpeedBoostItem>();
            if(sbi==null) sgo.AddComponent<SpeedBoostItem>();
        }
    }

    private List<MazeGrid.Cell> GetAllFloorCellsExcludingStartAndExit(MazeGrid grid)
    {
        List<MazeGrid.Cell> lmgc= new List<MazeGrid.Cell>();
        MazeGrid gd=generator.grid;

        foreach (MazeGrid.Cell cell in gd.grid)
        {
            if (cell.type== MazeGrid.CellType.Floor&&cell.position!=startCell.position&& cell.position != exitCell.position)
            {
                lmgc.Add(cell);
            }
        }
        return lmgc;
    }
    public List<Vector3> GetRandomPatrolPath(int pathLength, int maxBacktracks = 3)
    {
        List<Vector3> result = new List<Vector3>();
        List<MazeGrid.Cell> resultR = new List<MazeGrid.Cell>();
        MazeGrid grid = generator.grid;
        List<MazeGrid.Cell> availableCells = GetAllFloorCellsExcludingStartAndExit(grid);
        if (availableCells.Count == 0) return result;

        resultR.Add(availableCells[Random.Range(0, availableCells.Count)]);
        for (int j = 0; j < pathLength - 1; j++)
        {
            List<MazeGrid.Cell> ners = grid.GetMazeFloorNeighbors(resultR[j], false);
            if (ners.Count == 0)
            {
                // 回溯：尝试移除上一个点，重新选
                int backtrackSteps = 1;
                while (backtrackSteps <= maxBacktracks && j - backtrackSteps >= 0)
                {
                    resultR.RemoveRange(resultR.Count - backtrackSteps, backtrackSteps);
                    j -= backtrackSteps;
                    // 重新获取邻居
                    ners = grid.GetMazeFloorNeighbors(resultR[j], false);
                    // 过滤掉已访问
                    ners.RemoveAll(n => resultR.Contains(n));
                    if (ners.Count > 0) break;
                    backtrackSteps++;
                }
                if (ners.Count == 0) break;
            }
            // 随机选择一个邻居（未访问）
            MazeGrid.Cell a = ners[Random.Range(0, ners.Count)];
            resultR.Add(a);
        }
        for (int i = 0; i < resultR.Count; i++)
            result.Add(resultR[i].position);
        return result;
    }

    void DestroyPre(string a, object b)
    {
        if (b is bool c)
        {
            if (c)
            {
                gamelanch.Instance.GameRD("mazegb");
                gamelanch.Instance.PlayerRD();
                Resourcemanger.Instance.ClearCache();
            }
        }
    }
    // 获取对面边界索引
    private int GetOppositeSide(int side)
    {
        switch (side)
        {
            case 0: return 1; // 左对右
            case 1: return 0; // 右对左
            case 2: return 3; // 下对上
            case 3: return 2; // 上对下
            default: return 0;
        }
    }

    // 在指定边界上随机选择一个地板单元格（房间坐标）
    private MazeGrid.Cell GetRandomBoundaryFloor(MazeGrid grid, int side)
    {
        int width = grid.width;
        int length = grid.length;
        int x, y;
        switch (side)
        {
            case 0: // 左边界
                x = 0;
                y = Random.Range(0, length);
                break;
            case 1: // 右边界
                x = width - 1;
                y = Random.Range(0, length);
                break;
            case 2: // 下边界
                x = Random.Range(0, width);
                y = 0;
                break;
            case 3: // 上边界
                x = Random.Range(0, width);
                y = length - 1;
                break;
            default:
                x = 0; y = 0;
                break;
        }
        return grid.GetRoomCell(x, y);
    }

    // 在指定边界上随机选择一个墙单元格（双倍网格坐标）
    private MazeGrid.Cell GetRandomBoundaryWall(MazeGrid grid, int side)
    {
        int rows = grid.grid.GetLength(0); // 2*length+1
        int cols = grid.grid.GetLength(1); // 2*width+1
        int r, c;
        switch (side)
        {
            case 0: // 左边界：列索引为0
                c = 0;
                r = Random.Range(0, rows);
                break;
            case 1: // 右边界：列索引为cols-1
                c = cols - 1;
                r = Random.Range(0, rows);
                break;
            case 2: // 下边界：行索引为0
                r = 0;
                c = Random.Range(0, cols);
                break;
            case 3: // 上边界：行索引为rows-1
                r = rows - 1;
                c = Random.Range(0, cols);
                break;
            default:
                r = 0; c = 0;
                break;
        }
        return grid.GetCell(r, c);
    }
    // 新增：在指定边界上随机选择一个墙单元格，且其至少有一个邻居地板
    private MazeGrid.Cell GetRandomBoundaryWallWithAdjacentFloor(MazeGrid grid, int side, int maxAttempts)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            MazeGrid.Cell wall = GetRandomBoundaryWall(grid, side);
            if (HasAdjacentFloor(grid, wall))
            {
                return wall;
            }
        }
        return null;
    }

    // 检查墙单元格是否至少有一个相邻地板
    private bool HasAdjacentFloor(MazeGrid grid, MazeGrid.Cell wall)
    {
        // 获取墙的索引
        (int r, int c) = grid.FindCellIndex(wall); // 已经实现了 FindCellIndex 方法（或者用字典缓存）
                                                   // 四个方向：上、下、左、右（双倍网格中，偏移±1）
        Vector2Int[] dirs = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
        foreach (var dir in dirs)
        {
            int nr = r + dir.x;
            int nc = c + dir.y;
            if (grid.IsValidIndex(nr, nc))
            {
                MazeGrid.Cell neighbor = grid.GetCell(nr, nc);
                if (neighbor != null && neighbor.type == MazeGrid.CellType.Floor)
                {
                    return true;
                }
            }
        }
        return false;
    }
}