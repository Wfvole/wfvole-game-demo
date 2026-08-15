using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pauseUICtrl : UIcontroller
{
    // Start is called before the first frame update
    void Start()
    {
        add_button_listener("comebackButton", OnComeBackButton);
        add_button_listener("psaveButton", OnPSaveButton);
        add_button_listener("ploadButton", OnPLoadButton);
        add_button_listener("psettingButton", OnPSettingButton);
        add_button_listener("getunstuckButton", OnGetUnstuckButtonButton);
    }
    void OnComeBackButton()
    {
        ThirdPersonShooterController.Instance.listenP = false;
        string ui = "pauseUI";
        UImanager.Instance.RemoveUIview(ui);
        GameplayManager.Instance.TurnToGamingUI();

    }
    void OnPSettingButton()
    {
        UImanager.Instance.RemoveUIview("pauseUI");
        GameplayManager.Instance.TurnToSettingUI();
        UImanager.Instance.ShowUIView("settingUI");
    }
    void OnPSaveButton()
    {
        Time.timeScale = 1f;
        SaveManager.Instance.SavePlayDate();
        Time.timeScale = 0f;
    }
    void OnPLoadButton()
    {
        Time.timeScale = 1f;
        SaveManager.Instance.LoadPlayDate();
    }
    void OnGetUnstuckButtonButton()
    {
        ThirdPersonShooterController.Instance.transform.position = new Vector3(0, 3f, 0);
    }

}
