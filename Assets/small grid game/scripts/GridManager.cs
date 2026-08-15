using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("网格设置")]
    public int width = 5;
    public int height = 5;
    public float cellSize = 1f;
    //public GameObject gridCellPrefab;      // 地板格子预制体（带半透明材质）
    //public BuildingData collectorData;
    //public BuildingData converterData;
    //public BuildingData storageData;
    //public BuildingData defenseData;
    public BuildingData floorData;
    public GridCell[,] Cells { get; private set; }

    void Start()
    {
        
        InitializeGrid();
        DrawGridBorder();
    }
    
    // 绘制网格边界线（可选）
    void DrawGridBorder()
    {
        LineRenderer lr = gameObject.AddComponent<LineRenderer>();
        lr.positionCount = 5;
        lr.loop = true;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.material = new Material(Shader.Find("Sprites/Default")) { color = Color.white };

        Vector3[] corners = new Vector3[4];
        corners[0] = new Vector3(0, 0.01f, 0);
        corners[1] = new Vector3(width * cellSize, 0.01f, 0);
        corners[2] = new Vector3(width * cellSize, 0.01f, height * cellSize);
        corners[3] = new Vector3(0, 0.01f, height * cellSize);
        lr.SetPositions(corners);
    }
    /// <summary>
    /// 初始化网格：创建数据 + 实例化视觉格子
    /// </summary>
    void InitializeGrid()
    {
        Cells = new GridCell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector2Int pos = new Vector2Int(x, z);
                if (floorData == null)
                {
                    Debug.Log("floorData为空，初始化失败");
                    return;
                }
                Vector3 worldPos = GridToWorld(pos);
                GameObject cellObj = Instantiate(floorData.prefab, worldPos, Quaternion.identity, transform);
                cellObj.name = $"Grid_{x}_{z}";
                // 创建数据
                Cells[x, z] = new GridCell
                {
                    gridPos = pos,
                    isOccupied = false,
                    //buildingData = floorData,
                    buildingInstance = null,
                    connectedCells = new List<Vector2Int>()
                };
            }
        }
    }

    // ============ 坐标转换 ============
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        float x = gridPos.x * cellSize + cellSize * 0.5f;
        float z = gridPos.y * cellSize + cellSize * 0.5f;
        return new Vector3(x, 0, z);
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / cellSize);
        int z = Mathf.FloorToInt(worldPos.z / cellSize);
        return new Vector2Int(Mathf.Clamp(x, 0, width - 1), Mathf.Clamp(z, 0, height - 1));
    }

    // ============ 边界检查 ============
    public bool IsInsideGrid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    // ============ 放置检查 ============
    public bool CanPlaceBuilding(Vector2Int pos, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector2Int check = pos + new Vector2Int(x, z);
                if (!IsInsideGrid(check)) return false;
                if (Cells[check.x, check.y].isOccupied) return false;
            }
        }
        return true;
    }

    // ============ 放置建筑 ============
    public Vector3 FixBuildingPos(Vector3 v, BuildingData data)
    {
        switch (data.size)
        {
            case Vector2Int { x: 1, y: 1 }:
                return v;
            case Vector2Int { x: 2, y: 2 }:
                return v+Vector3.right*0.5f+Vector3.forward*0.5f;
            case Vector2Int { x: 3, y: 3 }:
                return v+Vector3.right + Vector3.forward;
            default : return v;
        }
    }
    public void PlaceBuilding(Vector2Int pos, BuildingData data)
    {
        // 1. 先检查是否可以放置
        if (!CanPlaceBuilding(pos, data.size)) return;

        // 2. 实例化建筑（只实例化一次）
        //Vector3 worldPos = GridToWorld(pos); // 建筑左下角对齐到格子中心
        Vector3 worldPos = FixBuildingPos(GridToWorld(pos), data);
        GameObject instance = Instantiate(data.prefab, worldPos+Vector3.up*0.5f, Quaternion.identity);
        instance.name= data.name;
        //更新格子被何建筑占据信息
        Vector2Int size = data.size;
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector2Int cellPos = pos + new Vector2Int(x, z);
                Cells[cellPos.x, cellPos.y].isOccupied = true;
                //Cells[cellPos.x, cellPos.y].buildingData = data;
                //Cells[cellPos.x, cellPos.y].buildingData.prefab;
                if(instance.TryGetComponent<Building>(out Building b)) Cells[cellPos.x, cellPos.y].buildingInstance = b;
            }
        }

    }

    // ============ 移除建筑 ============
    public void RemoveBuilding(Building instance)
    {
        for (int x = 0; x <width ; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector2Int cellPos =new Vector2Int(x, z);
                if (Cells[cellPos.x, cellPos.y].buildingInstance == instance) 
                {
                    Cells[cellPos.x, cellPos.y].isOccupied = false;
                    Cells[cellPos.x, cellPos.y].buildingInstance= null;
                } 
            }
        }
    }

    // ============ 获取格子数据 ============
    public GridCell GetCell(Vector2Int pos)
    {
        if (IsInsideGrid(pos)) return Cells[pos.x, pos.y];
        return null;
    }

    // ============ 添加连接 ============
    public void AddConnection(Vector2Int from, Vector2Int to)
    {
        if (IsInsideGrid(from) && IsInsideGrid(to))
        {
            if (!Cells[from.x, from.y].connectedCells.Contains(to))
                Cells[from.x, from.y].connectedCells.Add(to);
            if (!Cells[to.x, to.y].connectedCells.Contains(from))
                Cells[to.x, to.y].connectedCells.Add(from);
        }
    }

    // ============ 移除连接 ============
    public void RemoveConnection(Vector2Int from, Vector2Int to)
    {
        if (IsInsideGrid(from))
            Cells[from.x, from.y].connectedCells.Remove(to);
        if (IsInsideGrid(to))
            Cells[to.x, to.y].connectedCells.Remove(from);
    }

    // ============ 获取所有建筑实例（用于资源循环） ============
    public List<Building> GetAllBuildings()
    {
        List<Building> result = new();
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                if (Cells[x, z].isOccupied && Cells[x, z].buildingInstance != null)
                {
                    Building b = Cells[x, z].buildingInstance.GetComponent<Building>();
                    if (b != null) result.Add(b);
                }
            }
        }
        return result;
    }
}