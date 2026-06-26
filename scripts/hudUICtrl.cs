using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class hudUICtrl : UIcontroller
{
    private TextMeshProUGUI pickupTipText;    // 拾取提示文本
    private Image weaponIconImage;            // 武器图标
    private TextMeshProUGUI weaponInfoText;   // 武器信息文本
    bool getPressDown=false;
    void Start()
    {
        add_button_listener("PickupTipText", OnPickupTipText);
        // 在 Awake 中获取控件引用（假设路径已知）
        pickupTipText = view["PickupTipText"].GetComponent<TextMeshProUGUI>();
        weaponIconImage = view["WeaponIcon"].GetComponent<Image>();
        weaponInfoText = view["WeaponInfoText"].GetComponent<TextMeshProUGUI>();

        // 初始隐藏提示
        if (pickupTipText != null)
            pickupTipText.gameObject.SetActive(false);
            weaponIconImage.gameObject.SetActive(false);
            weaponInfoText.gameObject.SetActive(false);

    }
    void OnPickupTipText()
    {
        getPressDown = true;
        Eventmanager.Instance.Emit("touchPick", getPressDown);
    }

    public void ShowPickupTip(string message)
    {
        if (pickupTipText != null)
        {
            pickupTipText.text = message;
            pickupTipText.gameObject.SetActive(true);
        }
        //else Debug.Log("pickupTipText未识别");
    }

    public void HidePickupTip()
    {
        if (pickupTipText != null)
            pickupTipText.gameObject.SetActive(false);
    }

    public void UpdateWeaponUI(Sprite icon, string info)
    {
        weaponInfoText.gameObject.SetActive(true);
        weaponIconImage.gameObject.SetActive(true);
        if (weaponIconImage != null)
            weaponIconImage.sprite = icon;
        if (weaponInfoText != null)
            weaponInfoText.text = info;
    }
}