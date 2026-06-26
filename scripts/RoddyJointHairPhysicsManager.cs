using UnityEngine;
using System.Collections.Generic;

public class RJHairPhysicsManager : MonoBehaviour
{
    [Header("头发骨骼设置")]
    public Transform hairRoot;
    public bool autoDetectChain = true;
    public List<Transform> hairBones;

    [Header("物理参数")]
    public float boneMass = 0.1f;
    public float boneDrag = 0.5f;
    public float boneAngularDrag = 0.5f;
    public float colliderRadius = 0.02f;
    public float colliderHeightMultiplier = 1.0f;

    [Header("关节参数")]
    public float springStrength = 100f;
    public float damperStrength = 10f;
    public float maxForce = 1000f;

    [Header("根骨骼固定")]
    public bool fixRootToWorld = true;
    public Transform rootFixedReference;

    private List<HairBoneData> boneDataList = new List<HairBoneData>();

    private class HairBoneData
    {
        public Transform boneTransform;
        public Rigidbody rigidbody;
        public CapsuleCollider capsuleCollider;
        public ConfigurableJoint joint;
        public Vector3 initialLocalPosition;
        public Vector3  initialLocalRotation;
        public Transform parentBone;
        public Vector3 localPos;
        public float lengthToParent;     // 与父骨骼的原始距离
    }

    void Start()
    {
        InitializeHairPhysics();
    }

