using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class dialogUICtrl : UIcontroller
{
    private TextMeshProUGUI dialogText;    // 对话框文本
    private Image background;
    // Start is called before the first frame update
    void Start()
    {
        dialogText = view["Image/dialogText"].GetComponent<TextMeshProUGUI>();
        background = view["Image"].GetComponent<Image>();
        if (dialogText == null) Debug.Log("找不到文本组件");
        if (background == null) Debug.Log("找不到图像组件");
        //初始隐藏
        dialogText.gameObject.SetActive(false);
        background.gameObject.SetActive(false);
    }
    public void SetUIActive(bool b)
    {
        dialogText.gameObject.SetActive(b);
        background.gameObject.SetActive(b);
    }
    public void ChangeDText(string t)
    {
        dialogText.text = t;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
