using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;  // 要跟随的物体
    public Vector3 offset = new Vector3(0, 2, -5);  // 摄像机相对于目标的偏移
    public float smoothSpeed = 0.125f;  // 平滑跟随的速度

    [Header("摄像机角度控制")]
    [Range(-90, 90)]
    public float pitch = 20f;  // 俯仰角 (X轴旋转)
    [Range(-180, 180)]
    public float yaw = 0f;    // 偏航角 (Y轴旋转)
    [Range(-180, 180)]
    public float roll = 0f;    // 滚转角 (Z轴旋转)

    [Header("距离控制")]
    public float distance = 5f;  // 摄像机距离
    public float minDistance = 1f;
    public float maxDistance = 20f;

    [Header("角度插值")]
    public bool smoothRotation = true;
    public float rotationSmoothSpeed = 5f;

    // 用于存储当前旋转
    private Quaternion currentRotation;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraFollow: 没有设置目标，将在运行时查找Player标签的物体");
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        // 初始化当前旋转
        currentRotation = Quaternion.Euler(pitch, yaw, roll);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 根据角度创建旋转
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, roll);

        // 平滑旋转或立即应用
        if (smoothRotation)
        {
            currentRotation = Quaternion.Slerp(currentRotation, targetRotation,
                rotationSmoothSpeed * Time.deltaTime);
        }
        else
        {
            currentRotation = targetRotation;
        }

        // 计算偏移方向
        Vector3 rotatedOffset = currentRotation * new Vector3(0, 0, -distance);

        // 计算目标位置
        Vector3 desiredPosition = target.position + rotatedOffset;

        // 使用插值平滑移动摄像机
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // 让摄像机始终看向目标
        transform.LookAt(target);
    }

    // 在编辑器中实时预览
    private void OnValidate()
    {
        // 限制距离在合理范围内
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // 在编辑器模式下预览角度变化
        if (Application.isPlaying && target != null)
        {
            Quaternion targetRotation = Quaternion.Euler(pitch, yaw, roll);
            Vector3 rotatedOffset = targetRotation * new Vector3(0, 0, -distance);
            transform.position = target.position + rotatedOffset;
            transform.LookAt(target);
        }
    }
}