using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupWeapon : MonoBehaviour
{
    public WeaponData weaponData;
    private bool isInRange = false;
    //bool touchpick=false;
    private void Start()
    {
        Eventmanager.Instance.AddListener("touchPick", checktouchpick);
    }
    void checktouchpick(string eventName, object udata)
    {
        if (udata is bool d)
        {
            if (isInRange&&d) 
            {
                PickUp();
                isInRange = false;
                Eventmanager.Instance.Emit("touchPick", false);
            }
            
        }
    }
    void OnTriggerEnter(Collider other)
    {
        //Debug.Log("武器发生接触");
        if (other.CompareTag("Player")&&PlayerWeaponManager.Instance.iswear==false)
        {
            isInRange = true;
            // 显示UI提示
            UImanager.Instance.ShowPickupTip("点击拾取至装备位："+weaponData.weaponName);
            //Debug.Log("提示捡起");
        }
        //else Debug.Log("错误接触");
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInRange = false;
            UImanager.Instance.HidePickupTip();
        }
    }

    void Update()
    {
        //if (isInRange && (Input.GetKeyDown(KeyCode.E)||touchpick))
        //{
        //    PickUp();
        //} 
    }

    void PickUp()
    {
        // 通知玩家装备该武器
        PlayerWeaponManager.Instance.EquipWeapon(weaponData);
        PlayerWeaponManager.Instance.currentWeapon=weaponData;
        ThirdPersonShooterController.Instance.weapon = ThirdPersonShooterController.Weapon.Staff;
        UImanager.Instance.HidePickupTip();
        // 销毁场景中的武器模型
        Destroy(gameObject);
    }
}