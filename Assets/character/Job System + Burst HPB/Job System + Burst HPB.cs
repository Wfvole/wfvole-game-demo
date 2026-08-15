using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

public struct HairParticleData
{
    public int test;
    public float3 position;      // 当前世界位置
    public float3 prevPosition;  // 上一帧世界位置
    public int parentIndex;      // 父骨骼索引（-1 表示根）
    public float restLength;     // 与父骨骼的原始距离
    public float damping;        // 阻尼系数（每个粒子可不同）
    public float elasticity;     // 弹性系数
    public float stiffness;      // 刚性系数
    public float inert;          // 惯性系数
}

[BurstCompile]
struct VerletIntegrationJob : IJobParallelFor
{
    public NativeArray<HairParticleData> particles;
    [ReadOnly] public float3 totalForce;   // 每帧相同的合力（重力+惯性等）
    [ReadOnly] public float3 objectMove;   // 参考物体位移（用于惯性）
    [ReadOnly] public float dt;

    public void Execute(int i)
    {
        var p = particles[i];
        p.test = 0;
        if (p.parentIndex>=0)  // 根粒子，不更新（或直接跟随）
        {
            p.test=1;
            // Verlet 速度
            float3 velocity = p.position - p.prevPosition;
            // 惯性
            float3 rmove = objectMove * p.inert;
            // 更新上一帧位置
            p.prevPosition = p.position + rmove;
            // 更新当前位置
            p.position += velocity * (1 - p.damping) + totalForce * dt + rmove;
            particles[i] = p;
        }
    }
}