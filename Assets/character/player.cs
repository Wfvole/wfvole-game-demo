using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class ThirdPersonShooterController : UnitySingleton<ThirdPersonShooterController>
{
    public GameplayManager.PlayerModel playerModel;
    public Transform playerTransform;
    public Animator animator;
    public Transform cameraTransform;
    CharacterController characterController;
    public Transform targetCamera;
    private Vector2 joystickDir = Vector2.zero; // 从事件接收的摇杆方向
    //下蹲_保存cc组件需用到的原始参数
    private float originalHeight;   // 记录原始高度
    private Vector3 originalCenter; // 记录原始中心位置

    #region 玩家姿态及相关动画参数阈值
    public enum PlayerPosture
    {
        Crouch,
        Stand,
        Falling,
        Jumping,
        Landing
    };
    [HideInInspector]
    public PlayerPosture playerPosture = PlayerPosture.Stand;

    readonly float crouchThreshold = 0f;
    readonly float standThreshold = 1f;
    readonly float midairThreshold = 2.1f;
    float landingThreshold = 1f;
    #endregion

    //玩家运动状态
    public enum LocomotionState
    {
        Idle,
        Walk,
        Run
    };
    [HideInInspector]
    public LocomotionState locomotionState = LocomotionState.Idle;

    public enum GameMode
    {
        maze,
        Paradise
    }
    public GameMode gameMode;

    //玩家状态瞄准/常态
    public enum ArmState
    {
        Normal,
        Aim
    };
    [HideInInspector]
    public ArmState armState = ArmState.Normal;

    //玩家装备武器状态,
    public enum Weapon
    {
        Nothing,
        Staff,
        Sword
    }
    public Weapon weapon=Weapon.Nothing;
    //玩家不同状态的运动速度
    public float crouchSpeed = 0.56f;
    public float walkSpeed = 1.55f;
    public float runSpeed = 5.66f;

    #region 输入值
    Vector2 moveInput;
    Vector2 combined;
    [HideInInspector]
    public bool isRunning;
    [HideInInspector]
    public bool isCrouch;
    bool isAiming;
    [HideInInspector] 
    public bool isJumping;
    #endregion

    #region 状态机参数的ID
    string postureID;
    string moveSpeedID;
    string turnSpeedID;
    string verticalVelID;
    string feetTweenID;
    #endregion


    Vector3 playerMovement = Vector3.zero;

    //重力
    public float gravity = -9.8f;

    //垂直方向速度
    float VerticalVelocity;

    //最大跳起高度
    public float maxHeight = 1.5f;

    //滞空左右脚状态
    float feetTween;

    #region 速度缓存池定义
    static readonly int CACHE_SIZE = 3;
    readonly Vector3[] velCache = new Vector3[CACHE_SIZE];
    int currentChacheIndex = 0;
    Vector3 averageVel = Vector3.zero;
    #endregion

    //下落时加速度的倍数
    readonly float fallMultiplier = 1.5f;

    //玩家是否着地
    bool isGrounded;

    //玩家是否可以跌落
    bool couldFall;

    //跌落的最小高度，小于此高度不会切换到跌落姿态
    readonly float fallHeight = 0.5f;

    //是否处于跳跃CD状态
    bool isLanding;

    //地标检测射线的偏移量
    readonly float groundCheckOffset = 0.5f;

    //跳跃的CD设置
    readonly float jumpCD = 0.15f;

    //上一帧的动画nornalized时间
    //float lastFootCycle = 0;

    //是否可以重新站起
    bool iscouldstand;

    Vector3 Zdir;

    public bool listenP=false;

    [Header("速度增益")]
    //private float originalWalkSpeed;
   // private float originalRunSpeed;
    private Coroutine speedBoostCoroutine;   //是否加速中
    private Coroutine lookedByGuardCoroutine;//是否减速中
    // Start is called before the first frame update
    private void OnEnable()
    {
        playerTransform = transform;
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        postureID = "玩家姿态";
        moveSpeedID = "移动速度";
        turnSpeedID = "转弯速度";
        verticalVelID = "垂直速度";
        feetTweenID = "左右脚";
        //Cursor.lockState = CursorLockMode.Locked;
        // 记录cc部分参数原始值，以便后续恢复站立状态
        originalHeight = characterController.height;
        originalCenter = characterController.center;
        //接收摇杆输入
        Eventmanager.Instance.AddListener("JoyStick", OnJoystickInput);
    }
    private void OnDisable()
    {
        //Eventmanager.Instance.RemoveListener("JoyStick", OnJoystickInput);
    }
    void Start()
    {
        
        if (targetCamera == null)
        {
            targetCamera = GameObject.Find("main camera").transform;
        }
        cameraTransform = targetCamera;
        //Cursor.lockState = CursorLockMode.Locked;
        // 记录cc部分参数原始值，以便后续恢复站立状态
        originalHeight = characterController.height;
        originalCenter = characterController.center;
        //接收摇杆输入
        Eventmanager.Instance.AddListener("JoyStick", OnJoystickInput);
        GlobalHairPhysics.Instance.enabled = GameplayManager.Instance.enableGHPM;
        GlobalHairPhysics.Instance.constraintIterations = GameplayManager.Instance.loopGHPM;
    }

    // Update is called once per frame
    void Update()
    {
        CheckGround();
        Crouchcollider();
        SwitchPlayerStates();
        CaculateGravity();
        Jump();
        CaculateInputDirection();
        SetupAnimator();
        //PlayFootStep();
        //向事件管理器发送自身z轴方向在世界坐标的向量，用于方向盘UI监听
        this.Zdir = transform.TransformDirection(Vector3.forward);
        Eventmanager.Instance.Emit("playerZ", this.Zdir);
    }
    public void ApplyLookedByGuard(float multiplier, float duration)
    {
        if (lookedByGuardCoroutine != null)
        {
            StopCoroutine(lookedByGuardCoroutine);
            animator.speed = 1f;
            UImanager.Instance.OffDialog();
            lookedByGuardCoroutine = StartCoroutine(LookedByGuard(duration));
        }
        animator.speed = multiplier;
        speedBoostCoroutine = StartCoroutine(LookedByGuard(duration));
    }
    private IEnumerator LookedByGuard(float duration) 
    {
        yield return new WaitForSeconds(duration);
        animator.speed = 1f;
        speedBoostCoroutine = null;
        UImanager.Instance.OffDialog();
    }
    public void ApplySpeedBoost(float multiplier, float duration)
    {
        //Debug.Log("ApplySpeedBoost触发");
        // 如果已有增益，先恢复再应用新增益（避免叠加）
        if (speedBoostCoroutine != null)
        {
            StopCoroutine(speedBoostCoroutine);
            // 恢复原始速度
            animator.speed = 1f;
            //walkSpeed = originalWalkSpeed;
            //runSpeed = originalRunSpeed;
            UImanager.Instance.OffDialog();
            speedBoostCoroutine = StartCoroutine(SpeedBoostCoroutine(duration));
        }
        // 记录当前速度作为原始速度
        //originalWalkSpeed = walkSpeed;
        //originalRunSpeed = runSpeed;
        // 应用增益
        animator.speed = multiplier;
        //walkSpeed *= multiplier;
        //runSpeed *= multiplier;
        // 启动协程恢复速度
        speedBoostCoroutine = StartCoroutine(SpeedBoostCoroutine(duration));
    }

    private IEnumerator SpeedBoostCoroutine(float duration)
    {
        //Debug.Log("IEnumerator SpeedBoostCoroutine触发");
        yield return new WaitForSeconds(duration);
        //walkSpeed = originalWalkSpeed;
        //runSpeed = originalRunSpeed;
        animator.speed = 1f;
        speedBoostCoroutine = null;
        UImanager.Instance.OffDialog();
    }
    private void OnJoystickInput(string eventName, object udata)
    {
        if (udata is Vector2 dir)
        {
            joystickDir = dir;
        }
    }
    #region 输入相关
    public void GetMoveInput(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }
    public void ListenPause(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            listenP = !listenP;
            Eventmanager.Instance.Emit("ListenPause",listenP);
        }
    }

    public void GetRunInput(InputAction.CallbackContext ctx)
    {
        if(ctx.performed) isRunning = !isRunning;
    }

    public void GetCrouchInput(InputAction.CallbackContext ctx)
    {
        //isCrouch = ctx.ReadValueAsButton();
        if (ctx.performed) // 按下瞬间
        {
            isCrouch = !isCrouch; // 切换状态
        }
    }

    public void GetAimInput(InputAction.CallbackContext ctx)
    {
        isAiming = ctx.ReadValueAsButton();
    }

    public void GetJumpInput(InputAction.CallbackContext ctx)
    {
        isJumping = ctx.ReadValueAsButton();
    }

    #endregion


    /// 用于切换玩家的各种状态
    void SwitchPlayerStates()
    {
        if (!isGrounded)
        {
            if (VerticalVelocity > 0)
            {
                playerPosture = PlayerPosture.Jumping;
            }
            else if (playerPosture != PlayerPosture.Jumping)
            {
                if (couldFall)
                {
                    playerPosture = PlayerPosture.Falling;
                }
            }

        }
        else if (playerPosture == PlayerPosture.Jumping)
        {
            StartCoroutine(CoolDownJump());
        }
        else if (isLanding)
        {
            playerPosture = PlayerPosture.Landing;
        }
        else if(isCrouch||!iscouldstand)
        {
            playerPosture = PlayerPosture.Crouch;
        }
        else
        {
            playerPosture = PlayerPosture.Stand;
        }

        if (combined.magnitude == 0)
        {
            locomotionState = LocomotionState.Idle;
        }
        else if (!isRunning)
        {
            locomotionState = LocomotionState.Walk;
        }
        else
        {
            locomotionState = LocomotionState.Run;
        }

        if (isAiming)
        {
            armState = ArmState.Aim;
        }
        else
        {
            armState = ArmState.Normal;
        }
    }

    void Crouchcollider()
    {
        Couldrestand();
        if(isCrouch)
        {

            characterController.center = new Vector3(originalCenter.x, originalCenter.y *0.6f , originalCenter.z);
            characterController.height =originalHeight *0.6f;
        }
        else if(!isCrouch && iscouldstand)
        {
            characterController.center = originalCenter;
            characterController.height = originalHeight;
        }
    }
    void Couldrestand()
    {
        if (playerPosture == PlayerPosture.Crouch)
        {
            float checkRadius = characterController.radius;
            Vector3 headCheckPosition = transform.position + Vector3.up * (characterController.height + checkRadius);
            iscouldstand = !Physics.CheckSphere(headCheckPosition, checkRadius);
        }
        else iscouldstand = true;
    }

    /// 落地检测
    void CheckGround()
    {
        if (Physics.SphereCast(playerTransform.position + (Vector3.up * groundCheckOffset), characterController.radius, Vector3.down, hitInfo: out _, groundCheckOffset - characterController.radius + 2 * characterController.skinWidth))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
            couldFall = !Physics.Raycast(playerTransform.position, Vector3.down, fallHeight);
        }
    }

    /// 计算跳跃CD
    IEnumerator CoolDownJump()
    {
        landingThreshold = Mathf.Clamp(VerticalVelocity, -10, 0);
        landingThreshold /= 20f;
        landingThreshold += 1f;
        isLanding = true;
        playerPosture = PlayerPosture.Landing;
        yield return new WaitForSeconds(jumpCD);
        isLanding = false;
    }

    /// 计算下落速度
    void CaculateGravity()
    {
        if (playerPosture != PlayerPosture.Jumping && playerPosture != PlayerPosture.Falling)
        {
            if (!isGrounded)
            {
                VerticalVelocity += gravity * fallMultiplier * Time.deltaTime;
            }
            else
            {
                VerticalVelocity = gravity * Time.deltaTime;
            }
        }
        else
        {
            if (VerticalVelocity <= 0 || !isJumping)
            {
                VerticalVelocity += gravity * fallMultiplier * Time.deltaTime;
            }
            else
            {
                VerticalVelocity += gravity * Time.deltaTime;
            }
        }

    }


    /// 跳跃方法
    void Jump()
    {
        if (playerPosture == PlayerPosture.Stand && isJumping)
        {
            VerticalVelocity = Mathf.Sqrt(-2 * gravity * maxHeight);
            feetTween = Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);
            feetTween = feetTween < 0.5f ? 1 : -1;
            if (locomotionState == LocomotionState.Run)
            {
                feetTween *= 3;
            }
            else if (locomotionState == LocomotionState.Walk)
            {
                feetTween *= 2;
            }
            else
            {
                feetTween = Random.Range(0.5f, 1f) * feetTween;
            }
            isJumping = false;
        }
    }


    /// 计算玩家输入相对于相机的方向
    void CaculateInputDirection()
    {
        combined = (moveInput + joystickDir).normalized;
        if (combined.sqrMagnitude > 1f) combined.Normalize(); // 避免超出单位圆
        //Debug.Log(combined);
        //Debug.Log(moveInput);
        Vector3 camForwardProjection = new Vector3(cameraTransform.forward.x, 0, cameraTransform.forward.z).normalized;
        playerMovement = camForwardProjection * combined.y + cameraTransform.right * combined.x;
        playerMovement = playerTransform.InverseTransformVector(playerMovement);
    }


    /// 设置动画状态机的参数
    void SetupAnimator()
    {
        if (playerPosture == PlayerPosture.Stand)
        {
            animator.SetFloat(postureID, standThreshold, 0.1f, Time.deltaTime);
            switch (locomotionState)
            {
                case LocomotionState.Idle:
                    animator.SetFloat(moveSpeedID, 0, 0.1f, Time.deltaTime);
                    break;
                case LocomotionState.Walk:
                    animator.SetFloat(moveSpeedID, playerMovement.magnitude * walkSpeed, 0.1f, Time.deltaTime);
                    break;
                case LocomotionState.Run:
                    animator.SetFloat(moveSpeedID, playerMovement.magnitude * runSpeed, 0.1f, Time.deltaTime);
                    break;
            }
        }
        else if (playerPosture == PlayerPosture.Crouch)
        {
            animator.SetFloat(postureID, crouchThreshold, 0.1f, Time.deltaTime);
            switch (locomotionState)
            {
                case LocomotionState.Idle:
                    animator.SetFloat(moveSpeedID, 0, 0.1f, Time.deltaTime);
                    break;
                default:
                    animator.SetFloat(moveSpeedID, playerMovement.magnitude * crouchSpeed, 0.1f, Time.deltaTime);
                    break;
            }
        }
        else if (playerPosture == PlayerPosture.Jumping)
        {
            animator.SetFloat(postureID, midairThreshold);
            animator.SetFloat(verticalVelID, VerticalVelocity);
            animator.SetFloat(feetTweenID, feetTween);
        }
        else if (playerPosture == PlayerPosture.Landing)
        {
            animator.SetFloat(postureID, landingThreshold, 0.03f, Time.deltaTime);
            switch (locomotionState)
            {
                case LocomotionState.Idle:
                    animator.SetFloat(moveSpeedID, 0, 0.1f, Time.deltaTime);
                    break;
                case LocomotionState.Walk:
                    animator.SetFloat(moveSpeedID, playerMovement.magnitude * walkSpeed, 0.1f, Time.deltaTime);
                    break;
                case LocomotionState.Run:
                    animator.SetFloat(moveSpeedID, playerMovement.magnitude * runSpeed, 0.1f, Time.deltaTime);
                    break;
            }
        }
        else if (playerPosture == PlayerPosture.Falling)
        {
            animator.SetFloat(postureID, midairThreshold);
            animator.SetFloat(verticalVelID, VerticalVelocity);
        }

        if (armState == ArmState.Normal && playerPosture != PlayerPosture.Jumping)
        {
            float rad = Mathf.Atan2(playerMovement.x, playerMovement.z);
            animator.SetFloat(turnSpeedID, rad, 0.1f, Time.deltaTime);
            playerTransform.Rotate(0, rad * 200 * Time.deltaTime, 0f);
        }
    }


    /// 计算前三帧的速度平均值
    /// newVel当前帧的速度平均值
    /// return平均速度
    Vector3 AverageVel(Vector3 newVel)
    {
        velCache[currentChacheIndex] = newVel;
        currentChacheIndex++;
        currentChacheIndex %= CACHE_SIZE;
        Vector3 average = Vector3.zero;
        foreach (Vector3 vel in velCache)
        {
            average += vel;
        }
        return average / CACHE_SIZE;
    }

    /// 播放脚步声
    //void PlayFootStep()
    //{
    //    if (playerPosture != PlayerPosture.Jumping && playerPosture != PlayerPosture.Falling)
    //    {
    //        if (locomotionState == LocomotionState.Walk || locomotionState == LocomotionState.Run)
    //        {
    //            //float currentFootCycle = Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);
    //            //lastFootCycle = currentFootCycle;
    //        }
    //    }
    //}

    /// 动画系统的回调方法
    private void OnAnimatorMove()
    {
        if (animator==null||characterController==null)
        {
            return;
        }

        if (playerPosture != PlayerPosture.Jumping && playerPosture != PlayerPosture.Falling)
        {
            Vector3 playerDeltaMovement = animator.deltaPosition;
            playerDeltaMovement.y = VerticalVelocity* Time.deltaTime;
            characterController.Move(playerDeltaMovement);
            averageVel = AverageVel(animator.velocity);
        }
        else
        {
            averageVel.y = VerticalVelocity;
            Vector3 playerDeltaMovement = averageVel * Time.deltaTime;
            characterController.Move(playerDeltaMovement);
        }
    }
}