using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GridGameUICtrl : UIcontroller
{
    public Image isPMode;
    public Slider sd;
    public TextMeshProUGUI gTText;//提示文本
    private Coroutine gTTCoroutine;//文本显示中协程
    void Start()
    {
        if (view["BuildingDataPanel/IsPModeButton/Image"].TryGetComponent(out Image a))
        {
            isPMode = a;
            isPMode.enabled = false;
        }
        if(view["GTText"].TryGetComponent(out TextMeshProUGUI t))
        {
            gTText = t;
            t.enabled = false;
        }
        add_button_listener("BuildingDataPanel/IsPModeButton", OnIsPModeButton);
        

    }
    public void OnIsPModeButton()
    {

        switch(GridInteractionManager.Instance.currentState)
        {
            case GridInteractionManager.InteractionState.Idle:
                isPMode.enabled = true; 
                GridInteractionManager.Instance.currentState = GridInteractionManager.InteractionState.Placing;
                break;
            case GridInteractionManager.InteractionState.Placing:
                isPMode.enabled=false;
                GridInteractionManager.Instance.previewObject.SetActive(false);
                GridInteractionManager.Instance.currentState = GridInteractionManager.InteractionState.Idle;
                break;
            default: break;
        }
    }
    public void ChangeGTText(string txt)
    {
        if (gTTCoroutine != null)
        {
            StopCoroutine(gTTCoroutine);
            gTTCoroutine = null;
        }
        gTText.enabled = true;
        gTText.text = txt;
        gTText.enabled = true;
        gTTCoroutine =StartCoroutine(AutoCloseGTT());
        
    }
    IEnumerator AutoCloseGTT()
    {
        yield return new WaitForSeconds(5f);
        gTTCoroutine = null;
        gTText.enabled = false;
    }
}
