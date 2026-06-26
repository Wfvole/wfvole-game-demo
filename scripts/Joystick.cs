using UnityEngine;
using UnityEngine.EventSystems;

public class Joystick : MonoBehaviour, IDragHandler, IEndDragHandler
{
    // 摇杆背景所在的Canvas（用于坐标转换）
    private Canvas cs;
    // 摇杆中心小圆点的Transform
    public Transform stick;
    // 摇杆可拖动的最大半径（单位：像素）
    public float max_R = 80;
    // 当前摇杆的方向向量（归一化后的值），外部通过属性dir读取
    private Vector2 touch_dir = Vector2.zero;
    public Vector2 dir
    {
        get { return this.touch_dir; }
    }

    void Start()
    {
        // 获取场景中的Canvas组件，用于坐标转换时确定参照相机
        this.cs = GameObject.Find("Canvas").GetComponent<Canvas>();
        // 将摇杆小圆点置于背景中心
        this.stick.localPosition = Vector2.zero;
        // 初始方向为零
        this.touch_dir = Vector2.zero;
    }

    void Update()
    {
        // 通过事件管理器发射当前摇杆方向（供其他模块监听）
        Eventmanager.Instance.Emit("JoyStick", this.touch_dir);
    }

    // 实现IDragHandler接口，当拖拽时调用
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos = Vector2.zero;
        // 使用事件数据中的屏幕位置转换为摇杆背景的局部坐标
        // 参数1：背景的RectTransform；参数2：屏幕坐标；参数3：UI相机；输出：局部坐标pos
        // 使用eventData.pressEventCamera通常指向渲染UI的相机
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            this.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out pos
        );

        float len = pos.magnitude;
        if (len <= 0)
        {
            this.touch_dir = Vector2.zero;
            return;
        }

        // 计算方向（归一化）
        this.touch_dir.x = pos.x / len;
        this.touch_dir.y = pos.y / len;

        // 限制摇杆小圆点位置
        if (len >= this.max_R)
        {
            pos.x = pos.x * this.max_R / len;
            pos.y = pos.y * this.max_R / len;
        }

        // 更新摇杆小圆点位置
        this.stick.localPosition = pos;
    }

    // 实现IEndDragHandler接口，当拖拽结束时调用
    public void OnEndDrag(PointerEventData eventData)
    {
        // 将摇杆复位到中心
        this.stick.localPosition = Vector2.zero;
        // 方向置零
        this.touch_dir = Vector2.zero;
    }
}