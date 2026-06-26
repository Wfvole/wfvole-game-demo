using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : UnitySingleton<ProjectilePool>
{

    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int preloadCount = 10;
    [SerializeField] private GameObject magicCircle;
    [SerializeField] private int circleCount = 5;

    private Queue<GameObject> pool = new Queue<GameObject>();
    private Queue<GameObject> cirPool= new Queue<GameObject>();

    public override void Awake()
    {
        base.Awake();
        // 预创建指定数量的小球
        for (int i = 0; i < preloadCount; i++)
        {
            GameObject obj = CreateNewProjectile();
            obj.transform.SetParent(transform); // 确保放回父节点下
            pool.Enqueue(obj);
            if (i<circleCount)//创建指定数量的粒子特效
            {
                GameObject circle = CreatMagicCircle();
                circle.transform.SetParent(transform);
                cirPool.Enqueue(circle);
            }
        }

    }
    public GameObject GetMagicCircle()
    {
        if (cirPool.Count > 0)
        {
            GameObject obj = cirPool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        else return CreatMagicCircle();
    }
    private GameObject CreatMagicCircle()
    {
        GameObject obj = Instantiate(magicCircle);
        obj.SetActive(false);
        return obj;
    }
    private GameObject CreateNewProjectile()
    {
        GameObject obj = Instantiate(projectilePrefab);
        obj.SetActive(false);
        //// 确保小球挂载必要的移动和伤害组件（已在预制体上有，这里仅为安全）
        //if (obj.GetComponent<ProjectileMovement>() == null)
        //    obj.AddComponent<ProjectileMovement>();
        //if (obj.GetComponent<ProjectileDamage>() == null)
        //    obj.AddComponent<ProjectileDamage>();
        return obj;
    }

    public GameObject Get()
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        else
        {
            // 池子空了，动态创建一个（增加容量）
            //Debug.LogWarning("对象池耗尽，动态新建一个小球，建议增加预加载数量");
            return CreateNewProjectile();
        }
    }

    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        // 可选：重置位置和速度
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null) rb.velocity = Vector3.zero;
        pool.Enqueue(obj);
    }
    public void MCReturn(GameObject obj)
    {
        obj.SetActive(false);
        // 可选：重置位置和速度
        cirPool.Enqueue(obj);
    }
}