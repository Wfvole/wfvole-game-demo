using UnityEngine;

public class PlayerWeaponManager : UnitySingleton<PlayerWeaponManager>
{
    public Transform weaponHoldPoint;      // 闲置武器挂载点
    public Transform weaponATKPoint;      // 武器攻击挂载点
    public WeaponData currentWeapon;
    private GameObject currentWeaponModel;
    private FairyStaff ATKWP;
    public bool iswear;
    //void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Q))
    //    {
    //        WeaponAttack();
    //    }
    //}

    public void EquipWeapon(WeaponData newWeapon)
    {
        // 卸下当前武器（可选）
        if (currentWeaponModel != null)
            Destroy(currentWeaponModel);

        // 实例化新武器模型并挂载到武器点
        currentWeaponModel = Instantiate(newWeapon.weaponPrefab, weaponHoldPoint);
        iswear = true;
        currentWeaponModel.transform.localPosition =new Vector3(0,-0.5f,0);
        currentWeaponModel.transform.localRotation = Quaternion.identity;
        //对武器攻击脚本进行更新
        currentWeaponModel.GetComponent<FairyStaff>().firePoint = ThirdPersonShooterController.Instance.transform;
        currentWeapon = newWeapon;
        //绑定攻击效果脚本
        ATKWP = currentWeaponModel.GetComponent<FairyStaff>();
        if (ATKWP.projectilePrefab == null)//通过资源管理器添加小球预制体
        {
            string path = "Player/Sphere";
            ATKWP.projectilePrefab = Resourcemanger.Instance.GetAssetCache<GameObject>(path);
        }
        if (true||ATKWP.firePoint == null)
        {
            ATKWP.firePoint=ThirdPersonShooterController.Instance.transform;
        }
        // 更新UI
        UImanager.Instance.UpdateWeaponUI(newWeapon.icon,newWeapon.weaponIntroduction);
        // 播放装备动画（可调用动画控制器）
        //ThirdPersonShooterController.Instance.animator.SetTrigger("Equip");
    }

    public void WeaponAttack()
    {
        if (ThirdPersonShooterController.Instance.weapon != ThirdPersonShooterController.Weapon.Nothing)
        {
            //发动攻击
            currentWeaponModel.transform.parent = weaponATKPoint;
            currentWeaponModel.transform.localPosition = new Vector3(0, -0.5f, 0);
            currentWeaponModel.transform.localRotation = Quaternion.identity;
            //后续根据武器类型sword/staff播放人物攻击动画
            ThirdPersonShooterController.Instance.animator.SetTrigger("Attack");
            //实现武器介绍攻击效果
            ATKWP.Attack();
        }


    }
}