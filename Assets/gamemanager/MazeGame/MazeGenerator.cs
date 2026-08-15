using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MazeGenerator : MonoBehaviour
{
    public enum Algorithm
    {
        Prim,
        RecursiveBacktrack
    }

    public Algorithm algorithm = Algorithm.Prim;
    public int width = 10;
    public int length = 10;
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public bool generateOnStart = false;
    public bool generateListen = false;
    public int generateSpeed = 10;
    public UnityEngine.Events.UnityEvent OnGenerationFinished;

    public MazeGrid grid; // get; private set; }

    void Start()
    {
        if (generateOnStart)
            StartCoroutine(GenerateMaze());
        else
        {
            Eventmanager.Instance.AddListener("CreateMaze", GetCreateMazeBool);
        }
    }
    public void ReCreatMaze(int[,] mg)
    {
        GameObject gridObj = new GameObject("MazeGrid");
        grid = gridObj.AddComponent<MazeGrid>();
        grid.width = width;
        grid.length = length;
        grid.floorPrefab = floorPrefab;
        grid.wallPrefab = wallPrefab;
        gridObj.tag = "mazegb";
        grid.Initialize(mg);
        OnGenerationFinished?.Invoke();
    }
    void  GetCreateMazeBool(string eventName, object udata) 
    {
        if (udata is bool c)
        {
            generateListen = c;
            if (generateListen)
            {
                StartCoroutine(GenerateMaze());
                generateListen = false;
                //Eventmanager.Instance.AddListener("CreateMaze", GetCreateMazeBool);
            }
        }
    }

    public IEnumerator GenerateMaze()
    {
        // 创建网格对象
        GameObject gridObj = new GameObject("MazeGrid");
        grid = gridObj.AddComponent<MazeGrid>();
        grid.width = width;
        grid.length = length;
        grid.floorPrefab = floorPrefab;
        grid.wallPrefab = wallPrefab;
        grid.Initialize();
        grid.tag="mazegb";

        // 根据所选算法运行生成协程
        switch (algorithm)
        {
            case Algorithm.Prim:
                yield return StartCoroutine(PrimGenerator());
                break;
            case Algorithm.RecursiveBacktrack:
                yield return StartCoroutine(RecursiveBacktrackGenerator());
                break;
        }

        //Debug.Log("Maze generation finished.");
    }

    // ---------- Prim 算法 ----------
    private IEnumerator PrimGenerator()
    {
        // 收集所有地板单元格
        List<MazeGrid.Cell> allFloors = new List<MazeGrid.Cell>();
        for (int y = 0; y < length; y++)
            for (int x = 0; x < width; x++)
                allFloors.Add(grid.GetRoomCell(x, y));

        // 随机选起点
        int startIdx = Random.Range(0, allFloors.Count);
        MazeGrid.Cell start = allFloors[startIdx];
        start.visited = true;
        allFloors.RemoveAt(startIdx);

        List<MazeGrid.Cell> frontier = grid.GetFloorNeighbors(start, includeUnvisitedOnly: true);

        int generateNum = 0;
        while (allFloors.Count > 0)
        {
            if (frontier.Count == 0) break; // 安全退出
            generateNum++;
            // 随机选一个 frontier 单元格
            int idx = Random.Range(0, frontier.Count);
            MazeGrid.Cell current = frontier[idx];
            frontier.RemoveAt(idx);

            // 找一个已访问的邻居（确保至少有一个）
            var visitedNeighbors = grid.GetFloorNeighbors(current, includeUnvisitedOnly: false)
                                        .FindAll(cell => cell.visited);
            if (visitedNeighbors.Count > 0)
            {
                MazeGrid.Cell neighbor = visitedNeighbors[Random.Range(0, visitedNeighbors.Count)];
                grid.CarvePath(current, neighbor);
                current.visited = true;
                allFloors.Remove(current);

                // 将 current 的未访问邻居加入 frontier
                var newFrontier = grid.GetFloorNeighbors(current, includeUnvisitedOnly: true);
                foreach (var cell in newFrontier)
                    if (!frontier.Contains(cell))
                        frontier.Add(cell);
            }
            if (generateNum % generateSpeed == 0)
            {
                yield return null; // 初始设置为每帧一步，便于观察
            }
            
        }
        OnGenerationFinished?.Invoke();
    }

    // ---------- 递归回溯（深度优先）算法 ----------
    private IEnumerator RecursiveBacktrackGenerator()
    {
        // 随机选起点
        int startX = Random.Range(0, width);
        int startY = Random.Range(0, length);
        MazeGrid.Cell start = grid.GetRoomCell(startX, startY);
        start.visited = true;

        Stack<MazeGrid.Cell> stack = new Stack<MazeGrid.Cell>();
        stack.Push(start);

        while (stack.Count > 0)
        {
            MazeGrid.Cell current = stack.Peek();
            var unvisitedNeighbors = grid.GetFloorNeighbors(current, includeUnvisitedOnly: true);

            if (unvisitedNeighbors.Count > 0)
            {
                MazeGrid.Cell next = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];
                grid.CarvePath(current, next);
                next.visited = true;
                stack.Push(next);
            }
            else
            {
                stack.Pop();
            }

            yield return null;
        }
    }
}