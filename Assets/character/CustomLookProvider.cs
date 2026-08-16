using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
public class CustomLookProvider : MonoBehaviour, AxisState.IInputAxisProvider
{
    public InputActionReference XYAxis;
    public Vector2 lookInput; // 由摇杆脚本赋值
    public TInputModel inputModel=TInputModel.混合;
    public LookModel lookModel = LookModel.第三人称;
    public CinemachineFreeLook freeLook;
    public CinemachineCollider cc;
    public float zoomSpeed = 1f;
    public float minRadius = 0.5f;
    public float maxRadius = 6f;
    public float zoomSmoothTime = 0.15f;   // 缩放平滑时间，越大越柔
    private readonly float[] baseRadii = { 1f, 4f, 6f };  // 三轨道基础半径
    private float[] targetRadii;           // 缩放目标半径（滚轮修改）
    private float[] radiusVel = new float[3]; // SmoothDamp 速度缓存
    public Transform firstCamera;
    public Transform firstFllow;
    public Transform firstLookAT;
    public Transform thirdLookAT;
    public bool yAXisEnable=true;
    private int yZero;
    public enum TInputModel
    {
        触屏,
        键鼠,
        混合
    }
    public enum LookModel
    {
        第一人称,
        第三人称
    }
    private void OnEnable()
    {
        if (GameObject.FindWithTag("freeLookCamera") != null)
        {
            freeLook = GameObject.FindWithTag("freeLookCamera").GetComponent<CinemachineFreeLook>();
        }
        if(TryGetComponent(out CinemachineCollider a))cc=a;
        SetLookModel();
        Eventmanager.Instance.Emit("FLCready",true);
        if (inputModel != TInputModel.触屏) UpdateCursor();
    }
    void Update()
    {
        // 键鼠/混合模式：默认锁定隐藏光标，按住 Alt 释放显示（可操作 UI）
        if (inputModel != TInputModel.触屏) UpdateCursor();

        if (lookModel == LookModel.第一人称)
        {
            //firstLookAT.localPosition += XYAxis.action.ReadValue<Vector2>().y * Vector3.up*0.001f;
            return;
        }
        else
        {
            Eventmanager.Instance.Emit("MouseSroll" ,Input.GetAxis("Mouse ScrollWheel"));
            SmoothZoom();
        }
    }
    // Alt 是否按住（临时释放鼠标用）
    private bool IsAltPressed =>
        Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
    // 光标管理：平时锁定隐藏（鼠标移动转视野），按住 Alt 释放显示（操作 UI/菜单）
    private void UpdateCursor()
    {
        if (IsAltPressed)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }
        else
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
    }
    // 每帧把轨道半径向目标值平滑逼近，消除滚轮缩放/碰撞的瞬跳
    private void SmoothZoom()
    {
        if (freeLook == null || targetRadii == null) return;
        var orbits = freeLook.m_Orbits;
        for (int i = 0; i < 3; i++)
        {
            orbits[i].m_Radius = Mathf.SmoothDamp(
                orbits[i].m_Radius, targetRadii[i], ref radiusVel[i], zoomSmoothTime);
        }
    }
    public void SetLookModel()
    {
        if (freeLook==null) return;
        switch (lookModel) 
        {
            case LookModel.第一人称:
                //if (firstFllow==null||firstLookAT==null)
                //{
                //    freeLook.Follow = GameObject.Find("CameraRoot").transform;
                //    freeLook.LookAt = GameObject.Find("CameraLA").transform;
                //}
                //else
                //{
                //    freeLook .Follow = firstFllow;
                //    freeLook.LookAt = firstLookAT;
                //}
                //Eventmanager.Instance.RemoveListener("MouseSroll", ChangeR);
                //var orbits = freeLook.m_Orbits;
                //for (int i = 0; i < 3; i++)
                //{
                //    orbits[i].m_Height = 0;
                //    orbits[i].m_Radius = 0;
                //}
                freeLook.enabled = false;
                ThirdPersonShooterController.Instance.targetCamera = firstCamera;
                ThirdPersonShooterController.Instance.cameraTransform = firstCamera;
                SkinnedMeshRenderer sMR= ThirdPersonShooterController.Instance.GetComponentInChildren<SkinnedMeshRenderer>();
                sMR.enabled = false;
                Camera camera = Camera.main;
                camera.enabled = false;
                Eventmanager.Instance.RemoveListener("MouseSroll", ChangeR);
                break;
            case LookModel.第三人称:
                if(thirdLookAT!=null&&freeLook.LookAt==null)freeLook.LookAt = thirdLookAT;
                if (freeLook.Follow == null)
                    freeLook.Follow = gameObject.transform.parent != null
                        ? gameObject.transform.parent
                        : ThirdPersonShooterController.Instance != null
                            ? ThirdPersonShooterController.Instance.transform
                            : null;
                // 仅在首次进入第三人称时初始化轨道半径，之后保留玩家的缩放结果
                if (targetRadii == null)
                {
                    targetRadii = (float[])baseRadii.Clone();
                    var orbitss = freeLook.m_Orbits;
                    orbitss[0].m_Height = 0.5f;
                    orbitss[0].m_Radius = baseRadii[0];
                    orbitss[1].m_Height = 2.5f;
                    orbitss[1].m_Radius = baseRadii[1];
                    orbitss[2].m_Height = 5f;
                    orbitss[2].m_Radius = baseRadii[2];
                }
                Eventmanager.Instance.AddListener("MouseSroll", ChangeR);
                break;
        }
    }
    private void ChangeR(string eventName, object udata)
    {
        if (udata is float scroll)
        {
            if (Mathf.Approximately(scroll, 0) || targetRadii == null) return;
            // 只修改目标半径，实际半径由 SmoothZoom 每帧平滑逼近
            for (int i = 0; i < 3; i++)
            {
                float r = scroll > 0 ? targetRadii[i] * 1.2f : targetRadii[i] / 1.2f;
                targetRadii[i] = Mathf.Clamp(r, minRadius, maxRadius);
            }
        }
    }
    public float GetAxisValue(int axis)
    {
        if (yAXisEnable)yZero = 1;
        else yZero = 0;
        switch (inputModel)
        {
            case TInputModel.触屏:
                if (axis == 0) return zoomSpeed*lookInput.x;
                if (axis == 1) return zoomSpeed * -lookInput.y*yZero;
                break;
            case TInputModel.键鼠:
                if (IsAltPressed) return 0;   // 按住 Alt：截断鼠标视野
                InputAction act = XYAxis.action;
                if (axis == 0) return zoomSpeed * act.ReadValue<Vector2>().x;
                if (axis == 1) return zoomSpeed * -act.ReadValue<Vector2>().y * yZero;
                break;
            case  TInputModel.混合:
                InputAction acts = XYAxis.action;
                if (axis == 0)
                    return IsAltPressed
                        ? zoomSpeed * lookInput.x                    // Alt：仅保留触屏输入
                        : (lookInput.x + acts.ReadValue<Vector2>().x) * zoomSpeed;
                if (axis == 1)
                    return IsAltPressed
                        ? zoomSpeed * -lookInput.y * yZero
                        : (-lookInput.y - acts.ReadValue<Vector2>().y) * zoomSpeed * yZero;
                break;
        }
        return 0;
    }
    
}