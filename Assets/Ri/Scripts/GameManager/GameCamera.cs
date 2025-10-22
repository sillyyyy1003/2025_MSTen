using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class GameCamera : MonoBehaviour
{

    [Header("缩放设置")]
    public float zoomSpeed = 50f;         // 滚轮缩放速度
    public float minZoomDistance = 10f;   // 最近高度
    public float maxZoomDistance = 50f;   // 最远高度

    [Header("移动设置")]
    private float moveSpeedMinZoom = 10f;  // 缩小时的移动速度
    private float moveSpeedMaxZoom = 40f;  // 放大时的移动速度
    public float edgeScrollSpeed = 30f;   // 鼠标到屏幕边缘时的移动速度
    public int edgeSize = 10;             // 屏幕边缘触发范围（像素）

    [Header("旋转设置")]
    public float rotationSpeed = 100f;    // Q/E 旋转速度（可选）

    [Header("地图限制")]
    public HexGrid grid;

    float zoom = 0.5f;    // 当前缩放 (0~1)
    float yaw;            // 水平旋转角
    float pitch = 45f;    // 固定一个俯视角（RTS 常用）

    private bool bCanUseCamera;
  
    void Start()
    {
        yaw = transform.eulerAngles.y;
        Cursor.lockState = CursorLockMode.None; // 鼠标可见
        Cursor.visible = true;
    }

    void Update()
    {
        if(bCanUseCamera)
        {
            HandleZoom();
            HandleRotation();
            HandleMovement();
        }
    }


    public void SetCanUseCamera(bool canNot)
    {
        if (canNot)
            bCanUseCamera = true;
        else
            bCanUseCamera = false;
    }
    /// <summary>
    /// 得到玩家位置
    /// </summary>
    public void GetPlayerPosition(Vector3 playerPos)
    {
        this.transform.position = new Vector3(playerPos.x, 30, playerPos.z-21);
    }


    void HandleZoom()
    {
        float delta = -Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(delta) > 0.0001f)
        {
            zoom = Mathf.Clamp01(zoom + delta * zoomSpeed * Time.deltaTime);
        }
    }

    void HandleRotation()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            yaw = 0.0f;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            return;
        }
        // Q/E 控制水平旋转
        float rotateInput = 0f;
        if (Input.GetKey(KeyCode.Q)) rotateInput = -1f;
        if (Input.GetKey(KeyCode.E)) rotateInput = 1f;

        if (Mathf.Abs(rotateInput) > 0.0001f)
        {
            yaw += rotateInput * rotationSpeed * Time.deltaTime;
        }


       
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        
    }

    void HandleMovement()
    {
        Vector3 moveInput = Vector3.zero;

        // 键盘控制 WASD
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        moveInput += new Vector3(x, 0f, z);

        // 鼠标到屏幕边缘
         Vector3 mousePos = Input.mousePosition;
        if (mousePos.x >= 0 && mousePos.x <= Screen.width &&
     mousePos.y >= 0 && mousePos.y <= Screen.height)
        {
            if (mousePos.x <= edgeSize)
                moveInput += Vector3.left;
            if (mousePos.x >= Screen.width - edgeSize)
                moveInput += Vector3.right;
            if (mousePos.y <= edgeSize)
                moveInput += Vector3.back;
            if (mousePos.y >= Screen.height - edgeSize)
                moveInput += Vector3.forward;
            // 如果有输入
            Vector3 finalPos = transform.position;

            if (moveInput.sqrMagnitude > 0.0001f)
            {
                moveInput.Normalize();


                Quaternion yawOnly = Quaternion.Euler(0f, yaw, 0f);
                Vector3 worldDir = yawOnly * moveInput;

                // 速度随缩放变化
                float speed = Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) * edgeScrollSpeed;

                finalPos += worldDir * speed * Time.deltaTime;
            }

            // 根据缩放调整相机高度（始终应用）
            float desiredY = Mathf.Lerp(minZoomDistance, maxZoomDistance, zoom);
            finalPos.y = desiredY;

            // 最后 clamp（先 clamp x/z，再把高度写入）
            finalPos = ClampPosition(finalPos);

            // 写回
            transform.position = finalPos;
        }
    }

    Vector3 ClampPosition(Vector3 position)
    {
        if (grid == null) return position;

        // 考虑地图在世界中的起点（grid.transform.position）
        Vector3 origin = grid.transform.position;

        // X 最大值（保持和你最初近似公式一致）
        float xMax =(grid.CellCountX-1) * 10*Mathf.Sqrt(3);
        float xMin = 0;
        // Z 最大值（修正过的公式）
        float zMax = (grid.CellCountZ-1)*10+10;
        float zMin = -15 ;

        // 如果地图原点不是 (0,0)，把 origin 加进来
        float xMinWorld = origin.x+xMin;
        float xMaxWorld = origin.x + xMax;

        float zMinWorld = origin.z+zMin-this.transform.position.y/5;
        float zMaxWorld = origin.z + zMax + (maxZoomDistance - this.transform.position.y);

        position.x = Mathf.Clamp(position.x, xMinWorld, xMaxWorld);
        position.z = Mathf.Clamp(position.z, zMinWorld, zMaxWorld);

        return position;
    }
}