using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 2, -5);
    public float smoothSpeed = 0.125f;

    [Header("角度控制")]
    [Range(-90, 90)]
    public float pitch = 20f;
    [Range(-180, 180)]
    public float yaw = 0f;
    [Range(-180, 180)]
    public float roll = 0f;

    [Header("距离")]
    public float distance = 5f;
    public float minDistance = 1f;
    public float maxDistance = 20f;

    [Header("旋转平滑")]
    public bool smoothRotation = true;
    public float rotationSmoothSpeed = 5f;

    private Quaternion currentRotation;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraFollow: 没有设置目标，尝试查找Player标签");
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        currentRotation = Quaternion.Euler(pitch, yaw, roll);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, roll);

        if (smoothRotation)
        {
            currentRotation = Quaternion.Slerp(currentRotation, targetRotation,
                rotationSmoothSpeed * Time.deltaTime);
        }
        else
        {
            currentRotation = targetRotation;
        }

        Vector3 rotatedOffset = currentRotation * new Vector3(0, 0, -distance);
        Vector3 desiredPosition = target.position + rotatedOffset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
        transform.LookAt(target);
    }

    private void OnValidate()
    {
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        if (Application.isPlaying && target != null)
        {
            Quaternion targetRotation = Quaternion.Euler(pitch, yaw, roll);
            Vector3 rotatedOffset = targetRotation * new Vector3(0, 0, -distance);
            transform.position = target.position + rotatedOffset;
            transform.LookAt(target);
        }
    }
}
