using UnityEngine;
using UnityEngine.EventSystems;
using Cinemachine;
using System.Collections;

public enum LookInputMode
{
    Velocity,  // 归一化速度：推多少给多少持续速度（帧率无关，手感稳）
    Delta      // 帧增量：每帧手指位移差（原始手感，受帧率影响）
}

public class RightLookJoystick : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [Header("输入模式")]
    public LookInputMode inputMode = LookInputMode.Velocity;

    [Header("摇杆外观")]
    public RectTransform stick;          // 摇杆拇指的 RectTransform
    public float maxRadius = 80f;        // 最大拖拽半径（像素）
    public float sensitivity = 1f;       // 灵敏度

    [Header("目标摄像机")]
    public CinemachineFreeLook freeLook; // 场景中的 FreeLook 相机（拖拽赋值）
    Vector2 pos;                         // 摇杆当前偏移（限制在 maxRadius 内，外观/Velocity 模式用）
    Vector2 rawPos;                      // 未截断的原始指针位置（Delta 模式输入源，不受半径限制）
    Vector2 lastRawPos;                  // 上一帧原始指针位置
    public bool isDragging;
    public Vector2 delta;                // 保留：帧间位移差（调试用）
    public CustomLookProvider lookProvider;

    void OnEnable()
    {
        StartCoroutine(GetFLandLP());
        pos = Vector2.zero;
        rawPos = Vector2.zero;
        lastRawPos = Vector2.zero;
    }

    IEnumerator GetFLandLP()
    {
        if (freeLook == null)
        {
            while (ThirdPersonShooterController.Instance == null)
            {
                yield return null;
            }
            if (GameObject.FindWithTag("freeLookCamera")!=null)
            {
                freeLook = GameObject.FindWithTag("freeLookCamera").GetComponent<CinemachineFreeLook>();
                lookProvider = freeLook.GetComponent<CustomLookProvider>();
            }
            if (lookProvider == null)
            {
                lookProvider = freeLook.gameObject.GetComponent<CustomLookProvider>();
            }
            yield break;
        }
        else 
        { 
            //Debug.Log("协程结束");
            yield break; 
        }
    }
    void Update()
    {
        if (lookProvider != null && isDragging)
        {
            switch (inputMode)
            {
                case LookInputMode.Velocity:
                    // 归一化速度：摇杆偏移/最大半径 → [-1,1] 的持续速度输入
                    // 好处：与帧率无关，手指停在边缘保持匀速旋转，松手即停
                    {
                        Vector2 v = (pos / maxRadius) * sensitivity;
                        delta = new Vector2(v.x, -v.y);
                        lookProvider.lookInput = new Vector2(delta.x, delta.y);
                    }
                    break;

                case LookInputMode.Delta:
                    // 帧增量：每帧用未截断的原始位置算位移差
                    // 不受 maxRadius 截断影响（急速拖动准确），手指停住时差值为 0（不旋转）
                    {
                        delta = (rawPos - lastRawPos) * sensitivity;
                        lastRawPos = rawPos;
                        lookProvider.lookInput = new Vector2(delta.x, -delta.y);
                    }
                    break;
            }
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        isDragging = true;

        // 显示摇杆
        if (stick != null && !stick.gameObject.activeSelf)
            stick.gameObject.SetActive(true);

        // 获取拖拽位置（相对于摇杆底座中心）
        // rawPos：未截断的原始位置（Delta 模式输入源，保留真实位移）
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out rawPos);

        // pos：限制拖拽半径后的位置（外观显示 / Velocity 模式用）
        pos = rawPos;
        float len = pos.magnitude;
        if (len > maxRadius)
            pos = pos.normalized * maxRadius;

        if (stick != null)
            stick.localPosition = pos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (lookProvider==null) return;
        isDragging = false;
        delta = Vector2.zero;
        lookProvider.lookInput = Vector2.zero;
        // 复位摇杆
        if (stick != null)
        {
            stick.localPosition = Vector2.zero;
            //stick.gameObject.SetActive(false);
        }
        pos = Vector2.zero;
        rawPos = Vector2.zero;
        lastRawPos = Vector2.zero;
    }
}