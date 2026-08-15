using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    public BuildingData data;
    public Vector2Int gridPosition;
    public List<Building> connectedBuildings = new List<Building>();
    public List<MeshRenderer> r;
    private MaterialPropertyBlock propertyBlock;
    int seconds=1;
    private void Awake()
    {
        r = new();
        MeshRenderer[] a = gameObject.GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < a.Length; i++) { r.Add(a[i]); }
        propertyBlock = new MaterialPropertyBlock();
    }
    public void SetHighlight(bool active)
    {
        if (r != null)
        {
            for (int i = 0; i < r.Count; i++)
            {
                r[i].GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_EmissionColor", active ? Color.white*0.5f : Color.black*0f);
                r[i].SetPropertyBlock(propertyBlock);
            }
        }
    }
    public virtual void BeDestroy() { Destroy(gameObject); }
    public virtual void OnPlaced() { GridInteractionManager.Instance.lightEnergy++;}
    public virtual void OnConnected(Building other) { }
    public virtual void OnDisconnected(Building other) { }

    // 点击建筑时触发（用于连接或信息查看）
    public virtual void OnClick() { }
    void Update()
    {
        seconds++;
        if(seconds%50==0)
        {
            seconds=0;
            OnPlaced();
        }
    }
}