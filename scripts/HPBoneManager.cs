using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Burst;
/// <summary>
/// 基于质点模型的头发物理模拟，参考 Dynamic Bone 实现
/// </summary>
public class HPBoneManager : MonoBehaviour
{
    //private NativeArray<HairParticleData> particleDataArray;//job
    public bool isEnable=true;
    public List<HairPhysicalBone> GetBoneList() => hpbones;
    //合批
    [Header("骨骼链设置")]
    public Transform hairRoot;                      // 头发根骨骼
    public bool autoDetectChain = true;             // 自动遍历子骨骼作为链
    public List<Transform> hairBones;                // 手动指定的骨骼列表（当 autoDetectChain=false 时使用）
    public Vector3 endOffset = Vector3.zero;         // 末端虚拟骨骼的局部偏移
    public float endLength = 0f;                     // 末端虚拟骨骼的长度（当骨骼无子节点时）
    public List<Transform> exclusions = null;        // 不参与模拟的骨骼

    [Header("物理参数")]
    [Range(0, 1)] public float damping = 0.1f;       // 阻尼系数（模拟空气阻力）
    [Range(0, 1)] public float elasticity = 0.1f;    // 弹性系数（拉回初始姿态的强度）
    [Range(0, 1)] public float stiffness = 0.1f;     // 刚性系数（限制偏离初始姿态的距离）
    [Range(0, 1)] public float inertia = 0.5f;       // 惯性系数（参考物体运动的影响）
    public Vector3 gravity = new Vector3(0, -9.8f, 0); // 重力向量
    public Vector3 force = Vector3.zero;              // 额外作用力
    public float radius = 0.02f;                      // 默认碰撞半径（可被后续曲线覆盖）
    public int constraintIterations = 3;              // 约束迭代次数
    public float updateRate = 60.0f;                  // 物理更新频率（次/秒）

    [Header("根骨骼")]
    public bool fixRoot = false;                       // 是否固定根骨骼

    [Header("碰撞体")]
    public List<GameObject> gameObjects;            //需要注意碰撞的游戏物体
    public List<HPBCollider> HPBColliders = null;     //碰撞体列表
    public HPBCollider.ColliderDirection colliderDirection=HPBCollider.ColliderDirection.Y;//骨骼轴向
    public HPBCollider.BoneDirection boneDirection=HPBCollider.BoneDirection.forward;    //骨骼方向

    [Header("参考对象（用于惯性）")]
    public Transform referenceObject;                 // 运动参照物（通常为角色根节点）

    // 内部数据
    private List<HairPhysicalBone> hpbones = new List<HairPhysicalBone>();
    public Vector3 objectPrevPosition;               // 参考对象上一帧位置
    private Vector3 objectMove;                        // 当前帧参考对象的位移
    private float objectScale = 1f;                    // 全局缩放（取 X 轴绝对值）
    private float boneTotalLength = 0f;                 // 所有骨骼累计总长度（用于参数分布）
    private float accumulatedTime = 0f;                 // 时间累积（用于固定步长）
    private float weight = 1f;                          // 整体权重

    // 重力预处理
    private Vector3 localGravity;                        // 初始重力在 hairRoot 局部空间中的方向

    //void Start()
    //{
    //    if (hairRoot==null)
    //    {
    //        hairRoot = GetComponent<Transform>();
    //    }
    //    if (referenceObject==null)
    //    {
    //        referenceObject=transform.parent.GetComponent<Transform>();
    //    }
    //    SetupHPBones();
    //    AddHPBColliders();
    //}

    void OnEnable()
    {
        if (hairRoot == null)
        {
            hairRoot = GetComponent<Transform>();
        }
        if (referenceObject == null)
        {
            referenceObject = transform.parent.GetComponent<Transform>();
        }
        updateRate = (int)limit_fps.Instance.limitfpstype;
        SetupHPBones();
        AddHPBColliders();
        ResetHPBonesPosition();
    }

    void OnDisable()
    {
        InitTransforms();
    }

