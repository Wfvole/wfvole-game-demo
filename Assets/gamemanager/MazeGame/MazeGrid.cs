using UnityEngine;
using System.Collections.Generic;

public class MazeGrid : MonoBehaviour
{
    public int width;          // 实际房间数（列）
    public int length;         // 实际房间数（行）
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public Dictionary<Cell, (int,int)> cellView;

    [System.Serializable]
    public class Cell
    {
        public Vector3 position;
        public CellType type;
        public bool visited;          //是否被访问过
        public GameObject obj;        // 对应的游戏对象
        // 用于 Eller 算法的附加数据（可选）
        public int setID = -1;

        public Cell(Vector3 pos, CellType t)
        {
            position = pos;
            type = t;
            visited = false;
        }
        
    }

    public enum CellType { Floor, Wall }
    public Cell[,] grid;   // 二维网格，大小为 [2*length+1, 2*width+1]

    // 初始化网格并实例化预制体
    public void Initialize()
    {
        int rows = 2 * length + 1;
        int cols = 2 * width + 1;
        grid = new Cell[rows, cols];
        cellView = new Dictionary<Cell, (int, int)>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 pos = new Vector3(c, 0, r);
                CellType type = (r % 2 == 1 && c % 2 == 1) ? CellType.Floor : CellType.Wall;
                Cell cell = new Cell(pos, type);
                cell.obj = Instantiate(type == CellType.Floor ? floorPrefab : wallPrefab, pos, Quaternion.identity, transform);
                grid[r, c] = cell;
                cellView[cell] = (r,c);//在字典中添加索引
            }
        }
    }

    // 索引有效性检查
    public bool IsValidIndex(int r, int c) => r >= 0 && r < grid.GetLength(0) && c >= 0 && c < grid.GetLength(1);

    // 获取单元格
    public Cell GetCell(int r, int c) => IsValidIndex(r, c) ? grid[r, c] : null;

    // 根据实际房间坐标获取地板单元格（房间坐标从0到width-1, 0到length-1）
    public Cell GetRoomCell(int x, int y) => GetCell(2 * y + 1, 2 * x + 1);

    // 获取相邻的地板单元格（上下左右，步长为2）
    public List<Cell> GetFloorNeighbors(Cell cell, bool includeUnvisitedOnly = true)
    {
        var neighbors = new List<Cell>();
        (int r, int c) = FindCellIndex(cell);
        Vector2Int[] dirs = { new Vector2Int(2, 0), new Vector2Int(-2, 0), new Vector2Int(0, 2), new Vector2Int(0, -2) };
        foreach (var dir in dirs)
        {
            int nr = r + dir.x;
            int nc = c + dir.y;
            if (IsValidIndex(nr, nc) && grid[nr, nc].type == CellType.Floor)
            {
                if (!includeUnvisitedOnly || !grid[nr, nc].visited)//是否仅包含问未访问单元为否，或此单元格没被访问过时执行
                    neighbors.Add(grid[nr, nc]);
            }
        }
        return neighbors;
    }

    // 打通两个地板单元格之间的路径（假设它们同行或同列且相距2）
    public void CarvePath(Cell from, Cell to)
    {
        (int r1, int c1) = FindCellIndex(from);
        (int r2, int c2) = FindCellIndex(to);
        // 确定中间墙的坐标
        int midR = (r1 + r2) / 2;
        int midC = (c1 + c2) / 2;
        Cell mid = grid[midR, midC];
        if (mid.type == CellType.Wall)
        {
            ChangeCellType(mid, CellType.Floor);
        }
    }

    // 改变单元格类型并更新对象
    public void ChangeCellType(Cell cell, CellType newType)
    {
        if (cell.type == newType) return;
        cell.type = newType;
        Destroy(cell.obj);
        cell.obj = Instantiate(newType == CellType.Floor ? floorPrefab : wallPrefab, cell.position, Quaternion.identity, transform);
    }

    // 字典查找单元格的索引
    public (int, int) FindCellIndex(Cell target)
    {
        if(cellView.TryGetValue(target, out var index))
        {
            return index;
        }
        return (-1, -1);
    }
    public List<Cell> GetMazeFloorNeighbors(Cell cell, bool includeUnvisitedOnly = true)
    {
        var neighbors = new List<Cell>();
        (int r, int c) = FindCellIndex(cell);
        Vector2Int[] dirs = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
        foreach (var dir in dirs)
        {
            int nr = r + dir.x;
            int nc = c + dir.y;
            if (IsValidIndex(nr, nc) && grid[nr, nc].type == CellType.Floor)
            {
                if (!includeUnvisitedOnly || !grid[nr, nc].visited)//是否仅包含问未访问单元为否，或此单元格没被访问过时执行
                    neighbors.Add(grid[nr, nc]);
            }
        }
        return neighbors;
    }
    public void Initialize(int[,] mg)
    {
        int rows = 2 * length + 1;
        int cols = 2 * width + 1;
        grid = new Cell[rows, cols];
        cellView = new Dictionary<Cell, (int, int)>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 pos = new Vector3(c, 0, r);
                CellType type = (mg[r,c]==0) ? CellType.Floor : CellType.Wall;
                Cell cell = new Cell(pos, type);
                cell.obj = Instantiate(type == CellType.Floor ? floorPrefab : wallPrefab, pos, Quaternion.identity, transform);
                grid[r, c] = cell;
                cellView[cell] = (r, c);//在字典中添加索引
            }
        }
    }
}