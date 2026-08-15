using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class HairPhysicalBone : MonoBehaviour
{
    // 类质点数据结构
    public string boneName = "";
    //public Transform parentBone;     //父骨骼Transform位置信息
    public Transform bone;           //骨骼自身Transform位置信息
    public Vector3 nowWorldPos;      //当前世界位置
    public Vector3 preWorldPos;      // 上一帧世界位置
    public Vector3 localPos;         // 当前局部位置
    public Vector3 prevLocalPos;     // 上一帧局部位置（用于Verlet积分）
    public Vector3 nowLocalPos;      //当前局部位置
    public Vector3 initialLocalPos;  // 初始局部位置（用于回位）
    public quaternion initialLocalRot;   // 初始局部旋转（同上）
    public Vector3 nowVelocity;      //当前世界速度
    public Vector3 prevVelocity;     //上一帧世界速度
    public Vector3 endOffset = Vector3.zero;       // 末端偏移量（仅对末端粒子有效）
    public float damping = 0;                     // 该粒子的阻尼（经过曲线调整后）
    public float elasticity = 0;
    public float stiffness = 0;
    public float inert = 0;
    public float radius = 0;
    public float boneLength = 0;                  // 从根到该粒子的累计长度
    public float lengthToParent;     // 与父骨骼的原始距离//父骨骼长度
    public int parentIndex;          //父骨骼在链中序号
    public float mass;               // 质量（可用骨骼长度或固定值，这里暂时简化为1）
    public HPBCollider cc;       //骨骼上的碰撞体

    /// <summary>
    /// 根据 CapsuleCollider 计算世界空间中的两个球心位置和半径（考虑缩放）
    /// </summary>
    
}
