using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public GameObject weaponPrefab;      // 拾取时实例化的模型（或捡起后用于装备的模型）
    public Sprite icon;
    public float damage;
    public string weaponIntroduction;
    public ThirdPersonShooterController.Weapon weapontype;
    // 其他属性...
}