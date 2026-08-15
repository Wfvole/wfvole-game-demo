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
    public float minRadius = 1f;
    public float maxRadius = 10f;
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
    }
    void Update()
    {
        if (lookModel == LookModel.第一人称)
        {
            //firstLookAT.localPosition += XYAxis.action.ReadValue<Vector2>().y * Vector3.up*0.001f;
            return;
        }
        else Eventmanager.Instance.Emit("MouseSroll" ,Input.GetAxis("Mouse ScrollWheel"));
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
                if(freeLook==null)freeLook.Follow = gameObject.transform.parent.transform;
                var orbitss = freeLook.m_Orbits;
                orbitss[0].m_Height = 0.5f;
                orbitss[0].m_Radius = 1f;
                orbitss[1].m_Height = 2.5f;
                orbitss[1].m_Radius = 4f; 
                orbitss[2].m_Height = 5f;
                orbitss[2].m_Radius = 6f;
                Eventmanager.Instance.AddListener("MouseSroll", ChangeR);
                break;
        }
    }
    private void ChangeR(string eventName, object udata)
    {
        if (udata is float scroll) 
        {
            if (Mathf.Approximately(scroll, 0)) return;
            // 获取当前的半径值数组，并调整每个轨道的半径
            var orbits = freeLook.m_Orbits;
            if (scroll > 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (orbits[i].m_Radius >= 6f) continue;
                    orbits[i].m_Radius *= 1.2f;
                }   
            }
            else if (scroll < 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (orbits[i].m_Radius <= 0.5f) continue;
                    orbits[i].m_Radius *= 0.9f;
                } 
            }
        }
    }
    public float GetAxisValue(int axis)
    {
        if (yAXisEnable)yZero = 1;
        else yZero = 0;
        if(Input.GetKey(KeyCode.LeftAlt))zoomSpeed = 0;
        else zoomSpeed = 1;
        switch (inputModel)
        {
            case TInputModel.触屏:
                if (axis == 0) return zoomSpeed*lookInput.x;
                if (axis == 1) return zoomSpeed * -lookInput.y*yZero;
                break;
            case TInputModel.键鼠:
                InputAction act = XYAxis.action;
                if (axis == 0) return zoomSpeed * act.ReadValue<Vector2>().x;
                if (axis == 1) return zoomSpeed * -act.ReadValue<Vector2>().y * yZero;
                break;
            case  TInputModel.混合:
                InputAction acts = XYAxis.action;
                if (axis == 0) return (lookInput.x+acts.ReadValue<Vector2>().x)*zoomSpeed;
                if (axis == 1) return (-lookInput.y - acts.ReadValue<Vector2>().y) * zoomSpeed * yZero;
                break;
        }
        return 0;
    }
    
}