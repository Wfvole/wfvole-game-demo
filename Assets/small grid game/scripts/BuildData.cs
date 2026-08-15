using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "Game/BuildingData")]
public class BuildingData : ScriptableObject
{
    public string buildingName;
    public GameObject prefab;
    public Vector2Int size = Vector2Int.one;      // 占用网格大小（如 2x2）
    public BuildingType type;
    public ResourceType useResourceType;
    [Tooltip("消耗量 consume/s")]
    public int consume = 0;
    public float buildTime = 0f;                  // 0=即时建造
    public bool canConnect = false;                // 是否可连接
    public string introduction;
    // 生产/消耗参数...
    public float lightRate = 0f;      // 每秒光能变化
    public float lifeRate = 0f;       // 每秒生命能变化

    public float buildCostLight = 0f;
    public float buildCostLife = 0f;
    public float buildingMaxHealth = 100f;

}

public enum BuildingType { Collector, Converter, Storage, Defense,Null }
public enum ResourceType { 光能 , 生命能}