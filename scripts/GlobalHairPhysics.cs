using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;

public class GlobalHairPhysics : UnitySingleton<GlobalHairPhysics>
{
    public int constraintIterations = 3;              // 约束迭代次数
    [Header("全局合力（重力+外力，所有头发共享）")]
    public Vector3 gravity = new Vector3(0, -9.8f, 0);
    public Vector3 force = Vector3.zero;

    [Header("全局更新频率")]
    public float updateRate = 60f;

    [Header("参照物（用于惯性）")]
    public Transform referenceObject;

    // 运行时数据
    public List<HPBoneManager> allManagers;
    public List<HairPhysicalBone> allBones;
    public List<int> needrewrite;
    private NativeArray<HairParticleData> particleDataArray;
    private Dictionary<HairPhysicalBone, int> boneToIndex;
    private float accumulatedTime;
    private Vector3 objectMove;
    private Vector3 lastReferencePos;

    void OnDestroy()
    {
        if (particleDataArray.IsCreated)
            particleDataArray.Dispose();
    }
    private void OnEnable()
    {
        if (ThirdPersonShooterController.Instance==null) return;
        updateRate = ((int)limit_fps.Instance.limitfpstype);
        needrewrite.Clear();
        // 收集所有 HPBoneManager
        allManagers = new List<HPBoneManager>(FindObjectsOfType<HPBoneManager>());
        if (allManagers.Count == 0)
        {
            Debug.LogWarning("未找到任何 HPBoneManager，GlobalHairPhysics 将不工作");
            enabled = false;
            return;
        }

        // 收集所有头发骨骼，并记录每个骨骼的原始参数（从 HairPhysicalBone 中读取）
        allBones = new List<HairPhysicalBone>();
        boneToIndex = new Dictionary<HairPhysicalBone, int>();
        needrewrite.Add(0);
        foreach (var mgr in allManagers)
        {
            // 禁用每个管理器自身的物理更新（避免重复计算）
            mgr.isEnable = false;
            var bones = mgr.GetBoneList();
            foreach (var b in bones)
            {
                boneToIndex[b] = allBones.Count;
                allBones.Add(b);
            }
            needrewrite.Add(allBones.Count);
        }

        // 初始化 NativeArray，同时保存每个粒子的阻尼和惯性系数（从骨骼读取）
        particleDataArray = new NativeArray<HairParticleData>(allBones.Count, Allocator.Persistent);
        for (int i = 0; i < allBones.Count; i++)
        {
            var b = allBones[i];
            particleDataArray[i] = new HairParticleData
            {
                position = b.nowWorldPos,
                prevPosition = b.preWorldPos,
                parentIndex = b.parentIndex,
                restLength = b.lengthToParent,
                damping = b.damping,   // 保留骨骼自身的阻尼
                inert = b.inert        // 保留骨骼自身的惯性系数
                // 注意：elasticity 和 stiffness 不参与 Verlet 积分，只在约束中使用，因此不需要存入 NativeArray
            };
        }

        // 设置参考对象
        if (referenceObject == null && allManagers.Count > 0)
            referenceObject = allManagers[0].referenceObject;
        if (referenceObject != null)
            lastReferencePos = referenceObject.position;
    }

    void LateUpdate()
    {
        if (allBones.Count == 0) return;

        // 计算惯性位移
        if (referenceObject != null)
        {
            Vector3 newPos = referenceObject.position;
            objectMove = newPos - lastReferencePos;
            lastReferencePos = newPos;
        }

        float dt = Time.deltaTime;
        int loop = 1;
        if (updateRate > 0)
        {
            float step = 1f / updateRate;
            accumulatedTime += dt;
            loop = 0;
            while (accumulatedTime >= step)
            {
                accumulatedTime -= step;
                if (++loop >= constraintIterations)
                {
                    accumulatedTime = 0;
                    break;
                }
            }
        }

        // 计算全局合力（所有头发共享）
        Vector3 totalForce = gravity + force;
        // 缩放影响（如果角色有缩放）
        float scale = Mathf.Abs(transform.lossyScale.x);
        totalForce *= scale;

        if (loop > 0)
        {
            for (int iteration = 0; iteration < loop; iteration++)
            {
                float subDt = 1f / updateRate; // 或者更准确地使用 step，但 step 可能不固定
                // 同步当前骨骼位置到 NativeArray（每子步前）
                for (int i = 0; i < allBones.Count; i++)
                {
                    var b = allBones[i];
                    var data = particleDataArray[i];
                    data.position = b.nowWorldPos;
                    data.prevPosition = b.preWorldPos;
                    particleDataArray[i] = data;
                }

                // 调度 Verlet Job
                VerletIntegrationJob job = new VerletIntegrationJob
                {
                    particles = particleDataArray,
                    totalForce = totalForce,
                    objectMove = objectMove,
                    dt = subDt
                };
                JobHandle handle = job.Schedule(particleDataArray.Length, 64);
                handle.Complete();

                // 写回骨骼位置
                for (int i = 0; i < allBones.Count; i++)
                {
                    if(needrewrite.Contains(i))
                    {
                        allBones[i].preWorldPos = allBones[i].nowWorldPos;
                        allBones[i].nowWorldPos = allBones[i].bone.position;
                        continue;
                    }
                    var data = particleDataArray[i];
                    allBones[i].nowWorldPos = data.position;
                    allBones[i].preWorldPos = data.prevPosition;
                }

                // 对每个管理器执行约束（弹性、刚性、碰撞）和写回 Transform
                foreach (var mgr in allManagers)
                {
                    mgr.UpdateHPBones2();            // 内部使用各自的弹性、刚性等参数
                    mgr.ApplyHPBonesToTransforms();  // 应用位置和旋转
                }

                // 清除惯性位移（只在第一次子步使用）
                if (iteration == 0) objectMove = Vector3.zero;
            }
        }
        else
        {
            // 低帧率：跳过积分，只做约束和写回
            foreach (var mgr in allManagers)
            {
                mgr.UpdateHPBones2();
                mgr.ApplyHPBonesToTransforms();
            }
        }
    }
    public void OffEnable()
    {
        if (particleDataArray.IsCreated&&enabled)
            particleDataArray.Dispose();
        foreach (var mgr in allManagers)
        {
            // 禁用每个管理器自身的物理更新（避免重复计算）
            mgr.isEnable =enabled ;
        }
        enabled = !enabled;
    }
}