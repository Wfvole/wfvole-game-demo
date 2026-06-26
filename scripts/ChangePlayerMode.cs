using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ChangePlayerModel : MonoBehaviour
{
    public GameplayManager.PlayerModel tPlayerModel;
    private bool isrange=false;
    private void OnEnable()
    {
        Eventmanager.Instance.AddListener("touchPick", CPMtouchpick);//信号来自hud被点击
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isrange = true;
            UImanager.Instance.ShowPickupTip("获取并切换人物模型:" + tPlayerModel.ToString());
            GameplayManager.Instance.readyPlayerModels.Add(tPlayerModel.ToString());
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isrange = false;
            UImanager.Instance.HidePickupTip();
        }
    }
    void CPMtouchpick(string eventName, object udata)
    {
        if (udata is bool d  &&d&&isrange)
        {
            if (GameplayManager.Instance.readyPlayerModels.Contains(tPlayerModel.ToString()))
            {
                Vector3 lposition=ThirdPersonShooterController.Instance.transform.position;
                Quaternion lrotation = ThirdPersonShooterController.Instance.transform.rotation;
                ThirdPersonShooterController.GameMode Mode = ThirdPersonShooterController.Instance.gameMode;
                bool iswear = PlayerWeaponManager.Instance.iswear;
                string wn="";
                if (iswear) wn = PlayerWeaponManager.Instance.currentWeapon.weaponName;
                Destroy(ThirdPersonShooterController.Instance.gameObject);
                string path="Player/"+tPlayerModel.ToString();
                Resourcemanger.Instance.LoadAssetAsync<GameObject>(path, prefab =>
                {
                    Instantiate(prefab,lposition,lrotation);
                    ThirdPersonShooterController.Instance.gameMode = Mode;
                    UImanager.Instance.InitTLJ();
                    if (iswear)
                    {
                        path = "weapon/" + wn;
                        WeaponData wd = Resourcemanger.Instance.GetAssetCache<GameObject>(path).GetComponentInChildren<PickupWeapon>().weaponData;
                        PlayerWeaponManager.Instance.currentWeapon = wd;
                        PlayerWeaponManager.Instance.EquipWeapon(wd);
                        ThirdPersonShooterController.Instance.weapon = wd.weapontype;
                    }
                });
                
            }
        }
    }

}
