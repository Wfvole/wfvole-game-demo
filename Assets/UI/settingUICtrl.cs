using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.XR.OpenVR;
public class settingUICtrl : UIcontroller
{
    public TextMeshProUGUI qs;
    public Slider sd;
    public Slider audioSd;
    public TMP_Dropdown dropdown;
    public Toggle tg;
    public TMP_Dropdown hmldd;
    // Start is called before the first frame update
    void Start()
    {
        qs = view["renderQualitySlider/QualityScale"].GetComponent<TextMeshProUGUI>();
        sd= view["renderQualitySlider"].GetComponent <Slider>();
        sd.value = QualityController.Instance.targetScale;
        audioSd= view["volumeSlider"].GetComponent<Slider>();
        GameObject a = GameObject.FindWithTag("audioSource");
        if (a!= null)
        {
            if (a.TryGetComponent<AudioSource>(out var v)) audioSd.value = v.volume;
        }
        qs.text=sd.value.ToString();
        dropdown = view["FPSDropdown"].GetComponent<TMP_Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string> { "30FPS", "60FPS", "90FPS", "120FPS" });
        GetDropdownValue();
        tg = view["HPToggle"].GetComponent<Toggle>();
        tg.isOn = GlobalHairPhysics.Instance.enabled;
        hmldd= view["HPBMaxLoopDropdown"].GetComponent<TMP_Dropdown>();
        hmldd.ClearOptions();
        hmldd.AddOptions(new List<string> { "Loop<=1", "maxLoop<=2", "maxLoop<=3" });
        hmldd.value = GlobalHairPhysics.Instance.constraintIterations-1;
        view["volumeSlider"].SetActive(false);
        view["mazeSpeedInputField"].SetActive(false);
        view["renderQualitySlider"].SetActive(false);
        add_button_listener("volumeButton", OnVolumeButton);
        add_slider_listener("volumeSlider", OnVolumeSlider);
        add_button_listener("mazaSpeedButton", OnMazaSpeedButton);
        add_inputfield_listener("mazeSpeedInputField", OnMazeSpeedInputField);
        add_button_listener("renderQualityButton", OnRenderQualityButton);
        add_slider_listener("renderQualitySlider",OnRenderQualitySlider);
        add_button_listener("backButton", OnBackButton);
        add_toggle_listener("HPToggle",OnHPToggle);
        add_dropdowne_listener("FPSDropdown", OnFPSDropdown);
        add_dropdowne_listener("HPBMaxLoopDropdown", OnHPBmaxLoopDropdown);
        add_button_listener("saveSettingButton", OnSaveSettingButton);
    }
    void OnSaveSettingButton()
    {
        SaveManager.Instance.WriteSetting();
    }
    void GetDropdownValue()
    {
        switch(limit_fps.Instance.limitfpstype)
        {
            case limit_fps.Limitfpstype.limit30fps:
                dropdown.value = 0;
                break;
            case limit_fps.Limitfpstype.limit60fps:
                dropdown.value = 1;
                break;
            case limit_fps.Limitfpstype.limit90fps:
                dropdown.value = 2;
                break;
            case limit_fps.Limitfpstype.limit120fps:
                dropdown.value = 3;
                break;
        }
    }
    void OnHPBmaxLoopDropdown(int e)
    {
        switch (e) 
        {
            case 0:
                GlobalHairPhysics.Instance.constraintIterations=1;
                break;
            case 1:
                GlobalHairPhysics.Instance.constraintIterations = 2;
                break;
            case 2:
                GlobalHairPhysics.Instance.constraintIterations=3;
                break;
        }
        GameplayManager.Instance.loopGHPM = GlobalHairPhysics.Instance.constraintIterations;
    }
    void OnFPSDropdown(int a) 
    { 
        switch (a)
        {
            case 0:
                limit_fps.Instance.limitfpstype = limit_fps.Limitfpstype.limit30fps;
                break; 
            case 1:
                limit_fps.Instance.limitfpstype = limit_fps.Limitfpstype.limit60fps;
                break; 
            case 2:
                limit_fps.Instance.limitfpstype = limit_fps.Limitfpstype.limit90fps;
                break; 
            case 3:
                limit_fps.Instance.limitfpstype = limit_fps.Limitfpstype.limit120fps;
                break; 
        }
        limit_fps.Instance.ResetFPS();
    }
    public void OnHPToggle(bool b)
    {
        //Debug.Log("触发勾选");
        //Debug.Log(b);
        GlobalHairPhysics.Instance.OffEnable();
        GameplayManager.Instance.enableGHPM = GlobalHairPhysics.Instance.enabled;
    }
    void ChangeAtive(string name)
    {
        bool b = view[name].activeSelf;
        view[name].SetActive(!b);
        
    }
    void OnVolumeButton()
    {
        ChangeAtive("volumeSlider");
    }
    void OnVolumeSlider(float f)
    {
        GameObject a = GameObject.FindWithTag("audioSource");
        if (a != null)
        {
            if (a.TryGetComponent<AudioSource>(out var v)) v.volume=audioSd.value;
        }
    }
    void OnMazaSpeedButton()
    {
        ChangeAtive("mazeSpeedInputField");
    }
    void OnMazeSpeedInputField(string t)
    { 
        bool b=int.TryParse(t,out int a);
        if (b) MazeGame.Instance.generator.generateSpeed = a;
        else UImanager.Instance.DialogTextMgr("输入字段无效");
    }
    void OnRenderQualityButton()
    {
        ChangeAtive("renderQualitySlider");
    }
    void OnRenderQualitySlider(float q)
    {
        qs.text = q.ToString();
        QualityController.Instance.targetScale = q;
        QualityController.Instance.ResetRenderScale();
    }
    void OnBackButton()
    {
        if (GameplayManager.Instance.gameStatus == GameplayManager.GameStatus.InPause)
        {
            UImanager.Instance.RemoveUIview("settingUI");
            UImanager.Instance.RemoveUIview("gamingUI");
            Resourcemanger.Instance.ClearCache();
            GlobalHairPhysics.Instance.ClearThisLists();
            GlobalHairPhysics.Instance.enabled = false;
            Eventmanager.Instance.Emit("Destroy", true);//发送销毁信号给MazeGame和gamelanch
            UImanager.Instance.ShowUIView("mainUI");
            GameplayManager.Instance.TurnToMainUI();
        }
        else 
        { 
            UImanager.Instance.RemoveUIview("settingUI"); 
            GameplayManager.Instance.TurnToMainUI(); 
        }

    }

}