    void Update()
    {
        // 每帧更新时间累积，在 LateUpdate 中执行物理
        // 参考位置在 Update 中更新，以保证位移计算准确
        InitTransforms();
        if (referenceObject != null)
        {
            Vector3 newPos = referenceObject.position;
            objectMove = newPos - objectPrevPosition;
            objectPrevPosition = newPos;
        }
    }

    void LateUpdate()
    {
        if (isEnable == false)
            return;
        if (weight > 0 && hairRoot != null)
        {
            float dt = Time.deltaTime;
            UpdateHPBones(dt);
        }
    }
    void OnValidate()
    {
        if (Application.isEditor && Application.isPlaying)
        {
            InitTransforms();
            SetupHPBones();
        }
    }

    // 公开方法：设置权重
    public void SetWeight(float w)
    {
        if (weight != w)
        {
            if (w == 0)
                InitTransforms();
            else if (weight == 0)
                ResetHPBonesPosition();
            weight = w;
        }
    }

    public float GetWeight() => weight;

    // ==================== 初始化 ====================
    public void AddHPBColliders()//从物体列表子物体中添加碰撞体
    {
        HPBColliders.Clear();
        foreach(GameObject g in gameObjects)
        {
            foreach(HPBCollider c in g.GetComponentsInChildren<HPBCollider>())
            {
                HPBColliders.Add(c);
            }
        }
    }
    public void SetupHPBones()
    {
        hpbones.Clear();
        if (hairRoot == null)
            return;

        // 收集骨骼列表
        if (autoDetectChain)
        {
            hairBones = new List<Transform>();
            CollectChildBones(hairRoot, hairBones);
        }
        if (hairBones == null || hairBones.Count == 0)
        {
            Debug.LogError("没有找到头发骨骼！");
            return;
        }

        // 确保每个骨骼都有 HairPhysicalBone 组件
        for (int i = 0; i < hairBones.Count; i++)
        {
            Transform t = hairBones[i];
            HairPhysicalBone b = t.GetComponent<HairPhysicalBone>();
            if (b == null)
                b = t.gameObject.AddComponent<HairPhysicalBone>();
        }

        // 计算全局缩放
        objectScale = Mathf.Abs(transform.lossyScale.x);
        objectPrevPosition = referenceObject != null ? referenceObject.position : transform.position;
        objectMove = Vector3.zero;

        // 将重力转换到 hairRoot 的局部空间（用于后续投影）
        localGravity = hairRoot.InverseTransformDirection(gravity);

        boneTotalLength = 0;
        // 递归构建粒子
        AppendHPBones(hairRoot, -1, 0);

        // 为每个粒子分配经过分布曲线调整的参数（此处简化为全局值，可扩展）
        for (int i = 0; i < hpbones.Count; i++)
        {
            HairPhysicalBone b = hpbones[i];
            b.damping = damping;
            b.elasticity = elasticity;
            b.stiffness = stiffness;
            b.inert = inertia;
            b.radius = radius;

            // 待拓展：根据 b.boneLength / boneTotalLength 插值曲线
            // 如if (dampingDistrib != null) b.damping *= dampingDistrib.Evaluate(t);
        }
        // 为每个骨骼添加/配置胶囊碰撞体/
        for (int i = 0; i < hairBones.Count-1; i++)
        {
            HairPhysicalBone b = hpbones[i];
            HPBCollider capsule = b.GetComponent<HPBCollider>();
            if (capsule == null) capsule = b.gameObject.AddComponent<HPBCollider>();
            capsule.colliderRadius = b.radius;
            capsule.colliderHeight = hpbones[i+1].lengthToParent; //约束当前骨骼碰撞体的高度，由于当前骨骼长度由其子骨骼坐标决定，因此应为子骨骼的lengthToparent
            capsule.direction = colliderDirection; // Y轴（根据实际骨骼轴向调整）
            capsule.boneDirection = boneDirection;
            capsule.colliderCenter = new Vector3(0, b.lengthToParent * 0.5f, 0);
            capsule.bone = b.transform;
            b.cc = capsule;
        }
        ////初始化 NativeArray，初始化时创建并填充数组
        //int count = hpbones.Count;
        //particleDataArray = new NativeArray<HairParticleData>(count, Allocator.Persistent);

        //for (int i = 0; i < count; i++)
        //{
        //    HairPhysicalBone b = hpbones[i];
        //    particleDataArray[i] = new HairParticleData
        //    {
        //        //position = b.nowWorldPos,
        //        //prevPosition = b.preWorldPos,
        //        parentIndex = b.parentIndex,
        //        restLength = b.lengthToParent,
        //        damping = b.damping,
        //        elasticity = b.elasticity,
        //        stiffness = b.stiffness,
        //        inert = b.inert
        //    };
        //}
    }

