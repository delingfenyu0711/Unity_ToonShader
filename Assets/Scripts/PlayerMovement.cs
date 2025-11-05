using UnityEngine;

[RequireComponent(typeof(Rigidbody))] // 自动添加 Rigidbody 组件
public class PlayerMovement : MonoBehaviour
{
    [Header("移动设置")]
    [Tooltip("移动速度")]
    public float moveSpeed = 5f; // 移动速度（可在 Inspector 调节）
    
    [Tooltip("旋转速度（角色朝向移动方向）")]
    public float rotateSpeed = 10f; // 角色旋转速度

    [Header("跳跃设置")]
    [Tooltip("跳跃力度")]
    public float jumpForce = 7f; // 跳跃力度
    [Tooltip("是否允许二段跳")]
    public bool canDoubleJump = false; // 是否支持二段跳
    private int jumpCount = 0; // 跳跃次数计数
    private int maxJumpCount => canDoubleJump ? 2 : 1; // 最大跳跃次数

    [Header("地面检测")]
    [Tooltip("地面检测范围")]
    public float groundCheckRadius = 0.3f; // 检测半径
    [Tooltip("地面检测点（相对于角色中心）")]
    public Transform groundCheckPoint; // 检测点（建议在角色脚底）
    [Tooltip("哪些层是地面")]
    public LayerMask groundLayer; // 地面图层（需在 Inspector 赋值）
    private bool isGrounded; // 是否在地面上

    private Rigidbody rb; // 刚体组件引用
    private Vector3 moveDirection; // 移动方向

    void Start()
    {
        groundLayer =  LayerMask.GetMask("Ground");
        // 获取刚体组件
        rb = GetComponent<Rigidbody>();
        
        // 锁定刚体旋转（防止角色摔倒）
        rb.freezeRotation = true;
        groundCheckPoint = transform.Find("GroundCheckPoint");
    }

    void Update()
    {
        Shader.SetGlobalVector("_PlayerPostion", this.transform.position);
        // 1. 地面检测（每帧检测是否在地面）
        GroundCheck();

        // 2. 获取键盘输入（WASD 或方向键）
        float horizontal = Input.GetAxis("Horizontal"); // 左右（A/D 或 ←/→）
        float vertical = Input.GetAxis("Vertical"); // 前后（W/S 或 ↑/↓）

        // 3. 计算移动方向（基于相机视角，让移动与相机朝向一致）
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // 获取相机的水平朝向（忽略 Y 轴，避免上下移动）
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraRight = mainCamera.transform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // 计算最终移动方向（相对相机的前后左右）
            moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;
        }
        else
        {
            // 若无相机，使用世界坐标系（前后为 Z 轴，左右为 X 轴）
            moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
        }

        // 4. 角色旋转（朝向移动方向）
        if (moveDirection.magnitude > 0.1f) // 有移动输入时才旋转
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            // 平滑旋转
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }

        // 5. 跳跃输入检测（空格跳跃）
        if (Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumpCount)
        {
            Jump();
        }
    }

    // 物理相关逻辑（用 FixedUpdate 保证帧率稳定）
    void FixedUpdate()
    {
        // 6. 应用移动力（刚体移动，避免穿模）
        Vector3 moveVelocity = moveDirection * moveSpeed;
        // 保留 Y 轴速度（跳跃/重力），只修改 XZ 轴移动
        moveVelocity.y = rb.velocity.y;
        rb.velocity = moveVelocity;
    }

    // 地面检测逻辑
    void GroundCheck()
    {
        // 用球形检测判断是否接触地面（groundLayer 为地面图层）
        isGrounded = Physics.CheckSphere(groundCheckPoint.position, groundCheckRadius, groundLayer);

        // 落地时重置跳跃次数
        if (isGrounded)
        {
            jumpCount = 0;
        }
    }

    // 跳跃逻辑
    void Jump()
    {
        // 重置 Y 轴速度（避免跳跃叠加）
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        // 施加向上的力
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        // 跳跃次数 +1
        jumpCount++;
    }

    // 场景视图绘制检测范围（方便调试）
    void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }
}