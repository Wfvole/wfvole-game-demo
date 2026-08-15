using UnityEngine;
using UnityEngine.EventSystems;
using Cinemachine;
using System.Collections;

public class RightLookJoystick : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [Header("摇杆外观")]
    public RectTransform stick;          // 摇杆拇指的 RectTransform
    public float maxRadius = 80f;        // 最大拖拽半径（像素）
    public float sensitivity = 1f;       // 灵敏度

    [Header("目标摄像机")]
    public CinemachineFreeLook freeLook; // 场景中的 FreeLook 相机（拖拽赋值）
    Vector2 pos;
    Vector2 lastPos;             // 上一帧拖拽位置
    public bool isDragging;
    public Vector2 delta;
    public CustomLookProvider lookProvider;

    void OnEnable()
    {
        StartCoroutine(GetFLandLP());
        pos = Vector2.zero;
        lastPos = Vector2.zero;
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
        if (lookProvider != null&&isDragging)
        {
            delta = (pos - lastPos) * sensitivity;
            lastPos = pos;
            lookProvider.lookInput = new Vector2(delta.x, -delta.y);
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        isDragging = true;
        // 显示摇杆
        if (stick != null && !stick.gameObject.activeSelf)
            stick.gameObject.SetActive(true);

        // 获取拖拽位置（相对于摇杆底座中心）
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out pos);

        // 限制拖拽半径
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
        lookProvider.lookInput = new Vector2(delta.x, -delta.y);
        // 复位摇杆
        if (stick != null)
        {
            stick.localPosition = Vector2.zero;
            //stick.gameObject.SetActive(false);
        }
        lastPos = Vector2.zero;

        //// 停止轴输入
        //freeLook.m_XAxis.m_InputAxisValue = 0;
        //freeLook.m_YAxis.m_InputAxisValue = 0;
    }
}