    // 递归添加骨骼（深度优先）
    private void AppendHPBones(Transform t, int parentIndex, float boneLength)
    {
        HairPhysicalBone p = t.GetComponent<HairPhysicalBone>();
        if (p == null) return; // 实际不应发生

        p.bone = t;
        p.parentIndex = parentIndex;
        //if (parentIndex >= 0)
        //    p.parentBone = hairBones[parentIndex];

        // 记录初始姿态
        p.initialLocalPos = t.localPosition;
        p.initialLocalRot = t.localRotation;
        p.nowWorldPos = t.position;
        p.preWorldPos = p.nowWorldPos;
        p.localPos = t.localPosition;
        p.prevLocalPos = p.localPos;

        // 计算与父骨骼的原始距离
        if (parentIndex >= 0)
        {
            Transform parent = hairBones[parentIndex];
            p.lengthToParent = Vector3.Distance(parent.position, t.position);
            boneLength += p.lengthToParent;
        }
        else
        {
            p.lengthToParent = 0;
        }

        p.boneLength = boneLength;
        boneTotalLength = Mathf.Max(boneTotalLength, boneLength);

        int index = hpbones.Count;
        hpbones.Add(p);

        // 遍历子骨骼
        if (t != null)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                Transform child = t.GetChild(i);
                bool excluded = false;
                if (exclusions != null)
                {
                    foreach (Transform e in exclusions)
                        if (e == child) { excluded = true; break; }
                }
                if (!excluded)
                    AppendHPBones(child, index, boneLength);
            }

