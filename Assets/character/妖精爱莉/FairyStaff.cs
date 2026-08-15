using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 法杖武器组件：攻击时生成魔法小球并射出
/// </summary>
public class FairyStaff : MonoBehaviour
{
    [Header("射击参数")]
    public GameObject projectilePrefab;   // 小球预制体
    public Transform firePoint;           // 子弹发射点（建议设为法杖尖端）
    public float projectileSpeed = 1f;   // 小球初速度
    public float projectileLifetime = 5f; // 小球自动销毁时间（秒）
    public float attackCooldown = 0.5f;   // 攻击冷却时间
    public float damage = 10f;            // 小球造成的伤害（可选，配合碰撞检测）
    private float lastAttackTime;          // 上次攻击时间
    private Vector3 fireV=Vector3.zero;
    [Header("魔法阵特效")]
    public GameObject magicCirclePrefab;  // 魔法阵特效预制体（粒子系统或 Sprite 动画）
    public float magicCircleDuration = 0.5f; // 魔法阵停留时间
    public bool ifgb=false;

    void Start()
    {
        if (firePoint == null)
        {
            // 如果没有指定发射点，默认使用自身位置
            firePoint = transform;
        }

        if (projectilePrefab == null)
        {
            //Debug.LogError("FairyStaff: 未设置小球预制体！");
            enabled = false;
        }
    }
    void SpawnMagicCircle()
    {
        //if (magicCirclePrefab != null) 
        //{
        //    GameObject circle=Instantiate(magicCirclePrefab,firePoint.position+new Vector3(0,1f,1f),firePoint.rotation); 
        //    Destroy(circle, magicCircleDuration);
        //}
        //对象池版
        GameObject circle = ProjectilePool.Instance.GetMagicCircle();
        circle.transform.position = firePoint.position + ThirdPersonShooterController.Instance.transform.forward+ new Vector3(0, 1f, 0);
        circle.transform.rotation = firePoint.rotation;
        circle.SetActive(true);
        StartCoroutine(MCReturnAfterDelay(circle, magicCircleDuration));
    }
    /// <summary>
    /// 外部调用的攻击方法（例如由动画事件、输入系统或 AI 调用）
    /// </summary>
    public void Attack()
    {
        // 检查冷却
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;

        //魔法阵特效
        SpawnMagicCircle();
        // 生成小球
        //GameObject projectile = Instantiate(projectilePrefab, firePoint.position+new Vector3(0,1f,1f), firePoint.rotation);
        // 从对象池获取小球
        GameObject projectile = ProjectilePool.Instance.Get();
        projectile.transform.SetPositionAndRotation(firePoint.position + ThirdPersonShooterController.Instance.transform.forward+ new Vector3(0, 1f, 0f), firePoint.rotation);
        projectile.tag = "playerAtk";
        // 获取 Rigidbody（如果有）并施加初速度
        if (ifgb) 
        {
            Rigidbody grb = projectile.GetComponent<Rigidbody>();
            if (grb == null)
            {
                grb = projectile.AddComponent<Rigidbody>();
            }
            fireV = PlayerWeaponManager.Instance.transform.forward;
            grb.velocity = fireV * projectileSpeed;
        }
        else
        {   //如果不启用Rigidbody，则使用简单的移动脚本（见下方）
            ProjectileMovement movement = projectile.GetComponent<ProjectileMovement>();
            if (movement == null)
                movement = projectile.AddComponent<ProjectileMovement>();
            movement.Initialize(firePoint.forward * projectileSpeed, projectileLifetime);
        }

        // 添加碰撞伤害
        //ProjectileDamage damageHandler = projectile.GetComponent<ProjectileDamage>();
        //if (damageHandler == null)
        //    damageHandler = projectile.AddComponent<ProjectileDamage>();
        //damageHandler.damage = damage;

        // 可选：设置自动销毁
        //Destroy(projectile, projectileLifetime);
        // 延时回收，而不是销毁
        StartCoroutine(ReturnAfterDelay(projectile, projectileLifetime));
    }
    private System.Collections.IEnumerator ReturnAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ProjectilePool.Instance.Return(obj);
    }
    private System.Collections.IEnumerator MCReturnAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ProjectilePool.Instance.MCReturn(obj);
    }
}

/// <summary>
/// 简易小球移动组件（当没有 Rigidbody 时使用）
/// </summary>
public class ProjectileMovement : MonoBehaviour
{
    private Vector3 velocity;
    private float lifeTime;

    public void Initialize(Vector3 velocity, float lifeTime)
    {
        this.velocity = velocity;
        this.lifeTime = lifeTime;
        //Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += velocity * Time.deltaTime;
    }
}

/// <summary>
/// 小球碰撞伤害组件（可选）
/// </summary>
public class ProjectileDamage : MonoBehaviour
{
    public float damage = 10f;

    void OnCollisionEnter(Collision collision)
    {
        // 尝试获取目标身上的健康组件
        // 例如：Health health = collision.gameObject.GetComponent<Health>();
        // if (health != null) health.TakeDamage(damage);

        // 碰撞后销毁小球
        //Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        // 如果使用触发器
        // Health health = other.GetComponent<Health>();
        // if (health != null) health.TakeDamage(damage);
        //Destroy(gameObject);
    }
}
