using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class gamingUICtrl : UIcontroller
{
    // Start is called before the first frame update
    void Start()
    {
        add_button_listener("jumpButton", OnJumpButton);
        add_button_listener("crouchButton", OnCrouchButton); 
        add_button_listener("shiftButton", OnShiftButton);
        add_button_listener("attackButton", OnAttackButton);
        add_button_listener("pauseButton", OnPauseButton);
        add_button_listener("Button", OnButton1);

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
        //Debug.Log("°´ÏÂ¹¥»÷¼ü");
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
