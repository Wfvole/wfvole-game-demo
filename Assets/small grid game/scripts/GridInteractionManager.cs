using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridInteractionManager : UnitySingleton<GridInteractionManager>
{
    [Header("资源")]
    [Tooltip("供UI读取")]
    public float lightEnergy;
    public float limtLE;
    public float lifePower;
    public float limtLP;
    [Header("建筑管理")]
    [Tooltip("场上建筑（已注册）")]
    public List<Building> registryBuilding;
    public LayerMask buildingLayer;         // 建筑碰撞层（用于点击选中）
    
    [Header("引用")]
    public GridManager gridManager;
    public Camera mainCamera;
    public GameObject previewPrefab;

    [Header("交互状态")]
    public InteractionState currentState = InteractionState.Idle;
    public BuildingData selectedBuilding;
    public Building selectedBuildingInstance;//点击选中的建筑
    public GameObject previewObject;
    public Building startBuilding;              // 连接起始建筑
    public Color colorCanPlace=Color.green;
    public Color colorNoCanPlace=Color.red;

    [Header("连接线")]
    public LineRenderer lineRenderer;
    private Vector3 mouseWorldPos;

    public GridGameUICtrl gTT;
    public enum InteractionState { Idle, Placing, Connecting }
    private void OnEnable()
    {
        registryBuilding = new List<Building>();
        Eventmanager.Instance.AddListener("DestroyBuilding", TryDestroyBuilding);
    }
    private void OnDisable()
    {
        Eventmanager.Instance?.RemoveListener("DestroyBuilding", TryDestroyBuilding);
    }
    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if(gTT==null)gTT=FindAnyObjectByType<GridGameUICtrl>();
    }
    private void Update()
    {
        if (currentState == InteractionState.Placing)
        {

            StartPlacing(selectedBuilding);
            if (Input.GetMouseButtonDown(0))
            {
                CGTT("尝试放置");
                TryPlaceBuilding();
            }
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacing();
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                // 检测是否点击了UI（避免点击UI时触发建筑选择）
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    CGTT("判断为误触UI");
                    return;
                }
                TrySelectBuilding();
            }
            if (Input.GetMouseButtonDown(1))
            {
                DeselectBuilding();
            }
        }
    }
    void CancelPlacing()
    {
        gTT.OnIsPModeButton();
        CGTT("结束放置");
        //selectedBuilding = null;
        if (previewObject != null)
            previewObject.SetActive(false);
    }
    public void CGTT(string t)
    {
        gTT.ChangeGTText(t);
    }
    public void StartPlacing(BuildingData data)
    {

        //放置模式启动！
        if (data == null) return;

        // 进入放置模式时清空当前选中的建筑实例，避免后续操作继续引用旧建筑
        if (selectedBuildingInstance != null)
        {
            selectedBuildingInstance.SetHighlight(false);
            selectedBuildingInstance = null;
        }

        // 检查资源是否足够
        if (lightEnergy < data.buildCostLight || lifePower < data.buildCostLife)
        {
            CGTT($"资源不足！需要 {data.buildCostLight} 光能 + {data.buildCostLife} 生命能");
            return;
        }
        selectedBuilding = data;
        // 创建预览物体
        if (previewObject == null)
        {
            previewObject = Instantiate(previewPrefab);
        }
        previewObject.SetActive(true);
        UpdatePreviewPosition();//让预览物体随鼠标移动
                                
        
    }
    void UpdatePreviewPosition()
    {
        CGTT("进入UpdatePreviewPosition()");
        Vector2Int gridPos = GetMouseGridPosition();
        if (gridPos.x < 0) return;
        bool canPlace = gridManager.CanPlaceBuilding(gridPos, selectedBuilding.size);
        // 更新预览位置
        Vector3 worldPos = gridManager.FixBuildingPos(gridManager.GridToWorld(gridPos),selectedBuilding);
        previewObject.transform.position = worldPos;
        // 根据是否可放置更新颜色
        if (previewObject.TryGetComponent<MeshRenderer>(out var rend))
        {
            rend.material.color = canPlace ? colorCanPlace : colorNoCanPlace;
        }
    }
    void TryPlaceBuilding()
    {
        Vector2Int gridPos = GetMouseGridPosition();
        if (gridPos.x < 0) return;

        if (gridManager.CanPlaceBuilding(gridPos, selectedBuilding.size))
        {
            // 扣资源
            lightEnergy -= selectedBuilding.buildCostLight;
            lifePower -= selectedBuilding.buildCostLife;

            // 放置建筑
            gridManager.PlaceBuilding(gridPos, selectedBuilding);

            // 注册到建筑注册列表
            if (gridManager.Cells[gridPos.x, gridPos.y].buildingInstance.TryGetComponent<Building>(out var building))
                RegisterBuilding(building);
            //隐藏预览体
            previewObject.SetActive(false);
            // 触发UI更新
            //OnResourcesChanged?.Invoke();

            // 退出放置模式
            gTT.OnIsPModeButton();
            gTT.ChangeGTText($"成功放置 {selectedBuilding.buildingName} 在 {gridPos}");
        }
        else
        {
            gTT.ChangeGTText("该位置无法放置建筑！");
            CancelPlacing();
        }
    }
    void TrySelectBuilding()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, buildingLayer))
        {
            if (hit.collider.TryGetComponent<Building>(out var building))
            {
                SelectBuilding(building);
                
                return;
            }
        }
        DeselectBuilding();
    }

    void SelectBuilding(Building building)
    {
        
        // 取消之前的选中高亮
        DeselectBuilding();

        selectedBuildingInstance = building;
        // 高亮效果（例如添加发光材质或改变颜色）
        building.SetHighlight(true);
        // 显示信息面板（gTT待完善）
        CGTT($"当前选中：{building.name}{building.GetHashCode()}");
        //纯是实验一下xlua
        Eventmanager.Instance.Emit("ReadBuildingData", building.data);

    }
    void DeselectBuilding()
    {
        if (selectedBuildingInstance != null)
        {
            selectedBuildingInstance.SetHighlight(false);
            selectedBuildingInstance = null;
        }
    }
    void TryDestroyBuilding(string a,object b)
    {
        if(b is bool o)
        {
            if (o)
            {
                if (selectedBuildingInstance == null) return;
                CGTT("尝试销毁");
                gridManager.RemoveBuilding(selectedBuildingInstance);
                selectedBuildingInstance.BeDestroy();
                selectedBuildingInstance=null;
            }
        }
    }


    //获取鼠标点击网格位置
    Vector2Int GetMouseGridPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane plane = new(Vector3.up, Vector3.zero);
        if (plane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector2Int gridPos = gridManager.WorldToGrid(hitPoint);
            return gridPos;
        }
        return new Vector2Int(-1, -1);
    }
    void RegisterBuilding(Building building)
    {
        registryBuilding.Add(building);
    }

}