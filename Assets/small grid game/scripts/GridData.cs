using System.Collections.Generic;
using UnityEngine;

public class GridCell
{
    public Vector2Int gridPos;//网格坐标
    public bool isOccupied;//此格是否被建筑占用
    public Building buildingInstance;//占据本格建筑信息
    public List<Vector2Int> connectedCells;// 连接到的格子
}
