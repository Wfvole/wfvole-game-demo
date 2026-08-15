using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SphereGame : MonoBehaviour
{
    [Header("灵敏度")]
    public float lmd = 50f;
    [Header("旋转轴")]
    public Vector3 axis = Vector3.up;
    [Header("按钮")]
    public Button btnClockwise;      // 顺时针
    public Button btnCounterClockwise; // 逆时针

    private bool isClockwise = false;
    private bool isCounterClockwise = false;

    void Start()
    {
        // 为按钮绑定按下/松开事件
        BindButton(btnClockwise,    (down) => isClockwise = down);
        BindButton(btnCounterClockwise, (down) => isCounterClockwise = down);
    }

    void BindButton(Button btn, System.Action<bool> setState)
    {
        if (btn == null) return;

        EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = btn.gameObject.AddComponent<EventTrigger>();

        // 按下
        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((data) => setState(true));
        trigger.triggers.Add(entryDown);

        // 松开
        EventTrigger.Entry entryUp = new EventTrigger.Entry();
        entryUp.eventID = EventTriggerType.PointerUp;
        entryUp.callback.AddListener((data) => setState(false));
        trigger.triggers.Add(entryUp);
    }

    void Update()
    {
        if (isClockwise)
        {
            transform.Rotate(axis * lmd * Time.deltaTime);
        }
        if (isCounterClockwise)
        {
            transform.Rotate(axis * -lmd * Time.deltaTime);
        }
    }
}
