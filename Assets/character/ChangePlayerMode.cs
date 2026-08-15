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
    private void OnDisable()
    {
        Eventmanager.Instance.RemoveListener("touchPick", CPMtouchpick);
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isrange = true;
            string tName = tPlayerModel.ToString();
            if (!GameplayManager.Instance.readyPlayerModels.Contains(tName))
            {
                UImanager.Instance.ShowPickupTip("获取并切换人物模型:" + tName);
                GameplayManager.Instance.readyPlayerModels.Add(tName);
            }
            else
            {
                UImanager.Instance.ShowPickupTip("已获取人物模型:" + tName);
            }
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
                GlobalHairPhysics.Instance.ClearThisLists();
                GlobalHairPhysics.Instance.enabled = false;
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
                    GameplayManager.Instance.playerModel = ThirdPersonShooterController.Instance.playerModel;
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
