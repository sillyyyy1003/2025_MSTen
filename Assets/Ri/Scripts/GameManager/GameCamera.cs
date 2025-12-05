using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class GameCamera : MonoBehaviour
{
    //[Header("缩放设置")]
    //private float zoomSpeed = 2f;             // 滚轮缩放速度

    [Header("视角设置 - 三档锁定")]
    private float minPitch = 30f;             // 档位1：平视
    private float midPitch = 45f;             // 档位2：斜视
    private float maxPitch = 70f;             // 档位3：俯视

    private float minZoomDistance = 10f;       // 档位1距离
    private float midZoomDistance = 25f;      // 档位2距离
    private float maxZoomDistance = 50f;      // 档位3距离

    [Header("移动设置")]
    private float moveSpeed = 50f;            // WASD移动速度
    private float edgeScrollSpeed = 50f;     // 鼠标到屏幕边缘时的移动速度
    private int edgeSize = 20;                // 屏幕边缘触发范围（像素）

    [Header("平滑设置")]
    private float rotationSmoothTime = 0.8f;  // 旋转平滑时间
    private float zoomSmoothTime = 0.5f;      // 缩放平滑时间

    [Header("滚轮灵敏度")]
    private float scrollThreshold = 0.1f;     // 滚轮触发档位切换的阈值

    [Header("旋转设置")]
    private float rotationSpeed = 100f;       // Q/E 旋转速度

    [Header("调试")]
    public bool showDebugInfo = false;       // 是否显示调试信息（在Inspector中勾选查看）

    [Header("地图限制")]
    public HexGrid grid;

    // 中心点相关
    private Vector3 focusPoint;              // 当前关注的中心点

    // 缩放相关
    private float currentDistance;           // 当前距离
    private float targetDistance;            // 目标距离
    private float distanceVelocity;          // 用于平滑缩放的速度

    // 旋转相关
    private float currentPitch;              // 当前俯角
    private float targetPitch;               // 目标俯角
    private float pitchVelocity;             // 用于平滑旋转的速度
    private float yaw;                       // 水平旋转角

    // 档位控制
    private int currentZoomLevel = 1;        // 当前档位：0=近(平视), 1=中(斜视), 2=远(俯视)
    private float scrollAccumulator = 0f;    // 滚轮累积值，用于防止过快切换

    public bool bCanUseCamera;

    void Start()
    {
	    bCanUseCamera = false;
	    Initialize();

    }

    void Update()
    {
        if (bCanUseCamera)
        {
            HandleZoom();
            HandleRotation();
            HandleMovement();
            UpdateCameraPosition();
        }
    }

    public void SetCanUseCamera(bool canUse)
    {
        bCanUseCamera = canUse;
    }



    /// <summary>
    /// 镜头的初始化
    /// </summary>
    public void Initialize()
    {
	    yaw = transform.eulerAngles.y;

	    // 初始化中心点为当前摄像机前方的点
	    focusPoint = transform.position + transform.forward * 20f;

	    // 初始化为中档（斜视）
	    currentZoomLevel = 1;
	    currentDistance = maxZoomDistance;
        targetDistance = currentDistance;
	    currentPitch = midPitch;
	    targetPitch = currentPitch;

	    Cursor.lockState = CursorLockMode.None;
	    Cursor.visible = true;

		Debug.Log($"<color=cyan>GameCamera初始化 - edgeScrollSpeed: {edgeScrollSpeed}, moveSpeed: {moveSpeed}, edgeSize: {edgeSize}</color>");
	  
    }


    /// <summary>
    /// 设置玩家跟踪的位置 - 重置中心点
    /// </summary>
    public void GetPlayerPosition(Vector3 playerPos)
    {
        // 重置中心点为玩家位置（立即设置，无平滑）
        focusPoint = new Vector3(playerPos.x, playerPos.y + 7f, playerPos.z);
    }

    void HandleZoom()
    {
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scrollDelta) > 0.0001f)
        {
            scrollAccumulator += scrollDelta;

            // 向下滚动（正值）= 拉近，向上滚动（负值）= 拉远
            if (scrollAccumulator >= scrollThreshold)
            {
                // 拉近一档
                scrollAccumulator = 0f;
                SwitchZoomLevel(-1);
            }
            else if (scrollAccumulator <= -scrollThreshold)
            {
                // 拉远一档
                scrollAccumulator = 0f;
                SwitchZoomLevel(1);
            }
        }
        else
        {
            // 没有滚动时，慢慢衰减累积值
            scrollAccumulator = Mathf.Lerp(scrollAccumulator, 0f, Time.deltaTime * 5f);
        }
    }

    /// <summary>
    /// 切换缩放档位
    /// </summary>
    /// <param name="direction">-1=拉近, 1=拉远</param>
    void SwitchZoomLevel(int direction)
    {
        int newLevel = Mathf.Clamp(currentZoomLevel + direction, 0, 2);

        if (newLevel != currentZoomLevel)
        {
            currentZoomLevel = newLevel;

            // 根据档位设置目标距离和俯角
            switch (currentZoomLevel)
            {
                case 0: // 平视
                    targetDistance = minZoomDistance;
                    targetPitch = minPitch;
                    break;
                case 1: // 斜视
                    targetDistance = midZoomDistance;
                    targetPitch = midPitch;
                    break;
                case 2: // 俯视
                    targetDistance = maxZoomDistance;
                    targetPitch = maxPitch;
                    break;
            }
        }
    }

    void HandleRotation()
    {
        // F键重置旋转
        if (Input.GetKeyDown(KeyCode.F))
        {
            yaw = 0.0f;
        }

        // Q/E 控制水平旋转
        float rotateInput = 0f;
        if (Input.GetKey(KeyCode.Q)) rotateInput = -1f;
        if (Input.GetKey(KeyCode.E)) rotateInput = 1f;

        if (Mathf.Abs(rotateInput) > 0.0001f)
        {
            yaw += rotateInput * rotationSpeed * Time.deltaTime;
        }
    }

    void HandleMovement()
    {
        if (GameManage.Instance.IsPointerOverUIElement())
        {
            if (showDebugInfo)
            {
                Debug.Log("<color=yellow>鼠标在UI上，跳过移动</color>");
            }
            return;
        }

        Vector3 keyboardInput = Vector3.zero;
        Vector3 edgeInput = Vector3.zero;

        // 键盘控制 WASD
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        if (Mathf.Abs(x) > 0.01f || Mathf.Abs(z) > 0.01f)
        {
            keyboardInput = new Vector3(x, 0f, z);
        }

        // 鼠标到屏幕边缘
        Vector3 mousePos = Input.mousePosition;
        if (mousePos.x >= 0 && mousePos.x <= Screen.width &&
            mousePos.y >= 0 && mousePos.y <= Screen.height)
        {
            bool atEdge = false;
            string edgeInfo = "";

            if (mousePos.x <= edgeSize)
            {
                edgeInput += Vector3.left;
                atEdge = true;
                edgeInfo = "左边缘";
            }
            if (mousePos.x >= Screen.width - edgeSize)
            {
                edgeInput += Vector3.right;
                atEdge = true;
                edgeInfo = "右边缘";
            }
            if (mousePos.y <= edgeSize)
            {
                edgeInput += Vector3.back;
                atEdge = true;
                edgeInfo += " 下边缘";
            }
            if (mousePos.y >= Screen.height - edgeSize)
            {
                edgeInput += Vector3.forward;
                atEdge = true;
                edgeInfo += " 上边缘";
            }

            if (atEdge && showDebugInfo)
            {
                Debug.Log($"<color=green>边缘检测:{edgeInfo} - 鼠标:{mousePos}, 屏幕:{Screen.width}x{Screen.height}, 边缘大小:{edgeSize}, 速度:{edgeScrollSpeed}</color>");
            }
        }

        // 根据相机的yaw旋转移动方向
        Quaternion yawOnly = Quaternion.Euler(0f, yaw, 0f);

        // 分别处理键盘和边缘输入（使用不同的速度）
        if (keyboardInput.sqrMagnitude > 0.0001f)
        {
            keyboardInput.Normalize();
            Vector3 worldDir = yawOnly * keyboardInput;
            Vector3 movement = worldDir * moveSpeed * Time.deltaTime;
            focusPoint += movement;

            if (showDebugInfo)
            {
                Debug.Log($"<color=blue>键盘移动 - 速度:{moveSpeed}, 移动量:{movement.magnitude}</color>");
            }
        }

        if (edgeInput.sqrMagnitude > 0.0001f)
        {
            edgeInput.Normalize();
            Vector3 worldDir = yawOnly * edgeInput;
            Vector3 movement = worldDir * edgeScrollSpeed * Time.deltaTime;
            focusPoint += movement;

            if (showDebugInfo)
            {
                Debug.Log($"<color=red>边缘移动 - 速度:{edgeScrollSpeed}, 移动量:{movement.magnitude}, deltaTime:{Time.deltaTime}</color>");
            }
        }

        // 限制中心点在地图范围内
        focusPoint = ClampFocusPoint(focusPoint);
    }

    void UpdateCameraPosition()
    {
        // 平滑缩放距离
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, zoomSmoothTime);

        // 平滑旋转俯角
        currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, rotationSmoothTime);

        // 计算相机位置（从中心点向后偏移）
        Quaternion rotation = Quaternion.Euler(currentPitch, yaw, 0f);
        Vector3 offset = rotation * Vector3.back * currentDistance * 2;

        transform.position = focusPoint + offset;
        transform.rotation = rotation;
    }

    Vector3 ClampFocusPoint(Vector3 point)
    {
        if (grid == null) return point;

        Vector3 origin = grid.transform.position;

        // X 范围
        float xMax = (grid.CellCountX - 1) * 10 * Mathf.Sqrt(3);
        float xMin = 0;

        // Z 范围
        float zMax = (grid.CellCountZ - 1) * 10 + 10;
        float zMin = -15;

        float xMinWorld = origin.x + xMin;
        float xMaxWorld = origin.x + xMax;
        float zMinWorld = origin.z + zMin;
        float zMaxWorld = origin.z + zMax;

        point.x = Mathf.Clamp(point.x, xMinWorld, xMaxWorld);
        point.z = Mathf.Clamp(point.z, zMinWorld, zMaxWorld);

        // 保持中心点在地面附近
        point.y = 10f;

        return point;
    }

    void OnGUI()
    {
        if (showDebugInfo)
        {
            GUI.Label(new Rect(10, 10, 400, 20), $"边缘速度: {edgeScrollSpeed}");
            GUI.Label(new Rect(10, 30, 400, 20), $"键盘速度: {moveSpeed}");
            GUI.Label(new Rect(10, 50, 400, 20), $"鼠标位置: {Input.mousePosition}");
            GUI.Label(new Rect(10, 70, 400, 20), $"屏幕尺寸: {Screen.width} x {Screen.height}");
            GUI.Label(new Rect(10, 90, 400, 20), $"边缘检测范围: {edgeSize} 像素");
        }
    }
}