    void InitializeHairPhysics()
    {
        // 收集骨骼
        if (autoDetectChain && hairRoot != null)
        {
            hairBones = new List<Transform>();
            CollectChildBones(hairRoot, hairBones);
        }

        if (hairBones == null || hairBones.Count == 0)
        {
            Debug.LogError("没有找到头发骨骼！");
            return;
        }

        // 第一步：为所有骨骼添加/配置Rigidbody（暂时设为Kinematic，以便后续设置关节）
        foreach (Transform bone in hairBones)
        {

            if (bone == null) continue;
            Rigidbody rb = bone.GetComponent<Rigidbody>();
            if (rb == null) rb = bone.gameObject.AddComponent<Rigidbody>();
            rb.mass = boneMass;
            rb.drag = boneDrag;
            rb.angularDrag = boneAngularDrag;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.isKinematic = true; // 初始全部Kinematic，后面按需设置
        }

        // 第二步：为每个骨骼创建HairBoneData并添加碰撞体
        for (int i = 0; i < hairBones.Count; i++)
        {
            Transform bone = hairBones[i];
            HairBoneData data = new HairBoneData();
            data.boneTransform = bone;
            data.initialLocalPosition = bone.localPosition;
            data.initialLocalRotation = bone.localEulerAngles;
            data.lengthToParent = data.initialLocalPosition.magnitude;
            data.rigidbody = bone.GetComponent<Rigidbody>();

            // 计算骨骼长度（到下一个骨骼的距离）
            float boneLength = 0.1f;
            if (i < hairBones.Count - 1)
            {
                Transform nextBone = hairBones[i + 1];
                boneLength = Vector3.Distance(bone.position, nextBone.position);
            }
            else
            {
                boneLength = 0;
            }

            // 添加/配置胶囊碰撞体
            CapsuleCollider capsule = bone.GetComponent<CapsuleCollider>();
            if (capsule == null) capsule = bone.gameObject.AddComponent<CapsuleCollider>();
            capsule.radius = colliderRadius;
            capsule.height = boneLength * colliderHeightMultiplier;
            capsule.direction = 1; // Y轴（根据实际骨骼轴向调整）
            capsule.center = new Vector3(0, boneLength * 0.5f, 0);
            data.capsuleCollider = capsule;
            boneDataList.Add(data);
        }

        // 第三步：添加关节连接
        for (int i = 0; i < hairBones.Count; i++)
        {
            Transform bone = hairBones[i];
            HairBoneData data = boneDataList[i]; // 获取对应的data
            if (i==hairBones.Count) continue;
            if (i == 0) // 根骨骼
            {
                if (fixRootToWorld)
                {
                    if (rootFixedReference != null)
                    {
                        // 连接到固定参考点
                        ConfigurableJoint rootJoint = bone.gameObject.AddComponent<ConfigurableJoint>();
                        Rigidbody refRb = rootFixedReference.GetComponent<Rigidbody>();
                        if (refRb == null)
                        {
                            refRb = rootFixedReference.gameObject.AddComponent<Rigidbody>();
                            refRb.isKinematic = true;
                        }
                        rootJoint.connectedBody = refRb;
                        rootJoint.autoConfigureConnectedAnchor = false;
                        rootJoint.anchor = Vector3.zero;
                        rootJoint.connectedAnchor = rootFixedReference.InverseTransformPoint(bone.position);
                        LockJoint(rootJoint);
                        data.joint = rootJoint;
                        data.rigidbody.isKinematic = false; // 根骨骼亦如是
                    }
                    else
                    {
                        // 直接固定
                        data.rigidbody.isKinematic = true;
                    }
                }
                else
                {
                    data.rigidbody.isKinematic = false;
                }
                continue;
            }

            // 非根骨骼：连接到父骨骼
            Transform parentBone = hairBones[i - 1];
            Rigidbody parentRb = parentBone.GetComponent<Rigidbody>();
            Rigidbody childRb = bone.GetComponent<Rigidbody>();

            Vector3 connectedAnchorLocal = parentBone.InverseTransformPoint(bone.position);

            ConfigurableJoint joint = bone.GetComponent<ConfigurableJoint>();
            if (joint == null) joint = bone.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = parentRb;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = Vector3.zero;
            joint.connectedAnchor = connectedAnchorLocal;
            // 2. 启用投影，设置合理的阈值
            joint.projectionDistance = 0.001f;
            joint.projectionAngle = 1f;
            // 4. （可选）禁用预处理
            joint.enablePreprocessing = false;
            // 配置自由度（我使用的模型骨骼轴向是Y轴）
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;

            JointDrive drive = new JointDrive();
            drive.positionSpring = springStrength;
            drive.positionDamper = damperStrength;
            drive.maximumForce = maxForce;
            joint.angularXDrive = drive;
            joint.angularYZDrive = drive;
            joint.rotationDriveMode = RotationDriveMode.Slerp;

            data.joint = joint;

            // 启用物理（非Kinematic）
            childRb.isKinematic = false;
            data.rigidbody.useGravity = true;
            // 父骨骼如果不是固定的根，也要设为非Kinematic
            if (i > 1 || !fixRootToWorld)
            {
                parentRb.isKinematic = false;
            }

        }

        // 最后，确保所有非根骨骼的刚体为非Kinematic（以防父骨骼被误设为Kinematic）
        for (int i = 1; i < boneDataList.Count; i++)
        {
            boneDataList[i].rigidbody.isKinematic = false;
        }
    }

    private void LockJoint(ConfigurableJoint joint)
    {
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = ConfigurableJointMotion.Free;
        joint.angularYMotion = ConfigurableJointMotion.Free;
        joint.angularZMotion = ConfigurableJointMotion.Free;
    }

    private void CollectChildBones(Transform root, List<Transform> list)
    {
        list.Add(root);
        foreach (Transform child in root)
        {
            CollectChildBones(child, list);
        }
    }

    void FixedUpdate() 
    {
        for (int i = 0; i < boneDataList.Count; i++)
        {
            HairBoneData bd = boneDataList[i];
            bd.localPos = bd.boneTransform.localPosition;
            bd.boneTransform.localPosition=bd.lengthToParent* bd.localPos.normalized;
            //if (bd.boneTransform.localEulerAngles != bd.initialLocalRotation) 
            //{
            //    bd.boneTransform.localEulerAngles += (bd.initialLocalRotation- bd.boneTransform.localEulerAngles) / Time.fixedDeltaTime;

            //}
            
        }
    }
    void LateUpdate() { }
}