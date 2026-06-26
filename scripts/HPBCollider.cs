using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPBCollider : MonoBehaviour
{
    public Transform bone;
    public Vector3 colliderCenter;
    public float colliderRadius;
    public float colliderHeight;
    public Color cColor=Color.yellow;
    public enum ColliderDirection
    {
        X, Y, Z
    }
    public enum BoneDirection
    {
        forward, backward
    }
    public ColliderDirection direction = ColliderDirection.Y;
    public BoneDirection boneDirection = BoneDirection.forward;
    public Vector3 cP0, cP1;//球心
    private void Start()
    {
        UpdateColliderCenterP();
    }
    public void UpdateColliderCenterP()
    {
        if (bone==null) 
        {
            bone = transform;
            colliderHeight = bone.GetChild(0).GetComponent<Transform>().localPosition.magnitude; 
        }
        Vector3 d1=Vector3.zero;
        Vector3 d2=Vector3.zero;
        float cen=1;
        //计算更新球心位置，为中心在方向轴上加减(0.5*Height-Radius)
        switch (direction)
        {
            case ColliderDirection.X:
                d1 = Vector3.right;
                d2 = Vector3.left;
                break;
            case ColliderDirection.Y:
                d1 = Vector3.up;
                d2 = Vector3.down;
                break;
            case ColliderDirection.Z:
                d1 = Vector3.forward;
                d2 = Vector3.back;
                break;
        }
        switch (boneDirection)
        {
            case BoneDirection.backward: 
                cen=-1f;
                break;
            case BoneDirection.forward:
                cen = 1f;
                break;
        }
        if (cP0 == Vector3.zero && cP1 == Vector3.zero)
        {
            colliderCenter = d1 * 0.5f * colliderHeight * cen;
            cP0 = colliderCenter + d2 * (0.5f * colliderHeight - colliderRadius) * cen;
            cP1 = colliderCenter + d1 * (0.5f * colliderHeight - colliderRadius) * cen;
        }
        
        
    }
    public void Collision(ref Vector3 bPosition, float bRadius, Vector3 ccP0, Vector3 ccP1, float cRadius) 
    {
        //判断是否发生碰撞：碰撞体形状为胶囊，将骨骼视为线段，约束线段上均匀分出的多个点与胶囊轴线距离不能小于半径
        //传参数时要转换为世界坐标,判断依据是距离
        float r = cRadius + bRadius;
        float sqr = r * r;
        Vector3 dir = ccP1 - ccP0;//球心0到球心1的向量
        Vector3 d = bPosition - ccP0;//球心0到骨骼坐标的向量
        float dotl = Vector3.Dot(d, dir);//|d|*|dir|*cost
        if (dotl<=0)//说明骨骼坐标点在球心0一侧，距离长度是向量d的模长，出于性能考虑避免开方
        {
            float sqdlenth = d.sqrMagnitude;
            if (sqdlenth>=sqr) //没穿透碰撞体
            { }
            else //在碰撞体内//目前主要目标是防穿模，暂时只做外约束
            {
                float dlenth = Mathf.Sqrt(sqdlenth);
                bPosition += d * ((r - dlenth) /dlenth);//往d方向推
            }
        }
        else //说明骨骼坐标投影在球心0往球心1方向上
        {
            if (dotl*dotl >= dir.sqrMagnitude*dir.sqrMagnitude)//sqdotl/sqdir=sqd*sqcos=d在dir方向投影长度平方ddlen如果ddlen>sqdir则骨骼坐标投影
            {                             //在球心1一侧，二者向量可表达为 d-dir
                float sqdlenth = (d - dir).sqrMagnitude;
                if (sqdlenth >= sqr) //没穿透碰撞体 
                { }
                else //在碰撞体内
                {
                    float dlenth = Mathf.Sqrt(sqdlenth);
                    bPosition += (d-dir) * ((r - dlenth) / dlenth);//往d-dir方向推
                }
            }
            else//否则骨骼坐标投影在球心轴线段上
            {
                float sqdlenth=d.sqrMagnitude-dotl*dotl/dir.sqrMagnitude;
                if (sqdlenth >= sqr) //没穿透碰撞体
                { }
                else //在碰撞体内
                {
                    float dlenth = Mathf.Sqrt(sqdlenth);
                    Vector3 vd =d-dir.normalized*dotl/dir.magnitude;
                    bPosition += vd* ((r - dlenth) / dlenth);//往投影点到骨骼坐标方向推
                }
            }
        }

    }

    void OnDrawGizmosSelected()
    {
        if (!enabled)
            return;
        Gizmos.color = cColor;
        
        float radius = colliderRadius * Mathf.Abs(transform.lossyScale.x);
        float h = colliderHeight * 0.5f - colliderRadius;
        if (h <= 0)
        {
            Gizmos.DrawWireSphere(transform.TransformPoint(colliderCenter), radius);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.TransformPoint(cP0), radius);
            Gizmos.DrawWireSphere(transform.TransformPoint(cP1), radius);
            Gizmos.DrawLine(transform.TransformPoint(cP0), transform.TransformPoint(cP1));
        }
    }
}