            // 如果当前骨骼没有子节点且设置了末端参数，添加一个虚拟粒子
            if (t.childCount == 0 && (endLength > 0 || endOffset != Vector3.zero))
            {
                // 为简化，暂不处理末端虚拟骨骼。
            }
        }
    }

    // 收集所有子骨骼（线性链）
    private void CollectChildBones(Transform root, List<Transform> list)
    {
        list.Add(root);
        foreach (Transform child in root)
            CollectChildBones(child, list);
    }

    // ==================== 核心物理更新 ====================
    private void UpdateHPBones(float deltaTime)
    {
        if (hairRoot == null || hpbones.Count == 0)
            return;

        // 更新物体缩放（如果缩放变化）
        objectScale = Mathf.Abs(transform.lossyScale.x);

        int loop = 1;
        if (updateRate > 0)
        {
            float step = 1f / updateRate;
            accumulatedTime += deltaTime;
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

        if (loop > 0)
        {
            for (int j = 0; j < loop; j++)
            {
                UpdateHPBones1(deltaTime);   // 积分
                UpdateHPBones2();   // 约束
                objectMove = Vector3.zero; // 只在第一次子步使用物体位移，后续子步视为静止
            }
        }
        else
        {
            SkipUpdateHPBones();   // 帧率过低时只更新刚性/长度约束
        }

        ApplyHPBonesToTransforms();
    }

    // 第一步：Verlet 积分 + 外力（重力、作用力） + 阻尼 + 惯性
    private void UpdateHPBones1(float deltaTime)
    {
        // 计算重力作用力（类似 Dynamic Bone 的处理：投影到初始重力方向，避免旋转影响）
        Vector3 totalForce = gravity + force;
        Vector3 fdir = gravity.normalized;
        Vector3 rf = hairRoot.TransformDirection(localGravity); // 当前模型空间中的重力方向
        Vector3 pf = fdir * Mathf.Max(Vector3.Dot(rf, fdir), 0);
        totalForce -= pf;   // 移除与初始重力垂直的分量
        totalForce *= objectScale;
        for (int i = 0; i < hpbones.Count; i++)
        {
            HairPhysicalBone p = hpbones[i];
            if (i == 0 && fixRoot) continue; // 根骨骼固定，不更新

            if (p.parentIndex >= 0) // 非根粒子
            {
                // Verlet 速度
                Vector3 v = p.nowWorldPos - p.preWorldPos;
                // 惯性：物体移动量乘以惯性系数
                Vector3 rmove = objectMove * p.inert;
                //Debug.Log(rmove.sqrMagnitude);
                // 更新上一帧位置（用于下一帧速度计算）
                p.preWorldPos = p.nowWorldPos + rmove;
                // 更新当前位置：速度 * (1 - 阻尼) + 力 + 惯性移动
                p.nowWorldPos += v * (1 - p.damping) + totalForce + rmove;
            }
            else // 根粒子
            {
                p.preWorldPos = p.nowWorldPos;
                p.nowWorldPos = p.bone.position;
            }
        }
    }
    public void UpdateHPBones2()
    {
        for (int i = 1; i < hpbones.Count; i++)
        {
            HairPhysicalBone p = hpbones[i];
            HairPhysicalBone p0 = hpbones[p.parentIndex];
            // 静止长度（父子初始距离）
            float restLen = p.lengthToParent;//由于改写的p世界坐标影响其父骨骼长度，所以用父骨骼长度约束

            // ----- 弹性 + 刚性（保持形状）-----
            float stiff = Mathf.Lerp(1f, p.stiffness, weight);
            if (stiff > 0 || p.elasticity > 0)
            {
                // 获取父骨骼的 localToWorldMatrix（包含旋转和缩放）
                Matrix4x4 m0 = p0.bone.localToWorldMatrix;
                // 将平移分量替换为父粒子的当前位置（而非骨骼位置）
                m0.SetColumn(3, p0.nowWorldPos);
                // 计算静止姿态世界位置：基于父粒子当前位置和父骨骼旋转的初始偏移
                Vector3 restPos = m0.MultiplyPoint3x4(p.transform.localPosition);
                //// 计算粒子的静止姿态位置（父粒子位置 + 父旋转 * 初始局部位置）
                //Vector3 restPos = p0.bone.TransformPoint(p.localPos);
                Vector3 d = restPos - p.nowWorldPos;

                // 弹性：向静止位置移动
                p.nowWorldPos += d * p.elasticity;

                // 刚性：限制最大偏离距离
                if (stiff > 0)
                {
                    d = restPos - p.nowWorldPos;
                    float len = d.magnitude;
                    float maxLen = restLen * (1 - stiff) * 2;
                    if (len > maxLen)
                        p.nowWorldPos += d * ((len - maxLen) / len);
                }
            }
            //-------碰撞约束---------
            if (HPBColliders != null)
            {
                float bRadius = p.radius * objectScale;
                for (int j = 0; j < HPBColliders.Count; ++j)
                {
                    HPBCollider c = HPBColliders[j];
                    if (c != null && c.enabled) 
                    {
                        Vector3 ccP0 = c.transform.TransformPoint(c.cP0);
                        Vector3 ccP1 = c.transform.TransformPoint(c.cP1);
                        float cRadius=c.colliderRadius;
                        c.Collision(ref p.nowWorldPos, p.radius, ccP0, ccP1, cRadius);
                    }
                        
                }
            }
            // --- 保持长度（距离约束）-----
            Vector3 dd = p0.nowWorldPos - p.nowWorldPos;
            float dist = dd.magnitude;
            if (dist > 0)
            {
                Vector3 correction = dd * ((dist - restLen) / dist);
                // 简单处理：
                p.nowWorldPos += correction;
            }
        }
    }

    // 低帧率时跳过积分，只更新刚性和长度约束
    private void SkipUpdateHPBones()
    {
        for (int i = 0; i < hpbones.Count; i++)
        {
            HairPhysicalBone p = hpbones[i];
            if (p.parentIndex >= 0)
            {
                p.preWorldPos += objectMove;
                p.nowWorldPos += objectMove;

                HairPhysicalBone p0 = hpbones[p.parentIndex];
                float restLen = p.lengthToParent;

                // 刚性
                float stiff = Mathf.Lerp(1f, p.stiffness, weight);
                if (stiff > 0)
                {
                    Vector3 restPos = p0.bone.TransformPoint(p.initialLocalPos);
                    Vector3 d = restPos - p.nowWorldPos;
                    float len = d.magnitude;
                    float maxLen = restLen * (1 - stiff) * 2;
                    if (len > maxLen)
                        p.nowWorldPos += d * ((len - maxLen) / len);
                }

                // 长度约束
                Vector3 dd = p0.nowWorldPos - p.nowWorldPos;
                float dist = dd.magnitude;
                if (dist > 0)
                    p.nowWorldPos += dd * ((dist - restLen) / dist);
            }
            else
            {
                p.preWorldPos = p.nowWorldPos;
                p.nowWorldPos = p.bone.position;
            }
        }
    }

    // 将粒子位置写回 Transform，并调整旋转使父骨骼指向子骨骼
    public void ApplyHPBonesToTransforms()
    {
        for (int i = 1; i < hpbones.Count; i++)
        {
            HairPhysicalBone p = hpbones[i];
            HairPhysicalBone p0 = hpbones[p.parentIndex];

            // 只有当父骨骼只有一个子骨骼时才修改旋转，避免影响其他分支
            if (p0.bone.childCount <= 1)
            {
                // 获取子骨骼相对于父骨骼的变换前局部方向
                Vector3 localDir = p.bone.localPosition.normalized;
                if (localDir.sqrMagnitude > 0)
                {
                    // 将变换前局部方向转换到世界空间
                    Vector3 worldDir = p0.bone.TransformDirection(localDir);
                    // 当前世界空间中父到子的方向
                    Vector3 currentDir = p.nowWorldPos - p0.nowWorldPos;
                    if (currentDir.sqrMagnitude > 0.0001f)
                    {
                        Quaternion rot = Quaternion.FromToRotation(worldDir, currentDir);
                        p0.bone.rotation = rot * p0.bone.rotation;
                    }
                }
            }

            // 设置子骨骼位置
            p.bone.position = p.nowWorldPos;
        }
    }

    // 重置粒子位置到当前骨骼的实际位置（用于重新启用时）
    private void ResetHPBonesPosition()
    {
        for (int i = 0; i < hpbones.Count; i++)
        {
            HairPhysicalBone p = hpbones[i];
            p.nowWorldPos = p.preWorldPos = p.bone.position;
        }
        if (referenceObject != null)
            objectPrevPosition = referenceObject.position;
        else
            objectPrevPosition = transform.position;
        objectMove = Vector3.zero;
    }

    // 将所有骨骼恢复到初始局部位置/旋转（复位）
    private void InitTransforms()
    {
        for (int i = 0; i < hpbones.Count; i++)
        {
            HairPhysicalBone p = hpbones[i];
            p.bone.localPosition = p.initialLocalPos;
            p.bone.localRotation = p.initialLocalRot;
        }
        //Debug.Log("执行了一次复位");
    }

    // 绘制 Gizmos 显示粒子链
    void OnDrawGizmosSelected()
    {
        if (!enabled || hairRoot == null) return;
        Gizmos.color = Color.white;
        for (int i = 0; i < hpbones.Count; i++)
        {
            HairPhysicalBone p = hpbones[i];
            if (p.parentIndex >= 0)
            {
                HairPhysicalBone p0 = hpbones[p.parentIndex];
                Gizmos.DrawLine(p.nowWorldPos, p0.nowWorldPos);
            }
            if (p.radius > 0)
                Gizmos.DrawWireSphere(p.nowWorldPos, p.radius * objectScale);
        }
    }
}
