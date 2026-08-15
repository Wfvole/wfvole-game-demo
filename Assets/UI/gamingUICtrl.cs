using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
public class gamingUICtrl : UIcontroller
{
    public Toggle ytg;
    public Toggle ctg;
    // Start is called before the first frame update
    void Start()
    {
        add_button_listener("jumpButton", OnJumpButton);
        add_button_listener("crouchButton", OnCrouchButton); 
        add_button_listener("shiftButton", OnShiftButton);
        add_button_listener("attackButton", OnAttackButton);
        add_button_listener("pauseButton", OnPauseButton);
        add_button_listener("Button", OnButton1);
        ytg = view["CYToggle"].GetComponent<Toggle>();
        ctg= view["CCToggle"].GetComponent<Toggle>();
        add_toggle_listener("CYToggle", OnCYToggle);
        add_toggle_listener("CCToggle", OnCCToggle);
        Eventmanager.Instance.AddListener("FLCready",GetFLCmassage);
    }
    void GetFLCmassage(string a,object b)
    {
        if (b is bool o && o)
        {
            GetYTGIsOn(true);
            Eventmanager.Instance.RemoveListener("FLCready", GetFLCmassage);
        }
    }
    public void GetYTGIsOn(bool b)
    {
        //b真为校对ytg.isOn,b假则调整yAXisEnable
        GameObject g = GameObject.FindGameObjectWithTag("freeLookCamera");
        if (g != null)
        {
            if (g.TryGetComponent<CustomLookProvider>(out var v)&&b)ytg.isOn = v.yAXisEnable;
            else v.yAXisEnable = !v.yAXisEnable;
        }
    }
    public void GetCTGIsOn(bool b)
    {
        //b真为校对ytg.isOn,b假则调整yAXisEnable
        GameObject g = GameObject.FindGameObjectWithTag("freeLookCamera");
        if (g != null)
        {
            if (g.TryGetComponent<CinemachineCollider>(out var v) && b) ctg.isOn = v.enabled;
            else v.enabled = !v.enabled;
        }
    }
    void OnCCToggle(bool b)
    {
        GetCTGIsOn(false);
    }
    void OnCYToggle(bool b)
    {
        GetYTGIsOn(false);
    }
    void OnButton1()
    {
        GlobalHairPhysics.Instance.OffEnable();
    }
    void OnJumpButton()
    {
        ThirdPersonShooterController.Instance.isJumping = true;
    }
    void OnCrouchButton()
    {
        ThirdPersonShooterController.Instance.isCrouch = !ThirdPersonShooterController.Instance.isCrouch;
    }
    void OnShiftButton()
    {
        ThirdPersonShooterController.Instance.isRunning =!ThirdPersonShooterController.Instance.isRunning;
    }
    public void OnAttackButton()
    {
        //Debug.Log("按下攻击键");
        PlayerWeaponManager.Instance.WeaponAttack();
    }
    public void OnPauseButton()
    {
        ThirdPersonShooterController.Instance.listenP=!ThirdPersonShooterController.Instance.listenP;
        Eventmanager.Instance.Emit("ListenPause", ThirdPersonShooterController.Instance.listenP);
    }
    public void TLJg()
    {
        var t1 = view["touch1"].GetComponent<RightLookJoystick>();
        if (t1 != null)
        {
            t1.enabled = false;
            StartCoroutine(WaitSomeTime(0.5f));
        }
    }
    IEnumerator WaitSomeTime(float time)
    {
        yield return new WaitForSeconds(time);
        var t1 = view["touch1"].GetComponent<RightLookJoystick>();
        if (t1 != null) t1.enabled = true;
        

    }
}
